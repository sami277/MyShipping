using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using MyShipping.Hubs;
using MyShipping.Models;
using MyShipping.Services;

namespace MyShipping.Tests;

public class NotificationServiceTests
{
    private static (NotificationService svc, Data.ApplicationDbContext db)
        Create(string dbName)
    {
        var db      = DbHelper.CreateInMemory(dbName);
        var clients = new Mock<IHubClients>();
        var client  = new Mock<IClientProxy>();

        clients.Setup(c => c.Group(It.IsAny<string>())).Returns(client.Object);

        var hub = new Mock<IHubContext<NotificationHub>>();
        hub.Setup(h => h.Clients).Returns(clients.Object);

        var svc = new NotificationService(db, hub.Object);
        return (svc, db);
    }

    [Fact]
    public async Task SendAsync_PersistsNotificationToDatabase()
    {
        var (svc, db) = Create("notif_persist");
        var user      = DbHelper.SeedUser(db);

        await svc.SendAsync(user.Id, "Hello", "World", NotificationType.General);

        var notif = await db.Notifications.SingleAsync(n => n.UserId == user.Id);
        Assert.Equal("Hello", notif.Title);
        Assert.Equal("World", notif.Message);
        Assert.False(notif.IsRead);
    }

    [Fact]
    public async Task GetUnreadCountAsync_ReturnsCorrectCount()
    {
        var (svc, db) = Create("notif_count");
        var user      = DbHelper.SeedUser(db);

        await svc.SendAsync(user.Id, "A", "msg 1");
        await svc.SendAsync(user.Id, "B", "msg 2");
        await svc.SendAsync(user.Id, "C", "msg 3");

        var count = await svc.GetUnreadCountAsync(user.Id);
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task MarkReadAsync_MarksSpecificNotificationRead()
    {
        var (svc, db) = Create("notif_markread");
        var user      = DbHelper.SeedUser(db);

        await svc.SendAsync(user.Id, "First",  "msg");
        await svc.SendAsync(user.Id, "Second", "msg");

        var first = await db.Notifications.FirstAsync(n => n.Title == "First");
        await svc.MarkReadAsync(first.Id, user.Id);

        db.Entry(first).Reload();
        Assert.True(first.IsRead);

        var second = await db.Notifications.FirstAsync(n => n.Title == "Second");
        Assert.False(second.IsRead);
    }

    [Fact]
    public async Task MarkAllReadAsync_MarksAllNotificationsRead()
    {
        var (svc, db) = Create("notif_markallread");
        var user      = DbHelper.SeedUser(db);

        await svc.SendAsync(user.Id, "A", "msg");
        await svc.SendAsync(user.Id, "B", "msg");
        await svc.SendAsync(user.Id, "C", "msg");

        await svc.MarkAllReadAsync(user.Id);

        var unread = await svc.GetUnreadCountAsync(user.Id);
        Assert.Equal(0, unread);
    }

    [Fact]
    public async Task MarkReadAsync_IgnoresNotificationFromDifferentUser()
    {
        var (svc, db) = Create("notif_crossuser");
        var user1     = DbHelper.SeedUser(db);
        var user2     = new ApplicationUser
        {
            Id       = Guid.NewGuid().ToString(),
            UserName = "other@example.com",
            Email    = "other@example.com",
            FullName = "Other User",
        };
        db.Users.Add(user2);
        await db.SaveChangesAsync();

        await svc.SendAsync(user1.Id, "Private", "msg");
        var notif = await db.Notifications.FirstAsync(n => n.UserId == user1.Id);

        // user2 tries to mark user1's notification as read
        await svc.MarkReadAsync(notif.Id, user2.Id);

        db.Entry(notif).Reload();
        Assert.False(notif.IsRead);   // should remain unread
    }
}
