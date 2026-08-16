using DotnetMinimalApi.Common.Pagination;

namespace DotnetMinimalApi.Models.Dtos;

/// <summary>
/// Payload for creating a new product.
/// </summary>
public record ProductCreateDto(
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    bool IsActive = true
);

/// <summary>
/// Payload for updating an existing product.
/// </summary>
public record ProductUpdateDto(
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity,
    int CategoryId,
    bool IsActive
);

/// <summary>
/// Payload for adjusting product inventory stock.
/// </summary>
public record ProductStockAdjustmentDto(
    int Adjustment,
    string? Reason
);

/// <summary>
/// Payload for updating the active status of a product.
/// </summary>
public record ProductStatusUpdateDto(
    bool IsActive
);

/// <summary>
/// Detailed response representation of a product.
/// </summary>
public record ProductResponseDto(
    int Id,
    string Name,
    string Sku,
    string? Description,
    decimal Price,
    int StockQuantity,
    bool IsActive,
    int CategoryId,
    string CategoryName,
    double AverageRating,
    int ReviewCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

/// <summary>
/// Compact summary response of a product.
/// </summary>
public record ProductSummaryDto(
    int Id,
    string Name,
    string Sku,
    decimal Price,
    int StockQuantity,
    bool IsActive
);

/// <summary>
/// Query filters for product searching and filtering.
/// </summary>
public class ProductFilterParams : PaginationParams
{
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? InStockOnly { get; set; }
    public bool? IsActive { get; set; }
}
