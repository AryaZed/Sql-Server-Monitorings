namespace Sql_Server_Monitoring.Domain.Models
{
    public class CpuIntensiveQuery
    {
        public int QueryId { get; set; }
        public string QueryText { get; set; }
        public double CpuTimeMs { get; set; }
        public DateTime LastExecutionTime { get; set; }
        public long ExecutionCount { get; set; }
    }
}
