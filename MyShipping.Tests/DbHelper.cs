using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyShipping.Data;
using MyShipping.Models;
using MyShipping.Services;

namespace MyShipping.Tests;

/// <summary>Helpers to build an in-memory ApplicationDbContext for each test.</summary>
internal static class DbHelper
{
    public static ApplicationDbContext CreateInMemory(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static ApplicationUser SeedUser(ApplicationDbContext db)
    {
        var user = new ApplicationUser
        {
            Id       = Guid.NewGuid().ToString(),
            UserName = "test@example.com",
            Email    = "test@example.com",
            FullName = "Test User",
        };
        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Shipment SeedShipment(ApplicationDbContext db, string userId,
        decimal weight = 2m, string service = "Standard")
    {
        var svc      = new PaymentService(db, NullLogger<PaymentService>.Instance);
        var cost     = svc.CalculateShippingCost(weight, service);
        var shipment = new Shipment
        {
            TrackingNumber   = $"MSH{Random.Shared.Next(10000000, 99999999)}",
            SenderName       = "Alice Sender",
            SenderAddress    = "1 Sender St",
            RecipientName    = "Bob Recipient",
            RecipientAddress = "2 Recipient Ave",
            RecipientEmail   = "bob@example.com",
            Weight           = weight,
            ServiceType      = service,
            ShippingCost     = cost,
            UserId           = userId,
        };
        db.Shipments.Add(shipment);
        db.SaveChanges();
        return shipment;
    }
}
