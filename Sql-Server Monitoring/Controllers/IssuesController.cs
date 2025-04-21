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
    public class IssuesController : ControllerBase
    {
        private readonly IIssueRepository _issueRepository;
        private readonly ILogger<IssuesController> _logger;

        public IssuesController(
            IIssueRepository issueRepository,
            ILogger<IssuesController> logger)
        {
            _issueRepository = issueRepository;
            _logger = logger;
        }

        /// <summary>
        /// Gets a list of issues based on specified filters.
        /// </summary>
        /// <param name="connectionString">Optional. The connection string to filter issues by server.</param>
        /// <param name="databaseName">Optional. The database name to filter issues by database.</param>
        /// <param name="type">Optional. The issue type to filter by.</param>
        /// <param name="minSeverity">Optional. The minimum severity level to include.</param>
        /// <param name="includeResolved">Optional. Whether to include resolved issues. Default is false.</param>
        /// <returns>A list of issues matching the specified filters.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<DbIssue>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<DbIssue>>> GetIssues(
            [FromQuery] string connectionString = null,
            [FromQuery] string databaseName = null,
            [FromQuery] IssueType? type = null,
            [FromQuery] IssueSeverity? minSeverity = null,
            [FromQuery] bool includeResolved = false)
        {
            try
            {
                var issues = await _issueRepository.GetIssuesAsync(connectionString, databaseName, type, minSeverity, includeResolved);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting issues");
                return StatusCode(500, new { message = "Error getting issues", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets a specific issue by ID.
        /// </summary>
        /// <param name="id">The ID of the issue to retrieve.</param>
        /// <returns>The issue with the specified ID.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DbIssue), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<DbIssue>> GetIssue(Guid id)
        {
            try
            {
                var issue = await _issueRepository.GetIssueByIdAsync(id);
                if (issue == null)
                {
                    return NotFound(new { message = $"Issue with ID '{id}' not found." });
                }
                return Ok(issue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting issue with ID '{id}'");
                return StatusCode(500, new { message = $"Error getting issue with ID '{id}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Marks an issue as resolved.
        /// </summary>
        /// <param name="id">The ID of the issue to mark as resolved.</param>
        /// <returns>A result indicating success or failure.</returns>
        [HttpPut("{id}/resolve")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> ResolveIssue(Guid id)
        {
            try
            {
                await _issueRepository.MarkIssueAsResolvedAsync(id);
                return Ok(new { message = $"Issue with ID '{id}' marked as resolved." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Issue with ID '{id}' not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error resolving issue with ID '{id}'");
                return StatusCode(500, new { message = $"Error resolving issue with ID '{id}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Deletes an issue.
        /// </summary>
        /// <param name="id">The ID of the issue to delete.</param>
        /// <returns>A result indicating success or failure.</returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> DeleteIssue(Guid id)
        {
            try
            {
                await _issueRepository.DeleteIssueAsync(id);
                return Ok(new { message = $"Issue with ID '{id}' deleted." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Issue with ID '{id}' not found." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting issue with ID '{id}'");
                return StatusCode(500, new { message = $"Error deleting issue with ID '{id}'", error = ex.Message });
            }
        }
    }
}
