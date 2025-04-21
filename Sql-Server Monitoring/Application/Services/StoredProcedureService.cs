using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;
using System.Text.RegularExpressions;

namespace Sql_Server_Monitoring.Application.Services
{
    public class StoredProcedureService : IStoredProcedureService
    {
        private readonly IStoredProcedureRepository _repository;
        private readonly ILogger<StoredProcedureService> _logger;

        private static readonly string[] SqlInjectionPatterns = new[]
        {
            @"EXEC\s*\(\s*.*\+.*\)",
            @"EXECUTE\s*\(\s*.*\+.*\)",
            @"sp_executesql\s+.*\+.*",
            @"DECLARE\s+@sql\s+.*\+.*\s+EXEC",
            @"DECLARE\s+@cmd\s+.*\+.*\s+EXEC",
            @"EXEC\s+@sql",
            @"EXEC\s+@cmd"
        };

        private static readonly string[] BestPracticePatterns = new[]
        {
            @"SELECT\s+\*",                             // SELECT * is discouraged
            @"CREATE\s+TABLE\s+#.*",                    // Temp tables without explicit schema
            @"WITH\s+RECOMPILE",                        // WITH RECOMPILE should be used sparingly
            @"SET\s+NOCOUNT\s+OFF",                     // SET NOCOUNT OFF is generally not recommended
            @"SET\s+ROWCOUNT\s+",                       // SET ROWCOUNT usage is discouraged
            @"WAITFOR\s+DELAY",                         // WAITFOR DELAY can cause blocking
            @"BEGIN\s+TRY.*END\s+TRY\s+(?!BEGIN\s+CATCH)", // TRY without CATCH
            @"SELECT\s+(?!TOP|DISTINCT)[^;]*INTO\s+#",  // SELECT INTO without TOP/DISTINCT
            @"UPDATE\s+[^;]*\s+FROM\s+[^;]*\s+WHERE\s+(?!.*=)",  // UPDATE without equals in WHERE
            @"DELETE\s+FROM\s+[^;]*\s+WHERE\s+(?!.*=)" // DELETE without equals in WHERE
        };

        private static readonly string[] PerformancePatterns = new[]
        {
            @"SELECT\s+.*\s+WHERE\s+.*\s+LIKE\s+[',""']%",      // LIKE with leading wildcard
            @"SELECT\s+.*\s+WHERE\s+.*\s+NOT\s+IN\s+\(.*\)",    // NOT IN can perform poorly
            @"SELECT\s+.*\s+WHERE\s+.*\s+NOT\s+EXISTS\s+\(.*\)", // NOT EXISTS can perform poorly
            @"SELECT\s+.*\s+FROM\s+.*\s+ORDER\s+BY\s+.*\s+FOR\s+XML", // ORDER BY with FOR XML can be slow
            @"SELECT\s+.*\s+FROM\s+.*\s+CROSS\s+JOIN\s+.*",     // CROSS JOIN can cause cartesian products
            @"SELECT\s+.*\s+FROM\s+.*\s+WHERE\s+.*\s+OR\s+.*",  // OR conditions can prevent index usage
            @"SELECT\s+.*\s+FROM\s+.*\s+WHERE\s+.+\s*\+\s*.+",  // Expressions in WHERE can prevent index usage
            @"ISNULL\s*\(\s*.*\s*,\s*.*\s*\)",                  // ISNULL can prevent index usage
            @"CONVERT\s*\(\s*.*\s*,\s*.*\s*\)",                 // CONVERT can prevent index usage
            @"CURSOR\s+",                                       // Cursors are generally slow
            @"WHILE\s+.*\s+BEGIN\s+.*\s+SELECT"                 // WHILE loops with queries can be slow
        };

        public StoredProcedureService(
            IStoredProcedureRepository repository,
            ILogger<StoredProcedureService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<StoredProcedure>> GetAllStoredProceduresAsync(string connectionString, string databaseName)
        {
            try
            {
                _logger.LogInformation($"Getting all stored procedures for database '{databaseName}'");
                return await _repository.GetAllStoredProceduresAsync(connectionString, databaseName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting all stored procedures for database '{databaseName}'");
                throw;
            }
        }

        public async Task<StoredProcedure> GetStoredProcedureDetailsAsync(string connectionString, string databaseName, string schemaName, string procedureName)
        {
            try
            {
                _logger.LogInformation($"Getting details for stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                return await _repository.GetStoredProcedureDetailsAsync(connectionString, databaseName, schemaName, procedureName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting details for stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                throw;
            }
        }

        public async Task<string> CreateStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName, string definition)
        {
            try
            {
                _logger.LogInformation($"Creating stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                
                // Validate the procedure definition
                if (string.IsNullOrWhiteSpace(definition))
                {
                    throw new ArgumentException("Stored procedure definition cannot be empty");
                }

                // Basic validation - ensure the CREATE PROCEDURE statement is present and correctly formatted
                if (!definition.TrimStart().StartsWith("CREATE PROCEDURE", StringComparison.OrdinalIgnoreCase) &&
                    !definition.TrimStart().StartsWith("CREATE OR ALTER PROCEDURE", StringComparison.OrdinalIgnoreCase) &&
                    !definition.TrimStart().StartsWith("CREATE PROC", StringComparison.OrdinalIgnoreCase) &&
                    !definition.TrimStart().StartsWith("CREATE OR ALTER PROC", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Definition must start with CREATE [OR ALTER] PROCEDURE or CREATE [OR ALTER] PROC");
                }

                // Add schema prefix if not included in definition
                if (!Regex.IsMatch(definition, @"CREATE\s+(OR\s+ALTER\s+)?PROC(EDURE)?\s+\[\w+\]\.\[\w+\]", RegexOptions.IgnoreCase) &&
                    !Regex.IsMatch(definition, @"CREATE\s+(OR\s+ALTER\s+)?PROC(EDURE)?\s+\w+\.\w+", RegexOptions.IgnoreCase))
                {
                    // Extract just the procedure name from the definition
                    var match = Regex.Match(definition, @"CREATE\s+(OR\s+ALTER\s+)?PROC(EDURE)?\s+(\[\w+\]|\w+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        string procNameInDef = match.Groups[3].Value;
                        
                        // Replace with schema.procedure notation
                        definition = Regex.Replace(
                            definition,
                            @"(CREATE\s+(OR\s+ALTER\s+)?PROC(EDURE)?\s+)(\[\w+\]|\w+)",
                            $"$1[{schemaName}].[{procedureName}]",
                            RegexOptions.IgnoreCase);
                    }
                }

                return await _repository.CreateStoredProcedureAsync(connectionString, databaseName, schemaName, procedureName, definition);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                throw;
            }
        }

        public async Task<bool> UpdateStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName, string newDefinition)
        {
            try
            {
                _logger.LogInformation($"Updating stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                
                // Validate the procedure definition
                if (string.IsNullOrWhiteSpace(newDefinition))
                {
                    throw new ArgumentException("Stored procedure definition cannot be empty");
                }

                // Basic validation - ensure the ALTER or CREATE OR ALTER PROCEDURE statement is present
                if (!newDefinition.TrimStart().StartsWith("ALTER PROCEDURE", StringComparison.OrdinalIgnoreCase) &&
                    !newDefinition.TrimStart().StartsWith("ALTER PROC", StringComparison.OrdinalIgnoreCase) &&
                    !newDefinition.TrimStart().StartsWith("CREATE OR ALTER PROCEDURE", StringComparison.OrdinalIgnoreCase) &&
                    !newDefinition.TrimStart().StartsWith("CREATE OR ALTER PROC", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Definition must start with ALTER PROCEDURE, ALTER PROC, CREATE OR ALTER PROCEDURE, or CREATE OR ALTER PROC");
                }

                // Add schema prefix if not included in definition
                if (!Regex.IsMatch(newDefinition, @"(ALTER|CREATE\s+OR\s+ALTER)\s+PROC(EDURE)?\s+\[\w+\]\.\[\w+\]", RegexOptions.IgnoreCase) &&
                    !Regex.IsMatch(newDefinition, @"(ALTER|CREATE\s+OR\s+ALTER)\s+PROC(EDURE)?\s+\w+\.\w+", RegexOptions.IgnoreCase))
                {
                    // Extract just the procedure name from the definition
                    var match = Regex.Match(newDefinition, @"(ALTER|CREATE\s+OR\s+ALTER)\s+PROC(EDURE)?\s+(\[\w+\]|\w+)", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        // Replace with schema.procedure notation
                        newDefinition = Regex.Replace(
                            newDefinition,
                            @"((ALTER|CREATE\s+OR\s+ALTER)\s+PROC(EDURE)?\s+)(\[\w+\]|\w+)",
                            $"$1[{schemaName}].[{procedureName}]",
                            RegexOptions.IgnoreCase);
                    }
                }

                return await _repository.UpdateStoredProcedureAsync(connectionString, databaseName, schemaName, procedureName, newDefinition);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                throw;
            }
        }

        public async Task<bool> DeleteStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName)
        {
            try
            {
                _logger.LogInformation($"Deleting stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                return await _repository.DeleteStoredProcedureAsync(connectionString, databaseName, schemaName, procedureName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                throw;
            }
        }

        public async Task<string> ExecuteStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName, Dictionary<string, object> parameters)
        {
            try
            {
                _logger.LogInformation($"Executing stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                
                // Get procedure details to validate it exists
                var proc = await _repository.GetStoredProcedureDetailsAsync(connectionString, databaseName, schemaName, procedureName);
                if (proc == null)
                {
                    throw new ArgumentException($"Stored procedure '{schemaName}.{procedureName}' not found in database '{databaseName}'");
                }

                return await _repository.ExecuteStoredProcedureAsync(connectionString, databaseName, schemaName, procedureName, parameters ?? new Dictionary<string, object>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error executing stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                throw;
            }
        }

        public async Task<IEnumerable<StoredProcedure>> FindUnusedStoredProceduresAsync(string connectionString, string databaseName, int daysSinceLastExecution = 90)
        {
            try
            {
                _logger.LogInformation($"Finding unused stored procedures in database '{databaseName}' (not executed in {daysSinceLastExecution} days)");
                return await _repository.FindUnusedStoredProceduresAsync(connectionString, databaseName, daysSinceLastExecution);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding unused stored procedures in database '{databaseName}'");
                throw;
            }
        }

        public async Task<IEnumerable<StoredProcedure>> FindPotentialSqlInjectionProceduresAsync(string connectionString, string databaseName)
        {
            try
            {
                _logger.LogInformation($"Finding stored procedures with potential SQL injection risks in database '{databaseName}'");
                
                var allProcedures = await _repository.GetAllStoredProceduresAsync(connectionString, databaseName);
                
                var riskyProcedures = new List<StoredProcedure>();
                
                foreach (var proc in allProcedures)
                {
                    // Skip encrypted procedures as we can't analyze them
                    if (proc.IsEncrypted)
                    {
                        continue;
                    }
                    
                    if (proc.HasDynamicSql && CheckForSqlInjectionPatterns(proc.Definition))
                    {
                        riskyProcedures.Add(proc);
                    }
                }
                
                return riskyProcedures;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finding potential SQL injection procedures in database '{databaseName}'");
                throw;
            }
        }

        public async Task<StoredProcedureAnalysisResult> AnalyzeStoredProcedureAsync(string connectionString, string databaseName, string schemaName, string procedureName)
        {
            try
            {
                _logger.LogInformation($"Analyzing stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                
                var result = new StoredProcedureAnalysisResult();
                
                // Get procedure details
                var proc = await _repository.GetStoredProcedureDetailsAsync(connectionString, databaseName, schemaName, procedureName);
                if (proc == null)
                {
                    throw new ArgumentException($"Stored procedure '{schemaName}.{procedureName}' not found in database '{databaseName}'");
                }
                
                // Skip analysis for encrypted procedures
                if (proc.IsEncrypted)
                {
                    result.Issues.Add("Procedure is encrypted and cannot be analyzed");
                    result.Recommendations.Add("Consider creating a non-encrypted version for analysis");
                    return result;
                }
                
                // Check for SQL injection risks
                if (proc.HasDynamicSql)
                {
                    if (CheckForSqlInjectionPatterns(proc.Definition))
                    {
                        result.HasSqlInjectionRisk = true;
                        result.Issues.Add("Potential SQL injection risk detected: Dynamic SQL with string concatenation");
                        result.Recommendations.Add("Use parameterized queries with sp_executesql instead of string concatenation");
                    }
                }
                
                // Check for performance issues
                foreach (var pattern in PerformancePatterns)
                {
                    if (Regex.IsMatch(proc.Definition, pattern, RegexOptions.IgnoreCase))
                    {
                        result.HasPerformanceIssues = true;
                        string issue = GetPerformanceIssueDescription(pattern);
                        string recommendation = GetPerformanceRecommendation(pattern);
                        
                        result.Issues.Add($"Performance issue detected: {issue}");
                        result.Recommendations.Add(recommendation);
                    }
                }
                
                // Check for best practice violations
                foreach (var pattern in BestPracticePatterns)
                {
                    if (Regex.IsMatch(proc.Definition, pattern, RegexOptions.IgnoreCase))
                    {
                        result.HasBestPracticeViolations = true;
                        string issue = GetBestPracticeIssueDescription(pattern);
                        string recommendation = GetBestPracticeRecommendation(pattern);
                        
                        result.Issues.Add($"Best practice violation: {issue}");
                        result.Recommendations.Add(recommendation);
                    }
                }
                
                // Check for missing SET NOCOUNT ON
                if (!Regex.IsMatch(proc.Definition, @"SET\s+NOCOUNT\s+ON", RegexOptions.IgnoreCase))
                {
                    result.HasBestPracticeViolations = true;
                    result.Issues.Add("Missing SET NOCOUNT ON statement");
                    result.Recommendations.Add("Add SET NOCOUNT ON at the beginning of the procedure to improve performance");
                }
                
                // Check for proper error handling
                if (!Regex.IsMatch(proc.Definition, @"TRY\s+.*\s+CATCH", RegexOptions.IgnoreCase))
                {
                    result.HasBestPracticeViolations = true;
                    result.Issues.Add("Missing TRY/CATCH error handling");
                    result.Recommendations.Add("Implement TRY/CATCH blocks for robust error handling");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing stored procedure '{schemaName}.{procedureName}' in database '{databaseName}'");
                throw;
            }
        }

        #region Private methods

        private bool CheckForSqlInjectionPatterns(string definition)
        {
            foreach (var pattern in SqlInjectionPatterns)
            {
                if (Regex.IsMatch(definition, pattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }

        private string GetPerformanceIssueDescription(string pattern)
        {
            return pattern switch
            {
                var p when p.Contains("LIKE") => "Using LIKE with leading wildcard (%) which prevents index usage",
                var p when p.Contains("NOT IN") => "Using NOT IN operator which can cause performance issues",
                var p when p.Contains("NOT EXISTS") => "Using NOT EXISTS which can cause performance issues",
                var p when p.Contains("ORDER BY") && p.Contains("FOR XML") => "Using ORDER BY with FOR XML which can be slow",
                var p when p.Contains("CROSS JOIN") => "Using CROSS JOIN which can cause cartesian products",
                var p when p.Contains("OR") => "Using OR conditions which can prevent index usage",
                var p when p.Contains("+") => "Using expressions in WHERE clause which can prevent index usage",
                var p when p.Contains("ISNULL") => "Using ISNULL function which can prevent index usage",
                var p when p.Contains("CONVERT") => "Using CONVERT function which can prevent index usage",
                var p when p.Contains("CURSOR") => "Using cursor which is generally slower than set-based operations",
                var p when p.Contains("WHILE") => "Using WHILE loop with queries which can be slow",
                _ => "Performance issue detected"
            };
        }

        private string GetPerformanceRecommendation(string pattern)
        {
            return pattern switch
            {
                var p when p.Contains("LIKE") => "Consider redesigning query to avoid leading wildcards or use full-text indexes",
                var p when p.Contains("NOT IN") => "Replace NOT IN with NOT EXISTS or LEFT JOIN / IS NULL pattern",
                var p when p.Contains("NOT EXISTS") => "Consider alternative query designs using EXISTS or LEFT JOIN / IS NULL pattern",
                var p when p.Contains("ORDER BY") && p.Contains("FOR XML") => "Consider removing ORDER BY or handling ordering in application code",
                var p when p.Contains("CROSS JOIN") => "Replace with proper INNER JOIN or other join types with join conditions",
                var p when p.Contains("OR") => "Consider splitting into UNION of queries or using index-friendly techniques",
                var p when p.Contains("+") => "Move expressions to computed columns or separate calculations from filtering",
                var p when p.Contains("ISNULL") => "Replace with COALESCE or redesign query to avoid NULL handling in WHERE clauses",
                var p when p.Contains("CONVERT") => "Apply conversions to parameters before the query or use computed columns",
                var p when p.Contains("CURSOR") => "Replace with set-based operations or consider table variables for row-by-row operations",
                var p when p.Contains("WHILE") => "Replace with set-based operations where possible",
                _ => "Optimize query for better performance"
            };
        }

        private string GetBestPracticeIssueDescription(string pattern)
        {
            return pattern switch
            {
                var p when p.Contains("SELECT *") => "Using SELECT * which retrieves unnecessary columns",
                var p when p.Contains("CREATE TABLE #") => "Creating temp table without explicitly defining schema",
                var p when p.Contains("WITH RECOMPILE") => "Using WITH RECOMPILE which can cause unnecessary compilations",
                var p when p.Contains("SET NOCOUNT OFF") => "Using SET NOCOUNT OFF which can decrease performance",
                var p when p.Contains("SET ROWCOUNT") => "Using deprecated SET ROWCOUNT",
                var p when p.Contains("WAITFOR DELAY") => "Using WAITFOR DELAY which can cause blocking",
                var p when p.Contains("BEGIN TRY") && !p.Contains("BEGIN CATCH") => "Using TRY without CATCH block",
                var p when p.Contains("SELECT") && p.Contains("INTO #") => "Using SELECT INTO without TOP/DISTINCT",
                var p when p.Contains("UPDATE") => "Using UPDATE without equals in WHERE clause",
                var p when p.Contains("DELETE") => "Using DELETE without equals in WHERE clause",
                _ => "Best practice violation detected"
            };
        }

        private string GetBestPracticeRecommendation(string pattern)
        {
            return pattern switch
            {
                var p when p.Contains("SELECT *") => "Explicitly list needed columns to improve performance and reduce network traffic",
                var p when p.Contains("CREATE TABLE #") => "Use explicit schema definition for temp tables: CREATE TABLE #table (column datatype, ...)",
                var p when p.Contains("WITH RECOMPILE") => "Use query hints or plan guides instead of WITH RECOMPILE where possible",
                var p when p.Contains("SET NOCOUNT OFF") => "Use SET NOCOUNT ON to improve performance by reducing network traffic",
                var p when p.Contains("SET ROWCOUNT") => "Replace SET ROWCOUNT with TOP or other filtering methods",
                var p when p.Contains("WAITFOR DELAY") => "Avoid WAITFOR DELAY as it blocks the connection; consider alternative designs",
                var p when p.Contains("BEGIN TRY") && !p.Contains("BEGIN CATCH") => "Always use CATCH blocks with TRY blocks for proper error handling",
                var p when p.Contains("SELECT") && p.Contains("INTO #") => "Use CREATE TABLE #temp first, then INSERT INTO to avoid potential issues",
                var p when p.Contains("UPDATE") => "Ensure UPDATE operations have specific equals conditions in WHERE clause",
                var p when p.Contains("DELETE") => "Ensure DELETE operations have specific equals conditions in WHERE clause",
                _ => "Follow SQL Server best practices"
            };
        }

        #endregion
    }
} 