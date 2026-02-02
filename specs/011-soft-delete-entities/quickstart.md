# Quickstart: Soft Delete World Entities API

**Feature**: 011-soft-delete-entities  
**Date**: 2026-01-31 (Updated for all-async approach)

## Prerequisites

- .NET 10 SDK
- Docker Desktop (for Cosmos DB emulator via Aspire)
- VS Code or Visual Studio 2022

## Quick Test

### 1. Start the Development Environment

```bash
cd libris-maleficarum-service
dotnet run --project src/Orchestration/AppHost
```

This starts:

- API service on `https://localhost:5001`
- Cosmos DB emulator
- Aspire Dashboard on `http://localhost:15888`

### 2. Create Test Data

```bash
# Create a world
curl -X POST https://localhost:5001/api/v1/worlds \
  -H "Content-Type: application/json" \
  -d '{"name": "Test World", "description": "For testing delete"}'

# Note the worldId from response

# Create a parent entity
curl -X POST https://localhost:5001/api/v1/worlds/{worldId}/entities \
  -H "Content-Type: application/json" \
  -d '{"name": "Parent Entity", "entityType": "Location"}'

# Note the entityId from response

# Create child entities
curl -X POST https://localhost:5001/api/v1/worlds/{worldId}/entities \
  -H "Content-Type: application/json" \
  -d '{"name": "Child 1", "entityType": "Location", "parentId": "{parentEntityId}"}'

curl -X POST https://localhost:5001/api/v1/worlds/{worldId}/entities \
  -H "Content-Type: application/json" \
  -d '{"name": "Child 2", "entityType": "Location", "parentId": "{parentEntityId}"}'
```

### 3. Initiate Delete With Cascade

> **Note**: The `cascade` parameter controls whether descendant entities are deleted:
>
> - `cascade=true` (default): Deletes the entity and all its descendants
> - `cascade=false`: Deletes only the specified entity if it has no children (returns 400 Bad Request if children exist)
>
> All deletes return `202 Accepted` immediately and are processed asynchronously in the background.

```bash
# Delete parent with cascade (returns 202 immediately)
curl -X DELETE "https://localhost:5001/api/v1/worlds/{worldId}/entities/{parentEntityId}?cascade=true" \
  -v

# Response: 202 Accepted
# Location: /api/v1/worlds/{worldId}/delete-operations/{operationId}
# Body:
# {
#   "data": {
#     "id": "{operationId}",
#     "status": "pending",
#     "totalEntities": 0,
#     "deletedCount": 0
#   }
# }
```

### 4. Poll for Completion

```bash
# Check operation status (poll every 500ms until complete)
curl https://localhost:5001/api/v1/worlds/{worldId}/delete-operations/{operationId}

# Response (in progress):
# {
#   "data": {
#     "id": "{operationId}",
#     "status": "in_progress",
#     "totalEntities": 3,
#     "deletedCount": 1
#   }
# }

# Response (completed):
# {
#   "data": {
#     "id": "{operationId}",
#     "status": "completed",
#     "totalEntities": 3,
#     "deletedCount": 3,
#     "failedCount": 0,
#     "completedAt": "2026-01-31T12:00:05.000Z"
#   }
# }
```

### 5. List Recent Delete Operations

```bash
# View all recent delete operations for the world
curl https://localhost:5001/api/v1/worlds/{worldId}/delete-operations?limit=10

# Response:
# {
#   "data": [
#     { "id": "op1", "status": "completed", "deletedCount": 3 },
#     { "id": "op2", "status": "in_progress", "deletedCount": 5 }
#   ],
#   "meta": { "count": 2 }
# }
```

### 6. Verify Deletion

```bash
# Get entity (should return 404)
curl https://localhost:5001/api/v1/worlds/{worldId}/entities/{parentEntityId}

# List entities (deleted entities excluded)
curl https://localhost:5001/api/v1/worlds/{worldId}/entities
```

### 7. View Telemetry

Open Aspire Dashboard at `http://localhost:15888`:

1. Navigate to **Traces** tab
1. Filter by operation name `EntitySoftDelete` or `DeleteOperation`
1. View structured attributes:
   - `operation.id`, `entity.id`, `entity.name`, `entity.type`
   - `delete.cascade`, `delete.total_count`, `delete.deleted_count`
   - `delete.status`, `user.id`

## Running Tests

### Unit Tests

```bash
cd libris-maleficarum-service
dotnet test --filter TestCategory=Unit
```

### Integration Tests

```bash
cd libris-maleficarum-service
dotnet test --filter TestCategory=Integration
```

### Specific Delete Tests

```bash
dotnet test --filter FullyQualifiedName~DeleteAsync
dotnet test --filter FullyQualifiedName~SoftDelete
dotnet test --filter FullyQualifiedName~DeleteOperation
```

## Configuration

### Delete Operation Settings

Edit `src/Api/appsettings.json`:

```json
{
  "DeleteOperation": {
    "PollingIntervalMs": 500,
    "OperationTtlHours": 24,
    "MaxBatchSize": 50,
    "RateLimitPerSecond": 50
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `PollingIntervalMs` | 500 | Recommended polling interval for clients |
| `OperationTtlHours` | 24 | Hours before completed operations are purged |
| `MaxBatchSize` | 50 | Max entities processed per batch |
| `RateLimitPerSecond` | 50 | Rate limit for background processing |

## Key Files

| File | Purpose |
|------|---------|
| [WorldEntity.cs](../../libris-maleficarum-service/src/Domain/Entities/WorldEntity.cs) | Entity with soft delete fields |
| [DeleteOperation.cs](../../libris-maleficarum-service/src/Domain/Entities/DeleteOperation.cs) | Operation tracking entity |
| [WorldEntityRepository.cs](../../libris-maleficarum-service/src/Infrastructure/Repositories/WorldEntityRepository.cs) | Repository with delete logic |
| [DeleteOperationRepository.cs](../../libris-maleficarum-service/src/Infrastructure/Repositories/DeleteOperationRepository.cs) | Operation persistence |
| [WorldEntitiesController.cs](../../libris-maleficarum-service/src/Api/Controllers/WorldEntitiesController.cs) | DELETE endpoint (202 response) |
| [DeleteOperationsController.cs](../../libris-maleficarum-service/src/Api/Controllers/DeleteOperationsController.cs) | Status polling endpoints |
| [DeleteService.cs](../../libris-maleficarum-service/src/Infrastructure/Services/DeleteService.cs) | Delete orchestration |
| [DeleteOperationProcessor.cs](../../libris-maleficarum-service/src/Infrastructure/Processors/DeleteOperationProcessor.cs) | Background processor |

## Troubleshooting

### Delete Returns 403 Forbidden

User doesn't own the world. Check:

- `IUserContextService` returns correct user ID
- World's `OwnerId` matches current user

### Delete Returns 404 Not Found

Entity or world doesn't exist:

- Verify world ID is valid
- Verify entity ID is valid

### Operation Status Returns 404

Operation may have expired (24-hour TTL):

- Operations auto-purge 24 hours after completion
- Re-initiate delete if needed

### Operation Stuck in "pending" or "in_progress"

Background processor may have issues:

1. Check Aspire Dashboard for processor errors
1. Verify `DeleteOperationProcessor` is running
1. Check for exceptions in processor logs
1. Restart AppHost if needed

### High RU Usage

Large cascade may be consuming RUs:

1. Lower `RateLimitPerSecond` setting
1. Check `MaxBatchSize` configuration
1. Monitor RU consumption in Aspire Dashboard

### Partial Completion

Operation completed with some failures:

1. Check `failedEntityIds` in operation response
1. Review `errorDetails` for failure reasons
1. Retry individual failed entities if needed

### Rate Limit Exceeded (429)

Too many concurrent delete operations for this user/world:

1. Wait for `Retry-After` seconds indicated in response header
1. Check `GET /delete-operations` to see pending/in-progress operations
1. Wait for existing operations to complete before retrying
1. Limit: 5 concurrent operations per user per world
