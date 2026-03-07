using System.Diagnostics;

namespace LibrisMaleficarum.Domain.Interfaces.Services;

/// <summary>
/// Service that provides access to telemetry instruments (meters and activity sources)
/// for recording metrics and traces throughout the application.
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Records that a world was created.
    /// </summary>
    /// <param name="worldName">The name of the created world.</param>
    void RecordWorldCreated(string worldName);

    /// <summary>
    /// Records that a world was deleted.
    /// </summary>
    /// <param name="worldName">The name of the deleted world.</param>
    void RecordWorldDeleted(string worldName);

    /// <summary>
    /// Records that an entity was created.
    /// </summary>
    /// <param name="entityType">The type of the created entity.</param>
    void RecordEntityCreated(string entityType);

    /// <summary>
    /// Records that an entity was deleted.
    /// </summary>
    /// <param name="entityType">The type of the deleted entity.</param>
    void RecordEntityDeleted(string entityType);

    /// <summary>
    /// Records that a document was successfully indexed in the search index.
    /// </summary>
    /// <param name="entityType">The entity type of the indexed document.</param>
    void RecordDocumentIndexed(string entityType);

    /// <summary>
    /// Records an indexing failure.
    /// </summary>
    /// <param name="entityType">The entity type that failed to index.</param>
    void RecordIndexingFailure(string entityType);

    /// <summary>
    /// Records a search query execution.
    /// </summary>
    /// <param name="searchMode">The search mode used (text, vector, hybrid).</param>
    void RecordSearchQuery(string searchMode);

    /// <summary>
    /// Records the sync lag between entity change and index update.
    /// </summary>
    /// <param name="lagSeconds">The lag in seconds.</param>
    void RecordSyncLag(double lagSeconds);

    /// <summary>
    /// Records the latency of embedding generation.
    /// </summary>
    /// <param name="latencyMs">The latency in milliseconds.</param>
    void RecordEmbeddingLatency(double latencyMs);

    /// <summary>
    /// Records the latency of a search query.
    /// </summary>
    /// <param name="latencyMs">The latency in milliseconds.</param>
    void RecordSearchLatency(double latencyMs);

    /// <summary>
    /// Starts a new activity for indexing operations.
    /// </summary>
    /// <param name="entityId">The entity identifier being indexed.</param>
    /// <param name="entityType">The entity type being indexed.</param>
    /// <returns>An activity that should be disposed after the operation completes.</returns>
    Activity? StartIndexingActivity(string entityId, string entityType);

    /// <summary>
    /// Starts a new activity for search operations.
    /// </summary>
    /// <param name="worldId">The world being searched.</param>
    /// <param name="searchMode">The search mode being used.</param>
    /// <returns>An activity that should be disposed after the operation completes.</returns>
    Activity? StartSearchActivity(string worldId, string searchMode);

    /// <summary>
    /// Starts a new activity with the given name and tags.
    /// </summary>
    /// <param name="operationName">The name of the operation being traced.</param>
    /// <param name="tags">Optional tags to add to the activity.</param>
    /// <returns>An activity that should be disposed after the operation completes.</returns>
    Activity? StartActivity(string operationName, Dictionary<string, object>? tags = null);
}
