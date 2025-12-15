using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MvpMakerApi.Domain.Interfaces;
using MvpMakerApi.Application.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace MvpMakerApi.Infrastructure.Services;

public class StripePaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMvpRepository _mvpRepository;
    private readonly IGitHubService _gitHubService;
    private readonly IGoogleDriveService _googleDriveService;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<StripePaymentService> _logger;
    private readonly string _successUrl;
    private readonly string _cancelUrl;
    private readonly string _webhookSecret;

    public StripePaymentService(
        IConfiguration configuration,
        ITransactionRepository transactionRepository,
        IMvpRepository mvpRepository,
        IGitHubService gitHubService,
        IGoogleDriveService googleDriveService,
        IEncryptionService encryptionService,
        ILogger<StripePaymentService> logger)
    {
        _configuration = configuration;
        _transactionRepository = transactionRepository;
        _mvpRepository = mvpRepository;
        _gitHubService = gitHubService;
        _googleDriveService = googleDriveService;
        _encryptionService = encryptionService;
        _logger = logger;

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
        string currency = "usd")
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

    public async Task<bool> HandleWebhookAsync(string json, string signature)
    {
        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                signature,
                _webhookSecret
            );

            _logger.LogInformation("Received Stripe webhook event: {EventType}", stripeEvent.Type);

            // Handle the checkout.session.completed event
            if (stripeEvent.Type == "checkout.session.completed")
            {
                var session = stripeEvent.Data.Object as Session;
                if (session != null && session.Metadata.TryGetValue("transactionId", out var transactionIdStr))
                {
                    if (Guid.TryParse(transactionIdStr, out var transactionId))
                    {
                        _logger.LogInformation("Processing payment for transaction {TransactionId}", transactionId);

                        // Get the transaction
                        var transaction = await _transactionRepository.GetByIdAsync(transactionId);
                        if (transaction == null)
                        {
                            _logger.LogError("Transaction {TransactionId} not found", transactionId);
                            return false;
                        }

                        // Update transaction status to PAID
                        transaction.Status = MvpMakerApi.Domain.Entities.TransactionStatus.PENDING_TRANSFER;
                        transaction.StripeSessionId = session.Id;
                        await _transactionRepository.UpdateAsync(transaction);

                        _logger.LogInformation("Transaction {TransactionId} marked as PENDING_TRANSFER", transactionId);

                        // Execute transfer automatically
                        await ExecuteTransferAsync(transaction);

                        return true;
                    }
                }
            }

            return false;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook error");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return false;
        }
    }

    private async Task ExecuteTransferAsync(MvpMakerApi.Domain.Entities.Transaction transaction)
    {
        _logger.LogInformation("=== STARTING TRANSFER EXECUTION for transaction {TransactionId} ===", transaction.Id);

        try
        {
            // Get MVP to retrieve credentials
            var mvp = await _mvpRepository.GetByIdAsync(transaction.MvpId);
            if (mvp == null)
            {
                _logger.LogError("❌ MVP {MvpId} not found for transaction {TransactionId}", transaction.MvpId, transaction.Id);
                return;
            }

            _logger.LogInformation("✅ MVP found: {MvpName} (ID: {MvpId})", mvp.Name, mvp.Id);

            // Get buyer username from transaction
            var buyerUsername = transaction.BuyerGitHubUsername;
            if (string.IsNullOrEmpty(buyerUsername))
            {
                _logger.LogError("❌ Buyer username not found in transaction {TransactionId}", transaction.Id);
                return;
            }

            _logger.LogInformation("✅ Buyer username: {BuyerUsername}", buyerUsername);

            // Execute transfer based on product type
            if (transaction.ProductType == "GitHubRepo")
            {
                _logger.LogInformation("📦 Product type: GitHubRepo");

                if (string.IsNullOrEmpty(mvp.GitHubPatToken))
                {
                    _logger.LogError("❌ GitHub PAT token not found for MVP {MvpId}", mvp.Id);
                    return;
                }

                _logger.LogInformation("✅ GitHub token exists (encrypted)");

                // Decrypt token
                string decryptedToken;
                try
                {
                    decryptedToken = _encryptionService.Decrypt(mvp.GitHubPatToken);
                    _logger.LogInformation("✅ Token decrypted successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to decrypt GitHub token for MVP {MvpId}", mvp.Id);
                    return;
                }

                _logger.LogInformation("🚀 Calling GitHub TransferRepositoryAsync...");
                _logger.LogInformation("   Repo URL: {RepoUrl}", mvp.Link);
                _logger.LogInformation("   Buyer: {BuyerUsername}", buyerUsername);

                var (success, message) = await _gitHubService.TransferRepositoryAsync(
                    mvp.Link,
                    decryptedToken,
                    buyerUsername
                );

                _logger.LogInformation("📬 GitHub API Response: Success={Success}, Message={Message}", success, message);

                if (success)
                {
                    transaction.Status = MvpMakerApi.Domain.Entities.TransactionStatus.WAITING_ACCEPTANCE;
                    await _transactionRepository.UpdateAsync(transaction);
                    _logger.LogInformation("✅ GitHub transfer initiated successfully for transaction {TransactionId}", transaction.Id);
                    _logger.LogInformation("✅ Transaction status updated to WAITING_ACCEPTANCE");
                }
                else
                {
                    _logger.LogError("❌ GitHub transfer failed for transaction {TransactionId}: {Message}", transaction.Id, message);
                }
            }
            else
            {
                _logger.LogWarning("⚠️ Product type {ProductType} not handled yet", transaction.ProductType);
            }

            _logger.LogInformation("=== TRANSFER EXECUTION COMPLETED for transaction {TransactionId} ===", transaction.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "💥 EXCEPTION in ExecuteTransferAsync for transaction {TransactionId}", transaction.Id);
            _logger.LogError("Exception Type: {ExceptionType}", ex.GetType().Name);
            _logger.LogError("Exception Message: {ExceptionMessage}", ex.Message);
            _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
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
