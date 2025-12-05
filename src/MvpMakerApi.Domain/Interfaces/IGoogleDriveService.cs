namespace MvpMakerApi.Domain.Interfaces;

public interface IGoogleDriveService
{
    /// <summary>
    /// Transfers ownership of a file to another user (only works within the same Google Workspace domain)
    /// </summary>
    Task<(bool Success, string Message)> TransferOwnershipAsync(string fileId, string ownerToken, string newOwnerEmail);

    /// <summary>
    /// Shares a file with another user with specified permissions
    /// </summary>
    /// <param name="role">Permission role: "reader", "writer", "commenter"</param>
    Task<(bool Success, string Message)> ShareFileAsync(string fileId, string ownerToken, string userEmail, string role);

    /// <summary>
    /// Creates a copy of a file
    /// </summary>
    Task<(bool Success, string Message, string? NewFileId)> CopyFileAsync(string fileId, string ownerToken, string newFileName);

    /// <summary>
    /// Gets file information from Google Drive
    /// </summary>
    Task<DriveFileInfo?> GetFileInfoAsync(string fileId, string token);

    /// <summary>
    /// Validates a Google access token and returns the user's email
    /// </summary>
    Task<(bool isValid, string? email)> ValidateTokenAsync(string token);

    /// <summary>
    /// Extracts file ID from various Google Drive URL formats
    /// </summary>
    string? ExtractFileIdFromUrl(string driveUrl);
}

public class DriveFileInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long Size { get; set; }
    public string OwnerEmail { get; set; } = string.Empty;
    public string WebViewLink { get; set; } = string.Empty;
}
