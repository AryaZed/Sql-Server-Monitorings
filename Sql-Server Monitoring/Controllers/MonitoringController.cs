using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Sql_Server_Monitoring.Application.Hub;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "UserPolicy")]
    public class MonitoringController : ControllerBase
    {
        private readonly IDatabaseMonitorService _monitorService;
        private readonly ISettingsRepository _settingsRepository;
        private readonly IHubContext<MonitoringHub> _hubContext;
        private readonly ILogger<MonitoringController> _logger;
        private static CancellationTokenSource _monitoringCts;

        public MonitoringController(
            IDatabaseMonitorService monitorService,
            ISettingsRepository settingsRepository,
            IHubContext<MonitoringHub> hubContext,
            ILogger<MonitoringController> logger)
        {
            _monitorService = monitorService;
            _settingsRepository = settingsRepository;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// Checks database connectivity.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <returns>True if the database is reachable, otherwise false.</returns>
        [HttpGet("check-connectivity")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<object>> CheckConnectivity([FromQuery] string connectionString)
        {
            try
            {
                bool isConnected = await _monitorService.CheckDatabaseConnectivityAsync(connectionString);
                var serverName = isConnected ? ExtractServerName(connectionString) : "Unknown";
                
                return Ok(new { 
                    isConnected,
                    serverName,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking database connectivity");
                return StatusCode(500, new { message = "Error checking database connectivity", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the current monitoring settings.
        /// </summary>
        /// <returns>The current monitoring settings.</returns>
        [HttpGet("settings")]
        [ProducesResponseType(typeof(MonitoringSettings), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<MonitoringSettings>> GetMonitoringSettings()
        {
            try
            {
                var settings = await _settingsRepository.GetMonitoringSettingsAsync();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monitoring settings");
                return StatusCode(500, new { message = "Error getting monitoring settings", error = ex.Message });
            }
        }

        /// <summary>
        /// Updates the monitoring settings.
        /// </summary>
        /// <param name="settings">The new monitoring settings.</param>
        /// <returns>A result indicating success or failure.</returns>
        [HttpPut("settings")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> UpdateMonitoringSettings([FromBody] MonitoringSettings settings)
        {
            try
            {
                await _settingsRepository.SaveMonitoringSettingsAsync(settings);
                return Ok(new { message = "Monitoring settings updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating monitoring settings");
                return StatusCode(500, new { message = "Error updating monitoring settings", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the current server performance metrics.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <returns>The current server performance metrics.</returns>
        [HttpGet("metrics")]
        [ProducesResponseType(typeof(ServerPerformanceMetrics), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ServerPerformanceMetrics>> GetCurrentMetrics([FromQuery] string connectionString)
        {
            try
            {
                var metrics = await _monitorService.GetCurrentMetricsAsync(connectionString);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current metrics");
                return StatusCode(500, new { message = "Error getting current metrics", error = ex.Message });
            }
        }

        /// <summary>
        /// Starts monitoring a SQL Server.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <returns>A result indicating success or failure.</returns>
        [HttpPost("start")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> StartMonitoring([FromQuery] string connectionString)
        {
            try
            {
                // Cancel any existing monitoring
                _monitoringCts?.Cancel();
                _monitoringCts = new CancellationTokenSource();

                // Start new monitoring task
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Configure monitoring to push updates via SignalR
                        var serverName = ExtractServerName(connectionString);

                        // Start monitoring
                        await _monitorService.StartMonitoringAsync(connectionString, _monitoringCts.Token);

                        // Notify clients that monitoring has stopped
                        await _hubContext.Clients.Group(serverName).SendAsync("MonitoringStopped", serverName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in monitoring task");
                    }
                });

                // Update settings
                var settings = await _settingsRepository.GetMonitoringSettingsAsync();
                settings.MonitoringEnabled = true;
                await _settingsRepository.SaveMonitoringSettingsAsync(settings);

                return Ok(new { message = "Monitoring started successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting monitoring");
                return StatusCode(500, new { message = "Error starting monitoring", error = ex.Message });
            }
        }

        /// <summary>
        /// Stops monitoring a SQL Server.
        /// </summary>
        /// <returns>A result indicating success or failure.</returns>
        [HttpPost("stop")]
        [Authorize(Policy = "AdminPolicy")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> StopMonitoring()
        {
            try
            {
                // Cancel monitoring task
                _monitoringCts?.Cancel();
                await _monitorService.StopMonitoringAsync();

                // Update settings
                var settings = await _settingsRepository.GetMonitoringSettingsAsync();
                settings.MonitoringEnabled = false;
                await _settingsRepository.SaveMonitoringSettingsAsync(settings);

                return Ok(new { message = "Monitoring stopped successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping monitoring");
                return StatusCode(500, new { message = "Error stopping monitoring", error = ex.Message });
            }
        }

        /// <summary>
        /// Detects performance issues on a SQL Server.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <returns>A list of detected performance issues.</returns>
        [HttpGet("detect-issues")]
        [ProducesResponseType(typeof(IEnumerable<DbIssue>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<DbIssue>>> DetectPerformanceIssues([FromQuery] string connectionString)
        {
            try
            {
                var issues = await _monitorService.DetectPerformanceIssuesAsync(connectionString);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting performance issues");
                return StatusCode(500, new { message = "Error detecting performance issues", error = ex.Message });
            }
        }

        private string ExtractServerName(string connectionString)
        {
            // Simple parsing of server/data source from connection string
            // In a real implementation, use SqlConnectionStringBuilder
            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                var keyValue = part.Split('=');
                if (keyValue.Length == 2)
                {
                    var key = keyValue[0].Trim().ToLowerInvariant();
                    if (key == "server" || key == "data source")
                    {
                        return keyValue[1].Trim();
                    }
                }
            }
            return "unknown";
        }
    }
}
