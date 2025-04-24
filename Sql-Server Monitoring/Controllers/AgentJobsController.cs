using Microsoft.AspNetCore.Mvc;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AgentJobsController : ControllerBase
    {
        private readonly IAgentJobsService _agentJobsService;
        private readonly ILogger<AgentJobsController> _logger;

        public AgentJobsController(
            IAgentJobsService agentJobsService,
            ILogger<AgentJobsController> logger)
        {
            _agentJobsService = agentJobsService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllJobs([FromQuery] string connectionString)
        {
            try
            {
                var jobs = await _agentJobsService.GetAllJobsAsync(connectionString);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SQL Agent jobs");
                return StatusCode(500, "Error retrieving SQL Agent jobs");
            }
        }

        [HttpGet("failed")]
        public async Task<IActionResult> GetFailedJobs(
            [FromQuery] string connectionString,
            [FromQuery] int lookbackHours = 24)
        {
            try
            {
                var jobs = await _agentJobsService.GetFailedJobsAsync(connectionString, lookbackHours);
                return Ok(jobs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving failed SQL Agent jobs for past {lookbackHours} hours");
                return StatusCode(500, "Error retrieving failed SQL Agent jobs");
            }
        }

        [HttpGet("{jobName}")]
        public async Task<IActionResult> GetJobByName(
            [FromQuery] string connectionString,
            [FromRoute] string jobName)
        {
            try
            {
                var job = await _agentJobsService.GetJobDetailsByNameAsync(connectionString, jobName);
                
                if (job == null)
                {
                    return NotFound($"SQL Agent job '{jobName}' not found");
                }
                
                return Ok(job);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving SQL Agent job details for '{jobName}'");
                return StatusCode(500, "Error retrieving SQL Agent job details");
            }
        }

        [HttpGet("analyze")]
        public async Task<IActionResult> AnalyzeJobs(
            [FromQuery] string connectionString,
            [FromQuery] string databaseName = null)
        {
            try
            {
                var issues = await _agentJobsService.AnalyzeJobsAsync(connectionString, databaseName);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing SQL Agent jobs");
                return StatusCode(500, "Error analyzing SQL Agent jobs");
            }
        }

        [HttpGet("timeline")]
        public async Task<IActionResult> GetJobTimeline(
            [FromQuery] string connectionString,
            [FromQuery] int days = 7)
        {
            try
            {
                var jobs = await _agentJobsService.GetAllJobsAsync(connectionString);
                var startDate = DateTime.Now.AddDays(-days);

                // Create timeline data
                var timelineData = jobs
                    .SelectMany(job => job.History
                        .Where(h => h.RunDate >= startDate)
                        .Select(h => new
                        {
                            JobName = job.JobName,
                            RunDate = h.RunDate,
                            Duration = h.Duration,
                            Outcome = h.Outcome,
                            Message = h.Message
                        }))
                    .OrderBy(t => t.RunDate)
                    .ToList();

                return Ok(timelineData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SQL Agent job timeline");
                return StatusCode(500, "Error retrieving SQL Agent job timeline");
            }
        }
    }
} 