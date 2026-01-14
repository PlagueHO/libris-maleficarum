# Tasks: World Sidebar Navigation

**Input**: Design documents from `/specs/003-world-sidebar/`
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create WorldSidebar component directory structure at libris-maleficarum-app/src/components/WorldSidebar/
- [X] T002 [P] Create sessionStorage cache utilities at libris-maleficarum-app/src/lib/sessionCache.ts
- [X] T003 [P] Create entity icon mapping utilities at libris-maleficarum-app/src/lib/entityIcons.ts
- [X] T004 [P] Add context menu primitive from Shadcn/UI (npx shadcn@latest add context-menu) if not already present
- [X] T005 [P] Add dialog primitive from Shadcn/UI (npx shadcn@latest add dialog) if not already present
- [X] T006 [P] Add select primitive from Shadcn/UI (npx shadcn@latest add select) if not already present
- [X] T007 [P] Create data-model.md with WorldEntity schema, cache structures, and hierarchy navigation patterns at specs/003-world-sidebar/data-model.md
- [X] T008 [P] Create contracts/worldEntity.openapi.yaml with API contract definitions for 7 WorldEntity endpoints at specs/003-world-sidebar/contracts/worldEntity.openapi.yaml
- [X] T009 [P] Create quickstart.md developer guide for WorldSidebar component usage at specs/003-world-sidebar/quickstart.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T010 Define WorldEntity TypeScript interfaces in libris-maleficarum-app/src/services/types/worldEntity.types.ts
- [X] T011 Define WorldEntity API request/response types in libris-maleficarum-app/src/services/types/worldEntity.types.ts
- [X] T012 Create RTK Query worldEntityApi endpoints in libris-maleficarum-app/src/services/worldEntityApi.ts
- [X] T013 Create worldSidebarSlice Redux slice in libris-maleficarum-app/src/store/worldSidebarSlice.ts
- [X] T014 Register worldSidebarSlice in root Redux store at libris-maleficarum-app/src/store/store.ts
- [X] T015 [P] Create MSW handlers for worldEntityApi endpoints in libris-maleficarum-app/src/__tests__/mocks/handlers.ts
- [X] T016 [P] Backend: Create WorldEntity domain entity at libris-maleficarum-service/src/Domain/Entities/WorldEntity.cs
- [X] T017 [P] Backend: Create WorldEntityRepository at libris-maleficarum-service/src/Infrastructure/Repositories/WorldEntityRepository.cs
- [X] T018 [P] Backend: Create WorldEntityController at libris-maleficarum-service/src/Api/Controllers/WorldEntitiesController.cs
- [X] T019 [P] Backend: Add unit tests for WorldEntityRepository at libris-maleficarum-service/tests/unit/Infrastructure.UnitTests/Repositories/WorldEntityRepositoryTests.cs
- [X] T020 [P] Backend: Add unit tests for WorldEntityController at libris-maleficarum-service/tests/unit/Api.UnitTests/Controllers/WorldEntityControllerTests.cs

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - First-Time World Creation (Priority: P1) 🎯 MVP

**Goal**: Enable new users to create their first world via empty state prompt and modal form

**Independent Test**: Launch app with no worlds → see empty state → click "Create World" → fill form → world appears in sidebar

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [X] T021 [P] [US1] Write EmptyState component test in libris-maleficarum-app/src/components/WorldSidebar/EmptyState.test.tsx
- [X] T022 [P] [US1] Write WorldFormModal component test in libris-maleficarum-app/src/components/WorldSidebar/WorldFormModal.test.tsx
- [X] T023 [P] [US1] Write WorldSelector test for empty state handling in libris-maleficarum-app/src/components/WorldSidebar/WorldSelector.test.tsx
- [X] T024 [P] [US1] Write accessibility test for WorldFormModal with jest-axe in libris-maleficarum-app/src/components/WorldSidebar/WorldFormModal.test.tsx

### Implementation for User Story 1

- [X] T025 [P] [US1] Create EmptyState component in libris-maleficarum-app/src/components/WorldSidebar/EmptyState.tsx
- [X] T026 [P] [US1] Create WorldFormModal component in libris-maleficarum-app/src/components/WorldSidebar/WorldFormModal.tsx
- [X] T027 [US1] Create WorldSelector component in libris-maleficarum-app/src/components/WorldSidebar/WorldSelector.tsx
- [X] T028 [US1] Create WorldSidebar container component in libris-maleficarum-app/src/components/WorldSidebar/WorldSidebar.tsx
- [X] T029 [US1] Add WorldSidebar to App layout in libris-maleficarum-app/src/App.tsx
- [X] T030 [US1] Implement world creation form validation in WorldFormModal.tsx
- [X] T031 [US1] Connect WorldFormModal to createWorld mutation from worldApi
- [X] T032 [US1] Handle successful world creation (set as active world, display empty hierarchy)
- [X] T033 [US1] Add CSS Modules styling for EmptyState in libris-maleficarum-app/src/components/WorldSidebar/EmptyState.module.css
- [X] T034 [US1] Add CSS Modules styling for WorldFormModal (if needed beyond Shadcn Dialog defaults)
- [X] T035 [US1] Add CSS Modules styling for WorldSelector in libris-maleficarum-app/src/components/WorldSidebar/WorldSelector.module.css
- [X] T036 [US1] Add CSS Modules styling for WorldSidebar layout in libris-maleficarum-app/src/components/WorldSidebar/WorldSidebar.module.css

**Checkpoint**: At this point, User Story 1 should be fully functional - users can create their first world and see it in the sidebar

---

## Phase 4: User Story 2 - World Selection and Hierarchy Navigation (Priority: P1)

**Goal**: Enable users to switch between worlds and navigate entity hierarchies with expand/collapse

**Independent Test**: Create 2-3 worlds with entities → switch between worlds → expand/collapse entities → verify state persists from cache

### Tests for User Story 2 ⚠️

- [X] T037 [P] [US2] Write EntityTree component test in libris-maleficarum-app/src/components/WorldSidebar/EntityTree.test.tsx
- [X] T038 [P] [US2] Write EntityTreeNode component test in libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.test.tsx
- [ ] T039 [P] [US2] Write WorldSelector test for multi-world dropdown in libris-maleficarum-app/src/components/WorldSidebar/WorldSelector.test.tsx
- [X] T043 [P] [US2] Write accessibility test for EntityTree with ARIA tree pattern in libris-maleficarum-app/src/components/WorldSidebar/EntityTree.test.tsx
- [ ] T044 [P] [US2] Write sessionCache utility tests in libris-maleficarum-app/src/lib/sessionCache.test.ts
- [ ] T045 [P] [US2] Write integration test for hierarchy caching with MSW in libris-maleficarum-app/src/__tests__/integration/hierarchyCaching.test.tsx

### Implementation for User Story 2

- [X] T043 [P] [US2] Implement sessionCache.get() utility in libris-maleficarum-app/src/lib/sessionCache.ts
- [X] T044 [P] [US2] Implement sessionCache.set() utility with TTL in libris-maleficarum-app/src/lib/sessionCache.ts
- [X] T045 [P] [US2] Implement sessionCache.invalidate() utility in libris-maleficarum-app/src/lib/sessionCache.ts
- [X] T046 [P] [US2] Create EntityTreeNode component in libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.tsx
- [X] T047 [P] [US2] Create EntityTree recursive component in libris-maleficarum-app/src/components/WorldSidebar/EntityTree.tsx
- [X] T048 [US2] Add expand/collapse logic to EntityTreeNode with chevron icons
- [X] T049 [US2] Implement lazy loading of children in EntityTree (check cache first, then fetch)
- [X] T050 [US2] Connect EntityTree to getEntitiesByParent RTK Query endpoint
- [X] T051 [US2] Update worldSidebarSlice to track expandedNodeIds array
- [X] T052 [US2] Implement visual indentation for hierarchy levels in EntityTreeNode
- [X] T053 [US2] Implement connecting lines between parent-child entities in EntityTree
- [X] T054 [US2] Add world switching logic in WorldSelector (clear cache, load new world entities)
- [X] T055 [US2] Implement cache restoration when returning to previously viewed world
- [X] T056 [US2] Add loading skeleton UI while fetching entity children
- [X] T057 [US2] Update WorldSelector to display all worlds alphabetically with current world highlighted
- [X] T058 [US2] Add keyboard navigation handlers (Arrow keys, Enter) in EntityTree
- [X] T059 [US2] Add ARIA tree roles and attributes in EntityTree and EntityTreeNode
- [X] T060 [US2] Add CSS Modules styling for EntityTree in libris-maleficarum-app/src/components/WorldSidebar/EntityTree.module.css
- [X] T061 [US2] Add CSS Modules styling for EntityTreeNode in libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.module.css

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - full world navigation with caching

---

## Phase 5: User Story 3 - World Metadata Editing (Priority: P2)

**Goal**: Enable users to edit existing world metadata (name, description) via settings icon/menu

**Independent Test**: Select world → click edit icon → modify name/description → save → verify updates in sidebar

### Tests for User Story 3 ⚠️

- [X] T062 [P] [US3] Write WorldFormModal edit mode test in libris-maleficarum-app/src/components/WorldSidebar/WorldFormModal.test.tsx
- [X] T063 [P] [US3] Write WorldSelector edit button test in libris-maleficarum-app/src/components/WorldSidebar/WorldSelector.test.tsx
- [ ] T064 [P] [US3] Write integration test for world update flow in libris-maleficarum-app/src/__tests__/integration/worldEdit.test.tsx

### Implementation for User Story 3

- [X] T065 [US3] Add edit mode support to WorldFormModal (pre-populate fields when worldId provided)
- [X] T066 [US3] Add edit icon/button to WorldSelector for current world
- [X] T067 [US3] Connect edit button to open WorldFormModal in edit mode
- [X] T068 [US3] Connect WorldFormModal save to updateWorld mutation
- [X] T069 [US3] Update worldSidebarSlice to track editingWorldId state
- [X] T070 [US3] Implement optimistic update in worldSelector to show changes immediately
- [X] T071 [US3] Add cancel handling for WorldFormModal (close without saving)
- [X] T072 [US3] Add validation for empty world name in edit mode

**Checkpoint**: All P1-P2 user stories complete - users can create, navigate, and edit worlds

---

## Phase 6: User Story 4 - Efficient Hierarchy Loading with Caching (Priority: P2)

**Goal**: Optimize performance with sessionStorage caching and cache invalidation on mutations

**Independent Test**: Create world with 50+ entities → expand branches → switch world and back → verify instant load from cache

### Tests for User Story 4 ⚠️

- [ ] T073 [P] [US4] Write cache hit test in libris-maleficarum-app/src/lib/sessionCache.test.ts
- [ ] T074 [P] [US4] Write cache expiry (TTL) test in libris-maleficarum-app/src/lib/sessionCache.test.ts
- [ ] T075 [P] [US4] Write cache invalidation test in libris-maleficarum-app/src/**tests**/integration/cacheInvalidation.test.tsx
- [ ] T076 [P] [US4] Write performance test for cached vs uncached loads in libris-maleficarum-app/src/**tests**/performance/hierarchyLoad.test.tsx

### Implementation for User Story 4

- [ ] T077 [US4] Implement 5-minute TTL logic in sessionCache.get()
- [ ] T078 [US4] Add cache invalidation on entity create mutation
- [ ] T079 [US4] Add cache invalidation on entity update mutation
- [ ] T080 [US4] Add cache invalidation on entity delete mutation
- [ ] T081 [US4] Add cache invalidation on entity move mutation
- [ ] T082 [US4] Implement cache hit rate logging (for debugging)
- [ ] T083 [US4] Add cache restoration logic for expanded state in worldSidebarSlice
- [ ] T084 [US4] Test cache behavior with 100+ entities to verify performance goals (<100ms cached expansion)

**Checkpoint**: Caching fully functional - 80%+ cache hit rate achieved for typical navigation

---

## Phase 7: User Story 5 - Visual Hierarchy and Modern UI Polish (Priority: P3)

**Goal**: Polished visual experience with icons, hover states, themes, smooth scrolling

**Independent Test**: Visual inspection + usability testing - users can identify entity types, understand hierarchy at a glance

### Tests for User Story 5 ⚠️

- [ ] T085 [P] [US5] Write entity icon mapping test in libris-maleficarum-app/src/lib/entityIcons.test.ts
- [ ] T086 [P] [US5] Write visual regression tests for theme support (if tooling available)
- [ ] T087 [P] [US5] Write accessibility contrast test with jest-axe for dark/light themes

### Implementation for User Story 5

- [ ] T088 [P] [US5] Create entity type to Lucide icon mapping in libris-maleficarum-app/src/lib/entityIcons.ts
- [ ] T089 [US5] Add entity type icons to EntityTreeNode
- [ ] T090 [US5] Implement hover states for EntityTreeNode (inline + button appears)
- [ ] T091 [US5] Add smooth scrolling support to WorldSidebar with ScrollArea component
- [ ] T092 [US5] Implement sticky positioning for WorldSelector at top of sidebar
- [ ] T093 [US5] Add expand/collapse chevron icons to EntityTreeNode
- [ ] T094 [US5] Refine visual indentation styling (8-level max) in EntityTree.module.css
- [ ] T095 [US5] Add connecting lines styling between parent-child in EntityTree.module.css
- [ ] T096 [US5] Add dark theme support with appropriate contrast in all WorldSidebar components
- [ ] T097 [US5] Add hover tooltip support for truncated entity names
- [ ] T098 [US5] Polish empty state visual design in EmptyState.module.css

**Checkpoint**: All user stories complete with polished UI - ready for production

---

## Phase 8: Entity Creation & Context Menu (Cross-Cutting)

**Goal**: Multi-entry point entity creation with context menu actions

**Independent Test**: Right-click entity → context menu appears → add child/edit/delete/move → verify actions work

### Tests for Entity Creation ⚠️

- [ ] T099 [P] Write EntityFormModal component test in libris-maleficarum-app/src/components/WorldSidebar/EntityFormModal.test.tsx
- [ ] T100 [P] Write EntityContextMenu component test in libris-maleficarum-app/src/components/WorldSidebar/EntityContextMenu.test.tsx
- [ ] T101 [P] Write context-aware type suggestions test in EntityFormModal.test.tsx
- [ ] T102 [P] Write accessibility test for EntityContextMenu keyboard trigger (Shift+F10)

### Implementation for Entity Creation

- [ ] T103 [P] Create EntityFormModal component in libris-maleficarum-app/src/components/WorldSidebar/EntityFormModal.tsx
- [ ] T104 [P] Create EntityContextMenu component in libris-maleficarum-app/src/components/WorldSidebar/EntityContextMenu.tsx
- [ ] T105 Add inline "+" button to EntityTreeNode (visible on hover)
- [ ] T106 Add "Add Root Entity" button to WorldSidebar
- [ ] T107 Implement context-aware entity type suggestions in EntityFormModal
- [ ] T108 Connect EntityFormModal to createEntity mutation
- [ ] T109 Add context menu "Add Child Entity" action
- [ ] T110 Add context menu "Edit Entity" action
- [ ] T111 Add context menu "Delete Entity" action with confirmation
- [ ] T112 Add context menu "Move Entity" action (future: opens move dialog)
- [ ] T113 Update worldSidebarSlice to track entity form modal state (open/close, editing entity, parent entity)
- [ ] T114 Connect inline "+" button to open EntityFormModal with parent pre-selected
- [ ] T115 Connect "Add Root Entity" button to open EntityFormModal with worldId as parent
- [ ] T116 Implement entity creation form validation (name required, max lengths)
- [ ] T117 Add CSS Modules styling for EntityFormModal
- [ ] T118 Add CSS Modules styling for EntityContextMenu

**Checkpoint**: Full entity CRUD operations available via sidebar

---

## Phase 9: Entity Selection & Main Panel Integration

**Goal**: Click entity to select it and display details in main panel

**Independent Test**: Click entity in sidebar → verify selection highlight → verify main panel displays entity details

### Tests for Entity Selection ⚠️

- [ ] T119 [P] Write entity selection test in EntityTreeNode.test.tsx
- [ ] T120 [P] Write integration test for sidebar → main panel communication in libris-maleficarum-app/src/**tests**/integration/entitySelection.test.tsx

### Implementation for Entity Selection

- [ ] T121 Add click handler to EntityTreeNode to dispatch setSelectedEntity action
- [ ] T122 Update worldSidebarSlice to track selectedEntityId
- [ ] T123 Add visual selection highlight styling to EntityTreeNode.module.css
- [ ] T124 Update MainPanel component to listen to selectedEntityId from Redux store
- [ ] T125 Create basic EntityDetailView component in MainPanel (future enhancement)
- [ ] T126 Test entity selection flow end-to-end

**Checkpoint**: Complete sidebar-to-main-panel communication established

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T127 [P] Add JSDoc comments to all public component props and functions
- [ ] T128 [P] Create barrel export (index.ts) for WorldSidebar component
- [ ] T129 [P] Update README.md with WorldSidebar usage instructions
- [ ] T130 [P] Add error boundary around WorldSidebar for graceful error handling
- [ ] T131 Code cleanup: Remove console.logs, ensure consistent formatting
- [ ] T132 Run full test suite and ensure 100% pass rate
- [ ] T133 Run jest-axe accessibility validation across all components
- [ ] T134 Performance profiling: Verify <2s initial load, <100ms cached expansion
- [ ] T135 Security review: Ensure no sensitive data in sessionStorage
- [ ] T136 [P] Backend: Add API integration tests for WorldEntity endpoints
- [ ] T137 Final code review and constitution compliance check

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-7)**: All depend on Foundational phase completion
  - US1 (First-Time World Creation) - Can start after Foundational
  - US2 (World Selection & Hierarchy) - Can start after Foundational
  - US3 (World Editing) - Depends on US1 (needs WorldFormModal)
  - US4 (Caching) - Depends on US2 (needs hierarchy navigation)
  - US5 (Visual Polish) - Depends on US2 (needs all components present)
- **Entity Creation (Phase 8)**: Depends on US2 (needs EntityTree)
- **Entity Selection (Phase 9)**: Depends on US2 (needs EntityTreeNode)
- **Polish (Phase 10)**: Depends on all desired user stories being complete

### Recommended Implementation Order

**MVP Path** (fastest to value):

1. Phase 1: Setup (T001-T006)
1. Phase 2: Foundational (T007-T017) ⚠️ BLOCKS EVERYTHING
1. Phase 3: US1 - First-Time World Creation (T018-T033) → DEMO 1
1. Phase 4: US2 - World Selection & Hierarchy (T034-T061) → DEMO 2
1. Phase 8: Entity Creation (T099-T118) → DEMO 3
1. Phase 9: Entity Selection (T119-T126) → DEMO 4

**Full Feature Path** (all user stories):

1. MVP Path above (Phases 1-4, 8-9)
1. Phase 5: US3 - World Metadata Editing (T062-T072)
1. Phase 6: US4 - Efficient Caching (T073-T084)
1. Phase 7: US5 - Visual Polish (T085-T098)
1. Phase 10: Final Polish (T127-T137)

### Parallel Opportunities

**Within Setup (Phase 1)**: T002, T003, T004, T005, T006 can all run in parallel

**Within Foundational (Phase 2)**:

- Frontend: T007-T012 can run in parallel
- Backend: T013-T017 can run in parallel
- Frontend and Backend can run in parallel

**Within User Stories**:

- All test tasks marked [P] within a story can run in parallel
- Component creation tasks marked [P] can run in parallel (different files)

**Across User Stories** (if team capacity allows):

- US1 and US2 can be worked on in parallel by different developers after Foundational completes
- US3, US4, US5 can start in parallel after their dependencies are met

---

## Parallel Example: User Story 2

```bash
# Launch all tests for US2 together:
T034: EntityTree.test.tsx
T035: EntityTreeNode.test.tsx
T036: WorldSelector multi-world test
T037: EntityTree accessibility test
T038: sessionCache.test.ts
T039: hierarchyCaching integration test

# Launch all component creation tasks:
T043-T045: sessionCache utilities (3 functions)
T046: EntityTreeNode.tsx
T047: EntityTree.tsx
```

---

## Implementation Strategy

### MVP First (User Stories 1-2 + Entity Creation)

1. Complete Phase 1: Setup (9 tasks)
2. Complete Phase 2: Foundational (11 tasks) → Foundation ready
3. Complete Phase 3: US1 - First-Time World Creation (16 tasks) → Demo MVP
4. Complete Phase 4: US2 - World Selection & Hierarchy (25 tasks) → Core feature complete
5. Complete Phase 8: Entity Creation (16 tasks) → Full CRUD available
6. Complete Phase 9: Entity Selection (6 tasks) → Sidebar integrated with main panel
7. **STOP and VALIDATE**: 83 tasks complete, full feature functional

### Incremental Delivery Beyond MVP

1. Add Phase 5: US3 - World Editing (11 tasks) → Metadata management
1. Add Phase 6: US4 - Efficient Caching (12 tasks) → Performance optimized
1. Add Phase 7: US5 - Visual Polish (14 tasks) → Production-ready UI
1. Complete Phase 10: Polish (11 tasks) → Ship it!

**Total Tasks**: 137 tasks
**MVP Subset**: ~83 tasks (Phases 1-4, 8-9)
**Estimated Effort**: 26-31 hours for MVP, 41-46 hours for full feature

---

## Notes

- **[P]** tasks = different files, no dependencies - safe for parallel execution
- **[Story]** label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- **TDD Approach**: Write tests FIRST (marked ⚠️), ensure they FAIL, then implement
- **Commit Often**: Commit after each task or logical group
- **Validate Early**: Stop at checkpoints to validate story independently
- **Constitution Compliance**: Maintained throughout (see plan.md Post-Constitution Check)
- **Accessibility**: jest-axe validation required for all interactive components
- **No New Packages**: All dependencies already in package.json (see plan.md Appendix)
