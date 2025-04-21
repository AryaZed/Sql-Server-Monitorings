using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IIssueRepository
    {
        Task<IEnumerable<DbIssue>> GetIssuesAsync(string connectionString = null, string databaseName = null, IssueType? type = null, IssueSeverity? minSeverity = null, bool includeResolved = false);
        Task<DbIssue> GetIssueByIdAsync(Guid id);
        Task AddIssueAsync(DbIssue issue);
        Task UpdateIssueAsync(DbIssue issue);
        Task DeleteIssueAsync(Guid id);
        Task MarkIssueAsResolvedAsync(Guid id);
    }
}
