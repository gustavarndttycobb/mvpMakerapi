using System.ComponentModel.DataAnnotations.Schema;

namespace MvpMakerApi.Domain.Entities;

public class Mvp
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Stored as JSON
    public List<string> Technologies { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Price { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public Guid OwnerId { get; set; }
    public User? Owner { get; set; }
    
    // Details
    public List<string> Highlights { get; set; } = new();
    public string Objective { get; set; } = string.Empty;
    public List<string> MainFeatures { get; set; } = new();
    public string Status { get; set; } = "in progress"; // "completed" | "in progress"
    public List<string> Screenshots { get; set; } = new();
}
