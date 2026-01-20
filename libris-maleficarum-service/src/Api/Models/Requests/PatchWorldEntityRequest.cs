namespace LibrisMaleficarum.Api.Models.Requests;

using LibrisMaleficarum.Domain.ValueObjects;

/// <summary>
/// Request model for partially updating a world entity (all fields optional).
/// </summary>
public class PatchWorldEntityRequest
{
    /// <summary>
    /// Gets or sets the entity name (1-200 characters).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the entity description (max 5000 characters).
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the entity type (Character, Location, Campaign, etc.).
    /// </summary>
    public EntityType? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the parent entity identifier for hierarchical relationships.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the list of tags for categorization (max 20 tags, each max 50 characters).
    /// If provided, replaces existing tags.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets the custom attributes as key-value pairs (max 100KB serialized).
    /// If provided, merges with existing attributes.
    /// </summary>
    public Dictionary<string, object>? Attributes { get; set; }
}
