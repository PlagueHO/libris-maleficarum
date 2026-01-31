# Feature Specification: Soft Delete World Entities API

**Feature Branch**: `011-soft-delete-entities`  
**Created**: 2026-01-31  
**Status**: Draft  
**Input**: User description: "Add backend API support for soft deleting World Entities in the libris-maleficarum-service"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Delete Single Entity (Priority: P1)

As a world builder, I want to delete a single entity from my world so that I can remove content I no longer need while preserving the ability to recover it later.

**Why this priority**: This is the core soft-delete functionality. Without single-entity delete, no delete operations are possible. Most user deletions are single entities.

**Independent Test**: Can be fully tested by calling DELETE endpoint on a single entity with no children, receiving a 202 Accepted with operation ID, and polling the status endpoint until completion.

**Acceptance Scenarios**:

1. **Given** a world with an entity that has no children, **When** the user sends a DELETE request to `/api/v1/worlds/{worldId}/entities/{entityId}`, **Then** the response returns `202 Accepted` with a `Location` header containing the URL to the delete operation status endpoint.
1. **Given** a delete operation is initiated, **When** the user polls the status endpoint, **Then** the response shows the operation status (`pending`, `in_progress`, `completed`, or `failed`) and progress details.
1. **Given** a deleted entity, **When** the user queries for entities in the world, **Then** the deleted entity is not included in the response.
1. **Given** a deleted entity, **When** the user tries to access the entity directly via GET, **Then** the response returns `404 Not Found`.
1. **Given** a non-existent entity ID, **When** the user sends a DELETE request, **Then** the response returns `404 Not Found`.
1. **Given** an entity in a world the user does not own, **When** the user sends a DELETE request, **Then** the response returns `403 Forbidden`.

---

### User Story 2 - Delete Entity with Cascade (Priority: P2)

As a world builder, I want to delete an entity and have all its children automatically deleted so that I don't have orphaned entities cluttering my world.

**Why this priority**: Hierarchical deletion is essential for usability—deleting a continent should remove all countries, cities, and characters within it. This is the expected behavior for tree-structured content.

**Independent Test**: Can be fully tested by creating a parent entity with nested children, deleting the parent, and verifying all descendants are marked as deleted.

**Acceptance Scenarios**:

1. **Given** an entity with child entities (direct children only), **When** the user sends a DELETE request to the parent entity, **Then** the parent and all direct children are marked as `IsDeleted = true` with appropriate `DeletedDate` and `DeletedBy`.
1. **Given** an entity with deeply nested children (grandchildren, great-grandchildren), **When** the user sends a DELETE request to the root parent, **Then** all descendants at any depth are marked as deleted.
1. **Given** a parent entity is deleted, **When** the user queries for child entities by ParentId, **Then** no children are returned (filtered by `IsDeleted = false`).
1. **Given** a cascade delete operation, **When** the operation completes, **Then** all affected entity IDs are logged for audit purposes.

---

### User Story 3 - Monitor Delete Progress (Priority: P3)

As a world builder deleting a large portion of my world, I want to monitor the progress of the delete operation so I know when it's safe to continue working.

**Why this priority**: Large cascading deletes take time; users need visibility into progress and completion status to avoid confusion.

**Independent Test**: Can be tested by initiating a cascade delete on a hierarchy with 50+ entities and polling the status endpoint until all entities are processed.

**Acceptance Scenarios**:

1. **Given** a cascade delete operation is in progress, **When** the user polls the status endpoint, **Then** the response includes `total` (entities to delete), `deleted` (entities processed so far), and estimated time remaining.
1. **Given** a very large hierarchy (500+ entities), **When** the user monitors the delete operation, **Then** progress updates in near real-time (within 2 seconds of actual progress).
1. **Given** a delete operation completes, **When** the user polls the status endpoint, **Then** the status shows `completed` with final counts and duration.
1. **Given** a delete operation fails partially, **When** the user polls the status endpoint, **Then** the status shows `failed` or `partial` with details of which entities failed and why.

---

### Edge Cases

- What happens when deleting an entity that is already deleted? (Should return `202 Accepted` with a new operation that completes immediately with count=0 - idempotent)
- How does the system handle concurrent delete requests for the same entity? (Each request creates a new operation; second operation completes immediately with count=0 since entity already marked deleted)
- What happens if the cascade delete fails partway through? (Operation status shows `partial` or `failed` with list of failed entity IDs; user can retry or investigate)
- How are assets attached to deleted entities handled? (Assets remain but are orphaned; cleaned up in separate asset cleanup job)
- How long are delete operation records retained? (24 hours after completion, then auto-purged)
- What happens if the user polls a non-existent or expired operation ID? (Returns `404 Not Found`)
- Can the user cancel an in-progress delete operation? (Out of scope for initial release; operation runs to completion)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a DELETE endpoint at `/api/v1/worlds/{worldId}/entities/{entityId}` that initiates a soft-delete operation and returns `202 Accepted` with a `Location` header pointing to the operation status endpoint.
- **FR-002**: Soft delete MUST set `IsDeleted = true`, `DeletedDate` to current UTC timestamp, and `DeletedBy` to the authenticated user's ID.
- **FR-003**: System MUST exclude soft-deleted entities from all standard query results (entities with `IsDeleted = true` are filtered out).
- **FR-004**: System MUST return `404 Not Found` when attempting to GET a soft-deleted entity.
- **FR-005**: System MUST cascade soft-delete to all descendant entities when a parent entity is deleted (cascade=true, which is the default).
- **FR-006**: System MUST validate that the authenticated user owns the world before allowing delete operations (return `403 Forbidden` otherwise).
- **FR-007**: System MUST return `404 Not Found` when attempting to delete a non-existent entity or an entity in a non-existent world.
- **FR-008**: Delete operation MUST be idempotent—deleting an already-deleted entity returns `202 Accepted` with an operation that completes immediately with `deleted: 0`.
- **FR-009**: System MUST log all delete operations with entity IDs, user ID, timestamp, and cascade count via OpenTelemetry structured telemetry (routes to Application Insights in production, Aspire Dashboard locally).
- **FR-010**: All delete operations MUST be processed asynchronously via background processor (Change Feed or in-process queue) to provide consistent API response times.
- **FR-011**: System MUST NOT permanently remove soft-deleted entities—the DeletedWorldEntity container handles eventual hard deletion via TTL (handled by existing Change Feed processor design).
- **FR-012**: System MUST provide a GET endpoint at `/api/v1/worlds/{worldId}/delete-operations/{operationId}` that returns the current status and progress of a delete operation.
- **FR-013**: Delete operation status MUST include: `status` (pending, in_progress, completed, failed, partial), `progress` (total entities, deleted count), `createdAt`, `completedAt`, and error details if applicable.
- **FR-014**: System MUST provide a GET endpoint at `/api/v1/worlds/{worldId}/delete-operations` that lists recent delete operations for the world (last 24 hours).
- **FR-015**: Delete operation records MUST be automatically purged 24 hours after completion to prevent unbounded storage growth.
- **FR-016**: System MUST enforce a user-scoped limit of 5 concurrent delete operations per world; requests exceeding this limit MUST return `429 Too Many Requests` with a `Retry-After` header.
- **FR-017**: Azure AI Search queries MUST filter on `IsDeleted = false` to exclude soft-deleted entities from search results.
- **FR-018**: When entities are permanently removed from the DeletedWorldEntity container (TTL expiry), the corresponding Azure AI Search index entries MUST be removed (handled by existing/planned Change Feed processor for hard delete cleanup).
- **FR-019**: On processor restart, the system MUST resume any in-progress delete operations from their last checkpoint (based on DeleteOperation.DeletedCount) rather than restarting from scratch or marking as failed.

### Key Entities *(include if feature involves data)*

- **WorldEntity**: The entity being deleted. Key fields: `Id`, `WorldId`, `ParentId`, `IsDeleted`, `DeletedDate`, `DeletedBy`.
- **Cascade Descendants**: All entities where `ParentId` chain leads to the deleted entity. Must be discovered recursively via `ParentId` navigation.
- **DeleteOperation**: Tracks the status of a delete operation. Key fields: `Id` (operation ID), `WorldId`, `RootEntityId`, `Status` (pending/in_progress/completed/failed/partial), `TotalEntities`, `DeletedCount`, `FailedCount`, `CreatedAt`, `CompletedAt`, `CreatedBy`, `ErrorDetails`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: DELETE request returns `202 Accepted` in under 200ms regardless of hierarchy size.
- **SC-002**: Single entity deletion (no children) completes background processing in under 500ms.
- **SC-003**: Small cascade deletions (up to 50 descendants) complete background processing in under 5 seconds.
- **SC-004**: Large cascade deletions (500+ entities) complete background processing within 60 seconds.
- **SC-005**: Status endpoint returns current progress within 100ms.
- **SC-006**: RU cost spread over time using asynchronous processing, avoiding RU spikes above 100 RU/s sustained.
- **SC-007**: 100% of soft-deleted entities are excluded from standard API query responses.
- **SC-008**: All delete operations are logged and auditable.
- **SC-009**: Delete operation status is accurate within 2 seconds of actual progress.

## Assumptions

- The World container and WorldEntity container already exist with the schema defined in DATA_MODEL.md.
- Authentication and authorization infrastructure (`IUserContextService`) is stubbed and returns a test user ID.
- The hierarchical partition key structure `[/WorldId, /id]` is in place for the WorldEntity container.
- Change Feed processor infrastructure for moving entities to DeletedWorldEntity container after the grace period is a separate feature (not in scope for this specification).
- Azure AI Search index updates for soft-deleted entities are handled via query-time filtering (`IsDeleted = false`); index entry removal on permanent deletion is handled by the existing/planned Change Feed processor for hard delete cleanup.
- Restore/undo functionality is out of scope and will be delivered as a separate feature specification.

## RU Cost Analysis and Design Rationale

### The Challenge with Cascade Delete

The existing data model uses hierarchical partition key `[/WorldId, /id]`, which distributes entities across partitions. This prevents hot partitions but means cascade delete requires:

1. **Discover descendants**: Query by `ParentId` at each level (2-5 RUs per level)
1. **Update each entity**: 6-10 RUs per entity (read + update)

For a parent with 100 descendants across 5 levels:

- Discovery: ~15-25 RUs (multiple queries by ParentId)
- Updates: ~600-1000 RUs (100 entities × 6-10 RUs each)
- **Total: ~615-1025 RUs**

### Alternative Approaches Considered

| Approach | RU Cost | Complexity | Trade-offs |
|----------|---------|------------|------------|
| **Immediate cascade** (mark all descendants) | High (6-10 RU × N entities) | Low | Simple but expensive for large hierarchies |
| **Virtual cascade** (check parent IsDeleted on read) | Low (only parent marked) | High | Every read must traverse ancestry; adds query latency |
| **Deferred async cascade** (parent marked, children via Change Feed) | Low immediate, spread over time | Medium | Short inconsistency window; children visible briefly after parent deleted |
| **Batch async cascade** (queue processed in background) | Low immediate, spread over time | Medium | Dedicated background job; same temporary inconsistency |

### Recommended Approach: All Async with Polling Status

**All delete operations are asynchronous** for a consistent API contract:

1. DELETE request creates a `DeleteOperation` record and returns `202 Accepted` immediately
2. Background processor (Change Feed or in-process queue) processes the deletion
3. Frontend polls status endpoint to track progress
4. Operation marked `completed` when all entities processed

**Benefits of all-async approach**:

- **Consistent API contract**: Frontend always handles 202 + polling (no conditional logic)
- **Predictable response times**: DELETE always returns in <200ms
- **Scalable**: Same pattern works for 1 entity or 10,000 entities
- **Better UX**: User always sees progress indicator, never left wondering
- **Future-proof**: Easy to add SignalR real-time updates later

**Query-time filtering**: All queries filter `IsDeleted = false`, ensuring deleted entities never appear in results regardless of cascade progress.

## Clarifications

### Session 2026-01-31

- Q: Should restore/undo functionality be included in this feature? → A: Separate feature - restore will be specified/implemented in a future feature (out of scope here)
- Q: Which async mechanism for large hierarchy cascade? → A: Change Feed processor hosted in .NET 10 service on Azure Container Apps (reuses planned backend infrastructure)
- Q: How should cascade failures be handled? → A: Log and retry - log failures, continue processing remaining entities, retry failed entities on next poll (idempotent updates safe to retry)
- Q: How should cascade threshold (sync vs async) be detected? → A: All deletes are async for consistent API contract; no threshold needed
- Q: What audit logging format/mechanism? → A: Structured telemetry via OpenTelemetry (routes to Application Insights in production, Aspire Dashboard locally)
- Q: Should all deletes be async or use hybrid sync/async? → A: All deletes async with polling status endpoint for consistent frontend handling and better UX
- Q: What rate limiting for concurrent delete operations? → A: User-scoped limit per world (5 concurrent) - reject new requests with 429 Too Many Requests if limit exceeded
- Q: How should Azure AI Search index handle soft-deleted entities? → A: Filter at query time - deleted entities remain in index but IsDeleted filter applied on all searches; when entity is permanently removed from DeletedWorldEntity (after TTL), search index entry must also be removed
- Q: How should processor handle crash/restart mid-operation? → A: Resume from checkpoint - on restart, processor resumes unfinished operations from where they left off using DeleteOperation status
