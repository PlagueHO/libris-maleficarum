namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// Result of an import execution against the backend API.
/// </summary>
public sealed class ImportResult
{
    /// <summary>
    /// Gets a value indicating whether the import completed successfully.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the identifier of the created or updated world.
    /// </summary>
    public required Guid WorldId { get; init; }

    /// <summary>
    /// Gets the total number of entities successfully created.
    /// </summary>
    public required int TotalEntitiesCreated { get; init; }

    /// <summary>
    /// Gets the total number of entities that failed to import.
    /// </summary>
    public required int TotalEntitiesFailed { get; init; }

    /// <summary>
    /// Gets the total number of entities skipped (e.g., due to parent failure).
    /// </summary>
    public required int TotalEntitiesSkipped { get; init; }

    /// <summary>
    /// Gets the count of created entities grouped by entity type.
    /// </summary>
    public required IReadOnlyDictionary<string, int> CreatedByType { get; init; }

    /// <summary>
    /// Gets the list of errors encountered during entity import.
    /// </summary>
    public required IReadOnlyList<EntityImportError> Errors { get; init; }

    /// <summary>
    /// Gets the total duration of the import operation.
    /// </summary>
    public required TimeSpan Duration { get; init; }
}
