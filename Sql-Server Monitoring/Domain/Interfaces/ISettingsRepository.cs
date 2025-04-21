using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface ISettingsRepository
    {
        Task<MonitoringSettings> GetMonitoringSettingsAsync();
        Task SaveMonitoringSettingsAsync(MonitoringSettings settings);
    }
}
