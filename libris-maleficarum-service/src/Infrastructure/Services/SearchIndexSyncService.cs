namespace LibrisMaleficarum.Infrastructure.Services;

using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Domain.Models;
using LibrisMaleficarum.Infrastructure.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

/// <summary>
/// Background service that monitors the Cosmos DB Change Feed for WorldEntity changes
/// and synchronizes them to the Azure AI Search index with vector embeddings.
/// </summary>
public class SearchIndexSyncService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CosmosClient _cosmosClient;
    private readonly SearchOptions _options;
    private readonly ILogger<SearchIndexSyncService> _logger;
    private ChangeFeedProcessor? _changeFeedProcessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchIndexSyncService"/> class.
    /// </summary>
    public SearchIndexSyncService(
        IServiceScopeFactory scopeFactory,
        CosmosClient cosmosClient,
        IOptions<SearchOptions> options,
        ILogger<SearchIndexSyncService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SearchIndexSyncService starting, ensuring search index exists");

        // Ensure the search index exists before processing changes
        using (var scope = _scopeFactory.CreateScope())
        {
            var searchIndexService = scope.ServiceProvider.GetRequiredService<ISearchIndexService>();
            await searchIndexService.EnsureIndexExistsAsync(stoppingToken);
        }

        var database = _cosmosClient.GetDatabase("LibrisMaleficarum");
        var monitoredContainer = database.GetContainer("WorldEntities");
        var leaseContainer = database.GetContainer("leases");

        _changeFeedProcessor = monitoredContainer
            .GetChangeFeedProcessorBuilder<WorldEntity>(
                processorName: "SearchIndexSyncProcessor",
                onChangesDelegate: HandleChangesAsync)
            .WithInstanceName(Environment.MachineName)
            .WithLeaseContainer(leaseContainer)
            .WithPollInterval(TimeSpan.FromMilliseconds(_options.ChangeFeedPollIntervalMs))
            .WithStartTime(DateTime.MinValue.ToUniversalTime())
            .Build();

        _logger.LogInformation("Starting Change Feed Processor");
        await _changeFeedProcessor.StartAsync();

        // Wait until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SearchIndexSyncService shutting down");
        }
    }

    /// <inheritdoc/>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_changeFeedProcessor is not null)
        {
            _logger.LogInformation("Stopping Change Feed Processor");
            await _changeFeedProcessor.StopAsync();
        }

        await base.StopAsync(cancellationToken);
    }

    private async Task HandleChangesAsync(
        ChangeFeedProcessorContext context,
        IReadOnlyCollection<WorldEntity> changes,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing {ChangeCount} changes from lease {LeaseToken}",
            changes.Count,
            context.LeaseToken);

        using var scope = _scopeFactory.CreateScope();
        var embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
        var searchIndexService = scope.ServiceProvider.GetRequiredService<ISearchIndexService>();
        var telemetryService = scope.ServiceProvider.GetRequiredService<ITelemetryService>();

        var documentsToIndex = new List<SearchIndexDocument>();
        var documentsToRemove = new List<string>();

        foreach (var entity in changes)
        {
            try
            {
                if (entity.IsDeleted)
                {
                    documentsToRemove.Add(entity.Id.ToString());
                    _logger.LogInformation(
                        "Entity {EntityId} (type={EntityType}, world={WorldId}) is soft-deleted; queuing removal from index",
                        entity.Id, entity.EntityType, entity.WorldId);
                    continue;
                }

                using var activity = telemetryService.StartIndexingActivity(
                    entity.Id.ToString(),
                    entity.EntityType.ToString());

                var embeddingStopwatch = Stopwatch.StartNew();

                // Concatenate fields for embedding generation
                var embeddingContent = BuildEmbeddingContent(entity);
                var embedding = await embeddingService.GenerateEmbeddingAsync(
                    embeddingContent, cancellationToken);

                embeddingStopwatch.Stop();
                telemetryService.RecordEmbeddingLatency(embeddingStopwatch.ElapsedMilliseconds);

                var document = MapToSearchDocument(entity, embedding);
                documentsToIndex.Add(document);

                _logger.LogInformation(
                    "Mapped entity {EntityId} (type={EntityType}, world={WorldId}) to search document",
                    entity.Id, entity.EntityType, entity.WorldId);
            }
            catch (Exception ex)
            {
                telemetryService.RecordIndexingFailure(entity.EntityType.ToString());
                _logger.LogError(
                    ex,
                    "Failed to process entity {EntityId} (type={EntityType}, world={WorldId}) for indexing; dead-lettering",
                    entity.Id, entity.EntityType, entity.WorldId);
            }
        }

        // Batch index documents
        if (documentsToIndex.Count > 0)
        {
            try
            {
                await searchIndexService.IndexDocumentsBatchAsync(documentsToIndex, cancellationToken);
                _logger.LogInformation("Indexed {Count} documents successfully", documentsToIndex.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to batch index {Count} documents", documentsToIndex.Count);
                foreach (var doc in documentsToIndex)
                {
                    var telemetry = scope.ServiceProvider.GetRequiredService<ITelemetryService>();
                    telemetry.RecordIndexingFailure(doc.EntityType);
                }
            }
        }

        // Batch remove documents
        if (documentsToRemove.Count > 0)
        {
            try
            {
                await searchIndexService.RemoveDocumentsBatchAsync(documentsToRemove, cancellationToken);
                _logger.LogInformation("Removed {Count} deleted documents from index", documentsToRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to batch remove {Count} documents from index", documentsToRemove.Count);
            }
        }
    }

    /// <summary>
    /// Builds the embedding content by concatenating entity fields.
    /// </summary>
    internal static string BuildEmbeddingContent(WorldEntity entity)
    {
        var parts = new List<string> { entity.Name };

        if (!string.IsNullOrEmpty(entity.Description))
        {
            parts.Add(entity.Description);
        }

        if (entity.Tags is { Count: > 0 })
        {
            parts.Add(string.Join(" ", entity.Tags));
        }

        if (!string.IsNullOrEmpty(entity.Attributes))
        {
            parts.Add(entity.Attributes);
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Maps a WorldEntity to a SearchIndexDocument.
    /// </summary>
    internal static SearchIndexDocument MapToSearchDocument(
        WorldEntity entity,
        ReadOnlyMemory<float> contentVector)
    {
        return new SearchIndexDocument
        {
            Id = entity.Id.ToString(),
            WorldId = entity.WorldId.ToString(),
            EntityType = entity.EntityType.ToString(),
            Name = entity.Name,
            Description = entity.Description,
            Tags = entity.Tags ?? [],
            ParentId = entity.ParentId?.ToString(),
            OwnerId = entity.OwnerId,
            CreatedDate = new DateTimeOffset(entity.CreatedDate, TimeSpan.Zero),
            ModifiedDate = new DateTimeOffset(entity.ModifiedDate, TimeSpan.Zero),
            Path = entity.Path?.Select(g => g.ToString()).ToList() ?? [],
            Depth = entity.Depth,
            Attributes = entity.Attributes,
            SchemaVersion = entity.SchemaVersion,
            ContentVector = contentVector
        };
    }
}
