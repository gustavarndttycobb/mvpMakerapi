using Microsoft.EntityFrameworkCore;
using MvpMakerApi.Domain.Entities;
using MvpMakerApi.Domain.Interfaces;
using MvpMakerApi.Infrastructure.Data;

namespace MvpMakerApi.Infrastructure.Repositories;

public class TechnologyRepository : ITechnologyRepository
{
    private readonly AppDbContext _context;

    public TechnologyRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Technology>> GetAllAsync()
    {
        return await _context.Technologies
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<Technology?> GetByIdAsync(Guid id)
    {
        return await _context.Technologies.FindAsync(id);
    }

    public async Task<Technology> CreateAsync(Technology technology)
    {
        _context.Technologies.Add(technology);
        await _context.SaveChangesAsync();
        return technology;
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await _context.Technologies
            .AnyAsync(t => t.Name.ToLower() == name.ToLower());
    }
}
