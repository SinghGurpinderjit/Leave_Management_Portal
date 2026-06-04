using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace Shared.Common.Middleware;

/// <summary>
/// DEBUG ONLY: Middleware that logs all JWT claims for authenticated requests.
/// 
/// Purpose:
/// - Helps debug JWT authentication and authorization issues
/// - Shows all claims extracted from JWT token
/// - Highlights role claims specifically for authorization debugging
/// - Should be disabled in production (creates verbose logs)
/// 
/// Usage:
/// - Add after UseAuthentication() in Program.cs
/// - Comment out when authorization is working correctly
/// - Useful for debugging:
///   * Missing claims
///   * Claim name transformations (role vs user_role)
///   * Authorization failures (RouteClaimsRequirement)
/// </summary>
public class ClaimsLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ClaimsLoggingMiddleware> _logger;

    public ClaimsLoggingMiddleware(RequestDelegate next, ILogger<ClaimsLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only log for authenticated requests (JWT token present and valid)
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            _logger.LogInformation("========== AUTHENTICATED USER CLAIMS ==========");
            _logger.LogInformation("User Identity Name: {Name}", context.User.Identity.Name ?? "NULL");
            _logger.LogInformation("Authentication Type: {Type}", context.User.Identity.AuthenticationType ?? "NULL");

            // Log all claims from the JWT token
            _logger.LogInformation("All Claims ({Count}):", context.User.Claims.Count());
            foreach (var claim in context.User.Claims)
            {
                _logger.LogInformation("  Claim Type: [{Type}] = Value: [{Value}]", claim.Type, claim.Value);
            }

            // Specifically highlight role claims for authorization debugging
            // This helps verify that Ocelot's RouteClaimsRequirement can find the role
            var roleClaims = context.User.Claims.Where(c => c.Type == "user_role" || c.Type.Contains("role")).ToList();
            if (roleClaims.Any())
            {
                _logger.LogInformation("✅ ROLE CLAIMS FOUND:");
                foreach (var role in roleClaims)
                {
                    _logger.LogInformation("  ✅ Role Claim: [{Type}] = [{Value}]", role.Type, role.Value);
                }
            }
            else
            {
                _logger.LogWarning("❌ NO ROLE CLAIMS FOUND! Searched for claim type 'user_role'");
            }

            _logger.LogInformation("===============================================");
        }
        else
        {
            // Log unauthenticated requests for debugging
            _logger.LogInformation("Request to {Path} - User not authenticated", context.Request.Path);
        }

        await _next(context);
    }
}
