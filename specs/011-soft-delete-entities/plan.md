# Implementation Plan: Soft Delete World Entities API

**Branch**: `011-soft-delete-entities` | **Date**: 2026-01-31 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/011-soft-delete-entities/spec.md`

## Summary

Add backend API support for soft deleting World Entities with asynchronous cascade processing. All DELETE requests return `202 Accepted` immediately with a polling status endpoint for monitoring progress. Cascade deletes are processed by a background `IHostedService` using Cosmos DB Change Feed or in-process queue. DeleteOperation entity tracks progress with 24-hour TTL auto-cleanup. User-scoped rate limiting (5 concurrent operations per world) prevents abuse.

**Key Technical Approach**:

- All deletes are async for consistent API contract (no sync/async threshold)
- `DeleteOperation` entity stores in same WorldEntity container with discriminator `_type`
- Background processor (`DeleteOperationProcessor`) handles cascade logic
- OpenTelemetry structured logging for audit trail
- Query-time filtering for soft-deleted entities (IsDeleted=false)

## Technical Context

**Language/Version**: .NET 10, C# 14  
**Primary Dependencies**: ASP.NET Core, EF Core 10 (Cosmos DB provider), Aspire.NET (local dev)  
**Storage**: Azure Cosmos DB (WorldEntity container with hierarchical partition key `[/WorldId, /id]`)  
**Testing**: MSTest, FluentAssertions, Testcontainers (Cosmos emulator)  
**Target Platform**: Azure Container Apps (production), Aspire AppHost (local development)  
**Project Type**: Web application (API + frontend)  
**Performance Goals**: DELETE returns 202 in <200ms; cascade completion <60s for 500+ entities  
**Constraints**: RU limit 100 RU/s sustained; user-scoped 5 concurrent operations per world  
**Scale/Scope**: ~1000 entities per world typical; rare >5000 entity cascades

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Cloud-Native Architecture | ✅ PASS | Uses Cosmos DB, Azure Container Apps, private endpoints |
| II. Clean Architecture | ✅ PASS | Api → Domain ← Infrastructure layers; repository pattern |
| III. Test-Driven Development | ✅ PASS | Unit + integration tests planned; AAA pattern; jest-axe N/A (backend) |
| IV. Framework Standards | ✅ PASS | .NET 10, EF Core 10, Aspire.NET for local dev |
| V. Developer Experience | ✅ PASS | Single `dotnet run --project AppHost` command; Aspire Dashboard |
| VI. Security & Privacy | ✅ PASS | User context via service; RBAC for world ownership; no hardcoded secrets |
| VII. Semantic Versioning | ✅ PASS | New API endpoint; MINOR version bump; no breaking changes |

**Gate Status**: ✅ **PASS** - No violations. Proceed to implementation.

## Project Structure

### Documentation (this feature)

```text
specs/011-soft-delete-entities/
├── plan.md              # This file
├── research.md          # Technology decisions and existing code analysis
├── data-model.md        # Entity changes and new interfaces
├── quickstart.md        # Testing guide
├── contracts/
│   └── delete-entity.md # OpenAPI specification
└── checklists/
    └── requirements.md  # Requirements checklist
```

### Source Code (repository root)

```text
libris-maleficarum-service/
├── src/
│   ├── Api/
│   │   ├── Controllers/
│   │   │   ├── WorldEntitiesController.cs    # MODIFY: DELETE → 202 + Location header
│   │   │   └── DeleteOperationsController.cs # NEW: Status polling endpoints
│   │   └── appsettings.json                  # ADD: DeleteOperation config section
│   ├── Domain/
│   │   ├── Entities/
│   │   │   ├── WorldEntity.cs                # MODIFY: Add DeletedDate, DeletedBy
│   │   │   ├── DeleteOperation.cs            # NEW: Operation tracking entity
│   │   │   └── DeleteOperationStatus.cs      # NEW: Status enum
│   │   ├── Exceptions/
│   │   │   └── RateLimitExceededException.cs # NEW: Rate limit exception
│   │   └── Interfaces/
│   │       ├── Repositories/
│   │       │   ├── IWorldEntityRepository.cs      # MODIFY: Update DeleteAsync signature
│   │       │   └── IDeleteOperationRepository.cs  # NEW: Operation persistence
│   │       └── Services/
│   │           └── IDeleteService.cs              # NEW: Delete orchestration
│   ├── Infrastructure/
│   │   ├── Repositories/
│   │   │   ├── WorldEntityRepository.cs           # MODIFY: Cascade delete logic
│   │   │   └── DeleteOperationRepository.cs       # NEW: Cosmos persistence
│   │   ├── Services/
│   │   │   └── DeleteService.cs                   # NEW: Orchestrates delete flow
│   │   └── Processors/
│   │       └── DeleteOperationProcessor.cs        # NEW: Background IHostedService
│   └── Orchestration/
│       └── AppHost/
│           └── Program.cs                         # NO CHANGE (auto-discovers services)
└── tests/
    ├── unit/
    │   ├── Domain/
    │   │   ├── WorldEntitySoftDeleteTests.cs      # NEW: Entity method tests
    │   │   └── DeleteOperationTests.cs            # NEW: Operation entity tests
    │   └── Api/
    │       └── DeleteOperationsControllerTests.cs # NEW: Controller unit tests
    └── integration/
        └── Api/
            └── SoftDeleteIntegrationTests.cs      # NEW: End-to-end delete flow
```

**Structure Decision**: Follows existing Clean Architecture layout. New files added to appropriate layers. No new projects required.

## Complexity Tracking

> **No violations to justify.** Constitution check passed with no complexity warnings.

| Aspect | Complexity | Justification |
|--------|------------|---------------|
| DeleteOperation entity | Low | Simple POCO with status enum; reuses WorldEntity container |
| Background processor | Medium | Standard IHostedService pattern; well-documented in .NET |
| All-async API | Low | Consistent contract; simpler than conditional sync/async logic |

## Implementation Phases

### Phase 1: Entity & Repository Layer

1. Add `DeletedDate` and `DeletedBy` to `WorldEntity`
2. Create `DeleteOperation` entity and `DeleteOperationStatus` enum
3. Create `IDeleteOperationRepository` interface and implementation
4. Update `IWorldEntityRepository.DeleteAsync()` signature

### Phase 2: Service Layer

1. Create `IDeleteService` interface
2. Implement `DeleteService` with operation creation and cascading logic
3. Add rate limiting check (5 concurrent operations per user per world)

### Phase 3: API Layer

1. Update `WorldEntitiesController.DeleteEntity()` to return 202 + Location
2. Create `DeleteOperationsController` with GET endpoints
3. Add 429 response handling for rate limit

### Phase 4: Background Processing

1. Create `DeleteOperationProcessor` as `IHostedService`
2. Implement cascade delete logic with progress updates
3. Handle checkpoint resume on processor restart

### Phase 5: Testing

1. Unit tests for entity methods and controller logic
2. Integration tests for full delete flow with Cosmos emulator
3. Rate limiting and concurrency tests

## Dependencies (External)

| Dependency | Purpose | Version |
|------------|---------|---------|
| Microsoft.Azure.Cosmos | Cosmos SDK for Change Feed | 3.x |
| Microsoft.Extensions.Hosting | IHostedService | Built-in |
| OpenTelemetry | Structured logging | Existing |

## Out of Scope

- Azure AI Search index updates (noted as future feature per spec assumption)
- Restore/undo functionality (separate feature per spec)
- Change Feed processor for DeletedWorldEntity TTL migration (separate infra)
- Cancel in-progress operation (per spec edge case decision)
