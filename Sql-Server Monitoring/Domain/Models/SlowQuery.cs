namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public class SlowQuery
    {
        public int QueryId { get; set; }
        public string QueryText { get; set; }
        public double AvgDurationMs { get; set; }
        public double MaxDurationMs { get; set; }
        public double MinDurationMs { get; set; }
        public long ExecutionCount { get; set; }
        public double AvgCpuTimeMs { get; set; }
        public double AvgLogicalReads { get; set; }
        public double AvgPhysicalReads { get; set; }
        public double AvgWrites { get; set; }
        public DateTime LastExecutionTime { get; set; }
        public string? QueryPlan { get; internal set; }
    }
}
