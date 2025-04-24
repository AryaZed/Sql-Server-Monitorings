using Microsoft.AspNetCore.Mvc;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IdentityColumnController : ControllerBase
    {
        private readonly IIdentityColumnService _identityColumnService;
        private readonly ILogger<IdentityColumnController> _logger;

        public IdentityColumnController(
            IIdentityColumnService identityColumnService,
            ILogger<IdentityColumnController> logger)
        {
            _identityColumnService = identityColumnService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetIdentityColumns(
            [FromQuery] string connectionString,
            [FromQuery] string databaseName)
        {
            try
            {
                var identityColumns = await _identityColumnService.GetIdentityColumnsAsync(connectionString, databaseName);
                return Ok(identityColumns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving identity columns for database '{databaseName}'");
                return StatusCode(500, $"Error retrieving identity columns for database '{databaseName}'");
            }
        }

        [HttpGet("analyze")]
        public async Task<IActionResult> AnalyzeIdentityColumns(
            [FromQuery] string connectionString,
            [FromQuery] string databaseName)
        {
            try
            {
                var issues = await _identityColumnService.AnalyzeIdentityColumnsAsync(connectionString, databaseName);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing identity columns for database '{databaseName}'");
                return StatusCode(500, $"Error analyzing identity columns for database '{databaseName}'");
            }
        }

        [HttpPost("reseed")]
        public async Task<IActionResult> ReseedIdentityColumn(
            [FromQuery] string connectionString,
            [FromQuery] string databaseName,
            [FromQuery] string schemaName,
            [FromQuery] string tableName,
            [FromQuery] string columnName,
            [FromQuery] long newSeedValue)
        {
            try
            {
                var result = await _identityColumnService.ReseedIdentityColumnAsync(
                    connectionString,
                    databaseName,
                    schemaName,
                    tableName,
                    columnName,
                    newSeedValue);

                if (result)
                {
                    return Ok(new
                    {
                        Success = true,
                        Message = $"Successfully reseeded identity column '{schemaName}.{tableName}.{columnName}' to {newSeedValue}"
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        Success = false,
                        Message = $"Failed to reseed identity column '{schemaName}.{tableName}.{columnName}'"
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error reseeding identity column '{schemaName}.{tableName}.{columnName}'");
                return StatusCode(500, $"Error reseeding identity column '{schemaName}.{tableName}.{columnName}'");
            }
        }
    }
} 