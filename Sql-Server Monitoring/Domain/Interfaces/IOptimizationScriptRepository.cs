using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IOptimizationScriptRepository
    {
        Task<IEnumerable<OptimizationScript>> GetScriptsAsync(string connectionString = null, string databaseName = null, ScriptType? type = null);
        Task<OptimizationScript> GetScriptByIdAsync(Guid id);
        Task AddScriptAsync(OptimizationScript script);
        Task UpdateScriptAsync(OptimizationScript script);
        Task DeleteScriptAsync(Guid id);
        Task MarkScriptAsExecutedAsync(Guid id, string executionResult);
    }
}
