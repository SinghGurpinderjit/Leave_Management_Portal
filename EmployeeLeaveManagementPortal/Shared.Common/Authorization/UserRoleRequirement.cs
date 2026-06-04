using Microsoft.AspNetCore.Authorization;

namespace Shared.Common.Authorization;

public class UserRoleRequirement : IAuthorizationRequirement
{
    public string Role { get; }

    public UserRoleRequirement(string role)
    {
        Role = role;
    }
}
