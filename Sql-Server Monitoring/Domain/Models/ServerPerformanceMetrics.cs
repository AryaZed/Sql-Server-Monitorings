namespace Sql_Server_Monitoring.Domain.Models
{
    public class ServerPerformanceMetrics
    {
        public DateTime CollectionTime { get; set; }
        public CPUMetrics Cpu { get; set; }
        public MemoryMetrics Memory { get; set; }
        public DiskMetrics Disk { get; set; }
        public List<WaitStatistic> TopWaits { get; set; } = new List<WaitStatistic>();
    }
}
