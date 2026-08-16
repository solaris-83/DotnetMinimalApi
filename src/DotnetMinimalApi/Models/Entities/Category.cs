namespace DotnetMinimalApi.Models.Entities;

/// <summary>
/// Represents a product category in the catalog.
/// </summary>
public class Category : BaseEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }

    // Navigation property
    public ICollection<Product> Products { get; set; } = new List<Product>();
}
