using Shared.Common.Models.LeaveManagementService;

namespace Shared.Common.DTOs.Leaves;

public class LeaveBalanceDto
{
    public string LeaveType { get; set; } = null!;
    public int TotalAllocated { get; set; }
    public int UsedLeaves { get; set; }
    public int RemainingLeaves { get; set; }
}