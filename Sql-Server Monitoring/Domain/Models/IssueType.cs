namespace Sql_Server_Monitoring.Domain.Models
{
    public enum IssueType
    {
        Performance,
        Schema,
        Index,
        Configuration,
        Security,
        Backup,
        Capacity,
        Connectivity,
        Corruption,
        AgentJob,
        AvailabilityGroup,
        LogShipping,
        Mirroring,
        IdentityColumn,
        TempDB,
        ResourceGovernor,
        QueryStore,
        SchemaChange,
        DDLTracking,
        DriverIssue
    }
}
