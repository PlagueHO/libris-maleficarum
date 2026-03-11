namespace LibrisMaleficarum.Api.Client.Models;

/// <summary>
/// Response model for entity data returned by the API.
/// </summary>
public sealed class EntityResponse
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the world identifier this entity belongs to.
    /// </summary>
    public required Guid WorldId { get; init; }

    /// <summary>
    /// Gets the parent entity identifier (null for root-level entities).
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// Gets the entity type (e.g., "Character", "Location", "Campaign").
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Gets the entity name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the entity description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the list of tags.
    /// </summary>
    public required List<string> Tags { get; init; }

    /// <summary>
    /// Gets the array of ancestor IDs from root to parent (for hierarchy queries).
    /// </summary>
    public required List<Guid> Path { get; init; }

    /// <summary>
    /// Gets the hierarchy level (0 = root).
    /// </summary>
    public required int Depth { get; init; }

    /// <summary>
    /// Gets a value indicating whether this entity has children.
    /// </summary>
    public required bool HasChildren { get; init; }

    /// <summary>
    /// Gets the identifier of the user who owns this entity.
    /// </summary>
    public required Guid OwnerId { get; init; }

    /// <summary>
    /// Gets the custom attributes.
    /// </summary>
    public Dictionary<string, object>? Attributes { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the entity was created.
    /// </summary>
    public required DateTime CreatedDate { get; init; }

    /// <summary>
    /// Gets the UTC timestamp when the entity was last modified.
    /// </summary>
    public required DateTime ModifiedDate { get; init; }

    /// <summary>
    /// Gets a value indicating whether this entity has been soft-deleted.
    /// </summary>
    public required bool IsDeleted { get; init; }

    /// <summary>
    /// Gets the schema version of this entity.
    /// </summary>
    public required int SchemaVersion { get; init; }
}
