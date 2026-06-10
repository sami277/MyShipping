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
public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IPaymentService _paymentService;
    private readonly INotificationService _notifications;

    public PaymentsController(ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IPaymentService paymentService,
        INotificationService notifications)
    {
        _db             = db;
        _userManager    = userManager;
        _paymentService = paymentService;
        _notifications  = notifications;
    }

    // GET /Payments/Pay?shipmentId=5
    public async Task<IActionResult> Pay(int shipmentId)
    {
        var userId   = _userManager.GetUserId(User)!;
        var shipment = await _db.Shipments
            .Include(s => s.Payment)
            .FirstOrDefaultAsync(s => s.Id == shipmentId && s.UserId == userId);

        if (shipment is null) return NotFound();
        if (shipment.Payment?.Status == PaymentStatus.Completed)
            return RedirectToAction("Details", "Shipments", new { id = shipmentId });

        var vm = new PaymentViewModel
        {
            ShipmentId      = shipment.Id,
            Amount          = shipment.ShippingCost,
            TrackingNumber  = shipment.TrackingNumber,
        };
        return View(vm);
    }

    // POST /Payments/Pay
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Pay(PaymentViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var userId   = _userManager.GetUserId(User)!;
        var shipment = await _db.Shipments
            .FirstOrDefaultAsync(s => s.Id == vm.ShipmentId && s.UserId == userId);

        if (shipment is null) return NotFound();

        // Extract last 4 digits of card number for receipt display
        var cardLastFour = vm.Gateway == PaymentGateway.CreditCard && !string.IsNullOrEmpty(vm.CardNumber)
            ? vm.CardNumber.Replace(" ", "").TakeLast(4).ToArray() is var digits && digits.Length == 4
                ? new string(digits)
                : null
            : null;

        var result = await _paymentService.ProcessPaymentAsync(
            vm.ShipmentId, vm.Amount, vm.Gateway, cardLastFour);

        if (result.Success)
        {
            await _notifications.SendAsync(userId,
                "Payment Confirmed",
                $"Payment of ${vm.Amount:F2} for shipment {shipment.TrackingNumber} was successful. Tx: {result.TransactionId}",
                NotificationType.PaymentConfirmed, vm.ShipmentId);

            TempData["Success"] = $"Payment confirmed. Transaction ID: {result.TransactionId}";
            return RedirectToAction("Details", "Shipments", new { id = vm.ShipmentId });
        }

        await _notifications.SendAsync(userId,
            "Payment Failed",
            $"Payment for shipment {shipment.TrackingNumber} failed: {result.ErrorMessage}",
            NotificationType.PaymentFailed, vm.ShipmentId);

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Payment failed.");
        return View(vm);
    }

    // POST /Payments/Refund/5
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Refund(int paymentId)
    {
        var userId  = _userManager.GetUserId(User)!;
        var payment = await _db.Payments
            .Include(p => p.Shipment)
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.Shipment!.UserId == userId);

        if (payment is null) return NotFound();

        var result = await _paymentService.RefundPaymentAsync(paymentId);
        if (result.Success)
        {
            await _notifications.SendAsync(userId,
                "Refund Processed",
                $"Refund for shipment {payment.Shipment!.TrackingNumber} has been processed.",
                NotificationType.General, payment.ShipmentId);

            TempData["Success"] = "Refund processed successfully.";
        }
        else
        {
            TempData["Error"] = result.ErrorMessage;
        }

        return RedirectToAction("Details", "Shipments", new { id = payment.ShipmentId });
    }

    // GET /Payments/History
    public async Task<IActionResult> History()
    {
        var userId = _userManager.GetUserId(User)!;
        var payments = await _db.Payments
            .Include(p => p.Shipment)
            .Where(p => p.Shipment!.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return View(payments);
    }
}
