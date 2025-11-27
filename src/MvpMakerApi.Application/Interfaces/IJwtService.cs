namespace MvpMakerApi.Application.Interfaces;

public interface IJwtService
{
    string GenerateToken(Guid userId, string email, string name);
    bool ValidateToken(string token);
}
