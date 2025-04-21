using Microsoft.Data.SqlClient;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;
using System.Text.Json;

namespace Sql_Server_Monitoring.Infrastructure.Data
{
    public class OptimizationScriptRepository : IOptimizationScriptRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<OptimizationScriptRepository> _logger;
        private readonly string _storageConnectionString;
        private readonly Dictionary<Guid, OptimizationScript> _inMemoryCache = new();

        public OptimizationScriptRepository(
            IConfiguration configuration,
            ILogger<OptimizationScriptRepository> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // In a production environment, this would use a proper database
            // For now, we'll use in-memory storage with persistence to a local file
            _storageConnectionString = configuration.GetConnectionString("StorageConnection");
        }

        public async Task<IEnumerable<OptimizationScript>> GetScriptsAsync(string connectionString = null, string databaseName = null, ScriptType? type = null)
        {
            try
            {
                IEnumerable<OptimizationScript> scripts = _inMemoryCache.Values;

                // Apply filters
                if (!string.IsNullOrEmpty(databaseName))
                {
                    scripts = scripts.Where(s => s.DatabaseName == databaseName);
                }

                if (type.HasValue)
                {
                    scripts = scripts.Where(s => s.Type == type.Value);
                }

                return await Task.FromResult(scripts.ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving optimization scripts");
                throw;
            }
        }

        public async Task<OptimizationScript> GetScriptByIdAsync(Guid id)
        {
            try
            {
                if (_inMemoryCache.TryGetValue(id, out var script))
                {
                    return await Task.FromResult(script);
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving optimization script with ID {id}");
                throw;
            }
        }

        public async Task AddScriptAsync(OptimizationScript script)
        {
            try
            {
                if (_inMemoryCache.ContainsKey(script.Id))
                {
                    throw new InvalidOperationException($"Script with ID {script.Id} already exists");
                }

                _inMemoryCache[script.Id] = script;
                await PersistScriptsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error adding optimization script {script.Name}");
                throw;
            }
        }

        public async Task UpdateScriptAsync(OptimizationScript script)
        {
            try
            {
                if (!_inMemoryCache.ContainsKey(script.Id))
                {
                    throw new KeyNotFoundException($"Script with ID {script.Id} not found");
                }

                _inMemoryCache[script.Id] = script;
                await PersistScriptsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating optimization script {script.Name}");
                throw;
            }
        }

        public async Task DeleteScriptAsync(Guid id)
        {
            try
            {
                if (!_inMemoryCache.ContainsKey(id))
                {
                    throw new KeyNotFoundException($"Script with ID {id} not found");
                }

                _inMemoryCache.Remove(id);
                await PersistScriptsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting optimization script with ID {id}");
                throw;
            }
        }

        public async Task MarkScriptAsExecutedAsync(Guid id, string executionResult)
        {
            try
            {
                if (!_inMemoryCache.TryGetValue(id, out var script))
                {
                    throw new KeyNotFoundException($"Script with ID {id} not found");
                }

                script.IsExecuted = true;
                script.ExecutionTime = DateTime.Now;
                script.ExecutionResult = executionResult;

                await PersistScriptsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error marking script with ID {id} as executed");
                throw;
            }
        }

        private async Task PersistScriptsAsync()
        {
            try
            {
                // In a production environment, this would save to a database
                // For this implementation, we'll save to a JSON file
                var filePath = Path.Combine(AppContext.BaseDirectory, "optimization_scripts.json");
                var json = JsonSerializer.Serialize(_inMemoryCache.Values.ToList(), new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting optimization scripts");
            }
        }

        private async Task LoadScriptsAsync()
        {
            try
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, "optimization_scripts.json");
                if (File.Exists(filePath))
                {
                    var json = await File.ReadAllTextAsync(filePath);
                    var scripts = JsonSerializer.Deserialize<List<OptimizationScript>>(json);

                    _inMemoryCache.Clear();
                    foreach (var script in scripts)
                    {
                        _inMemoryCache[script.Id] = script;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading optimization scripts");
            }
        }
    }
} 