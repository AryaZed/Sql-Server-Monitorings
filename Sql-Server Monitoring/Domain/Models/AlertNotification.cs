namespace Sql_Server_Monitoring.Domain.Models
{
    public class AlertNotification
    {
        public NotificationType Type { get; set; }
        public string Target { get; set; } // Email address, webhook URL, etc.
        public bool IncludeDetails { get; set; } = true;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string DatabaseName { get; set; } = string.Empty;
        public IssueSeverity Severity { get; set; } = IssueSeverity.Medium;
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }
}
