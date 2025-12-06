namespace MvpMakerApi.Application.DTOs;

public class MvpListQuery
{
    public List<string>? Category { get; set; }
    public List<string>? Technology { get; set; }
    public List<decimal>? PriceRange { get; set; } // [min, max]
    public List<string>? DateRange { get; set; } // [start, end]
}

public class MvpDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Technologies { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public OwnerDto Owner { get; set; } = new();

    public string ProductType { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public string PreviewLink { get; set; } = string.Empty;
    public string? GitHubBusinessType { get; set; }
    public string? DriveBusinessType { get; set; } // Transfer or Share (only for Drive)
}

public class OwnerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class MvpDetailsDto : MvpDto
{
    public List<string> Highlights { get; set; } = new();
    public string Objective { get; set; } = string.Empty;
    public List<string> MainFeatures { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public List<string> Screenshots { get; set; } = new();
}

public class CreateMvpRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Technologies { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public List<string> Highlights { get; set; } = new();
    public string Objective { get; set; } = string.Empty;
    public List<string> MainFeatures { get; set; } = new();
    public string Status { get; set; } = "in progress";
    public List<string> Screenshots { get; set; } = new();

    public string ProductType { get; set; } = "GitHubRepo"; // GitHubRepo, Drive
    public string Link { get; set; } = string.Empty;
    public string PreviewLink { get; set; } = string.Empty;
    public string? GitHubBusinessType { get; set; } // Transfer or Fork (only for GitHubRepo)
    public string? DriveBusinessType { get; set; } // Transfer or Share (only for Drive)
}

public class UpdateMvpRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Technologies { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public List<string> Highlights { get; set; } = new();
    public string Objective { get; set; } = string.Empty;
    public List<string> MainFeatures { get; set; } = new();
    public string Status { get; set; } = "in progress";
    public List<string> Screenshots { get; set; } = new();

    public string ProductType { get; set; } = "GitHubRepo";
    public string Link { get; set; } = string.Empty;
    public string PreviewLink { get; set; } = string.Empty;
    public string? GitHubBusinessType { get; set; } // Transfer or Fork (only for GitHubRepo)
    public string? DriveBusinessType { get; set; } // Transfer or Share (only for Drive)
}

public class DeleteMvpRequest
{
    public Guid Id { get; set; }
}

// Category DTOs
public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

// Technology DTOs
public class TechnologyDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateTechnologyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

// Pagination
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

// Transaction DTOs
public class TransactionDto
{
    public Guid Id { get; set; }

    // IDs for filtering
    public Guid SellerId { get; set; }
    public Guid BuyerId { get; set; }
    public Guid MvpId { get; set; }

    // Related objects
    public MvpDto Mvp { get; set; } = new();
    public OwnerDto Seller { get; set; } = new();
    public OwnerDto Buyer { get; set; } = new();

    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // GitHub Transfer Fields
    public string? ProductType { get; set; }
    public string? RepoUrl { get; set; }
    public string? BuyerGitHubUsername { get; set; }
}

public class PurchaseMvpRequest
{
    // Empty for now, can add payment method later
}

public class PurchaseResponse
{
    public Guid TransactionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? CheckoutUrl { get; set; }
    public string? CheckoutSessionId { get; set; }
}

// GitHub DTOs
public class ConnectGitHubRequest
{
    public string GitHubUsername { get; set; } = string.Empty;
    public string GitHubToken { get; set; } = string.Empty;
}

public class GitHubStatusDto
{
    public bool IsConnected { get; set; }
    public string? Username { get; set; }
}
