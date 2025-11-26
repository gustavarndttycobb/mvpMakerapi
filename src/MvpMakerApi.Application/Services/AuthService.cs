using MvpMakerApi.Application.DTOs;
using MvpMakerApi.Application.Interfaces;
using MvpMakerApi.Domain.Entities;
using MvpMakerApi.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace MvpMakerApi.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;

    public AuthService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<UserResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new Exception("Invalid credentials");
        }

        return new UserResponse(user.Id, user.Name, user.Email, "mock-jwt-token");
    }

    public async Task<UserResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = await _userRepository.GetByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new Exception("User already exists");
        }

        var user = new User
        {
            Name = request.Name,
            Email = request.Email,
            PasswordHash = HashPassword(request.Password)
        };

        await _userRepository.AddAsync(user);

        return new UserResponse(user.Id, user.Name, user.Email, "mock-jwt-token");
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}
