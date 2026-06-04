namespace Shared.Common.DTOs.Employee;

public class UserDtoBase
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
