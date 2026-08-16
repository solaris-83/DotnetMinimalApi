namespace DotnetMinimalApi.Models.Dtos;

/// <summary>
/// Payload for submitting a review for a product.
/// </summary>
public record ReviewCreateDto(
    string AuthorName,
    int Rating,
    string? Comment
);

/// <summary>
/// Response representation of a single product review.
/// </summary>
public record ReviewResponseDto(
    int Id,
    int ProductId,
    string AuthorName,
    int Rating,
    string? Comment,
    DateTime CreatedAtUtc
);

/// <summary>
/// Aggregated review summary for a specific product.
/// </summary>
public record ProductReviewSummaryDto(
    int ProductId,
    string ProductName,
    double AverageRating,
    int TotalReviews,
    IReadOnlyList<ReviewResponseDto> Reviews
);
