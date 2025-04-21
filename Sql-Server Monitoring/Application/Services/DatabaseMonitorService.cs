using Microsoft.Data.SqlClient;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Application.Services
{
    public class DatabaseMonitorService : IDatabaseMonitorService
    {
        private readonly IDatabaseRepository _databaseRepository;
        private readonly ISettingsRepository _settingsRepository;
        private readonly IIssueRepository _issueRepository;
        private readonly IAlertService _alertService;
        private readonly ILogger<DatabaseMonitorService> _logger;
        private Timer _monitoringTimer;
        private bool _isMonitoring;

        public DatabaseMonitorService(
            IDatabaseRepository databaseRepository,
            ISettingsRepository settingsRepository,
            IIssueRepository issueRepository,
            IAlertService alertService,
            ILogger<DatabaseMonitorService> logger)
        {
            _databaseRepository = databaseRepository;
            _settingsRepository = settingsRepository;
            _issueRepository = issueRepository;
            _alertService = alertService;
            _logger = logger;
        }

        public async Task StartMonitoringAsync(string connectionString, CancellationToken cancellationToken)
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("Monitoring is already running.");
                return;
            }

            try
            {
                var settings = await _settingsRepository.GetMonitoringSettingsAsync();
                int intervalSeconds = settings.MonitoringIntervalSeconds;

                _logger.LogInformation($"Starting database monitoring with interval of {intervalSeconds} seconds.");
                _isMonitoring = true;

                // Create timer that triggers the monitoring process
                _monitoringTimer = new Timer(
                    async _ => await RunMonitoringCycleAsync(connectionString),
                    null,
                    TimeSpan.Zero,  // Start immediately
                    TimeSpan.FromSeconds(intervalSeconds));  // Then run at specified interval

                // Wait for cancellation
                await Task.Run(() =>
                {
                    cancellationToken.Register(() =>
                    {
                        _monitoringTimer?.Dispose();
                        _isMonitoring = false;
                        _logger.LogInformation("Database monitoring stopped.");
                    });
                });
            }
            catch (Exception ex)
            {
                _isMonitoring = false;
                _logger.LogError(ex, "Error starting database monitoring.");
                throw;
            }
        }

        public Task StopMonitoringAsync()
        {
            _monitoringTimer?.Dispose();
            _monitoringTimer = null;
            _isMonitoring = false;
            _logger.LogInformation("Database monitoring stopped.");
            return Task.CompletedTask;
        }

        public async Task<ServerPerformanceMetrics> GetCurrentMetricsAsync(string connectionString)
        {
            try
            {
                return await _databaseRepository.GetServerPerformanceMetricsAsync(connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current server performance metrics.");
                throw;
            }
        }

        public async Task<IEnumerable<DbIssue>> DetectPerformanceIssuesAsync(string connectionString)
        {
            var issues = new List<DbIssue>();

            try
            {
                var metrics = await GetCurrentMetricsAsync(connectionString);
                var settings = await _settingsRepository.GetMonitoringSettingsAsync();

                // Check for high CPU utilization
                if (metrics.Cpu.UtilizationPercent > settings.HighCpuThresholdPercent)
                {
                    var issue = new DbIssue
                    {
                        Type = IssueType.Performance,
                        Severity = IssueSeverity.High,
                        Message = $"High CPU utilization: {metrics.Cpu.UtilizationPercent:N1}%",
                        RecommendedAction = "Investigate high CPU query patterns and consider optimizing queries or adding resources",
                        DetectionTime = DateTime.Now
                    };

                    issues.Add(issue);
                    await _issueRepository.AddIssueAsync(issue);

                    // Send alert if enabled
                    await TriggerAlertIfEnabledAsync(connectionString, null, issue, AlertType.HighCpu);
                }

                // Check for low page life expectancy
                if (metrics.Memory.PageLifeExpectancy < settings.LowPageLifeExpectancyThreshold)
                {
                    var issue = new DbIssue
                    {
                        Type = IssueType.Performance,
                        Severity = IssueSeverity.Medium,
                        Message = $"Low page life expectancy: {metrics.Memory.PageLifeExpectancy:N0} seconds",
                        RecommendedAction = "Consider adding more memory to the SQL Server instance or optimizing queries with high memory usage",
                        DetectionTime = DateTime.Now
                    };

                    issues.Add(issue);
                    await _issueRepository.AddIssueAsync(issue);

                    // Send alert if enabled
                    await TriggerAlertIfEnabledAsync(connectionString, null, issue, AlertType.LowMemory);
                }

                // Check for IO bottlenecks
                foreach (var ioStat in metrics.Disk.IoStats)
                {
                    bool hasLatencyIssue = ioStat.ReadLatencyMs > 20 || ioStat.WriteLatencyMs > 20;

                    if (hasLatencyIssue)
                    {
                        var issue = new DbIssue
                        {
                            Type = IssueType.Performance,
                            Severity = IssueSeverity.Medium,
                            Message = $"IO bottleneck detected for database '{ioStat.DatabaseName}': Read latency = {ioStat.ReadLatencyMs:N0}ms, Write latency = {ioStat.WriteLatencyMs:N0}ms",
                            RecommendedAction = "Consider moving database files to faster storage or optimizing IO-intensive queries",
                            DatabaseName = ioStat.DatabaseName,
                            AffectedObject = ioStat.FileName,
                            DetectionTime = DateTime.Now
                        };

                        issues.Add(issue);
                        await _issueRepository.AddIssueAsync(issue);

                        // Send alert if enabled
                        await TriggerAlertIfEnabledAsync(connectionString, ioStat.DatabaseName, issue, AlertType.IoBottleneck);
                    }
                }

                // Check for blocking and deadlocks
                var blockingIssues = await CheckForBlockingIssuesAsync(connectionString);
                issues.AddRange(blockingIssues);

                // Check for long-running queries
                var longRunningQueryIssues = await CheckForLongRunningQueriesAsync(connectionString, settings.LongRunningQueryThresholdSec);
                issues.AddRange(longRunningQueryIssues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting performance issues.");
            }

            return issues;
        }

        private async Task RunMonitoringCycleAsync(string connectionString)
        {
            try
            {
                _logger.LogInformation("Running monitoring cycle...");

                // Get settings to determine what to monitor
                var settings = await _settingsRepository.GetMonitoringSettingsAsync();

                // Get server performance metrics
                var metrics = await GetCurrentMetricsAsync(connectionString);

                // Store metrics for historical analysis
                await StoreMetricsAsync(connectionString, metrics);

                // Check for performance issues
                await DetectPerformanceIssuesAsync(connectionString);

                // Analyze each user database
                var databases = await _databaseRepository.GetUserDatabasesAsync(connectionString);
                foreach (var dbName in databases)
                {
                    // Skip system databases
                    if (dbName == "master" || dbName == "model" || dbName == "msdb" || dbName == "tempdb")
                        continue;

                    try
                    {
                        // Monitor database-specific metrics
                        await MonitorDatabaseAsync(connectionString, dbName, settings);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error monitoring database '{dbName}'");
                    }
                }

                // Purge old metrics if required
                if (settings.RetentionDays > 0)
                {
                    await _databaseRepository.PurgeOldMetricsAsync(connectionString, settings.RetentionDays);
                }

                _logger.LogInformation("Monitoring cycle completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during monitoring cycle.");
            }
        }

        private async Task StoreMetricsAsync(string connectionString, ServerPerformanceMetrics metrics)
        {
            try
            {
                // Store metrics for historical analysis
                // In a real implementation, this would save to a repository
                _logger.LogInformation($"Storing performance metrics from {metrics.CollectionTime}");

                // For this example, we'll just log some key metrics
                _logger.LogInformation($"CPU: {metrics.Cpu.UtilizationPercent:N1}%, " +
                                      $"Memory: {metrics.Memory.TotalServerMemoryMB:N0} MB, " +
                                      $"PLE: {metrics.Memory.PageLifeExpectancy:N0} sec");

                foreach (var wait in metrics.TopWaits)
                {
                    _logger.LogInformation($"Wait type: {wait.WaitType}, " +
                                          $"Time: {wait.WaitTimeMs:N0} ms, " +
                                          $"Tasks: {wait.WaitingTasksCount:N0}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing performance metrics.");
            }
        }

        private async Task MonitorDatabaseAsync(string connectionString, string dbName, MonitoringSettings settings)
        {
            _logger.LogInformation($"Monitoring database '{dbName}'...");

            // In a real implementation, you would monitor database-specific metrics here
            // For this example, we'll just log that we're monitoring the database

            // Check database growth
            var dbDetails = await _databaseRepository.GetDatabaseDetailsAsync(connectionString, dbName);

            // Check file space usage
            foreach (var file in dbDetails.Files)
            {
                if (file.PercentUsed > 90)
                {
                    var issue = new DbIssue
                    {
                        Type = IssueType.Capacity,
                        Severity = IssueSeverity.High,
                        Message = $"Database file '{file.Name}' in '{dbName}' is {file.PercentUsed}% full",
                        RecommendedAction = "Increase file size or enable auto-growth with appropriate settings",
                        DatabaseName = dbName,
                        AffectedObject = file.Name,
                        DetectionTime = DateTime.Now
                    };

                    await _issueRepository.AddIssueAsync(issue);

                    // Send alert if enabled
                    await TriggerAlertIfEnabledAsync(connectionString, dbName, issue, AlertType.DiskSpace);
                }
            }

            // Check for long-running transactions
            if (settings.MonitorQueries)
            {
                var longRunningTransactions = await CheckForLongRunningTransactionsAsync(connectionString, dbName);
                foreach (var issue in longRunningTransactions)
                {
                    await _issueRepository.AddIssueAsync(issue);

                    // Send alert if enabled
                    await TriggerAlertIfEnabledAsync(connectionString, dbName, issue, AlertType.LongRunningQuery);
                }
            }

            _logger.LogInformation($"Monitoring of database '{dbName}' completed.");
        }

        private async Task<IEnumerable<DbIssue>> CheckForBlockingIssuesAsync(string connectionString)
        {
            var issues = new List<DbIssue>();

            try
            {
                var query = @"
                    WITH BlockingHierarchy AS (
                        SELECT
                            request_session_id AS session_id,
                            CAST('' AS nvarchar(max)) AS blocking_session_id,
                            CAST(0 AS int) AS blocking_level,
                            wait_time_ms,
                            wait_type
                        FROM sys.dm_os_waiting_tasks
                        WHERE blocking_session_id IS NULL
                        AND wait_type LIKE 'LCK%'
                        
                        UNION ALL
                        
                        SELECT
                            wt.request_session_id AS session_id,
                            CAST(wt.blocking_session_id AS nvarchar(max)) AS blocking_session_id,
                            bh.blocking_level + 1 AS blocking_level,
                            wt.wait_time_ms,
                            wt.wait_type
                        FROM sys.dm_os_waiting_tasks wt
                        JOIN BlockingHierarchy bh ON wt.blocking_session_id = bh.session_id
                        WHERE wt.wait_type LIKE 'LCK%'
                    )
                    SELECT 
                        bh.session_id,
                        bh.blocking_session_id,
                        bh.blocking_level,
                        bh.wait_time_ms / 1000.0 AS wait_time_seconds,
                        bh.wait_type,
                        DB_NAME(r.database_id) AS database_name,
                        OBJECT_NAME(p.object_id, p.database_id) AS object_name,
                        s.login_name,
                        s.host_name,
                        s.program_name,
                        SUBSTRING(t.text, (r.statement_start_offset / 2) + 1,
                            ((CASE r.statement_end_offset
                                WHEN -1 THEN DATALENGTH(t.text)
                                ELSE r.statement_end_offset
                            END - r.statement_start_offset) / 2) + 1) AS current_statement
                    FROM BlockingHierarchy bh
                    JOIN sys.dm_exec_sessions s ON bh.session_id = s.session_id
                    LEFT JOIN sys.dm_exec_requests r ON bh.session_id = r.session_id
                    LEFT JOIN sys.partitions p ON r.wait_resource LIKE 'KEY%' AND p.hobt_id = SUBSTRING(r.wait_resource, 6, CHARINDEX(':', r.wait_resource, 6) - 6)
                    OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) t
                    WHERE bh.blocking_level > 0
                    AND bh.wait_time_ms > 10000  -- 10 seconds
                    ORDER BY bh.blocking_level DESC;";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var sessionId = reader.GetInt32(0);
                    var blockingSessionId = reader.GetString(1);
                    var blockingLevel = reader.GetInt32(2);
                    var waitTimeSeconds = reader.GetDouble(3);
                    var waitType = reader.GetString(4);
                    var databaseName = reader.IsDBNull(5) ? "Unknown" : reader.GetString(5);
                    var objectName = reader.IsDBNull(6) ? "Unknown" : reader.GetString(6);
                    var loginName = reader.IsDBNull(7) ? "Unknown" : reader.GetString(7);
                    var hostName = reader.IsDBNull(8) ? "Unknown" : reader.GetString(8);
                    var programName = reader.IsDBNull(9) ? "Unknown" : reader.GetString(9);
                    var currentStatement = reader.IsDBNull(10) ? "Unknown" : reader.GetString(10);

                    var issue = new DbIssue
                    {
                        Type = IssueType.Performance,
                        Severity = waitTimeSeconds > 60 ? IssueSeverity.High : IssueSeverity.Medium,
                        Message = $"Blocking detected: Session {sessionId} is blocked by session {blockingSessionId} for {waitTimeSeconds:N1} seconds ({waitType})",
                        RecommendedAction = $"Investigate blocking chain and consider optimizing queries or resolving deadlocks",
                        DatabaseName = databaseName,
                        AffectedObject = objectName,
                        SqlScript = $"-- Details for blocked session {sessionId}:\n" +
                                   $"-- Login: {loginName}\n" +
                                   $"-- Host: {hostName}\n" +
                                   $"-- Program: {programName}\n" +
                                   $"-- SQL: {currentStatement}",
                        DetectionTime = DateTime.Now
                    };

                    issues.Add(issue);
                    await _issueRepository.AddIssueAsync(issue);

                    // Send alert if enabled
                    await TriggerAlertIfEnabledAsync(connectionString, databaseName, issue, AlertType.Blocking);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for blocking issues.");
            }

            return issues;
        }

        private async Task<IEnumerable<DbIssue>> CheckForLongRunningQueriesAsync(string connectionString, int thresholdSeconds)
        {
            var issues = new List<DbIssue>();

            try
            {
                var query = $@"
                    SELECT
                        r.session_id,
                        DB_NAME(r.database_id) AS database_name,
                        DATEDIFF(SECOND, s.last_request_start_time, GETDATE()) AS duration_seconds,
                        r.cpu_time / 1000 AS cpu_time_seconds,
                        r.total_elapsed_time / 1000 AS elapsed_time_seconds,
                        r.reads,
                        r.writes,
                        r.logical_reads,
                        s.login_name,
                        s.host_name,
                        s.program_name,
                        SUBSTRING(t.text, (r.statement_start_offset / 2) + 1,
                            ((CASE r.statement_end_offset
                                WHEN -1 THEN DATALENGTH(t.text)
                                ELSE r.statement_end_offset
                            END - r.statement_start_offset) / 2) + 1) AS current_statement
                    FROM sys.dm_exec_requests r
                    JOIN sys.dm_exec_sessions s ON r.session_id = s.session_id
                    OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) t
                    WHERE r.session_id <> @@SPID  -- Exclude this query
                    AND DATEDIFF(SECOND, s.last_request_start_time, GETDATE()) > {thresholdSeconds}
                    AND r.status = 'running'
                    ORDER BY duration_seconds DESC;";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var sessionId = reader.GetInt32(0);
                    var databaseName = reader.IsDBNull(1) ? "Unknown" : reader.GetString(1);
                    var durationSeconds = reader.GetInt32(2);
                    var cpuTimeSeconds = reader.GetInt32(3);
                    var elapsedTimeSeconds = reader.GetInt32(4);
                    var reads = reader.GetInt64(5);
                    var writes = reader.GetInt64(6);
                    var logicalReads = reader.GetInt64(7);
                    var loginName = reader.IsDBNull(8) ? "Unknown" : reader.GetString(8);
                    var hostName = reader.IsDBNull(9) ? "Unknown" : reader.GetString(9);
                    var programName = reader.IsDBNull(10) ? "Unknown" : reader.GetString(10);
                    var currentStatement = reader.IsDBNull(11) ? "Unknown" : reader.GetString(11);

                    // Calculate severity based on duration
                    var severity = IssueSeverity.Low;
                    if (durationSeconds > 3600) // > 1 hour
                        severity = IssueSeverity.High;
                    else if (durationSeconds > 600) // > 10 minutes
                        severity = IssueSeverity.Medium;

                    var issue = new DbIssue
                    {
                        Type = IssueType.Performance,
                        Severity = severity,
                        Message = $"Long-running query detected: Session {sessionId} has been running for {durationSeconds} seconds",
                        RecommendedAction = $"Investigate and optimize the long-running query",
                        DatabaseName = databaseName,
                        AffectedObject = $"Session {sessionId}",
                        SqlScript = $"-- Long running query details for session {sessionId}:\n" +
                                   $"-- Login: {loginName}\n" +
                                   $"-- Host: {hostName}\n" +
                                   $"-- Program: {programName}\n" +
                                   $"-- CPU Time: {cpuTimeSeconds}s, Elapsed: {elapsedTimeSeconds}s\n" +
                                   $"-- Reads: {reads}, Writes: {writes}, Logical Reads: {logicalReads}\n" +
                                   $"-- SQL: {currentStatement}",
                        DetectionTime = DateTime.Now
                    };

                    issues.Add(issue);
                    await _issueRepository.AddIssueAsync(issue);

                    // Send alert if enabled
                    await TriggerAlertIfEnabledAsync(connectionString, databaseName, issue, AlertType.LongRunningQuery);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for long-running queries.");
            }

            return issues;
        }

        private async Task<IEnumerable<DbIssue>> CheckForLongRunningTransactionsAsync(string connectionString, string databaseName)
        {
            var issues = new List<DbIssue>();

            try
            {
                var query = $@"
                    USE [{databaseName}];
                    SELECT
                        s.session_id,
                        DB_NAME(s.database_id) AS database_name,
                        s.login_name,
                        s.host_name,
                        s.program_name,
                        DATEDIFF(SECOND, at.transaction_begin_time, GETDATE()) AS transaction_duration_seconds,
                        at.transaction_type,
                        at.transaction_state,
                        SUBSTRING(t.text, (r.statement_start_offset / 2) + 1,
                            ((CASE r.statement_end_offset
                                WHEN -1 THEN DATALENGTH(t.text)
                                ELSE r.statement_end_offset
                            END - r.statement_start_offset) / 2) + 1) AS current_statement
                    FROM sys.dm_tran_active_transactions at
                    JOIN sys.dm_tran_session_transactions st ON at.transaction_id = st.transaction_id
                    JOIN sys.dm_exec_sessions s ON st.session_id = s.session_id
                    LEFT JOIN sys.dm_exec_requests r ON s.session_id = r.session_id
                    OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) t
                    WHERE at.transaction_type <> 2 -- Exclude system transactions
                    AND s.database_id = DB_ID()
                    AND DATEDIFF(SECOND, at.transaction_begin_time, GETDATE()) > 300 -- Transactions running > 5 minutes
                    ORDER BY transaction_duration_seconds DESC;";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var sessionId = reader.GetInt32(0);
                    var dbName = reader.GetString(1);
                    var loginName = reader.IsDBNull(2) ? "Unknown" : reader.GetString(2);
                    var hostName = reader.IsDBNull(3) ? "Unknown" : reader.GetString(3);
                    var programName = reader.IsDBNull(4) ? "Unknown" : reader.GetString(4);
                    var durationSeconds = reader.GetInt32(5);
                    var transactionType = reader.GetInt32(6);
                    var transactionState = reader.GetInt32(7);
                    var currentStatement = reader.IsDBNull(8) ? "Unknown" : reader.GetString(8);

                    // Calculate severity based on duration
                    var severity = IssueSeverity.Low;
                    if (durationSeconds > 3600) // > 1 hour
                        severity = IssueSeverity.High;
                    else if (durationSeconds > 600) // > 10 minutes
                        severity = IssueSeverity.Medium;

                    var issue = new DbIssue
                    {
                        Type = IssueType.Performance,
                        Severity = severity,
                        Message = $"Long-running transaction detected: Session {sessionId} has been in transaction for {durationSeconds} seconds",
                        RecommendedAction = $"Investigate and ensure transactions are properly committed or rolled back",
                        DatabaseName = dbName,
                        AffectedObject = $"Session {sessionId}",
                        SqlScript = $"-- Long running transaction details for session {sessionId}:\n" +
                                   $"-- Login: {loginName}\n" +
                                   $"-- Host: {hostName}\n" +
                                   $"-- Program: {programName}\n" +
                                   $"-- Transaction Type: {GetTransactionTypeName(transactionType)}\n" +
                                   $"-- Transaction State: {GetTransactionStateName(transactionState)}\n" +
                                   $"-- Current SQL: {currentStatement}",
                        DetectionTime = DateTime.Now
                    };

                    issues.Add(issue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking for long-running transactions in database '{databaseName}'.");
            }

            return issues;
        }

        private string GetTransactionTypeName(int transactionType)
        {
            return transactionType switch
            {
                1 => "Read/Write Transaction",
                2 => "Read-Only Transaction",
                3 => "System Transaction",
                4 => "Distributed Transaction",
                _ => $"Unknown ({transactionType})"
            };
        }

        private string GetTransactionStateName(int transactionState)
        {
            return transactionState switch
            {
                0 => "Initialized",
                1 => "Active",
                2 => "Prepared",
                3 => "Committed",
                4 => "Aborted",
                5 => "Primed",
                _ => $"Unknown ({transactionState})"
            };
        }

        private async Task TriggerAlertIfEnabledAsync(string connectionString, string databaseName, DbIssue issue, AlertType alertType)
        {
            try
            {
                // Get alert settings
                var settings = await _settingsRepository.GetMonitoringSettingsAsync();
                var alertSetting = settings.Alerts.FirstOrDefault(a => a.Type == alertType && a.IsEnabled);

                if (alertSetting != null && issue.Severity >= alertSetting.MinimumSeverity)
                {
                    await _alertService.TriggerAlertAsync(connectionString, databaseName, issue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error triggering alert for issue: {issue.Message}");
            }
        }
    }
}
