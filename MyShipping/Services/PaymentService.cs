using MyShipping.Data;
using MyShipping.Models;

namespace MyShipping.Services;

/// <summary>
/// Simulated payment gateway integration.
/// In production replace the gateway calls with real SDK calls
/// (e.g. Stripe, Braintree, PayPal SDK).
/// </summary>
public class PaymentService : IPaymentService
{
    private static readonly Dictionary<string, decimal> _rates = new()
    {
        ["Standard"]  = 5.00m,
        ["Express"]   = 12.50m,
        ["Overnight"] = 25.00m,
        ["Economy"]   = 3.00m,
    };

    private readonly ApplicationDbContext _db;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(ApplicationDbContext db, ILogger<PaymentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public decimal CalculateShippingCost(decimal weightKg, string serviceType)
    {
        var baseRate = _rates.TryGetValue(serviceType, out var r) ? r : _rates["Standard"];
        // Base rate per kg + handling fee
        return Math.Round(baseRate * weightKg + 2.50m, 2);
    }

    public async Task<PaymentResult> ProcessPaymentAsync(
        int shipmentId, decimal amount, PaymentGateway gateway, string? cardLastFour)
    {
        var shipment = await _db.Shipments.FindAsync(shipmentId);
        if (shipment is null)
            return new PaymentResult(false, null, "Shipment not found.");

        // Simulate gateway call
        var (success, txId, errMsg) = SimulateGatewayCall(gateway, amount);

        var payment = new Payment
        {
            ShipmentId    = shipmentId,
            Amount        = amount,
            Gateway       = gateway,
            Status        = success ? PaymentStatus.Completed : PaymentStatus.Failed,
            TransactionId = txId,
            GatewayResponse = success ? "Approved" : errMsg,
            CardLastFour  = gateway == PaymentGateway.CreditCard ? cardLastFour : null,
            ProcessedAt   = DateTime.UtcNow,
        };

        _db.Payments.Add(payment);

        if (success)
        {
            shipment.Status    = ShipmentStatus.Processing;
            shipment.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Payment {Status} for shipment {Id}: txId={TxId}",
            payment.Status, shipmentId, txId);

        return new PaymentResult(success, txId, errMsg);
    }

    public async Task<PaymentResult> RefundPaymentAsync(int paymentId)
    {
        var payment = await _db.Payments.FindAsync(paymentId);
        if (payment is null)
            return new PaymentResult(false, null, "Payment not found.");
        if (payment.Status != PaymentStatus.Completed)
            return new PaymentResult(false, null, "Only completed payments can be refunded.");

        // Simulate refund
        payment.Status = PaymentStatus.Refunded;
        payment.GatewayResponse = "Refunded";
        await _db.SaveChangesAsync();

        return new PaymentResult(true, payment.TransactionId, null);
    }

    // ------------------------------------------------------------------ //
    // Private helpers

    private static (bool success, string? txId, string? error) SimulateGatewayCall(
        PaymentGateway gateway, decimal amount)
    {
        // Simulate an occasional failure for demo purposes (5% chance)
        var rng = Random.Shared.NextDouble();
        if (rng < 0.05)
            return (false, null, "Gateway temporarily unavailable. Please try again.");

        var txId = $"TXN-{gateway}-{Guid.NewGuid():N}".ToUpperInvariant();
        return (true, txId, null);
    }
}
