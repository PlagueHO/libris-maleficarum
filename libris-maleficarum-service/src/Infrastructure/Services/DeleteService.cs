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

        // Get the entity including soft-deleted ones to distinguish between "doesn't exist" vs "already deleted"
        var entity = await _worldEntityRepository.GetByIdIncludingDeletedAsync(worldId, entityId, cancellationToken);
        if (entity == null)
        {
            // Entity truly doesn't exist
            throw new EntityNotFoundException(worldId, entityId);
        }

        // Create delete operation
        var operation = DeleteOperation.Create(
            worldId,
            entityId,
            entity.Name,
            userId,
            cascade,
            ttlSeconds: _options.OperationTtlHours * 3600);

        // If entity is already deleted, create an operation that completes immediately with count=0 (idempotent)
        if (entity.IsDeleted)
        {
            operation.Start(totalEntities: 0);
            operation.Complete();
        }

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
            { "delete.already_deleted", entity.IsDeleted },
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

        if (operation.Status != DeleteOperationStatus.Pending && operation.Status != DeleteOperationStatus.InProgress)
        {
            // Operation already completed, failed, or partial
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

            // Only start the operation if it's new (Pending); preserve existing progress if resuming (InProgress)
            if (operation.Status == DeleteOperationStatus.Pending)
            {
                operation.Start(totalEntities);
                await _deleteOperationRepository.UpdateAsync(operation, cancellationToken);
            }

            // Get current user from operation
            var userId = operation.CreatedBy;

            // T041: Process descendants in batches with real-time progress updates
            var configuredBatchSize = _options?.MaxBatchSize ?? 10;
            var batchSize = Math.Clamp(configuredBatchSize, 1, 1000);

            // Initialize processedCount from existing progress when resuming (InProgress), or 0 for new operations (Pending)
            var processedCount = operation.DeletedCount;

            // Process descendants first (deepest to shallowest for referential integrity)
            // Order by depth descending (children before parents)
            var orderedDescendants = descendants.OrderByDescending(d => d.Depth).ToList();

            for (var i = 0; i < orderedDescendants.Count; i += batchSize)
            {
                var batch = orderedDescendants.Skip(i).Take(batchSize);

                foreach (var entity in batch)
                {
                    try
                    {
                        // Skip if already deleted (idempotency for checkpoint resume)
                        // Don't increment processedCount for already-deleted entities to avoid double-counting on resume
                        if (entity.IsDeleted)
                        {
                            continue;
                        }

                        // Soft delete the entity
                        entity.SoftDelete(userId);
                        await _worldEntityRepository.UpdateAsync(entity, cancellationToken: cancellationToken);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        // Log the failure and add to failed list
                        operation.AddFailedEntity(entity.Id);
                        activity?.AddTag($"entity.{entity.Id}.error", ex.Message);
                    }
                }

                // Update progress after each batch
                operation.UpdateProgress(processedCount, operation.FailedCount);
                await _deleteOperationRepository.UpdateAsync(operation, cancellationToken);
            }

            // Delete root entity last
            try
            {
                var rootEntity = await _worldEntityRepository.GetByIdAsync(worldId, operation.RootEntityId, cancellationToken);
                if (rootEntity != null && !rootEntity.IsDeleted)
                {
                    rootEntity.SoftDelete(userId);
                    await _worldEntityRepository.UpdateAsync(rootEntity, cancellationToken: cancellationToken);
                    processedCount++;
                }
                // If already deleted, skip without incrementing (idempotency for checkpoint resume)
            }
            catch (Exception ex)
            {
                // Log root entity failure
                operation.AddFailedEntity(operation.RootEntityId);
                activity?.AddTag("root_entity.error", ex.Message);
            }

            // Final progress update and complete
            operation.UpdateProgress(processedCount, operation.FailedCount);
            operation.Complete();
            await _deleteOperationRepository.UpdateAsync(operation, cancellationToken);

            // T034: Log completion
            activity?.AddTag("operation.deleted_count", processedCount);
            activity?.AddTag("operation.status", operation.Status.ToString().ToLowerInvariant());
            activity?.AddTag("operation.processed_count", processedCount);
            activity?.AddTag("operation.failed_count", operation.FailedCount);
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
