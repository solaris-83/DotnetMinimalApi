using DotnetMinimalApi.Models.Dtos;
using FluentValidation;

namespace DotnetMinimalApi.Validation;

public class ReviewCreateValidator : AbstractValidator<ReviewCreateDto>
{
    public ReviewCreateValidator()
    {
        RuleFor(x => x.AuthorName)
            .NotEmpty().WithMessage("Author name is required.")
            .MinimumLength(2).WithMessage("Author name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Author name cannot exceed 100 characters.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5 stars.");

        RuleFor(x => x.Comment)
            .MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters.");
    }
}
