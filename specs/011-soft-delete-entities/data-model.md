# Data Model: Soft Delete World Entities API

**Feature**: 011-soft-delete-entities  
**Date**: 2026-01-31

## Entity Changes

### WorldEntity (MODIFY)

The existing `WorldEntity` class requires two new properties to track deletion metadata.

**Current State**:

```csharp
public bool IsDeleted { get; private set; }
// No DeletedDate or DeletedBy
```

**New State**:

```csharp
/// <summary>
/// Gets a value indicating whether this entity has been soft-deleted.
/// </summary>
public bool IsDeleted { get; private set; }

/// <summary>
/// Gets the UTC timestamp when this entity was soft-deleted.
/// Null if the entity has not been deleted.
/// </summary>
public DateTime? DeletedDate { get; private set; }

/// <summary>
/// Gets the user ID who soft-deleted this entity.
/// Null if the entity has not been deleted.
/// </summary>
public string? DeletedBy { get; private set; }

/// <summary>
/// Gets the time-to-live in seconds for automatic deletion by Cosmos DB.
/// Null means the item doesn't expire (uses container default behavior).
/// Set to 7776000 (90 days) when soft-deleted for automatic purge.
/// </summary>
public int? Ttl { get; private set; }
```

### SoftDelete Method Enhancement

**Current Implementation**:

```csharp
public void SoftDelete()
{
    IsDeleted = true;
    ModifiedDate = DateTime.UtcNow;
}
```

**New Implementation**:

```csharp
/// <summary>
/// Marks this entity as soft-deleted with audit metadata.
/// Sets TTL to 90 days (7776000 seconds) for automatic Cosmos DB cleanup.
/// </summary>
/// <param name="deletedBy">The user ID performing the deletion.</param>
public void SoftDelete(string deletedBy)
{
    if (string.IsNullOrWhiteSpace(deletedBy))
    {
        throw new ArgumentException("DeletedBy is required.", nameof(deletedBy));
    }

    IsDeleted = true;
    DeletedDate = DateTime.UtcNow;
    DeletedBy = deletedBy;
    Ttl = 7776000; // 90 days in seconds (90 * 24 * 60 * 60)
    ModifiedDate = DateTime.UtcNow;
}

/// <summary>
/// Restores a soft-deleted entity by clearing deletion metadata and TTL.
/// </summary>
public void Restore()
{
    IsDeleted = false;
    DeletedDate = null;
    DeletedBy = null;
    Ttl = null; // Remove TTL to prevent auto-deletion
    ModifiedDate = DateTime.UtcNow;
}
```

## New Interfaces

### ICascadeDeleteService

New service interface to encapsulate cascade delete logic and threshold detection.

```csharp
namespace LibrisMaleficarum.Domain.Interfaces.Services;

/// <summary>
/// Service for managing cascade delete operations.
/// </summary>
public interface ICascadeDeleteService
{
    /// <summary>
    /// Determines whether a cascade delete should be processed asynchronously.
    /// </summary>
    /// <param name="entity">The entity being deleted.</param>
    /// <param name="directChildrenCount">The number of direct children.</param>
    /// <returns>True if async processing is recommended; false for sync.</returns>
    bool ShouldProcessAsync(WorldEntity entity, int directChildrenCount);

    /// <summary>
    /// Gets the count of direct children for an entity.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The count of direct children (non-deleted).</returns>
    Task<int> GetDirectChildrenCountAsync(Guid worldId, Guid entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queues an entity for async cascade delete processing.
    /// </summary>
    /// <param name="worldId">The world identifier.</param>
    /// <param name="entityId">The entity identifier.</param>
    /// <param name="deletedBy">The user performing the deletion.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task QueueCascadeDeleteAsync(Guid worldId, Guid entityId, string deletedBy, CancellationToken cancellationToken = default);
}
```

## Configuration

### CascadeDeleteOptions

Configuration class for tuning cascade behavior.

```csharp
namespace LibrisMaleficarum.Domain.Configuration;

/// <summary>
/// Configuration options for cascade delete behavior.
/// </summary>
public class CascadeDeleteOptions
{
    /// <summary>
    /// Section name in configuration file.
    /// </summary>
    public const string SectionName = "CascadeDelete";

    /// <summary>
    /// Maximum number of direct children before switching to async cascade.
    /// Default: 10
    /// </summary>
    public int DirectChildrenThreshold { get; set; } = 10;

    /// <summary>
    /// Minimum entity depth to use sync cascade. Entities closer to root (lower depth)
    /// are more likely to have large subtrees and should use async.
    /// Default: 3 (entities at depth 0, 1, 2 use async)
    /// </summary>
    public int MinSyncDepth { get; set; } = 3;

    /// <summary>
    /// Maximum entities to process in a single sync cascade batch.
    /// If cascade discovers more entities, it switches to async.
    /// Default: 50
    /// </summary>
    public int MaxSyncBatchSize { get; set; } = 50;

    /// <summary>
    /// Rate limit for async cascade processing (entities per second).
    /// Default: 50
    /// </summary>
    public int AsyncRateLimitPerSecond { get; set; } = 50;
}
```

### appsettings.json

```json
{
  "CascadeDelete": {
    "DirectChildrenThreshold": 10,
    "MinSyncDepth": 3,
    "MaxSyncBatchSize": 50,
    "AsyncRateLimitPerSecond": 50
  }
}
```

## Repository Interface Changes

### IWorldEntityRepository

```csharp
/// <summary>
/// Soft-deletes an entity with optional cascade to descendants.
/// </summary>
/// <param name="worldId">The world identifier (partition key).</param>
/// <param name="entityId">The entity identifier to delete.</param>
/// <param name="deletedBy">The user ID performing the deletion.</param>
/// <param name="cascade">If true, recursively soft-delete all descendants; if false and children exist, throw InvalidOperationException.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The number of entities deleted (including descendants if cascade).</returns>
/// <exception cref="InvalidOperationException">Thrown when cascade=false and entity has children.</exception>
Task<int> DeleteAsync(Guid worldId, Guid entityId, string deletedBy, bool cascade = false, CancellationToken cancellationToken = default);

/// <summary>
/// Counts the direct children of an entity.
/// </summary>
/// <param name="worldId">The world identifier.</param>
/// <param name="entityId">The entity identifier.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>The count of direct children (non-deleted).</returns>
Task<int> CountChildrenAsync(Guid worldId, Guid entityId, CancellationToken cancellationToken = default);
```

## Telemetry Events

### Delete Event Structure

```csharp
// Activity tags for delete operations
var tags = new Dictionary<string, object>
{
    { "entity.id", entityId.ToString() },
    { "entity.world_id", worldId.ToString() },
    { "entity.name", entity.Name },
    { "entity.type", entity.EntityType.ToString() },
    { "entity.depth", entity.Depth },
    { "delete.cascade", cascade },
    { "delete.cascade_count", cascadeCount },
    { "delete.is_async", isAsync },
    { "delete.trigger", trigger }, // "api" | "change_feed"
    { "user.id", deletedBy }
};
```

### New Telemetry Methods

```csharp
/// <summary>
/// Records a cascade delete operation with detailed metrics.
/// </summary>
/// <param name="worldId">The world identifier.</param>
/// <param name="entityId">The root entity identifier.</param>
/// <param name="cascadeCount">Total entities deleted in cascade.</param>
/// <param name="isAsync">Whether this was async processing.</param>
/// <param name="durationMs">Duration of the operation in milliseconds.</param>
void RecordCascadeDelete(Guid worldId, Guid entityId, int cascadeCount, bool isAsync, long durationMs);
```

## Database Schema (Cosmos DB)

No container changes required. The WorldEntity container already supports the schema. New properties are added to documents on write:

```json
{
  "id": "entity-guid",
  "WorldId": "world-guid",
  "ParentId": "parent-guid",
  "Name": "Entity Name",
  "EntityType": "Character",
  "IsDeleted": true,
  "DeletedDate": "2026-01-31T12:00:00.000Z",
  "DeletedBy": "user-123",
  "ttl": 7776000,
  "ModifiedDate": "2026-01-31T12:00:00.000Z",
  // ... other fields
}
```

EF Core will automatically serialize the new properties. No migration needed for Cosmos DB.

**TTL Behavior**: 
- When `Ttl` is null in C#, the property is omitted from JSON (not serialized as `"ttl\": null`)
- When `Ttl = 7776000`, Cosmos DB automatically deletes the document 90 days after the `_ts` (last modified) timestamp
- Container must have `DefaultTimeToLive = -1` configured to enable item-level TTL

## Infrastructure Configuration

### Cosmos DB Container TTL Setup

For the TTL functionality to work, the WorldEntity container must be configured with container-level TTL enabled. This is an infrastructure setting, not application code:

**Via Azure Portal**:
1. Navigate to Container Settings
2. Set "Time to Live": On (no default)
3. Leave "Default TTL" blank or set to -1

**Via Bicep/ARM Template**:
```bicep
resource worldEntityContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2024-05-15' = {
  name: 'WorldEntities'
  properties: {
    resource: {
      id: 'WorldEntities'
      partitionKey: {
        paths: ['/WorldId']
        kind: 'Hash'
      }
      defaultTtl: -1  // Enable item-level TTL (documents without ttl field never expire)
    }
  }
}
```

**TTL Settings Explained**:
- `defaultTtl: -1` = Item-level TTL enabled; documents without `ttl` field never expire
- `defaultTtl: <positive number>` = Default expiration in seconds for documents without `ttl` field
- `defaultTtl: null` or omitted = TTL disabled; all documents persist indefinitely regardless of `ttl` field

**Note**: This infrastructure configuration is typically handled in the `infra/` folder Bicep templates, not in the application code.

## New Entity: DeleteOperation

Tracks the status of asynchronous delete operations for frontend polling.

### DeleteOperation Schema

```csharp
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

    private DeleteOperation()
    {
        RootEntityName = string.Empty;
        CreatedBy = string.Empty;
        FailedEntityIds = [];
    }

    /// <summary>
    /// Creates a new delete operation.
    /// </summary>
    public static DeleteOperation Create(
        Guid worldId,
        Guid rootEntityId,
        string rootEntityName,
        string createdBy,
        bool cascade = true)
    {
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
            Ttl = 86400 // 24 hours
        };
    }

    /// <summary>
    /// Marks the operation as in progress with the total entity count.
    /// </summary>
    public void Start(int totalEntities)
    {
        Status = DeleteOperationStatus.InProgress;
        TotalEntities = totalEntities;
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates progress after deleting entities.
    /// </summary>
    public void UpdateProgress(int deletedCount, int failedCount, List<Guid>? failedIds = null)
    {
        DeletedCount = deletedCount;
        FailedCount = failedCount;
        if (failedIds != null)
        {
            FailedEntityIds = failedIds;
        }
    }

    /// <summary>
    /// Marks the operation as completed successfully.
    /// </summary>
    public void Complete()
    {
        Status = FailedCount > 0 ? DeleteOperationStatus.Partial : DeleteOperationStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks the operation as failed with error details.
    /// </summary>
    public void Fail(string errorDetails)
    {
        Status = DeleteOperationStatus.Failed;
        ErrorDetails = errorDetails;
        CompletedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Status of a delete operation.
/// </summary>
public enum DeleteOperationStatus
{
    /// <summary>Operation created but not yet started.</summary>
    Pending,

    /// <summary>Operation is currently processing entities.</summary>
    InProgress,

    /// <summary>Operation completed successfully (all entities deleted).</summary>
    Completed,

    /// <summary>Operation completed with some failures (partial success).</summary>
    Partial,

    /// <summary>Operation failed completely.</summary>
    Failed
}
```

### DeleteOperation Container

Store in the WorldEntity container (same partition key) or a dedicated DeleteOperation container:

| Option | Pros | Cons |
|--------|------|------|
| Same container | Simpler infrastructure, same RU pool | Different TTL behavior needed |
| Separate container | Cleaner separation, dedicated TTL policy | Extra container to manage |

**Recommendation**: Store in **same WorldEntity container** with discriminator field `_type: "DeleteOperation"`. TTL is per-document in Cosmos DB, so 24-hour TTL works independently.

```json
{
  "id": "operation-guid",
  "_type": "DeleteOperation",
  "WorldId": "world-guid",
  "RootEntityId": "entity-guid",
  "RootEntityName": "Continent Arcanis",
  "Status": "InProgress",
  "TotalEntities": 150,
  "DeletedCount": 75,
  "FailedCount": 0,
  "FailedEntityIds": [],
  "CreatedBy": "user-123",
  "CreatedAt": "2026-01-31T12:00:00.000Z",
  "StartedAt": "2026-01-31T12:00:00.100Z",
  "CompletedAt": null,
  "Cascade": true,
  "ttl": 86400
}
```

## Repository Interfaces

### IDeleteOperationRepository

```csharp
namespace LibrisMaleficarum.Domain.Interfaces.Repositories;

/// <summary>
/// Repository interface for managing DeleteOperation persistence.
/// </summary>
public interface IDeleteOperationRepository
{
    /// <summary>
    /// Creates a new delete operation.
    /// </summary>
    Task<DeleteOperation> CreateAsync(DeleteOperation operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a delete operation by ID.
    /// </summary>
    Task<DeleteOperation?> GetByIdAsync(Guid worldId, Guid operationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a delete operation (progress, status).
    /// </summary>
    Task<DeleteOperation> UpdateAsync(DeleteOperation operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets recent delete operations for a world (last 24 hours).
    /// </summary>
    Task<IEnumerable<DeleteOperation>> GetRecentByWorldAsync(Guid worldId, int limit = 20, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts active (Pending or InProgress) delete operations for a user in a world.
    /// Used for rate limiting (max 5 concurrent operations per user per world).
    /// </summary>
    Task<int> CountActiveByUserAsync(Guid worldId, string userId, CancellationToken cancellationToken = default);
}
```

### IDeleteService

```csharp
namespace LibrisMaleficarum.Domain.Interfaces.Services;

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
    /// <param name="cascade">Whether to cascade delete to descendants.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created delete operation.</returns>
    /// <exception cref="RateLimitExceededException">Thrown when user has 5+ active operations in this world.</exception>
    Task<DeleteOperation> InitiateDeleteAsync(Guid worldId, Guid entityId, bool cascade = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a pending delete operation (called by background processor).
    /// </summary>
    Task ProcessDeleteAsync(Guid worldId, Guid operationId, CancellationToken cancellationToken = default);
}
```

## Domain Exceptions

### RateLimitExceededException

```csharp
namespace LibrisMaleficarum.Domain.Exceptions;

/// <summary>
/// Thrown when a user exceeds the rate limit for delete operations.
/// </summary>
public class RateLimitExceededException : Exception
{
    /// <summary>
    /// Gets the number of seconds to wait before retrying.
    /// </summary>
    public int RetryAfterSeconds { get; }

    /// <summary>
    /// Gets the current number of active operations.
    /// </summary>
    public int ActiveOperationCount { get; }

    /// <summary>
    /// Gets the maximum allowed concurrent operations.
    /// </summary>
    public int MaxConcurrentOperations { get; }

    public RateLimitExceededException(int activeCount, int maxConcurrent, int retryAfterSeconds = 30)
        : base($"Rate limit exceeded: {activeCount}/{maxConcurrent} active delete operations. Retry after {retryAfterSeconds} seconds.")
    {
        ActiveOperationCount = activeCount;
        MaxConcurrentOperations = maxConcurrent;
        RetryAfterSeconds = retryAfterSeconds;
    }
}
```
