using Microsoft.AspNetCore.Mvc;
using MvpMakerApi.Domain.Interfaces;

namespace MvpMakerApi.Api.Controllers;

[ApiController]
[Route("api/debug")]
public class DebugController : ControllerBase
{
    private readonly IGitHubService _gitHubService;

    public DebugController(IGitHubService gitHubService)
    {
        _gitHubService = gitHubService;
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
}

public class TestTransferRequest
{
    public string RepoUrl { get; set; } = string.Empty;
    public string SellerToken { get; set; } = string.Empty;
    public string BuyerUsername { get; set; } = string.Empty;
}
