namespace MvpMakerApi.Application.DTOs;

public class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string? RepositoryUrl { get; set; } // Optional: for GitHub, to test access to specific repo
    public string? FileId { get; set; } // Optional: for Drive, to test access to specific file
}
