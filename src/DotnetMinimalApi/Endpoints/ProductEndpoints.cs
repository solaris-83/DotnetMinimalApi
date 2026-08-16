using DotnetMinimalApi.Common.Filters;
using DotnetMinimalApi.Common.Pagination;
using DotnetMinimalApi.Data;
using DotnetMinimalApi.Models.Dtos;
using DotnetMinimalApi.Models.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotnetMinimalApi.Endpoints;

public static class ProductEndpoints
{
    public static RouteGroupBuilder MapProductEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/products")
            .WithTags("Products")
            .AddEndpointFilter<RequestTimingFilter>();

        group.MapGet("/", GetAllProducts)
            .WithName("GetAllProducts")
            .WithSummary("Get paginated list of products")
            .WithDescription("Retrieves products with optional filtering by category, price range, stock availability, search term, and sorting.");

        group.MapGet("/{id:int}", GetProductById)
            .WithName("GetProductById")
            .WithSummary("Get product by ID")
            .WithDescription("Retrieves detailed product information including rating averages.");

        group.MapGet("/sku/{sku}", GetProductBySku)
            .WithName("GetProductBySku")
            .WithSummary("Get product by SKU")
            .WithDescription("Retrieves product details matching the specified unique SKU.");

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .WithSummary("Create a new product")
            .WithDescription("Creates a new product in the catalog with SKU uniqueness and category validation.")
            .AddEndpointFilter<ValidationFilter<ProductCreateDto>>();

        group.MapPut("/{id:int}", UpdateProduct)
            .WithName("UpdateProduct")
            .WithSummary("Update an existing product")
            .WithDescription("Updates all fields of an existing product.")
            .AddEndpointFilter<ValidationFilter<ProductUpdateDto>>();

        group.MapDelete("/{id:int}", DeleteProduct)
            .WithName("DeleteProduct")
            .WithSummary("Delete a product")
            .WithDescription("Deletes a product and its associated reviews.");

        group.MapPatch("/{id:int}/stock", AdjustProductStock)
            .WithName("AdjustProductStock")
            .WithSummary("Adjust product inventory stock")
            .WithDescription("Applies a positive or negative quantity adjustment to the product's inventory.")
            .AddEndpointFilter<ValidationFilter<ProductStockAdjustmentDto>>();

        group.MapPatch("/{id:int}/status", ToggleProductStatus)
            .WithName("ToggleProductStatus")
            .WithSummary("Toggle product active status")
            .WithDescription("Enables or disables product visibility in the store.");

        return group;
    }

    public static async Task<Ok<PagedList<ProductResponseDto>>> GetAllProducts(
        [AsParameters] ProductFilterParams filter,
        AppDbContext db,
        CancellationToken ct)
    {
        var pageNumber = filter.PageNumber ?? 1;
        var pageSize = filter.PageSize ?? 10;
        var sortDescending = filter.SortDescending ?? false;

        var query = db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .AsQueryable();

        // Filters
        if (filter.CategoryId.HasValue)
        {
            query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);
        }

        if (filter.InStockOnly.HasValue && filter.InStockOnly.Value)
        {
            query = query.Where(p => p.StockQuantity > 0);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(p => p.IsActive == filter.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(search) ||
                p.Sku.ToLower().Contains(search) ||
                (p.Description != null && p.Description.ToLower().Contains(search)));
        }

        // Sorting
        query = filter.SortBy?.ToLowerInvariant() switch
        {
            "name" => sortDescending ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
            "price" => sortDescending ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
            "stock" => sortDescending ? query.OrderByDescending(p => p.StockQuantity) : query.OrderBy(p => p.StockQuantity),
            "createdat" => sortDescending ? query.OrderByDescending(p => p.CreatedAtUtc) : query.OrderBy(p => p.CreatedAtUtc),
            _ => query.OrderByDescending(p => p.CreatedAtUtc)
        };

        var totalCount = await query.CountAsync(ct);
        var products = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var items = products
            .Select(p => ToResponseDto(p))
            .ToList();

        var pagedList = new PagedList<ProductResponseDto>(items, totalCount, pageNumber, pageSize);
        return TypedResults.Ok(pagedList);
    }

    public static async Task<Results<Ok<ProductResponseDto>, ProblemHttpResult>> GetProductById(
        int id,
        AppDbContext db,
        CancellationToken ct)
    {
        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
        {
            return TypedResults.Problem(
                title: "Product Not Found",
                detail: $"Product with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(ToResponseDto(product));
    }

    public static async Task<Results<Ok<ProductResponseDto>, ProblemHttpResult>> GetProductBySku(
        string sku,
        AppDbContext db,
        CancellationToken ct)
    {
        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Sku.ToUpper() == sku.ToUpper(), ct);

        if (product is null)
        {
            return TypedResults.Problem(
                title: "Product Not Found",
                detail: $"Product with SKU '{sku}' was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return TypedResults.Ok(ToResponseDto(product));
    }

    public static async Task<Results<CreatedAtRoute<ProductResponseDto>, ValidationProblem, ProblemHttpResult>> CreateProduct(
        ProductCreateDto dto,
        AppDbContext db,
        CancellationToken ct)
    {
        var categoryExists = await db.Categories.AnyAsync(c => c.Id == dto.CategoryId, ct);
        if (!categoryExists)
        {
            return TypedResults.Problem(
                title: "Category Not Found",
                detail: $"Category with ID {dto.CategoryId} does not exist.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var skuExists = await db.Products.AnyAsync(p => p.Sku.ToUpper() == dto.Sku.ToUpper(), ct);
        if (skuExists)
        {
            return TypedResults.Problem(
                title: "Duplicate SKU",
                detail: $"A product with SKU '{dto.Sku}' already exists.",
                statusCode: StatusCodes.Status409Conflict);
        }

        var product = new Product
        {
            Name = dto.Name.Trim(),
            Sku = dto.Sku.Trim().ToUpperInvariant(),
            Description = dto.Description?.Trim(),
            Price = dto.Price,
            StockQuantity = dto.StockQuantity,
            IsActive = dto.IsActive,
            CategoryId = dto.CategoryId
        };

        db.Products.Add(product);
        await db.SaveChangesAsync(ct);

        await db.Entry(product).Reference(p => p.Category).LoadAsync(ct);

        var responseDto = ToResponseDto(product);

        return TypedResults.CreatedAtRoute(
            responseDto,
            routeName: "GetProductById",
            routeValues: new { id = product.Id });
    }

    public static async Task<Results<Ok<ProductResponseDto>, ValidationProblem, ProblemHttpResult>> UpdateProduct(
        int id,
        ProductUpdateDto dto,
        AppDbContext db,
        CancellationToken ct)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
        {
            return TypedResults.Problem(
                title: "Product Not Found",
                detail: $"Product with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        if (product.CategoryId != dto.CategoryId)
        {
            var categoryExists = await db.Categories.AnyAsync(c => c.Id == dto.CategoryId, ct);
            if (!categoryExists)
            {
                return TypedResults.Problem(
                    title: "Category Not Found",
                    detail: $"Category with ID {dto.CategoryId} does not exist.",
                    statusCode: StatusCodes.Status404NotFound);
            }
        }

        var normalizedSku = dto.Sku.Trim().ToUpperInvariant();
        if (product.Sku != normalizedSku)
        {
            var skuExists = await db.Products.AnyAsync(p => p.Sku.ToUpper() == normalizedSku && p.Id != id, ct);
            if (skuExists)
            {
                return TypedResults.Problem(
                    title: "Duplicate SKU",
                    detail: $"A product with SKU '{dto.Sku}' already exists.",
                    statusCode: StatusCodes.Status409Conflict);
            }
        }

        product.Name = dto.Name.Trim();
        product.Sku = normalizedSku;
        product.Description = dto.Description?.Trim();
        product.Price = dto.Price;
        product.StockQuantity = dto.StockQuantity;
        product.CategoryId = dto.CategoryId;
        product.IsActive = dto.IsActive;

        await db.SaveChangesAsync(ct);

        await db.Entry(product).Reference(p => p.Category).LoadAsync(ct);

        return TypedResults.Ok(ToResponseDto(product));
    }

    public static async Task<Results<NoContent, ProblemHttpResult>> DeleteProduct(
        int id,
        AppDbContext db,
        CancellationToken ct)
    {
        var product = await db.Products.FindAsync([id], ct);
        if (product is null)
        {
            return TypedResults.Problem(
                title: "Product Not Found",
                detail: $"Product with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        db.Products.Remove(product);
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    public static async Task<Results<Ok<ProductResponseDto>, ProblemHttpResult>> AdjustProductStock(
        int id,
        ProductStockAdjustmentDto dto,
        AppDbContext db,
        CancellationToken ct)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
        {
            return TypedResults.Problem(
                title: "Product Not Found",
                detail: $"Product with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var newQuantity = product.StockQuantity + dto.Adjustment;
        if (newQuantity < 0)
        {
            return TypedResults.Problem(
                title: "Invalid Stock Adjustment",
                detail: $"Cannot reduce stock by {Math.Abs(dto.Adjustment)}. Current stock is {product.StockQuantity}.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        product.StockQuantity = newQuantity;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ToResponseDto(product));
    }

    public static async Task<Results<Ok<ProductResponseDto>, ProblemHttpResult>> ToggleProductStatus(
        int id,
        ProductStatusUpdateDto dto,
        AppDbContext db,
        CancellationToken ct)
    {
        var product = await db.Products
            .Include(p => p.Category)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (product is null)
        {
            return TypedResults.Problem(
                title: "Product Not Found",
                detail: $"Product with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        product.IsActive = dto.IsActive;
        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(ToResponseDto(product));
    }

    private static ProductResponseDto ToResponseDto(Product p) => new(
        p.Id,
        p.Name,
        p.Sku,
        p.Description,
        p.Price,
        p.StockQuantity,
        p.IsActive,
        p.CategoryId,
        p.Category?.Name ?? string.Empty,
        p.Reviews != null && p.Reviews.Count > 0 ? Math.Round(p.Reviews.Average(r => r.Rating), 1) : 0.0,
        p.Reviews?.Count ?? 0,
        p.CreatedAtUtc,
        p.UpdatedAtUtc
    );
}
