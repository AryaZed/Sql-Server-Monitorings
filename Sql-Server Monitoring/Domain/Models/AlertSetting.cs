namespace Sql_Server_Monitoring.Domain.Models
{
    public class AlertSetting
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsEnabled { get; set; } = true;
        public AlertType Type { get; set; }
        public IssueSeverity MinimumSeverity { get; set; } = IssueSeverity.Medium;
        public List<AlertNotification> Notifications { get; set; } = new List<AlertNotification>();
    }
}
