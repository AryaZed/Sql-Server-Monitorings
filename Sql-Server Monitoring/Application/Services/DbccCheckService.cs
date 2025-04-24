using Microsoft.Data.SqlClient;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;
using System.Data;

namespace Sql_Server_Monitoring.Application.Services
{
    public class DbccCheckService : IDbccCheckService
    {
        private readonly ILogger<DbccCheckService> _logger;
        private readonly IIssueRepository _issueRepository;
        private readonly IAlertService _alertService;

        public DbccCheckService(
            ILogger<DbccCheckService> logger,
            IIssueRepository issueRepository,
            IAlertService alertService)
        {
            _logger = logger;
            _issueRepository = issueRepository;
            _alertService = alertService;
        }

        public async Task<IEnumerable<DbccCheckHistory>> GetDbccCheckHistoryAsync(string connectionString)
        {
            var results = new List<DbccCheckHistory>();

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                string sql = @"
                    SELECT
                        d.name AS DatabaseName,
                        MAX(CASE WHEN eh.message_id = 5242 THEN eh.timestamp END) AS LastGoodCheckDate,
                        'CHECKDB' AS CheckType,
                        CASE 
                            WHEN MAX(CASE WHEN eh.message_id IN (
                                3604,  -- DBCC results
                                7926,  -- Check complete errors
                                7928,  -- Corruption summary
                                7930,  -- Table errors
                                7931,  -- Index errors
                                7932,  -- Data page errors
                                7933   -- Page count errors
                            ) THEN 1 ELSE 0 END) = 1 THEN 1
                            ELSE 0
                        END AS HasErrors,
                        MAX(CASE WHEN eh.message_id IN (
                            3604,  -- DBCC results
                            7926,  -- Check complete errors
                            7928,  -- Corruption summary
                            7930,  -- Table errors
                            7931,  -- Index errors
                            7932,  -- Data page errors
                            7933   -- Page count errors
                        ) THEN eh.message ELSE NULL END) AS ErrorMessage
                    FROM 
                        master.sys.databases d
                    LEFT JOIN msdb.dbo.sysjobhistory jh ON jh.step_name LIKE '%CHECKDB%' OR jh.step_name LIKE '%DBCC%'
                    LEFT JOIN msdb.dbo.sysjobsteps js ON js.job_id = jh.job_id AND js.step_id = jh.step_id
                    LEFT JOIN msdb.dbo.sysjobs j ON j.job_id = js.job_id
                    LEFT JOIN msdb.dbo.sysjobhistory h ON h.job_id = j.job_id
                    LEFT JOIN sys.dm_exec_requests r ON r.command LIKE '%DBCC%' OR r.command LIKE '%CHECKDB%'
                    LEFT JOIN sys.dm_exec_sessions s ON s.session_id = r.session_id
                    LEFT JOIN sys.traces t ON t.id = 1
                    LEFT JOIN ::fn_trace_gettable(CONVERT(VARCHAR(150), t.path), DEFAULT) ft ON ft.TextData LIKE '%DBCC%' OR ft.TextData LIKE '%CHECKDB%'
                    LEFT JOIN msdb.dbo.sysdtslog90 dl ON dl.description LIKE '%CHECKDB%' OR dl.description LIKE '%DBCC%'
                    LEFT JOIN msdb.dbo.sysssislog sl ON sl.message LIKE '%CHECKDB%' OR sl.message LIKE '%DBCC%'
                    LEFT JOIN sys.event_log el ON el.message LIKE '%CHECKDB%' OR el.message LIKE '%DBCC%'
                    LEFT JOIN sys.dm_os_ring_buffers rb ON rb.ring_buffer_type = 'RING_BUFFER_DBCC'
                    LEFT JOIN sys.messages m ON m.message_id IN (5242, 3604, 7926, 7928, 7930, 7931, 7932, 7933) AND m.language_id = 1033
                    LEFT JOIN sys.dm_exec_session_wait_stats w ON w.session_id = s.session_id AND w.wait_type = 'DBCC_CHECKDB'
                    LEFT JOIN (
                        SELECT 
                            session_id, 
                            module, 
                            timestamp AS execution_time,
                            message_id, 
                            message
                        FROM 
                            sys.dm_exec_sessions es
                        CROSS APPLY 
                            sys.dm_exec_query_plan(plan_handle) qp
                        CROSS APPLY 
                            sys.dm_exec_sql_text(sql_handle) st
                        WHERE 
                            module LIKE '%DBCC%' OR st.text LIKE '%DBCC%'
                    ) eh ON eh.session_id = s.session_id
                    WHERE
                        d.state_desc = 'ONLINE'
                    GROUP BY
                        d.name
                    ORDER BY
                        d.name;";

                await using var command = new SqlCommand(sql, connection);
                command.CommandTimeout = 300; // 5-minute timeout
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var dbCheck = new DbccCheckHistory
                    {
                        DatabaseName = reader["DatabaseName"].ToString(),
                        CheckType = reader["CheckType"].ToString(),
                        HasErrors = Convert.ToBoolean(reader["HasErrors"])
                    };

                    if (reader["LastGoodCheckDate"] != DBNull.Value)
                    {
                        dbCheck.LastGoodCheckDate = Convert.ToDateTime(reader["LastGoodCheckDate"]);
                    }

                    if (reader["ErrorMessage"] != DBNull.Value)
                    {
                        dbCheck.ErrorMessage = reader["ErrorMessage"].ToString();
                    }

                    results.Add(dbCheck);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving DBCC check history");
                throw;
            }
        }

        public async Task<IEnumerable<DbIssue>> AnalyzeDbccChecksAsync(string connectionString)
        {
            var issues = new List<DbIssue>();

            try
            {
                var dbccChecks = await GetDbccCheckHistoryAsync(connectionString);

                // Check for databases with errors
                var databasesWithErrors = dbccChecks.Where(c => c.HasErrors);
                foreach (var db in databasesWithErrors)
                {
                    var issue = new DbIssue
                    {
                        Type = IssueType.Corruption,
                        Severity = IssueSeverity.Critical,
                        Message = $"DBCC check detected corruption in database '{db.DatabaseName}'",
                        RecommendedAction = "Restore from a backup or repair the database. Contact Microsoft Support for assistance.",
                        DatabaseName = db.DatabaseName,
                        AffectedObject = db.DatabaseName,
                        DetectionTime = DateTime.Now
                    };

                    issues.Add(issue);
                    await _issueRepository.AddIssueAsync(issue);

                    // Generate alert
                    await _alertService.CreateAlertAsync(
                        connectionString,
                        db.DatabaseName,
                        AlertType.CorruptionDetected,
                        issue.Message,
                        issue.RecommendedAction,
                        IssueSeverity.Critical);
                }

                // Check for databases without a recent DBCC check
                var threshold = DateTime.Now.AddDays(-7); // One week threshold
                var databasesWithoutRecentCheck = dbccChecks
                    .Where(c => c.LastGoodCheckDate == DateTime.MinValue || c.LastGoodCheckDate < threshold);

                foreach (var db in databasesWithoutRecentCheck)
                {
                    var daysSinceLastCheck = db.LastGoodCheckDate == DateTime.MinValue ? 
                        "Never" : 
                        ((int)(DateTime.Now - db.LastGoodCheckDate).TotalDays).ToString();

                    var issue = new DbIssue
                    {
                        Type = IssueType.Corruption,
                        Severity = IssueSeverity.Medium,
                        Message = $"Database '{db.DatabaseName}' has not had a successful DBCC CHECKDB in {daysSinceLastCheck} days",
                        RecommendedAction = "Run DBCC CHECKDB to verify database integrity",
                        DatabaseName = db.DatabaseName,
                        AffectedObject = db.DatabaseName,
                        DetectionTime = DateTime.Now,
                        SqlScript = $"DBCC CHECKDB (N'{db.DatabaseName}') WITH NO_INFOMSGS, ALL_ERRORMSGS;"
                    };

                    issues.Add(issue);
                    await _issueRepository.AddIssueAsync(issue);

                    // Only create an alert if it has been more than 14 days since the last check
                    if (db.LastGoodCheckDate == DateTime.MinValue || db.LastGoodCheckDate < DateTime.Now.AddDays(-14))
                    {
                        await _alertService.CreateAlertAsync(
                            connectionString,
                            db.DatabaseName,
                            AlertType.DbccCheckFailure,
                            issue.Message,
                            issue.RecommendedAction,
                            IssueSeverity.Medium);
                    }
                }

                return issues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing DBCC checks");
                throw;
            }
        }

        public async Task<bool> RunDbccCheckAsync(string connectionString, string databaseName)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Begin tracking execution time
                var startTime = DateTime.Now;

                string dbccSql = $"DBCC CHECKDB (N'{databaseName}') WITH NO_INFOMSGS, ALL_ERRORMSGS;";
                await using var command = new SqlCommand(dbccSql, connection);
                command.CommandTimeout = 3600; // 1-hour timeout for large databases

                // Execute DBCC CHECKDB
                await command.ExecuteNonQueryAsync();

                // Calculate execution time
                var duration = DateTime.Now - startTime;

                // Record the successful check
                var check = new DbccCheckHistory
                {
                    DatabaseName = databaseName,
                    LastGoodCheckDate = DateTime.Now,
                    CheckType = "CHECKDB",
                    HasErrors = false,
                    Duration = duration
                };

                // Log the successful check
                _logger.LogInformation($"DBCC CHECKDB completed successfully for database '{databaseName}' in {duration.TotalSeconds:N1} seconds");

                return true;
            }
            catch (SqlException ex)
            {
                // Check if the error contains corruption messages
                bool hasCorruption = ex.Message.Contains("corruption") || 
                                    ex.Message.Contains("damaged") || 
                                    ex.Message.Contains("torn") ||
                                    ex.Number == 8939 || // Table error
                                    ex.Number == 8928 || // Object inconsistency
                                    ex.Number == 8966 || // Unable to read allocation structure
                                    ex.Number == 8976;   // Extent inconsistency

                if (hasCorruption)
                {
                    var issue = new DbIssue
                    {
                        Type = IssueType.Corruption,
                        Severity = IssueSeverity.Critical,
                        Message = $"DBCC CHECKDB detected corruption in database '{databaseName}': {ex.Message}",
                        RecommendedAction = "Restore from a backup or repair the database. Contact Microsoft Support for assistance.",
                        DatabaseName = databaseName,
                        AffectedObject = databaseName,
                        DetectionTime = DateTime.Now
                    };

                    await _issueRepository.AddIssueAsync(issue);

                    // Generate alert
                    await _alertService.CreateAlertAsync(
                        connectionString,
                        databaseName,
                        AlertType.CorruptionDetected,
                        issue.Message,
                        issue.RecommendedAction,
                        IssueSeverity.Critical);
                }

                _logger.LogError(ex, $"Error running DBCC CHECKDB for database '{databaseName}'");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error running DBCC CHECKDB for database '{databaseName}'");
                return false;
            }
        }
    }
} 