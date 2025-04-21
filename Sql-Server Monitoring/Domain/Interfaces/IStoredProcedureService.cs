using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IStoredProcedureService
    {
        Task<IEnumerable<StoredProcedure>> GetAllStoredProceduresAsync(string connectionString, string databaseName);
        Task<StoredProcedure> GetStoredProcedureDetailsAsync(string connectionString, string databaseName, string schemaName, string procedureName);
        Task<string> CreateStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName, string definition);
        Task<bool> UpdateStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName, string newDefinition);
        Task<bool> DeleteStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName);
        Task<string> ExecuteStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName, Dictionary<string, object> parameters);
        Task<IEnumerable<StoredProcedure>> FindUnusedStoredProceduresAsync(string connectionString, string databaseName, int daysSinceLastExecution = 90);
        Task<IEnumerable<StoredProcedure>> FindPotentialSqlInjectionProceduresAsync(string connectionString, string databaseName);
        Task<StoredProcedureAnalysisResult> AnalyzeStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName);
    }

    public class StoredProcedureAnalysisResult
    {
        public List<string> Issues { get; set; } = new List<string>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public bool HasSqlInjectionRisk { get; set; }
        public bool HasPerformanceIssues { get; set; }
        public bool HasBestPracticeViolations { get; set; }
    }
} 