namespace MvpMakerApi.Application.DTOs;

public record LoginRequest(string Email, string Password);
public record RegisterRequest(string Name, string Email, string Password);
public record UserResponse(Guid Id, string Name, string Email, string Token); // Token will be empty for now or simple mock
