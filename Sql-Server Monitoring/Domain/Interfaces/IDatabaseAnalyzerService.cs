using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IDatabaseAnalyzerService
    {
        Task<IEnumerable<DbIssue>> AnalyzeDatabaseAsync(string connectionString, string databaseName);
        Task<IEnumerable<DbIssue>> AnalyzeSchemaStructureAsync(string connectionString, string databaseName);
        Task<IEnumerable<DbIssue>> AnalyzePerformanceAsync(string connectionString, string databaseName);
        Task<IEnumerable<DbIssue>> AnalyzeIndexesAsync(string connectionString, string databaseName);
        Task<IEnumerable<DbIssue>> AnalyzeConfigurationAsync(string connectionString, string databaseName);
        Task<IEnumerable<DbIssue>> AnalyzeSecurityAsync(string connectionString, string databaseName);
        Task<IEnumerable<DbIssue>> AnalyzeBackupStrategyAsync(string connectionString, string databaseName);
        Task<IEnumerable<DbIssue>> AnalyzeCapacityAsync(string connectionString, string databaseName);
    }
}
