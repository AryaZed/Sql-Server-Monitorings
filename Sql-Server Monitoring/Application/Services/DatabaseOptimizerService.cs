using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;
using System.Text;

namespace Sql_Server_Monitoring.Application.Services
{
    public class DatabaseOptimizerService : IDatabaseOptimizerService
    {
        private readonly IDatabaseRepository _databaseRepository;
        private readonly IOptimizationScriptRepository _optimizationScriptRepository;
        private readonly ILogger<DatabaseOptimizerService> _logger;

        public DatabaseOptimizerService(
            IDatabaseRepository databaseRepository,
            IOptimizationScriptRepository optimizationScriptRepository,
            ILogger<DatabaseOptimizerService> logger)
        {
            _databaseRepository = databaseRepository;
            _optimizationScriptRepository = optimizationScriptRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<OptimizationScript>> GenerateOptimizationScriptsAsync(
            string connectionString, string databaseName, IEnumerable<DbIssue> issues)
        {
            var scripts = new List<OptimizationScript>();

            try
            {
                _logger.LogInformation($"Generating optimization scripts for database '{databaseName}'");

                // Group issues by type
                var issuesByType = issues
                    .Where(i => !string.IsNullOrEmpty(i.SqlScript)) // Only consider issues with SQL scripts
                    .GroupBy(i => i.Type)
                    .ToDictionary(g => g.Key, g => g.ToList());

                // Generate scripts for each issue type
                foreach (var issueType in issuesByType.Keys)
                {
                    var scriptType = MapIssueTypeToScriptType(issueType);
                    var typeIssues = issuesByType[issueType];

                    // Skip if no issues of this type
                    if (!typeIssues.Any())
                        continue;

                    var script = GenerateScriptForIssueType(databaseName, issueType, typeIssues);

                    // Create optimization script entity
                    var optimizationScript = new OptimizationScript
                    {
                        Name = $"{issueType}Optimization_{databaseName}_{DateTime.Now:yyyyMMdd}",
                        Content = script,
                        Description = $"Optimization script to address {typeIssues.Count} {issueType} issues in database '{databaseName}'",
                        DatabaseName = databaseName,
                        Type = scriptType,
                        GenerationTime = DateTime.Now,
                        RelatedIssueIds = typeIssues.Select(i => i.Id).ToList()
                    };

                    // Save to repository
                    await _optimizationScriptRepository.AddScriptAsync(optimizationScript);

                    scripts.Add(optimizationScript);
                }

                _logger.LogInformation($"Generated {scripts.Count} optimization scripts for database '{databaseName}'");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating optimization scripts for database '{databaseName}'");
            }

            return scripts;
        }

        public async Task<string> ExecuteOptimizationScriptAsync(string connectionString, string databaseName, Guid scriptId)
        {
            try
            {
                _logger.LogInformation($"Executing optimization script {scriptId} for database '{databaseName}'");

                // Get the script
                var script = await _optimizationScriptRepository.GetScriptByIdAsync(scriptId);
                if (script == null)
                {
                    throw new KeyNotFoundException($"Optimization script with ID {scriptId} not found");
                }

                // Verify database name
                if (script.DatabaseName != databaseName)
                {
                    throw new InvalidOperationException($"Script {scriptId} is for database '{script.DatabaseName}', not '{databaseName}'");
                }

                // Execute the script
                await _databaseRepository.ExecuteScriptAsync(connectionString, databaseName, script.Content);

                // Mark script as executed
                script.IsExecuted = true;
                script.ExecutionTime = DateTime.Now;
                script.ExecutionResult = "Success";
                await _optimizationScriptRepository.UpdateScriptAsync(script);

                // Mark related issues as resolved
                foreach (var issueId in script.RelatedIssueIds)
                {
                    // Mark the issue as resolved
                    // In a real implementation, this would call the IssueRepository
                }

                _logger.LogInformation($"Successfully executed optimization script {scriptId} for database '{databaseName}'");

                return "Script executed successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing optimization script {scriptId} for database '{databaseName}'");

                // Update script with error
                var script = await _optimizationScriptRepository.GetScriptByIdAsync(scriptId);
                if (script != null)
                {
                    script.ExecutionResult = $"Error: {ex.Message}";
                    await _optimizationScriptRepository.UpdateScriptAsync(script);
                }

                throw;
            }
        }

        public async Task<OptimizationScript> GetRecommendedOptimizationAsync(string connectionString, string databaseName, IssueType issueType)
        {
            try
            {
                _logger.LogInformation($"Getting recommended optimization for {issueType} issues in database '{databaseName}'");

                var scriptType = MapIssueTypeToScriptType(issueType);

                // Get all scripts for this database and type
                var scripts = await _optimizationScriptRepository.GetScriptsAsync(connectionString, databaseName, scriptType);

                // Return the most recent one
                return scripts.OrderByDescending(s => s.GenerationTime).FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting recommended optimization for {issueType} issues in database '{databaseName}'");
                return null;
            }
        }

        #region Helper Methods
        private ScriptType MapIssueTypeToScriptType(IssueType issueType)
        {
            return issueType switch
            {
                IssueType.Schema => ScriptType.Schema,
                IssueType.Performance => ScriptType.Performance,
                IssueType.Index => ScriptType.Index,
                IssueType.Configuration => ScriptType.Configuration,
                IssueType.Security => ScriptType.Security,
                IssueType.Backup => ScriptType.Maintenance,
                IssueType.Corruption => ScriptType.Maintenance,
                IssueType.Capacity => ScriptType.Maintenance,
                _ => ScriptType.Maintenance
            };
        }

        private string GenerateScriptForIssueType(string databaseName, IssueType issueType, List<DbIssue> issues)
        {
            var scriptBuilder = new StringBuilder();

            // Add header
            scriptBuilder.AppendLine($"-- {issueType} Optimization Script for database '{databaseName}'");
            scriptBuilder.AppendLine($"-- Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            scriptBuilder.AppendLine($"-- Addressing {issues.Count} issues");
            scriptBuilder.AppendLine();

            // Add USE statement
            scriptBuilder.AppendLine($"USE [{databaseName}];");
            scriptBuilder.AppendLine("GO");
            scriptBuilder.AppendLine();

            // Add SQL for each issue
            foreach (var issue in issues.Where(i => !string.IsNullOrEmpty(i.SqlScript)))
            {
                scriptBuilder.AppendLine($"-- Issue: {issue.Message}");
                scriptBuilder.AppendLine($"-- Severity: {issue.Severity}");
                scriptBuilder.AppendLine($"-- Object: {issue.AffectedObject}");
                scriptBuilder.AppendLine(issue.SqlScript);
                scriptBuilder.AppendLine("GO");
                scriptBuilder.AppendLine();
            }

            return scriptBuilder.ToString();
        }
        #endregion
    }
}
