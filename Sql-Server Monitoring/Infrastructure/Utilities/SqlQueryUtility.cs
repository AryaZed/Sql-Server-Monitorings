using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Sql_Server_Monitoring.Infrastructure.Utilities
{
    /// <summary>
    /// Utility class for executing SQL queries with improved error handling and deadlock prevention
    /// </summary>
    public static class SqlQueryUtility
    {
        private static readonly ILogger _logger = new NullLoggerFactory().CreateLogger("SqlQueryUtility");
        
        /// <summary>
        /// Maximum number of retry attempts for queries that encounter deadlocks or timeouts
        /// </summary>
        public const int MaxRetryAttempts = 3;
        
        /// <summary>
        /// Base delay in milliseconds between retry attempts
        /// </summary>
        public const int BaseRetryDelayMs = 100;
        
        /// <summary>
        /// Executes a SQL query with automatic deadlock handling and retry logic
        /// </summary>
        /// <typeparam name="T">The return type</typeparam>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <param name="query">SQL query to execute</param>
        /// <param name="parameters">Optional SQL parameters</param>
        /// <param name="readerFunc">Function to process the SqlDataReader</param>
        /// <param name="commandTimeout">Command timeout in seconds (default 30)</param>
        /// <returns>The result of the query execution</returns>
        public static async Task<T> ExecuteQueryWithRetryAsync<T>(
            string connectionString,
            string query,
            Dictionary<string, object> parameters,
            Func<SqlDataReader, Task<T>> readerFunc,
            int commandTimeout = 30)
        {
            int retryCount = 0;
            
            while (true)
            {
                try
                {
                    using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();
                    
                    // Add deadlock prevention hint for read operations
                    if (IsReadOnlyQuery(query))
                    {
                        query = AddReadUncommittedHint(query);
                    }
                    
                    using var command = new SqlCommand(query, connection);
                    command.CommandTimeout = commandTimeout;
                    
                    // Add parameters
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    
                    using var reader = await command.ExecuteReaderAsync();
                    return await readerFunc(reader);
                }
                catch (SqlException ex) when ((ex.Number == 1205 || ex.Number == -2 || ex.Number == 11) && retryCount < MaxRetryAttempts)
                {
                    // Error 1205: Deadlock victim
                    // Error -2: Timeout
                    // Error 11: General network error
                    retryCount++;
                    int delay = BaseRetryDelayMs * (int)Math.Pow(2, retryCount - 1); // Exponential backoff
                    
                    _logger.LogWarning(
                        "SQL error {ErrorNumber} detected. Retrying operation (attempt {RetryCount}/{MaxRetries}) after {Delay}ms. Error: {ErrorMessage}",
                        ex.Number, retryCount, MaxRetryAttempts, delay, ex.Message);
                    
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing SQL query: {Query}", SanitizeQueryForLogging(query));
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Executes a non-query SQL command with retry logic
        /// </summary>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <param name="query">SQL query to execute</param>
        /// <param name="parameters">Optional SQL parameters</param>
        /// <param name="commandTimeout">Command timeout in seconds (default 30)</param>
        /// <returns>Number of rows affected</returns>
        public static async Task<int> ExecuteNonQueryWithRetryAsync(
            string connectionString,
            string query,
            Dictionary<string, object> parameters = null,
            int commandTimeout = 30)
        {
            int retryCount = 0;
            
            while (true)
            {
                try
                {
                    using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();
                    
                    using var command = new SqlCommand(query, connection);
                    command.CommandTimeout = commandTimeout;
                    
                    // Add parameters
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    
                    return await command.ExecuteNonQueryAsync();
                }
                catch (SqlException ex) when ((ex.Number == 1205 || ex.Number == -2 || ex.Number == 11) && retryCount < MaxRetryAttempts)
                {
                    retryCount++;
                    int delay = BaseRetryDelayMs * (int)Math.Pow(2, retryCount - 1);
                    
                    _logger.LogWarning(
                        "SQL error {ErrorNumber} detected. Retrying operation (attempt {RetryCount}/{MaxRetries}) after {Delay}ms. Error: {ErrorMessage}",
                        ex.Number, retryCount, MaxRetryAttempts, delay, ex.Message);
                    
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing non-query SQL command: {Query}", SanitizeQueryForLogging(query));
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Executes a SQL query and returns a scalar result with retry logic
        /// </summary>
        /// <typeparam name="T">The expected return type</typeparam>
        /// <param name="connectionString">SQL Server connection string</param>
        /// <param name="query">SQL query to execute</param>
        /// <param name="parameters">Optional SQL parameters</param>
        /// <param name="commandTimeout">Command timeout in seconds (default 30)</param>
        /// <returns>The scalar result</returns>
        public static async Task<T> ExecuteScalarWithRetryAsync<T>(
            string connectionString,
            string query,
            Dictionary<string, object> parameters = null,
            int commandTimeout = 30)
        {
            int retryCount = 0;
            
            while (true)
            {
                try
                {
                    using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();
                    
                    // Add deadlock prevention hint for read operations
                    if (IsReadOnlyQuery(query))
                    {
                        query = AddReadUncommittedHint(query);
                    }
                    
                    using var command = new SqlCommand(query, connection);
                    command.CommandTimeout = commandTimeout;
                    
                    // Add parameters
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                        {
                            command.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }
                    }
                    
                    var result = await command.ExecuteScalarAsync();
                    
                    if (result == DBNull.Value)
                        return default;
                        
                    return (T)Convert.ChangeType(result, typeof(T));
                }
                catch (SqlException ex) when ((ex.Number == 1205 || ex.Number == -2 || ex.Number == 11) && retryCount < MaxRetryAttempts)
                {
                    retryCount++;
                    int delay = BaseRetryDelayMs * (int)Math.Pow(2, retryCount - 1);
                    
                    _logger.LogWarning(
                        "SQL error {ErrorNumber} detected. Retrying operation (attempt {RetryCount}/{MaxRetries}) after {Delay}ms. Error: {ErrorMessage}",
                        ex.Number, retryCount, MaxRetryAttempts, delay, ex.Message);
                    
                    await Task.Delay(delay);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing scalar SQL query: {Query}", SanitizeQueryForLogging(query));
                    throw;
                }
            }
        }
        
        /// <summary>
        /// Determines if a query is read-only based on its first statement
        /// </summary>
        private static bool IsReadOnlyQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return false;
                
            string firstWord = query.TrimStart().Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.ToUpper();
            return firstWord == "SELECT" || firstWord == "WITH";
        }
        
        /// <summary>
        /// Adds a nolock hint to a SELECT query to prevent deadlocks
        /// </summary>
        private static string AddReadUncommittedHint(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return query;
                
            if (query.Contains("WITH (NOLOCK)", StringComparison.OrdinalIgnoreCase) || 
                query.Contains("WITH(NOLOCK)", StringComparison.OrdinalIgnoreCase) ||
                query.IndexOf("READUNCOMMITTED", StringComparison.OrdinalIgnoreCase) >= 0 ||
                query.IndexOf("READ UNCOMMITTED", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // Already has nolock hints
                return query;
            }
            
            // Add transaction isolation level hint to the beginning of the query
            return "SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;\r\n" + query;
        }
        
        /// <summary>
        /// Sanitizes a SQL query for logging by truncating it and removing sensitive information
        /// </summary>
        private static string SanitizeQueryForLogging(string query)
        {
            if (string.IsNullOrEmpty(query))
                return string.Empty;
                
            // Truncate long queries
            const int maxLength = 200;
            string truncated = query.Length > maxLength 
                ? query.Substring(0, maxLength) + "..." 
                : query;
                
            // Remove potential passwords
            truncated = Regex.Replace(truncated, 
                @"password\s*=\s*['\""][^'""]*['\""]", 
                "password='***'", 
                RegexOptions.IgnoreCase);
                
            return truncated;
        }
    }
} 