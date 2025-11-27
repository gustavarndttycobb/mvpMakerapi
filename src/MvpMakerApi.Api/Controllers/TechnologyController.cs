using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvpMakerApi.Application.DTOs;
using MvpMakerApi.Application.Interfaces;

namespace MvpMakerApi.Api.Controllers;

[ApiController]
[Route("api/mvp/technologies")]
public class TechnologyController : ControllerBase
{
    private readonly ITechnologyService _technologyService;

    public TechnologyController(ITechnologyService technologyService)
    {
        _technologyService = technologyService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var technologies = await _technologyService.GetAllTechnologiesAsync();
            return Ok(technologies);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateTechnologyRequest request)
    {
        try
        {
            var technology = await _technologyService.CreateTechnologyAsync(request);
            return Ok(technology);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
