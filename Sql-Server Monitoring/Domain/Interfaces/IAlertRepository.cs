using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IAlertRepository
    {
        Task<IEnumerable<AlertNotification>> GetAllAlertsAsync();
        Task<IEnumerable<AlertNotification>> GetAlertsByTypeAsync(AlertType alertType);
        Task<IEnumerable<AlertNotification>> GetAlertsByDatabaseAsync(string databaseName);
        Task<IEnumerable<AlertNotification>> GetAlertsBySeverityAsync(IssueSeverity severity);
        Task<IEnumerable<AlertNotification>> GetRecentAlertsAsync(int hours);
        Task AddAlertAsync(AlertNotification alertNotification);
        Task MarkAlertAsResolvedAsync(string alertId);
        Task DeleteAlertAsync(string alertId);
    }
} 