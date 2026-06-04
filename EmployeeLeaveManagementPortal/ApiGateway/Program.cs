using Serilog;
using Shared.Common.Extensions;
using Ocelot.DependencyInjection;
using Ocelot.Provider.Eureka;
using Ocelot.Middleware;
using ApiGateway.Extensions;
using Shared.Common.Middleware;
using ApiGateway.Aggregators;

var builder = WebApplication.CreateBuilder(args);

// ========== OCELOT CONFIGURATION SELECTION ========== 
// Dynamically choose between static routing (hardcoded URLs) and Eureka-based service discovery 
// UseEureka flag is read from appsettings.json
var useEureka = builder.Configuration.GetValue<bool>("UseEureka", false);
var ocelotConfigFile = useEureka ? "ocelot.eureka.json" : "ocelot.static.json";

// Load the selected Ocelot configuration file 
// - ocelot.static.json: Contains DownstreamHostAndPorts with hardcoded service URLs 
// - ocelot.eureka.json: Contains ServiceName for dynamic discovery via Eureka
builder.Configuration.AddJsonFile(ocelotConfigFile, optional: false, reloadOnChange: true);


// ========== LOGGING CONFIGURATION ========== 
// Configure Serilog for structured logging with multiple sinks
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "ApiGateway")
    .WriteTo.Console() // For Docker container logs
    .WriteTo.File("logs/apigateway-.txt", rollingInterval: RollingInterval.Day) // Daily log files
    .CreateLogger();
builder.Host.UseSerilog();

// ========== JWT AUTHENTICATION ========== 
// Add JWT Bearer authentication using shared configuration extension 
// Reads JwtSettings from appsettings.json (SecretKey, Issuer, Audience)
builder.Services.AddJwtAuthentication(builder.Configuration);

// ========== SERVICE DISCOVERY ========== 
// Conditionally add Eureka service discovery based on UseEureka flag 
// When enabled: Gateway can discover and communicate with services registered in Eureka
if (useEureka)
{
    builder.Services.AddEurekaServiceDiscovery(builder.Configuration);
    Log.Information("API Gateway configured with Eureka service discovery");
}
else
{
    Log.Information("API Gateway configured with static routing");
}

// ========== OCELOT API GATEWAY SETUP ========== 
// Configure Ocelot with multiple features: 
// 1. Core gateway routing and request forwarding 
// 2. Request aggregation for combining multiple downstream API calls 
// 3. Load balancing across multiple service instances
var ocelotBuilder =builder.Services
                          .AddOcelot()
                          // Aggregates user + leaves request + leaves balances data in single request
                          .AddTransientDefinedAggregator<UserLeaveSummaryAggregator>(); 

// Add Eureka provider to Ocelot if service discovery is enabled 
// This allows Ocelot to resolve ServiceName from ocelot.eureka.json to actual service URLs
if (useEureka)
{
    ocelotBuilder.AddEureka();
    Log.Information("Ocelot configured with Eureka provider for dynamic ServiceName-based discovery");
}
else
{
    Log.Information("Ocelot configured with static DownstreamHostAndPorts");
}

// ========== CORS CONFIGURATION ========== 
// Allow all origins, methods, and headers for development 
// TODO: Restrict this in production to specific origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ========== OPENTELEMETRY & JAEGER TRACING ========== 
// Distributed tracing allows tracking requests as they flow through multiple services 
// Traces are exported to Jaeger UI (http://localhost:16686) for visualization 
// Configured via extension method in Extensions/OpenTelemetryExtensions.cs
builder.Services.AddOpenTelemetryTracing(builder.Configuration);

var app = builder.Build();

// ========== MIDDLEWARE PIPELINE ========== 
// ORDER IS CRITICAL! Middleware executes in the order added

// 1. Handle CORS preflight and add headers 
app.UseCors();

// 2.Handle all unhandled exception globally for each incoming request
app.UseMiddleware<CustomExceptionMiddleware>();

// 3. Add correlation ID for distributed tracing across services
app.UseMiddleware<CorrelationIdMiddleware>();

// 4. Add service instance identifier to response headers (for debugging load balancing)
app.UseMiddleware<ServiceInstanceMiddleware>();

// 5. Intercept /health endpoint and return health status
app.UseMiddleware<HealthCheckMiddleware>("ApiGateway", useEureka ? "Eureka" : "Static");

// 6. Validate JWT tokens and populate User claims
app.UseAuthentication();

// 7. (Optional) Log all JWT claims for debugging authentication issues //
app.UseMiddleware<ClaimsLoggingMiddleware>(); // Uncomment for debugging JWT claims 

// 8. Check authorization rules (used by Ocelot's RouteClaimsRequirement)
app.UseAuthorization();

// Add services to the container.

try
{
    Log.Information("Starting API Gateway with {Mode} routing", useEureka ? "Eureka" : "Static");

    // ========== OCELOT MIDDLEWARE ========== 
    // MUST BE LAST! Routes all requests through Ocelot 
    // - Matches UpstreamPathTemplate from config 
    // - Applies authentication/authorization rules (RouteClaimsRequirement) 
    // - Forwards to downstream services 
    // - Applies QoS policies (retry, circuit breaker) 
    // - Handles request aggregation
    
    await app.UseOcelot();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "API Gateway terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}