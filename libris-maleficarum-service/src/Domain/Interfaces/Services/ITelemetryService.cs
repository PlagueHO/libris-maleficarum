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
    /// Starts a new activity with the given name and tags.
    /// </summary>
    /// <param name="operationName">The name of the operation being traced.</param>
    /// <param name="tags">Optional tags to add to the activity.</param>
    /// <returns>An activity that should be disposed after the operation completes.</returns>
    Activity? StartActivity(string operationName, Dictionary<string, object>? tags = null);
}
