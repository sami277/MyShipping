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
public class ShipmentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPaymentService _payment;
    private readonly INotificationService _notifications;

    public ShipmentsController(ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IPaymentService payment,
        INotificationService notifications)
    {
        _db            = db;
        _userManager   = userManager;
        _payment       = payment;
        _notifications = notifications;
    }

    // GET /Shipments
    public async Task<IActionResult> Index(string? status, string? search, int page = 1)
    {
        const int pageSize = 10;
        var userId = _userManager.GetUserId(User)!;

        var query = _db.Shipments
            .Where(s => s.UserId == userId)
            .Include(s => s.Payment)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(s =>
                s.TrackingNumber.Contains(search) ||
                s.RecipientName.Contains(search) ||
                s.RecipientAddress.Contains(search));

        if (Enum.TryParse<ShipmentStatus>(status, out var parsedStatus))
            query = query.Where(s => s.Status == parsedStatus);

        var total = await query.CountAsync();

        var shipments = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.Page      = page;
        ViewBag.PageSize  = pageSize;
        ViewBag.Total     = total;
        ViewBag.Status    = status;
        ViewBag.Search    = search;

        return View(shipments);
    }

    // GET /Shipments/Details/5
    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User)!;
        var shipment = await _db.Shipments
            .Include(s => s.Payment)
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (shipment is null) return NotFound();
        return View(shipment);
    }

    // GET /Shipments/Create
    public IActionResult Create() => View(new CreateShipmentViewModel());

    // POST /Shipments/Create
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateShipmentViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var userId = _userManager.GetUserId(User)!;
        var cost   = _payment.CalculateShippingCost(vm.Weight, vm.ServiceType);

        var shipment = new Shipment
        {
            TrackingNumber    = GenerateTrackingNumber(),
            SenderName        = vm.SenderName,
            SenderAddress     = vm.SenderAddress,
            RecipientName     = vm.RecipientName,
            RecipientAddress  = vm.RecipientAddress,
            RecipientEmail    = vm.RecipientEmail,
            Weight            = vm.Weight,
            ServiceType       = vm.ServiceType,
            ShippingCost      = cost,
            UserId            = userId,
            EstimatedDelivery = CalculateEstimatedDelivery(vm.ServiceType),
        };

        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync();

        await _notifications.SendAsync(userId,
            "Shipment Created",
            $"Your shipment {shipment.TrackingNumber} has been created. Shipping cost: ${cost:F2}",
            NotificationType.ShipmentCreated, shipment.Id);

        return RedirectToAction("Pay", "Payments", new { shipmentId = shipment.Id });
    }

    // POST /Shipments/UpdateStatus  (admin-style quick update for demo)
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, ShipmentStatus status)
    {
        var userId   = _userManager.GetUserId(User)!;
        var shipment = await _db.Shipments.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (shipment is null) return NotFound();

        shipment.Status    = status;
        shipment.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        await _notifications.SendAsync(userId,
            "Status Updated",
            $"Shipment {shipment.TrackingNumber} is now {status}.",
            NotificationType.StatusUpdate, shipment.Id);

        return RedirectToAction("Details", new { id });
    }

    // GET /Shipments/Track?trackingNumber=...  (public tracking page)
    [AllowAnonymous]
    public async Task<IActionResult> Track(string? trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return View((Shipment?)null);

        var shipment = await _db.Shipments
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber);

        return View(shipment);
    }

    // ------------------------------------------------------------------ //
    private static string GenerateTrackingNumber()
    {
        var prefix = "MSH";
        var number = Random.Shared.Next(10000000, 99999999);
        return $"{prefix}{number}";
    }

    private static DateTime CalculateEstimatedDelivery(string serviceType) =>
        serviceType switch
        {
            "Overnight" => DateTime.UtcNow.AddDays(1),
            "Express"   => DateTime.UtcNow.AddDays(3),
            "Economy"   => DateTime.UtcNow.AddDays(10),
            _           => DateTime.UtcNow.AddDays(5),  // Standard
        };
}
