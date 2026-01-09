using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Infrastructure.Extensions;
using LibrisMaleficarum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LibrisMaleficarum.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for world aggregate operations using EF Core Cosmos provider.
/// </summary>
public class WorldRepository : IWorldRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IUserContextService _userContextService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userContextService">The user context service.</param>
    public WorldRepository(ApplicationDbContext context, IUserContextService userContextService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
    }

    /// <inheritdoc />
    public async Task<World?> GetByIdAsync(Guid worldId, CancellationToken cancellationToken = default)
    {
        var currentUserId = await _userContextService.GetCurrentUserIdAsync();

        var world = await _context.Worlds
            .WithPartitionKeyIfCosmos(_context, worldId)
            .FirstOrDefaultAsync(w => w.Id == worldId && !w.IsDeleted, cancellationToken);

        if (world is not null && world.OwnerId != currentUserId)
        {
            throw new UnauthorizedWorldAccessException(worldId, currentUserId);
        }

        return world;
    }

    /// <inheritdoc />
    public async Task<(IEnumerable<World> Worlds, string? NextCursor)> GetAllByOwnerAsync(
        Guid ownerId,
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        // Validate and cap limit
        limit = Math.Clamp(limit, 1, 200);

        // Build query with all filters first
        IQueryable<World> query = _context.Worlds
            .Where(w => w.OwnerId == ownerId && !w.IsDeleted);

        // Apply cursor-based pagination if provided
        if (!string.IsNullOrEmpty(cursor) && DateTime.TryParse(cursor, null, System.Globalization.DateTimeStyles.RoundtripKind, out var cursorDate))
        {
            query = query.Where(w => w.CreatedDate > cursorDate);
        }

        // Apply ordering AFTER all filters
        var orderedQuery = query.OrderBy(w => w.CreatedDate);

        // Fetch one extra item to determine if there are more results
        var worlds = await orderedQuery
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        string? nextCursor = null;
        if (worlds.Count > limit)
        {
            // Remove the extra item and set the next cursor
            var lastItem = worlds[limit - 1];
            worlds.RemoveAt(limit);
            nextCursor = lastItem.CreatedDate.ToString("O"); // ISO 8601 format
        }

        return (worlds, nextCursor);
    }

    /// <inheritdoc />
    public async Task<World> CreateAsync(World world, CancellationToken cancellationToken = default)
    {
        var currentUserId = await _userContextService.GetCurrentUserIdAsync();

        // Create new world with current user as owner
        var newWorld = World.Create(currentUserId, world.Name, world.Description);

        await _context.Worlds.AddAsync(newWorld, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return newWorld;
    }

    /// <inheritdoc />
    public async Task<World> UpdateAsync(World world, string? etag = null, CancellationToken cancellationToken = default)
    {
        var currentUserId = await _userContextService.GetCurrentUserIdAsync();

        // Retrieve existing world
        var existingWorld = await _context.Worlds
            .WithPartitionKeyIfCosmos(_context, world.Id)
            .FirstOrDefaultAsync(w => w.Id == world.Id && !w.IsDeleted, cancellationToken);

        if (existingWorld is null)
        {
            throw new WorldNotFoundException(world.Id);
        }

        // Authorization check
        if (existingWorld.OwnerId != currentUserId)
        {
            throw new UnauthorizedWorldAccessException(world.Id, currentUserId);
        }

        // ETag validation for optimistic concurrency
        if (!string.IsNullOrEmpty(etag))
        {
            var entry = _context.Entry(existingWorld);
            var currentETag = entry.Property("_etag").CurrentValue?.ToString();

            if (currentETag != etag)
            {
                throw new InvalidOperationException($"ETag mismatch. The world has been modified by another user. Expected: {etag}, Current: {currentETag}");
            }
        }

        // Update properties
        existingWorld.Update(world.Name, world.Description);

        await _context.SaveChangesAsync(cancellationToken);

        return existingWorld;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid worldId, CancellationToken cancellationToken = default)
    {
        var currentUserId = await _userContextService.GetCurrentUserIdAsync();

        var world = await _context.Worlds
            .WithPartitionKeyIfCosmos(_context, worldId)
            .FirstOrDefaultAsync(w => w.Id == worldId && !w.IsDeleted, cancellationToken);

        if (world is null)
        {
            throw new WorldNotFoundException(worldId);
        }

        // Authorization check
        if (world.OwnerId != currentUserId)
        {
            throw new UnauthorizedWorldAccessException(worldId, currentUserId);
        }

        // Soft delete
        world.SoftDelete();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
