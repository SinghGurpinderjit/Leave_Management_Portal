using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace ApiGateway.Extensions;

/// <summary>
/// Extension methods for configuring OpenTelemetry distributed tracing with Jaeger
/// Tracks gateway requests and downstream service calls
/// </summary>
public static class OpenTelemetryExtensions
{
    /// <summary>
    /// Adds OpenTelemetry tracing with Jaeger exporter for API Gateway
    /// Instruments: ASP.NET Core (incoming requests), HttpClient (downstream calls)
    /// </summary>
    public static IServiceCollection AddOpenTelemetryTracing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService("api-gateway"))  // Service name in Jaeger UI
            .WithTracing(tracing =>
            {
                tracing
                    // Track incoming HTTP requests to the gateway
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;  // Capture exception details in traces
                    })

                    // Track outgoing HTTP calls to downstream services (UserService, OrderService)
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;  // Capture errors when calling downstream services

                        // Filter out Eureka service discovery calls (heartbeats, registration)
                        // These create noise in Jaeger UI and aren't relevant for business logic tracing
                        options.FilterHttpRequestMessage = (httpRequestMessage) =>
                        {
                            return !httpRequestMessage.RequestUri?.ToString().Contains("/eureka/") ?? true;
                        };
                    })

                    // Export all traces to Jaeger backend
                    .AddOtlpExporter(options =>
                    {
                        var agentHost = configuration["JAEGER_AGENT_HOST"] ?? "localhost";
                        var agentPort = int.Parse(configuration["JAEGER_AGENT_PORT"] ?? "6831");

                        var endpoint = $"http://{agentHost}:{agentPort}";

                        options.Endpoint = new Uri(endpoint);

                        // gRPC transport
                        options.Protocol = OtlpExportProtocol.Grpc;

                        Log.Information("Jaeger tracing: {Host}:{Port}", agentHost, agentPort);
                    });
            });

        Log.Information("OpenTelemetry distributed tracing configured for API Gateway");
        return services;
    }
}
