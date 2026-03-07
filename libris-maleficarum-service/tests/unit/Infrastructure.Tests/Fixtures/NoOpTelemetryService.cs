using System.Diagnostics;
using LibrisMaleficarum.Domain.Interfaces.Services;

namespace LibrisMaleficarum.Infrastructure.Tests.Fixtures;

/// <summary>
/// No-op implementation of <see cref="ITelemetryService"/> for unit testing.
/// This implementation does nothing and is useful for dependency injection in tests.
/// </summary>
public class NoOpTelemetryService : ITelemetryService
{
    /// <inheritdoc />
    public void RecordWorldCreated(string worldName)
    {
        // No-op
    }

    /// <inheritdoc />
    public void RecordWorldDeleted(string worldName)
    {
        // No-op
    }

    /// <inheritdoc />
    public void RecordEntityCreated(string entityType)
    {
        // No-op
    }

    /// <inheritdoc />
    public void RecordEntityDeleted(string entityType)
    {
        // No-op
    }

    /// <inheritdoc />
    public void RecordDocumentIndexed(string entityType)
    {
        // No-op
    }

    /// <inheritdoc />
    public void RecordIndexingFailure(string entityType)
    {
        // No-op
    }

    /// <inheritdoc />
    public void RecordSearchQuery(string searchMode)
    {
        // No-op
    }

    /// <inheritdoc />
    public void RecordSyncLag(double lagSeconds)
    {
        // No-op
    }

    /// <inheritdoc />
    public void RecordEmbeddingLatency(double latencyMs)
    {
        // No-op
    }

    /// <inheritdoc />
    public void RecordSearchLatency(double latencyMs)
    {
        // No-op
    }

    /// <inheritdoc />
    public Activity? StartIndexingActivity(string entityId, string entityType)
    {
        return null;
    }

    /// <inheritdoc />
    public Activity? StartSearchActivity(string worldId, string searchMode)
    {
        return null;
    }

    /// <inheritdoc />
    public Activity? StartActivity(string operationName, Dictionary<string, object>? tags = null)
    {
        // Return null activity to indicate tracing is not enabled
        return null;
    }
}
