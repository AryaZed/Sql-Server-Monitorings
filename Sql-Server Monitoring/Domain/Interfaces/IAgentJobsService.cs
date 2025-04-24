using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IAgentJobsService
    {
        Task<IEnumerable<AgentJob>> GetAllJobsAsync(string connectionString);
        Task<AgentJob> GetJobDetailsByNameAsync(string connectionString, string jobName);
        Task<IEnumerable<AgentJob>> GetFailedJobsAsync(string connectionString, int lookbackHours = 24);
        Task<IEnumerable<DbIssue>> AnalyzeJobsAsync(string connectionString, string databaseName = null);
    }
} 