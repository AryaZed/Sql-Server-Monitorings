using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IAlertService
    {
        Task ConfigureAlertAsync(AlertSetting alertSetting);
        Task TriggerAlertAsync(string connectionString, string databaseName, DbIssue issue);
        Task<IEnumerable<AlertSetting>> GetAlertSettingsAsync();
        Task<bool> SetupAlertsAsync(string connectionString);
        Task<bool> EnableAlertAsync(AlertType alertType, bool enabled);
        Task<bool> TestAlertsAsync();
        Task<AlertNotification> CreateAlertAsync(
            string connectionString,
            string databaseName,
            AlertType alertType,
            string message,
            string recommendedAction,
            IssueSeverity severity);
    }
}
