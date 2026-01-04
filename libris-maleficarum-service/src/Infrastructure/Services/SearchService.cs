namespace LibrisMaleficarum.Infrastructure.Services;

using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Basic search service implementation using LINQ queries.
/// Provides case-insensitive partial matching on Name, Description, and Tags.
/// This implementation can be replaced with Azure AI Search for more advanced capabilities.
/// </summary>
public class SearchService : ISearchService
{
    private readonly ApplicationDbContext _context;
    private readonly IUserContextService _userContextService;
    private readonly IWorldRepository _worldRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchService"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="userContextService">The user context service for authorization.</param>
    /// <param name="worldRepository">The world repository for validation.</param>
    public SearchService(
        ApplicationDbContext context,
        IUserContextService userContextService,
        IWorldRepository worldRepository)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _worldRepository = worldRepository ?? throw new ArgumentNullException(nameof(worldRepository));
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<WorldEntity> Entities, string? NextCursor)> SearchEntitiesAsync(
        Guid worldId,
        string query,
        string? sortBy = null,
        string? sortOrder = null,
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Search query cannot be empty.", nameof(query));
        }

        // Verify world access authorization
        var currentUserId = await _userContextService.GetCurrentUserIdAsync();
        var world = await _worldRepository.GetByIdAsync(worldId, cancellationToken);

        if (world == null)
        {
            throw new WorldNotFoundException(worldId);
        }

        if (world.OwnerId != currentUserId)
        {
            throw new UnauthorizedWorldAccessException(worldId, currentUserId);
        }

        // Clamp limit to reasonable range (max 200)
        limit = Math.Clamp(limit, 1, 200);

        // Start with base query - case-insensitive partial matching on Name and Description
        // Tags matching is done client-side to work around in-memory database provider limitations
        // Note: WithPartitionKey() is removed to support in-memory database testing
        // EF Core Cosmos provider will still use the partition key for efficient queries when filtering by WorldId
        var queryLower = query.ToLowerInvariant();

        var baseEntities = await _context.WorldEntities
            .Where(e => e.WorldId == worldId && !e.IsDeleted)
            .ToListAsync(cancellationToken);

        // Client-side filtering for case-insensitive search on Name, Description, and Tags
        var filteredEntities = baseEntities
            .Where(e => e.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        (e.Description != null && e.Description.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                        e.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .AsQueryable();

        // Apply sorting (default: modifiedDate desc)
        IOrderedQueryable<WorldEntity> orderedQuery;
        var normalizedSortBy = sortBy?.ToLowerInvariant() ?? "modifieddate";
        var normalizedSortOrder = sortOrder?.ToLowerInvariant() ?? "desc";

        switch (normalizedSortBy)
        {
            case "name":
                orderedQuery = normalizedSortOrder == "asc"
                    ? filteredEntities.OrderBy(e => e.Name)
                    : filteredEntities.OrderByDescending(e => e.Name);
                break;
            case "createddate":
                orderedQuery = normalizedSortOrder == "asc"
                    ? filteredEntities.OrderBy(e => e.CreatedDate)
                    : filteredEntities.OrderByDescending(e => e.CreatedDate);
                break;
            case "modifieddate":
            default:
                orderedQuery = normalizedSortOrder == "asc"
                    ? filteredEntities.OrderBy(e => e.ModifiedDate)
                    : filteredEntities.OrderByDescending(e => e.ModifiedDate);
                break;
        }

        // Apply cursor pagination if provided
        int skip = 0;
        if (!string.IsNullOrEmpty(cursor))
        {
            try
            {
                // Cursor is an offset (skip count)
                skip = int.Parse(cursor);
            }
            catch (FormatException)
            {
                // Invalid cursor - ignore and start from beginning
                skip = 0;
            }
        }

        // Fetch limit + 1 to determine if there are more results
        // Since we already loaded entities from the database, just materialize the query
        var results = orderedQuery
            .Skip(skip)
            .Take(limit + 1)
            .ToList();

        // Determine next cursor
        string? nextCursor = null;
        if (results.Count > limit)
        {
            nextCursor = (skip + limit).ToString();
            results = results.Take(limit).ToList();
        }

        return (results, nextCursor);
    }
}