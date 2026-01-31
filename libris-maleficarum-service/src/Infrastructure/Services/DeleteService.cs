namespace LibrisMaleficarum.Infrastructure.Services;

using LibrisMaleficarum.Domain.Configuration;
using LibrisMaleficarum.Domain.Entities;
using LibrisMaleficarum.Domain.Exceptions;
using LibrisMaleficarum.Domain.Interfaces.Repositories;
using LibrisMaleficarum.Domain.Interfaces.Services;
using Microsoft.Extensions.Options;

/// <summary>
/// Service for orchestrating delete operations with rate limiting and async processing.
/// </summary>
public class DeleteService : IDeleteService
{
    private readonly IWorldEntityRepository _worldEntityRepository;
    private readonly IDeleteOperationRepository _deleteOperationRepository;
    private readonly IUserContextService _userContextService;
    private readonly ITelemetryService _telemetryService;
    private readonly DeleteOperationOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteService"/> class.
    /// </summary>
    /// <param name="worldEntityRepository">The world entity repository.</param>
    /// <param name="deleteOperationRepository">The delete operation repository.</param>
    /// <param name="userContextService">The user context service.</param>
    /// <param name="telemetryService">The telemetry service.</param>
    /// <param name="options">The delete operation options.</param>
    public DeleteService(
        IWorldEntityRepository worldEntityRepository,
        IDeleteOperationRepository deleteOperationRepository,
        IUserContextService userContextService,
        ITelemetryService telemetryService,
        IOptions<DeleteOperationOptions> options)
    {
        _worldEntityRepository = worldEntityRepository ?? throw new ArgumentNullException(nameof(worldEntityRepository));
        _deleteOperationRepository = deleteOperationRepository ?? throw new ArgumentNullException(nameof(deleteOperationRepository));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc/>
    public async Task<DeleteOperation> InitiateDeleteAsync(Guid worldId, Guid entityId, bool cascade = true, CancellationToken cancellationToken = default)
    {
        var currentUserId = await _userContextService.GetCurrentUserIdAsync();
        var userId = currentUserId.ToString();

        // Rate limiting: Check active operations for this user in this world
        var activeCount = await _deleteOperationRepository.CountActiveByUserAsync(worldId, userId, cancellationToken);
        if (activeCount >= _options.MaxConcurrentPerUserPerWorld)
        {
            throw new RateLimitExceededException(activeCount, _options.MaxConcurrentPerUserPerWorld, _options.RetryAfterSeconds);
        }

        // Get the entity to be deleted
        var entity = await _worldEntityRepository.GetByIdAsync(worldId, entityId, cancellationToken);
        if (entity == null)
        {
            throw new EntityNotFoundException(worldId, entityId);
        }

        // Create delete operation
        var operation = DeleteOperation.Create(
            worldId,
            entityId,
            entity.Name,
            userId,
            cascade);

        // Persist the operation
        operation = await _deleteOperationRepository.CreateAsync(operation, cancellationToken);

        // Log telemetry
        using var activity = _telemetryService.StartActivity("DeleteOperationInitiated", new Dictionary<string, object>
        {
            { "operation.id", operation.Id },
            { "entity.id", entityId },
            { "entity.name", entity.Name },
            { "entity.type", entity.EntityType.ToString() },
            { "entity.world_id", worldId },
            { "delete.cascade", cascade },
            { "user.id", userId }
        });

        return operation;
    }

    /// <inheritdoc/>
    public async Task ProcessDeleteAsync(Guid worldId, Guid operationId, CancellationToken cancellationToken = default)
    {
        // Get the operation
        var operation = await _deleteOperationRepository.GetByIdAsync(worldId, operationId, cancellationToken);
        if (operation == null)
        {
            throw new InvalidOperationException($"Delete operation '{operationId}' not found.");
        }

        if (operation.Status != DeleteOperationStatus.Pending)
        {
            // Operation already processed or in progress
            return;
        }

        using var activity = _telemetryService.StartActivity("ProcessDeleteOperation", new Dictionary<string, object>
        {
            { "operation.id", operationId },
            { "operation.root_entity_id", operation.RootEntityId },
            { "operation.cascade", operation.Cascade },
            { "operation.world_id", worldId }
        });

        try
        {
            // T029: Discover all descendants if cascade is enabled
            var descendants = new List<WorldEntity>();
            if (operation.Cascade)
            {
                descendants = (await _worldEntityRepository.GetDescendantsAsync(
                    operation.RootEntityId,
                    worldId,
                    cancellationToken)).ToList();

                activity?.AddTag("descendants_discovered", descendants.Count);
            }

            // Calculate total entities: root + descendants
            var totalEntities = 1 + descendants.Count;

            // Start the operation with actual count
            operation.Start(totalEntities);
            await _deleteOperationRepository.UpdateAsync(operation, cancellationToken);

            // Get current user from operation
            var userId = operation.CreatedBy;

            // Perform the delete (repository handles cascade internally)
            var deletedCount = await _worldEntityRepository.DeleteAsync(
                worldId,
                operation.RootEntityId,
                userId,
                operation.Cascade,
                cancellationToken);

            // Update operation progress and complete
            operation.UpdateProgress(deletedCount, 0);
            operation.Complete();
            await _deleteOperationRepository.UpdateAsync(operation, cancellationToken);

            // T034: Log completion
            activity?.AddTag("operation.deleted_count", deletedCount);
            activity?.AddTag("operation.status", "completed");
            activity?.AddTag("operation.processed_count", deletedCount);
            activity?.AddTag("operation.failed_count", 0);
        }
        catch (Exception ex)
        {
            // Mark operation as failed
            operation.Fail(ex.Message);
            await _deleteOperationRepository.UpdateAsync(operation, cancellationToken);

            // Log failure
            activity?.AddTag("operation.status", "failed");
            activity?.AddTag("operation.error", ex.Message);

            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<DeleteOperation?> GetOperationStatusAsync(Guid worldId, Guid operationId, CancellationToken cancellationToken = default)
    {
        return await _deleteOperationRepository.GetByIdAsync(worldId, operationId, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<DeleteOperation>> ListRecentOperationsAsync(Guid worldId, int limit = 20, CancellationToken cancellationToken = default)
    {
        // Clamp limit to max 100
        limit = Math.Min(limit, 100);

        return await _deleteOperationRepository.GetRecentByWorldAsync(worldId, limit, cancellationToken);
    }
}
