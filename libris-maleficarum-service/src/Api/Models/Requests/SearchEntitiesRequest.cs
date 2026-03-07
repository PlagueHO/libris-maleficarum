namespace LibrisMaleficarum.Api.Models.Requests;

/// <summary>
/// Request model for searching entities within a world.
/// Binds query parameters from the search endpoint.
/// </summary>
public class SearchEntitiesRequest
{
    /// <summary>
    /// Gets or sets the search query string (required).
    /// </summary>
    public string Q { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the search mode (hybrid, text, vector). Default: hybrid.
    /// </summary>
    public string? Mode { get; set; }

    /// <summary>
    /// Gets or sets the entity type filter.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Gets or sets the tags filter (comma-separated).
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets the name filter (prefix match).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the parent entity ID filter.
    /// </summary>
    public Guid? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of results (default 50, max 200).
    /// </summary>
    public int Limit { get; set; } = 50;

    /// <summary>
    /// Gets or sets the number of results to skip for pagination.
    /// </summary>
    public int Offset { get; set; } = 0;
}
