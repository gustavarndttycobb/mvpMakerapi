using MvpMakerApi.Domain.Interfaces;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace MvpMakerApi.Infrastructure.Services;

public class GitHubService : IGitHubService
{
    private readonly HttpClient _httpClient;
    private const string UserAgent = "MvpMakerApi/1.0";

    public GitHubService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.github.com");
    }

    public async Task<(bool Success, string Message)> TransferRepositoryAsync(string repoUrl, string currentOwnerToken, string newOwnerUsername)
    {
        try
        {
            var (owner, repo) = ParseRepoUrl(repoUrl);
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            {
                return (false, "Invalid repository URL format");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"/repos/{owner}/{repo}/transfer");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", currentOwnerToken);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            var payload = new
            {
                new_owner = newOwnerUsername
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return (true, "Transfer initiated successfully");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[GitHubService] Error Content: {errorContent}"); // DEBUG LOG

            // Se o erro for "já em progresso" ou "já existe", tratamos como sucesso
            if (errorContent.Contains("already in progress", StringComparison.OrdinalIgnoreCase) ||
                errorContent.Contains("Repository has already been taken", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[GitHubService] Detected 'already in progress' or 'already taken'. Treating as success.");
                return (true, "Transfer already in progress or completed. Please check GitHub.");
            }

            return (false, $"GitHub API Error ({response.StatusCode}): {errorContent}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GitHubService] Exception: {ex.Message}");
            return (false, $"Exception: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> InviteCollaboratorAsync(string repoUrl, string ownerToken, string collaboratorUsername)
    {
        try
        {
            var (owner, repo) = ParseRepoUrl(repoUrl);
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            {
                return (false, "Invalid repository URL format");
            }

            // PUT /repos/{owner}/{repo}/collaborators/{username}
            var request = new HttpRequestMessage(HttpMethod.Put, $"/repos/{owner}/{repo}/collaborators/{collaboratorUsername}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            var payload = new
            {
                permission = "pull" // Read-only access
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return (true, "Collaborator invitation sent successfully");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[GitHubService] Invite Error Content: {errorContent}");

            // Check if already a collaborator
            if (errorContent.Contains("is already a collaborator", StringComparison.OrdinalIgnoreCase))
            {
                return (true, "User is already a collaborator");
            }

            return (false, $"GitHub API Error ({response.StatusCode}): {errorContent}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GitHubService] Invite Exception: {ex.Message}");
            return (false, $"Exception: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> ForkRepositoryAsync(string repoUrl, string buyerToken, string targetUsername)
    {
        try
        {
            var (owner, repo) = ParseRepoUrl(repoUrl);
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            {
                return (false, "Invalid repository URL format");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, $"/repos/{owner}/{repo}/forks");
            // Use buyer's token so the fork is created in their account
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", buyerToken);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            // GitHub API will create fork in the authenticated user's account
            // No payload needed for basic fork

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return (true, "Fork created successfully");
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"[GitHubService] Fork Error Content: {errorContent}");

            // Check if fork already exists
            if (errorContent.Contains("already exists", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("[GitHubService] Fork already exists. Treating as success.");
                return (true, "Fork already exists for this user.");
            }

            return (false, $"GitHub API Error ({response.StatusCode}): {errorContent}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GitHubService] Fork Exception: {ex.Message}");
            return (false, $"Exception: {ex.Message}");
        }
    }

    public async Task<GitHubRepoInfo?> GetRepositoryInfoAsync(string repoUrl, string token)
    {
        try
        {
            var (owner, repo) = ParseRepoUrl(repoUrl);
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo))
            {
                return null;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, $"/repos/{owner}/{repo}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);

            return new GitHubRepoInfo
            {
                Name = json.GetProperty("name").GetString() ?? string.Empty,
                FullName = json.GetProperty("full_name").GetString() ?? string.Empty,
                Owner = json.GetProperty("owner").GetProperty("login").GetString() ?? string.Empty,
                Description = json.TryGetProperty("description", out var desc) ? desc.GetString() ?? string.Empty : string.Empty,
                IsPrivate = json.GetProperty("private").GetBoolean()
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task<(bool isValid, string? username)> ValidateTokenAsync(string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/user");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.UserAgent.ParseAdd(UserAgent);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return (false, null);
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(content);
            var username = json.GetProperty("login").GetString();

            return (!string.IsNullOrEmpty(username), username);
        }
        catch
        {
            return (false, null);
        }
    }

    private (string owner, string repo) ParseRepoUrl(string repoUrl)
    {
        try
        {
            // https://github.com/owner/repo
            var uri = new Uri(repoUrl);
            var parts = uri.AbsolutePath.Trim('/').Split('/');

            if (parts.Length >= 2)
            {
                return (parts[0], parts[1]);
            }

            return (string.Empty, string.Empty);
        }
        catch
        {
            return (string.Empty, string.Empty);
        }
    }
}
