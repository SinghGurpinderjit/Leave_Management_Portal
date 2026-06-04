namespace Shared.Common.DTOs.Employee;

/// <summary>
/// User info with role — returned by /api/users (admin/manager list views).
/// </summary>
public class UserRolesInfoDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public Guid? ManagerId { get; init; }
}

