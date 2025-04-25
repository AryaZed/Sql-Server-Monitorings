using System.Net;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Sql_Server_Monitoring.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger,
            IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            HttpStatusCode statusCode;
            string errorMessage;
            string errorCode = "INTERNAL_ERROR";

            // Determine the status code and error message based on the exception type
            switch (exception)
            {
                case ArgumentException _:
                    statusCode = HttpStatusCode.BadRequest;
                    errorMessage = "Invalid arguments provided.";
                    errorCode = "INVALID_ARGUMENT";
                    break;
                case KeyNotFoundException _:
                    statusCode = HttpStatusCode.NotFound;
                    errorMessage = "The requested resource was not found.";
                    errorCode = "RESOURCE_NOT_FOUND";
                    break;
                case UnauthorizedAccessException _:
                    statusCode = HttpStatusCode.Unauthorized;
                    errorMessage = "Unauthorized access to the resource.";
                    errorCode = "UNAUTHORIZED";
                    break;
                case InvalidOperationException _:
                    statusCode = HttpStatusCode.BadRequest;
                    errorMessage = "Invalid operation.";
                    errorCode = "INVALID_OPERATION";
                    break;
                case SqlException sqlEx:
                    statusCode = HttpStatusCode.InternalServerError;
                    errorCode = $"SQL_{sqlEx.Number}";

                    // Handle specific SQL error codes
                    switch (sqlEx.Number)
                    {
                        case 2:
                            errorMessage = "Could not connect to SQL Server instance.";
                            statusCode = HttpStatusCode.ServiceUnavailable;
                            break;
                        case 4060:
                            errorMessage = "Database access denied.";
                            statusCode = HttpStatusCode.Forbidden;
                            break;
                        case 4064:
                            errorMessage = "SQL Server login failed.";
                            statusCode = HttpStatusCode.Unauthorized;
                            break;
                        case 208:
                            errorMessage = "The specified object was not found in the database.";
                            statusCode = HttpStatusCode.NotFound;
                            break;
                        case 1205:
                            errorMessage = "Transaction deadlock detected.";
                            break;
                        case 229:
                            errorMessage = "The current operation was canceled by user.";
                            break;
                        case 547:
                            errorMessage = "Database constraint violation.";
                            statusCode = HttpStatusCode.BadRequest;
                            break;
                        case 2627:
                        case 2601:
                            errorMessage = "Duplicate key violation.";
                            statusCode = HttpStatusCode.Conflict;
                            break;
                        case 8645:
                        case 8651:
                            errorMessage = "A timeout occurred while processing the request.";
                            statusCode = HttpStatusCode.RequestTimeout;
                            break;
                        default:
                            errorMessage = "Database error occurred.";
                            break;
                    }
                    
                    _logger.LogError(sqlEx, "SQL Exception: {ErrorMessage}, Number: {Number}, State: {State}, Server: {Server}", 
                        sqlEx.Message, sqlEx.Number, sqlEx.State, sqlEx.Server);
                    break;
                case TimeoutException _:
                    statusCode = HttpStatusCode.RequestTimeout;
                    errorMessage = "The request timed out.";
                    errorCode = "TIMEOUT";
                    break;
                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    errorMessage = "An unexpected error occurred.";
                    break;
            }

            var requestId = Guid.NewGuid().ToString();
            _logger.LogError(exception, "RequestId: {RequestId}, Exception: {ExceptionType}: {ExceptionMessage}", 
                requestId, exception.GetType().Name, exception.Message);

            // Set the response status code
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            // Create the error response
            var response = new ErrorResponse
            {
                StatusCode = (int)statusCode,
                Message = errorMessage,
                ErrorCode = errorCode,
                RequestId = requestId,
                DetailedMessage = _env.IsDevelopment() ? exception.Message : null,
                StackTrace = _env.IsDevelopment() ? exception.StackTrace : null,
                RequestPath = context.Request.Path,
                Timestamp = DateTime.UtcNow
            };

            // Serialize and return the response
            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = _env.IsDevelopment()
            });
            return context.Response.WriteAsync(json);
        }
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string ErrorCode { get; set; }
        public string RequestId { get; set; }
        public string DetailedMessage { get; set; }
        public string StackTrace { get; set; }
        public string RequestPath { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Extension method to register the middleware
    public static class ExceptionHandlingMiddlewareExtensions
    {
        public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ExceptionHandlingMiddleware>();
        }
    }
} 