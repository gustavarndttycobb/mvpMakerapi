using MvpMakerApi.Application.DTOs;

namespace MvpMakerApi.Application.Interfaces;

public interface IAuthService
{
    Task<UserResponse> LoginAsync(LoginRequest request);
    Task<UserResponse> RegisterAsync(RegisterRequest request);
}
