using Microsoft.AspNetCore.Authorization;
using Shared.Common.Constants;

namespace Shared.Common.Authorization;

public class UserRoleRequirementHandler : AuthorizationHandler<UserRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserRoleRequirement requirement)
    {
        var roles = context.User
                           .FindAll(AppConstants.jwtCustomUserRoleClaimName)
                           .Select(x => x.Value);

        if (roles.Contains(requirement.Role))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
