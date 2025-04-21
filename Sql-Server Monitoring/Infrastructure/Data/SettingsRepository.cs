using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;
using System.Text.Json;

namespace Sql_Server_Monitoring.Infrastructure.Data
{
    public class SettingsRepository : ISettingsRepository
    {
        private readonly ILogger<SettingsRepository> _logger;
        private readonly string _settingsFilePath;
        private MonitoringSettings _cachedSettings;

        public SettingsRepository(ILogger<SettingsRepository> logger)
        {
            _logger = logger;
            _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "monitoring_settings.json");
        }

        public async Task<MonitoringSettings> GetMonitoringSettingsAsync()
        {
            try
            {
                // Return cached settings if available
                if (_cachedSettings != null)
                {
                    return _cachedSettings;
                }

                // Check if settings file exists
                if (File.Exists(_settingsFilePath))
                {
                    // Read settings from file
                    var json = await File.ReadAllTextAsync(_settingsFilePath);
                    _cachedSettings = JsonSerializer.Deserialize<MonitoringSettings>(json);
                }
                else
                {
                    // Create default settings
                    _cachedSettings = new MonitoringSettings
                    {
                        MonitoringIntervalSeconds = 300,
                        MonitorCpu = true,
                        MonitorMemory = true,
                        MonitorDisk = true,
                        MonitorQueries = true,
                        MonitorBlocking = true,
                        MonitorDeadlocks = true,
                        HighCpuThresholdPercent = 85,
                        LowPageLifeExpectancyThreshold = 300,
                        LongRunningQueryThresholdSec = 30,
                        RetentionDays = 30,
                        MonitoringEnabled = false,
                        Alerts = new List<AlertSetting>
                        {
                            new AlertSetting
                            {
                                Name = "High CPU Alert",
                                Description = "Alert when CPU usage exceeds threshold",
                                IsEnabled = true,
                                Type = AlertType.HighCpu,
                                MinimumSeverity = IssueSeverity.Medium,
                                Notifications = new List<AlertNotification>
                                {
                                    new AlertNotification
                                    {
                                        Type = NotificationType.Email,
                                        Target = "admin@example.com"
                                    }
                                }
                            },
                            new AlertSetting
                            {
                                Name = "Low Memory Alert",
                                Description = "Alert when memory pressure is detected",
                                IsEnabled = true,
                                Type = AlertType.LowMemory,
                                MinimumSeverity = IssueSeverity.Medium,
                                Notifications = new List<AlertNotification>
                                {
                                    new AlertNotification
                                    {
                                        Type = NotificationType.Email,
                                        Target = "admin@example.com"
                                    }
                                }
                            }
                        }
                    };

                    // Save default settings
                    await SaveMonitoringSettingsAsync(_cachedSettings);
                }

                return _cachedSettings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monitoring settings");
                
                // Return default settings in case of error
                return new MonitoringSettings();
            }
        }

        public async Task SaveMonitoringSettingsAsync(MonitoringSettings settings)
        {
            try
            {
                // Update cache
                _cachedSettings = settings;

                // Serialize settings to JSON
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                // Write to file
                await File.WriteAllTextAsync(_settingsFilePath, json);
                
                _logger.LogInformation("Monitoring settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving monitoring settings");
                throw;
            }
        }
    }
} 