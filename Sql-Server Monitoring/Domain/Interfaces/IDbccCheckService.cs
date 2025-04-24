using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IDbccCheckService
    {
        Task<IEnumerable<DbccCheckHistory>> GetDbccCheckHistoryAsync(string connectionString);
        Task<IEnumerable<DbIssue>> AnalyzeDbccChecksAsync(string connectionString);
        Task<bool> RunDbccCheckAsync(string connectionString, string databaseName);
    }
} 