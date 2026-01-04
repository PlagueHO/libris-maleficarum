namespace LibrisMaleficarum.Domain.Interfaces.Services;

using LibrisMaleficarum.Domain.Entities;

/// <summary>
/// Service interface for searching entities within a world.
/// Provides abstraction for future Azure AI Search integration.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Searches for entities within a world by query string.
    /// Performs case-insensitive partial matching on Name, Description, and Tags.
    /// </summary>
    /// <param name="worldId">The world identifier to search within.</param>
    /// <param name="query">The search query string.</param>
    /// <param name="sortBy">Field to sort by (name, createdDate, modifiedDate). Default: modifiedDate.</param>
    /// <param name="sortOrder">Sort order (asc, desc). Default: desc.</param>
    /// <param name="limit">Maximum number of results to return (default 50, max 200).</param>
    /// <param name="cursor">Continuation cursor for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple containing matching entities and optional next cursor.</returns>
    Task<(IEnumerable<WorldEntity> Entities, string? NextCursor)> SearchEntitiesAsync(
        Guid worldId,
        string query,
        string? sortBy = null,
        string? sortOrder = null,
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default);
}
