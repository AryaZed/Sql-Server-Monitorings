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
    public class BackupController : ControllerBase
    {
        private readonly IBackupService _backupService;
        private readonly ILogger<BackupController> _logger;

        public BackupController(
            IBackupService backupService,
            ILogger<BackupController> logger)
        {
            _backupService = backupService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the backup history for a database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A list of backup history records.</returns>
        [HttpGet("{databaseName}/history")]
        [ProducesResponseType(typeof(IEnumerable<BackupHistory>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<BackupHistory>>> GetBackupHistory(
            [FromQuery] string connectionString,
            string databaseName)
        {
            try
            {
                var history = await _backupService.GetBackupHistoryAsync(connectionString, databaseName);
                return Ok(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting backup history for database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting backup history for database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Performs a backup of a database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="backupType">The type of backup to perform.</param>
        /// <param name="backupPath">The path to store the backup file.</param>
        /// <returns>A result indicating success or failure.</returns>
        [HttpPost("{databaseName}/backup")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> PerformBackup(
            [FromQuery] string connectionString,
            string databaseName,
            [FromQuery] BackupType backupType,
            [FromQuery] string backupPath)
        {
            try
            {
                var result = await _backupService.PerformBackupAsync(connectionString, databaseName, backupType, backupPath);
                return Ok(new { message = "Backup completed successfully", filePath = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error performing {backupType} backup for database '{databaseName}'");
                return StatusCode(500, new { message = $"Error performing backup for database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Restores a database from a backup file.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database to restore.</param>
        /// <param name="backupFilePath">The path to the backup file.</param>
        /// <param name="newDatabaseName">Optional. The name of the new database to restore to.</param>
        /// <returns>A result indicating success or failure.</returns>
        [HttpPost("{databaseName}/restore")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> RestoreDatabase(
            [FromQuery] string connectionString,
            string databaseName,
            [FromQuery] string backupFilePath,
            [FromQuery] string newDatabaseName = null)
        {
            try
            {
                var result = await _backupService.PerformRestoreAsync(connectionString, databaseName, backupFilePath, newDatabaseName);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error restoring database '{databaseName}' from backup file {backupFilePath}");
                return StatusCode(500, new { message = $"Error restoring database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Analyzes the backup strategy for a database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A list of issues with the backup strategy.</returns>
        [HttpGet("{databaseName}/analyze")]
        [ProducesResponseType(typeof(IEnumerable<DbIssue>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<DbIssue>>> AnalyzeBackupStrategy(
            [FromQuery] string connectionString,
            string databaseName)
        {
            try
            {
                var issues = await _backupService.AnalyzeBackupStrategyAsync(connectionString, databaseName);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing backup strategy for database '{databaseName}'");
                return StatusCode(500, new { message = $"Error analyzing backup strategy for database '{databaseName}'", error = ex.Message });
            }
        }
    }
} 