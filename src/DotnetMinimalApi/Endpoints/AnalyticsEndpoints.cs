using DotnetMinimalApi.Common.Filters;
using DotnetMinimalApi.Data;
using DotnetMinimalApi.Models.Dtos;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace DotnetMinimalApi.Endpoints;

public class AnalyticsEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/analytics")
            .WithTags("Analytics & Reports")
            .AddEndpointFilter<RequestTimingFilter>();

        group.MapGet("/summary", GetCatalogSummary)
            .WithName("GetCatalogSummary")
            .WithSummary("Get catalog financial and inventory summary")
            .WithDescription("Calculates real-time aggregate metrics across all products, categories, reviews, and total inventory value.");

        group.MapGet("/low-stock", GetLowStockProducts)
            .WithName("GetLowStockProducts")
            .WithSummary("Get products requiring inventory restocking")
            .WithDescription("Lists products where stock level is at or below the specified threshold.");
    }

    private async Task<Ok<CatalogSummaryDto>> GetCatalogSummary(
        AppDbContext db,
        IConfiguration config,
        CancellationToken ct)
    {
        var lowStockThreshold = config.GetValue("CatalogSettings:LowStockThreshold", 5);

        var totalProducts = await db.Products.CountAsync(ct);
        var activeProducts = await db.Products.CountAsync(p => p.IsActive, ct);
        var outOfStockProducts = await db.Products.CountAsync(p => p.StockQuantity == 0, ct);
        var lowStockProducts = await db.Products.CountAsync(p => p.StockQuantity > 0 && p.StockQuantity <= lowStockThreshold, ct);

        // Fetch products for valuation to avoid SQLite decimal translation quirks
        var productValues = await db.Products
            .AsNoTracking()
            .Select(p => new { p.Price, p.StockQuantity })
            .ToListAsync(ct);

        var totalInventoryValue = productValues.Sum(p => p.Price * p.StockQuantity);
        var totalCategories = await db.Categories.CountAsync(ct);
        var totalReviews = await db.Reviews.CountAsync(ct);

        var overallRating = await db.Reviews.AnyAsync(ct)
            ? Math.Round(await db.Reviews.AverageAsync(r => (double)r.Rating, ct), 1)
            : 0.0;

        var rawCategoryStats = await db.Categories
            .AsNoTracking()
            .Select(c => new
            {
                c.Id,
                c.Name,
                Products = c.Products.Select(p => new { p.Price, p.StockQuantity }).ToList()
            })
            .ToListAsync(ct);

        var categoryBreakdown = rawCategoryStats
            .Select(c => new CategoryStockSummaryDto(
                c.Id,
                c.Name,
                c.Products.Count,
                c.Products.Sum(p => p.Price * p.StockQuantity)))
            .OrderByDescending(c => c.TotalValue)
            .ToList();

        var summary = new CatalogSummaryDto(
            totalProducts,
            activeProducts,
            outOfStockProducts,
            lowStockProducts,
            totalInventoryValue,
            totalCategories,
            totalReviews,
            overallRating,
            categoryBreakdown);

        return TypedResults.Ok(summary);
    }

    private async Task<Ok<IReadOnlyList<LowStockProductDto>>> GetLowStockProducts(
        int? threshold,
        AppDbContext db,
        IConfiguration config,
        CancellationToken ct)
    {
        var actualThreshold = threshold ?? config.GetValue("CatalogSettings:LowStockThreshold", 5);

        var lowStockProducts = await db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.StockQuantity <= actualThreshold)
            .OrderBy(p => p.StockQuantity)
            .Select(p => new LowStockProductDto(
                p.Id,
                p.Name,
                p.Sku,
                p.StockQuantity,
                p.Price,
                p.Category.Name))
            .ToListAsync(ct);

        return TypedResults.Ok<IReadOnlyList<LowStockProductDto>>(lowStockProducts);
    }
}
