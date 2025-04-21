using Microsoft.Data.SqlClient;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace Sql_Server_Monitoring.Infrastructure.Data
{
    public class StoredProcedureRepository : IStoredProcedureRepository
    {
        private readonly ILogger<StoredProcedureRepository> _logger;

        public StoredProcedureRepository(ILogger<StoredProcedureRepository> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<StoredProcedure>> GetAllStoredProceduresAsync(string connectionString, string databaseName)
        {
            var procedures = new List<StoredProcedure>();

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var query = $@"
                    USE [{databaseName}];
                    SELECT 
                        SCHEMA_NAME(p.schema_id) AS SchemaName,
                        p.name AS ProcedureName,
                        ISNULL(OBJECT_DEFINITION(p.object_id), 'Encrypted') AS Definition,
                        p.create_date AS CreatedDate,
                        p.modify_date AS LastModifiedDate,
                        p.is_encrypted AS IsEncrypted,
                        (SELECT COUNT(*) FROM sys.parameters WHERE object_id = p.object_id) AS ParameterCount,
                        CASE WHEN OBJECT_DEFINITION(p.object_id) LIKE '%EXEC%' 
                             OR OBJECT_DEFINITION(p.object_id) LIKE '%EXECUTE%' 
                             OR OBJECT_DEFINITION(p.object_id) LIKE '%sp_executesql%' THEN 1 ELSE 0 END AS HasDynamicSql,
                        0 AS ExecutionCount, 
                        0 AS AverageDurationMs,
                        0 AS LastDurationMs,
                        NULL AS LastExecutionTime
                    FROM sys.procedures p
                    WHERE p.is_ms_shipped = 0
                    ORDER BY SchemaName, ProcedureName";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var proc = new StoredProcedure
                    {
                        SchemaName = reader.GetString(0),
                        Name = reader.GetString(1),
                        Definition = reader.GetString(2),
                        CreatedDate = reader.GetDateTime(3),
                        LastModifiedDate = reader.GetDateTime(4),
                        IsEncrypted = reader.GetBoolean(5),
                        ParameterCount = reader.GetInt32(6),
                        HasDynamicSql = reader.GetInt32(7) == 1,
                        ExecutionCount = reader.GetInt32(8),
                        AverageDurationMs = reader.GetInt32(9),
                        LastDurationMs = reader.GetInt32(10),
                        LastExecutionTime = reader.IsDBNull(11) ? null : reader.GetDateTime(11)
                    };

                    procedures.Add(proc);
                }

                // Get execution stats if Query Store is enabled
                if (await IsQueryStoreEnabledAsync(connection, databaseName))
                {
                    await EnrichWithQueryStoreDataAsync(connection, databaseName, procedures);
                }

                return procedures;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving stored procedures for database '{databaseName}'");
                throw;
            }
        }

        public async Task<StoredProcedure> GetStoredProcedureDetailsAsync(string connectionString, string databaseName, string schemaName, string procedureName)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Get procedure details
                var query = $@"
                    USE [{databaseName}];
                    SELECT 
                        SCHEMA_NAME(p.schema_id) AS SchemaName,
                        p.name AS ProcedureName,
                        ISNULL(OBJECT_DEFINITION(p.object_id), 'Encrypted') AS Definition,
                        p.create_date AS CreatedDate,
                        p.modify_date AS LastModifiedDate,
                        p.is_encrypted AS IsEncrypted,
                        (SELECT COUNT(*) FROM sys.parameters WHERE object_id = p.object_id) AS ParameterCount,
                        CASE WHEN OBJECT_DEFINITION(p.object_id) LIKE '%EXEC%' 
                             OR OBJECT_DEFINITION(p.object_id) LIKE '%EXECUTE%' 
                             OR OBJECT_DEFINITION(p.object_id) LIKE '%sp_executesql%' THEN 1 ELSE 0 END AS HasDynamicSql,
                        0 AS ExecutionCount, 
                        0 AS AverageDurationMs,
                        0 AS LastDurationMs,
                        NULL AS LastExecutionTime
                    FROM sys.procedures p
                    WHERE SCHEMA_NAME(p.schema_id) = @SchemaName AND p.name = @ProcedureName";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SchemaName", schemaName);
                command.Parameters.AddWithValue("@ProcedureName", procedureName);

                using var reader = await command.ExecuteReaderAsync();
                if (!await reader.ReadAsync())
                {
                    return null;
                }

                var proc = new StoredProcedure
                {
                    SchemaName = reader.GetString(0),
                    Name = reader.GetString(1),
                    Definition = reader.GetString(2),
                    CreatedDate = reader.GetDateTime(3),
                    LastModifiedDate = reader.GetDateTime(4),
                    IsEncrypted = reader.GetBoolean(5),
                    ParameterCount = reader.GetInt32(6),
                    HasDynamicSql = reader.GetInt32(7) == 1,
                    ExecutionCount = reader.GetInt32(8),
                    AverageDurationMs = reader.GetInt32(9),
                    LastDurationMs = reader.GetInt32(10),
                    LastExecutionTime = reader.IsDBNull(11) ? null : reader.GetDateTime(11)
                };

                // Get parameters
                proc.Parameters = (await GetStoredProcedureParametersAsync(connectionString, databaseName, schemaName, procedureName)).ToList();

                // Get execution stats if Query Store is enabled
                if (await IsQueryStoreEnabledAsync(connection, databaseName))
                {
                    await EnrichWithQueryStoreDataAsync(connection, databaseName, new List<StoredProcedure> { proc });
                }

                // Get dependencies
                proc.Dependencies = await GetProcedureDependenciesAsync(connection, databaseName, schemaName, procedureName);

                return proc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving details for stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                throw;
            }
        }

        public async Task<string> CreateStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName, string definition)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Check if the schema exists, create it if not
                var schemaQuery = $@"
                    USE [{databaseName}];
                    IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = @SchemaName)
                    BEGIN
                        EXEC('CREATE SCHEMA ' + QUOTENAME(@SchemaName));
                    END";

                using (var schemaCommand = new SqlCommand(schemaQuery, connection))
                {
                    schemaCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                    await schemaCommand.ExecuteNonQueryAsync();
                }

                // Execute the create procedure statement
                var createQuery = $"USE [{databaseName}]; {definition}";
                using var command = new SqlCommand(createQuery, connection);
                await command.ExecuteNonQueryAsync();

                return $"Created stored procedure {schemaName}.{procedureName} successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                throw;
            }
        }

        public async Task<bool> UpdateStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName, string newDefinition)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Check if the procedure exists
                var checkQuery = $@"
                    USE [{databaseName}];
                    SELECT COUNT(*) 
                    FROM sys.procedures p
                    JOIN sys.schemas s ON p.schema_id = s.schema_id
                    WHERE s.name = @SchemaName AND p.name = @ProcedureName";

                using (var checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                    checkCommand.Parameters.AddWithValue("@ProcedureName", procedureName);
                    int count = (int)await checkCommand.ExecuteScalarAsync();

                    if (count == 0)
                    {
                        _logger.LogWarning($"Stored procedure '{schemaName}.{procedureName}' not found in database '{databaseName}'");
                        return false;
                    }
                }

                // Execute the update
                var updateQuery = $"USE [{databaseName}]; {newDefinition}";
                using var command = new SqlCommand(updateQuery, connection);
                await command.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                throw;
            }
        }

        public async Task<bool> DeleteStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Check if the procedure exists
                var checkQuery = $@"
                    USE [{databaseName}];
                    SELECT COUNT(*) 
                    FROM sys.procedures p
                    JOIN sys.schemas s ON p.schema_id = s.schema_id
                    WHERE s.name = @SchemaName AND p.name = @ProcedureName";

                using (var checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@SchemaName", schemaName);
                    checkCommand.Parameters.AddWithValue("@ProcedureName", procedureName);
                    int count = (int)await checkCommand.ExecuteScalarAsync();

                    if (count == 0)
                    {
                        _logger.LogWarning($"Stored procedure '{schemaName}.{procedureName}' not found in database '{databaseName}'");
                        return false;
                    }
                }

                // Execute the drop procedure statement
                var dropQuery = $@"
                    USE [{databaseName}];
                    DROP PROCEDURE [{schemaName}].[{procedureName}]";

                using var command = new SqlCommand(dropQuery, connection);
                await command.ExecuteNonQueryAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                throw;
            }
        }

        public async Task<IEnumerable<StoredProcedureParameter>> GetStoredProcedureParametersAsync(string connectionString, string databaseName, string schemaName, string procedureName)
        {
            var parameters = new List<StoredProcedureParameter>();

            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var query = $@"
                    USE [{databaseName}];
                    SELECT 
                        p.name AS ParameterName,
                        t.name AS DataType,
                        p.has_default_value AS HasDefault,
                        p.is_output AS IsOutput,
                        p.max_length AS MaxLength,
                        p.precision AS Precision,
                        p.scale AS Scale
                    FROM sys.parameters p
                    JOIN sys.types t ON p.user_type_id = t.user_type_id
                    JOIN sys.procedures proc ON p.object_id = proc.object_id
                    JOIN sys.schemas s ON proc.schema_id = s.schema_id
                    WHERE s.name = @SchemaName AND proc.name = @ProcedureName
                    ORDER BY p.parameter_id";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@SchemaName", schemaName);
                command.Parameters.AddWithValue("@ProcedureName", procedureName);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    parameters.Add(new StoredProcedureParameter
                    {
                        Name = reader.GetString(0),
                        DataType = reader.GetString(1),
                        HasDefault = reader.GetBoolean(2),
                        IsOutput = reader.GetBoolean(3),
                        MaxLength = reader.GetInt16(4),
                        Precision = reader.GetByte(5),
                        Scale = reader.GetByte(6)
                    });
                }

                return parameters;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving parameters for stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                throw;
            }
        }

        public async Task<string> ExecuteStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName, Dictionary<string, object> parameters)
        {
            try
            {
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Set the database context
                using (var useDbCommand = new SqlCommand($"USE [{databaseName}]", connection))
                {
                    await useDbCommand.ExecuteNonQueryAsync();
                }

                // Get the parameter information
                var procParams = await GetStoredProcedureParametersAsync(connectionString, databaseName, schemaName, procedureName);
                
                // Create the command
                using var command = new SqlCommand($"[{schemaName}].[{procedureName}]", connection);
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 300; // 5 minutes timeout

                // Add parameters to the command
                foreach (var param in procParams)
                {
                    var sqlParameter = new SqlParameter(param.Name, GetSqlDbType(param.DataType));
                    
                    // Set direction
                    if (param.IsOutput)
                    {
                        sqlParameter.Direction = ParameterDirection.Output;
                    }
                    else
                    {
                        // If parameter provided in the input
                        if (parameters.TryGetValue(param.Name.TrimStart('@'), out var value))
                        {
                            sqlParameter.Value = value ?? DBNull.Value;
                        }
                        else
                        {
                            sqlParameter.Value = DBNull.Value;
                        }
                    }

                    command.Parameters.Add(sqlParameter);
                }

                // Create an output parameter to hold the return value
                var returnParam = new SqlParameter("@ReturnValue", SqlDbType.Int);
                returnParam.Direction = ParameterDirection.ReturnValue;
                command.Parameters.Add(returnParam);

                // Execute the procedure
                var result = new StringBuilder();
                result.AppendLine("Execution results:");

                // If any output parameters, need to execute non-query and then read output params
                // Otherwise, can read results directly
                bool hasOutputParams = procParams.Any(p => p.IsOutput);
                
                if (hasOutputParams)
                {
                    // Execute non-query to process params
                    await command.ExecuteNonQueryAsync();
                    
                    // Get return value
                    result.AppendLine($"Return value: {returnParam.Value}");
                    
                    // Get output parameters
                    result.AppendLine("Output parameters:");
                    foreach (var param in procParams.Where(p => p.IsOutput))
                    {
                        var sqlParam = command.Parameters[param.Name];
                        result.AppendLine($"  {param.Name}: {(sqlParam.Value == DBNull.Value ? "NULL" : sqlParam.Value)}");
                    }
                }
                else
                {
                    // Execute and get results as a reader
                    using var reader = await command.ExecuteReaderAsync();
                    
                    // Process all result sets
                    int resultSetCount = 0;
                    do
                    {
                        resultSetCount++;
                        result.AppendLine($"Result set {resultSetCount}:");
                        
                        // Get column names
                        var columnNames = new List<string>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            columnNames.Add(reader.GetName(i));
                        }
                        
                        result.AppendLine($"  {string.Join(" | ", columnNames)}");
                        result.AppendLine($"  {new string('-', columnNames.Sum(c => c.Length + 3))}");
                        
                        // Get rows (limit to 100 for readability)
                        int rowCount = 0;
                        while (await reader.ReadAsync() && rowCount < 100)
                        {
                            rowCount++;
                            var row = new List<string>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                row.Add(reader.IsDBNull(i) ? "NULL" : reader.GetValue(i).ToString());
                            }
                            result.AppendLine($"  {string.Join(" | ", row)}");
                        }
                        
                        if (rowCount >= 100)
                        {
                            result.AppendLine("  ... (more rows)");
                        }
                        
                    } while (await reader.NextResultAsync());
                    
                    // Get return value after reading all results
                    result.AppendLine($"Return value: {returnParam.Value}");
                }

                return result.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                throw;
            }
        }

        public async Task<IEnumerable<StoredProcedure>> FindUnusedStoredProceduresAsync(string connectionString, string databaseName, int daysSinceLastExecution = 90)
        {
            try
            {
                // Get all stored procedures
                var allProcedures = await GetAllStoredProceduresAsync(connectionString, databaseName);
                
                // Filter out procedures without execution stats or not used in the specified period
                var cutoffDate = DateTime.UtcNow.AddDays(-daysSinceLastExecution);
                
                var unusedProcedures = allProcedures.Where(p => 
                    p.ExecutionCount == 0 || 
                    (p.LastExecutionTime.HasValue && p.LastExecutionTime < cutoffDate))
                    .ToList();
                
                return unusedProcedures;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding unused stored procedures in database '{databaseName}'");
                throw;
            }
        }

        #region Private methods

        private async Task<bool> IsQueryStoreEnabledAsync(SqlConnection connection, string databaseName)
        {
            try
            {
                var query = $@"
                    USE [{databaseName}];
                    SELECT CASE WHEN EXISTS (
                        SELECT 1 FROM sys.database_query_store_options WHERE actual_state = 1
                    ) THEN 1 ELSE 0 END";

                using var command = new SqlCommand(query, connection);
                var result = await command.ExecuteScalarAsync();
                return Convert.ToBoolean(result);
            }
            catch
            {
                // If any error occurs (like Query Store not being supported in this version),
                // we'll just assume it's not enabled
                return false;
            }
        }

        private async Task EnrichWithQueryStoreDataAsync(SqlConnection connection, string databaseName, List<StoredProcedure> procedures)
        {
            if (procedures.Count == 0)
            {
                return;
            }

            try
            {
                var query = $@"
                    USE [{databaseName}];
                    WITH ProcStats AS (
                        SELECT 
                            OBJECT_NAME(q.object_id) AS proc_name,
                            SCHEMA_NAME(o.schema_id) AS schema_name,
                            COUNT(DISTINCT q.query_id) AS query_count,
                            SUM(rs.count_executions) AS execution_count,
                            AVG(rs.avg_duration / 1000.0) AS avg_duration_ms,
                            MAX(rs.last_execution_time) AS last_execution_time
                        FROM sys.query_store_query q
                        JOIN sys.objects o ON q.object_id = o.object_id
                        JOIN sys.query_store_plan p ON q.query_id = p.query_id
                        JOIN sys.query_store_runtime_stats rs ON p.plan_id = rs.plan_id
                        WHERE q.object_id > 0  -- Ensures it belongs to a procedure
                        GROUP BY q.object_id, SCHEMA_NAME(o.schema_id)
                    )
                    SELECT 
                        schema_name,
                        proc_name,
                        execution_count,
                        avg_duration_ms,
                        last_execution_time
                    FROM ProcStats
                    ORDER BY schema_name, proc_name";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                var statsDict = new Dictionary<string, (int ExecutionCount, int AverageDurationMs, DateTime LastExecutionTime)>();
                
                while (await reader.ReadAsync())
                {
                    string schemaName = reader.GetString(0);
                    string procName = reader.GetString(1);
                    int executionCount = reader.GetInt32(2);
                    int avgDurationMs = (int)reader.GetDouble(3);
                    DateTime lastExecutionTime = reader.GetDateTime(4);
                    
                    statsDict[$"{schemaName}.{procName}"] = (executionCount, avgDurationMs, lastExecutionTime);
                }

                // Update the procedures with the stats
                foreach (var proc in procedures)
                {
                    if (statsDict.TryGetValue($"{proc.SchemaName}.{proc.Name}", out var stats))
                    {
                        proc.ExecutionCount = stats.ExecutionCount;
                        proc.AverageDurationMs = stats.AverageDurationMs;
                        proc.LastExecutionTime = stats.LastExecutionTime;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enriching stored procedures with Query Store data");
                // We won't throw here, just continue with the base data
            }
        }

        private async Task<List<string>> GetProcedureDependenciesAsync(SqlConnection connection, string databaseName, string schemaName, string procedureName)
        {
            var dependencies = new List<string>();

            try
            {
                var query = $@"
                    USE [{databaseName}];
                    SELECT 
                        COALESCE(OBJECT_SCHEMA_NAME(referenced_id), '') + '.' + 
                        COALESCE(OBJECT_NAME(referenced_id), '') AS dependency
                    FROM sys.sql_expression_dependencies
                    WHERE referencing_id = OBJECT_ID('{schemaName}.{procedureName}')
                    AND referenced_id IS NOT NULL";

                using var command = new SqlCommand(query, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    dependencies.Add(reader.GetString(0));
                }

                return dependencies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting dependencies for stored procedure '{schemaName}.{procedureName}'");
                return dependencies; // Return empty list rather than failing
            }
        }

        private SqlDbType GetSqlDbType(string typeName)
        {
            return typeName.ToLower() switch
            {
                "bigint" => SqlDbType.BigInt,
                "binary" => SqlDbType.Binary,
                "bit" => SqlDbType.Bit,
                "char" => SqlDbType.Char,
                "date" => SqlDbType.Date,
                "datetime" => SqlDbType.DateTime,
                "datetime2" => SqlDbType.DateTime2,
                "datetimeoffset" => SqlDbType.DateTimeOffset,
                "decimal" => SqlDbType.Decimal,
                "float" => SqlDbType.Float,
                "image" => SqlDbType.Image,
                "int" => SqlDbType.Int,
                "money" => SqlDbType.Money,
                "nchar" => SqlDbType.NChar,
                "ntext" => SqlDbType.NText,
                "numeric" => SqlDbType.Decimal,
                "nvarchar" => SqlDbType.NVarChar,
                "real" => SqlDbType.Real,
                "smalldatetime" => SqlDbType.SmallDateTime,
                "smallint" => SqlDbType.SmallInt,
                "smallmoney" => SqlDbType.SmallMoney,
                "text" => SqlDbType.Text,
                "time" => SqlDbType.Time,
                "timestamp" => SqlDbType.Timestamp,
                "tinyint" => SqlDbType.TinyInt,
                "uniqueidentifier" => SqlDbType.UniqueIdentifier,
                "varbinary" => SqlDbType.VarBinary,
                "varchar" => SqlDbType.VarChar,
                "xml" => SqlDbType.Xml,
                _ => SqlDbType.NVarChar // Default
            };
        }

        #endregion
    }
} 