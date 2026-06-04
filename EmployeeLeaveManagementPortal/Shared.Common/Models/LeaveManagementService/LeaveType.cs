namespace Shared.Common.Models.LeaveManagementService;

// Sick Leave, Casual Leave, Privilege Leave
public class LeaveType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DefaultAllocation { get; set; }
    public string? Description { get; set; } = string.Empty;
}


