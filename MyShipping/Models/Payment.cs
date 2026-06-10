using System.ComponentModel.DataAnnotations;

namespace MyShipping.Models;

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}

public enum PaymentGateway
{
    CreditCard,
    PayPal,
    BankTransfer
}

public class Payment
{
    public int Id { get; set; }

    [Required]
    public int ShipmentId { get; set; }
    public Shipment? Shipment { get; set; }

    [Required]
    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentGateway Gateway { get; set; } = PaymentGateway.CreditCard;

    public string? TransactionId { get; set; }
    public string? GatewayResponse { get; set; }

    [Display(Name = "Card Last Four")]
    public string? CardLastFour { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
