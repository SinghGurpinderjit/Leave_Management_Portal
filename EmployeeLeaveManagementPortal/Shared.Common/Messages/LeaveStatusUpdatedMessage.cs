using Shared.Common.DTOs.Employee;
using Shared.Common.DTOs.Leaves;
using Shared.Common.Models.LeaveManagementService;

namespace Shared.Common.Messages;

/// <summary>
/// Message published to RabbitMQ on any leave status change.
/// Consumed by NotificationService which logs the notification.
/// 
/// Routing keys:
///   leave.applied   → when employee submits a request
///   leave.approved  → when manager approves
///   leave.rejected  → when manager rejects
/// </summary>
public class LeaveNotificationMessage
{
    public Guid LeaveRequestId { get; init; }
    public Guid EmployeeId { get; init; }
    public string EmployeeEmail { get; init; } = string.Empty;
    public string? ManagerEmail { get; init; } = null;

    public Guid? ManagerId { get; init; }

    public string LeaveType { get; init; } = string.Empty;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public int NumberOfDays { get; init; }
    public string Status { get; init; } = string.Empty;

    public string? ReviewReason { get; init; }

    public LeaveNotificationType NotificationType { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
