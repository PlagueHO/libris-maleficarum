namespace LibrisMaleficarum.Domain.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Represents a WorldEntity document mapped for the Azure AI Search index.
/// </summary>
/// <remarks>
/// All properties use <see cref="JsonPropertyNameAttribute"/> to map between PascalCase C# names
/// and the camelCase field names defined in the Azure AI Search index schema.
/// </remarks>
public class SearchIndexDocument
{
    /// <summary>
    /// Gets the unique identifier (WorldEntity.Id as string).
    /// </summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>
    /// Gets the world identifier (partition filter).
    /// </summary>
    [JsonPropertyName("worldId")]
    public required string WorldId { get; init; }

    /// <summary>
    /// Gets the entity type as a string.
    /// </summary>
    [JsonPropertyName("entityType")]
    public required string EntityType { get; init; }

    /// <summary>
    /// Gets the entity name.
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>
    /// Gets the entity description.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Gets the tags collection.
    /// </summary>
    [JsonPropertyName("tags")]
    public required List<string> Tags { get; init; }

    /// <summary>
    /// Gets the parent entity identifier.
    /// </summary>
    [JsonPropertyName("parentId")]
    public string? ParentId { get; init; }

    /// <summary>
    /// Gets the owner identifier.
    /// </summary>
    [JsonPropertyName("ownerId")]
    public required string OwnerId { get; init; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Gets the last modification timestamp.
    /// </summary>
    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Gets the ancestor IDs for hierarchy queries.
    /// </summary>
    [JsonPropertyName("path")]
    public required List<string> Path { get; init; }

    /// <summary>
    /// Gets the hierarchy depth level.
    /// </summary>
    [JsonPropertyName("depth")]
    public required int Depth { get; init; }

    /// <summary>
    /// Gets the schema identifier for this entity's property template.
    /// </summary>
    [JsonPropertyName("schemaId")]
    public string? SchemaId { get; init; }

    /// <summary>
    /// Gets the common properties as a JSON string.
    /// </summary>
    [JsonPropertyName("properties")]
    public string? Properties { get; init; }

    /// <summary>
    /// Gets the system-specific properties as a JSON string.
    /// </summary>
    [JsonPropertyName("systemProperties")]
    public string? SystemProperties { get; init; }

    /// <summary>
    /// Gets the schema version for compatibility.
    /// Not projected in search result responses; defaults to 0 when absent.
    /// </summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; }

    /// <summary>
    /// Gets the vector embedding for semantic search.
    /// Azure AI Search never returns vector fields in query results; null when deserializing search responses.
    /// </summary>
    [JsonPropertyName("contentVector")]
    public IReadOnlyList<float>? ContentVector { get; init; }
}
