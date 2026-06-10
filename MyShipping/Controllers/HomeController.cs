using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyShipping.Data;
using MyShipping.Models;
using MyShipping.Models.ViewModels;
using MyShipping.Services;

namespace MyShipping.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly INotificationService _notifications;

    public HomeController(ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        INotificationService notifications)
    {
        _db            = db;
        _userManager   = userManager;
        _notifications = notifications;
    }

    public async Task<IActionResult> Index()
    {
        var userId = _userManager.GetUserId(User)!;

        var shipments = await _db.Shipments
            .Where(s => s.UserId == userId)
            .Include(s => s.Payment)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var monthlyStats = Enumerable.Range(0, 6)
            .Select(i =>
            {
                var month = now.AddMonths(-i);
                var items = shipments.Where(s =>
                    s.CreatedAt.Year == month.Year && s.CreatedAt.Month == month.Month).ToList();
                return new MonthlyStats
                {
                    Month         = month.ToString("MMM yyyy"),
                    ShipmentCount = items.Count,
                    Revenue       = items.Sum(s => s.ShippingCost),
                };
            })
            .OrderBy(m => m.Month)
            .ToList();

        var vm = new DashboardViewModel
        {
            TotalShipments       = shipments.Count,
            ActiveShipments      = shipments.Count(s => s.Status is
                ShipmentStatus.Pending or ShipmentStatus.Processing or
                ShipmentStatus.InTransit or ShipmentStatus.OutForDelivery),
            DeliveredShipments   = shipments.Count(s => s.Status == ShipmentStatus.Delivered),
            TotalRevenue         = shipments.Sum(s => s.ShippingCost),
            UnreadNotifications  = await _notifications.GetUnreadCountAsync(userId),
            RecentShipments      = shipments.Take(5).ToList(),
            MonthlyStats         = monthlyStats,
            StatusBreakdown      = Enum.GetValues<ShipmentStatus>()
                .ToDictionary(s => s.ToString(), s => shipments.Count(sh => sh.Status == s)),
        };

        return View(vm);
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
