# Tasks: World Sidebar Navigation

**Input**: Design documents from `/specs/003-world-sidebar/`
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

**Architecture**: Main panel forms (not modals) for world/entity CRUD alongside ChatWindow; responsive layouts at 768px/1024px breakpoints; delete/move operations use modal confirmations only.

## Format: `- [ ] [ID] [P?] [Story] Description`

- **[ ]**: Task checkbox (unchecked = not started)
- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

✅ **COMPLETED**: All Phase 1 tasks finished

- [X] T001 Create WorldSidebar component directory structure at libris-maleficarum-app/src/components/WorldSidebar/
- [X] T002 [P] Create MainPanel component directory structure at libris-maleficarum-app/src/components/MainPanel/ (extend existing)
- [X] T003 [P] Create sessionStorage cache utilities at libris-maleficarum-app/src/lib/sessionCache.ts
- [X] T004 [P] Create entity icon mapping utilities at libris-maleficarum-app/src/lib/entityIcons.ts
- [X] T005 [P] Add context menu primitive from Shadcn/UI (npx shadcn@latest add context-menu) if not already present
- [X] T006 [P] Add dialog primitive from Shadcn/UI (npx shadcn@latest add dialog) if not already present
- [X] T007 [P] Add select primitive from Shadcn/UI (npx shadcn@latest add select) if not already present
- [X] T008 [P] Add drawer primitive from Shadcn/UI (npx shadcn@latest add drawer) for mobile ChatWindow if not already present
- [X] T009 [P] Create data-model.md with WorldEntity schema, cache structures, main panel state modes, and responsive layout patterns at specs/003-world-sidebar/data-model.md
- [X] T010 [P] Create contracts/worldEntity.openapi.yaml with API contract definitions for 7 WorldEntity endpoints at specs/003-world-sidebar/contracts/worldEntity.openapi.yaml
- [X] T011 [P] Create quickstart.md developer guide for WorldSidebar and main panel form integration at specs/003-world-sidebar/quickstart.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

✅ **COMPLETED**: All Phase 2 tasks finished - foundation ready

- [X] T012 Define WorldEntity TypeScript interfaces in libris-maleficarum-app/src/services/types/worldEntity.types.ts
- [X] T013 Define WorldEntity API request/response types in libris-maleficarum-app/src/services/types/worldEntity.types.ts
- [X] T014 Create RTK Query worldEntityApi endpoints in libris-maleficarum-app/src/services/worldEntityApi.ts with cache tags for invalidation
  - **API Endpoint Pattern**: 
    - `getEntities`: `GET /api/v1/worlds/{worldId}/entities` (returns all entities, filter client-side for roots)
    - `getChildrenByParent`: `GET /api/v1/worlds/{worldId}/entities/{parentId}/children` (returns only direct children)
  - **Cache Tag Strategy**: 
    - Tag: `{ type: 'WorldEntity', id: worldId }` for all entity queries per world
    - Invalidate entire world hierarchy on any entity mutation to ensure freshness
- [X] T015 Create worldSidebarSlice Redux slice in libris-maleficarum-app/src/store/worldSidebarSlice.ts with MainPanelMode types
- [X] T016 Register worldSidebarSlice in root Redux store at libris-maleficarum-app/src/store/store.ts
- [X] T017 [P] Create MSW handlers for worldEntityApi endpoints in libris-maleficarum-app/src/__tests__/mocks/handlers.ts
- [X] T018 [P] Backend: Create WorldEntity domain entity at libris-maleficarum-service/src/Domain/Entities/WorldEntity.cs
- [X] T019 [P] Backend: Create WorldEntityRepository at libris-maleficarum-service/src/Infrastructure/Repositories/WorldEntityRepository.cs
- [X] T020 [P] Backend: Create WorldEntityController at libris-maleficarum-service/src/Api/Controllers/WorldEntitiesController.cs
- [X] T021 [P] Backend: Add unit tests for WorldEntityRepository at libris-maleficarum-service/tests/unit/Infrastructure.UnitTests/Repositories/WorldEntityRepositoryTests.cs
- [X] T022 [P] Backend: Add unit tests for WorldEntityController at libris-maleficarum-service/tests/unit/Api.UnitTests/Controllers/WorldEntityControllerTests.cs
- [X] T023 Create MainPanel.tsx router component for form mode display in libris-maleficarum-app/src/components/MainPanel/MainPanel.tsx

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - First-Time World Creation (Priority: P1) 🎯 MVP

**Goal**: Enable new users to create their first world via empty state prompt and main panel form with ChatWindow accessibility

**Independent Test**: Launch app with no worlds → see empty state → click "Create World" → fill form in main panel with ChatWindow visible → world appears in sidebar

### Tests for User Story 1 ⚠️

> **NOTE: Write these tests FIRST, ensure they FAIL before implementation**

- [x] T025 [P] [US1] Write EmptyState component test in libris-maleficarum-app/src/components/WorldSidebar/EmptyState.test.tsx
- [x] T026 [P] [US1] Write WorldDetailForm component test in libris-maleficarum-app/src/components/MainPanel/WorldDetailForm.test.tsx
- [x] T027 [P] [US1] Write WorldSelector test for empty state and world creation trigger in libris-maleficarum-app/src/components/WorldSidebar/WorldSelector.test.tsx
- [x] T028 [P] [US1] Write accessibility test for WorldDetailForm with jest-axe in libris-maleficarum-app/src/components/MainPanel/WorldDetailForm.test.tsx
- [x] T029 [P] [US1] Write integration test for world creation flow in libris-maleficarum-app/src/__tests__/integration/worldCreation.test.tsx

### Implementation for User Story 1

- [x] T030 [P] [US1] Create EmptyState component in libris-maleficarum-app/src/components/WorldSidebar/EmptyState.tsx with "Create Your First World" prompt
- [x] T031 [P] [US1] Create WorldDetailForm component in libris-maleficarum-app/src/components/MainPanel/WorldDetailForm.tsx (main panel form, not modal)
- [x] T032 [US1] Create WorldSelector component in libris-maleficarum-app/src/components/WorldSidebar/WorldSelector.tsx with dropdown and create button
- [x] T033 [US1] Create WorldSidebar container component in libris-maleficarum-app/src/components/WorldSidebar/WorldSidebar.tsx with responsive layout
- [x] T034 [US1] Integrate WorldSidebar into App layout (responsive: sidebar drawer on mobile) in libris-maleficarum-app/src/App.tsx
- [x] T035 [US1] Implement world creation form validation in WorldDetailForm (name required, max 100 chars, description max 500 chars)
- [x] T036 [US1] Connect WorldDetailForm to createWorld mutation from worldApi with error handling
- [x] T037 [US1] Handle successful world creation: set as active world, clear form, show empty hierarchy message in main panel
- [x] T038 [US1] Add CSS Modules styling for EmptyState in libris-maleficarum-app/src/components/WorldSidebar/EmptyState.module.css
- [x] T039 [US1] Add CSS Modules styling for WorldDetailForm (responsive form layout) in libris-maleficarum-app/src/components/MainPanel/WorldDetailForm.module.css
- [x] T040 [US1] Add CSS Modules styling for WorldSelector in libris-maleficarum-app/src/components/WorldSidebar/WorldSelector.module.css
- [x] T041 [US1] Add CSS Modules styling for WorldSidebar in libris-maleficarum-app/src/components/WorldSidebar/WorldSidebar.module.css (responsive drawer on <768px)
- [x] T042 [US1] Integrate MainPanel router to display WorldDetailForm when mode is 'creating_world'
- [x] T043 [US1] Add unsaved change detection to worldSidebarSlice for form state tracking
- [x] T044 [US1] Implement "unsaved changes" warning when navigating away from WorldDetailForm

**Checkpoint**: At this point, User Story 1 should be fully functional - users can create their first world and see it in the sidebar

---

## Phase 4: User Story 2 - World Selection and Hierarchy Navigation (Priority: P1)

**Goal**: Enable users to switch between worlds and navigate entity hierarchies with expand/collapse alongside ChatWindow

**Independent Test**: Create 2-3 worlds with entities → switch between worlds → expand/collapse entities → verify state persists from cache and ChatWindow stays accessible

### Tests for User Story 2 ⚠️

- [x] T045 [P] [US2] Write EntityTree component test in libris-maleficarum-app/src/components/WorldSidebar/EntityTree.test.tsx
- [x] T046 [P] [US2] Write EntityTreeNode component test in libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.test.tsx
- [x] T047 [P] [US2] Write WorldSelector test for multi-world dropdown in libris-maleficarum-app/src/components/WorldSidebar/WorldSelector.test.tsx
- [x] T048 [P] [US2] Write EntityDetailForm test for read-only entity display in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.test.tsx
- [x] T049 [P] [US2] Write accessibility test for EntityTree with ARIA tree pattern in libris-maleficarum-app/src/components/WorldSidebar/EntityTree.test.tsx
- [x] T050 [P] [US2] Write sessionCache utility tests in libris-maleficarum-app/src/lib/sessionCache.test.ts
- [x] T051 [P] [US2] Write integration test for hierarchy caching with MSW in libris-maleficarum-app/src/__tests__/integration/hierarchyCaching.test.tsx

### Implementation for User Story 2

- [x] T052 [P] [US2] Implement sessionCache.get() utility with TTL check in libris-maleficarum-app/src/lib/sessionCache.ts
- [x] T053 [P] [US2] Implement sessionCache.set() utility with TTL metadata in libris-maleficarum-app/src/lib/sessionCache.ts
- [x] T054 [P] [US2] Implement sessionCache.invalidate() utility in libris-maleficarum-app/src/lib/sessionCache.ts
- [x] T055 [P] [US2] Create EntityTreeNode component in libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.tsx with chevron expand/collapse
- [x] T056 [P] [US2] Create EntityTree recursive component in libris-maleficarum-app/src/components/WorldSidebar/EntityTree.tsx
- [x] T057 [P] [US2] Create EntityDetailForm component in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.tsx (read-only form display)
- [x] T058 [US2] Add expand/collapse logic to EntityTreeNode with chevron icons (using Lucide ChevronRight)
- [x] T059 [US2] Implement lazy loading of children in EntityTree (check sessionCache first, then fetch via RTK Query)
- [x] T060 [US2] Connect EntityTree to getEntitiesByParent RTK Query endpoint
- [x] T061 [US2] Update worldSidebarSlice to track expandedNodeIds array and selectedEntityId
- [x] T062 [US2] Implement visual indentation for hierarchy levels in EntityTreeNode (progressive indentation)
- [x] T063 [US2] Implement connecting lines between parent-child entities in EntityTree (CSS connectors or SVG)
- [x] T064 [US2] Add world switching logic in WorldSelector (invalidate cache, load new world entities, reset expanded state)
- [x] T065 [US2] Implement cache restoration when returning to previously viewed world in worldSidebarSlice
- [x] T066 [US2] Add loading skeleton UI while fetching entity children in EntityTree
- [x] T067 [US2] Update WorldSelector to display all worlds alphabetically with current world highlighted
- [x] T068 [US2] Add keyboard navigation handlers (Arrow Up/Down, Enter, Escape) in EntityTree per ARIA tree pattern
- [x] T069 [US2] Add ARIA tree roles and attributes in EntityTree and EntityTreeNode (role="tree", "treeitem", aria-expanded, aria-level, etc.)
- [x] T070 [US2] Add CSS Modules styling for EntityTree in libris-maleficarum-app/src/components/WorldSidebar/EntityTree.module.css
- [x] T071 [US2] Add CSS Modules styling for EntityTreeNode in libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.module.css
- [x] T072 [US2] Add CSS Modules styling for EntityDetailForm in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.module.css (responsive read-only form)
- [x] T073 [US2] Integrate MainPanel router to display EntityDetailForm when mode is 'viewing_entity'
- [x] T074 [US2] Connect entity click to dispatch selection action and set mainPanel mode to 'viewing_entity'

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - full world navigation with caching and entity viewing

---

## Phase 5: User Story 3 - World Metadata Editing (Priority: P2)

**Goal**: Enable users to edit existing world metadata via main panel form with ChatWindow accessibility

**Independent Test**: Select world → click edit icon → modify name/description in main panel with ChatWindow visible → save → verify updates in sidebar

### Tests for User Story 3 ⚠️

- [X] T075 [P] [US3] Write WorldDetailForm edit mode test in libris-maleficarum-app/src/components/MainPanel/WorldDetailForm.test.tsx
- [X] T076 [P] [US3] Write WorldSelector edit button test in libris-maleficarum-app/src/components/WorldSidebar/WorldSelector.test.tsx
- [X] T077 [P] [US3] Write integration test for world update flow in libris-maleficarum-app/src/__tests__/integration/worldEdit.test.tsx

### Implementation for User Story 3

- [X] T078 [US3] Add edit mode support to WorldDetailForm (pre-populate fields when worldId provided via Redux state)
- [X] T079 [US3] Add edit icon/button to WorldSelector for current world with menu/dropdown
- [X] T080 [US3] Connect edit button to dispatch action that sets mainPanel mode to 'editing_world' and loads world data
- [X] T081 [US3] Connect WorldDetailForm save to updateWorld mutation with optimistic update
- [X] T082 [US3] Update worldSidebarSlice to track editingWorldId state and edit mode
- [X] T083 [US3] Implement optimistic update in worldSelector to show changes immediately after edit
  - **Pattern**: Dispatch Redux state change immediately on form submit (user sees instant feedback), revert if API fails with error message
- [X] T084 [US3] Add cancel handling for WorldDetailForm (close without saving, return to previous mode)
- [X] T085 [US3] Add validation for empty world name in edit mode (same as create mode)
- [X] T086 [US3] Implement "unsaved changes" warning when user attempts to navigate away from editing_world mode

**Checkpoint**: All P1-P2 user stories complete - users can create, navigate, view, and edit worlds with ChatWindow accessibility

---

## Phase 5.5: Entity Edit Button (MVP Clarification)

**Goal**: Clarify entity editing UI for MVP phase

- [ ] T087 [US2.5] Add Edit button to EntityDetailForm (visible but disabled in MVP)
  - **Button State**: Disabled with tooltip "Entity editing coming in Phase 2"
  - **Future**: When full edit mode ready, enable button to show edit form

---

## Phase 6: User Story 2.5 - Entity Creation and Context Menu (Priority: P1)

**Goal**: Enable users to create new entities via multiple entry points (inline +, context menu, root button) with main panel form display

**Independent Test**: Expand entity with children → right-click on entity → select "Add Child" → fill form in main panel → new entity appears in tree

### Tests for User Story 2.5 ⚠️

- [ ] T088 [P] [US2.5] Write EntityContextMenu component test in libris-maleficarum-app/src/components/WorldSidebar/EntityContextMenu.test.tsx
- [ ] T089 [P] [US2.5] Write EntityCreationForm component test in libris-maleficarum-app/src/components/MainPanel/EntityCreationForm.test.tsx
- [ ] T090 [P] [US2.5] Write integration test for entity creation flow in libris-maleficarum-app/src/__tests__/integration/entityCreation.test.tsx
- [ ] T091 [P] [US2.5] Write DeleteConfirmationModal component test in libris-maleficarum-app/src/components/MainPanel/DeleteConfirmationModal.test.tsx

### Implementation for User Story 2.5

- [ ] T092 [P] [US2.5] Create EntityContextMenu component in libris-maleficarum-app/src/components/WorldSidebar/EntityContextMenu.tsx with Shadcn ContextMenu
- [ ] T093 [P] [US2.5] Create EntityCreationForm component in libris-maleficarum-app/src/components/MainPanel/EntityCreationForm.tsx (main panel form, not modal)
- [ ] T094 [P] [US2.5] Create DeleteConfirmationModal component in libris-maleficarum-app/src/components/MainPanel/DeleteConfirmationModal.tsx (ONLY modal for entity operations)
- [ ] T095 [US2.5] Add context menu (right-click) to EntityTreeNode nodes with Edit, Add Child, Delete, Move options
- [ ] T096 [US2.5] Add inline "+" button to EntityTreeNode (appears on hover) to create child entities
- [ ] T097 [US2.5] Add "Add Root Entity" button at end of EntityTree for creating world-level entities
- [ ] T098 [US2.5] Implement entity creation form with type selector, name, description, tags fields
- [ ] T099 [US2.5] Add context-aware entity type suggestions in EntityCreationForm based on parent entity type
- [ ] T100 [US2.5] Connect EntityContextMenu actions to mainPanel mode updates (set mode to 'creating_entity', store parentEntityId)
- [ ] T101 [US2.5] Connect EntityCreationForm to createEntity mutation with proper parent and world ID parameters
- [ ] T102 [US2.5] Implement "Edit" action in EntityContextMenu (future: full edit form, currently view-only detail form)
- [ ] T103 [US2.5] Implement "Delete" action with DeleteConfirmationModal overlay confirmation
- [ ] T104 [US2.5] Implement "Move" action with move modal (picker for new parent - per spec, move stays as modal for now)
- [ ] T105 [US2.5] Add CSS Modules styling for EntityContextMenu in libris-maleficarum-app/src/components/WorldSidebar/EntityContextMenu.module.css
- [ ] T106 [US2.5] Add CSS Modules styling for EntityCreationForm in libris-maleficarum-app/src/components/MainPanel/EntityCreationForm.module.css
- [ ] T107 [US2.5] Add CSS Modules styling for DeleteConfirmationModal in libris-maleficarum-app/src/components/MainPanel/DeleteConfirmationModal.module.css
- [ ] T108 [US2.5] Integrate MainPanel router to display EntityCreationForm when mode is 'creating_entity'
- [ ] T109 [US2.5] Add unsaved change detection for EntityCreationForm (warn on navigation away with unsaved input)

**Checkpoint**: Users can now create entities via multiple entry points with ChatWindow accessible throughout

---

## Phase 7: Responsive Design & ChatWindow Integration

**Goal**: Implement responsive layouts for mobile/tablet/desktop and ChatWindow drawer/sheet on smaller viewports

**Independent Test**: View on desktop (verify 3-column layout), tablet (verify sidebar/form/drawer), mobile (verify tabbed interface)

### Tests for Responsive Design ⚠️

- [ ] T110 [P] Write responsive layout test for MainPanel at 768px breakpoint in libris-maleficarum-app/src/__tests__/integration/responsiveLayout.test.tsx
- [ ] T111 [P] Write ChatWindow drawer test for mobile viewport in libris-maleficarum-app/src/__tests__/integration/chatWindowDrawer.test.tsx
- [ ] T112 [P] Write WorldSidebar drawer test for mobile (<768px) in libris-maleficarum-app/src/__tests__/integration/sidebarResponsive.test.tsx

### Implementation for Responsive Design

- [ ] T113 [P] Create responsive CSS Media queries for 768px and 1024px breakpoints in MainPanel.module.css
- [ ] T114 [P] Implement ChatWindow bottom sheet drawer for tablet (768px-1023px) layout using Shadcn Drawer component
  - **Pattern**: Bottom sheet drawer (swipe-up to expand, swipe-down to collapse)
- [ ] T115 [P] Implement ChatWindow bottom sheet tab for mobile (< 768px) layout with tab switcher
  - **Mobile Pattern**: Bottom sheet drawer with swiping affordance (consistent with tablet pattern)
- [ ] T116 [P] Implement WorldSidebar drawer on mobile (<768px) with toggle button (Shadcn Drawer)
  - **Mobile Pattern**: Left-slide drawer with hamburger toggle button
- [ ] T117 [P] Update App.tsx layout wrapper to handle responsive grid/flex changes at 768px/1024px
- [ ] T118 Ensure form panel content is responsive (stack vertically on mobile, side-by-side on desktop)
- [ ] T119 Add touch-friendly targets for mobile (buttons, form inputs min 44px height)
- [ ] T120 Test ChatWindow always discoverable on all viewport sizes (visible on desktop, drawer/tab on mobile)
- [ ] T121 Add orientation change handling for mobile (portrait/landscape)

**Checkpoint**: Responsive layout complete - app works seamlessly on desktop, tablet, and mobile

---

## Phase 8: Efficient Hierarchy Loading with Caching (Priority: P2)

**Goal**: Optimize performance with sessionStorage caching and cache invalidation on mutations

**Independent Test**: Create world with 50+ entities → expand branches → switch world and back → verify instant load from cache

### Tests for User Story 4 (Caching) ⚠️

- [ ] T122 [P] [US4] Write cache hit test in libris-maleficarum-app/src/lib/sessionCache.test.ts
  - **Test**: Verify cached expansion loads instantly (<100ms) without API call
- [ ] T123 [P] [US4] Write cache expiry (TTL) test in libris-maleficarum-app/src/lib/sessionCache.test.ts
  - **Test**: Verify 5-minute TTL causes cache invalidation after expiry
- [ ] T124 [P] [US4] Write cache invalidation test in libris-maleficarum-app/src/__tests__/integration/cacheInvalidation.test.tsx
  - **Test**: Verify cache clears when entity created/updated/deleted
- [ ] T125 [P] [US4] Write performance test for cached vs uncached loads in libris-maleficarum-app/src/__tests__/performance/hierarchyLoad.test.tsx
  - **Test**: Verify 80%+ cache hit rate for typical navigation (SC-008)

### Implementation for User Story 4 (Caching)

- [ ] T126 [US4] Implement 5-minute TTL logic in sessionCache.get() with timestamp validation
  - **Behavior**: Return null if cache age exceeds 5 minutes (300000ms)
- [ ] T127 [US4] Add cache invalidation scope clarification: **Entire world hierarchy MUST invalidate on any entity mutation**
  - **Rationale**: When any entity in a world changes (create, update, delete, move), clear `sidebar_hierarchy_{worldId}` to prevent stale data
  - **Implementation**: RTK Query onQueryStarted hook or mutation.fulfilled listener invalidates cache for that world
- [ ] T128 [US4] Add cache invalidation on entity create mutation (clear world hierarchy cache)
- [ ] T129 [US4] Add cache invalidation on entity update mutation (clear world hierarchy cache)
- [ ] T130 [US4] Add cache invalidation on entity delete mutation (clear world hierarchy cache)
- [ ] T131 [US4] Add cache invalidation on entity move mutation (clear world hierarchy cache)
- [ ] T132 [US4] Implement cache hit rate logging for debugging (log to console/analytics when cache hit/miss occurs)
- [ ] T133 [US4] Add cache restoration logic for expanded state in worldSidebarSlice on world switch
  - **Behavior**: When returning to previously viewed world, restore expanded nodes from cache if within TTL
- [ ] T134 [US4] Performance test: Verify <100ms cached expansion with 100+ entities

**Checkpoint**: Caching fully functional - 80%+ cache hit rate achieved for typical navigation

---

## Phase 9: User Story 5 - Visual Hierarchy and Modern UI Polish (Priority: P3)

**Goal**: Polished visual experience with icons, hover states, themes, smooth scrolling

**Independent Test**: Visual inspection + usability testing - users can identify entity types, understand hierarchy at a glance

### Tests for User Story 5 ⚠️

- [ ] T135 [P] [US5] Write entity icon mapping test in libris-maleficarum-app/src/lib/entityIcons.test.ts
- [ ] T136 [P] [US5] Write visual regression tests for theme support (if tooling available)
- [ ] T137 [P] [US5] Write accessibility contrast test with jest-axe for dark/light themes

### Implementation for User Story 5

- [ ] T138 [P] [US5] Create entity type to Lucide icon mapping in libris-maleficarum-app/src/lib/entityIcons.ts
- [ ] T139 [US5] Add entity type icons to EntityTreeNode
- [ ] T140 [US5] Implement hover states for EntityTreeNode (inline + button appears)
- [ ] T141 [US5] Add smooth scrolling support to WorldSidebar with ScrollArea component
- [ ] T142 [US5] Implement sticky positioning for WorldSelector at top of sidebar
- [ ] T143 [US5] Add expand/collapse chevron icons to EntityTreeNode
- [ ] T144 [US5] Refine visual indentation styling (8-level max) in EntityTree.module.css
- [ ] T145 [US5] Add connecting lines styling between parent-child in EntityTree.module.css
- [ ] T146 [US5] Add dark theme support with appropriate contrast in all WorldSidebar components
- [ ] T147 [US5] Add hover tooltip support for truncated entity names
- [ ] T148 [US5] Polish empty state visual design in EmptyState.module.css

**Checkpoint**: All user stories complete with polished UI - ready for production

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T149 [P] Add JSDoc comments to all public component props and functions
- [ ] T150 [P] Create barrel export (index.ts) for WorldSidebar component
- [ ] T151 [P] Update README.md with WorldSidebar usage instructions
- [ ] T152 [P] Add error boundary around WorldSidebar for graceful error handling
- [ ] T153 Code cleanup: Remove console.logs, ensure consistent formatting
- [ ] T154 Run full test suite and ensure 100% pass rate
- [ ] T155 Run jest-axe accessibility validation across all components
- [ ] T156 Performance profiling: Verify <2s initial load, <100ms cached expansion
- [ ] T157 Security review: Ensure no sensitive data in sessionStorage
- [ ] T158 [P] Backend: Add API integration tests for WorldEntity endpoints
- [ ] T159 Final code review and constitution compliance check

**Checkpoint**: All phases complete - feature ready for production shipping

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
1. Complete Phase 2: Foundational (11 tasks) → Foundation ready
1. Complete Phase 3: US1 - First-Time World Creation (16 tasks) → Demo MVP
1. Complete Phase 4: US2 - World Selection & Hierarchy (25 tasks) → Core feature complete
1. Complete Phase 8: Entity Creation (16 tasks) → Full CRUD available
1. Complete Phase 9: Entity Selection (6 tasks) → Sidebar integrated with main panel
1. **STOP and VALIDATE**: 83 tasks complete, full feature functional

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
