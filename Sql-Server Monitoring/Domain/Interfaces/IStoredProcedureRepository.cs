using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Domain.Interfaces
{
    public interface IStoredProcedureRepository
    {
        Task<IEnumerable<StoredProcedure>> GetAllStoredProceduresAsync(string connectionString, string databaseName);
        Task<StoredProcedure> GetStoredProcedureDetailsAsync(string connectionString, string databaseName, string schemaName, string procedureName);
        Task<string> CreateStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName, string definition);
        Task<bool> UpdateStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName, string newDefinition);
        Task<bool> DeleteStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName);
        Task<IEnumerable<StoredProcedureParameter>> GetStoredProcedureParametersAsync(string connectionString, string databaseName, string schemaName, string procedureName);
        Task<string> ExecuteStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName, Dictionary<string, object> parameters);
        Task<IEnumerable<StoredProcedure>> FindUnusedStoredProceduresAsync(string connectionString, string databaseName, int daysSinceLastExecution = 90);
    }
} 