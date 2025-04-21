namespace Sql_Server_Monitoring.Domain.Models
{
    public enum WaitCategory
    {
        Cpu,
        Memory,
        Disk,
        Network,
        Locking,
        Other
    }
}
