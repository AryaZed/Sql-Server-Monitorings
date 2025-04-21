namespace Sql_Server_Monitoring.Domain.Models
{
    public class CPUMetrics
    {
        public double UtilizationPercent { get; set; }
        public int ActiveWorkerThreads { get; set; }
        public int ActiveRequests { get; set; }
        public List<CpuIntensiveQuery> TopCpuQueries { get; set; } = new List<CpuIntensiveQuery>();
    }
}
