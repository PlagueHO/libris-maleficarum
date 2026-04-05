using FluentValidation;
using LibrisMaleficarum.Api.BackgroundServices;
using LibrisMaleficarum.Api.Filters;
using LibrisMaleficarum.Api.Middleware;
using LibrisMaleficarum.Api.Validators;
using LibrisMaleficarum.Domain.Configuration;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Infrastructure.Configuration;
using LibrisMaleficarum.Infrastructure.Persistence;
using LibrisMaleficarum.Infrastructure.Repositories;
using LibrisMaleficarum.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Web;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Azure.Cosmos;
using OpenAI.Embeddings;
using AppSearchOptions = LibrisMaleficarum.Infrastructure.Configuration.SearchOptions;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, resilience, service discovery)
builder.AddServiceDefaults();

// Add HttpContextAccessor for accessing HttpContext in services
builder.Services.AddHttpContextAccessor();

// Detect authentication mode from configuration
var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
var isMultiUserMode = !string.IsNullOrEmpty(azureAdClientId);

if (isMultiUserMode)
{
    // Multi-user mode: Configure JWT bearer authentication with Entra ID
    builder.Services.AddMicrosoftIdentityWebApiAuthentication(builder.Configuration);
}
else
{
    // Single-user mode: Add authentication services with a default scheme
    // The anonymous middleware will inject claims on each request
    builder.Services.AddAuthentication().AddJwtBearer();
}

// Add custom application telemetry (meters, activity sources, and counters)
// Must be called after AddServiceDefaults to ensure OpenTelemetry is configured
builder.AddApplicationTelemetry();

// Configure entity schema versioning
builder.Services.Configure<EntitySchemaVersionConfig>(
    builder.Configuration.GetSection("EntitySchemaVersions"));

// Configure delete operation options
builder.Services.Configure<DeleteOperationOptions>(
    builder.Configuration.GetSection(DeleteOperationOptions.SectionName));

// Configure search options
builder.Services.Configure<AppSearchOptions>(
    builder.Configuration.GetSection(AppSearchOptions.SectionName));

// Configure DbContext with Cosmos DB via Aspire EF Core Cosmos client integration
// Registers ApplicationDbContext using the "cosmosdb" connection from AppHost
// Aspire handles connection string parsing, health checks, logging, tracing, and metrics
builder.AddCosmosDbContext<ApplicationDbContext>("cosmosdb", "LibrisMaleficarum",
    configureDbContextOptions: options =>
        options.UseCosmos(cosmosOptions =>
        {
            // Gateway mode is required for the emulator (Direct mode is not supported)
            cosmosOptions.ConnectionMode(Microsoft.Azure.Cosmos.ConnectionMode.Gateway);

            // Increase timeout for emulator initialization
            cosmosOptions.RequestTimeout(TimeSpan.FromSeconds(60));
        }));

// Register domain services
builder.Services.AddScoped<IUserContextService, UserContextService>();
builder.Services.AddScoped<ITelemetryService>(sp =>
    new TelemetryService(
        sp.GetRequiredService<Meter>(),
        sp.GetRequiredService<ActivitySource>()));
builder.Services.AddScoped<IWorldRepository, WorldRepository>();
builder.Services.AddScoped<IWorldEntityRepository, WorldEntityRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IDeleteOperationRepository, DeleteOperationRepository>();
builder.Services.AddScoped<IDeleteService, DeleteService>();

// Configure Azure AI Search clients via Aspire client integration
// Registers SearchIndexClient using the "aisearch" connection from AppHost
// Uses DefaultAzureCredential (RBAC) — no API keys needed
builder.AddAzureSearchClient("aisearch");

// Register SearchClient from SearchIndexClient for query operations
builder.Services.AddSingleton<SearchClient>(sp =>
{
    var indexClient = sp.GetRequiredService<SearchIndexClient>();
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSearchOptions>>().Value;
    return indexClient.GetSearchClient(opts.IndexName);
});

// Configure Azure AI Foundry OpenAI client via Aspire client integration
// Registers AzureOpenAIClient using the "embedding" deployment connection from AppHost
builder.AddAzureOpenAIClient("embedding");

// Register EmbeddingClient from AzureOpenAIClient for vector embedding generation
builder.Services.AddSingleton<EmbeddingClient>(sp =>
{
    var openAIClient = sp.GetRequiredService<AzureOpenAIClient>();
    var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSearchOptions>>().Value;
    return openAIClient.GetEmbeddingClient(opts.EmbeddingModelName);
});

// Register search services
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<ISearchService, AzureAISearchService>();

// Register validators
builder.Services.AddScoped<SchemaVersionValidator>();

// Add Azure Blob Storage client (uses connection named "blobs" from AppHost)
// This will use Azurite emulator locally and Azure Blob Storage in production
builder.AddAzureBlobServiceClient("blobs");

// Register Azure Blob Storage service
builder.Services.AddScoped<IBlobStorageService, BlobStorageService>();

// Register background services
builder.Services.AddHostedService<DeleteOperationProcessor>();

// Add controllers with JSON options and exception filters
builder.Services.AddControllers(options =>
    {
        options.Filters.Add<DomainExceptionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add CORS policy to allow frontend access
// In development, allow any origin (e.g., Vite dev server on localhost:5173)
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Add HTTP request logging for OpenTelemetry/Aspire Dashboard
builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields = Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.All;
    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
    logging.CombineLogs = true; // Combine request and response into a single log entry
});

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

// Ensure database and containers exist
// Database created by Aspire AppHost (.AddDatabase), but containers must be created by EF Core
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.

// HTTP request logging (must be early in pipeline, after exception handling)
app.UseHttpLogging();

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

// Enable CORS (must be before Authorization)
app.UseCors();

// Authentication and authorization pipeline
app.UseAuthentication();

if (!isMultiUserMode)
{
    // In single-user mode, inject anonymous claims for unauthenticated requests
    app.UseMiddleware<AnonymousClaimsMiddleware>();
}

app.UseAuthorization();

app.MapControllers();

// Map default Aspire endpoints (health checks)
app.MapDefaultEndpoints();

// Log authentication mode at startup
if (isMultiUserMode)
{
    app.Logger.LogInformation("Authentication mode: Multi-user (Entra ID configured)");
}
else
{
    app.Logger.LogWarning("Authentication mode: Single-user anonymous (no AzureAd:ClientId configured)");
}

app.Run();
