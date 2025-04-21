using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IDatabaseOptimizerService
    {
        Task<IEnumerable<OptimizationScript>> GenerateOptimizationScriptsAsync(string connectionString, string databaseName, IEnumerable<DbIssue> issues);
        Task<string> ExecuteOptimizationScriptAsync(string connectionString, string databaseName, Guid scriptId);
        Task<OptimizationScript> GetRecommendedOptimizationAsync(string connectionString, string databaseName, IssueType issueType);
    }

    public static class DatabaseOptimizerServiceExtensions
    {
        public static Task<IEnumerable<OptimizationScript>> GetScriptsAsync(
            this IDatabaseOptimizerService service,
            string connectionString = null,
            string databaseName = null,
            ScriptType? type = null)
        {
            // This implementation would typically delegate to IOptimizationScriptRepository
            // For simplicity, we'll assume the extension method is implemented elsewhere
            throw new NotImplementedException("This method should be implemented by the concrete service class");
        }

        public static Task<OptimizationScript> GetScriptByIdAsync(
            this IDatabaseOptimizerService service,
            Guid id)
        {
            // This implementation would typically delegate to IOptimizationScriptRepository
            // For simplicity, we'll assume the extension method is implemented elsewhere
            throw new NotImplementedException("This method should be implemented by the concrete service class");
        }
    }
}
