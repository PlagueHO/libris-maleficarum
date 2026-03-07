namespace LibrisMaleficarum.Api.Models.Responses;

/// <summary>
/// Response model for entity search results.
/// </summary>
public class SearchResponse
{
    /// <summary>
    /// Gets or sets the search result items.
    /// </summary>
    public required List<SearchResultItem> Data { get; set; }

    /// <summary>
    /// Gets or sets the pagination metadata.
    /// </summary>
    public required SearchMeta Meta { get; set; }
}

/// <summary>
/// A single search result item.
/// </summary>
public class SearchResultItem
{
    /// <summary>
    /// Gets or sets the entity identifier.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the entity type.
    /// </summary>
    public required string EntityType { get; set; }

    /// <summary>
    /// Gets or sets a snippet of the entity description.
    /// </summary>
    public string? DescriptionSnippet { get; set; }

    /// <summary>
    /// Gets or sets the relevance score (0 to 1).
    /// </summary>
    public double RelevanceScore { get; set; }

    /// <summary>
    /// Gets or sets the world identifier.
    /// </summary>
    public Guid WorldId { get; set; }

    /// <summary>
    /// Gets or sets the parent entity identifier.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the tags associated with the entity.
    /// </summary>
    public required List<string> Tags { get; set; }

    /// <summary>
    /// Gets or sets the owner identifier.
    /// </summary>
    public required string OwnerId { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTimeOffset CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the last modification timestamp.
    /// </summary>
    public DateTimeOffset ModifiedDate { get; set; }
}

/// <summary>
/// Pagination metadata for search results.
/// </summary>
public class SearchMeta
{
    /// <summary>
    /// Gets or sets the total number of matching results.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the current offset in the result set.
    /// </summary>
    public int Offset { get; set; }

    /// <summary>
    /// Gets or sets the maximum results per page.
    /// </summary>
    public int Limit { get; set; }
}
