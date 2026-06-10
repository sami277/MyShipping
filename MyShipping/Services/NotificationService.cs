using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using MyShipping.Data;
using MyShipping.Hubs;
using MyShipping.Models;

namespace MyShipping.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;

    public NotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hub)
    {
        _db  = db;
        _hub = hub;
    }

    public async Task SendAsync(string userId, string title, string message,
        NotificationType type = NotificationType.General, int? shipmentId = null)
    {
        var notification = new Notification
        {
            UserId     = userId,
            Title      = title,
            Message    = message,
            Type       = type,
            ShipmentId = shipmentId,
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync();

        // Push real-time via SignalR to the user's private group
        await _hub.Clients.Group(userId).SendAsync("ReceiveNotification", new
        {
            notification.Id,
            notification.Title,
            notification.Message,
            Type      = notification.Type.ToString(),
            notification.ShipmentId,
            CreatedAt = notification.CreatedAt.ToString("o"),
        });
    }

    public async Task MarkReadAsync(int notificationId, string userId)
    {
        var notification = await _db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (notification is not null)
        {
            notification.IsRead = true;
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkAllReadAsync(string userId)
    {
        var unread = await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
            n.IsRead = true;

        await _db.SaveChangesAsync();
    }

    public async Task<int> GetUnreadCountAsync(string userId) =>
        await _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
}
