using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvpMakerApi.Application.DTOs;
using MvpMakerApi.Application.Interfaces;
using MvpMakerApi.Domain.Interfaces;
using System.Security.Claims;

namespace MvpMakerApi.Api.Controllers;

[ApiController]
[Route("api/mvp")]
public class MvpController : ControllerBase
{
    private readonly IMvpService _mvpService;
    private readonly IGitHubService _gitHubService;
    private readonly IGoogleDriveService _googleDriveService;

    public MvpController(IMvpService mvpService, IGitHubService gitHubService, IGoogleDriveService googleDriveService)
    {
        _mvpService = mvpService;
        _gitHubService = gitHubService;
        _googleDriveService = googleDriveService;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateMvpRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _mvpService.CreateMvpAsync(request, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("update")]
    [Authorize]
    public async Task<IActionResult> Update([FromBody] UpdateMvpRequest request)
    {
        try
        {
            var mvpId = request.Id;
            var result = await _mvpService.UpdateMvpAsync(request, mvpId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("delete")]
    [Authorize]
    public async Task<IActionResult> Delete([FromBody] DeleteMvpRequest request)
    {
        try
        {
            var mvpId = request.Id;
            var result = await _mvpService.DeleteMvpAsync(mvpId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }



    [HttpPost("list")]
    public async Task<IActionResult> List([FromBody] MvpListQuery query)
    {
        try
        {
            var result = await _mvpService.GetMvpListAsync(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("details/{id}")]
    public async Task<IActionResult> Details(Guid id)
    {
        try
        {
            var result = await _mvpService.GetMvpDetailsAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("validate-github-token")]
    [Authorize]
    public async Task<IActionResult> ValidateGitHubToken([FromBody] ValidateTokenRequest request)
    {
        try
        {
            var (isValid, username) = await _gitHubService.ValidateTokenAsync(request.Token);

            return Ok(new ValidateTokenResponse
            {
                IsValid = isValid,
                Message = isValid ? "GitHub token is valid" : "GitHub token is invalid or expired",
                Username = username
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ValidateTokenResponse
            {
                IsValid = false,
                Message = $"Error validating GitHub token: {ex.Message}",
                Username = null
            });
        }
    }

    [HttpPost("validate-drive-token")]
    [Authorize]
    public async Task<IActionResult> ValidateDriveToken([FromBody] ValidateTokenRequest request)
    {
        try
        {
            var (isValid, email) = await _googleDriveService.ValidateTokenAsync(request.Token);

            return Ok(new ValidateTokenResponse
            {
                IsValid = isValid,
                Message = isValid ? "Google Drive token is valid" : "Google Drive token is invalid or expired",
                Username = email
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new ValidateTokenResponse
            {
                IsValid = false,
                Message = $"Error validating Google Drive token: {ex.Message}",
                Username = null
            });
        }
    }
}
