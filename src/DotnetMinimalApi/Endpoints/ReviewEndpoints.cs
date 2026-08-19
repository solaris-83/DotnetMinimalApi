using DotnetMinimalApi.Common.Filters;
using DotnetMinimalApi.Data;
using DotnetMinimalApi.Models.Dtos;
using DotnetMinimalApi.Models.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DotnetMinimalApi.Endpoints;

public class ReviewEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes)
    {
        var productReviewsGroup = routes.MapGroup("/api/products/{productId:int}/reviews")
            .WithTags("Reviews")
            .AddEndpointFilter<RequestTimingFilter>();

        productReviewsGroup.MapGet("/", GetProductReviews)
            .WithName("GetProductReviews")
            .WithSummary("Get reviews for a product")
            .WithDescription("Retrieves customer reviews and aggregate rating calculation for a product.");

        productReviewsGroup.MapPost("/", CreateProductReview)
            .WithName("CreateProductReview")
            .WithSummary("Add a review to a product")
            .WithDescription("Submits a customer rating (1-5) and review comment for a product.")
            .AddEndpointFilter<ValidationFilter<ReviewCreateDto>>();

        var reviewsGroup = routes.MapGroup("/api/reviews")
            .WithTags("Reviews")
            .AddEndpointFilter<RequestTimingFilter>();

        reviewsGroup.MapDelete("/{id:int}", DeleteReview)
            .WithName("DeleteReview")
            .WithSummary("Delete a review")
            .WithDescription("Removes a specific review by ID.");

        return productReviewsGroup;
    }

    private async Task<Results<Ok<ProductReviewSummaryDto>, ProblemHttpResult>> GetProductReviews(
        int productId,
        AppDbContext db,
        CancellationToken ct)
    {
        var product = await db.Products
            .AsNoTracking()
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == productId, ct);

        if (product is null)
        {
            return TypedResults.Problem(
                title: "Product Not Found",
                detail: $"Product with ID {productId} was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var reviewDtos = product.Reviews
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new ReviewResponseDto(
                r.Id,
                r.ProductId,
                r.AuthorName,
                r.Rating,
                r.Comment,
                r.CreatedAtUtc))
            .ToList();

        var averageRating = reviewDtos.Count > 0 ? Math.Round(reviewDtos.Average(r => r.Rating), 1) : 0.0;

        var summary = new ProductReviewSummaryDto(
            product.Id,
            product.Name,
            averageRating,
            reviewDtos.Count,
            reviewDtos);

        return TypedResults.Ok(summary);
    }

    private async Task<Results<CreatedAtRoute<ReviewResponseDto>, ValidationProblem, ProblemHttpResult>> CreateProductReview(
        int productId,
        ReviewCreateDto dto,
        AppDbContext db,
        CancellationToken ct)
    {
        var product = await db.Products.FindAsync([productId], ct);
        if (product is null)
        {
            return TypedResults.Problem(
                title: "Product Not Found",
                detail: $"Product with ID {productId} was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        var review = new Review
        {
            ProductId = productId,
            AuthorName = dto.AuthorName.Trim(),
            Rating = dto.Rating,
            Comment = dto.Comment?.Trim()
        };

        db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);

        var responseDto = new ReviewResponseDto(
            review.Id,
            review.ProductId,
            review.AuthorName,
            review.Rating,
            review.Comment,
            review.CreatedAtUtc);

        return TypedResults.CreatedAtRoute(
            responseDto,
            routeName: "GetProductReviews",
            routeValues: new { productId });
    }

    private async Task<Results<NoContent, ProblemHttpResult>> DeleteReview(
        int id,
        AppDbContext db,
        CancellationToken ct)
    {
        var review = await db.Reviews.FindAsync([id], ct);
        if (review is null)
        {
            return TypedResults.Problem(
                title: "Review Not Found",
                detail: $"Review with ID {id} was not found.",
                statusCode: StatusCodes.Status404NotFound);
        }

        db.Reviews.Remove(review);
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
