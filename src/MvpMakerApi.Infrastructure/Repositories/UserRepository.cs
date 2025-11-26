using Microsoft.EntityFrameworkCore;
using MvpMakerApi.Domain.Entities;
using MvpMakerApi.Domain.Interfaces;
using MvpMakerApi.Infrastructure.Data;

namespace MvpMakerApi.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }
}
