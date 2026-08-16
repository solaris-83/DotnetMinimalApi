using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace DotnetMinimalApi.Common.Exceptions;

/// <summary>
/// Centralized exception handler implementing Microsoft's IExceptionHandler interface (.NET 8/9)
/// to transform unhandled exceptions into RFC 7807 compliant ProblemDetails responses.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        logger.LogError(
            exception,
            "Unhandled exception occurred. TraceId: {TraceId}, Path: {Path}, Message: {Message}",
            traceId,
            httpContext.Request.Path,
            exception.Message);

        var (statusCode, title, detail, type) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation Error",
                "One or more validation errors occurred.",
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            ),
            ResourceNotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                notFoundException.Message,
                "https://tools.ietf.org/html/rfc7231#section-6.5.4"
            ),
            ConflictException conflictException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflictException.Message,
                "https://tools.ietf.org/html/rfc7231#section-6.5.8"
            ),
            BadHttpRequestException badHttpRequestException => (
                StatusCodes.Status400BadRequest,
                "Bad Request",
                badHttpRequestException.Message,
                "https://tools.ietf.org/html/rfc7231#section-6.5.1"
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred. Please refer to the trace ID for troubleshooting.",
                "https://tools.ietf.org/html/rfc7231#section-6.6.1"
            )
        };

        httpContext.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Type = type,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = traceId;

        if (exception is ValidationException valEx)
        {
            var errors = valEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            problemDetails.Extensions["errors"] = errors;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}
