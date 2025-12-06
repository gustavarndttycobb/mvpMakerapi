namespace MvpMakerApi.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // Encrypted Credentials
    public string? GitHubToken { get; set; }
    public string? GoogleDriveToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
