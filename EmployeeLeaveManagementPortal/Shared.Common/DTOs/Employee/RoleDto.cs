namespace Shared.Common.DTOs.Employee;

/// <summary>
/// Role response shape.
/// </summary>
public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
