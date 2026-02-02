# Quickstart Validation Checklist

**Feature**: 011-soft-delete-entities  
**Date**: 2026-02-01  
**Purpose**: Validation checklist for quickstart.md scenarios

## Prerequisites Verification

- [ ] .NET 10 SDK installed (`dotnet --version`)
- [ ] Docker Desktop running
- [ ] Aspire CLI installed (`dotnet workload list` shows `aspire`)
- [ ] Ports 5001, 15888 available

## Setup Validation

### Start Development Environment

- [ ] `cd libris-maleficarum-service`
- [ ] `dotnet run --project src/Orchestration/AppHost` succeeds
- [ ] API accessible at `https://localhost:5001`
- [ ] Aspire Dashboard accessible at `http://localhost:15888`
- [ ] Cosmos DB emulator container running (visible in Aspire Dashboard)

## Scenario 1: Create Test Data

### Create World

```bash
curl -X POST https://localhost:5001/api/v1/worlds \
  -H "Content-Type: application/json" \
  -d '{"name": "Test World", "description": "For testing delete"}' \
  -k
```

- [ ] Response: `201 Created`
- [ ] Response contains `id` field (save as `{worldId}`)
- [ ] Response contains `name: "Test World"`

### Create Parent Entity

```bash
curl -X POST https://localhost:5001/api/v1/worlds/{worldId}/entities \
  -H "Content-Type: application/json" \
  -d '{"name": "Parent Entity", "entityType": "Location"}' \
  -k
```

- [ ] Response: `201 Created`
- [ ] Response contains `id` field (save as `{parentEntityId}`)
- [ ] Response contains `name: "Parent Entity"`

### Create Child Entities

```bash
curl -X POST https://localhost:5001/api/v1/worlds/{worldId}/entities \
  -H "Content-Type: application/json" \
  -d '{"name": "Child 1", "entityType": "Location", "parentId": "{parentEntityId}"}' \
  -k

curl -X POST https://localhost:5001/api/v1/worlds/{worldId}/entities \
  -H "Content-Type: application/json" \
  -d '{"name": "Child 2", "entityType": "Location", "parentId": "{parentEntityId}"}' \
  -k
```

- [ ] Both responses: `201 Created`
- [ ] Both contain `parentId` matching `{parentEntityId}`

## Scenario 2: Delete With Cascade (Async Processing)

### Initiate Delete

```bash
curl -X DELETE "https://localhost:5001/api/v1/worlds/{worldId}/entities/{parentEntityId}?cascade=true" \
  -v -k
```

- [ ] Response: `202 Accepted`
- [ ] Response contains `Location` header pointing to `/delete-operations/{operationId}`
- [ ] Response body contains:
  - [ ] `id` field (save as `{operationId}`)
  - [ ] `status: "pending"` or `"in_progress"`
  - [ ] `totalEntities: 0` (initially)
  - [ ] `deletedCount: 0` (initially)

### Poll for Progress (In Progress)

```bash
curl https://localhost:5001/api/v1/worlds/{worldId}/delete-operations/{operationId} -k
```

**First poll (operation may still be in progress):**

- [ ] Response: `200 OK`
- [ ] Response contains `status` field (may be `"pending"` or `"in_progress"`)
- [ ] If `"in_progress"`:
  - [ ] `totalEntities` is greater than 0 (should be 3)
  - [ ] `deletedCount` is 0 or higher

### Poll for Completion

```bash
# Wait 500ms, then poll again
curl https://localhost:5001/api/v1/worlds/{worldId}/delete-operations/{operationId} -k
```

**Repeat polling until status is `"completed"`:**

- [ ] Response: `200 OK`
- [ ] Response contains:
  - [ ] `status: "completed"`
  - [ ] `totalEntities: 3` (parent + 2 children)
  - [ ] `deletedCount: 3`
  - [ ] `failedCount: 0`
  - [ ] `completedAt` timestamp is set

## Scenario 3: Delete Without Cascade (Validation Error)

### Attempt Delete Without Cascade on Parent

```bash
curl -X DELETE "https://localhost:5001/api/v1/worlds/{worldId}/entities/{parentEntityId}?cascade=false" \
  -v -k
```

**Note**: This test requires that the parent entity from Scenario 1 still has children. If you completed Scenario 2, you'll need to recreate the test data first.

- [ ] Response: `400 Bad Request`
- [ ] Response contains error code `ENTITY_HAS_CHILDREN`
- [ ] Response message mentions `cascade=true` as solution
- [ ] Parent entity is NOT deleted (verify with GET request)

### Verify Parent Still Exists

```bash
curl https://localhost:5001/api/v1/worlds/{worldId}/entities/{parentEntityId} -k
```

- [ ] Response: `200 OK`
- [ ] Parent entity is still present and not deleted

## Scenario 4: List Recent Operations

```bash
curl "https://localhost:5001/api/v1/worlds/{worldId}/delete-operations?limit=10" -k
```

- [ ] Response: `200 OK`
- [ ] Response contains `data` array with at least 1 operation
- [ ] First operation matches `{operationId}` from Scenario 2
- [ ] Response contains `meta.count` field
- [ ] Each operation has `id`, `status`, `deletedCount` fields

## Scenario 5: Verify Deletion (from Scenario 2)

### Get Entity (Should Return 404)

```bash
curl https://localhost:5001/api/v1/worlds/{worldId}/entities/{parentEntityId} -v -k
```

**Note**: This verifies the entity deleted in Scenario 2 is truly gone.

- [ ] Response: `404 Not Found`
- [ ] Response contains error code `ENTITY_NOT_FOUND`

### List All Entities (Deleted Excluded)

```bash
curl "https://localhost:5001/api/v1/worlds/{worldId}/entities" -k
```

- [ ] Response: `200 OK`
- [ ] Response `data` array does NOT contain:
  - [ ] Parent entity (`{parentEntityId}`)
  - [ ] Child 1 (from Scenario 1)
  - [ ] Child 2 (from Scenario 1)
- [ ] Soft-deleted entities are filtered out

## Scenario 6: Rate Limiting

### Create 6 More Entities

```bash
for i in {1..6}; do
  curl -X POST https://localhost:5001/api/v1/worlds/{worldId}/entities \
    -H "Content-Type: application/json" \
    -d "{\"name\": \"Rate Limit Test Entity $i\", \"entityType\": \"Location\"}" \
    -k
done
```

### Initiate 6 Concurrent Deletes

```bash
# Save entity IDs from above, then run 6 deletes in parallel
# (This is easier in a script, but can test manually)
```

- [ ] First 5 requests: `202 Accepted`
- [ ] 6th request: `429 Too Many Requests`
- [ ] 429 response contains `Retry-After` header
- [ ] 429 response contains error code `RATE_LIMIT_EXCEEDED`

## Scenario 7: Telemetry Verification

### Open Aspire Dashboard

1. Navigate to `http://localhost:15888`
1. Click **Traces** tab
1. Filter by operation name: `DeleteOperation`

**Verify traces contain:**

- [ ] `operation.id` attribute
- [ ] `entity.id` attribute
- [ ] `entity.name` attribute
- [ ] `entity.type` attribute
- [ ] `delete.cascade` attribute
- [ ] `delete.status` attribute
- [ ] Structured events with timestamps

## Scenario 8: Configuration Changes

### Verify Config File

```bash
cat src/Api/appsettings.json
```

- [ ] Contains `DeleteOperation` section
- [ ] `MaxConcurrentPerUserPerWorld: 5`
- [ ] `RetryAfterSeconds: 60` (or configured value)

### Optional: Test Config Override

Edit `appsettings.Development.json` to lower rate limit:

```json
{
  "DeleteOperation": {
    "MaxConcurrentPerUserPerWorld": 2
  }
}
```

- [ ] Restart AppHost
- [ ] Verify new rate limit is enforced (only 2 concurrent allowed)

## Summary

**Total Checks**: 70+  
**Required Pass Rate**: 100% for production readiness

## Troubleshooting Guide

If any scenario fails:

1. **Check AppHost logs** in Aspire Dashboard → Logs
1. **Verify Cosmos DB emulator** is running
1. **Check port conflicts** (5001, 15888)
1. **Restart AppHost** if services are stale
1. **Review test coverage** - unit/integration tests should catch issues first

## Sign-Off

- [ ] All scenarios executed manually
- [ ] All checks passed
- [ ] Performance metrics acceptable (see PerformanceTests.cs results)
- [ ] Telemetry verified in Aspire Dashboard
- [ ] No errors in Aspire logs during test execution

**Validated By**: _____________  
**Date**: _____________  
**Notes**: _____________
