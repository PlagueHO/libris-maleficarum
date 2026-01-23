# Tasks: Consolidate Entity Type Metadata into Single Registry

**Input**: Design documents from `/specs/007-entity-type-registry/`  
**Prerequisites**: plan.md ✅, spec.md ✅  
**Branch**: `007-entity-type-registry`  
**Related Issue**: [#90](https://github.com/PlagueHO/libris-maleficarum/issues/90)

**Tests**: Tests are REQUIRED per spec User Story 4 (validation tests) and User Story 5 (helper function tests)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4, US5)
- Include exact file paths in descriptions

## Path Conventions

All paths relative to `libris-maleficarum-app/` directory (React frontend).

---

## Phase 1: Setup (Documentation)

**Purpose**: Create design documentation and update agent context

- [X] T001 [P] Create data-model.md documenting EntityTypeConfig interface in specs/007-entity-type-registry/data-model.md
- [X] T002 [P] Create contracts/EntityTypeConfig.schema.json with JSON schema in specs/007-entity-type-registry/contracts/EntityTypeConfig.schema.json
- [X] T003 [P] Create quickstart.md developer guide in specs/007-entity-type-registry/quickstart.md
- [X] T004 Update Copilot agent context with registry patterns per plan.md §1.4 via .specify/scripts/powershell/update-agent-context.ps1

**Checkpoint**: Documentation complete - implementation can begin

---

## Phase 2: User Story 1 - Single Source Definition (Priority: P1) 🎯 MVP

**Goal**: Consolidate all 35 entity type definitions into single ENTITY_TYPE_REGISTRY array

**Independent Test**: Add new entity type "Kingdom" to registry and verify all derived constants automatically include it without manual updates elsewhere

### Implementation for User Story 1

- [X] T005 [US1] Create EntityTypeConfig interface in src/services/config/entityTypeRegistry.ts
- [X] T006 [US1] Create ENTITY_TYPE_REGISTRY array with all 35 entity types in src/services/config/entityTypeRegistry.ts
- [X] T007 [US1] Export registry as readonly array with const assertion in src/services/config/entityTypeRegistry.ts
- [X] T008 [US1] Verify TypeScript compilation succeeds with new registry file

**Checkpoint**: Registry exists with complete metadata for all 35 entity types

---

## Phase 3: User Story 2 - Type-Safe Access (Priority: P1)

**Goal**: Derive all constants (WorldEntityType, ENTITY_SCHEMA_VERSIONS, ENTITY_TYPE_META, ENTITY_TYPE_SUGGESTIONS) from registry with strong typing

**Independent Test**: TypeScript provides autocomplete for WorldEntityType and catches invalid type access at compile time

### Implementation for User Story 2

- [X] T009 [US2] Import ENTITY_TYPE_REGISTRY into src/services/types/worldEntity.types.ts
- [X] T010 [US2] Derive WorldEntityType const from registry using Object.fromEntries in src/services/types/worldEntity.types.ts
- [X] T011 [US2] Derive ENTITY_SCHEMA_VERSIONS as Record<WorldEntityType, number> in src/services/types/worldEntity.types.ts
- [X] T012 [US2] Derive ENTITY_TYPE_META as Record<WorldEntityType, EntityTypeMeta> in src/services/types/worldEntity.types.ts
- [X] T013 [US2] Derive ENTITY_TYPE_SUGGESTIONS as Record<WorldEntityType, WorldEntityType[]> in src/services/types/worldEntity.types.ts
- [X] T014 [US2] Verify all derived constants have correct Record<WorldEntityType, ...> types in src/services/types/worldEntity.types.ts
- [X] T015 [US2] Verify TypeScript compilation succeeds with zero errors

**Checkpoint**: All constants derived from registry with strong typing maintained

---

## Phase 4: User Story 3 - Backward Compatibility (Priority: P1)

**Goal**: Ensure all existing code continues to work without changes after refactor

**Independent Test**: Run full test suite and verify 100% pass rate without modifying any test files

### Implementation for User Story 3

- [X] T016 [P] [US3] Update import in src/services/worldEntityApi.ts from entitySchemaVersions to worldEntity.types
- [X] T017 [P] [US3] Update usage from getSchemaVersion(entityType) to ENTITY_SCHEMA_VERSIONS[entityType] in src/services/worldEntityApi.ts
- [X] T018 [P] [US3] Update import in src/components/MainPanel/EntityDetailForm.tsx from entitySchemaVersions to config/entityTypeRegistry
- [X] T019 [P] [US3] Update usage from getSchemaVersion(entityType) to getEntityTypeConfig(entityType)?.schemaVersion in src/components/MainPanel/EntityDetailForm.tsx
- [X] T020 [US3] Delete src/services/constants/entitySchemaVersions.ts file
- [X] T021 [US3] Verify TypeScript compilation succeeds with zero references to deleted file
- [X] T022 [US3] Run full test suite (pnpm test) and verify 100% pass rate

**Checkpoint**: Backward compatibility verified - all existing code works without changes

---

## Phase 5: User Story 4 - Comprehensive Validation (Priority: P2)

**Goal**: Automated tests validate registry completeness and correctness

**Independent Test**: Registry validation tests catch configuration errors (duplicate types, invalid versions, missing entries)

### Tests for User Story 4 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation (TDD)**

- [X] T023 [P] [US4] Create validation test for unique type identifiers in src/**tests**/services/entityTypeRegistry.test.ts
- [X] T024 [P] [US4] Create validation test for schema versions >= 1 in src/**tests**/services/entityTypeRegistry.test.ts
- [X] T025 [P] [US4] Create validation test for valid icon names (PascalCase) in src/**tests**/services/entityTypeRegistry.test.ts
- [X] T026 [P] [US4] Create validation test for completeness (all 29 types present) in src/**tests**/services/entityTypeRegistry.test.ts
- [X] T027 [P] [US4] Create validation test for no circular suggestions in src/**tests**/services/entityTypeRegistry.test.ts

### Implementation for User Story 4

- [X] T028 [US4] Verify all validation tests pass at 100% success rate
- [X] T029 [US4] Run test suite (pnpm test) and confirm validation coverage

**Checkpoint**: Registry validation tests provide comprehensive error detection

---

## Phase 6: User Story 5 - Helper Functions (Priority: P2)

**Goal**: Provide helper functions (getEntityTypeConfig, getRootEntityTypes, getAllEntityTypes) to query registry programmatically

**Independent Test**: Helper functions return correct data for all test cases with proper typing

### Tests for User Story 5 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation (TDD)**

- [X] T030 [P] [US5] Create test for getEntityTypeConfig() valid type in src/**tests**/config/entityTypeRegistry.test.ts
- [X] T031 [P] [US5] Create test for getEntityTypeConfig() invalid type returns undefined in src/**tests**/config/entityTypeRegistry.test.ts
- [X] T032 [P] [US5] Create test for getRootEntityTypes() returns only canBeRoot types in src/**tests**/config/entityTypeRegistry.test.ts
- [X] T033 [P] [US5] Create test for getAllEntityTypes() returns complete registry in src/**tests**/config/entityTypeRegistry.test.ts

### Implementation for User Story 5

- [X] T034 [US5] Implement getEntityTypeConfig(type) function in src/services/config/entityTypeRegistry.ts
- [X] T035 [US5] Implement getRootEntityTypes() function in src/services/config/entityTypeRegistry.ts
- [X] T036 [US5] Implement getAllEntityTypes() function in src/services/config/entityTypeRegistry.ts
- [X] T037 [US5] Export all helper functions with JSDoc comments in src/services/config/entityTypeRegistry.ts
- [X] T038 [US5] Verify all helper function tests pass at 100% success rate
- [X] T039 [US5] Verify TypeScript provides correct return type inference for helper functions

**Checkpoint**: Helper functions implemented and fully tested

---

## Phase 7: Polish & Verification

**Purpose**: Final validation and quality checks

- [X] T040 [P] Run full test suite (pnpm test) and verify 100% pass rate
- [X] T041 [P] Run TypeScript compilation and verify zero errors (pnpm build)
- [X] T042 [P] Run ESLint and verify zero warnings (pnpm lint)
- [X] T043 [P] Verify test coverage maintains or improves baseline (pnpm test -- --coverage)
- [X] T044 Verify dev server starts successfully (pnpm dev)
- [ ] T045 Manually test entity creation in browser to verify ENTITY_SCHEMA_VERSIONS integration
- [ ] T046 Complete Phase 3 verification checklist in plan.md (all FR and SC items)
- [ ] T047 Run quickstart.md validation (add new entity type and verify it works)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **User Story 1 (Phase 2)**: Depends on Setup completion
- **User Story 2 (Phase 3)**: Depends on User Story 1 (needs registry to derive from)
- **User Story 3 (Phase 4)**: Depends on User Story 2 (needs derived constants to update imports)
- **User Story 4 (Phase 5)**: Depends on User Story 1 (needs registry to validate)
- **User Story 5 (Phase 6)**: Depends on User Story 1 (needs registry to query)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: FOUNDATIONAL - All other stories depend on this
- **User Story 2 (P1)**: Depends on US1 - needs registry to derive constants from
- **User Story 3 (P1)**: Depends on US2 - needs derived constants to be backward compatible
- **User Story 4 (P2)**: Depends on US1 - independently tests registry
- **User Story 5 (P2)**: Depends on US1 - independently provides helper functions

### Within Each User Story

**User Story 1**:

- T005 → T006 → T007 → T008 (sequential)

**User Story 2**:

- T009 → T010, T011, T012, T013 (parallel after import) → T014 → T015

**User Story 3**:

- T016-T019 can run in parallel (different files)
- T020 → T021 → T022 (sequential)

**User Story 4**:

- T023-T027 can run in parallel (all test creation)
- T028 → T029 (sequential verification)

**User Story 5**:

- T030-T033 can run in parallel (all test creation)
- T034-T036 can run in parallel (different functions)
- T037 → T038 → T039 (sequential verification)

**Polish**:

- T040-T043 can run in parallel (different validation commands)
- T044 → T045 → T046 → T047 (sequential verification)

### Parallel Opportunities

**Setup (Phase 1)**: T001, T002, T003 can run in parallel

**User Story 2**: T010, T011, T012, T013 can run in parallel after T009

**User Story 3**: T016-T017 parallel, T018-T019 parallel

**User Story 4 Tests**: T023-T027 can all run in parallel

**User Story 5 Tests**: T030-T033 can all run in parallel  
**User Story 5 Impl**: T034-T036 can all run in parallel

**Polish**: T040-T043 can all run in parallel

---

## Parallel Example: User Story 2

```bash
# After T009 (import registry), launch these in parallel:
Task T010: "Derive WorldEntityType const from registry"
Task T011: "Derive ENTITY_SCHEMA_VERSIONS as Record"
Task T012: "Derive ENTITY_TYPE_META as Record"
Task T013: "Derive ENTITY_TYPE_SUGGESTIONS as Record"

# Then verify sequentially:
Task T014: "Verify all derived constants have correct types"
Task T015: "Verify TypeScript compilation succeeds"
```

---

## Parallel Example: User Story 4 (Tests)

```bash
# All validation tests can be written in parallel:
Task T023: "Test for unique type identifiers"
Task T024: "Test for schema versions >= 1"
Task T025: "Test for valid icon names"
Task T026: "Test for completeness (29 types)"
Task T027: "Test for no circular suggestions"

# Then verify:
Task T028: "Verify all validation tests pass"
```

---

## Implementation Strategy

### MVP First (User Stories 1-3 Only)

1. Complete Phase 1: Setup (documentation)
1. Complete Phase 2: User Story 1 (create registry) ← FOUNDATION
1. Complete Phase 3: User Story 2 (derive constants)
1. Complete Phase 4: User Story 3 (backward compatibility)
1. **STOP and VALIDATE**: Run full test suite, verify 100% pass
1. Deploy/demo - MVP delivers single source of truth with backward compatibility

### Incremental Delivery

1. MVP (US1-3) → Test independently → Merge to main (core value delivered)
1. Add US4 (validation) → Test independently → Merge (quality gates added)
1. Add US5 (helpers) → Test independently → Merge (DX improved)
1. Polish → Final validation → Complete

### Parallel Team Strategy

With 2 developers after completing User Story 1:

1. Developer A: User Story 2 + User Story 3
1. Developer B: User Story 4 (validation tests)
1. Then Developer A: User Story 5 (helpers)
1. Both: Polish together

Or sequential (single developer):

1. US1 → US2 → US3 (2-3 hours) → VALIDATE
1. US4 (1 hour)
1. US5 (1.25 hours)
1. Polish (1 hour)

Total: ~6-7 hours for implementation (excluding Phase 1 docs)

---

## Notes

- **TDD Required**: Tests for US4 and US5 must be written FIRST (per Constitution Principle III)
- **Accessibility**: Not applicable (no UI component changes)
- **[P] tasks**: Different files, no dependencies - can run in parallel
- **[Story] label**: Maps task to specific user story for traceability
- **File paths**: All relative to `libris-maleficarum-app/` directory
- **Backward compatibility**: US3 is critical - must maintain 100% compatibility
- **Verification**: After each phase checkpoint, verify independently
- **Commit strategy**: Commit after each task or phase completion
- **Rollback**: If issues found, US1-3 are self-contained and can be reverted as unit

---

## Success Criteria Checklist

After completing all tasks, verify these from spec.md:

- [ ] **SC-001**: Can add entity type with one registry entry
- [ ] **SC-002**: All 35 entity types in registry
- [ ] **SC-003**: TypeScript zero errors
- [ ] **SC-004**: 100% existing test pass rate
- [ ] **SC-005**: ESLint zero warnings
- [ ] **SC-006**: Validation tests 100% pass
- [ ] **SC-007**: Zero references to entitySchemaVersions.ts
- [ ] **SC-008**: Single file entityTypeRegistry.ts contains all definitions
- [ ] **SC-009**: Helper functions 100% test coverage
- [ ] **SC-010**: Registry is JSON-serializable
