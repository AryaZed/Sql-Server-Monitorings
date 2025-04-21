namespace Sql_Server_Monitoring.Domain.Models
{
    public class DiskIoStatistic
    {
        public string DatabaseName { get; set; }
        public string FileName { get; set; }
        public long ReadLatencyMs { get; set; }
        public long WriteLatencyMs { get; set; }
        public long ReadBytesPersec { get; set; }
        public long WriteBytesPersec { get; set; }
    }
}
