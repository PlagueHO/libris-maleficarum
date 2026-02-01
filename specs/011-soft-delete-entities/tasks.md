# Tasks: Soft Delete World Entities API

**Input**: Design documents from `/specs/011-soft-delete-entities/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/delete-entity.md

**Tests**: Unit and integration tests included per Constitution Principle III (Test-Driven Development).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `libris-maleficarum-service/src/` with Api, Domain, Infrastructure layers
- **Tests**: `libris-maleficarum-service/tests/` with unit/, integration/ folders

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and core entity modifications

- [X] T001 Add DeletedDate and DeletedBy properties to WorldEntity in libris-maleficarum-service/src/Domain/Entities/WorldEntity.cs
- [X] T001.1 Add Ttl property to WorldEntity for Cosmos DB automatic cleanup (90 days)
- [X] T002 Update SoftDelete() method to accept deletedBy parameter and set Ttl to 7776000 seconds in libris-maleficarum-service/src/Domain/Entities/WorldEntity.cs
- [X] T002.1 Add Restore() method to clear deletion metadata and TTL in WorldEntity
- [X] T003 [P] Create DeleteOperationStatus enum in libris-maleficarum-service/src/Domain/Entities/DeleteOperationStatus.cs
- [X] T004 [P] Create DeleteOperation entity in libris-maleficarum-service/src/Domain/Entities/DeleteOperation.cs
- [X] T005 [P] Create RateLimitExceededException in libris-maleficarum-service/src/Domain/Exceptions/RateLimitExceededException.cs
- [X] T006 Add DeleteOperation configuration section to libris-maleficarum-service/src/Api/appsettings.json
- [X] T006.1 Configure Ttl property mapping in WorldEntityConfiguration (maps to "ttl" JSON property)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core interfaces and repository infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T007 Create IDeleteOperationRepository interface in libris-maleficarum-service/src/Domain/Interfaces/Repositories/IDeleteOperationRepository.cs
- [X] T008 Create IDeleteService interface in libris-maleficarum-service/src/Domain/Interfaces/Services/IDeleteService.cs
- [X] T009 Update IWorldEntityRepository.DeleteAsync() signature to include deletedBy parameter in libris-maleficarum-service/src/Domain/Interfaces/Repositories/IWorldEntityRepository.cs
- [X] T010 Implement DeleteOperationRepository in libris-maleficarum-service/src/Infrastructure/Repositories/DeleteOperationRepository.cs
- [X] T011 Add CountActiveByUserAsync method to DeleteOperationRepository for rate limiting
- [X] T012 Register DeleteOperationRepository in DI container in libris-maleficarum-service/src/Api/Program.cs or DI configuration

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Delete Single Entity (Priority: P1) 🎯 MVP

**Goal**: User can delete a single entity and receive 202 Accepted with a status polling endpoint

**Independent Test**: Call DELETE on entity with no children, receive 202 with Location header, poll status until completed

### Tests for User Story 1

- [X] T013 [P] [US1] Create unit tests for WorldEntity.SoftDelete() method in libris-maleficarum-service/tests/unit/Domain/WorldEntitySoftDeleteTests.cs
- [X] T013.1 Add unit tests for WorldEntity.Restore() method to verify TTL clearing
- [X] T013.2 Add unit test for Ttl property initialization (null for new entities)
- [X] T014 [P] [US1] Create unit tests for DeleteOperation entity methods in libris-maleficarum-service/tests/unit/Domain/DeleteOperationTests.cs
- [X] T015 [P] [US1] Create unit tests for DeleteOperationsController in libris-maleficarum-service/tests/unit/Api/DeleteOperationsControllerTests.cs

### Implementation for User Story 1

- [X] T016 [US1] Implement DeleteService.InitiateDeleteAsync() for single entity (no cascade) in libris-maleficarum-service/src/Infrastructure/Services/DeleteService.cs
- [X] T017 [US1] Add rate limit check to DeleteService.InitiateDeleteAsync() (max 5 concurrent per user per world)
- [X] T018 [US1] Update WorldEntitiesController.DeleteEntity() to return 202 Accepted with Location header in libris-maleficarum-service/src/Api/Controllers/WorldEntitiesController.cs
- [X] T019 [US1] Create DeleteOperationsController with GET /{operationId} endpoint in libris-maleficarum-service/src/Api/Controllers/DeleteOperationsController.cs
- [X] T020 [US1] Add 429 Too Many Requests response handling for rate limit exceeded in WorldEntitiesController
- [X] T021 [US1] Register DeleteService in DI container
- [X] T022 [US1] Add OpenTelemetry structured logging for delete operation initiation

### Integration Tests for User Story 1

- [X] T023 [US1] Create integration test for single entity delete flow in libris-maleficarum-service/tests/integration/Api/SoftDeleteIntegrationTests.cs
- [X] T024 [US1] Create integration test for 404 on non-existent entity
- [X] T025 [US1] Create integration test for 403 on unauthorized world access

**Checkpoint**: User Story 1 complete - single entity delete with status polling works independently

**Note**: Tests (T013-T015, T023-T025) ✅ **COMPLETED AND PASSING** - All 311 unit tests passing (Domain: 91, Api: 146, Infrastructure: 74), integration test passing (DELETE returns 202 Accepted). Ready for CI/merge.

---

## Phase 4: User Story 2 - Delete Entity with Cascade (Priority: P2)

**Goal**: User can delete a parent entity and all descendants are automatically soft-deleted

**Independent Test**: Create parent with nested children, delete parent, verify all descendants marked as deleted

### Tests for User Story 2

- [X] T026 [P] [US2] Create unit tests for cascade delete logic in DeleteService in libris-maleficarum-service/tests/unit/Infrastructure/DeleteServiceCascadeTests.cs
- [X] T027 [P] [US2] Create unit tests for DeleteOperationProcessor in libris-maleficarum-service/tests/unit/Infrastructure/DeleteOperationProcessorTests.cs

### Implementation for User Story 2

- [X] T028 [US2] Create DeleteOperationProcessor as IHostedService in libris-maleficarum-service/src/Infrastructure/Processors/DeleteOperationProcessor.cs
- [X] T029 [US2] Implement cascade delete discovery logic (query descendants by ParentId recursively)
- [X] T030 [US2] Implement batch soft-delete with progress updates in DeleteOperationProcessor
- [X] T031 [US2] Update WorldEntityRepository.DeleteAsync() to support cascade parameter in libris-maleficarum-service/src/Infrastructure/Repositories/WorldEntityRepository.cs
- [X] T032 [US2] Implement DeleteService.ProcessDeleteAsync() for background processing
- [X] T033 [US2] Register DeleteOperationProcessor as hosted service in DI container
- [X] T034 [US2] Add OpenTelemetry logging for cascade delete with entity count

### Integration Tests for User Story 2

- [X] T035 [US2] Create integration test for cascade delete on parent with direct children
- [X] T036 [US2] Create integration test for cascade delete on deeply nested hierarchy (3+ levels)
- [X] T037 [US2] Create integration test for idempotent delete (already-deleted entity returns 202 with count=0)

**Checkpoint**: User Story 2 complete - cascade delete works independently

---

## Phase 5: User Story 3 - Monitor Delete Progress (Priority: P3)

**Goal**: User can monitor progress of large delete operations via status endpoint

**Independent Test**: Initiate cascade delete on 50+ entity hierarchy, poll status until complete, verify progress updates

### Tests for User Story 3

- [X] T038 [P] [US3] Create unit tests for DeleteOperation.UpdateProgress() and Complete() methods
- [X] T039 [P] [US3] Create unit tests for list operations endpoint in DeleteOperationsController

### Implementation for User Story 3

- [X] T040 [US3] Add GET /delete-operations endpoint to list recent operations in DeleteOperationsController
- [X] T041 [US3] Implement real-time progress updates in DeleteOperationProcessor (update every N entities)
- [X] T042 [US3] Implement checkpoint resume on processor restart (query in-progress operations on startup)
- [X] T043 [US3] Add failedEntityIds tracking to DeleteOperation for partial failure reporting
- [X] T044 [US3] Implement TTL auto-cleanup for DeleteOperation (24-hour expiry via Cosmos TTL)

### Integration Tests for User Story 3

- [X] T045 [US3] Create integration test for progress polling on large hierarchy (50+ entities)
- [X] T046 [US3] Create integration test for partial failure scenario
- [X] T047 [US3] Create integration test for list recent operations endpoint

**Checkpoint**: User Story 3 complete - progress monitoring works independently

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [X] T048 [P] Add XML documentation comments to all public APIs
- [X] T049 [P] Update OpenAPI/Swagger documentation for new endpoints
- [X] T050 Verify all queries filter IsDeleted=false (FR-003, FR-017)
- [X] T051 Run quickstart.md validation scenarios manually
- [X] T052 [P] Create rate limiting concurrency tests in libris-maleficarum-service/tests/integration/Api/RateLimitingTests.cs
- [X] T053 Performance test: verify DELETE returns 202 in <200ms
- [X] T054 Performance test: verify 500+ entity cascade completes in <60s

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 completion - BLOCKS all user stories
- **Phase 3-5 (User Stories)**: All depend on Phase 2 completion
  - User stories can proceed sequentially P1 → P2 → P3
  - Or in parallel if team capacity allows (after Phase 2)
- **Phase 6 (Polish)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Phase 2 - No dependencies on other stories
- **User Story 2 (P2)**: Uses DeleteService from US1, extends with cascade logic
- **User Story 3 (P3)**: Uses DeleteOperation from US1, adds progress tracking

### Within Each User Story

- Tests SHOULD be written first and FAIL before implementation (TDD per Constitution)
- Models/entities before services
- Services before controllers
- Core implementation before integration tests
- Story complete before moving to next priority

### Parallel Opportunities

```text
Phase 1 parallel batch:
  T003, T004, T005  (enum, entity, exception - different files)

Phase 2 sequential:
  T007 → T008 → T009 → T010 → T011 → T012

User Story 1 test batch:
  T013, T014, T015  (all tests can be written in parallel)

User Story 2 test batch:
  T026, T027  (cascade tests in parallel)

User Story 3 test batch:
  T038, T039  (progress tests in parallel)

Phase 6 parallel batch:
  T048, T049, T052  (docs and rate limit tests)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T006)
1. Complete Phase 2: Foundational (T007-T012)
1. Complete Phase 3: User Story 1 (T013-T025)
1. **STOP and VALIDATE**: Test single entity delete independently
1. Deploy/demo if ready - user can delete single entities

### Incremental Delivery

1. Phase 1 + Phase 2 → Foundation ready
1. Add User Story 1 → Test → **MVP deployed!** (single entity delete)
1. Add User Story 2 → Test → **Cascade delete works**
1. Add User Story 3 → Test → **Progress monitoring works**
1. Phase 6 → **Production ready**

### Task Count Summary

| Phase | Tasks | Parallel Opportunities |
|-------|-------|----------------------|
| Setup | 6 | 3 |
| Foundational | 6 | 0 (sequential) |
| User Story 1 | 13 | 3 |
| User Story 2 | 12 | 2 |
| User Story 3 | 10 | 2 |
| Polish | 7 | 3 |
| **Total** | **54** | **13** |

---

## Notes

- All tasks follow strict checklist format: `- [ ] [ID] [P?] [Story?] Description with file path`
- [P] tasks can run in parallel (different files, no dependencies)
- [US1/US2/US3] labels map tasks to specific user stories
- Each user story is independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- TDD: Write tests first, verify they fail, then implement
