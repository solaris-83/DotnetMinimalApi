namespace DotnetMinimalApi.Models.Dtos;

/// <summary>
/// Aggregated catalog statistics and financial summary.
/// </summary>
public record CatalogSummaryDto(
    int TotalProducts,
    int ActiveProducts,
    int OutOfStockProducts,
    int LowStockProducts,
    decimal TotalInventoryValue,
    int TotalCategories,
    int TotalReviews,
    double OverallAverageRating,
    IReadOnlyList<CategoryStockSummaryDto> CategoryBreakdown
);

/// <summary>
/// Category inventory valuation breakdown.
/// </summary>
public record CategoryStockSummaryDto(
    int CategoryId,
    string CategoryName,
    int ProductCount,
    decimal TotalValue
);

/// <summary>
/// Report item for products requiring inventory restocking.
/// </summary>
public record LowStockProductDto(
    int Id,
    string Name,
    string Sku,
    int StockQuantity,
    decimal Price,
    string CategoryName
);
