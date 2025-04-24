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
    public class QueryAnalyzerController : ControllerBase
    {
        private readonly IQueryAnalyzerService _queryAnalyzerService;
        private readonly ILogger<QueryAnalyzerController> _logger;

        public QueryAnalyzerController(
            IQueryAnalyzerService queryAnalyzerService,
            ILogger<QueryAnalyzerController> logger)
        {
            _queryAnalyzerService = queryAnalyzerService;
            _logger = logger;
        }

        /// <summary>
        /// Gets a list of slow queries for a database.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="topN">Optional. The number of slow queries to return. Default is 10.</param>
        /// <returns>A list of slow queries for the database.</returns>
        [HttpGet("{databaseName}/slow-queries")]
        [ProducesResponseType(typeof(IEnumerable<SlowQuery>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
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
        /// Gets the text of a specific query.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="queryId">The ID of the query.</param>
        /// <returns>The text of the query.</returns>
        [HttpGet("{databaseName}/queries/{queryId}/text")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> GetQueryText(
            [FromQuery] string connectionString,
            string databaseName,
            int queryId)
        {
            try
            {
                var queryText = await _queryAnalyzerService.GetQueryTextAsync(connectionString, databaseName, queryId);
                if (string.IsNullOrEmpty(queryText))
                {
                    return NotFound(new { message = $"Query with ID '{queryId}' not found in database '{databaseName}'." });
                }
                return Ok(new { queryText });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting query text for query ID '{queryId}' in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting query text", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets the execution plan of a specific query.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="queryId">The ID of the query.</param>
        /// <returns>The execution plan of the query.</returns>
        [HttpGet("{databaseName}/queries/{queryId}/plan")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> GetQueryPlan(
            [FromQuery] string connectionString,
            string databaseName,
            int queryId)
        {
            try
            {
                var queryPlan = await _queryAnalyzerService.GetQueryPlanAsync(connectionString, databaseName, queryId);
                if (string.IsNullOrEmpty(queryPlan))
                {
                    return NotFound(new { message = $"Query plan for query ID '{queryId}' not found in database '{databaseName}'." });
                }
                return Ok(new { queryPlan });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting query plan for query ID '{queryId}' in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting query plan", error = ex.Message });
            }
        }

        /// <summary>
        /// Analyzes the execution plan of a specific query for issues.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="queryId">The ID of the query.</param>
        /// <returns>A list of issues found in the query execution plan.</returns>
        [HttpGet("{databaseName}/queries/{queryId}/analyze-plan")]
        [ProducesResponseType(typeof(IEnumerable<QueryPlanIssue>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
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
                _logger.LogError(ex, $"Error analyzing query plan for query ID '{queryId}' in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error analyzing query plan", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets statistics for a specific query.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="queryId">The ID of the query.</param>
        /// <returns>Statistics for the query.</returns>
        [HttpGet("{databaseName}/queries/{queryId}/statistics")]
        [ProducesResponseType(typeof(IEnumerable<QueryStatistic>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
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
                _logger.LogError(ex, $"Error getting query statistics for query ID '{queryId}' in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting query statistics", error = ex.Message });
            }
        }

        /// <summary>
        /// Gets optimization recommendations for a specific query.
        /// </summary>
        /// <param name="connectionString">The connection string to the SQL Server.</param>
        /// <param name="databaseName">The name of the database.</param>
        /// <param name="queryId">The ID of the query.</param>
        /// <returns>Optimization recommendations for the query.</returns>
        [HttpGet("{databaseName}/queries/{queryId}/optimization-recommendations")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<ActionResult> GetQueryOptimizationRecommendations(
            [FromQuery] string connectionString,
            string databaseName,
            int queryId)
        {
            try
            {
                var recommendations = await _queryAnalyzerService.GetQueryOptimizationRecommendationsAsync(connectionString, databaseName, queryId);
                if (string.IsNullOrEmpty(recommendations))
                {
                    return NotFound(new { message = $"No optimization recommendations available for query ID '{queryId}' in database '{databaseName}'." });
                }
                return Ok(new { recommendations });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting query optimization recommendations for query ID '{queryId}' in database '{databaseName}'");
                return StatusCode(500, new { message = $"Error getting query optimization recommendations", error = ex.Message });
            }
        }
    }
}
