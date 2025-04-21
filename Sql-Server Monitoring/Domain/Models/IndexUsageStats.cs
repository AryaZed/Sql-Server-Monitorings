namespace Sql_Server_Monitoring.Domain.Models
{
    public class IndexUsageStats
    {
        public long UserSeeks { get; set; }
        public long UserScans { get; set; }
        public long UserLookups { get; set; }
        public long UserUpdates { get; set; }
        public DateTime LastUsed { get; set; }
    }
}
