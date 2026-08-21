using DotnetMinimalApi.Common.Exceptions;
using DotnetMinimalApi.Data;
using DotnetMinimalApi.Extensions;
using DotnetMinimalApi.Validation;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;


var builder = WebApplication.CreateBuilder(args);

// 1. Core Services & Problem Details (RFC 7807)
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// 2. OpenAPI Specification (.NET 9 native Microsoft.AspNetCore.OpenApi)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info.Title = ".NET 9 Minimal API - Best Practices Showcase";
        document.Info.Version = "v1";
        document.Info.Description = "Production-ready ASP.NET Core 9 Minimal API utilizing Scalar UI, SQLite EF Core, TypedResults, and Microsoft best practices.";
        return Task.CompletedTask;
    });
});

// 3. Database Context (Entity Framework Core SQLite)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=catalog.db";
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

// 4. FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<ProductCreateValidator>();

// 5. Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("sqlite_db", tags: ["ready", "db"]);

builder.Services.AddEndpoints(typeof(Program).Assembly);

var app = builder.Build();

// Configure Middleware Pipeline
app.UseExceptionHandler();
app.UseStatusCodePages();

// OpenAPI & Scalar API Reference Documentation (No Swagger)
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("api-docs", options =>
    {
        options
        .WithTitle(".NET 9 Minimal API - Scalar Reference")
        .WithTheme(ScalarTheme.Moon)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// Redirect root to Scalar Documentation
//app.MapGet("/", () => Results.Redirect("/scalar/v1")).ExcludeFromDescription();

app.MapEndpoints();
/*
// Health Check Endpoint
app.MapHealthChecks("/health")
    .WithTags("System & Admin")
    .WithSummary("System health check")
    .WithDescription("Checks application liveness and database connectivity.");

// Map Endpoint Groups
app.MapProductEndpoints();
app.MapCategoryEndpoints();
app.MapReviewEndpoints();
app.MapAnalyticsEndpoints();
app.MapSystemEndpoints();
*/

// Initialize and Seed Database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbInitializer.InitializeDatabaseAsync(db, logger);
}

app.Run();

// Make Program public for integration test fixtures if needed
public partial class Program { }

