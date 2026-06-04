namespace Shared.Common.DTOs.Leaves;

//public class LeaveRequestHistoryQuery
//{
//    public string Status { get; set; }
//    public int Page { get; set; } = 1;
//    public int PageSize { get; set; } = 10;
//}

public class LeaveRequestQueryBase
{
    /// <summary>
    /// Filter by status. Accepted values: All | Pending | Approved | Rejected | Cancelled.
    /// Null or "All" returns every status.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Filter: only requests whose StartDate >= this value.
    /// </summary>
    public DateOnly? FromDate { get; set; }

    /// <summary>
    /// Filter: only requests whose EndDate <= this value.
    /// </summary>
    public DateOnly? ToDate { get; set; }

    /// <summary>
    /// 1-based page number. Default: 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Items per page. 1–100. Default: 10.
    /// </summary>
    public int PageSize { get; set; } = 10;
}


