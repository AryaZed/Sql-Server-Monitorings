using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IBackupService
    {
        Task<IEnumerable<BackupHistory>> GetBackupHistoryAsync(string connectionString, string databaseName);
        Task<string> PerformBackupAsync(string connectionString, string databaseName, BackupType backupType, string backupPath);
        Task<string> PerformRestoreAsync(string connectionString, string databaseName, string backupFilePath, string newDatabaseName = null);
        Task<IEnumerable<DbIssue>> AnalyzeBackupStrategyAsync(string connectionString, string databaseName);
    }
}
