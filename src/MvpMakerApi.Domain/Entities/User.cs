namespace MvpMakerApi.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // GitHub Integration
    public string? GitHubUsername { get; set; }
    public string? GitHubToken { get; set; } // Personal Access Token

    // Wallet
    public decimal Balance { get; set; } = 0;
}
