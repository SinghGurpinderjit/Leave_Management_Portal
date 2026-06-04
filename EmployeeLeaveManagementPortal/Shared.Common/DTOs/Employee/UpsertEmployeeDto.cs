namespace Shared.Common.DTOs.Employee;

public class UpsertEmployeeDto
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
}