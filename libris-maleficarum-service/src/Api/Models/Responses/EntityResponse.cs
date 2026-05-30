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
    /// Gets or sets the optional schema identifier for this entity's property template.
    /// </summary>
    public string? SchemaId { get; set; }

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
    /// Gets or sets the identifier of the user who created this entity.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who last modified this entity.
    /// </summary>
    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Gets or sets the common properties.
    /// </summary>
    public required Dictionary<string, object> Properties { get; set; }

    /// <summary>
    /// Gets or sets the system-specific properties.
    /// </summary>
    public required Dictionary<string, object> SystemProperties { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public required DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last modification timestamp.
    /// </summary>
    public required DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entity has been soft-deleted.
    /// </summary>
    public required bool IsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the soft-delete timestamp.
    /// </summary>
    public DateTime? DeletedDate { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the user who soft-deleted this entity.
    /// </summary>
    public string? DeletedBy { get; set; }

    /// <summary>
    /// Gets or sets the schema version of this entity.
    /// Indicates which version of the entity type's schema was used when created/last migrated.
    /// </summary>
    public required int SchemaVersion { get; set; }
}
