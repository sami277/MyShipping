using System.ComponentModel.DataAnnotations;

namespace MyShipping.Models;

public enum NotificationType
{
    ShipmentCreated,
    StatusUpdate,
    PaymentConfirmed,
    PaymentFailed,
    DeliveryAlert,
    General
}

public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public NotificationType Type { get; set; } = NotificationType.General;
    public bool IsRead { get; set; } = false;
    public int? ShipmentId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
