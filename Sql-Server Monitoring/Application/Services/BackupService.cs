using Microsoft.Data.SqlClient;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Application.Services
{
    public class BackupService : IBackupService
    {
        private readonly IDatabaseRepository _databaseRepository;
        private readonly ILogger<BackupService> _logger;

        public BackupService(
            IDatabaseRepository databaseRepository,
            ILogger<BackupService> logger)
        {
            _databaseRepository = databaseRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<BackupHistory>> GetBackupHistoryAsync(string connectionString, string databaseName)
        {
            try
            {
                _logger.LogInformation($"Getting backup history for {(string.IsNullOrEmpty(databaseName) ? "all databases" : $"database '{databaseName}'")}");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT
                        s.database_name,
                        s.backup_start_date,
                        s.backup_finish_date,
                        CAST(s.backup_size / 1024.0 / 1024.0 AS DECIMAL(10, 2)) AS backup_size_mb,
                        s.type,
                        s.server_name,
                        s.recovery_model,
                        CAST(s.compressed_backup_size / 1024.0 / 1024.0 AS DECIMAL(10, 2)) AS compressed_size_mb,
                        m.physical_device_name
                    FROM msdb.dbo.backupset s
                    INNER JOIN msdb.dbo.backupmediafamily m ON s.media_set_id = m.media_set_id
                    WHERE 1=1
                        " + (string.IsNullOrEmpty(databaseName) ? "" : "AND s.database_name = @DatabaseName") + @"
                    ORDER BY s.backup_start_date DESC";

                using var command = new SqlCommand(query, connection);
                if (!string.IsNullOrEmpty(databaseName))
                {
                    command.Parameters.AddWithValue("@DatabaseName", databaseName);
                }

                var backupHistory = new List<BackupHistory>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var history = new BackupHistory
                    {
                        DatabaseName = reader.GetString(0),
                        BackupStartTime = reader.GetDateTime(1),
                        BackupFinishTime = reader.GetDateTime(2),
                        BackupSizeMB = reader.GetDecimal(3),
                        BackupType = MapBackupType(reader.GetString(4)),
                        ServerName = reader.GetString(5),
                        RecoveryModel = reader.GetString(6),
                        CompressedSizeMB = reader.GetDecimal(7),
                        BackupLocation = reader.GetString(8)
                    };

                    backupHistory.Add(history);
                }

                _logger.LogInformation($"Retrieved {backupHistory.Count} backup history records");
                return backupHistory;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting backup history for {(string.IsNullOrEmpty(databaseName) ? "all databases" : $"database '{databaseName}'")}");
                throw;
            }
        }

        public async Task<string> PerformBackupAsync(string connectionString, string databaseName, BackupType backupType, string backupPath)
        {
            try
            {
                _logger.LogInformation($"Initiating {backupType} backup for database '{databaseName}'");

                // Validate database exists
                var databases = await _databaseRepository.GetUserDatabasesAsync(connectionString);
                if (!databases.Contains(databaseName))
                {
                    throw new ArgumentException($"Database '{databaseName}' does not exist");
                }

                // Create backup file name
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var backupFileName = $"{databaseName}_{backupType}_{timestamp}.bak";
                var fullBackupPath = Path.Combine(backupPath, backupFileName);

                // Execute backup command
                var backupTypeString = GetBackupTypeString(backupType);
                var backupCommand = $@"
                    BACKUP {backupTypeString} {databaseName}
                    TO DISK = '{fullBackupPath}'
                    WITH COMPRESSION, CHECKSUM, STATS = 10";

                // Execute backup
                await _databaseRepository.ExecuteScriptAsync(connectionString, "master", backupCommand);

                _logger.LogInformation($"Successfully initiated {backupType} backup for database '{databaseName}' to {fullBackupPath}");
                return fullBackupPath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error initiating {backupType} backup for database '{databaseName}'");
                throw;
            }
        }

        public async Task<string> PerformRestoreAsync(string connectionString, string databaseName, string backupFilePath, string newDatabaseName = null)
        {
            try
            {
                string targetDatabase = newDatabaseName ?? databaseName;
                _logger.LogInformation($"Restoring database {targetDatabase} from backup file {backupFilePath}");

                // Verify backup file exists
                if (!File.Exists(backupFilePath))
                {
                    throw new FileNotFoundException($"Backup file not found: {backupFilePath}");
                }

                // Check if target database already exists
                var databases = await _databaseRepository.GetUserDatabasesAsync(connectionString);
                bool databaseExists = databases.Contains(targetDatabase);

                // Construct restore command
                string restoreCommand;
                if (databaseExists)
                {
                    // Need to handle existing database - first set to single user mode
                    restoreCommand = $@"
                        ALTER DATABASE [{targetDatabase}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        RESTORE DATABASE [{targetDatabase}] FROM DISK = '{backupFilePath}'
                        WITH REPLACE,
                        STATS = 10;
                        ALTER DATABASE [{targetDatabase}] SET MULTI_USER;";
                }
                else
                {
                    // Simple restore for new database
                    restoreCommand = $@"
                        RESTORE DATABASE [{targetDatabase}] FROM DISK = '{backupFilePath}'
                        WITH STATS = 10;";
                }

                // Execute restore
                await _databaseRepository.ExecuteScriptAsync(connectionString, "master", restoreCommand);

                _logger.LogInformation($"Successfully restored database {targetDatabase} from backup file {backupFilePath}");
                return $"Database {targetDatabase} successfully restored";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring database {databaseName} from backup file {backupFilePath}");
                throw;
            }
        }

        public async Task<IEnumerable<DbIssue>> AnalyzeBackupStrategyAsync(string connectionString, string databaseName)
        {
            try
            {
                _logger.LogInformation($"Analyzing backup strategy for database '{databaseName}'");
                var issues = new List<DbIssue>();

                // Get database info to determine recovery model
                var database = await _databaseRepository.GetDatabaseDetailsAsync(connectionString, databaseName);
                if (database == null)
                {
                    throw new ArgumentException($"Database '{databaseName}' not found");
                }

                // Get backup history
                var backupHistory = await GetBackupHistoryAsync(connectionString, databaseName);
                var fullBackups = backupHistory.Where(b => b.BackupType == BackupType.Full).ToList();
                var diffBackups = backupHistory.Where(b => b.BackupType == BackupType.Differential).ToList();
                var logBackups = backupHistory.Where(b => b.BackupType == BackupType.TransactionLog).ToList();

                // Check if there are any backups at all
                if (!fullBackups.Any())
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Backup,
                        Severity = IssueSeverity.High,
                        DatabaseName = databaseName,
                        Message = $"No full backups found for database '{databaseName}'",
                        RecommendedAction = "Implement a regular full backup strategy",
                        DetectionTime = DateTime.Now
                    });
                }
                else
                {
                    // Check when the last full backup was performed
                    var lastFullBackup = fullBackups.First().BackupStartTime;
                    var daysSinceLastFullBackup = (DateTime.Now - lastFullBackup).TotalDays;

                    if (daysSinceLastFullBackup > 7)
                    {
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Backup,
                            Severity = IssueSeverity.Medium,
                            DatabaseName = databaseName,
                            Message = $"Last full backup is {daysSinceLastFullBackup:N1} days old for database '{databaseName}'",
                            RecommendedAction = "Perform a full backup as soon as possible",
                            DetectionTime = DateTime.Now
                        });
                    }
                }

                // Check log backup strategy for databases in full recovery model
                if (database.RecoveryModel == RecoveryModel.Full && !logBackups.Any())
                {
                    issues.Add(new DbIssue
                    {
                        Type = IssueType.Backup,
                        Severity = IssueSeverity.High,
                        DatabaseName = databaseName,
                        Message = $"Database '{databaseName}' is in full recovery model but no log backups were found",
                        RecommendedAction = "Implement regular transaction log backups to prevent log file growth and enable point-in-time recovery",
                        DetectionTime = DateTime.Now
                    });
                }
                else if (database.RecoveryModel == RecoveryModel.Full && logBackups.Any())
                {
                    // Check when the last log backup was performed
                    var lastLogBackup = logBackups.First().BackupStartTime;
                    var hoursSinceLastLogBackup = (DateTime.Now - lastLogBackup).TotalHours;

                    if (hoursSinceLastLogBackup > 24)
                    {
                        issues.Add(new DbIssue
                        {
                            Type = IssueType.Backup,
                            Severity = hoursSinceLastLogBackup > 72 ? IssueSeverity.High : IssueSeverity.Medium,
                            DatabaseName = databaseName,
                            Message = $"Last log backup is {hoursSinceLastLogBackup:N1} hours old for database '{databaseName}'",
                            RecommendedAction = "Perform a transaction log backup as soon as possible",
                            DetectionTime = DateTime.Now
                        });
                    }
                }

                _logger.LogInformation($"Identified {issues.Count} backup strategy issues for database '{databaseName}'");
                return issues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing backup strategy for database '{databaseName}'");
                throw;
            }
        }

        private BackupType MapBackupType(string sqlBackupType)
        {
            return sqlBackupType.ToUpper() switch
            {
                "D" => BackupType.Full,
                "I" => BackupType.Differential,
                "L" => BackupType.TransactionLog,
                _ => BackupType.Full
            };
        }

        private string GetBackupTypeString(BackupType backupType)
        {
            return backupType switch
            {
                BackupType.Full => "DATABASE",
                BackupType.Differential => "DATABASE",
                BackupType.TransactionLog => "LOG",
                _ => "DATABASE"
            };
        }
    }
} 