using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IDatabaseRepository
    {
        Task<IEnumerable<string>> GetUserDatabasesAsync(string connectionString);
        Task<Database> GetDatabaseDetailsAsync(string connectionString, string databaseName);
        Task<IEnumerable<Table>> GetTablesAsync(string connectionString, string databaseName);
        Task<Table> GetTableDetailsAsync(string connectionString, string databaseName, string schemaName, string tableName);
        Task<IEnumerable<Models.Index>> GetIndexesAsync(string connectionString, string databaseName, string schemaName, string tableName);
        Task<ServerConfiguration> GetServerConfigurationAsync(string connectionString);
        Task<ServerPerformanceMetrics> GetServerPerformanceMetricsAsync(string connectionString);
        Task ExecuteScriptAsync(string connectionString, string databaseName, string script);
        Task PurgeOldMetricsAsync(string connectionString, int retentionDays);
    }
}
