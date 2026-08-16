using System.Text.RegularExpressions;
using DotnetMinimalApi.Common.Filters;
using DotnetMinimalApi.Common.Pagination;
using DotnetMinimalApi.Data;
using DotnetMinimalApi.Models.Dtos;
using DotnetMinimalApi.Models.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotnetMinimalApi.Endpoints;

public static partial class CategoryEndpoints
{
    public static RouteGroupBuilder MapCategoryEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/categories")
            .WithTags("Categories")
            .AddEndpointFilter<RequestTimingFilter>();

        group.MapGet("/", GetAllCategories)
            .WithName("GetAllCategories")
            .WithSummary("Get all categories")
            .WithDescription("Retrieves a list of all product categories with their respective product counts.");

        group.MapGet("/{id:int}", GetCategoryById)
            .WithName("GetCategoryById")
            .WithSummary("Get category by ID")
            .WithDescription("Retrieves detailed category information including financial inventory valuation and preview of top products.");

        group.MapGet("/{id:int}/products", GetCategoryProducts)
            .WithName("GetCategoryProducts")
            .WithSummary("Get products in category")
            .WithDescription("Retrieves a paginated list of products belonging to the specified category.");

        group.MapPost("/", CreateCategory)
            .WithName("CreateCategory")
            .WithSummary("Create a new category")
            .WithDescription("Creates a new product category with automatic slug generation.")
            .AddEndpointFilter<ValidationFilter<CategoryCreateDto>>();

        group.MapPut("/{id:int}", UpdateCategory)
            .WithName("UpdateCategory")
            .WithSummary("Update an existing category")
            .WithDescription("Updates name and description for an existing category.")
            .AddEndpointFilter<ValidationFilter<CategoryUpdateDto>>();

        group.MapDelete("/{id:int}", DeleteCategory)
            .WithName("DeleteCategory")
            .WithSummary("Delete a category")
            .WithDescription("Deletes a category if it has no associated products.");

        return group;
    }

    public static async Task<Ok<IReadOnlyList<CategoryResponseDto>>> GetAllCategories(
        AppDbContext db,
        CancellationToken ct)
    {
        var categories = await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryResponseDto(
                c.Id,
                c.Name,
                c.Slug,
                c.Description,
                c.Products.Count,
                c.CreatedAtUtc,
                c.UpdatedAtUtc))
            .ToListAsync(ct);

        return TypedResults.Ok<IReadOnlyList<CategoryResponseDto>>(categories);
    }

    public static async Task<Results<Ok<CategoryDetailDto>, ProblemHttpResult>> GetCategoryById(
        int id,
        AppDbContext db,
        CancellationToken ct)
    {
        var category = await db.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category is null)
        {
            return TypedResults.Problem(
                title: "Category Not Found",
                detail: $"Category with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var topProducts = category.Products
            .OrderByDescending(p => p.Price)
            .Take(5)
            .Select(p => new ProductSummaryDto(
                p.Id,
                p.Name,
                p.Sku,
                p.Price,
                p.StockQuantity,
                p.IsActive))
            .ToList();

        var totalValuation = category.Products.Sum(p => p.Price * p.StockQuantity);

        var detail = new CategoryDetailDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.Products.Count,
            totalValuation,
            category.CreatedAtUtc,
            category.UpdatedAtUtc,
            topProducts);

        return TypedResults.Ok(detail);
    }

    public static async Task<Results<Ok<PagedList<ProductResponseDto>>, ProblemHttpResult>> GetCategoryProducts(
        int id,
        [AsParameters] PaginationParams pagination,
        AppDbContext db,
        CancellationToken ct)
    {
        var categoryExists = await db.Categories.AnyAsync(c => c.Id == id, ct);
        if (!categoryExists)
        {
            return TypedResults.Problem(
                title: "Category Not Found",
                detail: $"Category with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var pageNumber = pagination.PageNumber ?? 1;
        var pageSize = pagination.PageSize ?? 10;

        var query = db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .Where(p => p.CategoryId == id)
            .OrderBy(p => p.Name);

        var totalCount = await query.CountAsync(ct);
        var products = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = products
            .Select(p => new ProductResponseDto(
                p.Id,
                p.Name,
                p.Sku,
                p.Description,
                p.Price,
                p.StockQuantity,
                p.IsActive,
                p.CategoryId,
                p.Category.Name,
                p.Reviews.Count > 0 ? Math.Round(p.Reviews.Average(r => r.Rating), 1) : 0.0,
                p.Reviews.Count,
                p.CreatedAtUtc,
                p.UpdatedAtUtc))
            .ToList();

        return TypedResults.Ok(new PagedList<ProductResponseDto>(items, totalCount, pageNumber, pageSize));
    }

    public static async Task<Results<CreatedAtRoute<CategoryResponseDto>, ValidationProblem, ProblemHttpResult>> CreateCategory(
        CategoryCreateDto dto,
        AppDbContext db,
        CancellationToken ct)
    {
        var slug = GenerateSlug(dto.Name);

        var slugExists = await db.Categories.AnyAsync(c => c.Slug == slug, ct);
        if (slugExists)
        {
            return TypedResults.Problem(
                title: "Duplicate Category",
                detail: $"A category with name '{dto.Name}' (slug '{slug}') already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var category = new Category
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            Description = dto.Description?.Trim()
        };

        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);

        var responseDto = new CategoryResponseDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            0,
            category.CreatedAtUtc,
            category.UpdatedAtUtc);

        return TypedResults.CreatedAtRoute(
            responseDto,
            routeName: "GetCategoryById",
            routeValues: new { id = category.Id });
    }

    public static async Task<Results<Ok<CategoryResponseDto>, ValidationProblem, ProblemHttpResult>> UpdateCategory(
        int id,
        CategoryUpdateDto dto,
        AppDbContext db,
        CancellationToken ct)
    {
        var category = await db.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category is null)
        {
            return TypedResults.Problem(
                title: "Category Not Found",
                detail: $"Category with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var newSlug = GenerateSlug(dto.Name);
        if (category.Slug != newSlug)
        {
            var slugExists = await db.Categories.AnyAsync(c => c.Slug == newSlug && c.Id != id, ct);
            if (slugExists)
            {
                return TypedResults.Problem(
                    title: "Duplicate Category Name",
                    detail: $"Another category already generates the slug '{newSlug}'.",
                    statusCode: StatusCodes.Status409Conflict);
            }
            category.Slug = newSlug;
        }

        category.Name = dto.Name.Trim();
        category.Description = dto.Description?.Trim();

        await db.SaveChangesAsync(ct);

        var responseDto = new CategoryResponseDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.Products.Count,
            category.CreatedAtUtc,
            category.UpdatedAtUtc);

        return TypedResults.Ok(responseDto);
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> DeleteCategory(
        int id,
        AppDbContext db,
        CancellationToken ct)
    {
        var category = await db.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (category is null)
        {
            return TypedResults.Problem(
                title: "Category Not Found",
                detail: $"Category with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (category.Products.Count > 0)
        {
            return TypedResults.Problem(
                title: "Cannot Delete Category",
                detail: $"Category '{category.Name}' contains {category.Products.Count} products. Reassign or delete those products first.",
                statusCode: StatusCodes.Status409Conflict);
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    private static string GenerateSlug(string phrase)
    {
        var str = phrase.ToLowerInvariant().Trim();
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
        str = Regex.Replace(str, @"\s+", " ").Trim();
        str = Regex.Replace(str, @"\s", "-");
        return str;
    }
}
