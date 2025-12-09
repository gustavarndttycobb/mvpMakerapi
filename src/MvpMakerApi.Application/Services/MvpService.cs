using MvpMakerApi.Application.DTOs;
using MvpMakerApi.Application.Interfaces;
using MvpMakerApi.Domain.Entities;
using MvpMakerApi.Domain.Interfaces;

namespace MvpMakerApi.Application.Services;

public class MvpService : IMvpService
{
    private readonly IMvpRepository _mvpRepository;
    private readonly IUserRepository _userRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IGitHubService _gitHubService;
    private readonly IGoogleDriveService _googleDriveService;

    public MvpService(
        IMvpRepository mvpRepository,
        IUserRepository userRepository,
        IEncryptionService encryptionService,
        IGitHubService gitHubService,
        IGoogleDriveService googleDriveService)
    {
        _mvpRepository = mvpRepository;
        _userRepository = userRepository;
        _encryptionService = encryptionService;
        _gitHubService = gitHubService;
        _googleDriveService = googleDriveService;
    }

    public async Task<MvpDto> CreateMvpAsync(CreateMvpRequest request, Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found");
        }

        // Validate GitHubBusinessType for GitHubRepo
        if (request.ProductType == "GitHubRepo" && string.IsNullOrEmpty(request.GitHubBusinessType))
        {
            throw new ArgumentException("GitHubBusinessType is required for GitHubRepo products");
        }

        // Validate GitHubPatToken for GitHubRepo
        if (request.ProductType == "GitHubRepo" && string.IsNullOrEmpty(request.GitHubPatToken))
        {
            throw new ArgumentException("GitHubPatToken is required for GitHubRepo products");
        }

        // Validate GoogleOAuthToken for Drive
        if (request.ProductType == "Drive" && string.IsNullOrEmpty(request.GoogleOAuthToken))
        {
            throw new ArgumentException("GoogleOAuthToken is required for Drive products");
        }

        var mvp = new Mvp
        {
            Name = request.Name,
            Description = request.Description,
            Technologies = request.Technologies,
            Categories = request.Categories,
            ImageUrl = request.ImageUrl,
            Price = request.Price,
            OwnerId = userId,
            Highlights = request.Highlights,
            Objective = request.Objective,
            MainFeatures = request.MainFeatures,
            Status = request.Status,
            Screenshots = request.Screenshots,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ProductType = Enum.Parse<MvpProductType>(request.ProductType),
            Link = request.Link,
            PreviewLink = request.PreviewLink,
            GitHubBusinessType = !string.IsNullOrEmpty(request.GitHubBusinessType)
                ? Enum.Parse<Domain.Entities.GitHubBusinessType>(request.GitHubBusinessType)
                : null,
            DriveBusinessType = !string.IsNullOrEmpty(request.DriveBusinessType)
                ? Enum.Parse<Domain.Entities.DriveBusinessType>(request.DriveBusinessType)
                : null,
            // Encrypt tokens before storing
            GitHubPatToken = !string.IsNullOrEmpty(request.GitHubPatToken)
                ? _encryptionService.Encrypt(request.GitHubPatToken)
                : null,
            GoogleOAuthToken = !string.IsNullOrEmpty(request.GoogleOAuthToken)
                ? _encryptionService.Encrypt(request.GoogleOAuthToken)
                : null
        };

        await _mvpRepository.AddAsync(mvp);

        return MapToDto(mvp, user);
    }

    public async Task<MvpDto> UpdateMvpAsync(UpdateMvpRequest request, Guid mvpId)
    {
        var mvp = await _mvpRepository.GetByIdAsync(mvpId);
        if (mvp == null)
        {
            throw new Exception("MVP not found");
        }

        mvp.Name = request.Name;
        mvp.Description = request.Description;
        mvp.Technologies = request.Technologies;
        mvp.Categories = request.Categories;
        mvp.ImageUrl = request.ImageUrl;
        mvp.Price = request.Price;
        mvp.Highlights = request.Highlights;
        mvp.Objective = request.Objective;
        mvp.MainFeatures = request.MainFeatures;
        mvp.Status = request.Status;
        mvp.Screenshots = request.Screenshots;
        mvp.UpdatedAt = DateTime.UtcNow;
        mvp.ProductType = Enum.Parse<MvpProductType>(request.ProductType);
        mvp.Link = request.Link;
        mvp.PreviewLink = request.PreviewLink;
        mvp.GitHubBusinessType = !string.IsNullOrEmpty(request.GitHubBusinessType)
            ? Enum.Parse<Domain.Entities.GitHubBusinessType>(request.GitHubBusinessType)
            : null;
        mvp.DriveBusinessType = !string.IsNullOrEmpty(request.DriveBusinessType)
            ? Enum.Parse<Domain.Entities.DriveBusinessType>(request.DriveBusinessType)
            : null;

        // Update tokens if provided (encrypt before storing)
        if (!string.IsNullOrEmpty(request.GitHubPatToken))
        {
            mvp.GitHubPatToken = _encryptionService.Encrypt(request.GitHubPatToken);
        }
        if (!string.IsNullOrEmpty(request.GoogleOAuthToken))
        {
            mvp.GoogleOAuthToken = _encryptionService.Encrypt(request.GoogleOAuthToken);
        }

        await _mvpRepository.UpdateAsync(mvp);

        return MapToDto(mvp, mvp.Owner!);
    }

    public async Task<bool> DeleteMvpAsync(Guid mvpId)
    {
        var mvp = await _mvpRepository.GetByIdAsync(mvpId);
        if (mvp == null)
        {
            throw new Exception("MVP not found");
        }

        await _mvpRepository.DeleteAsync(mvp);

        return true;
    }

    public async Task<List<MvpDto>> GetMvpListAsync(MvpListQuery query)
    {
        decimal? minPrice = null;
        decimal? maxPrice = null;
        if (query.PriceRange != null && query.PriceRange.Count == 2)
        {
            minPrice = query.PriceRange[0];
            maxPrice = query.PriceRange[1];
        }

        DateTime? startDate = null;
        DateTime? endDate = null;
        if (query.DateRange != null && query.DateRange.Count == 2)
        {
            if (DateTime.TryParse(query.DateRange[0], out var start)) startDate = start;
            if (DateTime.TryParse(query.DateRange[1], out var end)) endDate = end;
        }

        var mvps = await _mvpRepository.ListAsync(query.Category, query.Technology, minPrice, maxPrice, startDate, endDate);

        return mvps.Select(m => MapToDto(m, m.Owner!)).ToList();
    }

    public async Task<MvpDetailsDto> GetMvpDetailsAsync(Guid id)
    {
        var mvp = await _mvpRepository.GetByIdAsync(id);
        if (mvp == null)
        {
            throw new Exception("MVP not found");
        }

        return MapToDetailsDto(mvp, mvp.Owner!);
    }

    public async Task<PagedResult<MvpDto>> GetUserMvpsAsync(Guid userId, int pageNumber, int pageSize)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // Max 100 items per page

        var (items, totalCount) = await _mvpRepository.GetByOwnerIdAsync(userId, pageNumber, pageSize);

        return new PagedResult<MvpDto>
        {
            Items = items.Select(m => MapToDto(m, m.Owner!)).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private MvpDto MapToDto(Mvp mvp, User owner)
    {
        return new MvpDto
        {
            Id = mvp.Id,
            Name = mvp.Name,
            Description = mvp.Description,
            Technologies = mvp.Technologies,
            Categories = mvp.Categories,
            ImageUrl = mvp.ImageUrl,
            Price = mvp.Price,
            CreatedAt = mvp.CreatedAt,
            UpdatedAt = mvp.UpdatedAt,
            Owner = new OwnerDto
            {
                Id = owner.Id,
                Name = owner.Name,
                Email = owner.Email
            },
            ProductType = mvp.ProductType.ToString(),
            Link = mvp.Link,
            PreviewLink = mvp.PreviewLink,
            GitHubBusinessType = mvp.GitHubBusinessType?.ToString(),
            DriveBusinessType = mvp.DriveBusinessType?.ToString()
        };
    }

    private MvpDetailsDto MapToDetailsDto(Mvp mvp, User owner)
    {
        return new MvpDetailsDto
        {
            Id = mvp.Id,
            Name = mvp.Name,
            Description = mvp.Description,
            Technologies = mvp.Technologies,
            Categories = mvp.Categories,
            ImageUrl = mvp.ImageUrl,
            Price = mvp.Price,
            CreatedAt = mvp.CreatedAt,
            UpdatedAt = mvp.UpdatedAt,
            Owner = new OwnerDto
            {
                Id = owner.Id,
                Name = owner.Name,
                Email = owner.Email
            },
            Highlights = mvp.Highlights,
            Objective = mvp.Objective,
            MainFeatures = mvp.MainFeatures,
            Status = mvp.Status,
            Screenshots = mvp.Screenshots,
            ProductType = mvp.ProductType.ToString(),
            Link = mvp.Link,
            PreviewLink = mvp.PreviewLink,
            GitHubBusinessType = mvp.GitHubBusinessType?.ToString(),
            DriveBusinessType = mvp.DriveBusinessType?.ToString()
        };
    }

    public async Task<ValidateTokenResponse> ValidateMvpTokenAsync(Guid mvpId)
    {
        var mvp = await _mvpRepository.GetByIdAsync(mvpId);
        if (mvp == null)
        {
            return new ValidateTokenResponse
            {
                IsValid = false,
                Message = "MVP not found",
                Username = null
            };
        }

        // Validate based on product type
        if (mvp.ProductType == MvpProductType.GitHubRepo)
        {
            if (string.IsNullOrEmpty(mvp.GitHubPatToken))
            {
                return new ValidateTokenResponse
                {
                    IsValid = false,
                    Message = "GitHub PAT token not found for this MVP",
                    Username = null
                };
            }

            try
            {
                var decryptedToken = _encryptionService.Decrypt(mvp.GitHubPatToken);
                // Actually validate the token with GitHub API
                var (isValid, username) = await _gitHubService.ValidateTokenAsync(decryptedToken);

                return new ValidateTokenResponse
                {
                    IsValid = isValid,
                    Message = isValid ? "GitHub token is valid" : "GitHub token is invalid or expired",
                    Username = username
                };
            }
            catch (Exception ex)
            {
                return new ValidateTokenResponse
                {
                    IsValid = false,
                    Message = $"Failed to decrypt GitHub token: {ex.Message}",
                    Username = null
                };
            }
        }
        else if (mvp.ProductType == MvpProductType.Drive)
        {
            if (string.IsNullOrEmpty(mvp.GoogleOAuthToken))
            {
                return new ValidateTokenResponse
                {
                    IsValid = false,
                    Message = "Google OAuth token not found for this MVP",
                    Username = null
                };
            }

            try
            {
                var decryptedToken = _encryptionService.Decrypt(mvp.GoogleOAuthToken);
                // Actually validate the token with Google Drive API
                var (isValid, email) = await _googleDriveService.ValidateTokenAsync(decryptedToken);

                return new ValidateTokenResponse
                {
                    IsValid = isValid,
                    Message = isValid ? "Google Drive token is valid" : "Google Drive token is invalid or expired",
                    Username = email
                };
            }
            catch (Exception ex)
            {
                return new ValidateTokenResponse
                {
                    IsValid = false,
                    Message = $"Failed to decrypt Google OAuth token: {ex.Message}",
                    Username = null
                };
            }
        }

        return new ValidateTokenResponse
        {
            IsValid = false,
            Message = "Unknown product type",
            Username = null
        };
    }
}
