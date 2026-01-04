namespace LibrisMaleficarum.Domain.Interfaces.Repositories;

using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.ValueObjects;

/// <summary>
/// Repository interface for managing WorldEntity persistence operations.
/// </summary>
public interface IWorldEntityRepository
{
    /// <summary>
    /// Retrieves an entity by its unique identifier within a world partition.
    /// </summary>
    /// <param name="worldId">The world identifier (partition key).</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entity if found and not deleted; otherwise null.</returns>
    Task<WorldEntity?> GetByIdAsync(Guid worldId, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all entities in a world with optional filtering by type and tags.
    /// </summary>
    /// <param name="worldId">The world identifier (partition key).</param>
    /// <param name="entityType">Optional entity type filter.</param>
    /// <param name="tags">Optional tags filter (case-insensitive partial match).</param>
    /// <param name="limit">Maximum number of items to return (clamped to 1-200, default 50).</param>
    /// <param name="cursor">Continuation cursor from previous response.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tuple of entities and next cursor (null if no more pages).</returns>
    Task<(IEnumerable<WorldEntity> Entities, string? NextCursor)> GetAllByWorldAsync(
        Guid worldId,
        EntityType? entityType = null,
        List<string>? tags = null,
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all child entities of a parent entity.
    /// </summary>
    /// <param name="worldId">The world identifier (partition key).</param>
    /// <param name="parentId">The parent entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Collection of child entities.</returns>
    Task<IEnumerable<WorldEntity>> GetChildrenAsync(Guid worldId, Guid parentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new entity in the repository.
    /// </summary>
    /// <param name="entity">The entity to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created entity.</returns>
    Task<WorldEntity> CreateAsync(WorldEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity with optimistic concurrency control.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="etag">Optional ETag for concurrency validation (from If-Match header).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated entity.</returns>
    /// <exception cref="InvalidOperationException">Thrown when ETag validation fails (409 Conflict).</exception>
    Task<WorldEntity> UpdateAsync(WorldEntity entity, string? etag = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes an entity. If the entity has children, behavior depends on cascade parameter.
    /// </summary>
    /// <param name="worldId">The world identifier (partition key).</param>
    /// <param name="entityId">The entity identifier to delete.</param>
    /// <param name="cascade">If true, recursively soft-delete all descendants; if false and children exist, throw InvalidOperationException.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when cascade=false and entity has children.</exception>
    Task DeleteAsync(Guid worldId, Guid entityId, bool cascade = false, CancellationToken cancellationToken = default);
}
