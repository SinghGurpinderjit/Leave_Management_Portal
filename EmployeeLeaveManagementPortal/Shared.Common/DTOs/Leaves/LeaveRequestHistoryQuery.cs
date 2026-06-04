namespace Shared.Common.DTOs.Leaves;

/// <summary>
/// Query parameters for GET /api/v1/leave-requests/history
/// All fields are optional; defaults give the first page of all statuses.
/// Example: ?status=Approved&page=2&pageSize=5&startDate=2025-01-01&endDate=2025-06-30
/// </summary>
public class LeaveRequestHistoryQuery: LeaveRequestQueryBase
{
}

/// <summary>
/// Query parameters for GET /api/v1/leave-requests/team (Manager only)
/// Supports all filters that an employee's history query supports, plus employee targeting.
/// 
/// Example: ?status=Pending&employeeId=abc-123&fromDate=2025-01-01
/// </summary>
public class TeamLeaveRequestQuery: LeaveRequestQueryBase
{
    /// <summary>
    /// Optional: filter to a specific team member.
    /// If omitted, returns requests for all team members.
    /// </summary>
    public Guid? EmployeeId { get; set; }

    ///// <summary>Filter by status. Same values as LeaveRequestHistoryQuery.Status.</summary>
    //public string? Status { get; set; }

    ///// <summary>Filter: only requests whose StartDate >= this value.</summary>
    //public DateOnly? FromDate { get; set; }

    ///// <summary>Filter: only requests whose EndDate <= this value.</summary>
    //public DateOnly? ToDate { get; set; }

    ///// <summary>1-based page number. Default: 1.</summary>
    //public int Page { get; set; } = 1;

    ///// <summary>Items per page. 1–100. Default: 10.</summary>
    //public int PageSize { get; set; } = 10;
}


