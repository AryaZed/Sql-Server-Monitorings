using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Sql_Server_Monitoring.Domain.Models.Requests;
using System.Data;

namespace Sql_Server_Monitoring.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "AdminPolicy")]
    public class QueryController : ControllerBase
    {
        private readonly ILogger<QueryController> _logger;

        public QueryController(ILogger<QueryController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Executes a SQL query and returns the results.
        /// </summary>
        /// <param name="request">The query request.</param>
        /// <returns>A result containing the query results.</returns>
        [HttpPost("execute")]
        [ProducesResponseType(typeof(QueryResult), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<QueryResult>> ExecuteQuery([FromBody] QueryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation($"Executing query on database '{request.DatabaseName}'");

                // Validate the query to prevent dangerous operations
                if (IsDangerousQuery(request.QueryText))
                {
                    return BadRequest(new { message = "The query contains potentially dangerous operations. For security reasons, the query was not executed." });
                }

                var result = new QueryResult
                {
                    Success = false,
                    ExecutionTimeMs = -1,
                    RowCount = 0,
                    Columns = new List<string>(),
                    Rows = new List<Dictionary<string, object>>()
                };

                using var connection = new SqlConnection(request.ConnectionString);
                await connection.OpenAsync();

                // Set the database context
                using (var useDbCommand = new SqlCommand($"USE [{request.DatabaseName}]", connection))
                {
                    await useDbCommand.ExecuteNonQueryAsync();
                }

                // Execute the query
                using var command = new SqlCommand(request.QueryText, connection);
                command.CommandTimeout = request.TimeoutSeconds;

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                using var reader = await command.ExecuteReaderAsync();

                // Process results
                var rowCount = 0;
                
                // Get column names
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    result.Columns.Add(reader.GetName(i));
                }

                // Get rows
                while (await reader.ReadAsync() && rowCount < request.MaxRows)
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                        row[columnName] = value;
                    }
                    result.Rows.Add(row);
                    rowCount++;
                }

                stopwatch.Stop();

                result.Success = true;
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                result.RowCount = rowCount;
                result.MoreRows = (rowCount >= request.MaxRows);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing query on database '{request.DatabaseName}'");
                return StatusCode(500, new { message = $"Error executing query: {ex.Message}" });
            }
        }

        private bool IsDangerousQuery(string query)
        {
            // Normalize the query
            var normalizedQuery = query.ToUpper().Trim();

            // Dangerous operations to check for
            var dangerousOperations = new[]
            {
                "DROP DATABASE",
                "DROP TABLE",
                "TRUNCATE TABLE",
                "DELETE FROM",
                "UPDATE ",
                "ALTER DATABASE",
                "ALTER TABLE",
                "CREATE DATABASE",
                "CREATE TABLE",
                "INSERT INTO",
                "MERGE",
                "EXEC ",
                "EXECUTE ",
                "EXEC(",
                "EXECUTE(",
                "SP_EXECUTESQL",
                "BACKUP DATABASE",
                "RESTORE DATABASE",
                "SHUTDOWN",
                "XP_CMDSHELL",
                "GRANT ",
                "DENY ",
                "REVOKE ",
                "ADD USER",
                "DROP USER",
                "CREATE USER",
                "ALTER USER",
                "--",
                ";--",
                "/*",
                "*/",
                "WAITFOR DELAY",
                "OPENROWSET",
                "BULK INSERT"
            };

            // Check if the query contains any dangerous operations
            foreach (var operation in dangerousOperations)
            {
                if (normalizedQuery.Contains(operation))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class QueryResult
    {
        public bool Success { get; set; }
        public long ExecutionTimeMs { get; set; }
        public int RowCount { get; set; }
        public bool MoreRows { get; set; }
        public List<string> Columns { get; set; }
        public List<Dictionary<string, object>> Rows { get; set; }
    }
} 