using System.ComponentModel.DataAnnotations;

namespace MyShipping.Models;

public enum ShipmentStatus
{
    Pending,
    Processing,
    InTransit,
    OutForDelivery,
    Delivered,
    Cancelled
}

public class Shipment
{
    public int Id { get; set; }

    [Required]
    public string TrackingNumber { get; set; } = string.Empty;

    [Required, Display(Name = "Sender Name")]
    public string SenderName { get; set; } = string.Empty;

    [Required, Display(Name = "Sender Address")]
    public string SenderAddress { get; set; } = string.Empty;

    [Required, Display(Name = "Recipient Name")]
    public string RecipientName { get; set; } = string.Empty;

    [Required, Display(Name = "Recipient Address")]
    public string RecipientAddress { get; set; } = string.Empty;

    [Required, Display(Name = "Recipient Email")]
    [EmailAddress]
    public string RecipientEmail { get; set; } = string.Empty;

    [Required, Range(0.01, 10000)]
    public decimal Weight { get; set; }

    [Required, Display(Name = "Service Type")]
    public string ServiceType { get; set; } = "Standard";

    public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;

    [Display(Name = "Estimated Delivery")]
    public DateTime? EstimatedDelivery { get; set; }

    public decimal ShippingCost { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
    public Payment? Payment { get; set; }
}
