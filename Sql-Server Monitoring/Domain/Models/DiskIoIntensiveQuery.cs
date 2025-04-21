namespace Sql_Server_Monitoring.Domain.Models
{
    public class DiskIoIntensiveQuery
    {
        public int QueryId { get; set; }
        public string QueryText { get; set; }
        public long LogicalReads { get; set; }
        public long PhysicalReads { get; set; }
        public long Writes { get; set; }
        public DateTime LastExecutionTime { get; set; }
        public long ExecutionCount { get; set; }
    }
}
