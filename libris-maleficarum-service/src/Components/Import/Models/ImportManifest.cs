namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// Computed manifest representing the resolved import hierarchy.
/// </summary>
public sealed class ImportManifest
{
    /// <summary>
    /// Gets the world definition for this import.
    /// </summary>
    public required WorldImportDefinition World { get; init; }

    /// <summary>
    /// Gets the flat list of all resolved entities.
    /// </summary>
    public required IReadOnlyList<ResolvedEntity> Entities { get; init; }

    /// <summary>
    /// Gets entities grouped by their depth in the hierarchy.
    /// </summary>
    public required IReadOnlyDictionary<int, IReadOnlyList<ResolvedEntity>> EntitiesByDepth { get; init; }

    /// <summary>
    /// Gets the maximum depth in the entity hierarchy.
    /// </summary>
    public int MaxDepth { get; init; }

    /// <summary>
    /// Gets the total number of entities in the import.
    /// </summary>
    public int TotalEntityCount { get; init; }

    /// <summary>
    /// Gets the count of entities grouped by entity type.
    /// </summary>
    public required IReadOnlyDictionary<string, int> CountsByType { get; init; }
}
