using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MvpMakerApi.Application.DTOs;
using MvpMakerApi.Application.Interfaces;

namespace MvpMakerApi.Api.Controllers;

[ApiController]
[Route("api/mvp/categories")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoryController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        try
        {
            var category = await _categoryService.CreateCategoryAsync(request);
            return Ok(category);
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
