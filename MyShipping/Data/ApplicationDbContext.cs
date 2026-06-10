using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MyShipping.Models;

namespace MyShipping.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Shipment>(e =>
        {
            e.HasIndex(s => s.TrackingNumber).IsUnique();
            e.HasOne(s => s.User)
             .WithMany(u => u.Shipments)
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(s => s.Payment)
             .WithOne(p => p.Shipment)
             .HasForeignKey<Payment>(p => p.ShipmentId)
             .OnDelete(DeleteBehavior.Cascade);
            e.Property(s => s.ShippingCost).HasColumnType("decimal(18,2)");
            e.Property(s => s.Weight).HasColumnType("decimal(10,3)");
        });

        builder.Entity<Payment>(e =>
        {
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        });

        builder.Entity<Notification>(e =>
        {
            e.HasOne(n => n.User)
             .WithMany(u => u.Notifications)
             .HasForeignKey(n => n.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasIndex(n => new { n.UserId, n.IsRead });
        });
    }
}
