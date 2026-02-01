namespace LibrisMaleficarum.Infrastructure.Repositories;

using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using LibrisMaleficarum.Domain.ValueObjects;
using LibrisMaleficarum.Infrastructure.Extensions;
using LibrisMaleficarum.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Repository implementation for WorldEntity using EF Core and Cosmos DB.
/// </summary>
public class WorldEntityRepository : IWorldEntityRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IUserContextService _userContextService;
    private readonly IWorldRepository _worldRepository;
    private readonly ITelemetryService _telemetryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorldEntityRepository"/> class.
    /// </summary>
    /// <param name="context">The application database context.</param>
    /// <param name="userContextService">The user context service for authorization.</param>
    /// <param name="worldRepository">The world repository for validation.</param>
    /// <param name="telemetryService">The telemetry service for tracking metrics and traces.</param>
    public WorldEntityRepository(
        ApplicationDbContext context,
        IUserContextService userContextService,
        IWorldRepository worldRepository,
        ITelemetryService telemetryService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _worldRepository = worldRepository ?? throw new ArgumentNullException(nameof(worldRepository));
        _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
    }

    /// <inheritdoc/>
    public async Task<WorldEntity?> GetByIdAsync(Guid worldId, Guid entityId, CancellationToken cancellationToken = default)
    {
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

        // Query with partition key for efficiency
        return await _context.WorldEntities
            .WithPartitionKeyIfCosmos(_context, worldId)
            .Where(e => e.Id == entityId && !e.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<WorldEntity?> GetByIdIncludingDeletedAsync(Guid worldId, Guid entityId, CancellationToken cancellationToken = default)
    {
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

        // Query with partition key for efficiency, INCLUDE soft-deleted entities
        return await _context.WorldEntities
            .WithPartitionKeyIfCosmos(_context, worldId)
            .Where(e => e.Id == entityId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<(IEnumerable<WorldEntity> Entities, string? NextCursor)> GetAllByWorldAsync(
        Guid worldId,
        Guid? parentId = null,
        EntityType? entityType = null,
        List<string>? tags = null,
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
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

        // Clamp limit to valid range
        limit = Math.Clamp(limit, 1, 200);

        // Start with partition-scoped query and filters
        var query = _context.WorldEntities
            .WithPartitionKeyIfCosmos(_context, worldId)
            .Where(e => !e.IsDeleted);

        // Apply parentId filter
        // If parentId is provided (not Guid.Empty), filter by explicit parentId
        // If parentId is null (default), filter for root entities (ParentId == null)
        // Note: To get ALL entities regardless of hierarchy, pass Guid.Empty as parentId
        if (parentId != Guid.Empty)
        {
            query = query.Where(e => e.ParentId == parentId);
        }

        // Apply cursor pagination if provided
        if (!string.IsNullOrWhiteSpace(cursor) && DateTime.TryParse(cursor, null, System.Globalization.DateTimeStyles.RoundtripKind, out var cursorDate))
        {
            query = query.Where(e => e.CreatedDate > cursorDate);
        }

        // Apply entityType filter
        if (entityType.HasValue)
        {
            query = query.Where(e => e.EntityType == entityType.Value);
        }

        // Apply tags filter (case-sensitive partial match)
        // Note: Cosmos DB LINQ provider does not support string case conversion methods like ToLowerInvariant()
        // For case-insensitive search, tags should be stored in lowercase when saving
        if (tags != null && tags.Any())
        {
            foreach (var tag in tags)
            {
                query = query.Where(e => e.Tags.Any(t => t.Contains(tag)));
            }
        }

        // Apply ordering AFTER all filters
        var orderedQuery = query.OrderBy(e => e.CreatedDate).ThenBy(e => e.Id);

        // Fetch limit + 1 to determine if there are more pages
        var entities = await orderedQuery
            .Take(limit + 1)
            .ToListAsync(cancellationToken);

        string? nextCursor = null;
        if (entities.Count > limit)
        {
            var lastEntity = entities[limit - 1];
            entities.RemoveAt(limit);
            nextCursor = lastEntity.CreatedDate.ToString("O"); // ISO 8601 format
        }

        return (entities, nextCursor);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WorldEntity>> GetChildrenAsync(Guid worldId, Guid parentId, CancellationToken cancellationToken = default)
    {
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

        // Query children within partition
        return await _context.WorldEntities
            .WithPartitionKeyIfCosmos(_context, worldId)
            .Where(e => e.ParentId == parentId && !e.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<WorldEntity>> GetDescendantsAsync(Guid entityId, Guid worldId, CancellationToken cancellationToken = default)
    {
        var descendants = new List<WorldEntity>();
        var queue = new Queue<Guid>();
        queue.Enqueue(entityId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();

            // Get all non-deleted children of current entity
            var children = await _context.WorldEntities
                .WithPartitionKeyIfCosmos(_context, worldId)
                .Where(e => e.ParentId == currentId && !e.IsDeleted)
                .ToListAsync(cancellationToken);

            foreach (var child in children)
            {
                descendants.Add(child);
                queue.Enqueue(child.Id); // Add children to queue for recursive traversal
            }
        }

        return descendants;
    }

    /// <inheritdoc/>
    public async Task<WorldEntity> CreateAsync(WorldEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Verify world exists and user owns it
        var currentUserId = await _userContextService.GetCurrentUserIdAsync();
        var world = await _worldRepository.GetByIdAsync(entity.WorldId, cancellationToken);

        if (world == null)
        {
            throw new WorldNotFoundException(entity.WorldId);
        }

        if (world.OwnerId != currentUserId)
        {
            throw new UnauthorizedWorldAccessException(entity.WorldId, currentUserId);
        }

        // Verify parent entity exists if ParentId is specified
        if (entity.ParentId.HasValue)
        {
            var parent = await GetByIdAsync(entity.WorldId, entity.ParentId.Value, cancellationToken);
            if (parent == null)
            {
                throw new EntityNotFoundException(entity.WorldId, entity.ParentId.Value,
                    $"Parent entity with ID '{entity.ParentId.Value}' not found.");
            }

            // Update parent's HasChildren flag if it wasn't set
            if (!parent.HasChildren)
            {
                parent.SetHasChildren(true);
                // Parent is tracked, so this change will be persisted on SaveChangesAsync
            }
        }

        using var activity = _telemetryService.StartActivity("CreateWorldEntity", new Dictionary<string, object>
        {
            { "world.id", entity.WorldId },
            { "entity.id", entity.Id },
            { "entity.name", entity.Name },
            { "entity.type", entity.EntityType.ToString() },
            { "entity.parent_id", entity.ParentId?.ToString() ?? "null" }
        });

        try
        {
            await _context.WorldEntities.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            // Record metric
            _telemetryService.RecordEntityCreated(entity.EntityType.ToString());

            return entity;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task<WorldEntity> UpdateAsync(WorldEntity entity, string? etag = null, CancellationToken cancellationToken = default)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        // Retrieve existing entity for authorization and ETag validation
        var existingEntity = await GetByIdAsync(entity.WorldId, entity.Id, cancellationToken);
        if (existingEntity == null)
        {
            throw new EntityNotFoundException(entity.WorldId, entity.Id);
        }

        // Validate ETag if provided
        if (!string.IsNullOrWhiteSpace(etag))
        {
            var entry = _context.Entry(existingEntity);
            var currentETag = entry.Property("_etag").CurrentValue?.ToString();

            if (currentETag != etag)
            {
                throw new InvalidOperationException(
                    $"ETag mismatch: Expected '{etag}' but current is '{currentETag}'. The entity may have been modified by another request.");
            }
        }

        // Apply changes to the tracked entity
        var entryEntity = _context.Entry(existingEntity);
        entryEntity.CurrentValues.SetValues(entity);

        // Detect ParentId change using OriginalValues
        var oldParentId = entryEntity.OriginalValues.GetValue<Guid?>("ParentId");
        var newParentId = entity.ParentId;

        // Check for circular reference if ParentId is being changed
        if (newParentId.HasValue && newParentId != oldParentId)
        {
            await ValidateNoCircularReferenceAsync(entity.WorldId, entity.Id, newParentId.Value, cancellationToken);
        }

        // Handle Hierarchy updates for HasChildren flag
        if (oldParentId != newParentId)
        {
            // 1. Handle New Parent (increment children)
            if (newParentId.HasValue)
            {
                var newParent = await GetByIdAsync(entity.WorldId, newParentId.Value, cancellationToken);
                if (newParent != null && !newParent.HasChildren)
                {
                    newParent.SetHasChildren(true);
                }
            }

            // 2. Handle Old Parent (decrement children/check empty)
            if (oldParentId.HasValue)
            {
                var oldParent = await GetByIdAsync(entity.WorldId, oldParentId.Value, cancellationToken);
                if (oldParent != null)
                {
                    var remainingChildrenAny = await _context.WorldEntities
                        .WithPartitionKeyIfCosmos(_context, entity.WorldId)
                        .AnyAsync(e => e.ParentId == oldParentId.Value && !e.IsDeleted && e.Id != entity.Id, cancellationToken);

                    if (!remainingChildrenAny && oldParent.HasChildren)
                    {
                        oldParent.SetHasChildren(false);
                    }
                }
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return existingEntity;
    }

    /// <inheritdoc/>
    public async Task<int> DeleteAsync(Guid worldId, Guid entityId, string deletedBy, bool cascade = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
        {
            throw new ArgumentException("DeletedBy is required.", nameof(deletedBy));
        }

        var entity = await GetByIdAsync(worldId, entityId, cancellationToken);
        if (entity == null)
        {
            throw new EntityNotFoundException(worldId, entityId);
        }

        // Check if entity has children
        var children = await GetChildrenAsync(worldId, entityId, cancellationToken);
        var childrenList = children.ToList();

        if (childrenList.Any() && !cascade)
        {
            throw new InvalidOperationException(
                $"Cannot delete entity '{entityId}' because it has {childrenList.Count} child entities. " +
                "Use cascade=true to delete all descendants.");
        }

        int deletedCount = 0;

        using var activity = _telemetryService.StartActivity("DeleteWorldEntity", new Dictionary<string, object>
        {
            { "world.id", worldId },
            { "entity.id", entityId },
            { "entity.name", entity.Name },
            { "entity.type", entity.EntityType.ToString() },
            { "cascade", cascade },
            { "user.id", deletedBy }
        });

        try
        {
            // Recursively delete children if cascade is enabled
            if (cascade && childrenList.Any())
            {
                foreach (var child in childrenList)
                {
                    deletedCount += await DeleteAsync(worldId, child.Id, deletedBy, cascade: true, cancellationToken);
                }
            }

            // Update parent's HasChildren flag if this was the last child (BEFORE soft-deleting this entity)
            if (entity.ParentId.HasValue)
            {
                // Materialize parent ID as local variable
                var parentIdValue = entity.ParentId.Value;

                var parent = await GetByIdAsync(worldId, parentIdValue, cancellationToken);
                if (parent != null)
                {
                    // Load all children and check in-memory to avoid Cosmos DB LINQ translation issues
                    var siblings = await _context.WorldEntities
                        .WithPartitionKeyIfCosmos(_context, worldId)
                        .Where(e => e.ParentId == parentIdValue && !e.IsDeleted)
                        .ToListAsync(cancellationToken);

                    // Check if this is the only remaining child (excluding self)
                    var remainingChildrenExist = siblings.Any(s => s.Id != entityId);

                    if (!remainingChildrenExist && parent.HasChildren)
                    {
                        parent.SetHasChildren(false);
                    }
                }
            }

            // Soft delete the entity (AFTER updating parent)
            entity.SoftDelete(deletedBy);
            deletedCount++;

            await _context.SaveChangesAsync(cancellationToken);

            // Record metric
            _telemetryService.RecordEntityDeleted(entity.EntityType.ToString());
            activity?.AddTag("deleted_count", deletedCount);

            return deletedCount;
        }
        finally
        {
            activity?.Dispose();
        }
    }

    /// <inheritdoc/>
    public async Task<int> CountChildrenAsync(Guid worldId, Guid entityId, CancellationToken cancellationToken = default)
    {
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

        return await _context.WorldEntities
            .WithPartitionKeyIfCosmos(_context, worldId)
            .Where(e => e.ParentId == entityId && !e.IsDeleted)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    /// Validates that setting a parent ID will not create a circular reference.
    /// </summary>
    private async Task ValidateNoCircularReferenceAsync(Guid worldId, Guid entityId, Guid newParentId, CancellationToken cancellationToken)
    {
        // Traverse up the parent chain to ensure we don't encounter the entity itself
        var currentParentId = newParentId;
        var visited = new HashSet<Guid> { entityId };

        while (currentParentId != Guid.Empty)
        {
            if (visited.Contains(currentParentId))
            {
                throw new InvalidOperationException(
                    $"Circular reference detected: Entity '{entityId}' cannot have ancestor '{currentParentId}' as its parent.");
            }

            visited.Add(currentParentId);

            var parent = await _context.WorldEntities
                .WithPartitionKeyIfCosmos(_context, worldId)
                .Where(e => e.Id == currentParentId && !e.IsDeleted)
                .FirstOrDefaultAsync(cancellationToken);

            if (parent == null)
            {
                break;
            }

            currentParentId = parent.ParentId ?? Guid.Empty;
        }
    }
}
