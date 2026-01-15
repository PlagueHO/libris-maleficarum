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
    public Activity? StartActivity(string operationName, Dictionary<string, object>? tags = null)
    {
        // Return null activity to indicate tracing is not enabled
        return null;
    }
}
