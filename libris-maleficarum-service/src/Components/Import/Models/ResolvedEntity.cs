namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// An entity import definition with resolved hierarchy information.
/// </summary>
public sealed class ResolvedEntity
{
    /// <summary>
    /// Gets the original entity import definition.
    /// </summary>
    public required EntityImportDefinition Definition { get; init; }

    /// <summary>
    /// Gets the globally unique identifier assigned to this entity.
    /// </summary>
    public required Guid AssignedId { get; init; }

    /// <summary>
    /// Gets the resolved parent entity identifier, or <see langword="null"/> for root entities.
    /// </summary>
    public required Guid? ResolvedParentId { get; init; }

    /// <summary>
    /// Gets the ordered list of ancestor identifiers from root to this entity.
    /// </summary>
    public required List<Guid> Path { get; init; }

    /// <summary>
    /// Gets the depth of this entity in the hierarchy (0 for root entities).
    /// </summary>
    public required int Depth { get; init; }

    /// <summary>
    /// Gets the list of resolved child entities.
    /// </summary>
    public required List<ResolvedEntity> Children { get; init; }
}
