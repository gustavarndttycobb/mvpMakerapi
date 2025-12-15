using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MvpMakerApi.Application.Interfaces;
using MvpMakerApi.Domain.Entities;
using MvpMakerApi.Domain.Interfaces;

namespace MvpMakerApi.Infrastructure.Services;

/// <summary>
/// Background service that automatically processes pending transfers
/// This is a workaround for when Stripe webhooks are not configured
/// </summary>
public class TransferBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TransferBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30); // Check every 30 seconds

    public TransferBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<TransferBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🤖 Transfer Background Service started - checking for pending transfers every {Interval} seconds", _checkInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingTransfersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in Transfer Background Service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("🛑 Transfer Background Service stopped");
    }

    private async Task ProcessPendingTransfersAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var transactionRepository = scope.ServiceProvider.GetRequiredService<ITransactionRepository>();
        var mvpRepository = scope.ServiceProvider.GetRequiredService<IMvpRepository>();
        var gitHubService = scope.ServiceProvider.GetRequiredService<IGitHubService>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        // Find all transactions with PENDING_TRANSFER status
        var allTransactions = await transactionRepository.GetAllAsync();
        var pendingTransactions = allTransactions
            .Where(t => t.Status == TransactionStatus.PENDING_TRANSFER)
            .ToList();

        if (pendingTransactions.Any())
        {
            _logger.LogInformation("🔍 Found {Count} pending transfer(s)", pendingTransactions.Count);

            foreach (var transaction in pendingTransactions)
            {
                try
                {
                    await ProcessTransactionAsync(
                        transaction,
                        transactionRepository,
                        mvpRepository,
                        gitHubService,
                        encryptionService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to process transaction {TransactionId}", transaction.Id);
                }
            }
        }
    }

    private async Task ProcessTransactionAsync(
        Transaction transaction,
        ITransactionRepository transactionRepository,
        IMvpRepository mvpRepository,
        IGitHubService gitHubService,
        IEncryptionService encryptionService)
    {
        _logger.LogInformation("🚀 Processing transaction {TransactionId}", transaction.Id);

        // Get MVP
        var mvp = await mvpRepository.GetByIdAsync(transaction.MvpId);
        if (mvp == null)
        {
            _logger.LogError("❌ MVP {MvpId} not found", transaction.MvpId);
            return;
        }

        // Get buyer username
        var buyerUsername = transaction.BuyerGitHubUsername;
        if (string.IsNullOrEmpty(buyerUsername))
        {
            _logger.LogError("❌ Buyer username not found in transaction {TransactionId}", transaction.Id);
            return;
        }

        // Process based on product type
        if (transaction.ProductType == "GitHubRepo")
        {
            if (string.IsNullOrEmpty(mvp.GitHubPatToken))
            {
                _logger.LogError("❌ GitHub token not found for MVP {MvpId}", mvp.Id);
                return;
            }

            // Decrypt token
            string decryptedToken;
            try
            {
                decryptedToken = encryptionService.Decrypt(mvp.GitHubPatToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to decrypt token for MVP {MvpId}", mvp.Id);
                return;
            }

            // Execute transfer
            _logger.LogInformation("📦 Transferring GitHub repo {RepoUrl} to {BuyerUsername}", mvp.Link, buyerUsername);

            var (success, message) = await gitHubService.TransferRepositoryAsync(
                mvp.Link,
                decryptedToken,
                buyerUsername);

            if (success)
            {
                transaction.Status = TransactionStatus.WAITING_ACCEPTANCE;
                await transactionRepository.UpdateAsync(transaction);
                _logger.LogInformation("✅ Transfer initiated successfully for transaction {TransactionId}", transaction.Id);
            }
            else
            {
                _logger.LogError("❌ Transfer failed for transaction {TransactionId}: {Message}", transaction.Id, message);
            }
        }
        else
        {
            _logger.LogWarning("⚠️ Product type {ProductType} not supported yet", transaction.ProductType);
        }
    }
}
