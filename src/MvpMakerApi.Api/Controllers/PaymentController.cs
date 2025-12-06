using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvpMakerApi.Application.DTOs;
using MvpMakerApi.Application.Interfaces;
using MvpMakerApi.Domain.Interfaces;
using System.Security.Claims;

namespace MvpMakerApi.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ITransactionService _transactionService;

    public PaymentController(IPaymentService paymentService, ITransactionService transactionService)
    {
        _paymentService = paymentService;
        _transactionService = transactionService;
    }

    /// <summary>
    /// Stripe webhook endpoint - receives payment events
    /// </summary>
    [HttpPost("webhook")]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].FirstOrDefault();

        if (string.IsNullOrEmpty(signature))
        {
            return BadRequest("Missing Stripe-Signature header");
        }

        var success = await _paymentService.HandleWebhookAsync(json, signature);

        if (success)
        {
            return Ok();
        }

        return BadRequest("Webhook processing failed");
    }

    /// <summary>
    /// Check payment status for a transaction
    /// </summary>
    [HttpGet("{transactionId}/status")]
    [Authorize]
    public async Task<IActionResult> GetPaymentStatus(Guid transactionId)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var transaction = await _transactionService.GetTransactionAsync(transactionId, userId);

            return Ok(new PaymentStatusResponse
            {
                TransactionId = transactionId,
                Status = transaction.Status,
                IsPaid = transaction.Status != "PENDING"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
