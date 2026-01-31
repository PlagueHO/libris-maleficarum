namespace LibrisMaleficarum.Domain.Interfaces.Repositories;

using LibrisMaleficarum.Domain.Entities;

/// <summary>
/// Repository interface for managing DeleteOperation persistence.
/// </summary>
public interface IDeleteOperationRepository
{
    /// <summary>
    /// Creates a new delete operation.
    /// </summary>
    /// <param name="operation">The delete operation to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created delete operation.</returns>
    Task<DeleteOperation> CreateAsync(DeleteOperation operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a delete operation by ID.
    /// </summary>
    /// <param name="worldId">The world identifier (partition key).</param>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The delete operation if found, otherwise null.</returns>
    Task<DeleteOperation?> GetByIdAsync(Guid worldId, Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a delete operation (progress, status).
    /// </summary>
    /// <param name="operation">The delete operation to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated delete operation.</returns>
    Task<DeleteOperation> UpdateAsync(DeleteOperation operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent delete operations for a world (last 24 hours).
    /// </summary>
    /// <param name="worldId">The world identifier (partition key).</param>
    /// <param name="limit">Maximum number of operations to return (default: 20).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recent delete operations.</returns>
    Task<IEnumerable<DeleteOperation>> GetRecentByWorldAsync(Guid worldId, int limit = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts active (Pending or InProgress) delete operations for a user in a world.
    /// Used for rate limiting (max 5 concurrent operations per user per world).
    /// </summary>
    /// <param name="worldId">The world identifier (partition key).</param>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of active operations.</returns>
    Task<int> CountActiveByUserAsync(Guid worldId, string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all pending operations across all worlds (for background processor).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of pending operations.</returns>
    Task<IEnumerable<DeleteOperation>> GetPendingOperationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all in-progress operations across all worlds (for checkpoint resume).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of in-progress operations.</returns>
    Task<IEnumerable<DeleteOperation>> GetInProgressOperationsAsync(CancellationToken cancellationToken = default);
}
