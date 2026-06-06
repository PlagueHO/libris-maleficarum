namespace LibrisMaleficarum.Infrastructure.Services;

using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Domain.Models;
using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Infrastructure.Configuration;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text.Json;

/// <summary>
/// Background service that monitors the Cosmos DB Change Feed for WorldEntity changes
/// and synchronizes them to the Azure AI Search index with vector embeddings.
/// </summary>
public class SearchIndexSyncService : BackgroundService
{
    private const string DatabaseName = "LibrisMaleficarum";
    private const string MonitoredContainerName = "WorldEntities";
    private const string LeaseContainerName = "leases";
    private const string LeaseContainerPartitionKeyPath = "/id";
    private const string WorldEntityDiscriminator = "WorldEntity";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly CosmosClient _cosmosClient;
    private readonly SearchOptions _options;
    private readonly ILogger<SearchIndexSyncService> _logger;
    private int _isProcessorRunning;
    private ChangeFeedProcessor? _changeFeedProcessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchIndexSyncService"/> class.
    /// </summary>
    public SearchIndexSyncService(
        IServiceScopeFactory scopeFactory,
        CosmosClient cosmosClient,
        IOptions<SearchOptions> options,
        Meter meter,
        ILogger<SearchIndexSyncService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        ArgumentNullException.ThrowIfNull(meter);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        meter.CreateObservableGauge<int>(
            "search.worker.alive",
            () => Volatile.Read(ref _isProcessorRunning),
            description: "Indicates whether the search index worker is currently running (1=running, 0=stopped)");
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SearchIndexSyncService starting, ensuring search index exists");
        _logger.LogInformation(
            "SearchIndexSyncService configuration: Database={Database}, MonitoredContainer={MonitoredContainer}, LeaseContainer={LeaseContainer}, PollIntervalMs={PollIntervalMs}",
            DatabaseName,
            MonitoredContainerName,
            LeaseContainerName,
            _options.ChangeFeedPollIntervalMs);

        // Retry index creation with exponential backoff to handle transient auth/network failures
        // without crashing the host (BackgroundServiceExceptionBehavior.StopHost is the default).
        const int maxRetries = 5;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var searchIndexService = scope.ServiceProvider.GetRequiredService<ISearchIndexService>();
                await searchIndexService.EnsureIndexExistsAsync(stoppingToken);
                break; // success
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                if (attempt == maxRetries)
                {
                    _logger.LogCritical(
                        ex,
                        "Failed to ensure search index exists after {MaxRetries} attempts; stopping service",
                        maxRetries);
                    throw;
                }

                var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2, 4, 8, 16, 32s
                _logger.LogWarning(
                    ex,
                    "Failed to ensure search index exists (attempt {Attempt}/{MaxRetries}); retrying in {Delay}s",
                    attempt,
                    maxRetries,
                    delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
            }
        }

        var database = _cosmosClient.GetDatabase(DatabaseName);
        var monitoredContainer = database.GetContainer(MonitoredContainerName);

        // Change Feed Processor requires the lease container to exist before start.
        // Create it idempotently so local runs do not fail on first boot.
        await database.CreateContainerIfNotExistsAsync(
            new ContainerProperties(LeaseContainerName, LeaseContainerPartitionKeyPath),
            cancellationToken: stoppingToken);
        _logger.LogInformation(
            "Lease container ensured: Database={Database}, LeaseContainer={LeaseContainer}, PartitionKeyPath={PartitionKeyPath}",
            DatabaseName,
            LeaseContainerName,
            LeaseContainerPartitionKeyPath);

        var leaseContainer = database.GetContainer(LeaseContainerName);

        var changeFeedProcessor = monitoredContainer
            .GetChangeFeedProcessorBuilder<JsonElement>(
                processorName: "SearchIndexSyncProcessor",
                onChangesDelegate: HandleChangesAsync)
            .WithInstanceName(Environment.MachineName)
            .WithLeaseContainer(leaseContainer)
            .WithPollInterval(TimeSpan.FromMilliseconds(_options.ChangeFeedPollIntervalMs))
            .WithStartTime(DateTime.MinValue.ToUniversalTime())
            // Register life cycle notifications so SDK-level errors (delegate exceptions and
            // monitored-container access failures) become visible. Without these handlers the
            // Change Feed Processor silently swallows exceptions and retries the same batch
            // forever, which presents as "stuck" reprocessing with no error in the app logs.
            .WithErrorNotification(HandleChangeFeedErrorAsync)
            .WithLeaseAcquireNotification(HandleLeaseAcquireAsync)
            .WithLeaseReleaseNotification(HandleLeaseReleaseAsync)
            .Build();

        _logger.LogInformation(
            "Change Feed Processor configured: ProcessorName={ProcessorName}, InstanceName={InstanceName}, PollIntervalMs={PollIntervalMs}, StartTimeUtc={StartTimeUtc}",
            "SearchIndexSyncProcessor",
            Environment.MachineName,
            _options.ChangeFeedPollIntervalMs,
            DateTime.MinValue.ToUniversalTime());

        _logger.LogInformation("Starting Change Feed Processor");
        await changeFeedProcessor.StartAsync();
        _changeFeedProcessor = changeFeedProcessor;
        Interlocked.Exchange(ref _isProcessorRunning, 1);

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

            try
            {
                await _changeFeedProcessor.StopAsync();
            }
            catch (NullReferenceException ex)
            {
                // Cosmos SDK can throw during stop when startup failed before full initialization.
                _logger.LogWarning(ex, "Change Feed Processor stop skipped because processor was not fully initialized");
            }
            finally
            {
                _changeFeedProcessor = null;
                Interlocked.Exchange(ref _isProcessorRunning, 0);
            }
        }
        else
        {
            Interlocked.Exchange(ref _isProcessorRunning, 0);
        }

        await base.StopAsync(cancellationToken);
    }

    /// <summary>
    /// Receives notifications when the Change Feed Processor encounters an error during processing.
    /// Distinguishes unhandled exceptions thrown by the change handler delegate
    /// (<see cref="ChangeFeedProcessorUserException"/>) from errors the processor hits while accessing
    /// the monitored or lease containers (for example transient networking or checkpoint write failures).
    /// Without this handler the SDK swallows these errors and silently retries the same batch forever.
    /// </summary>
    private Task HandleChangeFeedErrorAsync(string leaseToken, Exception exception)
    {
        if (exception is ChangeFeedProcessorUserException userException)
        {
            _logger.LogError(
                userException,
                "Change Feed handler delegate threw an unhandled exception; batch will be retried. LeaseToken={LeaseToken}",
                leaseToken);
        }
        else
        {
            _logger.LogError(
                exception,
                "Change Feed Processor failed while accessing the monitored or lease container. LeaseToken={LeaseToken}",
                leaseToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Receives notifications when this host acquires a lease and starts processing its partition key range.
    /// </summary>
    private Task HandleLeaseAcquireAsync(string leaseToken)
    {
        _logger.LogInformation("Change Feed Processor acquired lease: LeaseToken={LeaseToken}", leaseToken);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Receives notifications when this host releases a lease and stops processing its partition key range.
    /// </summary>
    private Task HandleLeaseReleaseAsync(string leaseToken)
    {
        _logger.LogInformation("Change Feed Processor released lease: LeaseToken={LeaseToken}", leaseToken);
        return Task.CompletedTask;
    }

    private async Task HandleChangesAsync(
        ChangeFeedProcessorContext context,
        IReadOnlyCollection<JsonElement> changes,
        CancellationToken cancellationToken)
    {
        var batchStopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "Processing {ChangeCount} changes from lease {LeaseToken}",
            changes.Count,
            context.LeaseToken);

        using var scope = _scopeFactory.CreateScope();
        IEmbeddingService embeddingService;
        ISearchIndexService searchIndexService;
        ITelemetryService telemetryService;

        try
        {
            embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            searchIndexService = scope.ServiceProvider.GetRequiredService<ISearchIndexService>();
            telemetryService = scope.ServiceProvider.GetRequiredService<ITelemetryService>();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to resolve required services for change feed batch processing: LeaseToken={LeaseToken}",
                context.LeaseToken);
            throw;
        }

        var documentsToIndex = new List<SearchIndexDocument>();
        var documentsToRemove = new List<string>();
        var skippedNonWorldEntityCount = 0;
        var skippedMissingDiscriminatorCount = 0;
        var skippedUnexpectedDiscriminatorCount = 0;
        var perEntityFailureCount = 0;
        var indexedDocumentCount = 0;
        var removedDocumentCount = 0;

        foreach (var change in changes)
        {
            // Declare diagnostic variables before try so they're accessible in the catch block.
            // Defaults are used when the element is not a valid JSON object.
            var changeEntityId = "unknown";
            var changeWorldId = "unknown";
            var changeEntityType = "unknown";
            var changeDiscriminator = "<missing>";

            try
            {
                // JsonElement.TryGetProperty throws InvalidOperationException when ValueKind is not
                // Object. Skip and count non-object entries (e.g. lease documents, internal metadata)
                // rather than allowing them to escape and abort the entire batch.
                if (change.ValueKind != JsonValueKind.Object)
                {
                    skippedNonWorldEntityCount++;
                    skippedMissingDiscriminatorCount++;
                    _logger.LogWarning(
                        "Skipping non-object change feed entry: ValueKind={ValueKind}",
                        change.ValueKind);
                    continue;
                }

                changeEntityId = ParseNullableString(change, "id") ?? "unknown";
                changeWorldId = ParseNullableString(change, "worldId") ?? "unknown";
                changeEntityType = ParseNullableString(change, "entityType") ?? "unknown";
                changeDiscriminator = ParseNullableString(change, "_type") ?? "<missing>";

                if (TryParseUnixTimestamp(change, "_ts", out var tsSeconds))
                {
                    var syncLagSeconds = DateTimeOffset.UtcNow.Subtract(DateTimeOffset.FromUnixTimeSeconds(tsSeconds)).TotalSeconds;
                    telemetryService.RecordSyncLag(syncLagSeconds);
                }

                if (!TryMapToWorldEntity(change, out var entity))
                {
                    skippedNonWorldEntityCount++;

                    if (string.Equals(changeDiscriminator, "<missing>", StringComparison.Ordinal))
                    {
                        skippedMissingDiscriminatorCount++;
                    }
                    else
                    {
                        skippedUnexpectedDiscriminatorCount++;
                    }

                    continue;
                }

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
                perEntityFailureCount++;
                telemetryService.RecordIndexingFailure(changeEntityType);
                _logger.LogError(
                    ex,
                    "Failed to process change for indexing: EntityId={EntityId}, EntityType={EntityType}, WorldId={WorldId}, Discriminator={Discriminator}",
                    changeEntityId, changeEntityType, changeWorldId, changeDiscriminator);
            }
        }

        // Batch index documents
        if (documentsToIndex.Count > 0)
        {
            try
            {
                await searchIndexService.IndexDocumentsBatchAsync(documentsToIndex, cancellationToken);
                indexedDocumentCount = documentsToIndex.Count;
                _logger.LogInformation("Indexed {Count} documents successfully", documentsToIndex.Count);
                foreach (var doc in documentsToIndex)
                {
                    telemetryService.RecordDocumentIndexed(doc.EntityType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to batch index {Count} documents", documentsToIndex.Count);
                foreach (var doc in documentsToIndex)
                {
                    telemetryService.RecordIndexingFailure(doc.EntityType);
                }

                perEntityFailureCount += documentsToIndex.Count;
            }
        }
        else
        {
            _logger.LogInformation(
                "No documents queued for indexing in batch for lease {LeaseToken}",
                context.LeaseToken);
        }

        // Batch remove documents
        if (documentsToRemove.Count > 0)
        {
            try
            {
                await searchIndexService.RemoveDocumentsBatchAsync(documentsToRemove, cancellationToken);
                removedDocumentCount = documentsToRemove.Count;
                _logger.LogInformation("Removed {Count} deleted documents from index", documentsToRemove.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to batch remove {Count} documents from index", documentsToRemove.Count);
                perEntityFailureCount += documentsToRemove.Count;
            }
        }
        else
        {
            _logger.LogInformation(
                "No documents queued for removal in batch for lease {LeaseToken}",
                context.LeaseToken);
        }

        batchStopwatch.Stop();

        try
        {
            telemetryService.RecordBatchProcessed(indexedDocumentCount, removedDocumentCount, skippedNonWorldEntityCount, perEntityFailureCount);
            telemetryService.RecordBatchLatency(batchStopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to record batch telemetry for lease {LeaseToken}", context.LeaseToken);
        }

        _logger.LogInformation(
            "Change feed batch summary: LeaseToken={LeaseToken}, TotalChanges={TotalChanges}, MappedForIndex={MappedForIndex}, QueuedForRemoval={QueuedForRemoval}, SkippedNonWorldEntity={SkippedNonWorldEntity}, SkippedMissingDiscriminator={SkippedMissingDiscriminator}, SkippedUnexpectedDiscriminator={SkippedUnexpectedDiscriminator}, IndexedSuccess={IndexedSuccess}, RemovedSuccess={RemovedSuccess}, BatchFailures={BatchFailures}, BatchLatencyMs={BatchLatencyMs}",
            context.LeaseToken,
            changes.Count,
            documentsToIndex.Count,
            documentsToRemove.Count,
            skippedNonWorldEntityCount,
            skippedMissingDiscriminatorCount,
            skippedUnexpectedDiscriminatorCount,
            indexedDocumentCount,
            removedDocumentCount,
            perEntityFailureCount,
            batchStopwatch.ElapsedMilliseconds);
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

        if (entity.Properties is not null)
        {
            parts.Add(JsonSerializer.Serialize(entity.Properties));
        }

        if (entity.SystemProperties is not null)
        {
            parts.Add(JsonSerializer.Serialize(entity.SystemProperties));
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
            CreatedAt = new DateTimeOffset(entity.CreatedAt, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(entity.UpdatedAt, TimeSpan.Zero),
            Path = entity.Path?.Select(g => g.ToString()).ToList() ?? [],
            Depth = entity.Depth,
            SchemaId = entity.SchemaId,
            Properties = entity.Properties is not null ? JsonSerializer.Serialize(entity.Properties) : null,
            SystemProperties = entity.SystemProperties is not null ? JsonSerializer.Serialize(entity.SystemProperties) : null,
            SchemaVersion = entity.SchemaVersion,
            ContentVector = contentVector.ToArray()
        };
    }

    private static bool TryMapToWorldEntity(JsonElement changeDocument, out WorldEntity entity)
    {
        entity = null!;

        if (!changeDocument.TryGetProperty("_type", out var discriminatorElement) ||
            !string.Equals(discriminatorElement.GetString(), WorldEntityDiscriminator, StringComparison.Ordinal))
        {
            return false;
        }

        var worldId = Guid.Parse(changeDocument.GetProperty("worldId").GetString()!);
        var parentId = ParseNullableGuid(changeDocument, "parentId");
        var entityType = ParseEntityType(changeDocument.GetProperty("entityType"));
        var name = changeDocument.GetProperty("name").GetString()!;
        var ownerId = changeDocument.GetProperty("ownerId").GetString()!;
        var schemaId = ParseNullableString(changeDocument, "schemaId");
        var description = ParseNullableString(changeDocument, "description");
        var tags = ParseStringList(changeDocument, "tags");
        var path = ParseGuidPath(changeDocument, "path");
        var depth = ParseInt(changeDocument, "depth", 0);
        var schemaVersion = ParseInt(changeDocument, "schemaVersion", 1);
        var properties = ParsePropertyBag(changeDocument, "properties");
        var systemProperties = ParsePropertyBag(changeDocument, "systemProperties");

        entity = WorldEntity.Create(
            worldId,
            entityType,
            name,
            ownerId,
            description,
            parentId,
            tags,
            schemaId,
            properties,
            systemProperties,
            parentPath: parentId.HasValue ? path.Take(Math.Max(path.Count - 1, 0)).ToList() : null,
            parentDepth: parentId.HasValue ? depth - 1 : -1,
            schemaVersion: schemaVersion);

        SetPrivateProperty(entity, nameof(WorldEntity.Id), Guid.Parse(changeDocument.GetProperty("id").GetString()!));
        SetPrivateProperty(entity, nameof(WorldEntity.Path), path);
        SetPrivateProperty(entity, nameof(WorldEntity.Depth), depth);
        SetPrivateProperty(entity, nameof(WorldEntity.HasChildren), ParseBool(changeDocument, "hasChildren", false));
        SetPrivateProperty(entity, nameof(WorldEntity.CreatedAt), ParseDateTime(changeDocument, "createdAt"));
        SetPrivateProperty(entity, nameof(WorldEntity.UpdatedAt), ParseDateTime(changeDocument, "updatedAt"));
        SetPrivateProperty(entity, nameof(WorldEntity.IsDeleted), ParseBool(changeDocument, "isDeleted", false));
        SetPrivateProperty(entity, nameof(WorldEntity.DeletedDate), ParseNullableDateTime(changeDocument, "deletedDate"));
        SetPrivateProperty(entity, nameof(WorldEntity.DeletedBy), ParseNullableString(changeDocument, "deletedBy"));
        SetPrivateProperty(entity, nameof(WorldEntity.CreatedBy), ParseNullableString(changeDocument, "createdBy"));
        SetPrivateProperty(entity, nameof(WorldEntity.ModifiedBy), ParseNullableString(changeDocument, "modifiedBy"));
        SetPrivateProperty(entity, nameof(WorldEntity.Ttl), ParseNullableInt(changeDocument, "ttl"));

        return true;
    }

    private static Guid? ParseNullableGuid(JsonElement document, string propertyName)
    {
        return document.TryGetProperty(propertyName, out var propertyElement) &&
               propertyElement.ValueKind == JsonValueKind.String &&
               Guid.TryParse(propertyElement.GetString(), out var parsedValue)
            ? parsedValue
            : null;
    }

    private static EntityType ParseEntityType(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString();
            if (Enum.TryParse<EntityType>(value, ignoreCase: true, out var parsedEnum))
            {
                return parsedEnum;
            }
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var enumValue))
        {
            return (EntityType)enumValue;
        }

        throw new InvalidOperationException($"Invalid EntityType value '{element.GetRawText()}'.");
    }

    private static List<string> ParseStringList(JsonElement document, string propertyName)
    {
        if (!document.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return element.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToList();
    }

    private static List<Guid> ParseGuidPath(JsonElement document, string propertyName)
    {
        if (!document.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var result = new List<Guid>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out var parsed))
            {
                result.Add(parsed);
            }
        }

        return result;
    }

    private static Dictionary<string, object>? ParsePropertyBag(JsonElement document, string propertyName)
    {
        if (!document.TryGetProperty(propertyName, out var element) ||
            element.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            return null;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            var raw = element.GetString();
            return string.IsNullOrWhiteSpace(raw)
                ? null
                : JsonSerializer.Deserialize<Dictionary<string, object>>(raw);
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
        }

        throw new InvalidOperationException("Expected property bag to be either JSON object, JSON string, or null.");
    }

    private static DateTime ParseDateTime(JsonElement document, string propertyName)
    {
        return document.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? DateTime.Parse(element.GetString()!, null, System.Globalization.DateTimeStyles.RoundtripKind)
            : DateTime.UtcNow;
    }

    private static DateTime? ParseNullableDateTime(JsonElement document, string propertyName)
    {
        if (!document.TryGetProperty(propertyName, out var element) || element.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTime.Parse(element.GetString()!, null, System.Globalization.DateTimeStyles.RoundtripKind);
    }

    private static string? ParseNullableString(JsonElement document, string propertyName)
    {
        return document.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : null;
    }

    private static int ParseInt(JsonElement document, string propertyName, int defaultValue)
    {
        return document.TryGetProperty(propertyName, out var element) &&
               element.ValueKind == JsonValueKind.Number &&
               element.TryGetInt32(out var value)
            ? value
            : defaultValue;
    }

    private static bool TryParseUnixTimestamp(JsonElement document, string propertyName, out long value)
    {
        value = 0;

        return document.TryGetProperty(propertyName, out var element)
            && element.ValueKind == JsonValueKind.Number
            && element.TryGetInt64(out value);
    }

    private static int? ParseNullableInt(JsonElement document, string propertyName)
    {
        return document.TryGetProperty(propertyName, out var element) &&
               element.ValueKind == JsonValueKind.Number &&
               element.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static bool ParseBool(JsonElement document, string propertyName, bool defaultValue)
    {
        return document.TryGetProperty(propertyName, out var element) && element.ValueKind == JsonValueKind.True
            ? true
            : document.TryGetProperty(propertyName, out element) && element.ValueKind == JsonValueKind.False
                ? false
                : defaultValue;
    }

    private static void SetPrivateProperty<T>(WorldEntity entity, string propertyName, T value)
    {
        var propertyInfo = typeof(WorldEntity).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Property '{propertyName}' not found on WorldEntity.");

        propertyInfo.SetValue(entity, value);
    }
}
