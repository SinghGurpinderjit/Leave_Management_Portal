using Shared.Common.DTOs.Employee;

namespace Shared.Common.DTOs.Leaves;

public class TeamLeaveRequestSummaryDto
{
    public Guid LeaveRequestId { get; init; }
    public UserDtoBase EmployeeInfo { get; init; } = new();
    //public Guid EmployeeId { get; init; }
    //public string EmployeeName { get; init; } = string.Empty;
    public string LeaveType { get; init; } = string.Empty;
    public string StartDate { get; init; }
    public string EndDate { get; init; }
    public int NumberOfDays { get; init; }
    public string Status { get; init; } = string.Empty;

    public string? ApplicationReason { get; init; } = string.Empty;
    public string? ReviewReason { get; init; } = string.Empty;

    public Guid? ReportingManagerId { get; init; }
    public string? UpdatedAt { get; set; }


    /// <summary>UTC timestamp of submission.</summary>
    public string CreatedAt { get; init; }
}
