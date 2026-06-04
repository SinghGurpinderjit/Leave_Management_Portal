using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Shared.Common.Authorization;
using Shared.Common.Configuration;
using Shared.Common.DTOs;
using Shared.Common.Validators;
using Steeltoe.Discovery.Client;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using static Shared.Common.Constants.AppConstants;

namespace Shared.Common.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Configures JWT Bearer authentication for the service.
    /// 
    /// Configuration:
    /// - Reads JwtSettings from appsettings.json (SecretKey, Issuer, Audience, ExpirationMinutes)
    /// - Validates issuer, audience, lifetime, and signature on incoming JWT tokens
    /// - Uses HS256 (HMAC-SHA256) symmetric key algorithm
    /// 
    /// Custom claim names:
    /// - We use "user_role" instead of standard "role" claim to avoid ASP.NET Core transformations
    /// - Standard claims get transformed to long Microsoft URIs which break Ocelot's RouteClaimsRequirement
    /// 
    /// Usage: Called in Program.cs of all services (ApiGateway, UserService, OrderService)
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Load JWT settings from appsettings.json, throw if missing
        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException("Jwt settings not configured");

        // Registe JwtSettings for dependency injection
        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

        // Configure Jwt Bearer authentication as default scheme
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            // Configure token validation parameters - all must pass for autentication to succeed
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,                // Verify token was issued by our system
                ValidateAudience = true,              // Verify token is intended for our services
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,     // Expected Issuer: "MicroservicesArchitecture"
                ValidAudience = jwtSettings.Audience, // Expected auience: "MicroservicesClient"
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey)) // same secret key used for signing
            };

            options.Events = new JwtBearerEvents
            {
                OnForbidden = context =>
                {
                    context.Response.StatusCode = 403;
                    context.Response.ContentType = "application/json";

                    return context.Response.WriteAsync("""
                        {
                            "message": "You are not authorized to access this resource."
                        }
                    """);
                }
            };
        });

        return services;
    }

    public static void AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(CustomJwtPolicy.EmployeeOnly, policy =>
            {
                policy.Requirements.Add(new UserRoleRequirement(UserRole.Employee));
            });

            options.AddPolicy(CustomJwtPolicy.ManagerOnly, policy =>
            {
                policy.Requirements.Add(new UserRoleRequirement(UserRole.Manager));
            });

            //options.AddPolicy(CustomJwtPolicy.EmployeeOrManagerOnly, policy =>
            //{
            //    policy.Requirements.Add(
            //        new UserRoleRequirement(UserRole.Manager));
            //    policy.Requirements.Add(
            //        new UserRoleRequirement(UserRole.Employee));
            //});

            //options.AddPolicy(CustomJwtPolicy.AdminOnly, policy =>
            //{
            //    policy.Requirements.Add(new UserRoleRequirement(UserRole.Admin));
            //});
        });
    }

    public static void AddUserServiceFluentValidations(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginDtoValidator>();
    }

    public static void AddLeaveManagementServiceFluentValidations(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<CreateLeaveRequestDtoValidator>();
    }

    /// <summary>
    /// Configures Eureka service discovery client using Steeltoe.
    /// 
    /// Purpose:
    /// - Registers service with Eureka server for discovery by other services
    /// - Fetches registry of available service instances for client-side load balancing
    /// - Sends heartbeats to maintain registration
    /// 
    /// Configuration:
    /// - Reads Eureka settings from appsettings.json (ServiceUrl, AppName, etc.)
    /// - Used when UseEureka=true in configuration
    /// 
    /// Usage: Called conditionally in Program.cs when dynamic service discovery is enabled
    /// </summary>
    public static IServiceCollection AddEurekaServiceDiscovery(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Steeltoe's AddDiscoveryClient reads Eureka configuration and sets up client
        services.AddDiscoveryClient(configuration);
        return services;
    }

    public static void ConfigureInvalidModelStateOption(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                var response = new ApiResponse<List<string>>()
                    .Fail(HttpStatusCode.BadRequest, errors);

                return new ObjectResult(response)
                {
                    StatusCode = (int)HttpStatusCode.BadRequest
                };
            };
        });
    }
}
