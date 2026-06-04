namespace Shared.Common.Models.LeaveManagementService;

public class LeaveRequest
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int NumberOfDays { get; set; }
    public string? ApplicationReason { get; set; }
    public string? ReviewReason { get; set; } = string.Empty;
    public Guid? ReportingManagerId { get; set; }
    public LeaveStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedBy { get; set; }
}


