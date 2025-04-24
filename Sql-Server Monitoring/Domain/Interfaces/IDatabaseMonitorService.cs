using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IDatabaseMonitorService
    {
        Task StartMonitoringAsync(string connectionString, CancellationToken cancellationToken);
        Task StopMonitoringAsync();
        Task<ServerPerformanceMetrics> GetCurrentMetricsAsync(string connectionString);
        Task<IEnumerable<DbIssue>> DetectPerformanceIssuesAsync(string connectionString);
        Task<bool> CheckDatabaseConnectivityAsync(string connectionString);
    }
}
