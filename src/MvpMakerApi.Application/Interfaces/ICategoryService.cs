using MvpMakerApi.Application.DTOs;

namespace MvpMakerApi.Application.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync();
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request);
}
