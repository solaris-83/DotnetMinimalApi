namespace DotnetMinimalApi.Models.Entities;

/// <summary>
/// Represents a customer rating and review for a product.
/// </summary>
public class Review : BaseEntity
{
    public required string AuthorName { get; set; }
    public int Rating { get; set; } // 1 - 5
    public string? Comment { get; set; }

    // Foreign Key & Navigation property
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
