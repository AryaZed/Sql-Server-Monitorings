using Microsoft.Data.SqlClient;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;
using System.Text;
using System.Text.RegularExpressions;

namespace Sql_Server_Monitoring.Application.Services
{
    public class QueryAnalyzerService : IQueryAnalyzerService
    {
        private readonly IDatabaseRepository _databaseRepository;
        private readonly ILogger<QueryAnalyzerService> _logger;

        public QueryAnalyzerService(
            IDatabaseRepository databaseRepository,
            ILogger<QueryAnalyzerService> logger)
        {
            _databaseRepository = databaseRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<SlowQuery>> GetSlowQueriesAsync(string connectionString, string databaseName, int topN = 10)
        {
            var slowQueries = new List<SlowQuery>();

            try
            {
                _logger.LogInformation($"Getting top {topN} slow queries for database '{databaseName}'");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Use Query Store if available (SQL Server 2016+)
                bool hasQueryStore = await HasQueryStoreEnabled(connection, databaseName);
                if (hasQueryStore)
                {
                    // Get slow queries from Query Store
                    slowQueries = await GetSlowQueriesFromQueryStore(connection, databaseName, topN);
                }
                else
                {
                    // Fallback to DMVs for older SQL Server versions
                    slowQueries = await GetSlowQueriesFromDmv(connection, databaseName, topN);
                }

                _logger.LogInformation($"Retrieved {slowQueries.Count} slow queries for database '{databaseName}'");
                return slowQueries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting slow queries for database '{databaseName}'");
                throw;
            }
        }

        public async Task<IEnumerable<QueryPlanIssue>> AnalyzeQueryPlanAsync(string connectionString, string databaseName, int queryId)
        {
            var issues = new List<QueryPlanIssue>();

            try
            {
                _logger.LogInformation($"Analyzing query plan for query ID {queryId} in database '{databaseName}'");

                var planXml = await GetQueryPlanAsync(connectionString, databaseName, queryId);
                if (string.IsNullOrEmpty(planXml))
                {
                    _logger.LogWarning($"No query plan found for query ID {queryId} in database '{databaseName}'");
                    return issues;
                }

                // Extract plan issues using regex patterns for common issues
                // In a real implementation, this would use a proper XML parser
                issues.AddRange(IdentifyPlanIssues(planXml));

                _logger.LogInformation($"Identified {issues.Count} potential issues in query plan for query ID {queryId}");
                return issues;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing query plan for query ID {queryId} in database '{databaseName}'");
                throw;
            }
        }

        public async Task<string> GetQueryTextAsync(string connectionString, string databaseName, int queryId)
        {
            try
            {
                _logger.LogInformation($"Getting query text for query ID {queryId} in database '{databaseName}'");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Use Query Store if available (SQL Server 2016+)
                bool hasQueryStore = await HasQueryStoreEnabled(connection, databaseName);
                if (hasQueryStore)
                {
                    return await GetQueryTextFromQueryStore(connection, databaseName, queryId);
                }
                else
                {
                    return await GetQueryTextFromDmv(connection, databaseName, queryId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting query text for query ID {queryId} in database '{databaseName}'");
                throw;
            }
        }

        public async Task<string> GetQueryPlanAsync(string connectionString, string databaseName, int queryId)
        {
            try
            {
                _logger.LogInformation($"Getting query plan for query ID {queryId} in database '{databaseName}'");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Use Query Store if available (SQL Server 2016+)
                bool hasQueryStore = await HasQueryStoreEnabled(connection, databaseName);
                if (hasQueryStore)
                {
                    return await GetQueryPlanFromQueryStore(connection, databaseName, queryId);
                }
                else
                {
                    return await GetQueryPlanFromDmv(connection, databaseName, queryId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting query plan for query ID {queryId} in database '{databaseName}'");
                throw;
            }
        }

        public async Task<IEnumerable<QueryStatistic>> GetQueryStatisticsAsync(string connectionString, string databaseName, int queryId)
        {
            var statistics = new List<QueryStatistic>();

            try
            {
                _logger.LogInformation($"Getting statistics for query ID {queryId} in database '{databaseName}'");

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                // Use Query Store if available (SQL Server 2016+)
                bool hasQueryStore = await HasQueryStoreEnabled(connection, databaseName);
                if (hasQueryStore)
                {
                    // Query statistics from Query Store
                    var query = $@"
                        USE [{databaseName}];
                        SELECT 
                            rs.avg_duration / 1000.0 AS avg_duration_ms,
                            rs.avg_cpu_time / 1000.0 AS avg_cpu_time_ms,
                            rs.avg_logical_io_reads AS avg_logical_reads,
                            rs.avg_logical_io_writes AS avg_logical_writes,
                            rs.avg_physical_io_reads AS avg_physical_reads,
                            rs.avg_rowcount AS avg_row_count,
                            rs.count_executions AS execution_count,
                            CONVERT(VARCHAR, rs.last_execution_time, 120) AS last_execution_time
                        FROM sys.query_store_query AS q
                        JOIN sys.query_store_plan AS p ON q.query_id = p.query_id
                        JOIN sys.query_store_runtime_stats AS rs ON p.plan_id = rs.plan_id
                        WHERE q.query_id = @QueryId
                        ORDER BY rs.last_execution_time DESC";

                    using var command = new SqlCommand(query, connection);
                    command.Parameters.AddWithValue("@QueryId", queryId);

                    using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        statistics.Add(new QueryStatistic
                        {
                            Name = "Average Duration (ms)",
                            Value = reader.GetDouble(0).ToString("N2")
                        });
                        statistics.Add(new QueryStatistic
                        {
                            Name = "Average CPU Time (ms)",
                            Value = reader.GetDouble(1).ToString("N2")
                        });
                        statistics.Add(new QueryStatistic
                        {
                            Name = "Average Logical Reads",
                            Value = reader.GetDouble(2).ToString("N0")
                        });
                        statistics.Add(new QueryStatistic
                        {
                            Name = "Average Logical Writes",
                            Value = reader.GetDouble(3).ToString("N0")
                        });
                        statistics.Add(new QueryStatistic
                        {
                            Name = "Average Physical Reads",
                            Value = reader.GetDouble(4).ToString("N0")
                        });
                        statistics.Add(new QueryStatistic
                        {
                            Name = "Average Row Count",
                            Value = reader.GetDouble(5).ToString("N0")
                        });
                        statistics.Add(new QueryStatistic
                        {
                            Name = "Execution Count",
                            Value = reader.GetInt64(6).ToString("N0")
                        });
                        statistics.Add(new QueryStatistic
                        {
                            Name = "Last Execution Time",
                            Value = reader.GetString(7)
                        });
                    }
                }
                else
                {
                    // Fallback to DMVs for older SQL Server versions
                    statistics = await GetQueryStatisticsFromDmv(connection, databaseName, queryId);
                }

                _logger.LogInformation($"Retrieved {statistics.Count} statistics for query ID {queryId}");
                return statistics;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting statistics for query ID {queryId} in database '{databaseName}'");
                throw;
            }
        }

        public async Task<string> GetQueryOptimizationRecommendationsAsync(string connectionString, string databaseName, int queryId)
        {
            try
            {
                _logger.LogInformation($"Generating optimization recommendations for query ID {queryId} in database '{databaseName}'");

                var recommendations = new StringBuilder();
                
                // Get query text
                var queryText = await GetQueryTextAsync(connectionString, databaseName, queryId);
                if (string.IsNullOrEmpty(queryText))
                {
                    return "Query text not found.";
                }

                // Get query plan
                var planXml = await GetQueryPlanAsync(connectionString, databaseName, queryId);
                if (string.IsNullOrEmpty(planXml))
                {
                    return "Query plan not found. Cannot generate recommendations.";
                }

                // Analyze plan for issues
                var planIssues = await AnalyzeQueryPlanAsync(connectionString, databaseName, queryId);
                
                if (!planIssues.Any())
                {
                    recommendations.AppendLine("No significant issues detected in the query plan.");
                }
                else
                {
                    recommendations.AppendLine("Optimization Recommendations:");
                    recommendations.AppendLine();

                    foreach (var issue in planIssues)
                    {
                        recommendations.AppendLine($"- {issue.Description}");
                        recommendations.AppendLine($"  Recommendation: {issue.RecommendedAction}");
                        recommendations.AppendLine();
                    }
                }

                // General query review
                recommendations.AppendLine("General Query Review:");
                recommendations.AppendLine();
                
                // Check for obvious anti-patterns in the query text
                if (queryText.Contains("SELECT *", StringComparison.OrdinalIgnoreCase))
                {
                    recommendations.AppendLine("- Avoid using SELECT * as it retrieves unnecessary columns.");
                    recommendations.AppendLine("  Recommendation: Explicitly select only the columns you need.");
                    recommendations.AppendLine();
                }

                if (Regex.IsMatch(queryText, @"SELECT\s+.*FROM\s+.*WHERE\s+.*\s+OR\s+", RegexOptions.IgnoreCase))
                {
                    recommendations.AppendLine("- OR conditions in WHERE clauses can lead to suboptimal plans.");
                    recommendations.AppendLine("  Recommendation: Consider rewriting as UNION of simpler queries where appropriate.");
                    recommendations.AppendLine();
                }

                if (Regex.IsMatch(queryText, @"SELECT\s+.*FROM\s+.*WHERE\s+.*\s+NOT\s+", RegexOptions.IgnoreCase))
                {
                    recommendations.AppendLine("- NOT operators in WHERE clauses can lead to suboptimal plans.");
                    recommendations.AppendLine("  Recommendation: Consider rewriting with positive conditions where possible.");
                    recommendations.AppendLine();
                }

                if (Regex.IsMatch(queryText, @"LIKE\s+['\[]%.*['\]]", RegexOptions.IgnoreCase))
                {
                    recommendations.AppendLine("- Leading wildcard in LIKE predicates prevent index usage.");
                    recommendations.AppendLine("  Recommendation: Avoid using leading wildcards or consider full-text indexing.");
                    recommendations.AppendLine();
                }

                // Add more query pattern checks as needed

                _logger.LogInformation($"Generated optimization recommendations for query ID {queryId}");
                return recommendations.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating optimization recommendations for query ID {queryId} in database '{databaseName}'");
                throw;
            }
        }

        #region Helper Methods

        private async Task<bool> HasQueryStoreEnabled(SqlConnection connection, string databaseName)
        {
            var query = $"SELECT actual_state FROM sys.database_query_store_options WHERE database_id = DB_ID('{databaseName}')";
            using var command = new SqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync();
            
            return result != null && result != DBNull.Value && (int)result > 0;
        }

        private async Task<List<SlowQuery>> GetSlowQueriesFromQueryStore(SqlConnection connection, string databaseName, int topN)
        {
            var slowQueries = new List<SlowQuery>();

            var query = $@"
                USE [{databaseName}];
                SELECT TOP {topN}
                    q.query_id,
                    SUBSTRING(qt.query_sql_text, 1, 1000) AS query_text,
                    rs.avg_duration / 1000.0 AS avg_duration_ms,
                    rs.avg_cpu_time / 1000.0 AS avg_cpu_time_ms,
                    rs.avg_logical_io_reads AS avg_logical_reads,
                    rs.count_executions AS execution_count,
                    CONVERT(VARCHAR, rs.last_execution_time, 120) AS last_execution_time,
                    CAST(p.query_plan AS NVARCHAR(MAX)) AS query_plan
                FROM sys.query_store_query AS q
                JOIN sys.query_store_query_text AS qt ON q.query_text_id = qt.query_text_id
                JOIN sys.query_store_plan AS p ON q.query_id = p.query_id
                JOIN sys.query_store_runtime_stats AS rs ON p.plan_id = rs.plan_id
                JOIN sys.query_store_runtime_stats_interval AS rsi ON rs.runtime_stats_interval_id = rsi.runtime_stats_interval_id
                WHERE rsi.start_time >= DATEADD(day, -7, GETUTCDATE())
                ORDER BY rs.avg_duration DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                slowQueries.Add(new SlowQuery
                {
                    QueryId = reader.GetInt32(0),
                    QueryText = reader.GetString(1),
                    AvgDurationMs = reader.GetDouble(2),
                    AvgCpuTimeMs = reader.GetDouble(3),
                    AvgLogicalReads = reader.GetDouble(4),
                    ExecutionCount = reader.GetInt64(5),
                    LastExecutionTime = reader.IsDBNull(6) ? DateTime.MinValue : DateTime.Parse(reader.GetString(6)),
                    QueryPlan = reader.IsDBNull(7) ? null : reader.GetString(7)
                });
            }

            return slowQueries;
        }

        private async Task<List<SlowQuery>> GetSlowQueriesFromDmv(SqlConnection connection, string databaseName, int topN)
        {
            var slowQueries = new List<SlowQuery>();

            var query = $@"
                USE [{databaseName}];
                SELECT TOP {topN}
                    qs.sql_handle,
                    qs.plan_handle,
                    qs.statement_start_offset,
                    qs.statement_end_offset,
                    qs.total_elapsed_time / qs.execution_count / 1000.0 AS avg_duration_ms,
                    qs.total_worker_time / qs.execution_count / 1000.0 AS avg_cpu_time_ms,
                    qs.total_logical_reads / qs.execution_count AS avg_logical_reads,
                    qs.execution_count,
                    qs.last_execution_time
                FROM sys.dm_exec_query_stats qs
                ORDER BY avg_duration_ms DESC";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                byte[] sqlHandle = (byte[])reader["sql_handle"];
                byte[] planHandle = (byte[])reader["plan_handle"];
                int statementStartOffset = reader.GetInt32(2);
                int statementEndOffset = reader.GetInt32(3);
                
                // Get the query text using a separate call
                string queryText = await GetQueryTextByHandle(connection, sqlHandle, statementStartOffset, statementEndOffset);
                string queryPlan = await GetQueryPlanByHandle(connection, planHandle);

                slowQueries.Add(new SlowQuery
                {
                    QueryId = -1, // Not available from DMVs
                    QueryText = queryText,
                    AvgDurationMs = reader.GetDouble(4),
                    AvgCpuTimeMs = reader.GetDouble(5),
                    AvgLogicalReads = reader.GetDouble(6),
                    ExecutionCount = reader.GetInt64(7),
                    LastExecutionTime = reader.GetDateTime(8),
                    QueryPlan = queryPlan
                });
            }

            return slowQueries;
        }

        private async Task<string> GetQueryTextByHandle(SqlConnection connection, byte[] sqlHandle, int startOffset, int endOffset)
        {
            using var command = new SqlCommand("SELECT text FROM sys.dm_exec_sql_text(@handle)", connection);
            command.Parameters.AddWithValue("@handle", sqlHandle);
            
            string fullText = (string)await command.ExecuteScalarAsync();
            if (string.IsNullOrEmpty(fullText))
            {
                return string.Empty;
            }

            // Calculate the actual statement text if offsets are valid
            if (startOffset >= 0 && endOffset > startOffset)
            {
                int length = endOffset - startOffset;
                return length <= fullText.Length ? fullText.Substring(startOffset / 2, length / 2) : fullText;
            }
            
            return fullText;
        }

        private async Task<string> GetQueryPlanByHandle(SqlConnection connection, byte[] planHandle)
        {
            using var command = new SqlCommand("SELECT query_plan FROM sys.dm_exec_query_plan(@handle)", connection);
            command.Parameters.AddWithValue("@handle", planHandle);
            
            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? result.ToString() : string.Empty;
        }

        private async Task<string> GetQueryTextFromQueryStore(SqlConnection connection, string databaseName, int queryId)
        {
            var query = $@"
                USE [{databaseName}];
                SELECT query_sql_text
                FROM sys.query_store_query AS q
                JOIN sys.query_store_query_text AS qt ON q.query_text_id = qt.query_text_id
                WHERE q.query_id = @QueryId";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@QueryId", queryId);
            
            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? result.ToString() : string.Empty;
        }

        private async Task<string> GetQueryTextFromDmv(SqlConnection connection, string databaseName, int queryId)
        {
            // In this simplified version, we're returning an empty string
            // as DMVs don't store query_id in the same way as Query Store
            _logger.LogWarning("Query ID lookup from DMVs is not supported in this version");
            return string.Empty;
        }

        private async Task<string> GetQueryPlanFromQueryStore(SqlConnection connection, string databaseName, int queryId)
        {
            var query = $@"
                USE [{databaseName}];
                SELECT TOP 1 query_plan
                FROM sys.query_store_plan
                WHERE query_id = @QueryId
                ORDER BY last_execution_time DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@QueryId", queryId);
            
            var result = await command.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? result.ToString() : string.Empty;
        }

        private async Task<string> GetQueryPlanFromDmv(SqlConnection connection, string databaseName, int queryId)
        {
            // In this simplified version, we're returning an empty string
            // as DMVs don't store query_id in the same way as Query Store
            _logger.LogWarning("Query ID lookup from DMVs is not supported in this version");
            return string.Empty;
        }

        private async Task<List<QueryStatistic>> GetQueryStatisticsFromDmv(SqlConnection connection, string databaseName, int queryId)
        {
            // In this simplified version, we're returning an empty list
            // as DMVs don't store query_id in the same way as Query Store
            _logger.LogWarning("Query ID lookup from DMVs is not supported in this version");
            return new List<QueryStatistic>();
        }

        private IEnumerable<QueryPlanIssue> IdentifyPlanIssues(string planXml)
        {
            var issues = new List<QueryPlanIssue>();

            if (string.IsNullOrEmpty(planXml))
            {
                return issues;
            }

            // Check for table scans
            if (planXml.Contains("<ScanOperation>Table Scan</ScanOperation>") || 
                planXml.Contains("<ScanOperation>Clustered Index Scan</ScanOperation>"))
            {
                issues.Add(new QueryPlanIssue
                {
                    IssueName = "Table Scan",
                    Description = "The query is performing a full table scan which can be expensive for large tables",
                    RecommendedAction = "Consider adding appropriate indexes to support the query's filter and join conditions"
                });
            }

            // Check for key lookups
            if (planXml.Contains("<PhysicalOp>Key Lookup</PhysicalOp>"))
            {
                issues.Add(new QueryPlanIssue
                {
                    IssueName = "Key Lookup",
                    Description = "The query is using a non-covering index requiring additional key lookups",
                    RecommendedAction = "Create a covering index that includes all columns needed by the query"
                });
            }

            // Check for hash joins
            if (planXml.Contains("<PhysicalOp>Hash Match</PhysicalOp>"))
            {
                issues.Add(new QueryPlanIssue
                {
                    IssueName = "Hash Match",
                    Description = "The query is using a hash join which can be memory intensive for large datasets",
                    RecommendedAction = "Consider optimizing join conditions or adding appropriate indexes to enable merge or nested loops joins"
                });
            }

            // Check for sorts
            if (planXml.Contains("<PhysicalOp>Sort</PhysicalOp>"))
            {
                issues.Add(new QueryPlanIssue
                {
                    IssueName = "Sort Operation",
                    Description = "The query requires a sort operation which can be expensive for large datasets",
                    RecommendedAction = "Add an index with an appropriate sort order to eliminate the sort operation"
                });
            }

            // Check for warnings
            if (planXml.Contains("<Warnings>"))
            {
                issues.Add(new QueryPlanIssue
                {
                    IssueName = "Plan Warnings",
                    Description = "The query plan contains warnings which may indicate performance issues",
                    RecommendedAction = "Review the complete execution plan for specific warnings and address each one accordingly"
                });
            }

            return issues;
        }

        #endregion
    }
} 