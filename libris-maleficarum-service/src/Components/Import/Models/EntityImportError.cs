namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// An error that occurred while importing a specific entity.
/// </summary>
public sealed class EntityImportError
{
    /// <summary>
    /// Gets the local identifier of the entity that failed.
    /// </summary>
    public required string LocalId { get; init; }

    /// <summary>
    /// Gets the display name of the entity that failed.
    /// </summary>
    public required string EntityName { get; init; }

    /// <summary>
    /// Gets the error message describing the failure.
    /// </summary>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Gets the source file path of the entity, if available.
    /// </summary>
    public required string? FilePath { get; init; }

    /// <summary>
    /// Gets the local identifiers of descendant entities that were skipped due to this failure.
    /// </summary>
    public required IReadOnlyList<string> SkippedDescendantLocalIds { get; init; }
}
