using MvpMakerApi.Domain.Entities;

namespace MvpMakerApi.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task AddAsync(User user);
}
