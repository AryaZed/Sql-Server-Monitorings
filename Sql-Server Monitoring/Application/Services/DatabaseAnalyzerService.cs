using Microsoft.Data.SqlClient;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;
using Index = Sql_Server_Monitoring.Domain.Models.Index;

namespace Sql_Server_Monitoring.Application.Services
{
    public class DatabaseAnalyzerService : IDatabaseAnalyzerService
    {
        private readonly IDatabaseRepository _databaseRepository;
        private readonly IIssueRepository _issueRepository;
        private readonly ILogger<DatabaseAnalyzerService> _logger;

        public DatabaseAnalyzerService(
            IDatabaseRepository databaseRepository,
            IIssueRepository issueRepository,
            ILogger<DatabaseAnalyzerService> logger)
        {
            _databaseRepository = databaseRepository;
            _issueRepository = issueRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<DbIssue>> AnalyzeDatabaseAsync(string connectionString, string databaseName)
        {
            _logger.LogInformation($"Starting analysis of database '{databaseName}'");

            var allIssues = new List<DbIssue>();

            try
            {
                // Run all analysis methods
                var schemaIssues = await AnalyzeSchemaStructureAsync(connectionString, databaseName);
                var performanceIssues = await AnalyzePerformanceAsync(connectionString, databaseName);
                var indexIssues = await AnalyzeIndexesAsync(connectionString, databaseName);
                var configIssues = await AnalyzeConfigurationAsync(connectionString, databaseName);
                var securityIssues = await AnalyzeSecurityAsync(connectionString, databaseName);
                var backupIssues = await AnalyzeBackupStrategyAsync(connectionString, databaseName);
                var capacityIssues = await AnalyzeCapacityAsync(connectionString, databaseName);

                // Combine issues
                allIssues.AddRange(schemaIssues);
                allIssues.AddRange(performanceIssues);
                allIssues.AddRange(indexIssues);
                allIssues.AddRange(configIssues);
                allIssues.AddRange(securityIssues);
                allIssues.AddRange(backupIssues);
                allIssues.AddRange(capacityIssues);

                // Store issues in repository
                foreach (var issue in allIssues)
                {
                    await _issueRepository.AddIssueAsync(issue);
                }

                _logger.LogInformation($"Analysis completed for database '{databaseName}'. Found {allIssues.Count} issues.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing database '{databaseName}'");
            }

            return allIssues;
        }

        public async Task<IEnumerable<DbIssue>> AnalyzeSchemaStructureAsync(string connectionString, string databaseName)
        {
            var issues = new List<DbIssue>();

            try
            {
                _logger.LogInformation($"Analyzing schema structure for database '{databaseName}'");

                // Get all tables
                var tables = await _databaseRepository.GetTablesAsync(connectionString, databaseName);

                foreach (var table in tables)
                {
                    // Get detailed table info
                    var tableDetails = await _databaseRepository.GetTableDetailsAsync(connectionString, databaseName, table.Schema, table.Name);

                    // Check for tables without primary keys
                    if (!tableDetails.HasPrimaryKey)
                    {
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Schema,
                            Severity = IssueSeverity.Medium,
                            Message = $"Table '{table.Schema}.{table.Name}' does not have a primary key",
                            RecommendedAction = $"Add a primary key to '{table.Schema}.{table.Name}' to improve query performance and data integrity",
                            DatabaseName = databaseName,
                            AffectedObject = $"{table.Schema}.{table.Name}",
                            SqlScript = GeneratePrimaryKeyScript(tableDetails)
                        });
                    }

                    // Check for tables with excessive columns (more than 30)
                    if (tableDetails.Columns.Count > 30)
                    {
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Schema,
                            Severity = IssueSeverity.Medium,
                            Message = $"Table '{table.Schema}.{table.Name}' has {tableDetails.Columns.Count} columns, which is excessive",
                            RecommendedAction = $"Consider normalizing table '{table.Schema}.{table.Name}' by splitting it into multiple related tables",
                            DatabaseName = databaseName,
                            AffectedObject = $"{table.Schema}.{table.Name}"
                        });
                    }

                    // Check for columns with inappropriate data types
                    foreach (var column in tableDetails.Columns)
                    {
                        // Check for inappropriate use of nvarchar(max)
                        if (column.DataType.Equals("nvarchar", StringComparison.OrdinalIgnoreCase) && column.MaxLength == -1)
                        {
                            issues.Add(new DbIssue
                            {
                                Type = IssueType.Schema,
                                Severity = IssueSeverity.Low,
                                Message = $"Column '{column.Name}' in table '{table.Schema}.{table.Name}' uses nvarchar(MAX) which may lead to performance issues if used frequently in queries",
                                RecommendedAction = $"Consider using a fixed-length nvarchar type if the maximum length is known",
                                DatabaseName = databaseName,
                                AffectedObject = $"{table.Schema}.{table.Name}.{column.Name}"
                            });
                        }

                        // Check for use of TEXT, NTEXT, or IMAGE data types (deprecated)
                        if (column.DataType.Equals("text", StringComparison.OrdinalIgnoreCase) ||
                            column.DataType.Equals("ntext", StringComparison.OrdinalIgnoreCase) ||
                            column.DataType.Equals("image", StringComparison.OrdinalIgnoreCase))
                        {
                            issues.Add(new DbIssue
                            {
                                Type = IssueType.Schema,
                                Severity = IssueSeverity.Medium,
                                Message = $"Column '{column.Name}' in table '{table.Schema}.{table.Name}' uses deprecated data type '{column.DataType}'",
                                RecommendedAction = $"Replace '{column.DataType}' with a modern data type such as nvarchar(max), varchar(max), or varbinary(max)",
                                DatabaseName = databaseName,
                                AffectedObject = $"{table.Schema}.{table.Name}.{column.Name}",
                                SqlScript = $"ALTER TABLE [{table.Schema}].[{table.Name}] ALTER COLUMN [{column.Name}] {GetModernDataType(column.DataType)};"
                            });
                        }
                    }

                    // Check for foreign keys without indexes
                    var foreignKeyColumns = new HashSet<string>(tableDetails.Columns
                        .Where(c => c.IsForeignKey)
                        .Select(c => c.Name));

                    foreach (var fk in tableDetails.ForeignKeys)
                    {
                        foreach (var fkColumn in fk.Columns)
                        {
                            var isIndexed = tableDetails.Indexes.Any(idx => idx.Columns.Contains(fkColumn.Column));

                            if (!isIndexed)
                            {
                                issues.Add(new DbIssue
                                {
                                    Type = IssueType.Schema,
                                    Severity = IssueSeverity.Medium,
                                    Message = $"Foreign key column '{fkColumn.Column}' in table '{table.Schema}.{table.Name}' is not indexed",
                                    RecommendedAction = $"Add an index on foreign key column '{fkColumn.Column}' to improve query performance",
                                    DatabaseName = databaseName,
                                    AffectedObject = $"{table.Schema}.{table.Name}.{fkColumn.Column}",
                                    SqlScript = $"CREATE INDEX [IX_{table.Name}_{fkColumn.Column}] ON [{table.Schema}].[{table.Name}] ([{fkColumn.Column}]);"
                                });
                            }
                        }
                    }

                    // Check for disabled foreign keys
                    foreach (var fk in tableDetails.ForeignKeys.Where(fk => fk.IsDisabled))
                    {
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Schema,
                            Severity = IssueSeverity.High,
                            Message = $"Foreign key '{fk.Name}' in table '{table.Schema}.{table.Name}' is disabled",
                            RecommendedAction = $"Enable the foreign key constraint to maintain referential integrity",
                            DatabaseName = databaseName,
                            AffectedObject = $"{table.Schema}.{table.Name}.{fk.Name}",
                            SqlScript = $"ALTER TABLE [{table.Schema}].[{table.Name}] CHECK CONSTRAINT [{fk.Name}];"
                        });
                    }
                }

                _logger.LogInformation($"Schema analysis completed for database '{databaseName}'. Found {issues.Count} issues.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing schema structure for database '{databaseName}'");
            }

            return issues;
        }

        public async Task<IEnumerable<DbIssue>> AnalyzePerformanceAsync(string connectionString, string databaseName)
        {
            var issues = new List<DbIssue>();

            try
            {
                _logger.LogInformation($"Analyzing performance for database '{databaseName}'");

                // Check for tables without clustered indexes
                var tables = await _databaseRepository.GetTablesAsync(connectionString, databaseName);
                foreach (var table in tables)
                {
                    if (!table.HasClusteredIndex && table.RowCount > 1000)
                    {
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Performance,
                            Severity = IssueSeverity.Medium,
                            Message = $"Table '{table.Schema}.{table.Name}' has {table.RowCount:N0} rows but no clustered index",
                            RecommendedAction = $"Add a clustered index to table '{table.Schema}.{table.Name}' to improve query performance",
                            DatabaseName = databaseName,
                            AffectedObject = $"{table.Schema}.{table.Name}"
                        });
                    }

                    // Check for large tables (> 10M rows) with no partitioning
                    if (table.RowCount > 10_000_000)
                    {
                        // Check if the table is partitioned
                        var isPartitioned = await IsTablePartitionedAsync(connectionString, databaseName, table.Schema, table.Name);

                        if (!isPartitioned)
                        {
                            issues.Add(new DbIssue
                            {
                                Type = IssueType.Performance,
                                Severity = IssueSeverity.Medium,
                                Message = $"Table '{table.Schema}.{table.Name}' has {table.RowCount:N0} rows but is not partitioned",
                                RecommendedAction = $"Consider implementing table partitioning for large table '{table.Schema}.{table.Name}' to improve manageability and query performance",
                                DatabaseName = databaseName,
                                AffectedObject = $"{table.Schema}.{table.Name}"
                            });
                        }
                    }
                }

                // Check for slow queries using Query Store (if available)
                try
                {
                    var slowQueries = await GetSlowQueriesAsync(connectionString, databaseName);

                    foreach (var query in slowQueries)
                    {
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Performance,
                            Severity = IssueSeverity.High,
                            Message = $"Slow query detected with average duration of {query.AvgDurationMs:N2}ms and executed {query.ExecutionCount:N0} times",
                            RecommendedAction = $"Review and optimize query execution plan: {query.QueryText}",
                            DatabaseName = databaseName,
                            AffectedObject = $"Query ID: {query.QueryId}"
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"Could not analyze Query Store for database '{databaseName}'. Query Store might not be enabled.");
                }

                _logger.LogInformation($"Performance analysis completed for database '{databaseName}'. Found {issues.Count} issues.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing performance for database '{databaseName}'");
            }

            return issues;
        }

        public async Task<IEnumerable<DbIssue>> AnalyzeIndexesAsync(string connectionString, string databaseName)
        {
            var issues = new List<DbIssue>();

            try
            {
                _logger.LogInformation($"Analyzing indexes for database '{databaseName}'");

                // Get all tables
                var tables = await _databaseRepository.GetTablesAsync(connectionString, databaseName);

                foreach (var table in tables)
                {
                    // Skip very small tables
                    if (table.RowCount < 100)
                    {
                        continue;
                    }

                    var tableDetails = await _databaseRepository.GetTableDetailsAsync(connectionString, databaseName, table.Schema, table.Name);

                    // Check for unused indexes
                    foreach (var index in tableDetails.Indexes.Where(i => !i.IsPrimaryKey && !i.IsClustered))
                    {
                        if (index.UsageStats != null)
                        {
                            // If index has never been used or hasn't been used in the last 30 days, and has updates
                            if ((index.UsageStats.UserSeeks == 0 && index.UsageStats.UserScans == 0 && index.UsageStats.UserLookups == 0) ||
                                (index.UsageStats.LastUsed < DateTime.Now.AddDays(-30) && index.UsageStats.UserUpdates > 0))
                            {
                                issues.Add(new DbIssue
                                {
                                    Type = IssueType.Index,
                                    Severity = IssueSeverity.Medium,
                                    Message = $"Unused index '{index.Name}' on table '{table.Schema}.{table.Name}'",
                                    RecommendedAction = $"Consider dropping unused index '{index.Name}' to improve write performance and reduce storage",
                                    DatabaseName = databaseName,
                                    AffectedObject = $"{table.Schema}.{table.Name}.{index.Name}",
                                    SqlScript = $"DROP INDEX [{index.Name}] ON [{table.Schema}].[{table.Name}];"
                                });
                            }
                        }
                    }

                    // Check for duplicate/overlapping indexes
                    var potentialDuplicates = FindPotentialDuplicateIndexes(tableDetails.Indexes);

                    foreach (var (index1, index2) in potentialDuplicates)
                    {
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Index,
                            Severity = IssueSeverity.Medium,
                            Message = $"Potential duplicate indexes: '{index1.Name}' and '{index2.Name}' on table '{table.Schema}.{table.Name}'",
                            RecommendedAction = $"Consider consolidating these indexes to reduce overhead",
                            DatabaseName = databaseName,
                            AffectedObject = $"{table.Schema}.{table.Name}",
                            SqlScript = GetIndexConsolidationScript(table.Schema, table.Name, index1, index2)
                        });
                    }

                    // Check for highly fragmented indexes
                    foreach (var index in tableDetails.Indexes.Where(i => i.FragmentationPercent > 30))
                    {
                        var action = index.FragmentationPercent > 70 ? "REBUILD" : "REORGANIZE";

                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Index,
                            Severity = index.FragmentationPercent > 70 ? IssueSeverity.High : IssueSeverity.Medium,
                            Message = $"Index '{index.Name}' on table '{table.Schema}.{table.Name}' is {index.FragmentationPercent:N2}% fragmented",
                            RecommendedAction = $"Perform index maintenance ({action}) to reduce fragmentation",
                            DatabaseName = databaseName,
                            AffectedObject = $"{table.Schema}.{table.Name}.{index.Name}",
                            SqlScript = $"ALTER INDEX [{index.Name}] ON [{table.Schema}].[{table.Name}] {action};"
                        });
                    }
                }

                _logger.LogInformation($"Index analysis completed for database '{databaseName}'. Found {issues.Count} issues.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing indexes for database '{databaseName}'");
            }

            return issues;
        }

        public async Task<IEnumerable<DbIssue>> AnalyzeConfigurationAsync(string connectionString, string databaseName)
        {
            var issues = new List<DbIssue>();

            try
            {
                _logger.LogInformation($"Analyzing configuration for database '{databaseName}'");

                // Get database details
                var dbDetails = await _databaseRepository.GetDatabaseDetailsAsync(connectionString, databaseName);

                // Check recovery model
                if (dbDetails.RecoveryModel == RecoveryModel.Simple && dbDetails.SizeInMB > 10_000)
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Configuration,
                        Severity = IssueSeverity.Medium,
                        Message = $"Large database '{databaseName}' ({dbDetails.SizeInMB:N0} MB) is using Simple recovery model",
                        RecommendedAction = "Consider using Full recovery model for large databases to enable point-in-time recovery",
                        DatabaseName = databaseName,
                        AffectedObject = databaseName,
                        SqlScript = $"ALTER DATABASE [{databaseName}] SET RECOVERY FULL;"
                    });
                }

                // Check database compatibility level
                if (dbDetails.CompatibilityLevel < CompatibilityLevel.Sql2016)
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Configuration,
                        Severity = IssueSeverity.Medium,
                        Message = $"Database '{databaseName}' is using outdated compatibility level {dbDetails.CompatibilityLevel}",
                        RecommendedAction = "Consider upgrading the compatibility level to benefit from latest query optimizer improvements",
                        DatabaseName = databaseName,
                        AffectedObject = databaseName,
                        SqlScript = $"ALTER DATABASE [{databaseName}] SET COMPATIBILITY_LEVEL = 150;" // SQL Server 2019
                    });
                }

                // Check for auto-growth settings
                foreach (var file in dbDetails.Files)
                {
                    // Check if using percentage growth
                    if (file.GrowthType == FileGrowthType.Percent)
                    {
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Configuration,
                            Severity = IssueSeverity.Medium,
                            Message = $"Database file '{file.Name}' uses percentage auto-growth ({file.GrowthValue}%)",
                            RecommendedAction = "Use fixed-size auto-growth instead of percentage to avoid large, unpredictable growth steps",
                            DatabaseName = databaseName,
                            AffectedObject = file.Name,
                            SqlScript = $"ALTER DATABASE [{databaseName}] MODIFY FILE (NAME = N'{file.Name}', FILEGROWTH = 1024MB);"
                        });
                    }

                    // Check if using small auto-growth steps for large files
                    if (file.GrowthType == FileGrowthType.MB && file.SizeInMB > 10_000 && file.GrowthValue < 1024)
                    {
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Configuration,
                            Severity = IssueSeverity.Low,
                            Message = $"Large database file '{file.Name}' ({file.SizeInMB:N0} MB) has small auto-growth setting ({file.GrowthValue} MB)",
                            RecommendedAction = "Increase auto-growth setting for large files to reduce frequency of growth events",
                            DatabaseName = databaseName,
                            AffectedObject = file.Name,
                            SqlScript = $"ALTER DATABASE [{databaseName}] MODIFY FILE (NAME = N'{file.Name}', FILEGROWTH = 1024MB);"
                        });
                    }

                    // Check if files are almost at maximum size
                    if (file.MaxSizeInMB > 0 && file.SizeInMB > file.MaxSizeInMB * 0.9)
                    {
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Configuration,
                            Severity = IssueSeverity.High,
                            Message = $"Database file '{file.Name}' is nearing its maximum size (Current: {file.SizeInMB:N0} MB, Max: {file.MaxSizeInMB:N0} MB)",
                            RecommendedAction = "Increase the maximum file size or create additional filegroup",
                            DatabaseName = databaseName,
                            AffectedObject = file.Name,
                            SqlScript = $"ALTER DATABASE [{databaseName}] MODIFY FILE (NAME = N'{file.Name}', MAXSIZE = UNLIMITED);"
                        });
                    }
                }

                // Check if Query Store is enabled
                var isQueryStoreEnabled = await IsQueryStoreEnabledAsync(connectionString, databaseName);
                if (!isQueryStoreEnabled)
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Configuration,
                        Severity = IssueSeverity.Low,
                        Message = $"Query Store is not enabled for database '{databaseName}'",
                        RecommendedAction = "Enable Query Store to track query performance metrics over time",
                        DatabaseName = databaseName,
                        AffectedObject = databaseName,
                        SqlScript = @"
ALTER DATABASE [" + databaseName + @"] SET QUERY_STORE = ON;
ALTER DATABASE [" + databaseName + @"] SET QUERY_STORE (
    OPERATION_MODE = READ_WRITE,
    CLEANUP_POLICY = (STALE_QUERY_THRESHOLD_DAYS = 30),
    DATA_FLUSH_INTERVAL_SECONDS = 900,
    MAX_STORAGE_SIZE_MB = 1000,
    INTERVAL_LENGTH_MINUTES = 60
);"
                    });
                }

                _logger.LogInformation($"Configuration analysis completed for database '{databaseName}'. Found {issues.Count} issues.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing configuration for database '{databaseName}'");
            }

            return issues;
        }

        public async Task<IEnumerable<DbIssue>> AnalyzeSecurityAsync(string connectionString, string databaseName)
        {
            var issues = new List<DbIssue>();

            try
            {
                _logger.LogInformation($"Analyzing security for database '{databaseName}'");

                // Check for guest user permissions
                var hasGuestPermissions = await HasGuestPermissionsAsync(connectionString, databaseName);
                if (hasGuestPermissions)
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Security,
                        Severity = IssueSeverity.High,
                        Message = $"Guest user has permissions in database '{databaseName}'",
                        RecommendedAction = "Revoke permissions from the guest user to reduce security risk",
                        DatabaseName = databaseName,
                        AffectedObject = "guest",
                        SqlScript = $"REVOKE CONNECT FROM guest;"
                    });
                }

                // Check for users with sysadmin or db_owner roles
                var powerUsers = await GetPowerUsersAsync(connectionString, databaseName);
                foreach (var user in powerUsers)
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Security,
                        Severity = IssueSeverity.Medium,
                        Message = $"User '{user}' has high-privilege role in database '{databaseName}'",
                        RecommendedAction = "Review if this user requires elevated permissions, and consider using a more restricted role",
                        DatabaseName = databaseName,
                        AffectedObject = user
                    });
                }

                // Check for transparent data encryption (TDE)
                var dbDetails = await _databaseRepository.GetDatabaseDetailsAsync(connectionString, databaseName);
                if (!dbDetails.IsEncrypted && await HasSensitiveDataAsync(connectionString, databaseName))
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Security,
                        Severity = IssueSeverity.High,
                        Message = $"Database '{databaseName}' potentially contains sensitive data but is not encrypted with TDE",
                        RecommendedAction = "Enable Transparent Data Encryption (TDE) to protect data at rest",
                        DatabaseName = databaseName,
                        AffectedObject = databaseName,
                        SqlScript = $@"
-- First, create a master key and certificate (if not exists)
USE [master];
IF NOT EXISTS (SELECT 1 FROM sys.symmetric_keys WHERE name = '##MS_DatabaseMasterKey##')
BEGIN
    CREATE MASTER KEY ENCRYPTION BY PASSWORD = '<strong_password_here>';
END

IF NOT EXISTS (SELECT 1 FROM sys.certificates WHERE name = 'TDECertificate')
BEGIN
    CREATE CERTIFICATE TDECertificate WITH SUBJECT = 'TDE Certificate';
END

-- Enable TDE for the database
USE [master];
CREATE DATABASE ENCRYPTION KEY WITH ALGORITHM = AES_256 ENCRYPTION BY SERVER CERTIFICATE TDECertificate;
ALTER DATABASE [{databaseName}] SET ENCRYPTION ON;"
                    });
                }

                // Check for SQL injection vulnerable stored procedures
                var potentialSqlInjectionProcs = await FindPotentialSqlInjectionProcsAsync(connectionString, databaseName);
                foreach (var proc in potentialSqlInjectionProcs)
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Security,
                        Severity = IssueSeverity.Critical,
                        Message = $"Potential SQL injection vulnerability in stored procedure '{proc}'",
                        RecommendedAction = "Review and parameterize dynamic SQL in this stored procedure",
                        DatabaseName = databaseName,
                        AffectedObject = proc
                    });
                }

                // Check for sensitive columns without encryption
                var sensitiveColumns = await FindSensitiveColumnsAsync(connectionString, databaseName);
                foreach (var column in sensitiveColumns)
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Security,
                        Severity = IssueSeverity.High,
                        Message = $"Potential sensitive data in column '{column.TableName}.{column.ColumnName}' without encryption",
                        RecommendedAction = "Consider using Always Encrypted, column-level encryption or data masking for sensitive data",
                        DatabaseName = databaseName,
                        AffectedObject = $"{column.TableName}.{column.ColumnName}"
                    });
                }

                _logger.LogInformation($"Security analysis completed for database '{databaseName}'. Found {issues.Count} issues.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing security for database '{databaseName}'");
            }

            return issues;
        }

        public async Task<IEnumerable<DbIssue>> AnalyzeBackupStrategyAsync(string connectionString, string databaseName)
        {
            var issues = new List<DbIssue>();

            try
            {
                _logger.LogInformation($"Analyzing backup strategy for database '{databaseName}'");

                // Get database details
                var dbDetails = await _databaseRepository.GetDatabaseDetailsAsync(connectionString, databaseName);

                // Check for recent full backups
                if (dbDetails.LastBackupDate == DateTime.MinValue || dbDetails.LastBackupDate < DateTime.Now.AddDays(-7))
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Backup,
                        Severity = IssueSeverity.Critical,
                        Message = dbDetails.LastBackupDate == DateTime.MinValue
                            ? $"No backup found for database '{databaseName}'"
                            : $"Last backup for database '{databaseName}' was on {dbDetails.LastBackupDate:yyyy-MM-dd} which is over 7 days old",
                        RecommendedAction = "Implement regular database backups",
                        DatabaseName = databaseName,
                        AffectedObject = databaseName,
                        SqlScript = $"BACKUP DATABASE [{databaseName}] TO DISK = N'C:\\Backups\\{databaseName}_{DateTime.Now:yyyyMMdd}.bak' WITH COMPRESSION, CHECKSUM, STATS = 10;"
                    });
                }

                // Check for log backups if using full recovery model
                if (dbDetails.RecoveryModel == RecoveryModel.Full)
                {
                    var lastLogBackup = await GetLastLogBackupDateAsync(connectionString, databaseName);

                    if (lastLogBackup == DateTime.MinValue || lastLogBackup < DateTime.Now.AddHours(-24))
                    {
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Backup,
                            Severity = IssueSeverity.High,
                            Message = lastLogBackup == DateTime.MinValue
                                ? $"No transaction log backup found for database '{databaseName}' which is in Full recovery model"
                                : $"Last log backup for database '{databaseName}' was on {lastLogBackup:yyyy-MM-dd HH:mm} which is over 24 hours old",
                            RecommendedAction = "Implement regular transaction log backups for databases in Full recovery model",
                            DatabaseName = databaseName,
                            AffectedObject = databaseName,
                            SqlScript = $"BACKUP LOG [{databaseName}] TO DISK = N'C:\\Backups\\{databaseName}_{DateTime.Now:yyyyMMdd_HHmmss}.trn' WITH COMPRESSION, CHECKSUM, STATS = 10;"
                        });
                    }
                }

                // Check if backup validation is performed
                var isBackupValidated = await IsBackupValidatedAsync(connectionString, databaseName);
                if (!isBackupValidated)
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Backup,
                        Severity = IssueSeverity.Medium,
                        Message = $"Backups for database '{databaseName}' are not regularly validated",
                        RecommendedAction = "Implement regular backup validation with RESTORE VERIFYONLY",
                        DatabaseName = databaseName,
                        AffectedObject = databaseName,
                        SqlScript = $@"-- To validate the most recent backup:
DECLARE @BackupFile nvarchar(255)
SELECT TOP 1 @BackupFile = physical_device_name
FROM msdb.dbo.backupset bs
JOIN msdb.dbo.backupmediafamily bmf ON bs.media_set_id = bmf.media_set_id
WHERE bs.database_name = '{databaseName}'
ORDER BY bs.backup_finish_date DESC

IF @BackupFile IS NOT NULL
    RESTORE VERIFYONLY FROM DISK = @BackupFile;"
                    });
                }

                _logger.LogInformation($"Backup strategy analysis completed for database '{databaseName}'. Found {issues.Count} issues.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing backup strategy for database '{databaseName}'");
            }

            return issues;
        }

        public async Task<IEnumerable<DbIssue>> AnalyzeCapacityAsync(string connectionString, string databaseName)
        {
            var issues = new List<DbIssue>();

            try
            {
                _logger.LogInformation($"Analyzing capacity for database '{databaseName}'");

                // Get database details
                var dbDetails = await _databaseRepository.GetDatabaseDetailsAsync(connectionString, databaseName);

                // Check for files with high percent used space
                foreach (var file in dbDetails.Files.Where(f => f.PercentUsed > 90))
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Capacity,
                        Severity = IssueSeverity.High,
                        Message = $"Database file '{file.Name}' is {file.PercentUsed}% full",
                        RecommendedAction = "Increase the file size or add a new file to the filegroup",
                        DatabaseName = databaseName,
                        AffectedObject = file.Name,
                        SqlScript = $"ALTER DATABASE [{databaseName}] MODIFY FILE (NAME = N'{file.Name}', SIZE = {file.SizeInMB + 1024}MB);"
                    });
                }

                // Check for log file size (if too large compared to data file)
                var logFiles = dbDetails.Files.Where(f => f.IsLogFile).ToList();
                var dataFiles = dbDetails.Files.Where(f => !f.IsLogFile).ToList();

                if (logFiles.Any() && dataFiles.Any())
                {
                    long totalLogSize = logFiles.Sum(f => f.SizeInMB);
                    long totalDataSize = dataFiles.Sum(f => f.SizeInMB);

                    if (totalLogSize > totalDataSize * 0.5) // Log file > 50% of data file
                    {
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Capacity,
                            Severity = IssueSeverity.Medium,
                            Message = $"Transaction log file(s) for database '{databaseName}' are unusually large ({totalLogSize:N0} MB, {(double)totalLogSize / totalDataSize:P0} of data file size)",
                            RecommendedAction = "Check for long-running transactions, implement regular log backups, or shrink log file after addressing root cause",
                            DatabaseName = databaseName,
                            AffectedObject = string.Join(", ", logFiles.Select(f => f.Name))
                        });
                    }
                }

                // Check for disk space on volumes hosting database files
                var diskSpaceIssues = await CheckDiskSpaceAsync(connectionString, databaseName);
                foreach (var diskIssue in diskSpaceIssues)
                {
                    issues.Add(diskIssue);
                }

                // Check for database growth rate
                var growthRate = await GetDatabaseGrowthRateAsync(connectionString, databaseName);
                if (growthRate > 50) // More than 50% growth per month
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Capacity,
                        Severity = IssueSeverity.Medium,
                        Message = $"Database '{databaseName}' has a high growth rate (approximately {growthRate:N0}% per month)",
                        RecommendedAction = "Plan for additional capacity and investigate if the growth is expected",
                        DatabaseName = databaseName,
                        AffectedObject = databaseName
                    });
                }

                _logger.LogInformation($"Capacity analysis completed for database '{databaseName}'. Found {issues.Count} issues.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing capacity for database '{databaseName}'");
            }

            return issues;
        }

        #region Helper Methods
        private string GeneratePrimaryKeyScript(Table table)
        {
            // Try to find a suitable column for primary key
            Column idColumn = null;

            // First, look for an identity column
            idColumn = table.Columns.FirstOrDefault(c => c.IsIdentity);

            // If no identity column, look for columns named like ID
            if (idColumn == null)
            {
                idColumn = table.Columns.FirstOrDefault(c =>
                    (c.Name.Equals("ID", StringComparison.OrdinalIgnoreCase) ||
                     c.Name.EndsWith("ID", StringComparison.OrdinalIgnoreCase) ||
                     c.Name.Equals("Key", StringComparison.OrdinalIgnoreCase)) &&
                    !c.IsNullable);
            }

            if (idColumn != null)
            {
                return $"ALTER TABLE [{table.Schema}].[{table.Name}] ADD CONSTRAINT [PK_{table.Name}] PRIMARY KEY CLUSTERED ([{idColumn.Name}]);";
            }
            else
            {
                // Need to add an ID column
                return $@"
-- Add a new identity column to serve as the primary key
ALTER TABLE [{table.Schema}].[{table.Name}] ADD [ID] INT IDENTITY(1,1) NOT NULL;
-- Add a primary key constraint
ALTER TABLE [{table.Schema}].[{table.Name}] ADD CONSTRAINT [PK_{table.Name}] PRIMARY KEY CLUSTERED ([ID]);";
            }
        }

        private string GetModernDataType(string legacyType)
        {
            return legacyType.ToLower() switch
            {
                "text" => "VARCHAR(MAX)",
                "ntext" => "NVARCHAR(MAX)",
                "image" => "VARBINARY(MAX)",
                _ => "VARCHAR(MAX)" // Default fallback
            };
        }

        private IEnumerable<(Index, Index)> FindPotentialDuplicateIndexes(IEnumerable<Domain.Models.Index> indexes)
        {
            var result = new List<(Index, Index)>();
            var indexList = indexes.ToList();

            for (int i = 0; i < indexList.Count; i++)
            {
                for (int j = i + 1; j < indexList.Count; j++)
                {
                    var index1 = indexList[i];
                    var index2 = indexList[j];

                    // Skip primary keys and clustered indexes
                    if (index1.IsPrimaryKey || index2.IsPrimaryKey || index1.IsClustered || index2.IsClustered)
                    {
                        continue;
                    }

                    // Check if the indexes are potentially duplicates
                    if (IsPotentialDuplicate(index1, index2))
                    {
                        result.Add((index1, index2));
                    }
                }
            }

            return result;
        }

        private bool IsPotentialDuplicate(Index index1, Index index2)
        {
            // If both indexes have the same key columns (order matters)
            if (index1.Columns.Count == index2.Columns.Count &&
                !index1.Columns.Except(index2.Columns).Any() &&
                !index2.Columns.Except(index1.Columns).Any())
            {
                return true;
            }

            // If one index is a subset of another at the beginning of the column list
            if (index1.Columns.Count < index2.Columns.Count)
            {
                // Check if index1's columns are a leading subset of index2's columns
                for (int i = 0; i < index1.Columns.Count; i++)
                {
                    if (i >= index2.Columns.Count || index1.Columns[i] != index2.Columns[i])
                    {
                        return false;
                    }
                }
                return true;
            }

            // Same as above, but checking if index2's columns are a leading subset of index1's columns
            if (index2.Columns.Count < index1.Columns.Count)
            {
                for (int i = 0; i < index2.Columns.Count; i++)
                {
                    if (i >= index1.Columns.Count || index2.Columns[i] != index1.Columns[i])
                    {
                        return false;
                    }
                }
                return true;
            }

            return false;
        }

        private string GetIndexConsolidationScript(string schema, string tableName, Index index1, Index index2)
        {
            // Determine which index to keep and which to drop
            // Rule: Keep the one with included columns if applicable
            var indexToKeep = index1.IncludedColumns.Count >= index2.IncludedColumns.Count ? index1 : index2;
            var indexToDrop = indexToKeep == index1 ? index2 : index1;

            // If the kept index doesn't include all columns from the index to be dropped, create a new index
            var allIncludedColumns = new HashSet<string>(indexToKeep.IncludedColumns);

            // Add key columns from index to drop if they're not already key columns in index to keep
            var columnsToAdd = indexToDrop.Columns.Except(indexToKeep.Columns).ToList();

            if (columnsToAdd.Any() || indexToDrop.IncludedColumns.Except(indexToKeep.IncludedColumns).Any())
            {
                // Need to create a new index that combines both
                foreach (var col in indexToDrop.IncludedColumns)
                {
                    allIncludedColumns.Add(col);
                }

                var keyColumns = string.Join(", ", indexToKeep.Columns.Select(c => $"[{c}]"));
                var includedColumns = allIncludedColumns.Any()
                    ? $" INCLUDE ([{string.Join("], [", allIncludedColumns)}])"
                    : "";

                return $@"
-- Drop both existing indexes
DROP INDEX [{index1.Name}] ON [{schema}].[{tableName}];
DROP INDEX [{index2.Name}] ON [{schema}].[{tableName}];

-- Create a new consolidated index
CREATE INDEX [IX_{tableName}_Consolidated] ON [{schema}].[{tableName}] ({keyColumns}){includedColumns};";
            }
            else
            {
                // Just drop the duplicate index
                return $"DROP INDEX [{indexToDrop.Name}] ON [{schema}].[{tableName}];";
            }
        }

        private async Task<bool> IsTablePartitionedAsync(string connectionString, string databaseName, string schema, string tableName)
        {
            try
            {
                var query = $@"
                    USE [{databaseName}];
                    SELECT COUNT(*) 
                    FROM sys.tables t
                    JOIN sys.indexes i ON t.object_id = i.object_id
                    JOIN sys.partition_schemes ps ON i.data_space_id = ps.data_space_id
                    JOIN sys.partition_functions pf ON ps.function_id = pf.function_id
                    WHERE schema_name(t.schema_id) = '{schema}'
                    AND t.name = '{tableName}'
                    AND i.index_id = 1";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();

                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if table '{schema}.{tableName}' is partitioned");
                return false;
            }
        }

        private async Task<IEnumerable<SlowQuery>> GetSlowQueriesAsync(string connectionString, string databaseName)
        {
            var result = new List<SlowQuery>();

            try
            {
                var query = $@"
                    USE [{databaseName}];
                    IF EXISTS (SELECT 1 FROM sys.database_query_store_options WHERE actual_state = 1)
                    BEGIN
                        SELECT TOP 10 
                            q.query_id,
                            SUBSTRING(qt.query_sql_text, 1, 500) AS query_text,
                            rs.avg_duration / 1000.0 AS avg_duration_ms,
                            rs.max_duration / 1000.0 AS max_duration_ms,
                            rs.min_duration / 1000.0 AS min_duration_ms,
                            rs.avg_cpu_time / 1000.0 AS avg_cpu_time_ms,
                            rs.avg_logical_io_reads AS avg_logical_reads,
                            rs.avg_physical_io_reads AS avg_physical_reads,
                            rs.avg_logical_io_writes AS avg_writes,
                            rs.count_executions AS execution_count,
                            p.last_execution_time
                        FROM sys.query_store_query q
                        JOIN sys.query_store_query_text qt ON q.query_text_id = qt.query_text_id
                        JOIN sys.query_store_plan p ON q.query_id = p.query_id
                        JOIN sys.query_store_runtime_stats rs ON p.plan_id = rs.plan_id
                        JOIN sys.query_store_runtime_stats_interval rsi ON rs.runtime_stats_interval_id = rsi.runtime_stats_interval_id
                        WHERE rs.avg_duration > 100000  -- 100ms in microseconds
                        AND rs.count_executions > 10    -- Executed at least 10 times
                        AND p.last_execution_time > DATEADD(DAY, -7, GETUTCDATE())  -- Executed in the last 7 days
                        ORDER BY rs.avg_duration DESC;
                    END";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new SlowQuery
                    {
                        QueryId = reader.GetInt32(0),
                        QueryText = reader.GetString(1),
                        AvgDurationMs = reader.GetDouble(2),
                        MaxDurationMs = reader.GetDouble(3),
                        MinDurationMs = reader.GetDouble(4),
                        AvgCpuTimeMs = reader.GetDouble(5),
                        AvgLogicalReads = reader.GetDouble(6),
                        AvgPhysicalReads = reader.GetDouble(7),
                        AvgWrites = reader.GetDouble(8),
                        ExecutionCount = reader.GetInt64(9),
                        LastExecutionTime = reader.GetDateTime(10)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting slow queries for database '{databaseName}'");
            }

            return result;
        }

        private async Task<bool> IsQueryStoreEnabledAsync(string connectionString, string databaseName)
        {
            try
            {
                var query = $@"
                    USE [{databaseName}];
                    SELECT actual_state 
                    FROM sys.database_query_store_options;";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();

                // 1 = READ_WRITE, 2 = READ_ONLY
                return result != null && (Convert.ToInt32(result) == 1 || Convert.ToInt32(result) == 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if Query Store is enabled for database '{databaseName}'");
                return false;
            }
        }

        private async Task<bool> HasGuestPermissionsAsync(string connectionString, string databaseName)
        {
            try
            {
                var query = $@"
                    USE [{databaseName}];
                    SELECT COUNT(*) 
                    FROM sys.database_permissions
                    WHERE grantee_principal_id = DATABASE_PRINCIPAL_ID('guest')
                    AND state = 'G'  -- Only count explicitly granted permissions
                    AND permission_name <> 'CONNECT'  -- CONNECT permission is typically allowed for guest
                    ";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();

                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking guest permissions for database '{databaseName}'");
                return false;
            }
        }

        private async Task<IEnumerable<string>> GetPowerUsersAsync(string connectionString, string databaseName)
        {
            var result = new List<string>();

            try
            {
                var query = $@"
                    USE [{databaseName}];
                    SELECT dp.name AS principal_name
                    FROM sys.database_role_members drm
                    JOIN sys.database_principals dp ON drm.member_principal_id = dp.principal_id
                    JOIN sys.database_principals drole ON drm.role_principal_id = drole.principal_id
                    WHERE drole.name IN ('db_owner', 'db_securityadmin')
                    AND dp.name NOT IN ('dbo', 'sa')  -- Exclude system accounts
                    
                    UNION
                    
                    SELECT sp.name AS principal_name
                    FROM sys.server_principals sp
                    JOIN sys.server_role_members srm ON sp.principal_id = srm.member_principal_id
                    JOIN sys.server_principals srole ON srm.role_principal_id = srole.principal_id
                    WHERE srole.name = 'sysadmin'
                    AND sp.name NOT IN ('sa')  -- Exclude system accounts
                    AND sp.is_disabled = 0;  -- Only active logins
                    ";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(reader.GetString(0));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting power users for database '{databaseName}'");
            }

            return result;
        }

        private async Task<bool> HasSensitiveDataAsync(string connectionString, string databaseName)
        {
            try
            {
                // This is a simple heuristic to detect potentially sensitive data
                // In a real implementation, you would use more sophisticated methods
                var query = $@"
                    USE [{databaseName}];
                    SELECT COUNT(*) 
                    FROM sys.columns c
                    JOIN sys.tables t ON c.object_id = t.object_id
                    WHERE c.name LIKE '%credit%' OR c.name LIKE '%card%' OR
                          c.name LIKE '%ssn%' OR c.name LIKE '%social%security%' OR
                          c.name LIKE '%tax%id%' OR c.name LIKE '%password%' OR
                          c.name LIKE '%address%' OR c.name LIKE '%email%' OR
                          c.name LIKE '%phone%' OR c.name LIKE '%birth%date%' OR
                          c.name LIKE '%passport%' OR c.name LIKE '%license%' OR
                          c.name LIKE '%account%number%';
                    ";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();

                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking for sensitive data in database '{databaseName}'");
                return false;
            }
        }

        private async Task<IEnumerable<string>> FindPotentialSqlInjectionProcsAsync(string connectionString, string databaseName)
        {
            var result = new List<string>();

            try
            {
                // Look for stored procedures using dynamic SQL with concatenation
                var query = $@"
                    USE [{databaseName}];
                    SELECT SCHEMA_NAME(schema_id) + '.' + name AS proc_name
                    FROM sys.procedures
                    WHERE OBJECT_DEFINITION(object_id) LIKE '%EXEC(%'
                       OR OBJECT_DEFINITION(object_id) LIKE '%EXECUTE(%'
                       OR OBJECT_DEFINITION(object_id) LIKE '%sp_executesql%'
                       AND (OBJECT_DEFINITION(object_id) LIKE '%+%'
                       OR OBJECT_DEFINITION(object_id) LIKE '%@%+%'
                       OR OBJECT_DEFINITION(object_id) LIKE '%''%''%+%''%''%');
                    ";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(reader.GetString(0));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding potential SQL injection vulnerabilities in database '{databaseName}'");
            }

            return result;
        }

        private async Task<IEnumerable<SensitiveColumn>> FindSensitiveColumnsAsync(string connectionString, string databaseName)
        {
            var result = new List<SensitiveColumn>();

            try
            {
                // Simple heuristic to find potential sensitive columns based on naming
                var query = $@"
                    USE [{databaseName}];
                    SELECT 
                        SCHEMA_NAME(t.schema_id) + '.' + t.name AS table_name,
                        c.name AS column_name,
                        TYPE_NAME(c.user_type_id) AS data_type,
                        CASE
                            WHEN c.name LIKE '%credit%card%' OR c.name LIKE '%card%number%' THEN 'FinancialData'
                            WHEN c.name LIKE '%ssn%' OR c.name LIKE '%social%security%' THEN 'PersonalIdentifier'
                            WHEN c.name LIKE '%password%' OR c.name LIKE '%pwd%' THEN 'Credentials'
                            WHEN c.name LIKE '%email%' OR c.name LIKE '%phone%' OR c.name LIKE '%address%' THEN 'PersonalIdentifier'
                            WHEN c.name LIKE '%birth%date%' OR c.name LIKE '%dob%' THEN 'PersonalIdentifier'
                            WHEN c.name LIKE '%passport%' OR c.name LIKE '%license%' THEN 'PersonalIdentifier'
                            WHEN c.name LIKE '%account%number%' OR c.name LIKE '%routing%number%' THEN 'FinancialData'
                            WHEN c.name LIKE '%salary%' OR c.name LIKE '%income%' THEN 'FinancialData'
                            WHEN c.name LIKE '%health%' OR c.name LIKE '%medical%' THEN 'HealthInformation'
                            ELSE 'Other'
                        END AS sensitivity_type,
                        CASE WHEN EXISTS (
                            SELECT 1 FROM sys.column_encryption_keys 
                            JOIN sys.column_encryption_key_values ON column_encryption_keys.column_encryption_key_id = column_encryption_key_values.column_encryption_key_id
                            JOIN sys.columns_encryption_keys ON columns_encryption_keys.column_encryption_key_id = column_encryption_keys.column_encryption_key_id
                            WHERE columns_encryption_keys.column_id = c.column_id AND columns_encryption_keys.object_id = c.object_id
                        ) THEN 1 ELSE 0 END AS is_encrypted
                    FROM sys.columns c
                    JOIN sys.tables t ON c.object_id = t.object_id
                    WHERE c.name LIKE '%credit%card%' OR c.name LIKE '%card%number%' OR
                          c.name LIKE '%ssn%' OR c.name LIKE '%social%security%' OR
                          c.name LIKE '%tax%id%' OR c.name LIKE '%password%' OR c.name LIKE '%pwd%' OR
                          c.name LIKE '%email%' OR c.name LIKE '%phone%' OR c.name LIKE '%address%' OR
                          c.name LIKE '%birth%date%' OR c.name LIKE '%dob%' OR
                          c.name LIKE '%passport%' OR c.name LIKE '%license%' OR
                          c.name LIKE '%account%number%' OR c.name LIKE '%routing%number%' OR
                          c.name LIKE '%salary%' OR c.name LIKE '%income%' OR
                          c.name LIKE '%health%' OR c.name LIKE '%medical%';
                    ";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new SensitiveColumn
                    {
                        TableName = reader.GetString(0),
                        ColumnName = reader.GetString(1),
                        DataType = reader.GetString(2),
                        SensitivityType = Enum.Parse<SensitivityType>(reader.GetString(3)),
                        IsEncrypted = reader.GetInt32(4) == 1
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding sensitive columns in database '{databaseName}'");
            }

            return result;
        }

        private async Task<DateTime> GetLastLogBackupDateAsync(string connectionString, string databaseName)
        {
            try
            {
                var query = $@"
                    SELECT MAX(backup_finish_date)
                    FROM msdb.dbo.backupset
                    WHERE database_name = '{databaseName}'
                    AND type = 'L';  -- Log backup
                    ";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();

                return result == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting last log backup date for database '{databaseName}'");
                return DateTime.MinValue;
            }
        }

        private async Task<bool> IsBackupValidatedAsync(string connectionString, string databaseName)
        {
            try
            {
                // Check if there are backup verification entries in the jobhistory table
                var query = $@"
                    SELECT COUNT(*)
                    FROM msdb.dbo.sysjobs j
                    JOIN msdb.dbo.sysjobsteps js ON j.job_id = js.job_id
                    JOIN msdb.dbo.sysjobhistory jh ON j.job_id = jh.job_id
                    WHERE (j.name LIKE '%verify%backup%' OR j.name LIKE '%validate%backup%' OR 
                           js.command LIKE '%RESTORE VERIFYONLY%')
                    AND js.command LIKE '%{databaseName}%'
                    AND jh.run_date > CONVERT(int, CONVERT(varchar(8), DATEADD(month, -1, GETDATE()), 112));  -- Last month
                    ";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();

                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if backups are validated for database '{databaseName}'");
                return false;
            }
        }

        private async Task<IEnumerable<DbIssue>> CheckDiskSpaceAsync(string connectionString, string databaseName)
        {
            var issues = new List<DbIssue>();

            try
            {
                // First, get the drives where database files are stored
                var query = $@"
                    SELECT 
                        SUBSTRING(physical_name, 1, 3) AS drive,
                        SUM(size * 8 / 1024) AS size_mb
                    FROM sys.master_files
                    WHERE database_id = DB_ID('{databaseName}')
                    GROUP BY SUBSTRING(physical_name, 1, 3);
                    ";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var drives = new Dictionary<string, long>();

                using (var command = new SqlCommand(query, connection))
                {
                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        drives[reader.GetString(0)] = reader.GetInt64(1);
                    }
                }

                // Check disk space using xp_fixeddrives (requires permission)
                foreach (var drive in drives.Keys)
                {
                    try
                    {
                        var driveLetter = drive.TrimEnd(':', '\\');

                        using var command = new SqlCommand("EXEC xp_fixeddrives;", connection);
                        using var reader = await command.ExecuteReaderAsync();

                        while (await reader.ReadAsync())
                        {
                            string driveFromSql = reader.GetString(0);
                            int mbFree = reader.GetInt32(1);

                            if (driveFromSql.Equals(driveLetter, StringComparison.OrdinalIgnoreCase))
                            {
                                // Check if free space is less than 10% of total or less than 10GB
                                if (mbFree < 10240 || (drives.TryGetValue(drive, out long sizeMb) && mbFree < sizeMb * 0.1))
                                {
                                    issues.Add(new DbIssue
                                    {
                                        Type = IssueType.Capacity,
                                        Severity = IssueSeverity.High,
                                        Message = $"Low disk space on drive {drive}: {mbFree:N0} MB free",
                                        RecommendedAction = "Add additional disk space or move database files to a larger volume",
                                        DatabaseName = databaseName,
                                        AffectedObject = drive
                                    });
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error checking disk space for drive {drive}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking disk space for database '{databaseName}'");
            }

            return issues;
        }

        private async Task<double> GetDatabaseGrowthRateAsync(string connectionString, string databaseName)
        {
            try
            {
                // This is an approximation based on backup history
                var query = $@"
                    WITH BackupSizes AS (
                        SELECT 
                            backup_finish_date,
                            backup_size / 1024 / 1024 AS backup_size_mb
                        FROM msdb.dbo.backupset
                        WHERE database_name = '{databaseName}'
                        AND type = 'D'  -- Full backup
                        AND backup_finish_date > DATEADD(month, -3, GETDATE())
                        AND backup_finish_date < GETDATE()
                    )
                    SELECT 
                        CASE 
                            WHEN MIN(backup_size_mb) = 0 THEN 0  -- Avoid division by zero
                            ELSE (MAX(backup_size_mb) - MIN(backup_size_mb)) / MIN(backup_size_mb) * 100
                        END AS growth_percent_3months
                    FROM BackupSizes;
                    ";

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                using var command = new SqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();

                if (result == DBNull.Value)
                    return 0;

                // Convert 3-month growth rate to monthly
                double growthRate3Month = Convert.ToDouble(result);
                return growthRate3Month / 3;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating growth rate for database '{databaseName}'");
                return 0;
            }
        }
        #endregion
    }
}
