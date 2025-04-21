namespace Sql_Server_Monitoring.Domain.Models
{
    public enum AlertType
    {
        HighCpu,
        LowMemory,
        DiskSpace,
        LongRunningQuery,
        Blocking,
        Deadlock,
        BackupFailure,
        IoBottleneck,
        ConfigurationIssue,
        SecurityIssue
    }
}
