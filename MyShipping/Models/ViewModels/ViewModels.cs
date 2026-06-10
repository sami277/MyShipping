using System.ComponentModel.DataAnnotations;

namespace MyShipping.Models.ViewModels;

public class RegisterViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Company Name")]
    public string? CompanyName { get; set; }

    [Required, DataType(DataType.Password), MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Compare("Password")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class LoginViewModel
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}

public class CreateShipmentViewModel
{
    [Required, Display(Name = "Sender Name")]
    public string SenderName { get; set; } = string.Empty;

    [Required, Display(Name = "Sender Address")]
    public string SenderAddress { get; set; } = string.Empty;

    [Required, Display(Name = "Recipient Name")]
    public string RecipientName { get; set; } = string.Empty;

    [Required, Display(Name = "Recipient Address")]
    public string RecipientAddress { get; set; } = string.Empty;

    [Required, EmailAddress, Display(Name = "Recipient Email")]
    public string RecipientEmail { get; set; } = string.Empty;

    [Required, Range(0.01, 10000, ErrorMessage = "Weight must be between 0.01 and 10,000 kg")]
    public decimal Weight { get; set; }

    [Required, Display(Name = "Service Type")]
    public string ServiceType { get; set; } = "Standard";
}

public class PaymentViewModel
{
    public int ShipmentId { get; set; }
    public decimal Amount { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;

    [Required, Display(Name = "Payment Method")]
    public PaymentGateway Gateway { get; set; } = PaymentGateway.CreditCard;

    // Credit card fields (used when Gateway == CreditCard)
    [Display(Name = "Card Number")]
    public string? CardNumber { get; set; }

    [Display(Name = "Cardholder Name")]
    public string? CardholderName { get; set; }

    [Display(Name = "Expiry Date")]
    public string? ExpiryDate { get; set; }

    [Display(Name = "CVV")]
    public string? Cvv { get; set; }
}

public class DashboardViewModel
{
    public int TotalShipments { get; set; }
    public int ActiveShipments { get; set; }
    public int DeliveredShipments { get; set; }
    public decimal TotalRevenue { get; set; }
    public int UnreadNotifications { get; set; }
    public List<Shipment> RecentShipments { get; set; } = new();
    public List<MonthlyStats> MonthlyStats { get; set; } = new();
    public Dictionary<string, int> StatusBreakdown { get; set; } = new();
}

public class MonthlyStats
{
    public string Month { get; set; } = string.Empty;
    public int ShipmentCount { get; set; }
    public decimal Revenue { get; set; }
}
