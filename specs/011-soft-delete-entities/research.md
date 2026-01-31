# Research: Soft Delete World Entities API

**Feature**: 011-soft-delete-entities  
**Date**: 2026-01-31

## Existing Implementation Analysis

### Current State

The codebase already has soft delete infrastructure in place:

| Component | File | Status |
|-----------|------|--------|
| `WorldEntity.IsDeleted` | [Domain/Entities/WorldEntity.cs](../../libris-maleficarum-service/src/Domain/Entities/WorldEntity.cs) | ✅ Exists |
| `WorldEntity.SoftDelete()` | Same file | ⚠️ Needs enhancement (add DeletedDate, DeletedBy) |
| `IWorldEntityRepository.DeleteAsync()` | [Domain/Interfaces/Repositories/IWorldEntityRepository.cs](../../libris-maleficarum-service/src/Domain/Interfaces/Repositories/IWorldEntityRepository.cs) | ⚠️ Signature exists, needs enhancement |
| `WorldEntityRepository.DeleteAsync()` | [Infrastructure/Repositories/WorldEntityRepository.cs](../../libris-maleficarum-service/src/Infrastructure/Repositories/WorldEntityRepository.cs) | ⚠️ Basic sync cascade exists, needs async path |
| `WorldEntitiesController.DeleteEntity()` | [Api/Controllers/WorldEntitiesController.cs](../../libris-maleficarum-service/src/Api/Controllers/WorldEntitiesController.cs) | ⚠️ Endpoint exists, needs error handling for idempotency |
| `ITelemetryService` | [Domain/Interfaces/Services/ITelemetryService.cs](../../libris-maleficarum-service/src/Domain/Interfaces/Services/ITelemetryService.cs) | ✅ Exists with RecordEntityDeleted() |
| Change Feed Processor | N/A | ❌ Does not exist (new infrastructure needed) |

### Gaps to Address

1. **Entity Enhancement**: Add `DeletedDate` and `DeletedBy` properties to `WorldEntity`
1. **Idempotent Delete**: Currently throws `EntityNotFoundException` when entity doesn't exist or already deleted
1. **Async Cascade**: No infrastructure for Change Feed processing
1. **Cascade Heuristics**: No threshold detection for sync vs async path
1. **Audit Logging**: Need structured telemetry events with cascade count

## Technology Decisions

### Decision 1: Cosmos DB Change Feed vs Azure Queue Storage

**Context**: FR-010 requires async cascade processing for large hierarchies.

**Options Evaluated**:

| Option | Pros | Cons |
|--------|------|------|
| Cosmos DB Change Feed | Built-in, no new service, automatic retry, ordered delivery | Couples processor to Cosmos SDK, requires lease container |
| Azure Queue Storage | Decoupled, simple, per-message visibility | Extra service, no ordering guarantee, separate infra |
| Azure Service Bus | Enterprise features, dead-letter, sessions | Overkill for internal async work, added cost |

**Decision**: **Cosmos DB Change Feed**

**Rationale**:

- Already planned in architecture (DATA_MODEL.md mentions Change Feed for DeletedWorldEntity migration)
- No new Azure resources needed
- Automatic retry on processor failures
- Ordered processing per partition (important for hierarchy integrity)
- Lease container can be co-located in same Cosmos account

### Decision 2: Change Feed Processor Hosting

**Context**: Where does the Change Feed processor run?

**Options Evaluated**:

| Option | Pros | Cons |
|--------|------|------|
| In-process (API project) | Simple, no new deployable | Blocks API threads, scales with API not with work volume |
| Separate Container App | Independent scaling, isolated failures | Extra deployable, more infra complexity |
| Azure Functions | Serverless, built-in bindings | Cold start latency, less control over processing |

**Decision**: **In-process with hosted service** (for MVP), migrate to separate Container App if needed

**Rationale**:

- Simplest initial implementation
- `IHostedService` in API project runs continuously
- Low volume expected (deletes are infrequent)
- Can refactor to separate project later if delete volume increases
- Aspire dashboard shows processor telemetry locally

### Decision 3: Cascade Threshold Heuristics

**Context**: How to decide between sync and async cascade without expensive counting.

**Decision**: Use **direct children count + entity depth** as heuristics:

- If entity has >10 direct children → async
- If entity depth <3 (closer to root) → async
- Otherwise → sync

**Rationale**:

- Single query for direct children count (2-3 RUs)
- Depth already stored on entity (no query needed)
- Conservative: errs toward async for ambiguous cases
- Configurable via `appsettings.json` for tuning

### Decision 4: OpenTelemetry Logging Structure

**Context**: FR-009 requires structured audit logging.

**Decision**: Custom telemetry events via `Activity` with structured tags:

```csharp
using var activity = _telemetryService.StartActivity("EntitySoftDelete", new Dictionary<string, object>
{
    { "entity.id", entityId },
    { "entity.world_id", worldId },
    { "entity.name", entityName },
    { "entity.type", entityType },
    { "delete.cascade", isCascade },
    { "delete.cascade_count", cascadeCount },
    { "delete.async", isAsync },
    { "user.id", userId }
});
```

**Rationale**:

- OpenTelemetry SDK automatically routes to Application Insights (prod) or Aspire Dashboard (local)
- Structured tags enable rich querying in Application Insights
- Activity context propagates correlation IDs
- Existing `ITelemetryService.StartActivity()` pattern

### Decision 5: Rate Limiting Strategy

**Context**: FR-016 requires limiting concurrent delete operations to prevent abuse.

**Options Evaluated**:

| Option | Pros | Cons |
|--------|------|------|
| No limit | Simplest | Risk of RU exhaustion, abuse |
| Global limit per world | Simple | Unfair if one user blocks others |
| User-scoped limit per world | Fair, prevents single-user abuse | Slightly more complex query |
| Queue-based | Never rejects | Unbounded queue growth risk |

**Decision**: **User-scoped limit of 5 concurrent operations per world**

**Rationale**:

- Fair: Each user can have 5 pending/in-progress operations
- Simple enforcement: Query `DeleteOperation` by `WorldId` + `CreatedBy` + status in (Pending, InProgress)
- 429 response with `Retry-After` header provides clear guidance to clients
- Limit of 5 is generous for legitimate use but prevents abuse

## Best Practices Applied

### Cosmos DB Soft Delete Best Practices

1. **Filter at query level**: All queries must include `WHERE IsDeleted = false`
   - Already implemented in `WorldEntityRepository.GetByIdAsync()` and `GetAllByWorldAsync()`

1. **Separate deleted container**: Use Change Feed to move to DeletedWorldEntity after grace period
   - Planned but out of scope for this feature

1. **TTL for hard delete**: Configure TTL on DeletedWorldEntity container
   - Planned but out of scope for this feature

### .NET Change Feed Processor Best Practices

1. **Lease container**: Use separate container or partition for leases
1. **Processor name**: Unique name per processor instance
1. **Start from beginning vs now**: Start from "now" for new processors
1. **Handle exceptions gracefully**: Don't throw—log and continue
1. **Idempotent processing**: Re-processing same change must be safe

### ASP.NET Core Async Best Practices

1. **Don't await async processing in request**: Fire-and-forget for async cascade
1. **Return 204 immediately**: User sees success, cascade continues in background
1. **Use CancellationToken**: Respect request cancellation for sync operations
1. **Structured logging**: Use ILogger<T> with message templates

## Integration Points

### Existing Services to Integrate

| Service | Interface | Usage |
|---------|-----------|-------|
| User Context | `IUserContextService` | Get current user ID for `DeletedBy` |
| Telemetry | `ITelemetryService` | Audit logging with structured events |
| World Repository | `IWorldRepository` | Validate world ownership |

### New Services to Create

| Service | Interface | Purpose |
|---------|-----------|---------|
| Cascade Delete Service | `ICascadeDeleteService` | Encapsulate cascade logic, threshold detection |
| Change Feed Processor | `CascadeDeleteProcessor` | Async cascade processing |

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Async cascade fails silently | Medium | Medium | Structured logging, retry on Change Feed, dead-letter monitoring |
| RU spike on large sync cascade | Low | Low | Conservative heuristics favor async path |
| Inconsistent state during async | Medium | Low | All queries filter IsDeleted=false; temporary visibility acceptable |
| Change Feed lag | Low | Low | Near real-time for most scenarios; acceptable for delete cascade |

## References

- [Cosmos DB Change Feed Documentation](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/change-feed)
- [EF Core Cosmos Provider](https://learn.microsoft.com/en-us/ef/core/providers/cosmos/)
- [OpenTelemetry .NET SDK](https://opentelemetry.io/docs/languages/dotnet/)
- [DATA_MODEL.md](../../docs/design/DATA_MODEL.md) - Existing data model and RU analysis
