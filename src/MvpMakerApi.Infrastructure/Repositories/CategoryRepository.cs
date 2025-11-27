using Microsoft.EntityFrameworkCore;
using MvpMakerApi.Domain.Entities;
using MvpMakerApi.Domain.Interfaces;
using MvpMakerApi.Infrastructure.Data;

namespace MvpMakerApi.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _context.Categories
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _context.Categories.FindAsync(id);
    }

    public async Task<Category> CreateAsync(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.Categories
            .AnyAsync(c => c.Name.ToLower() == name.ToLower());
    }
}
