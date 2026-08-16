namespace DotnetMinimalApi.Models.Entities;

/// <summary>
/// Represents a product item in the inventory catalog.
/// </summary>
public class Product : BaseEntity
{
    public required string Name { get; set; }
    public required string Sku { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;

    // Foreign Key & Navigation property
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    // Navigation property
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}
