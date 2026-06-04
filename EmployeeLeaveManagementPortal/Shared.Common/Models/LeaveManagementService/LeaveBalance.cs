namespace Shared.Common.Models.LeaveManagementService;

public class LeaveBalance
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public LeaveType? LeaveType { get; set; } = null;
    public int UsedLeaves { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}


