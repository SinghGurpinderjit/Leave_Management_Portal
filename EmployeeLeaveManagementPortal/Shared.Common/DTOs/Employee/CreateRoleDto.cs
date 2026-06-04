using Shared.Common.Models.EmployeeService;

namespace Shared.Common.DTOs.Employee;

/// <summary>
/// Role creation payload.
/// </summary>
public class CreateRoleDto
{
    public string Name { get; set; } = string.Empty;
}
