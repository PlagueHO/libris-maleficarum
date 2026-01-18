namespace LibrisMaleficarum.Api.Models.Responses;

using LibrisMaleficarum.Domain.ValueObjects;

/// <summary>
/// Response model for entity data.
/// </summary>
public class EntityResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for this entity.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the world identifier.
    /// </summary>
    public required Guid WorldId { get; set; }

    /// <summary>
    /// Gets or sets the parent entity identifier (null for root-level entities).
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the entity type.
    /// </summary>
    public required EntityType EntityType { get; set; }

    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the entity description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the list of tags.
    /// </summary>
    public required List<string> Tags { get; set; }

    /// <summary>
    /// Gets or sets the array of ancestor IDs from root to parent (for hierarchy queries).
    /// </summary>
    public required List<Guid> Path { get; set; }

    /// <summary>
    /// Gets or sets the hierarchy level (0 = root).
    /// </summary>
    public required int Depth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entity has children (optimization flag).
    /// </summary>
    public required bool HasChildren { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who owns this entity.
    /// </summary>
    public required string OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the custom attributes.
    /// </summary>
    public required Dictionary<string, object> Attributes { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public required DateTime CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the last modification timestamp.
    /// </summary>
    public required DateTime ModifiedDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entity has been soft-deleted.
    /// </summary>
    public required bool IsDeleted { get; set; }
}
