using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MvpMakerApi.Application.Interfaces;
using MvpMakerApi.Domain.Interfaces;
using MvpMakerApi.Infrastructure.Data;

namespace MvpMakerApi.Api.Controllers;

[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    private readonly IGitHubService _gitHubService;
    private readonly ITransactionService _transactionService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IMvpRepository _mvpRepository;
    private readonly AppDbContext _context;

    public DebugController(
        IGitHubService gitHubService,
        ITransactionService transactionService,
        ITransactionRepository transactionRepository,
        IMvpRepository mvpRepository,
        AppDbContext context)
    {
        _gitHubService = gitHubService;
        _transactionService = transactionService;
        _transactionRepository = transactionRepository;
        _mvpRepository = mvpRepository;
        _context = context;
    }

    [HttpPost("github/transfer")]
    public async Task<IActionResult> TestTransfer([FromBody] TestTransferRequest request)
    {
        try
        {
            var (success, message) = await _gitHubService.TransferRepositoryAsync(
                request.RepoUrl,
                request.SellerToken,
                request.BuyerUsername
            );

            if (success)
            {
                return Ok(new { message = "Repository transfer initiated successfully! Check the buyer's email/GitHub notifications." });
            }

            return BadRequest(new { message = $"Failed to initiate transfer: {message}" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Force transfer for a transaction (for debugging webhook issues)
    /// </summary>
    [HttpPost("transactions/{id}/force-transfer")]
    [Authorize]
    public async Task<IActionResult> ForceTransfer(Guid id)
    {
        try
        {
            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction == null)
            {
                return NotFound(new { message = "Transaction not found" });
            }

            var mvp = await _mvpRepository.GetByIdAsync(transaction.MvpId);
            if (mvp == null)
            {
                return NotFound(new { message = "MVP not found" });
            }

            var buyerUsername = transaction.BuyerGitHubUsername;
            if (string.IsNullOrEmpty(buyerUsername))
            {
                return BadRequest(new
                {
                    message = "Buyer GitHub username not found in transaction",
                    transactionId = id,
                    status = transaction.Status.ToString()
                });
            }

            var (success, message) = await _transactionService.TransferGitHubRepositoryAsync(
                id,
                buyerUsername
            );

            return Ok(new
            {
                success,
                message,
                transactionId = id,
                buyerUsername,
                repoUrl = mvp.Link,
                hasToken = !string.IsNullOrEmpty(mvp.GitHubPatToken),
                transactionStatus = transaction.Status.ToString()
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Get transaction details for debugging
    /// </summary>
    [HttpGet("transactions/{id}")]
    public async Task<IActionResult> GetTransactionDebug(Guid id)
    {
        try
        {
            var transaction = await _transactionRepository.GetByIdAsync(id);
            if (transaction == null)
            {
                return NotFound(new { message = "Transaction not found" });
            }

            var mvp = await _mvpRepository.GetByIdAsync(transaction.MvpId);

            return Ok(new
            {
                transactionId = transaction.Id,
                status = transaction.Status.ToString(),
                productType = transaction.ProductType,
                buyerGitHubUsername = transaction.BuyerGitHubUsername,
                stripeSessionId = transaction.StripeSessionId,
                mvpId = transaction.MvpId,
                mvp = new
                {
                    id = mvp?.Id,
                    name = mvp?.Name,
                    link = mvp?.Link,
                    productType = mvp?.ProductType.ToString(),
                    hasGitHubToken = !string.IsNullOrEmpty(mvp?.GitHubPatToken),
                    hasDriveToken = !string.IsNullOrEmpty(mvp?.GoogleOAuthToken)
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Manually update MVP token (for debugging 401 errors)
    /// </summary>
    [HttpPost("mvp/{id}/update-token")]
    public async Task<IActionResult> UpdateMvpToken(Guid id, [FromBody] UpdateTokenRequest request)
    {
        try
        {
            var mvp = await _mvpRepository.GetByIdAsync(id);
            if (mvp == null)
            {
                return NotFound(new { message = "MVP not found" });
            }

            // Inject IEncryptionService manually since it wasn't in constructor
            var encryptionService = HttpContext.RequestServices.GetRequiredService<IEncryptionService>();

            if (!string.IsNullOrEmpty(request.GitHubToken))
            {
                mvp.GitHubPatToken = encryptionService.Encrypt(request.GitHubToken);
            }

            if (!string.IsNullOrEmpty(request.DriveToken))
            {
                mvp.GoogleOAuthToken = encryptionService.Encrypt(request.DriveToken);
            }

            await _mvpRepository.UpdateAsync(mvp);

            return Ok(new { message = "Token updated successfully", mvpId = id });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Clear all MVPs and Transactions (DANGER: Use with caution)
    /// </summary>
    [HttpDelete("clear-database")]
    public async Task<IActionResult> ClearDatabase()
    {
        try
        {
            // Delete all transactions first (FK constraint)
            await _context.Transactions.ExecuteDeleteAsync();

            // Delete all MVPs
            await _context.MVPs.ExecuteDeleteAsync();

            return Ok(new { message = "All Transactions and MVPs have been deleted successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class UpdateTokenRequest
{
    public string? GitHubToken { get; set; }
    public string? DriveToken { get; set; }
}

public class TestTransferRequest
{
    public string RepoUrl { get; set; } = string.Empty;
    public string SellerToken { get; set; } = string.Empty;
    public string BuyerUsername { get; set; } = string.Empty;
}
