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
    private readonly IGitHubService _gitHubService;

    public UserController(
        IMvpService mvpService,
        IUserRepository userRepository,
        IGitHubService gitHubService)
    {
        _mvpService = mvpService;
        _userRepository = userRepository;
        _gitHubService = gitHubService;
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
            balance = user.Balance,
            gitHubUsername = user.GitHubUsername,
            isGitHubConnected = !string.IsNullOrEmpty(user.GitHubToken)
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

    [HttpPost("github/connect")]
    public async Task<IActionResult> ConnectGitHub([FromBody] ConnectGitHubRequest request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // Validar token GitHub
            var (isValid, username) = await _gitHubService.ValidateTokenAsync(request.GitHubToken);
            if (!isValid || username != request.GitHubUsername)
            {
                return BadRequest(new { message = "Invalid GitHub token or username mismatch" });
            }

            // Atualizar usuário
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            user.GitHubUsername = request.GitHubUsername;
            user.GitHubToken = request.GitHubToken;
            await _userRepository.UpdateAsync(user);

            return Ok(new { message = "GitHub account connected successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("github/status")]
    public async Task<IActionResult> GetGitHubStatus()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = await _userRepository.GetByIdAsync(userId);

            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new GitHubStatusDto
            {
                IsConnected = !string.IsNullOrEmpty(user.GitHubUsername),
                Username = user.GitHubUsername
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
