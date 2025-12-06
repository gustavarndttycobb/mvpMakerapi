namespace MvpMakerApi.Domain.Interfaces;

public interface IPaymentService
{
    /// <summary>
    /// Creates a Stripe Checkout Session for the given transaction
    /// </summary>
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid transactionId,
        string productName,
        decimal amount,
        string currency = "brl"
    );

    /// <summary>
    /// Handles incoming Stripe webhook events
    /// </summary>
    Task<bool> HandleWebhookAsync(string json, string signature);

    /// <summary>
    /// Gets the payment status for a session
    /// </summary>
    Task<PaymentStatus> GetPaymentStatusAsync(string sessionId);
}

public class CheckoutSessionResult
{
    public bool Success { get; set; }
    public string? SessionId { get; set; }
    public string? SessionUrl { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum PaymentStatus
{
    Pending,
    Paid,
    Failed,
    Cancelled
}
