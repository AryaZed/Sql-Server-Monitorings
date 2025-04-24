using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Sql_Server_Monitoring.Application.Hub;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;
using System.Text.RegularExpressions;

namespace Sql_Server_Monitoring.Application.BackgroundService
{
    public class MonitoringBackgroundService : IHostedService, IDisposable
    {
        private readonly IDatabaseMonitorService _monitorService;
        private readonly ISettingsRepository _settingsRepository;
        private readonly IIssueRepository _issueRepository;
        private readonly IHubContext<MonitoringHub> _hubContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MonitoringBackgroundService> _logger;
        private Task _monitoringTask;
        private CancellationTokenSource _cancellationTokenSource;
        private Timer _connectivityCheckTimer;
        private string _currentConnectionString;

        public MonitoringBackgroundService(
            IDatabaseMonitorService monitorService,
            ISettingsRepository settingsRepository,
            IIssueRepository issueRepository,
            IHubContext<MonitoringHub> hubContext,
            IConfiguration configuration,
            ILogger<MonitoringBackgroundService> logger)
        {
            _monitorService = monitorService;
            _settingsRepository = settingsRepository;
            _issueRepository = issueRepository;
            _hubContext = hubContext;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Monitoring background service is starting.");

            try
            {
                // Get default connection string
                _currentConnectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(_currentConnectionString))
                {
                    _logger.LogWarning("Default connection string not found. Monitoring service will not start automatically.");
                    return;
                }

                // Verify connectivity before starting monitoring
                if (!await _monitorService.CheckDatabaseConnectivityAsync(_currentConnectionString))
                {
                    _logger.LogError("Cannot establish database connection. Monitoring service will not start automatically.");
                    
                    var issue = new DbIssue
                    {
                        Type = IssueType.Connectivity,
                        Severity = IssueSeverity.Critical,
                        Message = "Database connectivity check failed during service startup. Monitoring not started.",
                        RecommendedAction = "Check SQL Server instance, network connectivity, and credentials.",
                        DetectionTime = DateTime.Now
                    };
                    
                    await _issueRepository.AddIssueAsync(issue);
                    return;
                }

                // Get monitoring settings
                var settings = await _settingsRepository.GetMonitoringSettingsAsync();
                if (!settings.MonitoringEnabled)
                {
                    _logger.LogInformation("Monitoring is disabled in settings. Service will not start automatically.");
                    return;
                }

                // Create new cancellation token source
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // Start monitoring in a separate task
                _monitoringTask = _monitorService.StartMonitoringAsync(_currentConnectionString, _cancellationTokenSource.Token);

                // Set up a timer to periodically check connectivity
                _connectivityCheckTimer = new Timer(
                    async _ => await CheckConnectivityAsync(_currentConnectionString),
                    null,
                    TimeSpan.FromMinutes(1), // Start after 1 minute 
                    TimeSpan.FromMinutes(1)  // Then check every minute
                );

                _logger.LogInformation("Monitoring background service started successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting monitoring background service.");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Monitoring background service is stopping.");

            try
            {
                // Stop connectivity check timer
                _connectivityCheckTimer?.Change(Timeout.Infinite, 0);
                
                // Cancel the monitoring task
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Cancel();
                    _cancellationTokenSource.Dispose();
                }

                // Wait for the monitoring task to complete
                if (_monitoringTask != null)
                {
                    await _monitorService.StopMonitoringAsync();

                    // Wait for the task to complete with a timeout
                    await Task.WhenAny(_monitoringTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
                }

                _logger.LogInformation("Monitoring background service stopped successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping monitoring background service.");
            }
        }
        
        private async Task CheckConnectivityAsync(string connectionString)
        {
            try
            {
                bool isConnected = await _monitorService.CheckDatabaseConnectivityAsync(connectionString);
                
                // Broadcast connectivity status to all clients
                string serverName = ExtractServerName(connectionString);
                await _hubContext.Clients.Group(serverName).SendAsync(
                    "ConnectivityStatus", 
                    new { 
                        serverName,
                        isConnected,
                        timestamp = DateTime.UtcNow
                    });
                
                if (!isConnected)
                {
                    _logger.LogWarning("Database connectivity check failed during periodic check.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during periodic connectivity check");
            }
        }
        
        private string ExtractServerName(string connectionString)
        {
            try
            {
                // Parse connection string to extract server name using regex
                // Look for Server= or Data Source= pattern
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
        
        public void Dispose()
        {
            _connectivityCheckTimer?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
} 