using Asp.Versioning;
using LeaveManagementService.Data;
using LeaveManagementService.Extensions;
using LeaveManagementService.MappingProfile;
using LeaveManagementService.Models;
using LeaveManagementService.Repositories;
using LeaveManagementService.Services;
using LeaveManagementService.Services.Contracts;
using LeaveManagementService.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using Shared.Common.Authorization;
using Shared.Common.Configuration;
using Shared.Common.Extensions;
using Shared.Common.Messaging;
using Shared.Common.Middleware;
using Steeltoe.Common.Http.Discovery;

var builder = WebApplication.CreateBuilder(args);

// ========== LOGGING CONFIGURATION ========== 
// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "LeaveManagementService")
    .WriteTo.Console() // For Docker container logs
    .WriteTo.File("logs/leaveManagementService-.txt", rollingInterval: RollingInterval.Day) // Daily log files
    .CreateLogger();

builder.Host.UseSerilog();

// ========== SERVICE REGISTRATION ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Leave Management Service",
        Version = "v1",
        Description = "Handles leave requests for employees and managers"
    });

    // Define the JWT Bearer scheme
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token. Example: eyJhbGci..."
    });

    // Apply it globally — every endpoint requires auth unless explicitly opted out
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"       // must match the name in AddSecurityDefinition
                }
            },
            Array.Empty<string>()       // empty = no specific scopes required
        }
    });
});
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;   // fallback to v1 if no version sent
    options.ReportApiVersions = true;                     // adds api-supported-versions header in response
})
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";                   // v1, v2 format in Swagger
    options.SubstituteApiVersionInUrl = true;             // replaces {version} in route templates
});
builder.Services.AddHttpContextAccessor();

// ========== SERVICE DISCOVERY CONFIGURATION ========== 
// Bind ServiceDiscovery settings from appsettings.json 
// Determines whether to use Eureka (dynamic) or direct URLs (static) for calling UserService
builder.Services.Configure<ServiceDiscoverySettings>(builder.Configuration.GetSection("ServiceDiscovery"));
var serviceDiscoverySettings = builder.Configuration.GetSection("ServiceDiscovery").Get<ServiceDiscoverySettings>();

// ==========DATABASE CONFIGURATION =================
// Configure Entity Framework Core with Postgresql
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<LeavesMgmtDbContext>(options =>
    options.UseNpgsql(connectionString));

// ========= REPOSITORY PATTERN ===========
builder.Services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
builder.Services.AddScoped<ILeaveBalanceRepository, LeaveBalanceRepository>();
builder.Services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();

// ========= SERVICES PATTERN ===========
builder.Services.AddScoped<ILeaveBalanceService, LeaveBalanceService>();
builder.Services.AddScoped<ILeaveRequestService, LeaveRequestService>();
builder.Services.AddScoped<ILeaveTypeService, LeaveTypeService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ========== AUTHENTICATION =============
// Configure Jwt Bearer authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

// ========== AUTHORIZATION POLICIES =============
// Configure authorization policies by role: Employee or Manager
builder.Services.AddAuthorizationPolicies();
builder.Services.AddSingleton<IAuthorizationHandler, UserRoleRequirementHandler>();

// ========== SERVICE DISCOVERY ========== 
// Register with Eureka for service discovery
builder.Services.AddEurekaServiceDiscovery(builder.Configuration);

// ========== RABBITMQ MESSAGING ==========
// ADD RABBITMQ PUBLSIHER 
builder.Services.Configure<UserCreatedConsumerSettings>(builder.Configuration.GetSection("RabbitMQUserCreatedConsume"));
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQLeaveStatusPublish"));
builder.Services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();

// ADD RABBITMQ CONSUMER 
builder.Services.AddHostedService<UserCreatedConsumer>();

// ========== OPENTELEMETRY & JAEGER TRACING ========== 
// Distributed tracing tracks order creation flow through multiple services 
// Configured via extension method in Extensions/OpenTelemetryExtensions.cs
builder.Services.AddOpenTelemetryTracing(builder.Configuration);

// ========== HTTP CLIENT CONFIGURATION =============
// Configure HTTP client for calling UserService (inter-service communication) 
// 
// Service Discovery Integration: 
// - When UseEureka=true: Steeltoe's DiscoveryHttpClientHandler intercepts requests 
// * Resolves "USER-SERVICE" service name to actual instance URLs via Eureka 
// * Provides client-side load balancing across multiple UserService instances 
// - When UseEureka=false: Uses DirectUrl from configuration (e.g., http://user-service-1:8080) 
// 
// Resilience Patterns (configured via extension method in Extensions/ResiliencePolicyExtensions.cs): 
// - Retry Policy: Retries transient failures with exponential backoff + jitter 
// - Circuit Breaker: Simple (consecutive failures) or Advanced (failure rate in time window)
builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>(
    client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30); // Overall request timeout
    })
.AddServiceDiscovery() // Steeltoe integration for Eureka-based service resolution
.AddResiliencePolicies(builder.Configuration); // Polly retry + circuit breaker policies

Log.Information("UserServiceClient configured with {Mode} mode",
    serviceDiscoverySettings?.UseEureka == true ? "Eureka service name resolution" : "Direct URL");

//builder.Services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
//{
//    client.Timeout = TimeSpan.FromSeconds(30); // Overall request timeout
//});

// ========== FLUENT VALIDATIONS =============
builder.Services.AddLeaveManagementServiceFluentValidations();

// ========== AUTO MAPPER ==================== 
builder.Services.AddAutoMapper(cfg => cfg.ShouldMapField = _ => false, typeof(MappingProfile));


// ========= Add services to the container .==============
builder.Services.ConfigureInvalidModelStateOption();
builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

// CORS CONFIGURATION
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Handle all unhandled exception globally for each incoming request
app.UseMiddleware<CustomExceptionMiddleware>();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ServiceInstanceMiddleware>();
app.UseMiddleware<HealthCheckMiddleware>("LeaveManagementService");

app.UseAuthorization();
app.UseAuthorization();

app.MapControllers();

// ========== DATABASE INITIALIZATION ========== 
// Initialize Orders table schema on startup (thread-safe for multiple instances) 
// Configured via extension method in Extensions/DatabaseExtensions.cs
app.InitializeLeavesMgmtDatabase();

try
{
    Log.Information("Starting Leave Management Service");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Leave Management Service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}