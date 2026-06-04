using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Shared.Common.Middleware;

/// <summary>
/// Lightweight health check middleware that responds to /health endpoint.
/// 
/// Purpose:
/// - Provides quick health status endpoint for monitoring tools (Docker, Kubernetes, load balancers)
/// - Returns service name, status, and timestamp
/// - Does not check database connections or external dependencies (fast response)
/// - Used in docker-compose.yml healthcheck and by monitoring systems
/// 
/// Response example:
/// {
///   "status": "Healthy",
///   "service": "UserService",
///   "timestamp": "2026-05-06T10:30:00Z",
///   "additionalInfo": "Static" // Optional mode info (Static/Eureka)
/// }
/// </summary>
public class HealthCheckMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _serviceName;
    private readonly string? _additionalInfo;

    /// <summary>
    /// Initializes health check middleware with service identification.
    /// </summary>
    /// <param name="serviceName">Name of the service (e.g., "UserService", "ApiGateway")</param>
    /// <param name="additionalInfo">Optional info like routing mode ("Static"/"Eureka")</param>
    public HealthCheckMiddleware(RequestDelegate next, IConfiguration configuration, string serviceName, string? additionalInfo = null)
    {
        _next = next;
        _serviceName = serviceName;
        _additionalInfo = additionalInfo;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Intercept /health requests and return status immediately
        // Short-circuits the pipeline - other middleware won't execute for health checks
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            context.Response.ContentType = "application/json";

            var response = new
            {
                status = "Healthy",
                service = _serviceName,
                timestamp = DateTime.UtcNow,
                additionalInfo = _additionalInfo
            };

            await context.Response.WriteAsJsonAsync(response);
            return; // Don't call _next - request is complete
        }

        // Not a health check request, continue to next middleware
        await _next(context);
    }
}
