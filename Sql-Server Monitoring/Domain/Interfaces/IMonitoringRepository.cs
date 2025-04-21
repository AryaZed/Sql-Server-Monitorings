using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IMonitoringRepository
    {
        Task SavePerformanceMetricsAsync(string connectionString, ServerPerformanceMetrics metrics);
        Task<IEnumerable<ServerPerformanceMetrics>> GetPerformanceHistoryAsync(string connectionString, DateTime startTime, DateTime endTime);
        Task PurgeOldMetricsAsync(string connectionString, int retentionDays);
    }
}
