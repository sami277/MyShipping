using MyShipping.Models;

namespace MyShipping.Services;

public interface IPaymentService
{
    decimal CalculateShippingCost(decimal weightKg, string serviceType);
    Task<PaymentResult> ProcessPaymentAsync(int shipmentId, decimal amount, PaymentGateway gateway, string? cardLastFour);
    Task<PaymentResult> RefundPaymentAsync(int paymentId);
}

public record PaymentResult(bool Success, string? TransactionId, string? ErrorMessage);
