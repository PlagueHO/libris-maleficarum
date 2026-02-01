namespace LibrisMaleficarum.Domain.Entities;

/// <summary>
/// Represents an asynchronous delete operation for tracking progress and status.
/// Stored in Cosmos DB with TTL for automatic cleanup after 24 hours.
/// </summary>
public class DeleteOperation
{
    /// <summary>
    /// Gets the unique identifier for this operation.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets the world identifier this operation belongs to (partition key).
    /// </summary>
    public Guid WorldId { get; private set; }

    /// <summary>
    /// Gets the root entity ID being deleted.
    /// </summary>
    public Guid RootEntityId { get; private set; }

    /// <summary>
    /// Gets the name of the root entity (for display purposes).
    /// </summary>
    public string RootEntityName { get; private set; }

    /// <summary>
    /// Gets the current status of the operation.
    /// </summary>
    public DeleteOperationStatus Status { get; private set; }

    /// <summary>
    /// Gets the total number of entities to delete (including root and all descendants).
    /// </summary>
    public int TotalEntities { get; private set; }

    /// <summary>
    /// Gets the number of entities successfully deleted so far.
    /// </summary>
    public int DeletedCount { get; private set; }

    /// <summary>
    /// Gets the number of entities that failed to delete.
    /// </summary>
    public int FailedCount { get; private set; }

    /// <summary>
    /// Gets the list of entity IDs that failed to delete (for retry/debugging).
    /// </summary>
    public List<Guid> FailedEntityIds { get; private set; }

    /// <summary>
    /// Gets error details if the operation failed.
    /// </summary>
    public string? ErrorDetails { get; private set; }

    /// <summary>
    /// Gets the user ID who initiated this operation.
    /// </summary>
    public string CreatedBy { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this operation was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this operation started processing.
    /// </summary>
    public DateTime? StartedAt { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp when this operation completed (success or failure).
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// Gets whether cascade delete is enabled for this operation.
    /// </summary>
    public bool Cascade { get; private set; }

    /// <summary>
    /// Gets the TTL in seconds for automatic cleanup (default: 24 hours = 86400 seconds).
    /// </summary>
    public int Ttl { get; private set; } = 86400;

    /// <summary>
    /// Private constructor for EF Core.
    /// </summary>
    private DeleteOperation()
    {
        RootEntityName = string.Empty;
        CreatedBy = string.Empty;
        FailedEntityIds = [];
    }

    /// <summary>
    /// Creates a new delete operation.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="rootEntityId">The root entity ID being deleted.</param>
    /// <param name="rootEntityName">The name of the root entity.</param>
    /// <param name="createdBy">The user ID who initiated the operation.</param>
    /// <param name="cascade">Whether cascade delete is enabled.</param>
    /// <param name="ttlSeconds">TTL in seconds for automatic cleanup (default: 24 hours = 86400 seconds).</param>
    /// <returns>A new DeleteOperation instance.</returns>
    public static DeleteOperation Create(
        Guid worldId,
        Guid rootEntityId,
        string rootEntityName,
        string createdBy,
        bool cascade = true,
        int ttlSeconds = 86400)
    {
        if (string.IsNullOrWhiteSpace(rootEntityName))
        {
            throw new ArgumentException("Root entity name is required.", nameof(rootEntityName));
        }

        if (string.IsNullOrWhiteSpace(createdBy))
        {
            throw new ArgumentException("CreatedBy is required.", nameof(createdBy));
        }

        return new DeleteOperation
        {
            Id = Guid.NewGuid(),
            WorldId = worldId,
            RootEntityId = rootEntityId,
            RootEntityName = rootEntityName,
            Status = DeleteOperationStatus.Pending,
            TotalEntities = 0, // Updated when processing starts
            DeletedCount = 0,
            FailedCount = 0,
            FailedEntityIds = [],
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            Cascade = cascade,
            Ttl = ttlSeconds
        };
    }

    /// <summary>
    /// Marks the operation as in progress with the total entity count.
    /// </summary>
    /// <param name="totalEntities">The total number of entities to delete.</param>
    public void Start(int totalEntities)
    {
        if (totalEntities < 0)
        {
            throw new ArgumentException("Total entities must be non-negative.", nameof(totalEntities));
        }

        Status = DeleteOperationStatus.InProgress;
        TotalEntities = totalEntities;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates progress after deleting entities.
    /// </summary>
    /// <param name="deletedCount">The number of entities successfully deleted.</param>
    /// <param name="failedCount">The number of entities that failed to delete.</param>
    /// <param name="failedIds">Optional list of entity IDs that failed.</param>
    public void UpdateProgress(int deletedCount, int failedCount, List<Guid>? failedIds = null)
    {
        if (deletedCount < 0)
        {
            throw new ArgumentException("Deleted count must be non-negative.", nameof(deletedCount));
        }

        if (failedCount < 0)
        {
            throw new ArgumentException("Failed count must be non-negative.", nameof(failedCount));
        }

        DeletedCount = deletedCount;
        FailedCount = failedCount;
        if (failedIds != null)
        {
            FailedEntityIds = failedIds;
        }
    }

    /// <summary>
    /// Adds a failed entity ID to the list (ensures no duplicates).
    /// </summary>
    /// <param name="entityId">The entity ID that failed to delete.</param>
    public void AddFailedEntity(Guid entityId)
    {
        if (!FailedEntityIds.Contains(entityId))
        {
            FailedEntityIds.Add(entityId);
            FailedCount = FailedEntityIds.Count;
        }
    }

    /// <summary>
    /// Gets whether this operation has any failed entities.
    /// </summary>
    public bool HasFailures => FailedEntityIds.Count > 0;

    /// <summary>
    /// Marks the operation as completed successfully.
    /// Determines final status based on failures: Completed, Partial, or Failed.
    /// </summary>
    public void Complete()
    {
        // Determine status based on failure count
        if (TotalEntities == 0 || (DeletedCount == 0 && FailedCount == 0))
        {
            // Idempotent case or empty operation
            Status = DeleteOperationStatus.Completed;
        }
        else if (FailedCount == TotalEntities)
        {
            // All entities failed
            Status = DeleteOperationStatus.Failed;
        }
        else if (FailedCount > 0)
        {
            // Some failed
            Status = DeleteOperationStatus.Partial;
        }
        else
        {
            // All succeeded
            Status = DeleteOperationStatus.Completed;
        }

        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the operation as failed with error details.
    /// </summary>
    /// <param name="errorDetails">The error message or details.</param>
    public void Fail(string errorDetails)
    {
        if (string.IsNullOrWhiteSpace(errorDetails))
        {
            throw new ArgumentException("Error details are required.", nameof(errorDetails));
        }

        Status = DeleteOperationStatus.Failed;
        ErrorDetails = errorDetails;
        CompletedAt = DateTime.UtcNow;
    }
}
