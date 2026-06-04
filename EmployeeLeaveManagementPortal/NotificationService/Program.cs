using NotificationService.Extensions;
using Serilog;
using Services.NotificationService.Extensions;
using Services.NotificationService.Extensions.NotificationService.Services;
using Shared.Common.Configuration;
using Shared.Common.Middleware;
using Steeltoe.Discovery.Client;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "NotificationService")
    .WriteTo.Console()
    .WriteTo.File("logs/notificationservice-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container (minimal for health endpoint)
builder.Services.AddControllers();

// Add application services
builder.Services.AddSingleton<INotificationService, NotificationServiceImpl>();

// Add Eureka Service Discovery
//builder.Services.AddEurekaServiceDiscovery(builder.Configuration);
builder.Services.AddDiscoveryClient(builder.Configuration);

// Configure RabbitMQ
//builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.Configure<LeaveStatusUpdatedConsumerSettings>(builder.Configuration.GetSection("RabbitMQ:LeaveStatusUpdatedConsumer"));

// ADD RABBITMQ CONSUMER 
builder.Services.AddHostedService<LeaveStatusUpdatedConsumer>();

// ========== OPENTELEMETRY & JAEGER TRACING ========== //
// Distributed tracing for tracking notification processing from RabbitMQ messages 
// Configured via extension method in Extensions/OpenTelemetryExtensions.cs
builder.Services.AddOpenTelemetryTracing(builder.Configuration); 

var app = builder.Build();

// Handle all unhandled exception globally for each incoming request
app.UseMiddleware<CustomExceptionMiddleware>();

// Configure the HTTP request pipeline (minimal for health endpoint)
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ServiceInstanceMiddleware>(); 
app.UseMiddleware<HealthCheckMiddleware>("NotificationService"); 
app.MapControllers();

if (app.Environment.IsDevelopment())
{
}

try
{
    Log.Information("Starting Notification Service");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "User Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}