namespace WorkItems.Api.Middleware;

using Serilog.Context;

/// <summary>
/// Pushes the ASP.NET Core request TraceIdentifier into the Serilog LogContext so every
/// log entry produced within a single request carries a CorrelationId property.
/// This makes it possible to correlate all log lines for one request in a log aggregator.
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        using (LogContext.PushProperty("CorrelationId", context.TraceIdentifier))
        {
            context.Response.Headers["X-Correlation-Id"] = context.TraceIdentifier;
            await _next(context);
        }
    }
}
