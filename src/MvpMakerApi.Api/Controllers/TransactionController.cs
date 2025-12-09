using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvpMakerApi.Application.DTOs;
using MvpMakerApi.Application.Interfaces;
using System.Security.Claims;

namespace MvpMakerApi.Api.Controllers;

[ApiController]
[Route("api")]
public class TransactionController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost("mvp/{id}/purchase")]
    [Authorize]
    public async Task<IActionResult> PurchaseMvp(Guid id, [FromBody] PurchaseMvpRequest request)
    {
        try
        {
            var buyerId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _transactionService.InitiatePurchaseAsync(id, buyerId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("transactions/{id}/complete")]
    [Authorize]
    public async Task<IActionResult> CompleteTransaction(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _transactionService.CompleteTransactionAsync(id, userId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("transactions/{id}")]
    [Authorize]
    public async Task<IActionResult> GetTransaction(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _transactionService.GetTransactionAsync(id, userId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("user/transactions")]
    [Authorize]
    public async Task<IActionResult> GetUserTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _transactionService.GetUserTransactionsAsync(userId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("transactions/{id}/transfer-github")]
    [Authorize]
    public async Task<IActionResult> TransferGitHub(Guid id, [FromBody] TransferGitHubRequest request)
    {
        try
        {
            (bool success, string message) = await _transactionService.TransferGitHubRepositoryAsync(
                id,
                request.BuyerUsername
            );

            if (success)
            {
                return Ok(new { message = "Repository transfer initiated successfully! Buyer must accept the transfer on GitHub." });
            }

            return BadRequest(new { message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("transactions/{id}/transfer-drive")]
    [Authorize]
    public async Task<IActionResult> TransferDrive(Guid id, [FromBody] TransferDriveRequest request)
    {
        try
        {
            (bool success, string message) = await _transactionService.TransferDriveFileAsync(
                id,
                request.BuyerEmail
            );

            if (success)
            {
                return Ok(new { message = "Drive file shared/transferred successfully! Buyer should check their Google Drive." });
            }

            return BadRequest(new { message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("transactions/{id}/verify-transfer")]
    [Authorize]
    public async Task<IActionResult> VerifyTransfer(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _transactionService.VerifyTransferAsync(id, userId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
