namespace DotnetMinimalApi.Models.Dtos;

/// <summary>
/// Payload for creating a new product category.
/// </summary>
public record CategoryCreateDto(
    string Name,
    string? Description
);

/// <summary>
/// Payload for updating an existing product category.
/// </summary>
public record CategoryUpdateDto(
    string Name,
    string? Description
);

/// <summary>
/// Response representation of a product category.
/// </summary>
public record CategoryResponseDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    int ProductCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

/// <summary>
/// Detailed response representation of a category including top product previews.
/// </summary>
public record CategoryDetailDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    int ProductCount,
    decimal TotalInventoryValue,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<ProductSummaryDto> TopProducts
);
