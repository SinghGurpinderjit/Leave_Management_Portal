using Shared.Common.Models;

namespace Services.NotificationService.Extensions;

public interface INotificationService
{
    Task<Notification> SendNotificationAsync(Notification notification);
    Task<IEnumerable<Notification>> GetNotificationsByUserAsync(Guid userId);
}
