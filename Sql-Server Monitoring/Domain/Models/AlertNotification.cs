namespace Sql_Server_Monitoring.Domain.Models
{
    public class AlertNotification
    {
        public NotificationType Type { get; set; }
        public string Target { get; set; } // Email address, webhook URL, etc.
        public bool IncludeDetails { get; set; } = true;
    }
}
