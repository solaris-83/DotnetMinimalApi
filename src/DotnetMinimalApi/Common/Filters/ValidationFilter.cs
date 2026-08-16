using FluentValidation;

namespace DotnetMinimalApi.Common.Filters;

/// <summary>
/// Generic endpoint filter that executes FluentValidation validators on incoming request bodies
/// and returns a standard RFC 7807 ValidationProblem HttpResult if validation fails.
/// </summary>
/// <typeparam name="T">The type of the DTO to validate</typeparam>
public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is null)
        {
            return await next(context);
        }

        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument is null)
        {
            return Results.Problem(
                title: "Invalid Request Body",
                detail: $"Expected a body of type {typeof(T).Name}.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var validationResult = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            return TypedResults.ValidationProblem(errors);
        }

        return await next(context);
    }
}
