namespace Shared.Common.DTOs.Leaves;

//public class CreateLeaveRequestDto
//{
//    public int LeaveTypeId { get; set; }
//    public DateOnly StartDate { get; set; }
//    public DateOnly EndDate { get; set; }
//    public int NumberOfDays { get; set; }
//    public string Reason { get; set; } = string.Empty;
//    public Guid? ReportingManagerId { get; set; }
//}


/// <summary>
/// Payload for POST /api/v1/leave-requests
/// Employee submits a new leave application.
/// </summary>
public class CreateLeaveRequestDto
{
    /// <summary>FK to LeaveType table (1 = Sick, 2 = Casual, 3 = Privilege, etc.)</summary>
    public int LeaveTypeId { get; set; }

    /// <summary>Inclusive start date. Cannot be in the past.</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Inclusive end date. Must be >= StartDate.</summary>
    public DateOnly EndDate { get; set; }

    /// <summary>
    /// Must exactly equal EndDate.DayNumber - StartDate.DayNumber + 1.
    /// Validated by FluentValidation so the client cannot pass a mismatched value.
    /// </summary>
    public int NumberOfDays { get; set; }

    /// <summary>Free-text reason for the leave. 10–500 characters.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Required when the employee has a manager.
    /// Must match the manager stored against the employee in UserService.
    /// </summary>
    public Guid? ReportingManagerId { get; set; }
}