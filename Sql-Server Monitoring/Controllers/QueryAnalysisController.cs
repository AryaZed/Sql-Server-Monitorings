using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sql_Server_Monitoring.Domain.Interfaces;
using Sql_Server_Monitoring.Domain.Models;

namespace Sql_Server_Monitoring.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "UserPolicy")]
    public class QueryAnalysisController : ControllerBase
    {
        private readonly IQueryAnalyzerService _queryAnalyzerService;
        private readonly ILogger<QueryAnalysisController> _logger;

        public QueryAnalysisController(
            IQueryAnalyzerService queryAnalyzerService,
            ILogger<QueryAnalysisController> logger)
        {
            _queryAnalyzerService = queryAnalyzerService;
            _logger = logger;
        }

        /// <summary>
        /// Gets the slow queries for a database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="topN">Optional. The number of slow queries to retrieve.</param>
        /// <returns>A list of slow queries.</returns>
        [HttpGet("{databaseName}/slow-queries")]
        [ProducesResponseType(typeof(IEnumerable<SlowQuery>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<SlowQuery>>> GetSlowQueries(
            [FromQuery] string connectionString,
            string databaseName,
            [FromQuery] int topN = 10)
        {
            try
            {
                var queries = await _queryAnalyzerService.GetSlowQueriesAsync(connectionString, databaseName, topN);
                return Ok(queries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting slow queries for database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting slow queries for database '{databaseName}'", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the text of a query.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="queryId">The ID of the query.</param>
        /// <returns>The query text.</returns>
        [HttpGet("{databaseName}/query-text/{queryId}")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<string>> GetQueryText(
            [FromQuery] string connectionString,
            string databaseName,
            int queryId)
        {
            try
            {
                var queryText = await _queryAnalyzerService.GetQueryTextAsync(connectionString, databaseName, queryId);
                if (string.IsNullOrEmpty(queryText))
                {
                    return NotFound(new { message = $"Query text not found for query ID {queryId}" });
                }
                return Ok(queryText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting query text for query ID {queryId} in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting query text for query ID {queryId}", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the execution plan of a query.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="queryId">The ID of the query.</param>
        /// <returns>The query execution plan as XML.</returns>
        [HttpGet("{databaseName}/query-plan/{queryId}")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<string>> GetQueryPlan(
            [FromQuery] string connectionString,
            string databaseName,
            int queryId)
        {
            try
            {
                var queryPlan = await _queryAnalyzerService.GetQueryPlanAsync(connectionString, databaseName, queryId);
                if (string.IsNullOrEmpty(queryPlan))
                {
                    return NotFound(new { message = $"Query plan not found for query ID {queryId}" });
                }
                return Ok(queryPlan);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting query plan for query ID {queryId} in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting query plan for query ID {queryId}", error = ex.Message });
            }
        }

        /// <summary>
        /// Analyzes the execution plan of a query for issues.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="queryId">The ID of the query.</param>
        /// <returns>A list of issues found in the query plan.</returns>
        [HttpGet("{databaseName}/analyze-plan/{queryId}")]
        [ProducesResponseType(typeof(IEnumerable<QueryPlanIssue>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<QueryPlanIssue>>> AnalyzeQueryPlan(
            [FromQuery] string connectionString,
            string databaseName,
            int queryId)
        {
            try
            {
                var issues = await _queryAnalyzerService.AnalyzeQueryPlanAsync(connectionString, databaseName, queryId);
                return Ok(issues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing query plan for query ID {queryId} in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error analyzing query plan for query ID {queryId}", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the statistics of a query.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="queryId">The ID of the query.</param>
        /// <returns>A list of query statistics.</returns>
        [HttpGet("{databaseName}/query-statistics/{queryId}")]
        [ProducesResponseType(typeof(IEnumerable<QueryStatistic>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<IEnumerable<QueryStatistic>>> GetQueryStatistics(
            [FromQuery] string connectionString,
            string databaseName,
            int queryId)
        {
            try
            {
                var statistics = await _queryAnalyzerService.GetQueryStatisticsAsync(connectionString, databaseName, queryId);
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting query statistics for query ID {queryId} in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting query statistics for query ID {queryId}", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets optimization recommendations for a query.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="queryId">The ID of the query.</param>
        /// <returns>Optimization recommendations for the query.</returns>
        [HttpGet("{databaseName}/optimization-recommendations/{queryId}")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<string>> GetQueryOptimizationRecommendations(
            [FromQuery] string connectionString,
            string databaseName,
            int queryId)
        {
            try
            {
                var recommendations = await _queryAnalyzerService.GetQueryOptimizationRecommendationsAsync(connectionString, databaseName, queryId);
                return Ok(recommendations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting optimization recommendations for query ID {queryId} in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting optimization recommendations for query ID {queryId}", error = ex.Message });
            }
        }
    }
} 