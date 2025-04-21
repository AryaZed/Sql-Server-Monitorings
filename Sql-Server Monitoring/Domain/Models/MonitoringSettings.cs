namespace Sql_Server_Monitoring.Domain.Models
{
    public class MonitoringSettings
    {
        public int MonitoringIntervalSeconds { get; set; } = 300; // Default: 5 minutes
        public bool MonitorCpu { get; set; } = true;
        public bool MonitorMemory { get; set; } = true;
        public bool MonitorDisk { get; set; } = true;
        public bool MonitorQueries { get; set; } = true;
        public bool MonitorBlocking { get; set; } = true;
        public bool MonitorDeadlocks { get; set; } = true;
        public int HighCpuThresholdPercent { get; set; } = 85;
        public int LowPageLifeExpectancyThreshold { get; set; } = 300;
        public int LongRunningQueryThresholdSec { get; set; } = 30;
        public int RetentionDays { get; set; } = 30;
        public bool MonitoringEnabled { get; set; } = false;
        public List<AlertSetting> Alerts { get; set; } = new List<AlertSetting>();
    }
}
