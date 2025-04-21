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
    public class OptimizationController : ControllerBase
    {
        private readonly IDatabaseOptimizerService _optimizerService;
        private readonly IIssueRepository _issueRepository;
        private readonly ILogger<OptimizationController> _logger;

        public OptimizationController(
            IDatabaseOptimizerService optimizerService,
            IIssueRepository issueRepository,
            ILogger<OptimizationController> logger)
        {
            _optimizerService = optimizerService;
            _issueRepository = issueRepository;
            _logger = logger;
        }

        /// <summary>
        /// Generates optimization scripts for a database based on detected issues.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A list of generated optimization scripts.</returns>
        [HttpPost("{databaseName}/generate-scripts")]
        [ProducesResponseType(typeof(IEnumerable<OptimizationScript>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<OptimizationScript>>> GenerateOptimizationScripts(
            [FromQuery] string connectionString,
            string databaseName)
        {
            try
            {
                // Get all unresolved issues for the database
                var issues = await _issueRepository.GetIssuesAsync(connectionString, databaseName, null, null, false);

                // Generate optimization scripts
                var scripts = await _optimizerService.GenerateOptimizationScriptsAsync(connectionString, databaseName, issues);

                return Ok(scripts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating optimization scripts for database '{databaseName}'");
                return StatusCode(500, new { message = $"Error generating optimization scripts for database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a list of optimization scripts for a database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="type">Optional. The type of scripts to filter by.</param>
        /// <returns>A list of optimization scripts for the database.</returns>
        [HttpGet("{databaseName}/scripts")]
        [ProducesResponseType(typeof(IEnumerable<OptimizationScript>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<OptimizationScript>>> GetOptimizationScripts(
            [FromQuery] string connectionString,
            string databaseName,
            [FromQuery] ScriptType? type = null)
        {
            try
            {
                var scripts = await _optimizerService.GetScriptsAsync(connectionString, databaseName, type);
                return Ok(scripts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting optimization scripts for database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting optimization scripts for database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a specific optimization script by ID.
        /// </summary>
        /// <param name="id">The ID of the script to retrieve.</param>
        /// <returns>The optimization script with the specified ID.</returns>
        [HttpGet("scripts/{id}")]
        [ProducesResponseType(typeof(OptimizationScript), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<OptimizationScript>> GetOptimizationScript(Guid id)
        {
            try
            {
                var script = await _optimizerService.GetScriptByIdAsync(id);
                if (script == null)
                {
                    return NotFound(new { message = $"Optimization script with ID '{id}' not found." });
                }
                return Ok(script);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting optimization script with ID '{id}'");
                return StatusCode(500, new { message = $"Error getting optimization script with ID '{id}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Executes an optimization script on a database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="scriptId">The ID of the script to execute.</param>
        /// <returns>A result indicating success or failure.</returns>
        [HttpPost("{databaseName}/execute-script/{scriptId}")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> ExecuteOptimizationScript(
            [FromQuery] string connectionString,
            string databaseName,
            Guid scriptId)
        {
            try
            {
                var result = await _optimizerService.ExecuteOptimizationScriptAsync(connectionString, databaseName, scriptId);
                return Ok(new { message = "Script executed successfully.", result });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Optimization script with ID '{scriptId}' not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing optimization script with ID '{scriptId}' on database '{databaseName}'");
                return StatusCode(500, new { message = $"Error executing optimization script", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a recommended optimization script for a specific issue type.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="issueType">The type of issue to get a recommendation for.</param>
        /// <returns>A recommended optimization script for the issue type.</returns>
        [HttpGet("{databaseName}/recommended-script")]
        [ProducesResponseType(typeof(OptimizationScript), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<OptimizationScript>> GetRecommendedOptimization(
            [FromQuery] string connectionString,
            string databaseName,
            [FromQuery] IssueType issueType)
        {
            try
            {
                var script = await _optimizerService.GetRecommendedOptimizationAsync(connectionString, databaseName, issueType);
                if (script == null)
                {
                    return NotFound(new { message = $"No recommended optimization script found for issue type '{issueType}' in database '{databaseName}'." });
                }
                return Ok(script);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting recommended optimization script for issue type '{issueType}' in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting recommended optimization script", error = ex.Message });
            }
        }
    }
}
