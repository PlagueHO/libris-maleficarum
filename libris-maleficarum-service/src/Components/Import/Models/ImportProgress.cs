namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// Progress reporting for the import operation.
/// </summary>
public sealed class ImportProgress
{
    /// <summary>
    /// Gets the total number of entities to import.
    /// </summary>
    public required int TotalEntities { get; init; }

    /// <summary>
    /// Gets the number of entities completed so far.
    /// </summary>
    public required int CompletedEntities { get; init; }

    /// <summary>
    /// Gets the current hierarchy depth being processed.
    /// </summary>
    public required int CurrentDepth { get; init; }

    /// <summary>
    /// Gets the name of the entity currently being processed.
    /// </summary>
    public required string CurrentEntityName { get; init; }

    /// <summary>
    /// Gets the current phase of the import operation.
    /// </summary>
    public required ImportPhase Phase { get; init; }
}

/// <summary>
/// Specifies the phase of an import operation.
/// </summary>
public enum ImportPhase
{
    /// <summary>
    /// Reading and parsing import source files.
    /// </summary>
    Reading,

    /// <summary>
    /// Validating parsed content and resolving hierarchy.
    /// </summary>
    Validating,

    /// <summary>
    /// Creating the world entity via the backend API.
    /// </summary>
    CreatingWorld,

    /// <summary>
    /// Creating world entities via the backend API.
    /// </summary>
    CreatingEntities,

    /// <summary>
    /// Import operation has completed.
    /// </summary>
    Complete
}
