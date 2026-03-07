namespace LibrisMaleficarum.Domain.Interfaces.Services;

using LibrisMaleficarum.Domain.Models;

/// <summary>
/// Service interface for searching entities within a world.
/// Uses Azure AI Search for hybrid (text + vector) search.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Searches for entities within a world using the specified search request.
    /// </summary>
    /// <param name="request">The search request containing query, filters, and pagination options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A set of search results with pagination metadata.</returns>
    Task<SearchResultSet> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
}
