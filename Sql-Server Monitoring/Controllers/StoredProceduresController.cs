using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "UserPolicy")]
    public class StoredProceduresController : ControllerBase
    {
        private readonly IStoredProcedureService _storedProcedureService;
        private readonly ILogger<StoredProceduresController> _logger;

        public StoredProceduresController(
            IStoredProcedureService storedProcedureService,
            ILogger<StoredProceduresController> logger)
        {
            _storedProcedureService = storedProcedureService;
            _logger = logger;
        }

        /// <summary>
        /// Gets all stored procedures in a database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A list of stored procedures.</returns>
        [HttpGet("{databaseName}")]
        [ProducesResponseType(typeof(IEnumerable<StoredProcedure>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<StoredProcedure>>> GetAllStoredProcedures(
            [FromQuery] string connectionString,
            string databaseName)
        {
            try
            {
                var procedures = await _storedProcedureService.GetAllStoredProceduresAsync(connectionString, databaseName);
                return Ok(procedures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting stored procedures for database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting stored procedures for database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the details of a stored procedure.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="schemaName">The name of the schema.</param>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <returns>The stored procedure details.</returns>
        [HttpGet("{databaseName}/{schemaName}/{procedureName}")]
        [ProducesResponseType(typeof(StoredProcedure), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<StoredProcedure>> GetStoredProcedureDetails(
            [FromQuery] string connectionString,
            string databaseName,
            string schemaName,
            string procedureName)
        {
            try
            {
                var procedure = await _storedProcedureService.GetStoredProcedureDetailsAsync(connectionString, databaseName, schemaName, procedureName);
                if (procedure == null)
                {
                    return NotFound(new { message = $"Stored procedure '{schemaName}.{procedureName}' not found in database '{databaseName}'" });
                }
                return Ok(procedure);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting details for stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting details for stored procedure '{schemaName}.{procedureName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Creates a new stored procedure.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="schemaName">The name of the schema.</param>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <param name="request">The request containing the stored procedure definition.</param>
        /// <returns>A result indicating success or failure.</returns>
        [HttpPost("{databaseName}/{schemaName}/{procedureName}")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> CreateStoredProcedure(
            [FromQuery] string connectionString,
            string databaseName,
            string schemaName,
            string procedureName,
            [FromBody] StoredProcedureRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Definition))
                {
                    return BadRequest(new { message = "Stored procedure definition is required" });
                }

                var result = await _storedProcedureService.CreateStoredProcedureAsync(connectionString, databaseName, schemaName, procedureName, request.Definition);
                return CreatedAtAction(nameof(GetStoredProcedureDetails), new { connectionString, databaseName, schemaName, procedureName }, new { message = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error creating stored procedure '{schemaName}.{procedureName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Updates an existing stored procedure.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="schemaName">The name of the schema.</param>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <param name="request">The request containing the updated stored procedure definition.</param>
        /// <returns>A result indicating success or failure.</returns>
        [HttpPut("{databaseName}/{schemaName}/{procedureName}")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> UpdateStoredProcedure(
            [FromQuery] string connectionString,
            string databaseName,
            string schemaName,
            string procedureName,
            [FromBody] StoredProcedureRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Definition))
                {
                    return BadRequest(new { message = "Stored procedure definition is required" });
                }

                var success = await _storedProcedureService.UpdateStoredProcedureAsync(connectionString, databaseName, schemaName, procedureName, request.Definition);
                if (!success)
                {
                    return NotFound(new { message = $"Stored procedure '{schemaName}.{procedureName}' not found in database '{databaseName}'" });
                }

                return Ok(new { message = $"Stored procedure '{schemaName}.{procedureName}' updated successfully" });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error updating stored procedure '{schemaName}.{procedureName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a stored procedure.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="schemaName">The name of the schema.</param>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <returns>A result indicating success or failure.</returns>
        [HttpDelete("{databaseName}/{schemaName}/{procedureName}")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> DeleteStoredProcedure(
            [FromQuery] string connectionString,
            string databaseName,
            string schemaName,
            string procedureName)
        {
            try
            {
                var success = await _storedProcedureService.DeleteStoredProcedureAsync(connectionString, databaseName, schemaName, procedureName);
                if (!success)
                {
                    return NotFound(new { message = $"Stored procedure '{schemaName}.{procedureName}' not found in database '{databaseName}'" });
                }

                return Ok(new { message = $"Stored procedure '{schemaName}.{procedureName}' deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error deleting stored procedure '{schemaName}.{procedureName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Executes a stored procedure.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="schemaName">The name of the schema.</param>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <param name="parameters">The parameters to pass to the stored procedure.</param>
        /// <returns>The execution results.</returns>
        [HttpPost("{databaseName}/{schemaName}/{procedureName}/execute")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> ExecuteStoredProcedure(
            [FromQuery] string connectionString,
            string databaseName,
            string schemaName,
            string procedureName,
            [FromBody] Dictionary<string, object> parameters)
        {
            try
            {
                var result = await _storedProcedureService.ExecuteStoredProcedureAsync(connectionString, databaseName, schemaName, procedureName, parameters ?? new Dictionary<string, object>());
                return Ok(new { message = "Stored procedure executed successfully", result });
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error executing stored procedure '{schemaName}.{procedureName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Finds unused stored procedures.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="daysSinceLastExecution">Optional. The number of days since last execution.</param>
        /// <returns>A list of unused stored procedures.</returns>
        [HttpGet("{databaseName}/unused")]
        [ProducesResponseType(typeof(IEnumerable<StoredProcedure>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<StoredProcedure>>> FindUnusedStoredProcedures(
            [FromQuery] string connectionString,
            string databaseName,
            [FromQuery] int daysSinceLastExecution = 90)
        {
            try
            {
                var procedures = await _storedProcedureService.FindUnusedStoredProceduresAsync(connectionString, databaseName, daysSinceLastExecution);
                return Ok(procedures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding unused stored procedures in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error finding unused stored procedures in database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Finds stored procedures with potential SQL injection risks.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A list of potentially risky stored procedures.</returns>
        [HttpGet("{databaseName}/sql-injection-risks")]
        [ProducesResponseType(typeof(IEnumerable<StoredProcedure>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<StoredProcedure>>> FindPotentialSqlInjectionProcedures(
            [FromQuery] string connectionString,
            string databaseName)
        {
            try
            {
                var procedures = await _storedProcedureService.FindPotentialSqlInjectionProceduresAsync(connectionString, databaseName);
                return Ok(procedures);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding stored procedures with SQL injection risks in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error finding stored procedures with SQL injection risks in database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Analyzes a stored procedure for issues and recommendations.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="schemaName">The name of the schema.</param>
        /// <param name="procedureName">The name of the stored procedure.</param>
        /// <returns>Analysis results with issues and recommendations.</returns>
        [HttpGet("{databaseName}/{schemaName}/{procedureName}/analyze")]
        [ProducesResponseType(typeof(StoredProcedureAnalysisResult), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<StoredProcedureAnalysisResult>> AnalyzeStoredProcedure(
            [FromQuery] string connectionString,
            string databaseName,
            string schemaName,
            string procedureName)
        {
            try
            {
                var result = await _storedProcedureService.AnalyzeStoredProcedureAsync(connectionString, databaseName, schemaName, procedureName);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error analyzing stored procedure '{schemaName}.{procedureName}'", error = ex.Message });
            }
        }
    }

    public class StoredProcedureRequest
    {
        public string Definition { get; set; }
    }
} 