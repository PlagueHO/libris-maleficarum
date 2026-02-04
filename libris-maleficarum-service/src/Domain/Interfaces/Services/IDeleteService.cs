namespace LibrisMaleficarum.Domain.Interfaces.Services;

using LibrisMaleficarum.Domain.Entities;

/// <summary>
/// Service for orchestrating delete operations.
/// </summary>
public interface IDeleteService
{
    /// <summary>
    /// Initiates a delete operation for an entity.
    /// Checks rate limit (max 5 concurrent operations per user per world) before creating operation.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="entityId">The entity to delete.</param>
    /// <param name="cascade">Whether to cascade delete to descendants (default: true).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created delete operation.</returns>
    /// <exception cref="Exceptions.RateLimitExceededException">Thrown when user has 5+ active operations in this world.</exception>
    /// <exception cref="Exceptions.EntityNotFoundException">Thrown when entity does not exist.</exception>
    Task<DeleteOperation> InitiateDeleteAsync(Guid worldId, Guid entityId, bool cascade = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a pending delete operation (called by background processor).
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessDeleteAsync(Guid worldId, Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the status of a delete operation.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The delete operation if found, otherwise null.</returns>
    Task<DeleteOperation?> GetOperationStatusAsync(Guid worldId, Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists recent delete operations for a world.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="limit">Maximum number of operations to return (default: 20, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recent delete operations.</returns>
    Task<IEnumerable<DeleteOperation>> ListRecentOperationsAsync(Guid worldId, int limit = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retries a failed delete operation.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="operationId">The operation identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated delete operation.</returns>
    /// <exception cref="Exceptions.EntityNotFoundException">Thrown when operation does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when operation is not in Failed or Partial status.</exception>
    Task<DeleteOperation> RetryDeleteOperationAsync(Guid worldId, Guid operationId, CancellationToken cancellationToken = default);
}
