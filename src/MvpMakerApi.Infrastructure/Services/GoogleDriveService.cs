using MvpMakerApi.Domain.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MvpMakerApi.Infrastructure.Services;

public class GoogleDriveService : IGoogleDriveService
{
    private readonly HttpClient _httpClient;
    private const string DriveApiBaseUrl = "https://www.googleapis.com/drive/v3";
    private const string OAuth2ApiBaseUrl = "https://www.googleapis.com/oauth2/v3";

    public GoogleDriveService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(bool Success, string Message)> TransferOwnershipAsync(string fileId, string ownerToken, string newOwnerEmail)
    {
        try
        {
            // First, create a permission with role "owner"
            var request = new HttpRequestMessage(HttpMethod.Post, $"{DriveApiBaseUrl}/files/{fileId}/permissions?transferOwnership=true");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

            var payload = new
            {
                role = "owner",
                type = "user",
                emailAddress = newOwnerEmail
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return (true, "Ownership transfer initiated successfully");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[GoogleDriveService] Transfer Error: {errorContent}");

            // Check for common errors
            if (errorContent.Contains("notFound", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "File not found. Please check the file ID.");
            }

            if (errorContent.Contains("forbidden", StringComparison.OrdinalIgnoreCase) ||
                errorContent.Contains("insufficientPermissions", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Insufficient permissions. Make sure you have owner access to the file.");
            }

            if (errorContent.Contains("domainPolicy", StringComparison.OrdinalIgnoreCase) ||
                errorContent.Contains("cannotShareWithExternal", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Ownership transfer is only allowed between users in the same Google Workspace domain. Use 'Share' instead for external users.");
            }

            return (false, $"Google Drive API Error ({response.StatusCode}): {errorContent}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoogleDriveService] Transfer Exception: {ex.Message}");
            return (false, $"Exception: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ShareFileAsync(string fileId, string ownerToken, string userEmail, string role)
    {
        try
        {
            // Valid roles: reader, writer, commenter
            var validRoles = new[] { "reader", "writer", "commenter" };
            if (!validRoles.Contains(role.ToLower()))
            {
                return (false, $"Invalid role. Must be one of: {string.Join(", ", validRoles)}");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"{DriveApiBaseUrl}/files/{fileId}/permissions?sendNotificationEmail=true");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

            var payload = new
            {
                role = role.ToLower(),
                type = "user",
                emailAddress = userEmail
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return (true, $"File shared successfully with {role} access");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[GoogleDriveService] Share Error: {errorContent}");

            // Check if user already has access
            if (errorContent.Contains("alreadyExists", StringComparison.OrdinalIgnoreCase))
            {
                return (true, "User already has access to this file");
            }

            return (false, $"Google Drive API Error ({response.StatusCode}): {errorContent}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoogleDriveService] Share Exception: {ex.Message}");
            return (false, $"Exception: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message, string? NewFileId)> CopyFileAsync(string fileId, string ownerToken, string newFileName)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{DriveApiBaseUrl}/files/{fileId}/copy");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

            var payload = new
            {
                name = newFileName
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(content);
                var newId = json.GetProperty("id").GetString();

                return (true, "File copied successfully", newId);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[GoogleDriveService] Copy Error: {errorContent}");

            return (false, $"Google Drive API Error ({response.StatusCode}): {errorContent}", null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoogleDriveService] Copy Exception: {ex.Message}");
            return (false, $"Exception: {ex.Message}", null);
        }
    }

    public async Task<DriveFileInfo?> GetFileInfoAsync(string fileId, string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{DriveApiBaseUrl}/files/{fileId}?fields=id,name,mimeType,description,size,owners,webViewLink");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);

            var ownerEmail = string.Empty;
            if (json.TryGetProperty("owners", out var owners) && owners.GetArrayLength() > 0)
            {
                ownerEmail = owners[0].GetProperty("emailAddress").GetString() ?? string.Empty;
            }

            return new DriveFileInfo
            {
                Id = json.GetProperty("id").GetString() ?? string.Empty,
                Name = json.GetProperty("name").GetString() ?? string.Empty,
                MimeType = json.TryGetProperty("mimeType", out var mimeType) ? mimeType.GetString() ?? string.Empty : string.Empty,
                Description = json.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                Size = json.TryGetProperty("size", out var size) ? long.Parse(size.GetString() ?? "0") : 0,
                OwnerEmail = ownerEmail,
                WebViewLink = json.TryGetProperty("webViewLink", out var link) ? link.GetString() ?? string.Empty : string.Empty
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GoogleDriveService] GetFileInfo Exception: {ex.Message}");
            return null;
        }
    }

    public async Task<(bool isValid, string? email)> ValidateTokenAsync(string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{OAuth2ApiBaseUrl}/userinfo");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return (false, null);
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            var email = json.GetProperty("email").GetString();

            return (!string.IsNullOrEmpty(email), email);
        }
        catch
        {
            return (false, null);
        }
    }

    /// <summary>
    /// Extracts file ID from various Google Drive URL formats
    /// </summary>
    public string? ExtractFileIdFromUrl(string driveUrl)
    {
        try
        {
            if (string.IsNullOrEmpty(driveUrl))
                return null;

            // Handle direct file ID
            if (!driveUrl.Contains("google.com") && !driveUrl.Contains("/"))
                return driveUrl;

            var uri = new Uri(driveUrl);

            // Format: https://drive.google.com/file/d/{fileId}/view
            if (uri.AbsolutePath.Contains("/file/d/"))
            {
                var parts = uri.AbsolutePath.Split('/');
                var index = Array.IndexOf(parts, "d");
                if (index >= 0 && index + 1 < parts.Length)
                {
                    return parts[index + 1];
                }
            }

            // Format: https://drive.google.com/open?id={fileId}
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            if (query["id"] != null)
            {
                return query["id"];
            }

            // Format: https://docs.google.com/document/d/{fileId}/edit
            // Format: https://docs.google.com/spreadsheets/d/{fileId}/edit
            if (uri.Host == "docs.google.com")
            {
                var parts = uri.AbsolutePath.Split('/');
                var index = Array.IndexOf(parts, "d");
                if (index >= 0 && index + 1 < parts.Length)
                {
                    return parts[index + 1];
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
