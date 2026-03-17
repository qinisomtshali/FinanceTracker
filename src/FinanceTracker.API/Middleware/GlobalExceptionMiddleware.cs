using System.Net;
using System.Text.Json;
using FluentValidation;

namespace FinanceTracker.API.Middleware;

/// <summary>
/// Global exception handling middleware.
/// 
/// WHY: Without this, unhandled exceptions return ugly 500 errors with
/// stack traces (in dev) or empty responses (in prod). This middleware
/// catches ALL exceptions and returns consistent, clean JSON responses.
/// 
/// It also handles specific exception types differently:
///   - ValidationException → 400 Bad Request with error details
///   - UnauthorizedAccessException → 401 Unauthorized
///   - Everything else → 500 Internal Server Error (details logged, not exposed)
/// 
/// SECURITY: In production, we NEVER expose exception details to the client.
/// Stack traces, inner exceptions, and sensitive info stay in the logs only.
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, errors) = exception switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                validationEx.Errors.Select(e => e.ErrorMessage).ToList()
            ),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                new List<string> { "You are not authorized to perform this action." }
            ),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                new List<string> { "The requested resource was not found." }
            ),
            _ => (
                HttpStatusCode.InternalServerError,
                new List<string> { "An unexpected error occurred. Please try again later." }
            )
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            status = (int)statusCode,
            errors
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
