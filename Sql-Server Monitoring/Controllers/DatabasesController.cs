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
    public class DatabasesController : ControllerBase
    {
        private readonly IDatabaseRepository _databaseRepository;
        private readonly IDatabaseAnalyzerService _analyzerService;
        private readonly ILogger<DatabasesController> _logger;

        public DatabasesController(
            IDatabaseRepository databaseRepository,
            IDatabaseAnalyzerService analyzerService,
            ILogger<DatabasesController> logger)
        {
            _databaseRepository = databaseRepository;
            _analyzerService = analyzerService;
            _logger = logger;
        }

        /// <summary>
        /// Gets a list of all user databases on the server.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <returns>A list of database names.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<string>>> GetDatabases([FromQuery] string connectionString)
        {
            try
            {
                var databases = await _databaseRepository.GetUserDatabasesAsync(connectionString);
                return Ok(databases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting databases");
                return StatusCode(500, new { message = "Error getting databases", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets detailed information about a specific database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>Detailed information about the database.</returns>
        [HttpGet("{databaseName}")]
        [ProducesResponseType(typeof(Database), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<Database>> GetDatabase([FromQuery] string connectionString, string databaseName)
        {
            try
            {
                var database = await _databaseRepository.GetDatabaseDetailsAsync(connectionString, databaseName);
                if (database == null)
                {
                    return NotFound(new { message = $"Database '{databaseName}' not found." });
                }
                return Ok(database);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a list of tables in a specific database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A list of tables in the database.</returns>
        [HttpGet("{databaseName}/tables")]
        [ProducesResponseType(typeof(IEnumerable<Table>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<Table>>> GetTables([FromQuery] string connectionString, string databaseName)
        {
            try
            {
                var tables = await _databaseRepository.GetTablesAsync(connectionString, databaseName);
                return Ok(tables);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting tables for database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting tables for database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Analyzes a database for issues.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database to analyze.</param>
        /// <returns>A list of detected issues in the database.</returns>
        [HttpPost("{databaseName}/analyze")]
        [ProducesResponseType(typeof(IEnumerable<DbIssue>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<DbIssue>>> AnalyzeDatabase([FromQuery] string connectionString, string databaseName)
        {
            try
            {
                var issues = await _analyzerService.AnalyzeDatabaseAsync(connectionString, databaseName);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing database '{databaseName}'");
                return StatusCode(500, new { message = $"Error analyzing database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Executes a script on a specific database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="script">The SQL script to execute.</param>
        /// <returns>A result indicating success or failure.</returns>
        [HttpPost("{databaseName}/execute")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> ExecuteScript(
            [FromQuery] string connectionString,
            string databaseName,
            [FromBody] ScriptExecutionRequest script)
        {
            try
            {
                await _databaseRepository.ExecuteScriptAsync(connectionString, databaseName, script.Script);
                return Ok(new { message = "Script executed successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing script on database '{databaseName}'");
                return StatusCode(500, new { message = $"Error executing script on database '{databaseName}'", error = ex.Message });
            }
        }
    }

    public class ScriptExecutionRequest
    {
        public string Script { get; set; }
    }
}
