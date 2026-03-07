namespace LibrisMaleficarum.Domain.Models;

/// <summary>
/// Represents a single search result returned by the search service.
/// </summary>
public class SearchResult
{
    /// <summary>
    /// Gets the entity unique identifier.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the entity name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the entity type.
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Gets a snippet of the entity description relevant to the query.
    /// </summary>
    public string? DescriptionSnippet { get; init; }

    /// <summary>
    /// Gets the relevance score between 0 and 1.
    /// </summary>
    public required double RelevanceScore { get; init; }

    /// <summary>
    /// Gets the world identifier.
    /// </summary>
    public required Guid WorldId { get; init; }

    /// <summary>
    /// Gets the parent entity identifier.
    /// </summary>
    public Guid? ParentId { get; init; }

    /// <summary>
    /// Gets the tags associated with the entity.
    /// </summary>
    public required List<string> Tags { get; init; }

    /// <summary>
    /// Gets the owner identifier.
    /// </summary>
    public required string OwnerId { get; init; }

    /// <summary>
    /// Gets the creation timestamp.
    /// </summary>
    public required DateTimeOffset CreatedDate { get; init; }

    /// <summary>
    /// Gets the last modification timestamp.
    /// </summary>
    public required DateTimeOffset ModifiedDate { get; init; }
}

/// <summary>
/// Represents a paginated set of search results.
/// </summary>
public class SearchResultSet
{
    /// <summary>
    /// Gets the search results.
    /// </summary>
    public required List<SearchResult> Results { get; init; }

    /// <summary>
    /// Gets the total count of matching results.
    /// </summary>
    public required int TotalCount { get; init; }

    /// <summary>
    /// Gets the current offset in the result set.
    /// </summary>
    public required int Offset { get; init; }

    /// <summary>
    /// Gets the maximum results per page.
    /// </summary>
    public required int Limit { get; init; }
}
