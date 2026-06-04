using Shared.Common.DTOs;

namespace Shared.Common.Messages;

public class UserCreatedMessage
{
    public Guid EmployeeId { get; set; }
    public string EmployeeEmail { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }

}
