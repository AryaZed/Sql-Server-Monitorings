namespace Sql_Server_Monitoring.Domain.Models
{
    public class DiskMetrics
    {
        public List<DiskIoStatistic> IoStats { get; set; } = new List<DiskIoStatistic>();
        public List<DiskIoIntensiveQuery> TopIoQueries { get; set; } = new List<DiskIoIntensiveQuery>();
    }
}
