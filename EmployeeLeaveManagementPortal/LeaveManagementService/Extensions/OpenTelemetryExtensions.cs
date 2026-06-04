using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace LeaveManagementService.Extensions;

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
                .AddService("leavemanagement-service"))  // Service name in Jaeger UI
            .WithTracing(tracing =>
            {
                tracing

                    // Register custom ActivitySource for RabbitMQ message consumption
                    // Link to code in UserCreatedConsumer.cs that creates cosnumer spans
                    .AddSource("rabbitmq.consumer")

                    .AddSource("rabbitmq.publisher")

                    // Track incoming HTTP requests to the gateway
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;  // Capture exception details in traces
                    })

                    // Track outgoing HTTP calls to UserService (user validation)
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;

                        // Filter out Eureka service discovery calls from traces 
                        // Eureka heartbeats/registration would clutter Jaeger UI
                        options.FilterHttpRequestMessage = (httpRequestMessage) =>
                        {
                            return !httpRequestMessage.RequestUri?.ToString().Contains("/eureka/") ?? true;
                        };
                    })

                    // Track database queries via Entity Framework Core 
                    // Shows SQL queries (INSERT order, SELECT orders) and execution time
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = true; // Include SQL query text in traces
                    })

                    // Send all traces to Jaeger backend
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

        Log.Information("OpenTelemetry distributed tracing configured for LeaveManagementService");
        return services;
    }
}
