namespace Sql_Server_Monitoring.Domain.Models
{
    public class MemoryMetrics
    {
        public long TotalServerMemoryMB { get; set; }
        public long TargetServerMemoryMB { get; set; }
        public long SqlCacheMemoryMB { get; set; }
        public long PlanCacheMemoryMB { get; set; }
        public long BufferPoolMemoryMB { get; set; }
        public double PageLifeExpectancy { get; set; }
        public bool IsMemoryPressure { get; set; }
    }
}
