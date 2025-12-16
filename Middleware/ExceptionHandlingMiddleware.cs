using System.Data.Odbc;
using System.Net;
using System.Text.Json;

namespace ImpalaApi.Middleware;

/// <summary>
/// Middleware for handling exceptions with differentiated error responses
/// - Connection errors: 503 Service Unavailable
/// - Query errors: 500 Internal Server Error
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            InvalidOperationException { InnerException: OdbcException odbcEx } => 
                DetermineOdbcErrorType(odbcEx),
            OdbcException odbcEx => 
                DetermineOdbcErrorType(odbcEx),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        _logger.LogError(exception, 
            "Error occurred: {Message}. Status: {StatusCode}", 
            exception.Message, 
            statusCode);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        // Add Retry-After header for 503 errors
        if (statusCode == HttpStatusCode.ServiceUnavailable)
        {
            context.Response.Headers["Retry-After"] = "30";
        }

        var response = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = message,
            Details = exception.Message,
            Timestamp = DateTime.UtcNow,
            Path = context.Request.Path
        };

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }

    private (HttpStatusCode StatusCode, string Message) DetermineOdbcErrorType(OdbcException odbcException)
    {
        // Check if it's a connection-related error
        var errorMessage = odbcException.Message.ToLowerInvariant();
        var isConnectionError = errorMessage.Contains("connection") ||
                                errorMessage.Contains("timeout") ||
                                errorMessage.Contains("network") ||
                                errorMessage.Contains("host") ||
                                errorMessage.Contains("unable to connect") ||
                                odbcException.Errors.Cast<OdbcError>().Any(e => 
                                    e.SQLState == "08001" ||  // Connection failure
                                    e.SQLState == "08S01" ||  // Communication link failure
                                    e.SQLState == "HYT00" ||  // Timeout expired
                                    e.SQLState == "HYT01");   // Connection timeout

        if (isConnectionError)
        {
            _logger.LogWarning(odbcException, 
                "ODBC connection error detected. SQLState: {SQLState}", 
                odbcException.Errors.Count > 0 ? odbcException.Errors[0].SQLState : "Unknown");
            
            return (HttpStatusCode.ServiceUnavailable, 
                    "Database service is temporarily unavailable. Please try again later.");
        }

        // Query execution errors
        _logger.LogError(odbcException, 
            "ODBC query execution error. SQLState: {SQLState}", 
            odbcException.Errors.Count > 0 ? odbcException.Errors[0].SQLState : "Unknown");
        
        return (HttpStatusCode.InternalServerError, 
                "An error occurred while processing the database query.");
    }

    private class ErrorResponse
    {
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string Path { get; set; } = string.Empty;
    }
}

/// <summary>
/// Extension method for registering the exception handling middleware
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionHandlingMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
