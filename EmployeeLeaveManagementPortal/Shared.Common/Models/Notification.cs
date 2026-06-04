namespace Shared.Common.Models;

public class Notification
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public NotificationType Type { get; set; }
    public string Recepient { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsSent { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
}

public enum NotificationType
{
    Email,
    SMS,
    Push
}
