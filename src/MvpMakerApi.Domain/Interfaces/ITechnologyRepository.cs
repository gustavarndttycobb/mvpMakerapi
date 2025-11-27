using MvpMakerApi.Domain.Entities;

namespace MvpMakerApi.Domain.Interfaces;

public interface ITechnologyRepository
{
    Task<IEnumerable<Technology>> GetAllAsync();
    Task<Technology?> GetByIdAsync(Guid id);
    Task<Technology> CreateAsync(Technology technology);
    Task<bool> ExistsByNameAsync(string name);
}
