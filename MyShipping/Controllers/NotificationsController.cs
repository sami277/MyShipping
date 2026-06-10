using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShipping.Data;
using MyShipping.Models;
using MyShipping.Services;

namespace MyShipping.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notificationService;

    public NotificationsController(ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        INotificationService notificationService)
    {
        _db                  = db;
        _userManager         = userManager;
        _notificationService = notificationService;
    }

    // GET /Notifications
    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;
        var notifications = await _db.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return View(notifications);
    }

    // POST /Notifications/MarkRead/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        await _notificationService.MarkReadAsync(id, userId);
        return RedirectToAction("Index");
    }

    // POST /Notifications/MarkAllRead
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = _userManager.GetUserId(User)!;
        await _notificationService.MarkAllReadAsync(userId);
        return RedirectToAction("Index");
    }

    // GET /Notifications/GetUnreadCount  (AJAX endpoint)
    [HttpGet]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = _userManager.GetUserId(User)!;
        var count  = await _notificationService.GetUnreadCountAsync(userId);
        return Json(new { count });
    }
}
