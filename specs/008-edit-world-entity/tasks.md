# Tasks: World Entity Editing

**Feature**: Enable editing of existing world entities via hierarchy edit icon and detail view edit button  
**Branch**: `008-edit-world-entity`  
**Input**: Design documents from `specs/008-edit-world-entity/`

## Format: `- [ ] [ID] [P?] [Story?] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story identifier (US1, US2, US3) - required for user story phases only
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and component structure preparation

- [X] T001 Create feature branch `008-edit-world-entity` from main
- [X] T002 [P] Review contracts in specs/008-edit-world-entity/contracts/ for component interfaces
- [X] T003 [P] Review quickstart.md in specs/008-edit-world-entity/ for implementation guidance

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core component and state infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Component Refactoring

- [X] T004 Rename libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.tsx to WorldEntityForm.tsx
- [X] T005 Rename libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.test.tsx to WorldEntityForm.test.tsx
- [X] T006 Update all imports of EntityDetailForm to WorldEntityForm across codebase (MainPanel.tsx, etc.)
- [X] T007 Update component name references in WorldEntityForm.test.tsx

### State Management Foundation

- [X] T008 Verify worldSidebarSlice actions exist: openEntityFormEdit, closeEntityForm, setUnsavedChanges in libris-maleficarum-app/src/store/worldSidebarSlice.ts
- [X] T009 Verify Redux state supports editingEntityId and hasUnsavedChanges in libris-maleficarum-app/src/store/worldSidebarSlice.ts

### API Foundation

- [X] T010 Verify useUpdateWorldEntityMutation exists in libris-maleficarum-app/src/services/worldEntityApi.ts
- [X] T010a Verify WorldEntityForm has save and cancel actions (buttons/handlers) in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.tsx

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Quick Edit from Hierarchy (Priority: P1) 🎯 MVP

**Goal**: Users can click edit icon next to entities in hierarchy tree to immediately edit in MainPanel, with unsaved changes protection

**Independent Test**: Display hierarchy → click edit icon on any entity → verify edit form appears in MainPanel → modify fields → save → verify hierarchy updated and detail view shown

### Tests for User Story 1

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T011 [P] [US1] Create EntityTreeNode edit icon test in libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.test.tsx
- [X] T012 [P] [US1] Create UnsavedChangesDialog test in libris-maleficarum-app/src/components/MainPanel/UnsavedChangesDialog.test.tsx
- [X] T013 [P] [US1] Create integration test for hierarchy edit flow in libris-maleficarum-app/src/**tests**/integration/worldEntityEditing.integration.test.tsx

### Implementation for User Story 1

- [X] T014 [P] [US1] Create UnsavedChangesDialog component in libris-maleficarum-app/src/components/MainPanel/UnsavedChangesDialog.tsx
- [X] T015 [US1] Add hover state and edit icon (Pencil) to EntityTreeNode in libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.tsx
- [X] T016 [US1] Add edit icon click handler to dispatch openEntityFormEdit(entityId) in libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.tsx
- [X] T017 [US1] Add disabled={isEditing} prop to EntityTypeSelector when isEditing=true in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.tsx
- [X] T018 [US1] Integrate UnsavedChangesDialog with navigation interception logic in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.tsx
- [X] T019 [US1] Update MainPanel to render WorldEntityForm when mainPanelMode='editing_entity' in libris-maleficarum-app/src/components/MainPanel/MainPanel.tsx
- [X] T020 [US1] Add aria-labels to edit icon for accessibility in libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.tsx
- [X] T021 [US1] Verify all tests pass for User Story 1 (run pnpm test)
- [X] T021a [US1] Verify hierarchy refreshes after save (RTK Query cache invalidation triggers re-render)

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Edit from Detail View (Priority: P2)

**Goal**: Users viewing entity details can click Edit button in top-right corner to transition to edit mode without leaving MainPanel

**Independent Test**: Navigate to any entity's detail view → click Edit button in top-right → verify fields become editable → modify → save → verify returns to read-only detail view

### Tests for User Story 2

- [X] T022 [P] [US2] Create EntityDetailReadOnlyView test in libris-maleficarum-app/src/components/MainPanel/EntityDetailReadOnlyView.test.tsx
- [X] T023 [P] [US2] Create integration test for detail view edit flow in libris-maleficarum-app/src/**tests**/integration/worldEntityEditing.integration.test.tsx

### Implementation for User Story 2

- [X] T024 [P] [US2] Create EntityDetailReadOnlyView component in libris-maleficarum-app/src/components/MainPanel/EntityDetailReadOnlyView.tsx
- [X] T025 [US2] Add Edit button in top-right corner to EntityDetailReadOnlyView in libris-maleficarum-app/src/components/MainPanel/EntityDetailReadOnlyView.tsx
- [X] T026 [US2] Add Edit button click handler to dispatch openEntityFormEdit(entityId) in libris-maleficarum-app/src/components/MainPanel/EntityDetailReadOnlyView.tsx
- [X] T027 [US2] Update MainPanel to render EntityDetailReadOnlyView when mainPanelMode='viewing_entity' in libris-maleficarum-app/src/components/MainPanel/MainPanel.tsx
- [X] T028 [US2] Add custom properties display using Object.entries() per EntityDetailReadOnlyView.contract.ts in libris-maleficarum-app/src/components/MainPanel/EntityDetailReadOnlyView.tsx
- [X] T029 [US2] Add transition logic: after successful save, return to EntityDetailReadOnlyView in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.tsx
- [X] T030 [US2] Add aria-label to Edit button for accessibility in libris-maleficarum-app/src/components/MainPanel/EntityDetailReadOnlyView.tsx
- [X] T031 [US2] Verify all tests pass for User Story 2 (run pnpm test)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently

---

## Phase 5: User Story 3 - Edit with Validation Feedback (Priority: P3)

**Goal**: Invalid entity edits display clear schema-based error messages preventing save until corrected

**Independent Test**: Enter edit mode (from either entry point) → enter invalid data (empty required field, wrong format) → attempt save → verify error messages appear → correct data → verify save succeeds

### Tests for User Story 3

- [X] T032 [P] [US3] Create validation test for required fields in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.test.tsx
- [X] T033 [P] [US3] Create validation test for schema rules in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.test.tsx
- [X] T034 [P] [US3] Create integration test for validation error flow in libris-maleficarum-app/src/**tests**/integration/worldEntityEditing.integration.test.tsx

### Implementation for User Story 3

- [X] T035 [P] [US3] Create schema-based validation function inibris-maleficarum-app/src/services/validators/worldEntityValidator.ts using entity type registry
- [X] T036 [US3] Add inline error message display for invalid fields in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.tsx
- [X] T037 [US3] Add aria-invalid and aria-describedby to invalid inputs in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.tsx
- [X] T038 [US3] Disable Save button when validation errors exist in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.tsx
- [X] T039 [US3] Add validation error clearing on field correction in libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.tsx
- [X] T040 [US3] Verify all tests pass for User Story 3 (run pnpm test)

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [X] T041 [P] Add accessibility tests (jest-axe) for UnsavedChangesDialog in libris-maleficarum-app/src/components/MainPanel/UnsavedChangesDialog.test.tsx
- [X] T042 [P] Add accessibility tests (jest-axe) for EntityDetailReadOnlyView in libris-maleficarum-app/src/components/MainPanel/EntityDetailReadOnlyView.test.tsx
- [X] T043 [P] Add accessibility tests (jest-axe) for edit icon in EntityTreeNode in libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.test.tsx
- [ ] T044 [P] Verify keyboard navigation works for all edit interactions (Tab, Enter, Escape)
- [X] T045 [P] Add performance optimization: React.memo() for EntityTreeNode in libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.tsx
- [ ] T046 [P] Verify color contrast ratios meet WCAG 2.2 Level AA (4.5:1 text, 3:1 controls)
- [ ] T047 [P] Verify minimum touch targets meet 44x44px requirement
- [ ] T048 Run full test suite to verify all stories work independently (pnpm test)
- [ ] T049 Run quickstart.md validation checklist
- [X] T050 Update CHANGELOG.md with feature details

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 6)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Shares WorldEntityForm with US1, but independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Enhances WorldEntityForm validation, independently testable

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Components before integration
- Core implementation before accessibility enhancements
- Story complete and tested before moving to next priority

### Parallel Opportunities Per User Story

#### Phase 3 (User Story 1) Parallelization

```bash
# Parallel: All tests can be written simultaneously
T011 (EntityTreeNode.test.tsx)
T012 (UnsavedChangesDialog.test.tsx)
T013 (integration/entityEdit.test.tsx)

# Parallel: Independent component creation
T014 (UnsavedChangesDialog.tsx)

# Sequential after T014-T015: Integration work
T015 → T016 → T017 → T018 → T019 → T020 → T021
```

#### Phase 4 (User Story 2) Parallelization

```bash
# Parallel: All tests can be written simultaneously
T022 (EntityDetailReadOnlyView.test.tsx)
T023 (integration/entityEdit.test.tsx)

# Parallel: Component creation
T024 (EntityDetailReadOnlyView.tsx)

# Sequential after T024: Integration work
T025 → T026 → T027 → T028 → T029 → T030 → T031
```

#### Phase 5 (User Story 3) Parallelization

```bash
# Parallel: All validation tests can be written simultaneously
T032 (required fields validation test)
T033 (schema rules validation test)
T034 (integration validation test)

# Parallel: Validation implementation
T035 (schema validation function)

# Sequential after T035: UI integration
T036 → T037 → T038 → T039 → T040
```

#### Phase 6 (Polish) Parallelization

```bash
# All polish tasks can run in parallel (different aspects)
T041, T042, T043, T044, T045, T046, T047

# Sequential final validation
T048 → T049 → T050
```

---

## Parallel Example: User Story 1

```bash
# Launch all tests for User Story 1 together:
Task: "Create EntityTreeNode edit icon test"
Task: "Create UnsavedChangesDialog test"
Task: "Create integration test for hierarchy edit flow"
# Wait for tests to FAIL

# Launch independent component creation:
Task: "Create UnsavedChangesDialog component"
# Then proceed with sequential integration tasks
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T003)
1. Complete Phase 2: Foundational (T004-T010) ⚠️ CRITICAL - blocks all stories
1. Complete Phase 3: User Story 1 (T011-T021)
1. **STOP and VALIDATE**: Test User Story 1 independently
   - Verify edit icon appears on hover
   - Verify clicking icon opens edit form
   - Verify unsaved changes dialog works
   - Verify save updates hierarchy
   - Run: `pnpm test src/components/WorldSidebar/EntityTreeNode.test.tsx`
   - Run: `pnpm test src/__tests__/integration/entityEdit.test.tsx`
1. Deploy/demo if ready - users can now edit from hierarchy! 🎯

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
1. Add User Story 1 (P1) → Test independently → Deploy/Demo (MVP - Quick hierarchy edits!)
1. Add User Story 2 (P2) → Test independently → Deploy/Demo (Detail view editing!)
1. Add User Story 3 (P3) → Test independently → Deploy/Demo (Validation feedback!)
1. Add Polish (Phase 6) → Full accessibility and performance → Final release
1. Each phase adds value without breaking previous functionality

### Parallel Team Strategy

With multiple developers:

1. **Team completes Setup + Foundational together** (T001-T010)
1. **Once Foundational is done** (after T010 checkpoint):
   - **Developer A**: User Story 1 (T011-T021) - Hierarchy edit
   - **Developer B**: User Story 2 (T022-T031) - Detail view edit
   - **Developer C**: User Story 3 (T032-T040) - Validation
1. **Team reconvenes for Polish** (T041-T050)
1. Stories complete and integrate independently

---

## Testing Strategy

### Test-Driven Development (TDD) Workflow

For each user story:

1. **RED**: Write test, verify it fails (e.g., T011-T013 for US1)
1. **GREEN**: Implement minimum code to pass (e.g., T014-T020 for US1)
1. **REFACTOR**: Clean up, verify tests still pass (e.g., T021 for US1)

### Accessibility Testing (WCAG 2.2 Level AA)

Every component MUST have jest-axe test:

```typescript
it('has no accessibility violations', async () => {
  const { container } = render(<Component />);
  const results = await axe(container);
  expect(results).toHaveNoViolations();
});
```

### Performance Targets

- Edit initiation from hierarchy: < 2 seconds (US1)
- Edit initiation from detail view: < 1 second (US2)
- Validation feedback: < 500ms (US3)
- Hierarchy update after save: < 1 second (US1)

### Test Commands

```bash
# All tests
pnpm test

# Single component
pnpm test src/components/MainPanel/WorldEntityForm.test.tsx

# Test pattern match
pnpm test -- --grep "accessibility"

# Integration tests only
pnpm test src/__tests__/integration/

# Test UI (watch mode)
pnpm test:ui
```

---

## Success Metrics

**Total Tasks**: 52  
**MVP Tasks (Phase 1-3)**: 23 tasks (T001-T021a)  
**Parallel Opportunities**: 15 tasks across all phases  
**Test Coverage**: 13 test tasks ensuring TDD compliance

**Suggested MVP Scope**: Phase 1 (Setup) + Phase 2 (Foundational) + Phase 3 (User Story 1)  
**MVP Delivery**: Quick edit from hierarchy with unsaved changes protection (23 tasks)

**Format Validation**: ✅ All tasks follow checklist format (`- [ ] [ID] [Labels] Description with path`)

---

## Notes

- **[P] tasks**: Different files, no dependencies - safe to parallelize
- **[Story] labels**: Map tasks to user stories for traceability and independent testing
- **Component contracts**: See `specs/008-edit-world-entity/contracts/` for TypeScript interfaces
- **Quickstart guide**: See `specs/008-edit-world-entity/quickstart.md` for implementation details
- **Constitution compliance**: Principle III (TDD) enforced - tests before implementation, jest-axe for accessibility
- **Accessibility**: WCAG 2.2 Level AA required (aria-labels, keyboard nav, contrast, touch targets)
- **Performance**: Monitor targets via browser DevTools (Network tab for API latency)

**Ready to implement?** Start with Phase 1 (Setup), then Phase 2 (Foundational), then Phase 3 (User Story 1 MVP). Each checkpoint provides validation opportunity. Happy coding! 🚀
