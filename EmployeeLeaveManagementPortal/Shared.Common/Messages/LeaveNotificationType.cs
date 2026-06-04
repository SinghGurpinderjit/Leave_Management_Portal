namespace Shared.Common.Messages;

/// <summary>
/// Categorises the notification so the consumer can log a tailored message.
/// </summary>
public enum LeaveNotificationType
{
    LeaveApplied,
    LeaveApproved,
    LeaveRejected
}
