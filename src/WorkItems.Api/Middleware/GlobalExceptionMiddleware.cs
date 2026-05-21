using Microsoft.AspNetCore.Mvc;
using WorkItems.Api.Services;

namespace WorkItems.Api.Middleware;

/// <summary>
/// Catches all unhandled exceptions, logs them via Serilog, and returns a structured
/// ProblemDetails response (RFC 7807). Stack traces are never included in the response
/// body — they appear in server-side logs only (ADR-06).
/// </summary>
public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (NotFoundException ex)
        {
            // Business-level 404 — safe to surface the message to the client.
            logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
            await WriteProblemDetailsAsync(context, StatusCodes.Status404NotFound, "Resource not found", ex.Message);
        }
        catch (Exception ex)
        {
            // Unknown fault — log with full exception for diagnostics but hide detail from client.
            logger.LogError(ex, "Unhandled exception: {ExceptionType} — {Message}", ex.GetType().Name, ex.Message);
            await WriteProblemDetailsAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred", null);
        }
    }

    private static Task WriteProblemDetailsAsync(HttpContext context, int statusCode, string title, string? detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title  = title,
            Detail = detail,
        };

        return context.Response.WriteAsJsonAsync(problem);
    }
}
