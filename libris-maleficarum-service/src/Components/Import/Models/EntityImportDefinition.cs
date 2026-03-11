namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// Represents a world entity definition parsed from an entity JSON file.
/// </summary>
public sealed class EntityImportDefinition
{
    /// <summary>
    /// Gets the local identifier used to reference this entity within the import source.
    /// </summary>
    public required string LocalId { get; init; }

    /// <summary>
    /// Gets the type of entity (e.g., Continent, Country, Character).
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Gets the display name of the entity.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description of the entity.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the local identifier of this entity's parent, if any.
    /// </summary>
    public string? ParentLocalId { get; init; }

    /// <summary>
    /// Gets the optional tags associated with this entity.
    /// </summary>
    public List<string>? Tags { get; init; }

    /// <summary>
    /// Gets the optional custom properties for this entity.
    /// </summary>
    public Dictionary<string, object>? Properties { get; init; }

    /// <summary>
    /// The source file path (for error reporting). Not serialized from JSON.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? SourceFilePath { get; set; }
}
