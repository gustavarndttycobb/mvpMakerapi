using Microsoft.EntityFrameworkCore;
using MvpMakerApi.Domain.Entities;
using MvpMakerApi.Domain.Interfaces;
using MvpMakerApi.Infrastructure.Data;

namespace MvpMakerApi.Infrastructure.Repositories;

public class MvpRepository : IMvpRepository
{
    private readonly AppDbContext _context;

    public MvpRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Mvp mvp)
    {
        await _context.MVPs.AddAsync(mvp);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Mvp mvp)
    {
        _context.MVPs.Update(mvp);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Mvp mvp)
    {
        _context.MVPs.Remove(mvp);
        await _context.SaveChangesAsync();
    }

    public async Task<Mvp?> GetByIdAsync(Guid id)
    {
        return await _context.MVPs
            .Include(m => m.Owner)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<Mvp>> ListAsync(List<string>? categories, List<string>? technologies, decimal? minPrice, decimal? maxPrice, DateTime? startDate, DateTime? endDate)
    {
        var query = _context.MVPs.Include(m => m.Owner).AsQueryable();

        if (minPrice.HasValue)
        {
            query = query.Where(m => m.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(m => m.Price <= maxPrice.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.CreatedAt <= endDate.Value);
        }

        // Client-side evaluation for JSON lists if DB doesn't support advanced JSON querying easily via EF Core LINQ yet
        // For lists of strings stored as JSON/String, we often need to fetch and filter in memory or use specific functions.
        // Given the complexity of JSON array containment in EF Core + MySQL, let's do basic filtering in memory for the lists if the dataset is small,
        // OR use `EF.Functions.Like` if we store as simple strings.
        // But since we haven't defined the conversion yet, let's refine the DbContext first.
        
        // Let's assume we fetch and filter for now to be safe, or we can try to use Contains if we map it correctly.
        // For this MVP implementation, let's execute the query for scalar fields first, then filter in memory for list fields.
        
        var result = await query.ToListAsync();

        if (categories != null && categories.Any())
        {
            result = result.Where(m => m.Categories.Any(c => categories.Contains(c))).ToList();
        }

        if (technologies != null && technologies.Any())
        {
            result = result.Where(m => m.Technologies.Any(t => technologies.Contains(t))).ToList();
        }

        return result;
    }

    public async Task<(List<Mvp> Items, int TotalCount)> GetByOwnerIdAsync(Guid ownerId, int pageNumber, int pageSize)
    {
        var query = _context.MVPs
            .Include(m => m.Owner)
            .Where(m => m.OwnerId == ownerId)
            .OrderByDescending(m => m.CreatedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
