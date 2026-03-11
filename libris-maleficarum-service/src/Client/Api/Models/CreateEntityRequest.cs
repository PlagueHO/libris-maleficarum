namespace LibrisMaleficarum.Api.Client.Models;

/// <summary>
/// Request model for creating a new world entity.
/// </summary>
public sealed class CreateEntityRequest
{
    /// <summary>
    /// Gets the entity name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the entity description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the entity type (e.g., "Character", "Location", "Campaign").
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Gets the parent entity identifier for hierarchical relationships (null for root-level).
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// Gets the list of tags for categorization.
    /// </summary>
    public List<string>? Tags { get; init; }

    /// <summary>
    /// Gets the custom attributes as key-value pairs.
    /// </summary>
    public Dictionary<string, object>? Attributes { get; init; }

    /// <summary>
    /// Gets the schema version for this entity type.
    /// </summary>
    public int SchemaVersion { get; init; } = 1;
}
