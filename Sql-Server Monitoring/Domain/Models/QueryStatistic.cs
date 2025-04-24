namespace Sql_Server_Monitoring.Domain.Models
{
    public class QueryStatistic
    {
        public string QueryText { get; set; }
        public int ExecutionCount { get; set; }
        public int TotalCpuTimeMs { get; set; }
        public int TotalLogicalReads { get; set; }
        public int TotalLogicalWrites { get; set; }
        public int TotalElapsedTimeMs { get; set; }
        public int TotalRowCount { get; set; }
        public double AvgCpuTimeMs { get; set; }
        public double AvgLogicalReads { get; set; }
        public double AvgLogicalWrites { get; set; }
        public double AvgElapsedTimeMs { get; set; }
        public double AvgRowCount { get; set; }
        public int MaxCpuTimeMs { get; set; }
        public int MaxLogicalReads { get; set; }
        public int MaxLogicalWrites { get; set; }
        public int MaxElapsedTimeMs { get; set; }
        public int MaxRowCount { get; set; }
        public DateTime LastExecutionTime { get; set; }
        public string DatabaseName { get; set; }
        public string PlanHandle { get; set; }
        public string SqlHandle { get; set; }
        public string QueryHash { get; set; }
        public string QueryPlanHash { get; set; }
    }
}
