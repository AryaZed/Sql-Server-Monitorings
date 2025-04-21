using Microsoft.Extensions.Hosting;
using Sql_Server_Monitoring.Domain.Interfaces;

namespace Sql_Server_Monitoring.Application.BackgroundService
{
    public class MonitoringBackgroundService : IHostedService
    {
        private readonly IDatabaseMonitorService _monitorService;
        private readonly ISettingsRepository _settingsRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MonitoringBackgroundService> _logger;
        private Task _monitoringTask;
        private CancellationTokenSource _cancellationTokenSource;

        public MonitoringBackgroundService(
            IDatabaseMonitorService monitorService,
            ISettingsRepository settingsRepository,
            IConfiguration configuration,
            ILogger<MonitoringBackgroundService> logger)
        {
            _monitorService = monitorService;
            _settingsRepository = settingsRepository;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Monitoring background service is starting.");

            try
            {
                // Get default connection string
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    _logger.LogWarning("Default connection string not found. Monitoring service will not start automatically.");
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
                _monitoringTask = _monitorService.StartMonitoringAsync(connectionString, _cancellationTokenSource.Token);

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
    }
}
