using Microsoft.Data.SqlClient;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;
using System.Data;

namespace Sql_Server_Monitoring.Application.Services
{
    public class IdentityColumnService : IIdentityColumnService
    {
        private readonly ILogger<IdentityColumnService> _logger;
        private readonly IIssueRepository _issueRepository;
        private readonly IAlertService _alertService;

        public IdentityColumnService(
            ILogger<IdentityColumnService> logger,
            IIssueRepository issueRepository,
            IAlertService alertService)
        {
            _logger = logger;
            _issueRepository = issueRepository;
            _alertService = alertService;
        }

        public async Task<IEnumerable<IdentityColumnInfo>> GetIdentityColumnsAsync(string connectionString, string databaseName)
        {
            var results = new List<IdentityColumnInfo>();

            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                string sql = @"
                    SELECT 
                        DB_NAME() AS DatabaseName,
                        SCHEMA_NAME(t.schema_id) AS SchemaName,
                        t.name AS TableName,
                        c.name AS ColumnName,
                        ty.name AS DataType,
                        IDENT_CURRENT(SCHEMA_NAME(t.schema_id) + '.' + t.name) AS CurrentValue,
                        CASE 
                            WHEN ic.last_value IS NULL THEN IDENT_SEED(SCHEMA_NAME(t.schema_id) + '.' + t.name)
                            ELSE ic.last_value 
                        END AS LastValue,
                        IDENT_INCR(SCHEMA_NAME(t.schema_id) + '.' + t.name) AS Increment,
                        IDENT_SEED(SCHEMA_NAME(t.schema_id) + '.' + t.name) AS SeedValue,
                        CASE 
                            WHEN ty.name IN ('tinyint') THEN 
                                (IDENT_CURRENT(SCHEMA_NAME(t.schema_id) + '.' + t.name) * 100.0) / 255.0
                            WHEN ty.name IN ('smallint') THEN 
                                (IDENT_CURRENT(SCHEMA_NAME(t.schema_id) + '.' + t.name) * 100.0) / 32767.0
                            WHEN ty.name IN ('int') THEN 
                                (IDENT_CURRENT(SCHEMA_NAME(t.schema_id) + '.' + t.name) * 100.0) / 2147483647.0
                            WHEN ty.name IN ('bigint') THEN 
                                (IDENT_CURRENT(SCHEMA_NAME(t.schema_id) + '.' + t.name) * 100.0) / 9223372036854775807.0
                            ELSE 0
                        END AS PercentUsed
                    FROM 
                        sys.tables t
                    INNER JOIN 
                        sys.columns c ON t.object_id = c.object_id
                    INNER JOIN 
                        sys.types ty ON c.user_type_id = ty.user_type_id
                    LEFT JOIN 
                        sys.identity_columns ic ON c.object_id = ic.object_id AND c.column_id = ic.column_id
                    WHERE 
                        c.is_identity = 1
                    ORDER BY 
                        SchemaName, TableName;";

                await using var command = new SqlCommand(sql, connection);
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var identityColumn = new IdentityColumnInfo
                    {
                        DatabaseName = reader["DatabaseName"].ToString(),
                        SchemaName = reader["SchemaName"].ToString(),
                        TableName = reader["TableName"].ToString(),
                        ColumnName = reader["ColumnName"].ToString(),
                        DataType = reader["DataType"].ToString(),
                        CurrentValue = Convert.ToInt64(reader["CurrentValue"]),
                        LastValue = Convert.ToInt64(reader["LastValue"]),
                        Increment = Convert.ToInt64(reader["Increment"]),
                        SeedValue = Convert.ToInt64(reader["SeedValue"]),
                        PercentUsed = Convert.ToSingle(reader["PercentUsed"]),
                        LastChecked = DateTime.Now
                    };

                    results.Add(identityColumn);
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving identity columns for database '{databaseName}'");
                throw;
            }
        }

        public async Task<IEnumerable<DbIssue>> AnalyzeIdentityColumnsAsync(string connectionString, string databaseName)
        {
            var issues = new List<DbIssue>();

            try
            {
                var identityColumns = await GetIdentityColumnsAsync(connectionString, databaseName);

                // Check for identity columns nearing exhaustion (> 80% used)
                var nearingExhaustionColumns = identityColumns.Where(ic => ic.PercentUsed > 80);
                
                foreach (var column in nearingExhaustionColumns)
                {
                    string reseedScript = $@"
                        -- Take the table offline first or schedule during maintenance window
                        DECLARE @CurrentMax BIGINT;
                        SELECT @CurrentMax = MAX([{column.ColumnName}]) FROM [{column.SchemaName}].[{column.TableName}];
                        DBCC CHECKIDENT ('[{column.SchemaName}].[{column.TableName}]', RESEED, @CurrentMax);";

                    var issue = new DbIssue
                    {
                        Type = IssueType.IdentityColumn,
                        Severity = column.PercentUsed > 95 ? IssueSeverity.Critical : IssueSeverity.High,
                        Message = $"Identity column '{column.SchemaName}.{column.TableName}.{column.ColumnName}' is at {column.PercentUsed:N1}% capacity",
                        RecommendedAction = "Reseed the identity column or convert to a larger data type",
                        DatabaseName = databaseName,
                        AffectedObject = $"{column.SchemaName}.{column.TableName}.{column.ColumnName}",
                        DetectionTime = DateTime.Now,
                        SqlScript = reseedScript
                    };

                    issues.Add(issue);
                    await _issueRepository.AddIssueAsync(issue);

                    // Create alert if critically near exhaustion (> 95%)
                    if (column.PercentUsed > 95)
                    {
                        await _alertService.CreateAlertAsync(
                            connectionString,
                            databaseName,
                            AlertType.IdentityColumnExhaustion,
                            issue.Message,
                            issue.RecommendedAction,
                            IssueSeverity.Critical);
                    }
                }

                // Check for identity columns with a very small data type (tinyint)
                var tinyintIdentityColumns = identityColumns.Where(ic => 
                    ic.DataType.Equals("tinyint", StringComparison.OrdinalIgnoreCase) && 
                    ic.PercentUsed > 50);

                foreach (var column in tinyintIdentityColumns)
                {
                    string alterScript = $@"
                        -- Take the table offline first or schedule during maintenance window
                        ALTER TABLE [{column.SchemaName}].[{column.TableName}]
                        ALTER COLUMN [{column.ColumnName}] SMALLINT IDENTITY({column.CurrentValue}, {column.Increment});";

                    var issue = new DbIssue
                    {
                        Type = IssueType.IdentityColumn,
                        Severity = IssueSeverity.Medium,
                        Message = $"Identity column '{column.SchemaName}.{column.TableName}.{column.ColumnName}' uses tinyint data type which may not be sufficient",
                        RecommendedAction = "Consider converting to a larger data type (smallint, int, or bigint)",
                        DatabaseName = databaseName,
                        AffectedObject = $"{column.SchemaName}.{column.TableName}.{column.ColumnName}",
                        DetectionTime = DateTime.Now,
                        SqlScript = alterScript
                    };

                    issues.Add(issue);
                    await _issueRepository.AddIssueAsync(issue);
                }

                // Check for identity columns with high growth rate
                // This would require historical data to determine the growth rate
                // For now, we'll use a simple heuristic based on current value and table creation date

                return issues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing identity columns for database '{databaseName}'");
                throw;
            }
        }

        public async Task<bool> ReseedIdentityColumnAsync(
            string connectionString,
            string databaseName, 
            string schemaName, 
            string tableName, 
            string columnName, 
            long newSeedValue)
        {
            try
            {
                await using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                string reseedSql = $"DBCC CHECKIDENT ('[{schemaName}].[{tableName}]', RESEED, @NewSeedValue);";
                
                await using var command = new SqlCommand(reseedSql, connection);
                command.Parameters.AddWithValue("@NewSeedValue", newSeedValue);
                
                await command.ExecuteNonQueryAsync();
                
                _logger.LogInformation($"Successfully reseeded identity column '{schemaName}.{tableName}.{columnName}' to {newSeedValue}");
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reseeding identity column '{schemaName}.{tableName}.{columnName}'");
                return false;
            }
        }
    }
} 