using System.Diagnostics;

namespace DotnetMinimalApi.Common.Filters;

/// <summary>
/// Endpoint filter that benchmarks execution time and appends an X-Response-Time-Ms header.
/// </summary>
public class RequestTimingFilter(ILogger<RequestTimingFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var stopwatch = Stopwatch.StartNew();
        var httpContext = context.HttpContext;

        try
        {
            var result = await next(context);
            stopwatch.Stop();

            var elapsedMs = stopwatch.ElapsedMilliseconds;
            httpContext.Response.Headers["X-Response-Time-Ms"] = elapsedMs.ToString();

            if (elapsedMs > 500)
            {
                logger.LogWarning(
                    "Slow endpoint execution: {Method} {Path} took {ElapsedMs}ms",
                    httpContext.Request.Method,
                    httpContext.Request.Path,
                    elapsedMs);
            }

            return result;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            httpContext.Response.Headers["X-Response-Time-Ms"] = stopwatch.ElapsedMilliseconds.ToString();
            throw;
        }
    }
}
