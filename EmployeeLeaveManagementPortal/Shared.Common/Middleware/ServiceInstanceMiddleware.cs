using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Shared.Common.Middleware;

/// <summary>
/// Middleware that adds service instance identification headers to responses
/// Useful for verifying load balancing and debugging
/// </summary>
public class ServiceInstanceMiddleware
{
    private readonly RequestDelegate _next;
    private const string ServiceInstanceHeader = "X-Service-Instance";
    private const string ServiceNameHeader = "X-Service-Name";
    private static readonly string HostName = Environment.MachineName;
    private static readonly string InstanceId = Guid.NewGuid().ToString("N")[..8]; // Short unique ID

    public ServiceInstanceMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        var serviceName = configuration.GetValue<string>("ServiceName")
                         ?? configuration.GetValue<string>("Eureka:Instance:AppName")
                         ?? "Unknown";

        // Add service identification headers to response
        context.Response.OnStarting(() =>
        {
            // Add hostname/container name
            context.Response.Headers.TryAdd(ServiceInstanceHeader, HostName);

            // Add service name
            context.Response.Headers.TryAdd(ServiceNameHeader, serviceName);

            // Add instance identifier (useful when multiple instances have same hostname)
            context.Response.Headers.TryAdd("X-Instance-Id", InstanceId);

            return Task.CompletedTask;
        });

        await _next(context);
    }
}
