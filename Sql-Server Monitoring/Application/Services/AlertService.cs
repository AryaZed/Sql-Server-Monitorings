using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Application.Services
{
    public class AlertService : IAlertService
    {
        private readonly ISettingsRepository _settingsRepository;
        private readonly ILogger<AlertService> _logger;
        private readonly IAlertRepository _alertRepository;

        public AlertService(
            ISettingsRepository settingsRepository,
            ILogger<AlertService> logger,
            IAlertRepository alertRepository)
        {
            _settingsRepository = settingsRepository;
            _logger = logger;
            _alertRepository = alertRepository;
        }

        public async Task ConfigureAlertAsync(AlertSetting alertSetting)
        {
            try
            {
                _logger.LogInformation($"Configuring alert setting: {alertSetting.Name}");

                // Get current settings
                var settings = await _settingsRepository.GetMonitoringSettingsAsync();

                // Find existing alert with same name or type
                var existingAlertIndex = settings.Alerts.FindIndex(a => a.Type == alertSetting.Type);

                if (existingAlertIndex >= 0)
                {
                    // Update existing alert
                    settings.Alerts[existingAlertIndex] = alertSetting;
                }
                else
                {
                    // Add new alert
                    settings.Alerts.Add(alertSetting);
                }

                // Save settings
                await _settingsRepository.SaveMonitoringSettingsAsync(settings);

                _logger.LogInformation($"Alert setting {alertSetting.Name} configured successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error configuring alert setting: {alertSetting.Name}");
                throw;
            }
        }

        public async Task TriggerAlertAsync(string connectionString, string databaseName, DbIssue issue)
        {
            try
            {
                _logger.LogInformation($"Triggering alert for issue: {issue.Message}");

                // Get alert settings
                var settings = await _settingsRepository.GetMonitoringSettingsAsync();

                // Find alert setting for this issue type
                var alertType = MapIssueTypeToAlertType(issue.Type);
                var alertSetting = settings.Alerts.Find(a => a.Type == alertType && a.IsEnabled);

                if (alertSetting == null || issue.Severity < alertSetting.MinimumSeverity)
                {
                    _logger.LogInformation($"No alert configured for {issue.Type} issues with severity {issue.Severity}");
                    return;
                }

                // Process each notification channel
                foreach (var notification in alertSetting.Notifications)
                {
                    await SendNotificationAsync(notification, connectionString, databaseName, issue);
                }

                _logger.LogInformation($"Alert for issue {issue.Id} triggered successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error triggering alert for issue: {issue.Message}");
            }
        }

        public async Task<IEnumerable<AlertSetting>> GetAlertSettingsAsync()
        {
            try
            {
                var settings = await _settingsRepository.GetMonitoringSettingsAsync();
                return settings.Alerts;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting alert settings");
                throw;
            }
        }

        public async Task<AlertNotification> CreateAlertAsync(
            string connectionString,
            string databaseName,
            AlertType alertType,
            string message,
            string recommendedAction,
            IssueSeverity severity)
        {
            try
            {
                var alertNotification = new AlertNotification
                {
                    Type = alertType,
                    Message = message,
                    RecommendedAction = recommendedAction,
                    Severity = severity,
                    Timestamp = DateTime.Now,
                    DatabaseName = databaseName
                };

                // Store in repository
                await _alertRepository.AddAlertAsync(alertNotification);

                // Send notifications based on settings
                var notificationSettings = await _settingsRepository.GetAlertSettingsAsync();
                var shouldNotify = notificationSettings.Any(s => s.AlertType == alertType && s.Enabled);

                if (shouldNotify)
                {
                    await SendNotificationsAsync(alertNotification);
                }

                return alertNotification;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating alert for {alertType} on database {databaseName}");
                throw;
            }
        }

        #region Helper Methods
        private AlertType MapIssueTypeToAlertType(IssueType issueType)
        {
            return issueType switch
            {
                IssueType.Performance when issueType.ToString().Contains("CPU") => AlertType.HighCpu,
                IssueType.Performance when issueType.ToString().Contains("Memory") => AlertType.LowMemory,
                IssueType.Performance when issueType.ToString().Contains("I/O") => AlertType.IoBottleneck,
                IssueType.Performance when issueType.ToString().Contains("Query") => AlertType.LongRunningQuery,
                IssueType.Performance when issueType.ToString().Contains("Block") => AlertType.Blocking,
                IssueType.Performance when issueType.ToString().Contains("Deadlock") => AlertType.Deadlock,
                IssueType.Configuration => AlertType.ConfigurationIssue,
                IssueType.Security => AlertType.SecurityIssue,
                IssueType.Backup => AlertType.BackupFailure,
                IssueType.Capacity => AlertType.DiskSpace,
                _ => AlertType.ConfigurationIssue
            };
        }

        private async Task SendNotificationAsync(AlertNotification notification, string connectionString, string databaseName, DbIssue issue)
        {
            // Build notification content
            var subject = $"SQL Server Alert: {issue.Severity} {issue.Type} Issue";
            var message = new System.Text.StringBuilder();

            message.AppendLine($"Server: {connectionString?.Split(';').FirstOrDefault(s => s.StartsWith("Server=", StringComparison.OrdinalIgnoreCase))?.Substring(7) ?? "Unknown"}");
            message.AppendLine($"Database: {databaseName ?? "N/A"}");
            message.AppendLine($"Time: {DateTime.Now}");
            message.AppendLine($"Issue Type: {issue.Type}");
            message.AppendLine($"Severity: {issue.Severity}");
            message.AppendLine($"Message: {issue.Message}");

            if (notification.IncludeDetails)
            {
                message.AppendLine();
                message.AppendLine("Details:");
                message.AppendLine($"Affected Object: {issue.AffectedObject}");
                message.AppendLine($"Recommended Action: {issue.RecommendedAction}");

                if (!string.IsNullOrEmpty(issue.SqlScript))
                {
                    message.AppendLine();
                    message.AppendLine("Suggested Fix Script:");
                    message.AppendLine(issue.SqlScript);
                }
            }

            var content = message.ToString();

            // Send notification based on type
            switch (notification.Type)
            {
                case NotificationType.Email:
                    await SendEmailAsync(notification.Target, subject, content);
                    break;

                case NotificationType.Sms:
                    await SendSmsAsync(notification.Target, $"{subject}: {issue.Message}");
                    break;

                case NotificationType.Webhook:
                    await SendWebhookAsync(notification.Target, subject, content);
                    break;

                case NotificationType.Log:
                    _logger.LogWarning($"ALERT: {subject}\n{content}");
                    break;
            }
        }

        private Task SendEmailAsync(string emailAddress, string subject, string content)
        {
            // In a real implementation, this would send an email
            _logger.LogInformation($"Would send email to {emailAddress} with subject: {subject}");
            return Task.CompletedTask;
        }

        private Task SendSmsAsync(string phoneNumber, string message)
        {
            // In a real implementation, this would send an SMS
            _logger.LogInformation($"Would send SMS to {phoneNumber}: {message}");
            return Task.CompletedTask;
        }

        private Task SendWebhookAsync(string webhookUrl, string subject, string content)
        {
            // In a real implementation, this would call a webhook
            _logger.LogInformation($"Would call webhook {webhookUrl} with subject: {subject}");
            return Task.CompletedTask;
        }
        #endregion
    }
}
