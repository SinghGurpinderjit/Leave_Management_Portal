namespace Shared.Common.DTOs.Employee;

/// <summary>
/// Manager + their direct/indirect team members.
/// Returned by GET /api/users/team-members
/// </summary>
public class TeamMembersDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Email { get; init; } = string.Empty;
    public List<UserSummaryDto> TeamMembers { get; init; } = [];
}
