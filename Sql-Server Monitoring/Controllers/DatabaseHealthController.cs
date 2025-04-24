using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;
using System.Dynamic;
using System.Text.RegularExpressions;

namespace Sql_Server_Monitoring.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "UserPolicy")]
    public class DatabaseHealthController : ControllerBase
    {
        private readonly IDatabaseMonitorService _monitorService;
        private readonly IDatabaseRepository _databaseRepository;
        private readonly ISettingsRepository _settingsRepository;
        private readonly ILogger<DatabaseHealthController> _logger;

        public DatabaseHealthController(
            IDatabaseMonitorService monitorService,
            IDatabaseRepository databaseRepository,
            ISettingsRepository settingsRepository,
            ILogger<DatabaseHealthController> logger)
        {
            _monitorService = monitorService;
            _databaseRepository = databaseRepository;
            _settingsRepository = settingsRepository;
            _logger = logger;
        }

        /// <summary>
        /// Performs a comprehensive health check on the database server
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server</param>
        /// <returns>A detailed health report</returns>
        [HttpGet("comprehensive-check")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<object>> GetComprehensiveHealthCheck([FromQuery] string connectionString)
        {
            try
            {
                var healthReport = new Dictionary<string, object>();
                
                // Basic connectivity check
                bool isConnected = await _monitorService.CheckDatabaseConnectivityAsync(connectionString);
                var connectivityStatus = isConnected ? "Healthy" : "Unhealthy";
                
                healthReport.Add("connectivity", new
                {
                    status = connectivityStatus,
                    serverName = ExtractServerName(connectionString),
                    timestamp = DateTime.UtcNow
                });
                
                if (!isConnected)
                {
                    return Ok(healthReport);
                }
                
                // Get current server metrics
                var metrics = await _monitorService.GetCurrentMetricsAsync(connectionString);
                
                // CPU health
                var cpuStatus = metrics.Cpu.UtilizationPercent > 80 ? "Critical" : 
                                metrics.Cpu.UtilizationPercent > 60 ? "Warning" : "Healthy";
                                
                healthReport.Add("cpu", new
                {
                    status = cpuStatus,
                    utilizationPercent = metrics.Cpu.UtilizationPercent
                });
                
                // Memory health
                var memoryStatus = metrics.Memory.PageLifeExpectancy < 300 ? "Critical" :
                                  metrics.Memory.PageLifeExpectancy < 600 ? "Warning" : "Healthy";
                                  
                healthReport.Add("memory", new
                {
                    status = memoryStatus,
                    pageLifeExpectancy = metrics.Memory.PageLifeExpectancy
                });
                
                // Disk I/O health
                var diskIoStatuses = metrics.Disk.IoStats.Select(stat => new
                {
                    databaseName = stat.DatabaseName,
                    fileName = stat.FileName,
                    status = stat.ReadLatencyMs > 20 || stat.WriteLatencyMs > 20 ? "Warning" :
                             stat.ReadLatencyMs > 50 || stat.WriteLatencyMs > 50 ? "Critical" : "Healthy",
                    readLatencyMs = stat.ReadLatencyMs,
                    writeLatencyMs = stat.WriteLatencyMs
                }).ToList();
                
                var diskIoStatus = diskIoStatuses.Any(s => s.status == "Critical") ? "Critical" :
                             diskIoStatuses.Any(s => s.status == "Warning") ? "Warning" : "Healthy";
                             
                healthReport.Add("diskIo", new
                {
                    status = diskIoStatus,
                    details = diskIoStatuses
                });
                
                // Check for blocking and deadlocks
                var issues = await _monitorService.DetectPerformanceIssuesAsync(connectionString);
                var blockingIssues = issues.Where(i => i.Message.Contains("blocking") || i.Message.Contains("deadlock")).ToList();
                
                var blockingStatus = blockingIssues.Any(i => i.Severity == IssueSeverity.High) ? "Critical" :
                             blockingIssues.Any() ? "Warning" : "Healthy";
                             
                healthReport.Add("blocking", new
                {
                    status = blockingStatus,
                    count = blockingIssues.Count,
                    details = blockingIssues.Select(i => new
                    {
                        message = i.Message,
                        severity = i.Severity.ToString(),
                        detectionTime = i.DetectionTime
                    })
                });
                
                // Overall health rating
                var statusValues = new List<string>
                {
                    connectivityStatus,
                    cpuStatus, 
                    memoryStatus,
                    diskIoStatus,
                    blockingStatus
                };
                
                string overallStatus = statusValues.Any(s => s == "Critical") ? "Critical" :
                                     statusValues.Any(s => s == "Warning") ? "Warning" : "Healthy";
                                     
                healthReport.Add("overall", new
                {
                    status = overallStatus,
                    timestamp = DateTime.UtcNow,
                    message = GetHealthSummaryMessage(overallStatus)
                });
                
                return Ok(healthReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing comprehensive health check");
                return StatusCode(500, new { message = "Error performing health check", error = ex.Message });
            }
        }
        
        /// <summary>
        /// Gets a quick health status for a database server
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server</param>
        /// <returns>Quick health status info</returns>
        [HttpGet("quick-check")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<object>> GetQuickHealthCheck([FromQuery] string connectionString)
        {
            try
            {
                // Basic connectivity check
                bool isConnected = await _monitorService.CheckDatabaseConnectivityAsync(connectionString);
                
                if (!isConnected)
                {
                    return Ok(new
                    {
                        status = "Unhealthy",
                        message = "Cannot connect to database server",
                        serverName = ExtractServerName(connectionString),
                        timestamp = DateTime.UtcNow
                    });
                }
                
                // Get current server metrics for basic check
                var metrics = await _monitorService.GetCurrentMetricsAsync(connectionString);
                
                // Determine overall status
                string status = "Healthy";
                string message = "All systems operational";
                
                if (metrics.Cpu.UtilizationPercent > 80 || 
                    metrics.Memory.PageLifeExpectancy < 300 ||
                    metrics.Disk.IoStats.Any(io => io.ReadLatencyMs > 50 || io.WriteLatencyMs > 50))
                {
                    status = "Critical";
                    message = "Critical issues detected with server performance";
                }
                else if (metrics.Cpu.UtilizationPercent > 60 || 
                        metrics.Memory.PageLifeExpectancy < 600 ||
                        metrics.Disk.IoStats.Any(io => io.ReadLatencyMs > 20 || io.WriteLatencyMs > 20))
                {
                    status = "Warning";
                    message = "Performance warnings detected";
                }
                
                return Ok(new
                {
                    status,
                    message,
                    serverName = ExtractServerName(connectionString),
                    cpuUtilization = metrics.Cpu.UtilizationPercent,
                    memoryPLE = metrics.Memory.PageLifeExpectancy,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing quick health check");
                return StatusCode(500, new { message = "Error performing health check", error = ex.Message });
            }
        }
        
        private string ExtractServerName(string connectionString)
        {
            try
            {
                // Parse connection string to extract server name using regex
                var serverPattern = @"(?:Server|Data Source)\s*=\s*([^;]+)";
                var match = Regex.Match(connectionString, serverPattern, RegexOptions.IgnoreCase);
                
                if (match.Success && match.Groups.Count > 1)
                {
                    return match.Groups[1].Value.Trim();
                }
                
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }
        
        private string TruncateQuery(string queryText)
        {
            if (string.IsNullOrEmpty(queryText))
                return string.Empty;
                
            if (queryText.Length <= 100)
                return queryText;
                
            return queryText.Substring(0, 97) + "...";
        }
        
        private string GetHealthSummaryMessage(string status)
        {
            switch (status)
            {
                case "Critical":
                    return "Critical issues detected that require immediate attention";
                case "Warning":
                    return "Performance warnings detected that should be investigated";
                case "Healthy":
                    return "All database systems are operating normally";
                default:
                    return "Unable to determine database health status";
            }
        }
    }
} 