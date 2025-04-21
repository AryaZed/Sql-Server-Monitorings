namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IQueryAnalyzerService
    {
        Task<IEnumerable<SlowQuery>> GetSlowQueriesAsync(string connectionString, string databaseName, int topN = 10);
        Task<IEnumerable<QueryPlanIssue>> AnalyzeQueryPlanAsync(string connectionString, string databaseName, int queryId);
        Task<string> GetQueryTextAsync(string connectionString, string databaseName, int queryId);
        Task<string> GetQueryPlanAsync(string connectionString, string databaseName, int queryId);
        Task<IEnumerable<QueryStatistic>> GetQueryStatisticsAsync(string connectionString, string databaseName, int queryId);
        Task<string> GetQueryOptimizationRecommendationsAsync(string connectionString, string databaseName, int queryId);
    }
}
