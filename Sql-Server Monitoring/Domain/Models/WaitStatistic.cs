namespace Sql_Server_Monitoring.Domain.Models
{
    public class WaitStatistic
    {
        public string WaitType { get; set; }
        public long WaitTimeMs { get; set; }
        public long WaitingTasksCount { get; set; }
        public string Description { get; set; }
        public WaitCategory Category { get; set; }
    }
}
