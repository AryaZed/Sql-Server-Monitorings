using Microsoft.AspNetCore.Mvc;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DbccCheckController : ControllerBase
    {
        private readonly IDbccCheckService _dbccCheckService;
        private readonly ILogger<DbccCheckController> _logger;

        public DbccCheckController(
            IDbccCheckService dbccCheckService,
            ILogger<DbccCheckController> logger)
        {
            _dbccCheckService = dbccCheckService;
            _logger = logger;
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetDbccCheckHistory([FromQuery] string connectionString)
        {
            try
            {
                var history = await _dbccCheckService.GetDbccCheckHistoryAsync(connectionString);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving DBCC check history");
                return StatusCode(500, "Error retrieving DBCC check history");
            }
        }

        [HttpGet("analyze")]
        public async Task<IActionResult> AnalyzeDbccChecks([FromQuery] string connectionString)
        {
            try
            {
                var issues = await _dbccCheckService.AnalyzeDbccChecksAsync(connectionString);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing DBCC checks");
                return StatusCode(500, "Error analyzing DBCC checks");
            }
        }

        [HttpPost("run")]
        public async Task<IActionResult> RunDbccCheck(
            [FromQuery] string connectionString,
            [FromQuery] string databaseName)
        {
            try
            {
                var result = await _dbccCheckService.RunDbccCheckAsync(connectionString, databaseName);
                if (result)
                {
                    return Ok(new { Success = true, Message = $"DBCC CHECKDB completed successfully for database '{databaseName}'" });
                }
                else
                {
                    return BadRequest(new { Success = false, Message = $"DBCC CHECKDB failed for database '{databaseName}'. Check logs for details." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error running DBCC CHECKDB for database '{databaseName}'");
                return StatusCode(500, $"Error running DBCC CHECKDB for database '{databaseName}'");
            }
        }
    }
} 