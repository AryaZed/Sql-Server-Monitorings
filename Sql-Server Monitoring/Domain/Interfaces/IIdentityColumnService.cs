using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IIdentityColumnService
    {
        Task<IEnumerable<IdentityColumnInfo>> GetIdentityColumnsAsync(string connectionString, string databaseName);
        Task<IEnumerable<DbIssue>> AnalyzeIdentityColumnsAsync(string connectionString, string databaseName);
        Task<bool> ReseedIdentityColumnAsync(
            string connectionString,
            string databaseName,
            string schemaName,
            string tableName,
            string columnName,
            long newSeedValue);
    }
} 