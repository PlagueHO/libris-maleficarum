using FluentValidation;
using LibrisMaleficarum.Api.Filters;
using LibrisMaleficarum.Api.Middleware;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.Infrastructure.Repositories;
using LibrisMaleficarum.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, resilience, service discovery)
builder.AddServiceDefaults();

// Configure DbContext with Cosmos DB provider
var cosmosConnectionString = builder.Configuration.GetConnectionString("cosmosdb")
    ?? throw new InvalidOperationException("Cosmos DB connection string 'cosmosdb' not found");

// Log connection string endpoint for debugging (sanitized)
var endpointMatch = System.Text.RegularExpressions.Regex.Match(cosmosConnectionString, @"AccountEndpoint=([^;]+)");
if (endpointMatch.Success)
{
    Console.WriteLine($"Cosmos DB Endpoint: {endpointMatch.Groups[1].Value}");
}

// For Cosmos DB Emulator (HTTP or HTTPS), use Gateway mode and disable SSL validation
// Aspire automatically adds DisableServerCertificateValidation=True for the preview emulator
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseCosmos(
        connectionString: cosmosConnectionString,
        databaseName: "LibrisMaleficarum",
        cosmosOptionsAction: cosmosOptions =>
        {
            // Gateway mode is required for the emulator (Direct mode is not supported)
            cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);

            // Increase timeout for emulator initialization
            cosmosOptions.RequestTimeout(TimeSpan.FromSeconds(60));
        }));

// Register domain services
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<IWorldRepository, WorldRepository>();
builder.Services.AddScoped<IWorldEntityRepository, WorldEntityRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<ISearchService, SearchService>();

// Add Azure Blob Storage client (uses connection named "blobs" from AppHost)
// This will use Azurite emulator locally and Azure Blob Storage in production
builder.AddAzureBlobServiceClient("blobs");

// Register Azure Blob Storage service
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

// Add controllers with JSON options and exception filters
builder.Services.AddControllers(options =>
    {
        options.Filters.Add<DomainExceptionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add OpenAPI document generation with metadata
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "Libris Maleficarum API",
            Version = "v1",
            Description = "RESTful API for world-building and campaign management"
        };
        return Task.CompletedTask;
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.

// Exception handling (must be first)
app.UseExceptionHandling();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Only use HTTPS redirection in non-Development environments
// In Development, allow HTTP for local testing with Cosmos DB emulator
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

// Map default Aspire endpoints (health checks)
app.MapDefaultEndpoints();

app.Run();
