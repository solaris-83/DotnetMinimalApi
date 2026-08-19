using System.Runtime.InteropServices;
using DotnetMinimalApi.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using DotnetMinimalApi.Endpoints;

namespace DotnetMinimalApi.Endpoints;

public class SystemEndpoints : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/system")
            .WithTags("System & Admin");

        group.MapPost("/reset-and-seed", ResetAndSeedDatabase)
            .WithName("ResetAndSeedDatabase")
            .WithSummary("Reset and reseed SQLite database")
            .WithDescription("Deletes existing data and reseeds fresh realistic sample data.");

        group.MapGet("/info", GetSystemInfo)
            .WithName("GetSystemInfo")
            .WithSummary("Get system and runtime information")
            .WithDescription("Provides metadata about the running .NET 9 Minimal API environment.");
    }
   /* public static RouteGroupBuilder MapSystemEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/system")
            .WithTags("System & Admin");

        group.MapPost("/reset-and-seed", ResetAndSeedDatabase)
            .WithName("ResetAndSeedDatabase")
            .WithSummary("Reset and reseed SQLite database")
            .WithDescription("Deletes existing data and reseeds fresh realistic sample data.");

        group.MapGet("/info", GetSystemInfo)
            .WithName("GetSystemInfo")
            .WithSummary("Get system and runtime information")
            .WithDescription("Provides metadata about the running .NET 9 Minimal API environment.");

        return group;
    }*/

    private async Task<Ok<object>> ResetAndSeedDatabase(
        AppDbContext db,
        ILogger<AppDbContext> logger,
        CancellationToken ct)
    {
        logger.LogWarning("Resetting and reseeding the database upon user request.");
        await DbInitializer.ResetAndSeedDataAsync(db, ct);

        return TypedResults.Ok<object>(new
        {
            Message = "Database successfully reset and reseeded with initial sample data.",
            TimestampUtc = DateTime.UtcNow
        });
    }

    private Ok<object> GetSystemInfo(IHostEnvironment env)
    {
        return TypedResults.Ok<object>(new
        {
            Application = "DotnetMinimalApi",
            Framework = RuntimeInformation.FrameworkDescription,
            RuntimeVersion = Environment.Version.ToString(),
            Environment = env.EnvironmentName,
            Os = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            DocumentationUI = "Scalar UI (/scalar/v1)",
            SwaggerPresent = false,
            TimestampUtc = DateTime.UtcNow
        });
    }
}
