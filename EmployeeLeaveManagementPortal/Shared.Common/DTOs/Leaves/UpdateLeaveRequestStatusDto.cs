using Shared.Common.Models.LeaveManagementService;
using System.Text.Json.Serialization;

namespace Shared.Common.DTOs.Leaves;

///[RequiredIfRejected]
//public class UpdateLeaveRequestStatusDto
//{
//    //public Guid EmployeeId { get; set; }

//    //[JsonConverter(typeof(JsonStringEnumConverter))]
//    //public LeaveStatus Status { get; set; }

//    public string? Reason { get; set; }
//}


public class UpdateLeaveRequestStatusDto
{
    /// <summary>
    /// Target status. Managers may only set Approved or Rejected.
    /// Validated by FluentValidation — Pending/Cancelled are rejected.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Mandatory when Status == "Rejected". Optional for Approved.
    /// 10–500 characters when provided.
    /// </summary>
    public string? Reason { get; set; }
}
