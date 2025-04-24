using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;
using System.Text;

namespace Sql_Server_Monitoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "UserPolicy")]
    public class DatabaseComparisonController : ControllerBase
    {
        private readonly IDatabaseRepository _databaseRepository;
        private readonly ILogger<DatabaseComparisonController> _logger;

        public DatabaseComparisonController(
            IDatabaseRepository databaseRepository,
            ILogger<DatabaseComparisonController> logger)
        {
            _databaseRepository = databaseRepository;
            _logger = logger;
        }
        
        /// <summary>
        /// Compares the configuration of two databases and identifies differences
        /// </summary>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <param name="sourceDatabase">Source database name for comparison</param>
        /// <param name="targetDatabase">Target database name for comparison</param>
        /// <returns>List of configuration differences between the databases</returns>
        [HttpGet("compare")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<object>> CompareDatabases(
            [FromQuery] string connectionString,
            [FromQuery] string sourceDatabase,
            [FromQuery] string targetDatabase)
        {
            try
            {
                // Get details of both databases
                var sourceDb = await _databaseRepository.GetDatabaseDetailsAsync(connectionString, sourceDatabase);
                var targetDb = await _databaseRepository.GetDatabaseDetailsAsync(connectionString, targetDatabase);
                
                if (sourceDb == null)
                {
                    return NotFound(new { message = $"Source database '{sourceDatabase}' not found" });
                }
                
                if (targetDb == null)
                {
                    return NotFound(new { message = $"Target database '{targetDatabase}' not found" });
                }
                
                // Compare database settings
                var differences = new List<object>();
                
                // Compare recovery models
                if (sourceDb.RecoveryModel != targetDb.RecoveryModel)
                {
                    differences.Add(new
                    {
                        settingType = "RecoveryModel",
                        sourceValue = sourceDb.RecoveryModel.ToString(),
                        targetValue = targetDb.RecoveryModel.ToString(),
                        impact = "Different recovery models may affect backup and restoration strategies"
                    });
                }
                
                // Compare compatibility levels
                if (sourceDb.CompatibilityLevel != targetDb.CompatibilityLevel)
                {
                    differences.Add(new
                    {
                        settingType = "CompatibilityLevel",
                        sourceValue = sourceDb.CompatibilityLevel.ToString(),
                        targetValue = targetDb.CompatibilityLevel.ToString(),
                        impact = "Different compatibility levels may affect query behavior and available features"
                    });
                }
                
                // Compare collation settings
                if (!string.Equals(sourceDb.Collation, targetDb.Collation, StringComparison.OrdinalIgnoreCase))
                {
                    differences.Add(new
                    {
                        settingType = "Collation",
                        sourceValue = sourceDb.Collation,
                        targetValue = targetDb.Collation,
                        impact = "Different collation settings may affect string comparisons and sorting"
                    });
                }
                
                // Compare other properties as needed
                if (sourceDb.IsMonitored != targetDb.IsMonitored)
                {
                    differences.Add(new
                    {
                        settingType = "MonitoringState",
                        sourceValue = sourceDb.IsMonitored.ToString(),
                        targetValue = targetDb.IsMonitored.ToString(),
                        impact = "One database is being monitored while the other is not"
                    });
                }
                
                // Compare owners
                if (!string.Equals(sourceDb.Owner, targetDb.Owner, StringComparison.OrdinalIgnoreCase))
                {
                    differences.Add(new
                    {
                        settingType = "Owner",
                        sourceValue = sourceDb.Owner,
                        targetValue = targetDb.Owner,
                        impact = "Different database owners may affect permissions and access"
                    });
                }
                
                return Ok(new
                {
                    sourceDatabase = sourceDb.Name,
                    targetDatabase = targetDb.Name,
                    differences = differences,
                    totalDifferences = differences.Count,
                    hasMajorDifferences = differences.Count > 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing databases {SourceDb} and {TargetDb}", 
                    sourceDatabase, targetDatabase);
                return StatusCode(500, new { message = "Error comparing databases", error = ex.Message });
            }
        }
        
        /// <summary>
        /// Generates scripts to synchronize a target database to match the source database configuration
        /// </summary>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <param name="sourceDatabase">Source database name</param>
        /// <param name="targetDatabase">Target database name</param>
        /// <returns>SQL scripts to align configurations</returns>
        [HttpGet("sync-scripts")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<object>> GenerateSyncScripts(
            [FromQuery] string connectionString,
            [FromQuery] string sourceDatabase,
            [FromQuery] string targetDatabase)
        {
            try
            {
                // Get details of both databases
                var sourceDb = await _databaseRepository.GetDatabaseDetailsAsync(connectionString, sourceDatabase);
                var targetDb = await _databaseRepository.GetDatabaseDetailsAsync(connectionString, targetDatabase);
                
                if (sourceDb == null)
                {
                    return NotFound(new { message = $"Source database '{sourceDatabase}' not found" });
                }
                
                if (targetDb == null)
                {
                    return NotFound(new { message = $"Target database '{targetDatabase}' not found" });
                }
                
                var scripts = new List<object>();
                
                // Recovery model sync script
                if (sourceDb.RecoveryModel != targetDb.RecoveryModel)
                {
                    scripts.Add(new
                    {
                        settingType = "RecoveryModel",
                        description = $"Change recovery model of {targetDb.Name} to match {sourceDb.Name}",
                        script = $"ALTER DATABASE [{targetDb.Name}] SET RECOVERY {sourceDb.RecoveryModel};"
                    });
                }
                
                // Compatibility level sync script
                if (sourceDb.CompatibilityLevel != targetDb.CompatibilityLevel && sourceDb.CompatibilityLevel.HasValue)
                {
                    scripts.Add(new
                    {
                        settingType = "CompatibilityLevel",
                        description = $"Change compatibility level of {targetDb.Name} to match {sourceDb.Name}",
                        script = $"ALTER DATABASE [{targetDb.Name}] SET COMPATIBILITY_LEVEL = {sourceDb.CompatibilityLevel};"
                    });
                }
                
                // Collation script (if different)
                if (!string.Equals(sourceDb.Collation, targetDb.Collation, StringComparison.OrdinalIgnoreCase) && 
                    !string.IsNullOrEmpty(sourceDb.Collation))
                {
                    scripts.Add(new
                    {
                        settingType = "Collation",
                        description = $"Change database collation of {targetDb.Name} to match {sourceDb.Name}",
                        script = $"ALTER DATABASE [{targetDb.Name}] COLLATE {sourceDb.Collation};"
                    });
                }
                
                return Ok(new
                {
                    sourceDatabase = sourceDb.Name,
                    targetDatabase = targetDb.Name,
                    syncScripts = scripts,
                    scriptCount = scripts.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating sync scripts for databases {SourceDb} and {TargetDb}", 
                    sourceDatabase, targetDatabase);
                return StatusCode(500, new { message = "Error generating sync scripts", error = ex.Message });
            }
        }
    }
} 