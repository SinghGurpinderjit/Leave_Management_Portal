using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Diagnostics;

namespace Shared.Common.Middleware;

/// <summary>
/// Middleware that ensures every request has a correlation ID for distributed tracing.
/// 
/// Purpose:
/// - Enables tracking of requests across multiple microservices
/// - If client provides X-Correlation-ID header, it's preserved and propagated
/// - If not provided, a new GUID is generated
/// - Correlation ID is added to response headers, Serilog log context, and OpenTelemetry spans
/// 
/// Usage in distributed tracing:
/// 1. Client sends request with X-Correlation-ID
/// 2. API Gateway forwards it to downstream services
/// 3. All services log with same correlation ID
/// 4. Correlation ID is added to Jaeger spans for trace aggregation
/// 5. Logs can be filtered/searched by correlation ID to trace entire request flow
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private const string CorrelationIdHeader = "X-Correlation-ID";

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get existing correlation ID from request header or generate new one
        var correlationId = GetOrGenerateCorrelationId(context);

        // Add correlation ID to response headers so client can track the request
        context.Response.OnStarting(() =>
        {
            context.Response.Headers.TryAdd(CorrelationIdHeader, correlationId);
            return Task.CompletedTask;
        });

        // ========== INTEGRATE WITH OPENTELEMETRY/JAEGER ==========
        // Add correlation ID to current Activity (OpenTelemetry span)
        var activity = Activity.Current;
        if (activity != null)
        {
            // SetTag: Adds correlation ID as a span tag (visible in Jaeger UI)
            activity.SetTag("correlation.id", correlationId);

            // SetBaggage: Propagates correlation ID to all child spans automatically
            // This ensures inter-service calls maintain the same correlation ID
            activity.SetBaggage("correlation.id", correlationId);
        }

        // Push correlation ID to Serilog context - all logs in this request will include it
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }

    /// <summary>
    /// Extracts correlation ID from request header if present, otherwise generates new GUID
    /// </summary>
    private string GetOrGenerateCorrelationId(HttpContext context)
    {
        // Check if client already provided a correlation ID
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            return correlationId.ToString();
        }

        // Generate new correlation ID for this request chain
        return Guid.NewGuid().ToString();
    }
}
