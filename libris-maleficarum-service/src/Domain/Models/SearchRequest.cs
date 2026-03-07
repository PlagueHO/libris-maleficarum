namespace LibrisMaleficarum.Domain.Models;

using LibrisMaleficarum.Domain.ValueObjects;

/// <summary>
/// Represents a search query from the API layer to the search service.
/// </summary>
public class SearchRequest
{
    /// <summary>
    /// Gets the world identifier to search within.
    /// </summary>
    public required Guid WorldId { get; init; }

    /// <summary>
    /// Gets the search query string.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Gets the search mode (Text, Vector, or Hybrid).
    /// </summary>
    public SearchMode Mode { get; init; } = SearchMode.Hybrid;

    /// <summary>
    /// Gets the entity type filter.
    /// </summary>
    public EntityType? EntityTypeFilter { get; init; }

    /// <summary>
    /// Gets the tags filter.
    /// </summary>
    public List<string>? TagsFilter { get; init; }

    /// <summary>
    /// Gets the name filter (prefix match).
    /// </summary>
    public string? NameFilter { get; init; }

    /// <summary>
    /// Gets the parent entity ID filter.
    /// </summary>
    public Guid? ParentIdFilter { get; init; }

    /// <summary>
    /// Gets the maximum number of results to return.
    /// </summary>
    public int Limit { get; init; } = 50;

    /// <summary>
    /// Gets the number of results to skip for pagination.
    /// </summary>
    public int Offset { get; init; } = 0;
}

/// <summary>
/// Defines the available search modes.
/// </summary>
public enum SearchMode
{
    /// <summary>
    /// Full-text search only.
    /// </summary>
    Text,

    /// <summary>
    /// Vector similarity search only.
    /// </summary>
    Vector,

    /// <summary>
    /// Combined text and vector search for best relevance.
    /// </summary>
    Hybrid
}
