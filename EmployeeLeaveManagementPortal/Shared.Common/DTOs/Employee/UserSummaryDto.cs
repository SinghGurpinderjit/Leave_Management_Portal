namespace Shared.Common.DTOs.Employee;

/// <summary>
/// Minimal user info embedded inside other responses (e.g. LoginResponseDto, team lists).
/// </summary>
public class UserSummaryDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}

