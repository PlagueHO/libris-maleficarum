namespace LibrisMaleficarum.Domain.Models;

/// <summary>
/// Represents a WorldEntity document mapped for the Azure AI Search index.
/// </summary>
public class SearchIndexDocument
{
    /// <summary>
    /// Gets the unique identifier (WorldEntity.Id as string).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the world identifier (partition filter).
    /// </summary>
    public required string WorldId { get; init; }

    /// <summary>
    /// Gets the entity type as a string.
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
    /// Gets the tags collection.
    /// </summary>
    public required List<string> Tags { get; init; }

    /// <summary>
    /// Gets the parent entity identifier.
    /// </summary>
    public string? ParentId { get; init; }

    /// <summary>
    /// Gets the owner identifier.
    /// </summary>
    public required string OwnerId { get; init; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the last modification timestamp.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Gets the ancestor IDs for hierarchy queries.
    /// </summary>
    public required List<string> Path { get; init; }

    /// <summary>
    /// Gets the hierarchy depth level.
    /// </summary>
    public required int Depth { get; init; }

    /// <summary>
    /// Gets the schema identifier for this entity's property template.
    /// </summary>
    public string? SchemaId { get; init; }

    /// <summary>
    /// Gets the common properties as a JSON string.
    /// </summary>
    public string? Properties { get; init; }

    /// <summary>
    /// Gets the system-specific properties as a JSON string.
    /// </summary>
    public string? SystemProperties { get; init; }

    /// <summary>
    /// Gets the schema version for compatibility.
    /// </summary>
    public required int SchemaVersion { get; init; }

    /// <summary>
    /// Gets the vector embedding for semantic search.
    /// </summary>
    public required IReadOnlyList<float> ContentVector { get; init; }
}
