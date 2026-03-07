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
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text.Json.Serialization;
using Azure;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.Azure.Cosmos;
using AppSearchOptions = LibrisMaleficarum.Infrastructure.Configuration.SearchOptions;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, resilience, service discovery)
builder.AddServiceDefaults();

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
builder.Services.AddScoped<ITelemetryService>(sp =>
    new TelemetryService(
        sp.GetRequiredService<Meter>(),
        sp.GetRequiredService<ActivitySource>()));
builder.Services.AddScoped<IWorldRepository, WorldRepository>();
builder.Services.AddScoped<IWorldEntityRepository, WorldEntityRepository>();
builder.Services.AddScoped<IAssetRepository, AssetRepository>();
builder.Services.AddScoped<IDeleteOperationRepository, DeleteOperationRepository>();
builder.Services.AddScoped<IDeleteService, DeleteService>();

// Configure Azure AI Search clients
var searchConnectionString = builder.Configuration.GetConnectionString("search");
if (!string.IsNullOrEmpty(searchConnectionString))
{
    // Parse the search connection string (expected format: "Endpoint=https://...;ApiKey=...")
    var searchEndpoint = new Uri(searchConnectionString.Split(';')
        .FirstOrDefault(s => s.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
        ?.Substring("Endpoint=".Length) ?? throw new InvalidOperationException("Search connection string missing Endpoint"));
    var searchApiKey = searchConnectionString.Split(';')
        .FirstOrDefault(s => s.StartsWith("ApiKey=", StringComparison.OrdinalIgnoreCase))
        ?.Substring("ApiKey=".Length) ?? "";
    var searchCredential = new AzureKeyCredential(searchApiKey);

    var searchOptions = builder.Configuration.GetSection(AppSearchOptions.SectionName).Get<AppSearchOptions>() ?? new AppSearchOptions();

    builder.Services.AddSingleton(new SearchIndexClient(searchEndpoint, searchCredential));
    builder.Services.AddSingleton(new SearchClient(searchEndpoint, searchOptions.IndexName, searchCredential));
}
else
{
    // Fallback: register placeholder clients for environments without search configured
    builder.Services.AddSingleton(sp => new SearchIndexClient(new Uri("https://placeholder.search.windows.net"), new AzureKeyCredential("placeholder")));
    builder.Services.AddSingleton(sp =>
    {
        var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AppSearchOptions>>().Value;
        return new SearchClient(new Uri("https://placeholder.search.windows.net"), opts.IndexName, new AzureKeyCredential("placeholder"));
    });
}

// Configure Azure AI Services embedding client
var aiServicesConnectionString = builder.Configuration.GetConnectionString("aiservices");
if (!string.IsNullOrEmpty(aiServicesConnectionString))
{
    var aiEndpoint = new Uri(aiServicesConnectionString.Split(';')
        .FirstOrDefault(s => s.StartsWith("Endpoint=", StringComparison.OrdinalIgnoreCase))
        ?.Substring("Endpoint=".Length) ?? throw new InvalidOperationException("AI Services connection string missing Endpoint"));
    var aiApiKey = aiServicesConnectionString.Split(';')
        .FirstOrDefault(s => s.StartsWith("ApiKey=", StringComparison.OrdinalIgnoreCase))
        ?.Substring("ApiKey=".Length) ?? "";

    var searchOpts = builder.Configuration.GetSection(AppSearchOptions.SectionName).Get<AppSearchOptions>() ?? new AppSearchOptions();
    var azureOpenAIClient = new AzureOpenAIClient(aiEndpoint, new AzureKeyCredential(aiApiKey));
    builder.Services.AddSingleton(azureOpenAIClient.GetEmbeddingClient(searchOpts.EmbeddingModelName));
}
else
{
    // Fallback: register a placeholder embedding client
    var placeholderClient = new AzureOpenAIClient(new Uri("https://placeholder.openai.azure.com"), new AzureKeyCredential("placeholder"));
    builder.Services.AddSingleton(placeholderClient.GetEmbeddingClient("text-embedding-3-small"));
}

// Register Cosmos DB client for Change Feed Processor
builder.Services.AddSingleton(sp =>
{
    var connStr = builder.Configuration.GetConnectionString("cosmosdb")
        ?? throw new InvalidOperationException("Cosmos DB connection string 'cosmosdb' not found for Change Feed Processor");
    return new CosmosClient(connStr, new CosmosClientOptions
    {
        ConnectionMode = ConnectionMode.Gateway,
        RequestTimeout = TimeSpan.FromSeconds(60)
    });
});

// Register search services
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<ISearchIndexService, AzureAISearchService>();
builder.Services.AddScoped<ISearchService, AzureAISearchService>();

// Register Change Feed sync background service
builder.Services.AddHostedService<SearchIndexSyncService>();

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

app.UseAuthorization();

app.MapControllers();

// Map default Aspire endpoints (health checks)
app.MapDefaultEndpoints();

app.Run();
