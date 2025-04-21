using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Infrastructure.Data
{
    public class IssueRepository : IIssueRepository
    {
        private readonly ILogger<IssueRepository> _logger;
        private readonly Dictionary<Guid, DbIssue> _issues = new();

        public IssueRepository(ILogger<IssueRepository> logger)
        {
            _logger = logger;
        }

        public Task<IEnumerable<DbIssue>> GetIssuesAsync(string connectionString = null, string databaseName = null, IssueType? type = null, IssueSeverity? minSeverity = null, bool includeResolved = false)
        {
            var query = _issues.Values.AsEnumerable();

            if (!string.IsNullOrEmpty(connectionString))
            {
                // In a real implementation, this would filter by server
            }

            if (!string.IsNullOrEmpty(databaseName))
            {
                query = query.Where(i => i.DatabaseName == databaseName);
            }

            if (type.HasValue)
            {
                query = query.Where(i => i.Type == type.Value);
            }

            if (minSeverity.HasValue)
            {
                query = query.Where(i => i.Severity >= minSeverity.Value);
            }

            if (!includeResolved)
            {
                query = query.Where(i => !i.IsResolved);
            }

            return Task.FromResult<IEnumerable<DbIssue>>(query.OrderByDescending(i => i.Severity).ThenBy(i => i.DetectionTime));
        }

        public Task<DbIssue> GetIssueByIdAsync(Guid id)
        {
            return Task.FromResult(_issues.TryGetValue(id, out var issue) ? issue : null);
        }

        public Task AddIssueAsync(DbIssue issue)
        {
            _issues[issue.Id] = issue;
            _logger.LogInformation($"Added issue: {issue.Id} - {issue.Message}");
            return Task.CompletedTask;
        }

        public Task UpdateIssueAsync(DbIssue issue)
        {
            if (_issues.ContainsKey(issue.Id))
            {
                _issues[issue.Id] = issue;
                _logger.LogInformation($"Updated issue: {issue.Id}");
                return Task.CompletedTask;
            }

            throw new KeyNotFoundException($"Issue with ID {issue.Id} not found.");
        }

        public Task DeleteIssueAsync(Guid id)
        {
            if (_issues.Remove(id))
            {
                _logger.LogInformation($"Deleted issue: {id}");
                return Task.CompletedTask;
            }

            throw new KeyNotFoundException($"Issue with ID {id} not found.");
        }

        public async Task MarkIssueAsResolvedAsync(Guid id)
        {
            var issue = await GetIssueByIdAsync(id);
            if (issue != null)
            {
                issue.IsResolved = true;
                issue.ResolvedTime = DateTime.Now;
                await UpdateIssueAsync(issue);
            }
            else
            {
                throw new KeyNotFoundException($"Issue with ID {id} not found.");
            }
        }
    }
}
