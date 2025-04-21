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
    public class SecurityController : ControllerBase
    {
        private readonly ISecurityAuditService _securityAuditService;
        private readonly ILogger<SecurityController> _logger;

        public SecurityController(
            ISecurityAuditService securityAuditService,
            ILogger<SecurityController> logger)
        {
            _securityAuditService = securityAuditService;
            _logger = logger;
        }

        /// <summary>
        /// Audits the permissions in a database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A list of security issues related to permissions.</returns>
        [HttpGet("{databaseName}/audit-permissions")]
        [ProducesResponseType(typeof(IEnumerable<DbIssue>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<DbIssue>>> AuditPermissions(
            [FromQuery] string connectionString,
            string databaseName)
        {
            try
            {
                var issues = await _securityAuditService.AuditPermissionsAsync(connectionString, databaseName);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error auditing permissions for database '{databaseName}'");
                return StatusCode(500, new { message = $"Error auditing permissions for database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the user permissions in a database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="userName">Optional. The name of the user to filter by.</param>
        /// <returns>A list of user permissions.</returns>
        [HttpGet("{databaseName}/permissions")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(IEnumerable<UserPermission>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<UserPermission>>> GetUserPermissions(
            [FromQuery] string connectionString,
            string databaseName,
            [FromQuery] string userName = null)
        {
            try
            {
                var permissions = await _securityAuditService.GetUserPermissionsAsync(connectionString, databaseName, userName);
                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user permissions for database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting user permissions for database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Audits sensitive data in a database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A list of security issues related to sensitive data.</returns>
        [HttpGet("{databaseName}/audit-sensitive-data")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(IEnumerable<DbIssue>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<DbIssue>>> AuditSensitiveData(
            [FromQuery] string connectionString,
            string databaseName)
        {
            try
            {
                var issues = await _securityAuditService.AuditSensitiveDataAsync(connectionString, databaseName);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error auditing sensitive data for database '{databaseName}'");
                return StatusCode(500, new { message = $"Error auditing sensitive data for database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Identifies sensitive columns in a database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <returns>A list of sensitive columns.</returns>
        [HttpGet("{databaseName}/sensitive-columns")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(IEnumerable<SensitiveColumn>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<SensitiveColumn>>> GetSensitiveColumns(
            [FromQuery] string connectionString,
            string databaseName)
        {
            try
            {
                var columns = await _securityAuditService.IdentifySensitiveColumnsAsync(connectionString, databaseName);
                return Ok(columns);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error identifying sensitive columns for database '{databaseName}'");
                return StatusCode(500, new { message = $"Error identifying sensitive columns for database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Audits login security at the server level.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <returns>A list of security issues related to logins.</returns>
        [HttpGet("audit-logins")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(IEnumerable<DbIssue>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<DbIssue>>> AuditLoginSecurity(
            [FromQuery] string connectionString)
        {
            try
            {
                var issues = await _securityAuditService.AuditLoginSecurityAsync(connectionString);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error auditing login security");
                return StatusCode(500, new { message = "Error auditing login security", error = ex.Message });
            }
        }
    }
} 