using MvpMakerApi.Domain.Entities;

namespace MvpMakerApi.Domain.Interfaces;

public interface IMvpRepository
{
    Task AddAsync(Mvp mvp);
    Task<Mvp?> GetByIdAsync(Guid id);
    Task<List<Mvp>> ListAsync(List<string>? categories, List<string>? technologies, decimal? minPrice, decimal? maxPrice, DateTime? startDate, DateTime? endDate);
}
