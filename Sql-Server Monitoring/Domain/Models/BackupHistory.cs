namespace Sql_Server_Monitoring.Domain.Models
{
    public class BackupHistory
    {
        public string DatabaseName { get; set; }
        public DateTime BackupStartTime { get; set; }
        public DateTime BackupFinishTime { get; set; }
        public decimal BackupSizeMB { get; set; }
        public BackupType BackupType { get; set; }
        public string ServerName { get; set; }
        public string RecoveryModel { get; set; }
        public decimal CompressedSizeMB { get; set; }
        public string BackupLocation { get; set; }
        public bool Verified { get; set; }
    }
} 