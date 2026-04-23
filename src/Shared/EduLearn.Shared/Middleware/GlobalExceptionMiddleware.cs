using System.Text.Json;
using EduLearn.Shared.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EduLearn.Shared.Middleware;

// Global exception handler — catches ALL unhandled exceptions in every service
// Returns RFC 7807 ProblemDetails JSON so Angular can parse errors consistently
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
            // Pass request down the pipeline
            await _next(context);
        }
        catch (Exception ex)
        {
            // Log the exception with full details
            _logger.LogError(ex, "Unhandled exception on {Method} {Path}", 
                context.Request.Method, context.Request.Path);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Map exception type to HTTP status + message
        var (statusCode, errorCode, message) = exception switch
        {
            EduLearnException ex  => (ex.StatusCode, ex.ErrorCode, ex.Message),
            UnauthorizedAccessException => (401, "UNAUTHORIZED", "Authentication is required."),
            _                          => (500, "INTERNAL_ERROR", "An unexpected error occurred.")
        };

        // Build RFC 7807 ProblemDetails response
        var problemDetails = new
        {
            type     = $"https://httpstatuses.com/{statusCode}",
            title    = errorCode,
            status   = statusCode,
            detail   = message,
            instance = context.Request.Path.ToString()
        };

        context.Response.StatusCode  = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails));
    }
}
