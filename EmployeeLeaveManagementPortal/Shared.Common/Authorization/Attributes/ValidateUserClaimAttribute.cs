namespace Shared.Common.Authorization.Attributes
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Serilog;
    using System.Security.Claims;

    public class ValidateUserClaimAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string _instanceId = Environment.MachineName;

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                Log.Warning("[Instance: {InstanceId}] Invalid user claim. User Id claim not found", _instanceId);

                context.Result = new UnauthorizedObjectResult(
                    new
                    {
                        Message = "User claims missing or token expired. Try Login again."
                    });

                await Task.CompletedTask;
                return;
            }

            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                Log.Warning("[Instance: {InstanceId}] Invalid user Id claim: {UserIdClaim}", _instanceId, userIdClaim);
                context.Result = new UnauthorizedObjectResult(
                    new
                    {
                        Message = "Invalid user identity."
                    });

                await Task.CompletedTask;
            }
        }
    }
}
