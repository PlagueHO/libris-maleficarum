# Implementation Tasks: Schema-Driven Entity Properties (Phase 1)

**Plan**: [plan.md](plan.md)  
**Spec**: [spec.md](spec.md)  
**Status**: Not Started

## Phase 1: Setup & Foundational Components

**Goal**: Establish schema infrastructure and core field renderer that all user stories depend on.

**Independent Test Criteria**: TypeScript compiles successfully; PropertyFieldSchema interface complete; all 5 field types render correctly in isolation.

### Tasks

- [ ] T001 [P] Define PropertyFieldSchema interface in libris-maleficarum-app/src/services/config/entityTypeRegistry.ts
- [ ] T002 [P] Define PropertyFieldValidation interface in libris-maleficarum-app/src/services/config/entityTypeRegistry.ts
- [ ] T003 Extend EntityTypeConfig interface with optional propertySchema array in libris-maleficarum-app/src/services/config/entityTypeRegistry.ts
- [ ] T004 [P] Add propertySchema to GeographicRegion type in ENTITY_TYPE_REGISTRY in libris-maleficarum-app/src/services/config/entityTypeRegistry.ts
- [ ] T005 [P] Add propertySchema to PoliticalRegion type in ENTITY_TYPE_REGISTRY in libris-maleficarum-app/src/services/config/entityTypeRegistry.ts
- [ ] T006 [P] Add propertySchema to CulturalRegion type in ENTITY_TYPE_REGISTRY in libris-maleficarum-app/src/services/config/entityTypeRegistry.ts
- [ ] T007 [P] Add propertySchema to MilitaryRegion type in ENTITY_TYPE_REGISTRY in libris-maleficarum-app/src/services/config/entityTypeRegistry.ts
- [ ] T008 [P] Create propertyValidation utility with validateField function in libris-maleficarum-app/src/lib/validators/propertyValidation.ts
- [ ] T009 [P] Add type coercion logic for integer fields to propertyValidation in libris-maleficarum-app/src/lib/validators/propertyValidation.ts
- [ ] T010 [P] Add type coercion logic for decimal fields to propertyValidation in libris-maleficarum-app/src/lib/validators/propertyValidation.ts
- [ ] T011 Create DynamicPropertyField component skeleton in libris-maleficarum-app/src/components/MainPanel/DynamicPropertyField.tsx
- [ ] T012 [P] Implement text field type rendering in DynamicPropertyField in libris-maleficarum-app/src/components/MainPanel/DynamicPropertyField.tsx
- [ ] T013 [P] Implement textarea field type rendering in DynamicPropertyField in libris-maleficarum-app/src/components/MainPanel/DynamicPropertyField.tsx
- [ ] T014 [P] Implement integer field type rendering in DynamicPropertyField in libris-maleficarum-app/src/components/MainPanel/DynamicPropertyField.tsx
- [ ] T015 [P] Implement decimal field type rendering in DynamicPropertyField in libris-maleficarum-app/src/components/MainPanel/DynamicPropertyField.tsx
- [ ] T016 [P] Implement tagArray field type rendering in DynamicPropertyField in libris-maleficarum-app/src/components/MainPanel/DynamicPropertyField.tsx
- [ ] T017 Add duplicate tag prevention with visual feedback to TagInput in libris-maleficarum-app/src/components/shared/TagInput/TagInput.tsx
- [ ] T017a [P] Create test for duplicate tag prevention with 500ms highlight in TagInput in libris-maleficarum-app/src/components/shared/TagInput/TagInput.test.tsx

---

## Phase 2: User Story 1 - Edit Entity with Schema-Driven Properties (P1)

**Goal**: Enable editing of entities with schema-driven custom properties (GeographicRegion, CulturalRegion, etc.).

**Independent Test Criteria**: User can open GeographicRegion for editing, modify Climate field, save, and changes persist. All 4 Regional entity types render correctly in edit mode.

### Tests

- [ ] T018 [US1] Create unit tests for DynamicPropertyField component in libris-maleficarum-app/src/components/MainPanel/__tests__/DynamicPropertyField.test.tsx
- [ ] T019 [US1] Create accessibility tests for DynamicPropertyField using jest-axe in libris-maleficarum-app/src/components/MainPanel/__tests__/DynamicPropertyField.test.tsx

### Implementation

- [ ] T020 [US1] Create DynamicPropertiesForm component in libris-maleficarum-app/src/components/MainPanel/DynamicPropertiesForm.tsx
- [ ] T021 [US1] Implement schema field iteration in DynamicPropertiesForm in libris-maleficarum-app/src/components/MainPanel/DynamicPropertiesForm.tsx
- [ ] T022 [US1] Add section header rendering based on entity type in DynamicPropertiesForm (format: entity type label + ' Properties', e.g., 'Geographic Properties') in libris-maleficarum-app/src/components/MainPanel/DynamicPropertiesForm.tsx
- [ ] T023 [US1] Update WorldEntityForm to use DynamicPropertiesForm in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.tsx
- [ ] T024 [US1] Remove renderCustomProperties switch statement from WorldEntityForm in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.tsx
- [ ] T025 [US1] Remove custom property component imports from WorldEntityForm in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.tsx
- [ ] T026 [US1] Add empty/undefined property filtering on save in WorldEntityForm in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.tsx
- [ ] T027 [US1] Create unit tests for DynamicPropertiesForm in libris-maleficarum-app/src/components/MainPanel/__tests__/DynamicPropertiesForm.test.tsx
- [ ] T028 [US1] Update WorldEntityForm tests to use DynamicPropertiesForm expectations in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.test.tsx
- [ ] T029 [US1] Create integration test for editing GeographicRegion entity in libris-maleficarum-app/src/__tests__/integration/editEntityWithProperties.test.tsx

---

## Phase 3: User Story 2 - View Entity with Schema-Driven Properties (P1)

**Goal**: Enable read-only viewing of entities with formatted custom properties display.

**Independent Test Criteria**: User can view PoliticalRegion entity; GovernmentType displays as text; MemberStates displays as badges; Population displays with thousand separators.

### Implementation

- [ ] T030 [US2] Create DynamicPropertiesView component in libris-maleficarum-app/src/components/MainPanel/DynamicPropertiesView.tsx
- [ ] T031 [US2] Implement schema-based formatted value rendering in DynamicPropertiesView in libris-maleficarum-app/src/components/MainPanel/DynamicPropertiesView.tsx
- [ ] T032 [US2] Add fallback to generic Object.entries renderer when schema missing in DynamicPropertiesView in libris-maleficarum-app/src/components/MainPanel/DynamicPropertiesView.tsx
- [ ] T033 [US2] Implement numeric value formatting with thousand separators in DynamicPropertiesView in libris-maleficarum-app/src/components/MainPanel/DynamicPropertiesView.tsx
- [ ] T034 [US2] Implement tagArray rendering as badges in DynamicPropertiesView in libris-maleficarum-app/src/components/MainPanel/DynamicPropertiesView.tsx
- [ ] T035 [US2] Update EntityDetailReadOnlyView to use DynamicPropertiesView in libris-maleficarum-app/src/components/MainPanel/EntityDetailReadOnlyView.tsx
- [ ] T036 [US2] Create unit tests for DynamicPropertiesView in libris-maleficarum-app/src/components/MainPanel/__tests__/DynamicPropertiesView.test.tsx
- [ ] T037 [US2] Update EntityDetailReadOnlyView tests to use DynamicPropertiesView expectations in libris-maleficarum-app/src/components/MainPanel/EntityDetailReadOnlyView.test.tsx
- [ ] T038 [US2] Create integration test for viewing PoliticalRegion entity in libris-maleficarum-app/src/__tests__/integration/viewEntityWithProperties.test.tsx

---

## Phase 4: User Story 3 - Create Entity with Schema-Driven Properties (P2)

**Goal**: Enable creation of new entities with schema-driven custom properties.

**Independent Test Criteria**: User can create new MilitaryRegion entity; form displays empty property fields; properties save correctly.

### Tests

- [ ] T039 [US3] Create integration test for creating MilitaryRegion entity in libris-maleficarum-app/src/__tests__/integration/createEntityWithProperties.test.tsx
- [ ] T040 [US3] Verify DynamicPropertiesForm displays empty fields for new entities in test

---

## Phase 5: User Story 4 - Consistent Validation Across Property Types (P2)

**Goal**: Ensure validation works consistently for all field types with inline error messages.

**Independent Test Criteria**: Invalid data in Population field shows inline error; maxLength constraints work; correcting invalid value clears error.

### Implementation

- [ ] T041 [US4] Add validation error display to DynamicPropertyField in libris-maleficarum-app/src/components/MainPanel/DynamicPropertyField.tsx
- [ ] T042 [US4] Implement required field validation in DynamicPropertyField in libris-maleficarum-app/src/components/MainPanel/DynamicPropertyField.tsx
- [ ] T043 [US4] Implement min/max validation for integer fields in DynamicPropertyField in libris-maleficarum-app/src/components/MainPanel/DynamicPropertyField.tsx
- [ ] T044 [US4] Implement min/max validation for decimal fields in DynamicPropertyField in libris-maleficarum-app/src/components/MainPanel/DynamicPropertyField.tsx
- [ ] T045 [US4] Implement pattern validation for text/textarea fields in DynamicPropertyField in libris-maleficarum-app/src/components/MainPanel/DynamicPropertyField.tsx
- [ ] T046 [US4] Add character counter for textarea/text fields with maxLength in DynamicPropertyField in libris-maleficarum-app/src/components/MainPanel/DynamicPropertyField.tsx
- [ ] T047 [US4] Prevent form submission when validation errors exist in DynamicPropertiesForm in libris-maleficarum-app/src/components/MainPanel/DynamicPropertiesForm.tsx

### Tests

- [ ] T048 [US4] Create validation test for integer field rejecting invalid input in libris-maleficarum-app/src/components/MainPanel/__tests__/DynamicPropertyField.test.tsx
- [ ] T049 [US4] Create validation test for maxLength constraint in libris-maleficarum-app/src/components/MainPanel/__tests__/DynamicPropertyField.test.tsx
- [ ] T050 [US4] Create validation test for error clearing on valid input in libris-maleficarum-app/src/components/MainPanel/__tests__/DynamicPropertyField.test.tsx

---

## Phase 6: User Story 5 - Entity Types Without Custom Properties (P3)

**Goal**: Ensure backward compatibility for entity types without propertySchema.

**Independent Test Criteria**: Character entity (no schema) displays no custom properties section in both edit and view modes.

### Tests

- [ ] T051 [US5] Create test for entity without propertySchema in edit mode in libris-maleficarum-app/src/components/MainPanel/__tests__/DynamicPropertiesForm.test.tsx
- [ ] T052 [US5] Create test for entity without propertySchema in view mode in libris-maleficarum-app/src/components/MainPanel/__tests__/DynamicPropertiesView.test.tsx
- [ ] T053 [US5] Create integration test for Character entity without custom properties in libris-maleficarum-app/src/__tests__/integration/entityWithoutProperties.test.tsx

---

## Final Phase: Polish & Cross-Cutting Concerns

**Goal**: Cleanup, migration, and final validation.

**Independent Test Criteria**: All old components deleted; 100% existing entity data displays correctly; all tests pass; no linting/type errors.

### Tasks

- [ ] T054 Verify existing GeographicRegion entities display correctly after migration
- [ ] T055 Verify existing PoliticalRegion entities display correctly after migration
- [ ] T056 Verify existing CulturalRegion entities display correctly after migration  
- [ ] T057 Verify existing MilitaryRegion entities display correctly after migration
- [ ] T057a Verify SC-002: Attempt to add new entity type with custom properties using only registry configuration (no new component files) in libris-maleficarum-app/src/services/config/entityTypeRegistry.ts
- [ ] T058 Delete GeographicRegionProperties component and test from libris-maleficarum-app/src/components/MainPanel/customProperties/
- [ ] T059 Delete PoliticalRegionProperties component and test from libris-maleficarum-app/src/components/MainPanel/customProperties/
- [ ] T060 Delete CulturalRegionProperties component and test from libris-maleficarum-app/src/components/MainPanel/customProperties/
- [ ] T061 Delete MilitaryRegionProperties component and test from libris-maleficarum-app/src/components/MainPanel/customProperties/
- [ ] T062 Delete customProperties index file from libris-maleficarum-app/src/components/MainPanel/customProperties/
- [ ] T063 Run pnpm lint and fix any linting errors in libris-maleficarum-app
- [ ] T064 Run pnpm type-check and fix any TypeScript errors in libris-maleficarum-app
- [ ] T065 Run pnpm test and ensure all tests pass in libris-maleficarum-app
- [ ] T066 Run pnpm find-deadcode and remove any detected dead code in libris-maleficarum-app

---

## Dependencies

**Dependency Graph (User Story Completion Order)**:

1. **Phase 1** (Foundational) → MUST complete before any user stories
2. **Phase 2** (US1) + **Phase 3** (US2) → Can be developed in parallel after Phase 1
3. **Phase 4** (US3) → Depends on US1 completion (reuses edit logic)
4. **Phase 5** (US4) → Depends on Phase 1 (enhances field renderer)
5. **Phase 6** (US5) → Can be developed in parallel with US1-4 (independent test case)
6. **Final Phase** → Depends on all user stories being complete

## Parallel Execution Examples

**Per User Story**:

- **Phase 1**: Tasks T001-T002, T004-T007, T008-T010, T012-T016 can run in parallel
- **US1 (Phase 2)**: Tasks T018-T019 (tests) can run parallel to T020-T026 (implementation)
- **US2 (Phase 3)**: Can start in parallel with US1 after Phase 1 complete
- **US4 (Phase 5)**: Tasks T041-T046 can run in parallel
- **Final Phase**: Migration verification tasks T054-T057 can run in parallel

## Implementation Strategy

**MVP-First Approach**:

- **MVP = US1 + US2 completed**: Schema-driven edit and view for existing Regional entity types
- Next increment: US3 (create flow) + US4 (validation)
- Polish: US5 (backward compat) + Final Phase (cleanup)

**Recommended Execution Order**:

1. Complete Phase 1 (foundational infrastructure)
2. Implement US1 (P1 - edit) and US2 (P1 - view) in parallel
3. Add US4 (P2 - validation) to enhance field rendering
4. Add US3 (P2 - create flow) leveraging edit logic
5. Add US5 (P3 - backward compat) as edge case coverage
6. Execute Final Phase (cleanup and migration)

---

**Total Tasks**: 68  
**By Phase**:
- Phase 1 (Setup): 18 tasks
- Phase 2 (US1): 12 tasks
- Phase 3 (US2): 9 tasks
- Phase 4 (US3): 2 tasks
- Phase 5 (US4): 10 tasks
- Phase 6 (US5): 3 tasks
- Final Phase: 14 tasks

**By Priority**:
- P1 (US1 + US2): 21 tasks
- P2 (US3 + US4): 12 tasks
- P3 (US5): 3 tasks
- Foundation: 17 tasks
- Polish: 13 tasks
