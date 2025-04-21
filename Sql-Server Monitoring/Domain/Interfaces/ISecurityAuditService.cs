using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface ISecurityAuditService
    {
        Task<IEnumerable<DbIssue>> AuditPermissionsAsync(string connectionString, string databaseName);
        Task<IEnumerable<DbIssue>> AuditSensitiveDataAsync(string connectionString, string databaseName);
        Task<IEnumerable<DbIssue>> AuditLoginSecurityAsync(string connectionString);
        Task<IEnumerable<UserPermission>> GetUserPermissionsAsync(string connectionString, string databaseName, string userName = null);
        Task<IEnumerable<SensitiveColumn>> IdentifySensitiveColumnsAsync(string connectionString, string databaseName);
    }
}
