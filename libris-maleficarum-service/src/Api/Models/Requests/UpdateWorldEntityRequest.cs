namespace LibrisMaleficarum.Api.Models.Requests;

using LibrisMaleficarum.Domain.ValueObjects;

/// <summary>
/// Request model for updating an existing world entity.
/// </summary>
public class UpdateWorldEntityRequest
{
    /// <summary>
    /// Gets or sets the entity name (1-200 characters).
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the entity description (max 5000 characters).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the entity type (Character, Location, Campaign, etc.).
    /// </summary>
    public required EntityType EntityType { get; set; }

    /// <summary>
    /// Gets or sets the parent entity identifier for hierarchical relationships (null for root-level).
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the list of tags for categorization (max 20 tags, each max 50 characters).
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets the custom attributes as key-value pairs (max 100KB serialized).
    /// </summary>
    public Dictionary<string, object>? Attributes { get; set; }
}
