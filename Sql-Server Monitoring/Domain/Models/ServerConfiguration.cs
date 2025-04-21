namespace Sql_Server_Monitoring.Domain.Models
{
    public class ServerConfiguration
    {
        public int MaxMemoryMB { get; set; }
        public int MaxWorkerThreads { get; set; }
        public int MaxDegreeOfParallelism { get; set; }
        public bool OptimizeForAdHocWorkloads { get; set; }
        public int CostThresholdForParallelism { get; set; }
        public List<ConfigSetting> Settings { get; set; } = new List<ConfigSetting>();
    }
}
