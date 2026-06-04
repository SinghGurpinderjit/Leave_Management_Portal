namespace Shared.Common.DTOs.Leaves;

//public class CancelLeaveRequestDto
//{
//    public string Reason { get; set; } = string.Empty;
//}


/// <summary>
/// Payload for PATCH /api/v1/leave-requests/{id}/cancel (Employee only)
/// </summary>
public class CancelLeaveRequestDto
{
    /// <summary>Why the employee is cancelling. 10–500 characters.</summary>
    public string Reason { get; set; } = string.Empty;
}
