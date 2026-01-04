namespace LibrisMaleficarum.Api.Models.Responses;

/// <summary>
/// Generic wrapper for paginated API responses.
/// </summary>
/// <typeparam name="T">The type of the items in the response.</typeparam>
public class PaginatedApiResponse<T>
{
    /// <summary>
    /// Gets or sets the collection of items in the current page.
    /// </summary>
    public required IReadOnlyList<T> Data { get; set; }

    /// <summary>
    /// Gets or sets the pagination metadata.
    /// </summary>
    public required PaginationMeta Meta { get; set; }
}

/// <summary>
/// Pagination metadata.
/// </summary>
public class PaginationMeta
{
    /// <summary>
    /// Gets or sets the number of items returned in this response.
    /// </summary>
    public required int Count { get; set; }

    /// <summary>
    /// Gets or sets the continuation token for fetching the next page.
    /// </summary>
    public string? NextCursor { get; set; }
}
