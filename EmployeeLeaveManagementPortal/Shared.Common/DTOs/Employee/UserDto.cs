namespace Shared.Common.DTOs.Employee;

/// <summary>
/// Full user detail — returned by GET /api/users/{id}
/// </summary>
public class UserDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;

    /// <summary>
    /// Null when the user has no manager (e.g. top-level manager).
    /// </summary>
    public UserSummaryDto? Manager { get; init; }
}

