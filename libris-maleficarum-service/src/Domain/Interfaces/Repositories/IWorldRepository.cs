using LibrisMaleficarum.Domain.Entities;

namespace LibrisMaleficarum.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for world aggregate operations.
/// </summary>
public interface IWorldRepository
{
    /// <summary>
    /// Gets a world by its unique identifier.
    /// </summary>
    /// <param name="worldId">The unique identifier of the world.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The world if found; otherwise, null.</returns>
    Task<World?> GetByIdAsync(Guid worldId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all worlds owned by a specific user with optional pagination.
    /// </summary>
    /// <param name="ownerId">The unique identifier of the owner.</param>
    /// <param name="limit">Maximum number of items to return (default 50, max 200).</param>
    /// <param name="cursor">Continuation token for pagination.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the list of worlds and an optional continuation token.</returns>
    Task<(IEnumerable<World> Worlds, string? NextCursor)> GetAllByOwnerAsync(
        Guid ownerId,
        int limit = 50,
        string? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new world.
    /// </summary>
    /// <param name="world">The world to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created world.</returns>
    Task<World> CreateAsync(World world, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing world.
    /// </summary>
    /// <param name="world">The world to update.</param>
    /// <param name="etag">Optional ETag for optimistic concurrency control.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated world.</returns>
    Task<World> UpdateAsync(World world, string? etag = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a world by its unique identifier.
    /// </summary>
    /// <param name="worldId">The unique identifier of the world to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteAsync(Guid worldId, CancellationToken cancellationToken = default);
}
