namespace MvpMakerApi.Domain.Interfaces;

public interface IGitHubService
{
    Task<(bool Success, string Message)> TransferRepositoryAsync(string repoUrl, string currentOwnerToken, string newOwnerUsername);
    Task<GitHubRepoInfo?> GetRepositoryInfoAsync(string repoUrl, string token);
    Task<(bool isValid, string? username)> ValidateTokenAsync(string token);
}

public class GitHubRepoInfo
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Owner { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
}
