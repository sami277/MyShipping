using MyShipping.Models;

namespace MyShipping.Services;

public interface INotificationService
{
    Task SendAsync(string userId, string title, string message,
        NotificationType type = NotificationType.General, int? shipmentId = null);
    Task MarkReadAsync(int notificationId, string userId);
    Task MarkAllReadAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
}
