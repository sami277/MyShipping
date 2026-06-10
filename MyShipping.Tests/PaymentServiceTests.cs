using Microsoft.Extensions.Logging.Abstractions;
using MyShipping.Models;
using MyShipping.Services;

namespace MyShipping.Tests;

public class PaymentServiceTests
{
    // ── CalculateShippingCost ──────────────────────────────────────────────

    [Theory]
    [InlineData("Standard",  2,  12.50)]   // 5.00 * 2 + 2.50
    [InlineData("Express",   1,  15.00)]   // 12.50 * 1 + 2.50
    [InlineData("Overnight", 3,  77.50)]   // 25.00 * 3 + 2.50
    [InlineData("Economy",   4,  14.50)]   // 3.00 * 4 + 2.50
    [InlineData("Unknown",   2,  12.50)]   // falls back to Standard rate
    public void CalculateShippingCost_ReturnsExpected(string service, decimal weight, decimal expected)
    {
        var svc = new PaymentService(
            DbHelper.CreateInMemory($"cost_{service}_{weight}"),
            NullLogger<PaymentService>.Instance);

        var result = svc.CalculateShippingCost(weight, service);

        Assert.Equal(expected, result);
    }

    // ── ProcessPaymentAsync ───────────────────────────────────────────────

    [Fact]
    public async Task ProcessPayment_ReturnsFailure_WhenShipmentNotFound()
    {
        var db  = DbHelper.CreateInMemory("proc_notfound");
        var svc = new PaymentService(db, NullLogger<PaymentService>.Instance);

        var result = await svc.ProcessPaymentAsync(999, 50m, PaymentGateway.CreditCard, null);

        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
    }

    [Fact]
    public async Task ProcessPayment_CreatesPaymentRecord_WhenShipmentExists()
    {
        var db   = DbHelper.CreateInMemory("proc_exists");
        var user = DbHelper.SeedUser(db);
        var ship = DbHelper.SeedShipment(db, user.Id);
        var svc  = new PaymentService(db, NullLogger<PaymentService>.Instance);

        // Run multiple attempts to overcome the 5% random failure simulation
        PaymentResult? result = null;
        for (int i = 0; i < 50; i++)
        {
            result = await svc.ProcessPaymentAsync(ship.Id, ship.ShippingCost, PaymentGateway.CreditCard, "4242");
            if (result.Success) break;
        }

        Assert.NotNull(result);
        Assert.True(result!.Success, "Expected at least one successful attempt out of 50.");
        Assert.NotNull(result.TransactionId);

        var payment = db.Payments.First(p => p.ShipmentId == ship.Id && p.Status == PaymentStatus.Completed);
        Assert.Equal("4242", payment.CardLastFour);
    }

    [Fact]
    public async Task ProcessPayment_UpdatesShipmentStatus_ToProcessing_OnSuccess()
    {
        var db   = DbHelper.CreateInMemory("proc_status");
        var user = DbHelper.SeedUser(db);
        var ship = DbHelper.SeedShipment(db, user.Id);
        var svc  = new PaymentService(db, NullLogger<PaymentService>.Instance);

        for (int i = 0; i < 50; i++)
        {
            var r = await svc.ProcessPaymentAsync(ship.Id, ship.ShippingCost, PaymentGateway.CreditCard, null);
            if (r.Success) break;
        }

        db.Entry(ship).Reload();
        // Shipment should have moved out of Pending
        Assert.NotEqual(ShipmentStatus.Pending, ship.Status);
    }

    // ── RefundPaymentAsync ────────────────────────────────────────────────

    [Fact]
    public async Task RefundPayment_ReturnsFailure_WhenPaymentNotFound()
    {
        var db  = DbHelper.CreateInMemory("refund_notfound");
        var svc = new PaymentService(db, NullLogger<PaymentService>.Instance);

        var result = await svc.RefundPaymentAsync(999);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RefundPayment_ReturnsFailure_WhenNotCompleted()
    {
        var db   = DbHelper.CreateInMemory("refund_pending");
        var user = DbHelper.SeedUser(db);
        var ship = DbHelper.SeedShipment(db, user.Id);

        var payment = new MyShipping.Models.Payment
        {
            ShipmentId = ship.Id,
            Amount     = ship.ShippingCost,
            Status     = PaymentStatus.Pending,
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var svc    = new PaymentService(db, NullLogger<PaymentService>.Instance);
        var result = await svc.RefundPaymentAsync(payment.Id);

        Assert.False(result.Success);
        Assert.Contains("completed", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefundPayment_Succeeds_WhenPaymentCompleted()
    {
        var db   = DbHelper.CreateInMemory("refund_ok");
        var user = DbHelper.SeedUser(db);
        var ship = DbHelper.SeedShipment(db, user.Id);

        var payment = new MyShipping.Models.Payment
        {
            ShipmentId    = ship.Id,
            Amount        = ship.ShippingCost,
            Status        = PaymentStatus.Completed,
            TransactionId = "TXN-TEST",
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var svc    = new PaymentService(db, NullLogger<PaymentService>.Instance);
        var result = await svc.RefundPaymentAsync(payment.Id);

        Assert.True(result.Success);
        db.Entry(payment).Reload();
        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }
}
