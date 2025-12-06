using Microsoft.Extensions.Configuration;
using MvpMakerApi.Domain.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace MvpMakerApi.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly string _successUrl;
    private readonly string _cancelUrl;
    private readonly string _webhookSecret;

    public StripePaymentService(IConfiguration configuration)
    {
        _configuration = configuration;

        // Try environment variable first, then config
        var secretKey = Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY")
            ?? _configuration["Stripe:SecretKey"]
            ?? throw new InvalidOperationException("Stripe:SecretKey not configured");

        StripeConfiguration.ApiKey = secretKey;

        _successUrl = _configuration["Stripe:SuccessUrl"] ?? "http://localhost:3000/payment/success";
        _cancelUrl = _configuration["Stripe:CancelUrl"] ?? "http://localhost:3000/payment/cancel";
        _webhookSecret = Environment.GetEnvironmentVariable("STRIPE_WEBHOOK_SECRET")
            ?? _configuration["Stripe:WebhookSecret"] ?? "";
    }

    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        Guid transactionId,
        string productName,
        decimal amount,
        string currency = "brl")
    {
        try
        {
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,
                            UnitAmount = (long)(amount * 100), // Convert to cents
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = productName
                            }
                        },
                        Quantity = 1
                    }
                },
                Mode = "payment",
                SuccessUrl = _successUrl,
                CancelUrl = _cancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "transactionId", transactionId.ToString() }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return new CheckoutSessionResult
            {
                Success = true,
                SessionId = session.Id,
                SessionUrl = session.Url
            };
        }
        catch (StripeException ex)
        {
            return new CheckoutSessionResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public Task<bool> HandleWebhookAsync(string json, string signature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                _webhookSecret
            );

            // Handle the checkout.session.completed event
            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null)
                {
                    // Return true to indicate successful processing
                    // The actual transaction update will be done by the caller
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }
        catch (StripeException)
        {
            return Task.FromResult(false);
        }
    }

    public async Task<PaymentStatus> GetPaymentStatusAsync(string sessionId)
    {
        try
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            return session.PaymentStatus switch
            {
                "paid" => PaymentStatus.Paid,
                "unpaid" => PaymentStatus.Pending,
                _ => PaymentStatus.Pending
            };
        }
        catch
        {
            return PaymentStatus.Failed;
        }
    }
}
