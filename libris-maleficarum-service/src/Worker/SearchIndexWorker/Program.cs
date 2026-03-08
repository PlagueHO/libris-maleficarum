using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Infrastructure.Configuration;
using LibrisMaleficarum.Infrastructure.Services;
using Azure.AI.OpenAI;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Cosmos;
using OpenAI.Embeddings;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using AppSearchOptions = LibrisMaleficarum.Infrastructure.Configuration.SearchOptions;

var builder = WebApplication.CreateBuilder(args);

// Add Aspire service defaults (OpenTelemetry, health checks, resilience, service discovery)
builder.AddServiceDefaults();

// Add custom application telemetry (meters, activity sources, and counters)
builder.AddApplicationTelemetry("LibrisMaleficarum.SearchIndexWorker");

// Configure search options
builder.Services.Configure<AppSearchOptions>(
    builder.Configuration.GetSection(AppSearchOptions.SectionName));

// Configure Azure AI Search clients via Aspire client integration
// Registers SearchIndexClient using the "aisearch" connection from AppHost
builder.AddAzureSearchClient("aisearch");

// Register SearchClient from SearchIndexClient for index operations
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

// Register Cosmos DB client for Change Feed Processor via Aspire client integration
// Registers CosmosClient using the "cosmosdb" connection from AppHost
builder.AddAzureCosmosClient("cosmosdb",
    configureClientOptions: options =>
    {
        options.ConnectionMode = ConnectionMode.Gateway;
        options.RequestTimeout = TimeSpan.FromSeconds(60);
    });

// Register search index and embedding services (Infrastructure implementations of Domain interfaces)
builder.Services.AddScoped<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<ISearchIndexService, AzureAISearchService>();

// Register telemetry service for custom metrics and distributed tracing
builder.Services.AddScoped<ITelemetryService>(sp =>
    new TelemetryService(
        sp.GetRequiredService<Meter>(),
        sp.GetRequiredService<ActivitySource>()));

// Register the Change Feed sync background service
builder.Services.AddHostedService<SearchIndexSyncService>();

var app = builder.Build();

// Map health and alive endpoints for Aspire orchestration and Container Apps probes
app.MapDefaultEndpoints();

app.Run();
