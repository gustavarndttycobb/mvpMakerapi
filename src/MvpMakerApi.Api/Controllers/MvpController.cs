using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvpMakerApi.Application.DTOs;
using MvpMakerApi.Application.Interfaces;
using System.Security.Claims;

namespace MvpMakerApi.Api.Controllers;

[ApiController]
[Route("api/mvp")]
public class MvpController : ControllerBase
{
    private readonly IMvpService _mvpService;

    public MvpController(IMvpService mvpService)
    {
        _mvpService = mvpService;
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
}
