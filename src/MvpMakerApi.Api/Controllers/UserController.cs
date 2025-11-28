using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvpMakerApi.Application.Interfaces;
using System.Security.Claims;

namespace MvpMakerApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requer autenticação JWT
public class UserController : ControllerBase
{
    private readonly IMvpService _mvpService;

    public UserController(IMvpService mvpService)
    {
        _mvpService = mvpService;
    }

    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var name = User.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        return Ok(new
        {
            id = userId,
            email = email,
            name = name
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
