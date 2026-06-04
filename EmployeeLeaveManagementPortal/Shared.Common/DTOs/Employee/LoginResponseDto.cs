namespace Shared.Common.DTOs.Employee;

/// <summary>
/// Response for POST /api/users/login
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// Signed JWT. Include in every subsequent request as:
    /// Authorization: Bearer {Token}
    /// </summary>
    public string Token { get; init; } = string.Empty;

    /// <summary>
    /// UTC expiry of the token. Clients should re-login before this time.
    /// </summary>
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// Logged-in user summary.
    /// </summary>
    public UserSummaryDto User { get; init; } = null!;
}

