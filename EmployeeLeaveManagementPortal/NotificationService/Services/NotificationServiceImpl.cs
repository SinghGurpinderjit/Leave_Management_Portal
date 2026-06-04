using Shared.Common.Models;

namespace Services.NotificationService.Extensions
{
    public class NotificationServiceImpl : INotificationService
    {
        private readonly ILogger<NotificationServiceImpl> _logger;
        private static readonly List<Notification> _notifications = new();

        public NotificationServiceImpl(ILogger<NotificationServiceImpl> logger)
        {
            _logger = logger;
        }

        public async Task<Notification> SendNotificationAsync(Notification notification)
        {
            notification.Id = Guid.NewGuid();
            notification.CreatedAt = DateTime.UtcNow;

            // Simulate Sending notification
            await Task.Delay(100); // Simulate async operation

            switch (notification.Type)
            {
                case NotificationType.Email:
                    _logger.LogInformation(
                        "Sending EMAIL to {Recepient}: {Subject} - {Message}",
                        notification.Recepient,
                        notification.Subject,
                        notification.Message
                        );
                    break;
                case NotificationType.SMS:
                    _logger.LogInformation(
                        "Sending SMS to {Recepient}: {Message}",
                        notification.Recepient,
                        notification.Message
                        );
                    break;
                case NotificationType.Push:
                    _logger.LogInformation(
                        "Sending PUSH notification to {Recepient}: {Message}",
                        notification.Recepient,
                        notification.Message
                        );
                    break;
            }

            notification.IsSent = true;
            notification.SentAt = DateTime.UtcNow;

            // Store notification
            _notifications.Add(notification);

            _logger.LogInformation("Notification sent successfully: {NotificationId}", notification.Id);
            return notification;
        }

        public async Task<IEnumerable<Notification>> GetNotificationsByUserAsync(Guid userId)
        {
            await Task.CompletedTask;
            return _notifications.Where(n => n.EmployeeId == userId).ToList();
        }
    }
}
