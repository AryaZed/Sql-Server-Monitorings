using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IAlertService
    {
        Task ConfigureAlertAsync(AlertSetting alertSetting);
        Task TriggerAlertAsync(string connectionString, string databaseName, DbIssue issue);
        Task<IEnumerable<AlertSetting>> GetAlertSettingsAsync();
    }
}
