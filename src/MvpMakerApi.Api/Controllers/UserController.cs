using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvpMakerApi.Application.DTOs;
using MvpMakerApi.Application.Interfaces;
using MvpMakerApi.Domain.Interfaces;
using System.Security.Claims;

namespace MvpMakerApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requer autenticação JWT
public class UserController : ControllerBase
{
    private readonly IMvpService _mvpService;
    private readonly IUserRepository _userRepository;
    private readonly IEncryptionService _encryptionService;

    public UserController(
        IMvpService mvpService,
        IUserRepository userRepository,
        IEncryptionService encryptionService)
    {
        _mvpService = mvpService;
        _userRepository = userRepository;
        _encryptionService = encryptionService;
    }

    [HttpPut("credentials")]
    public async Task<IActionResult> UpdateCredentials([FromBody] UpdateCredentialsRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null) return NotFound(new { message = "User not found" });

            if (!string.IsNullOrEmpty(request.GitHubToken))
            {
                user.GitHubToken = _encryptionService.EncryptData(request.GitHubToken);
            }

            if (!string.IsNullOrEmpty(request.GoogleDriveToken))
            {
                user.GoogleDriveToken = _encryptionService.EncryptData(request.GoogleDriveToken);
            }

            await _userRepository.UpdateAsync(user);

            return Ok(new { message = "Credentials updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(new
        {
            id = user.Id,
            name = user.Name,
            email = user.Email,
            hasGitHubToken = !string.IsNullOrEmpty(user.GitHubToken),
            hasDriveToken = !string.IsNullOrEmpty(user.GoogleDriveToken)
        });
    }

    [HttpGet("mvps")]
    public async Task<IActionResult> GetMyMvps(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await _mvpService.GetUserMvpsAsync(userId, page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class UpdateCredentialsRequest
{
    public string? GitHubToken { get; set; }
    public string? GoogleDriveToken { get; set; }
}
