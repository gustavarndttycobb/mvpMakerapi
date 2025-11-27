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
