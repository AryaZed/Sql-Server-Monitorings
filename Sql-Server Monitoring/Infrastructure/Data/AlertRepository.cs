using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Infrastructure.Data
{
    public class AlertRepository : IAlertRepository
    {
        private readonly ILogger<AlertRepository> _logger;
        private readonly Dictionary<string, AlertNotification> _alerts = new();

        public AlertRepository(ILogger<AlertRepository> logger)
        {
            _logger = logger;
        }

        public Task<IEnumerable<AlertNotification>> GetAllAlertsAsync()
        {
            return Task.FromResult<IEnumerable<AlertNotification>>(_alerts.Values.OrderByDescending(a => a.Timestamp));
        }

        public Task<IEnumerable<AlertNotification>> GetAlertsByTypeAsync(AlertType alertType)
        {
            var query = _alerts.Values.Where(a => a.Type.ToString().Contains(alertType.ToString()));
            return Task.FromResult<IEnumerable<AlertNotification>>(query.OrderByDescending(a => a.Timestamp));
        }

        public Task<IEnumerable<AlertNotification>> GetAlertsByDatabaseAsync(string databaseName)
        {
            var query = _alerts.Values.Where(a => a.DatabaseName == databaseName);
            return Task.FromResult<IEnumerable<AlertNotification>>(query.OrderByDescending(a => a.Timestamp));
        }

        public Task<IEnumerable<AlertNotification>> GetAlertsBySeverityAsync(IssueSeverity severity)
        {
            var query = _alerts.Values.Where(a => a.Severity == severity);
            return Task.FromResult<IEnumerable<AlertNotification>>(query.OrderByDescending(a => a.Timestamp));
        }

        public Task<IEnumerable<AlertNotification>> GetRecentAlertsAsync(int hours)
        {
            var cutoffTime = DateTime.Now.AddHours(-hours);
            var query = _alerts.Values.Where(a => a.Timestamp >= cutoffTime);
            return Task.FromResult<IEnumerable<AlertNotification>>(query.OrderByDescending(a => a.Timestamp));
        }

        public Task AddAlertAsync(AlertNotification alertNotification)
        {
            _logger.LogInformation($"Adding alert {alertNotification.Id} of type {alertNotification.Type}");
            
            if (_alerts.ContainsKey(alertNotification.Id))
            {
                _alerts[alertNotification.Id] = alertNotification;
            }
            else
            {
                _alerts.Add(alertNotification.Id, alertNotification);
            }
            
            return Task.CompletedTask;
        }

        public Task MarkAlertAsResolvedAsync(string alertId)
        {
            if (_alerts.TryGetValue(alertId, out var alert))
            {
                _logger.LogInformation($"Marking alert {alertId} as resolved");
                // In a real implementation, you would update some property to indicate resolution
                // For this example, we'll just note it in the log
            }
            else
            {
                _logger.LogWarning($"Alert {alertId} not found for resolution");
            }
            
            return Task.CompletedTask;
        }

        public Task DeleteAlertAsync(string alertId)
        {
            if (_alerts.Remove(alertId))
            {
                _logger.LogInformation($"Deleted alert {alertId}");
            }
            else
            {
                _logger.LogWarning($"Alert {alertId} not found for deletion");
            }
            
            return Task.CompletedTask;
        }
    }
} 