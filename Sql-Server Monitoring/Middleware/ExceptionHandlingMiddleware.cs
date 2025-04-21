using System.Net;
using System.Text.Json;

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

            // Determine the status code and error message based on the exception type
            switch (exception)
            {
                case ArgumentException _:
                    statusCode = HttpStatusCode.BadRequest;
                    errorMessage = "Invalid arguments provided.";
                    break;
                case KeyNotFoundException _:
                    statusCode = HttpStatusCode.NotFound;
                    errorMessage = "The requested resource was not found.";
                    break;
                case UnauthorizedAccessException _:
                    statusCode = HttpStatusCode.Unauthorized;
                    errorMessage = "Unauthorized access to the resource.";
                    break;
                case InvalidOperationException _:
                    statusCode = HttpStatusCode.BadRequest;
                    errorMessage = "Invalid operation.";
                    break;
                case SqlException sqlEx:
                    statusCode = HttpStatusCode.InternalServerError;
                    errorMessage = "Database error occurred.";
                    _logger.LogError(sqlEx, "SQL Exception: {ErrorMessage}, Number: {Number}", sqlEx.Message, sqlEx.Number);
                    break;
                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    errorMessage = "An unexpected error occurred.";
                    break;
            }

            _logger.LogError(exception, "Exception: {ExceptionType}: {ExceptionMessage}", exception.GetType().Name, exception.Message);

            // Set the response status code
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            // Create the error response
            var response = new ErrorResponse
            {
                StatusCode = (int)statusCode,
                Message = errorMessage,
                DetailedMessage = _env.IsDevelopment() ? exception.Message : null,
                StackTrace = _env.IsDevelopment() ? exception.StackTrace : null,
                RequestPath = context.Request.Path,
                Timestamp = DateTime.UtcNow
            };

            // Serialize and return the response
            var json = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(json);
        }
    }

    public class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; }
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