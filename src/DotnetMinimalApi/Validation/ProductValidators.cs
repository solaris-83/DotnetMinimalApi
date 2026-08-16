using DotnetMinimalApi.Models.Dtos;
using FluentValidation;

namespace DotnetMinimalApi.Validation;

public class ProductCreateValidator : AbstractValidator<ProductCreateDto>
{
    public ProductCreateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MinimumLength(2).WithMessage("Product name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("Product SKU is required.")
            .Matches(@"^[A-Z0-9_-]{3,50}$")
            .WithMessage("SKU must be 3-50 characters uppercase alphanumeric, dash, or underscore (e.g., 'ELEC-PROD-001').");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .PrecisionScale(18, 2, false).WithMessage("Price cannot have more than 2 decimal places.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Valid CategoryId is required.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");
    }
}

public class ProductUpdateValidator : AbstractValidator<ProductUpdateDto>
{
    public ProductUpdateValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MinimumLength(2).WithMessage("Product name must be at least 2 characters.")
            .MaximumLength(200).WithMessage("Product name cannot exceed 200 characters.");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("Product SKU is required.")
            .Matches(@"^[A-Z0-9_-]{3,50}$")
            .WithMessage("SKU must be 3-50 characters uppercase alphanumeric, dash, or underscore.");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.")
            .PrecisionScale(18, 2, false).WithMessage("Price cannot have more than 2 decimal places.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stock quantity cannot be negative.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Valid CategoryId is required.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");
    }
}

public class ProductStockAdjustmentValidator : AbstractValidator<ProductStockAdjustmentDto>
{
    public ProductStockAdjustmentValidator()
    {
        RuleFor(x => x.Adjustment)
            .NotEqual(0).WithMessage("Adjustment quantity cannot be 0.");

        RuleFor(x => x.Reason)
            .MaximumLength(200).WithMessage("Reason cannot exceed 200 characters.");
    }
}
