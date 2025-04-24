using System;
using System.Collections.Generic;

namespace Sql_Server_Monitoring.Domain.Models
{
    public class LogShippingConfig
    {
        public string PrimaryServer { get; set; }
        public string PrimaryDatabase { get; set; }
        public string BackupDirectory { get; set; }
        public string BackupShare { get; set; }
        public int BackupRetentionPeriod { get; set; }
        public int BackupFrequencyMinutes { get; set; }
        public string BackupCompression { get; set; }
        public DateTime LastBackupDate { get; set; }
        public List<LogShippingSecondary> SecondaryServers { get; set; } = new List<LogShippingSecondary>();
        public List<LogShippingMonitor> MonitorServers { get; set; } = new List<LogShippingMonitor>();
    }

    public class LogShippingSecondary
    {
        public string SecondaryServer { get; set; }
        public string SecondaryDatabase { get; set; }
        public string SecondaryDataDirectory { get; set; }
        public string SecondaryLogDirectory { get; set; }
        public string CopyDirectory { get; set; }
        public int CopyFrequencyMinutes { get; set; }
        public int CopyMaxLatencyMinutes { get; set; }
        public DateTime LastCopyDate { get; set; }
        public int RestoreFrequencyMinutes { get; set; }
        public int RestoreMaxLatencyMinutes { get; set; }
        public string RestoreMode { get; set; }
        public bool DisconnectUsers { get; set; }
        public DateTime LastRestoreDate { get; set; }
        public bool IsOutOfSync => (DateTime.Now - LastRestoreDate).TotalMinutes > RestoreMaxLatencyMinutes;
        public TimeSpan Latency => LastRestoreDate - LastCopyDate;
    }

    public class LogShippingMonitor
    {
        public string MonitorServer { get; set; }
        public string MonitorServerSecurity { get; set; }
        public string AlertThresholds { get; set; }
        public bool AlertEnabled { get; set; }
        public string AlertJobId { get; set; }
    }
} 