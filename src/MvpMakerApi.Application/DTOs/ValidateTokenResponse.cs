namespace MvpMakerApi.Application.DTOs;

public class ValidateTokenResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Username { get; set; } // GitHub username or Google email
}
