using Microsoft.EntityFrameworkCore;
using Shared.Common.Utilities;
using Shared.Common.Extensions;
using UserService.Data;
using UserService.Repositories;
using UserService.Extensions;
using Serilog;
using Microsoft.AspNetCore.Authorization;
using Shared.Common.Authorization;
using UserService.Services;
using Shared.Common.Configuration;
using Shared.Common.Messaging;
using Microsoft.OpenApi.Models;
using Asp.Versioning;
using Shared.Common.MappingProfile;
using UserService.UnitOfWork;
using UserService.Services.Contracts;
using UserService.Repositories.Contracts;
using EmployeeService.Services;
using Shared.Common.Middleware;

var builder = WebApplication.CreateBuilder(args);


// ========== LOGGING CONFIGURATION ========== 
// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "UserService")
    .WriteTo.Console() // For Docker container logs
    .WriteTo.File("logs/userService-.txt", rollingInterval: RollingInterval.Day) // Daily log files
    .CreateLogger();

builder.Host.UseSerilog();

// ========== SERVICE REGISTRATION ==========
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("user-service", new OpenApiInfo
    {
        Title = "User Service",
        Description = "Handles users and roles"
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

// ==========DATABASE CONFIGURATION =================
// Configure Entity Framework Core with Postgresql
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<EmployeeDbContext>(options =>
    options.UseNpgsql(connectionString));

// ========= REPOSITORY PATTERN ===========
builder.Services.AddScoped<IUserRepository, EmployeeRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IEmployeeHierarchyService, EmployeeHierarchyService>();
builder.Services.AddScoped<IUserServiceManager, UserServiceManager>();
builder.Services.AddScoped<IRolesService, RolesService>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ========= JWT TOKEN GENERATION ===========
// Register JWT token generator for /login endpoint
// Singlton because it's stateless and thread-safe
builder.Services.AddSingleton<JwtTokenGenerator>();

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

// ========== OPENTELEMETRY & JAEGER TRACING ========== 
// Distributed tracing tracks order creation flow through multiple services 
// Configured via extension method in Extensions/OpenTelemetryExtensions.cs
builder.Services.AddOpenTelemetryTracing(builder.Configuration);

// ========== FLUENT VALIDATIONS =============
builder.Services.AddUserServiceFluentValidations();

// ========== RABBITMQ CONFIGURATION =================
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IMessagePublisher, RabbitMQPublisher>();

// ========== AUTO MAPPER ==================== 
builder.Services.AddAutoMapper(cfg => cfg.ShouldMapField = _ => false, typeof(MappingProfile));

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
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/user-service/swagger.json", "User Service API");
    });
}

app.UseCors();

// Handle all unhandled exception globally for each incoming request
app.UseMiddleware<CustomExceptionMiddleware>();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ServiceInstanceMiddleware>();
app.UseMiddleware<HealthCheckMiddleware>("UserService");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
Console.WriteLine($"=== CONNECTION STRING: {connStr} ===");

await app.InitializeUsersDatabase();
    
try
{
    Log.Information("Starting User Service");
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