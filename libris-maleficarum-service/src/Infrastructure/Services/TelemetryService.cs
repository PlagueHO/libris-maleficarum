using System.Diagnostics;
using System.Diagnostics.Metrics;
using LibrisMaleficarum.Domain.Interfaces.Services;

namespace LibrisMaleficarum.Infrastructure.Services;

/// <summary>
/// Default implementation of <see cref="ITelemetryService"/>.
/// Creates and manages counters and activities for application telemetry.
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly Counter<int> _worldsCreated;
    private readonly Counter<int> _worldsDeleted;
    private readonly Counter<int> _entitiesCreated;
    private readonly Counter<int> _entitiesDeleted;
    private readonly Counter<int> _documentsIndexed;
    private readonly Counter<int> _indexingFailures;
    private readonly Counter<int> _searchQueries;
    private readonly Counter<int> _batchDocumentsIndexed;
    private readonly Counter<int> _batchDocumentsRemoved;
    private readonly Counter<int> _batchDocumentsSkipped;
    private readonly Counter<int> _batchFailures;
    private readonly Histogram<double> _syncLagSeconds;
    private readonly Histogram<double> _embeddingLatencyMs;
    private readonly Histogram<double> _searchLatencyMs;
    private readonly Histogram<double> _batchLatencyMs;
    private readonly ActivitySource _activitySource;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryService"/> class.
    /// </summary>
    /// <param name="meter">The meter used to create counters for telemetry.</param>
    /// <param name="activitySource">The activity source used to create activities for distributed tracing.</param>
    public TelemetryService(Meter meter, ActivitySource activitySource)
    {
        _worldsCreated = meter.CreateCounter<int>("worlds.created", description: "Counts the number of worlds created");
        _worldsDeleted = meter.CreateCounter<int>("worlds.deleted", description: "Counts the number of worlds deleted");
        _entitiesCreated = meter.CreateCounter<int>("entities.created", description: "Counts the number of entities created");
        _entitiesDeleted = meter.CreateCounter<int>("entities.deleted", description: "Counts the number of entities deleted");
        _documentsIndexed = meter.CreateCounter<int>("search.documents.indexed", description: "Counts the number of documents indexed in search");
        _indexingFailures = meter.CreateCounter<int>("search.indexing.failures", description: "Counts the number of indexing failures");
        _searchQueries = meter.CreateCounter<int>("search.queries.executed", description: "Counts the number of search queries executed");
        _batchDocumentsIndexed = meter.CreateCounter<int>("search.batch.documents.indexed", description: "Counts the number of documents indexed in change feed batches");
        _batchDocumentsRemoved = meter.CreateCounter<int>("search.batch.documents.removed", description: "Counts the number of documents removed in change feed batches");
        _batchDocumentsSkipped = meter.CreateCounter<int>("search.batch.documents.skipped", description: "Counts the number of changes skipped in change feed batches");
        _batchFailures = meter.CreateCounter<int>("search.batch.failures", description: "Counts the number of failures in change feed batches");
        _syncLagSeconds = meter.CreateHistogram<double>("search.sync.lag.seconds", "s", "Index sync lag in seconds");
        _embeddingLatencyMs = meter.CreateHistogram<double>("search.embedding.latency.ms", "ms", "Embedding generation latency in milliseconds");
        _searchLatencyMs = meter.CreateHistogram<double>("search.query.latency.ms", "ms", "Search query latency in milliseconds");
        _batchLatencyMs = meter.CreateHistogram<double>("search.batch.latency.ms", "ms", "Change feed batch latency in milliseconds");
        _activitySource = activitySource;
    }

    /// <inheritdoc />
    public void RecordWorldCreated(string worldName)
    {
        _worldsCreated.Add(1, new KeyValuePair<string, object?>("world.name", worldName));
    }

    /// <inheritdoc />
    public void RecordWorldDeleted(string worldName)
    {
        _worldsDeleted.Add(1, new KeyValuePair<string, object?>("world.name", worldName));
    }

    /// <inheritdoc />
    public void RecordEntityCreated(string entityType)
    {
        _entitiesCreated.Add(1, new KeyValuePair<string, object?>("entity.type", entityType));
    }

    /// <inheritdoc />
    public void RecordEntityDeleted(string entityType)
    {
        _entitiesDeleted.Add(1, new KeyValuePair<string, object?>("entity.type", entityType));
    }

    /// <inheritdoc />
    public void RecordDocumentIndexed(string entityType)
    {
        _documentsIndexed.Add(1, new KeyValuePair<string, object?>("entity.type", entityType));
    }

    /// <inheritdoc />
    public void RecordIndexingFailure(string entityType)
    {
        _indexingFailures.Add(1, new KeyValuePair<string, object?>("entity.type", entityType));
    }

    /// <inheritdoc />
    public void RecordSearchQuery(string searchMode)
    {
        _searchQueries.Add(1, new KeyValuePair<string, object?>("search.mode", searchMode));
    }

    /// <inheritdoc />
    public void RecordSyncLag(double lagSeconds)
    {
        _syncLagSeconds.Record(lagSeconds);
    }

    /// <inheritdoc />
    public void RecordEmbeddingLatency(double latencyMs)
    {
        _embeddingLatencyMs.Record(latencyMs);
    }

    /// <inheritdoc />
    public void RecordSearchLatency(double latencyMs)
    {
        _searchLatencyMs.Record(latencyMs);
    }

    /// <inheritdoc />
    public void RecordBatchProcessed(int indexed, int removed, int skipped, int failures)
    {
        if (indexed > 0)
        {
            _batchDocumentsIndexed.Add(indexed);
        }

        if (removed > 0)
        {
            _batchDocumentsRemoved.Add(removed);
        }

        if (skipped > 0)
        {
            _batchDocumentsSkipped.Add(skipped);
        }

        if (failures > 0)
        {
            _batchFailures.Add(failures);
        }
    }

    /// <inheritdoc />
    public void RecordBatchLatency(double latencyMs)
    {
        _batchLatencyMs.Record(latencyMs);
    }

    /// <inheritdoc />
    public Activity? StartIndexingActivity(string entityId, string entityType)
    {
        var activity = _activitySource.StartActivity("SearchIndex.IndexDocument");
        activity?.SetTag("entity.id", entityId);
        activity?.SetTag("entity.type", entityType);
        return activity;
    }

    /// <inheritdoc />
    public Activity? StartSearchActivity(string worldId, string searchMode)
    {
        var activity = _activitySource.StartActivity("SearchIndex.SearchQuery");
        activity?.SetTag("world.id", worldId);
        activity?.SetTag("search.mode", searchMode);
        return activity;
    }

    /// <inheritdoc />
    public Activity? StartActivity(string operationName, Dictionary<string, object>? tags = null)
    {
        var activity = _activitySource.StartActivity(operationName);
        if (activity is not null && tags is not null)
        {
            foreach (var (key, value) in tags)
            {
                activity.SetTag(key, value);
            }
        }

        return activity;
    }
}
