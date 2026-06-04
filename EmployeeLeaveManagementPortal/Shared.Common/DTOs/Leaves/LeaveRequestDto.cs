namespace Shared.Common.DTOs.Leaves;

public class LeaveRequestDto
{
    public Guid LeaveRequestId { get; init; }
    public Guid EmployeeId { get; init; }

    /// <summary>
    /// Display name of the leave type (e.g. "Sick Leave").
    /// </summary>
    public string LeaveType { get; init; } = string.Empty;

    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public int NumberOfDays { get; init; }

    /// <summary>
    /// Current status string: Pending | Approved | Rejected | Cancelled.
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Employee's stated reason for the leave.
    /// </summary>
    public string ApplicationReason { get; init; } = string.Empty;
    public string ReviewReason { get; init; } = string.Empty;

    /// <summary>The manager this request was submitted to.</summary>
    public Guid? ReportingManagerId { get; init; }

    /// <summary>
    /// UTC timestamp when the request was created.
    /// </summary>
    public string CreatedAt { get; init; } = string.Empty;

    /// <summary>
    /// UTC timestamp of the last status change. Null if never updated.
    /// </summary>
    public string? UpdatedAt { get; init; }

}
//public class LeaveRequestDto
//{
//    public Guid LeaveRequestId { get; set; }
//    public Guid EmployeeId { get; set; }
//    public string LeaveType { get; set; }
//    public DateOnly StartDate { get; set; }
//    public DateOnly EndDate { get; set; }
//    public string Status { get; set; } = string.Empty;
//    public string Reason { get; set; } = string.Empty;
//}



// ── Query filters ─────────────────────────────────────────────────────────────
