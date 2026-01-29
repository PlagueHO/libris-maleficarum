# Tasks: Entity Type Selector Enhancements

**Input**: Design documents from `/specs/010-entity-selector-enhancements/`  
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md)

**Feature**: Enhance EntityTypeSelector component with icons (16×16px), compact spacing (8px padding), filter UI update ("Filter..." placeholder), and simplified grouping (Recommended + Other sections).

**Tests**: Included per TDD requirement from Constitution Principle III

**Organization**: Tasks grouped by user story for independent implementation and testing.

---

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: User story label (US1, US2, US3, US4)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Verify dependencies and create test scaffolding

- [X] T001 Verify Lucide React icons package installed in libris-maleficarum-app/package.json
- [X] T002 [P] Verify entity type registry contains icon property for all types in libris-maleficarum-app/src/services/config/entityTypeRegistry.ts
- [X] T003 [P] Verify Shadcn/UI Separator component exists in libris-maleficarum-app/src/components/ui/separator.tsx

**Checkpoint**: All dependencies confirmed - ready for TDD test creation

---

## Phase 2: Foundational (Test Scaffolding - TDD Requirement)

**Purpose**: Create failing tests BEFORE implementation per Constitution Principle III

**⚠️ CRITICAL**: Tests MUST be written first and FAIL before implementing changes

- [X] T004 [P] Add test case "displays icons for all entity types" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.test.tsx
- [X] T005 [P] Add test case "icons have correct size (16x16px)" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.test.tsx
- [X] T006 [P] Add test case "icons have aria-hidden attribute" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.test.tsx
- [X] T007 [P] Add test case "list items use compact spacing (py-2)" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.test.tsx
- [X] T008 [P] Add test case "8-10 items visible without scrolling" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.test.tsx
- [X] T009 [P] Add test case "filter placeholder shows 'Filter...'" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.test.tsx
- [X] T010 [P] Add test case "displays Recommended section with star icon" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.test.tsx
- [X] T011 [P] Add test case "displays separator between Recommended and Other" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.test.tsx
- [X] T012 [P] Add test case "displays Other section with alphabetical sorting" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.test.tsx
- [X] T013 [P] Add test case "no sections when no recommendations (flat list)" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.test.tsx
- [X] T014 [P] Add test case "empty state message format 'No entity types match [term]'" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.test.tsx
- [X] T015 [P] Add jest-axe accessibility test for entire component with new layout in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.test.tsx
- [X] T016 Run tests to verify all new test cases FAIL (expected before implementation)

**Checkpoint**: All test cases written and failing (9/12 failed as expected) - foundation ready for user story implementation

---

## Phase 3: User Story 1 - Visual Entity Type Identification (Priority: P1) 🎯 MVP

**Goal**: Display unique icons to the left of each entity type name for rapid visual identification

**Independent Test**: Open EntityTypeSelector dropdown and verify each entity type displays its unique 16×16px icon to the left of the name

### Implementation for User Story 1

- [X] T017 [US1] Import all required Lucide React icon components dynamically in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [X] T018 [US1] Create dynamic icon rendering helper that maps entity type → icon component in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [X] T019 [US1] Add icon rendering to list item template in Recommended section with className="w-4 h-4 flex-shrink-0" aria-hidden="true" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [X] T020 [US1] Add icon rendering to list item template in Other section with className="w-4 h-4 flex-shrink-0" aria-hidden="true" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [X] T021 [US1] Update list item flex container to include gap-2 for icon spacing in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [X] T022 [US1] Run tests T004-T006 to verify icon implementation passes

**Checkpoint**: Icons display correctly for all entity types - User Story 1 complete and independently testable

---

## Phase 4: User Story 2 - Compact List for Better Visibility (Priority: P2)

**Goal**: Reduce vertical spacing to show 8-10 entity types simultaneously without scrolling

**Independent Test**: Open EntityTypeSelector dropdown and count visible entity types - should see 8-10 items without scrolling on 1920x1080 viewport

### Implementation for User Story 2

- [X] T023 [US2] Change list item button padding from py-2.5 to py-2 in Recommended section in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [X] T024 [US2] Change list item button padding from py-2.5 to py-2 in Other section in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [X] T025 [US2] Update title text className to include text-sm for consistent small sizing in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [X] T026 [US2] Update description text className to include leading-tight for reduced line spacing in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [X] T027 [US2] Verify dropdown max-height (max-h-96 or max-h-[450px]) accommodates 8-10 items at ~50px each in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [X] T028 [US2] Run tests T007-T008 to verify compact spacing implementation passes

**Checkpoint**: 8-10 entity types visible without scrolling - User Story 2 complete and independently testable

---

## Phase 5: User Story 3 - Keyboard-Driven Type Selection (Priority: P3)

**Goal**: Update filter placeholder text to "Filter..." for clearer user guidance

**Independent Test**: Open EntityTypeSelector dropdown and verify filter input shows "Filter..." as placeholder text

### Implementation for User Story 3

- [X] T029 [US3] Change Input placeholder prop from "Search entity types..." to "Filter..." in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [X] T030 [US3] Verify existing filter logic (matching against name/description) preserved without changes in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [X] T031 [US3] Verify keyboard navigation (Tab to input, type to filter, Arrow keys to navigate) works correctly in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [X] T032 [US3] Run test T009 to verify filter placeholder implementation passes

**Checkpoint**: Filter placeholder updated and keyboard navigation preserved - User Story 3 complete and independently testable

---

## Phase 6: User Story 4 - Simplified Entity Type Grouping (Priority: P4)

**Goal**: Reorganize entity types into "Recommended" and "Other" sections (removing category-based grouping) for simplified visual hierarchy

**Independent Test**: Open EntityTypeSelector dropdown and verify Recommended section at top with star icon, separator line below, and Other section with alphabetically sorted types

### Implementation for User Story 4

- [ ] T033 [US4] Import Separator component from @/components/ui/separator in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [ ] T034 [US4] Import Star icon from lucide-react for Recommended section heading in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [ ] T035 [US4] Remove category-based grouping logic (categorized variable and Object.entries loop) in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [ ] T036 [US4] Update Recommended section heading to include Star icon component with className="w-3.5 h-3.5" aria-hidden="true" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [ ] T037 [US4] Add Separator component with className="my-2" between Recommended and Other sections (conditional on recommendedFiltered.length > 0) in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [ ] T038 [US4] Create "Other" section heading with className="px-2 py-1.5 text-xs font-semibold text-muted-foreground uppercase tracking-wide" in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [ ] T039 [US4] Implement alphabetical sorting for otherFiltered array using .sort((a, b) => getEntityTypeMeta(a).label.localeCompare(getEntityTypeMeta(b).label)) in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [ ] T040 [US4] Handle edge case: when recommendedFiltered.length === 0, display otherFiltered directly without sections/separator in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [ ] T041 [US4] Update empty state message to use format "No entity types match '{search}'" (with search term interpolated) in libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx
- [ ] T042 [US4] Run tests T010-T014 to verify grouping implementation passes

**Checkpoint**: Recommended/Other sections display correctly with proper separator and sorting - User Story 4 complete and independently testable

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, accessibility checks, and documentation

- [ ] T043 [P] Run full test suite with pnpm test in libris-maleficarum-app/ and verify all tests pass
- [ ] T044 [P] Run test T015 (jest-axe) to verify zero accessibility violations at WCAG 2.2 Level AA
- [ ] T045 [P] Verify keyboard navigation flows seamlessly across Recommended/Other boundary using arrow keys
- [ ] T046 [P] Test edge case: very long entity type names truncate with ellipsis
- [ ] T047 [P] Test edge case: narrow viewport (~800px) maintains readability with truncation
- [ ] T048 Run quickstart.md validation checklist for visual and functional verification
- [ ] T049 Run pnpm lint in libris-maleficarum-app/ and fix any linting errors
- [ ] T050 Run pnpm build in libris-maleficarum-app/ and verify zero build errors
- [ ] T051 [P] Update CHANGELOG.md with feature enhancement summary
- [ ] T052 [P] Run performance test: verify filter input response <100ms (manual DevTools Performance tab check)
- [ ] T053 [P] Run performance test: verify dropdown render time <200ms (manual DevTools Performance tab check)

**Checkpoint**: All tests passing, accessibility validated, performance verified, ready for code review

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - start immediately
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS all user stories (TDD requirement)
- **User Story 1 (Phase 3)**: Depends on Foundational - MVP starting point
- **User Story 2 (Phase 4)**: Depends on Foundational - Can run parallel to US1 if different developers
- **User Story 3 (Phase 5)**: Depends on Foundational - Can run parallel to US1/US2
- **User Story 4 (Phase 6)**: Depends on Foundational - Can run parallel to US1/US2/US3
- **Polish (Phase 7)**: Depends on completion of desired user stories

### User Story Dependencies

- **US1 (P1 - Icons)**: Independent - No dependencies on other stories
- **US2 (P2 - Spacing)**: Independent - No dependencies on other stories
- **US3 (P3 - Filter)**: Independent - No dependencies on other stories
- **US4 (P4 - Grouping)**: Independent - No dependencies on other stories

**All user stories can be implemented in parallel by different team members after Foundational phase completes.**

### Within Each User Story

- Tests written first and verified to FAIL (Phase 2)
- Implementation tasks in sequence (import → create → add → update → verify)
- Run related tests to verify implementation passes
- Story checkpoint confirms independent functionality

### Parallel Opportunities (Single Developer)

**Phase 1 Setup**: T001, T002, T003 can run in parallel (read-only verification tasks)

**Phase 2 Foundational**: T004-T015 can run in parallel (all independent test cases in same file)

**User Stories** (if  multiple developers available):

- After Foundational complete, all 4 user stories (US1-US4) can proceed in parallel
- Each developer takes a user story and works through its tasks sequentially

**Phase 7 Polish**: T043-T047, T051-T053 can run in parallel (independent validation tasks)

---

## Parallel Example: Multiple Developers

```bash
# After Phase 2 Foundational completes:

Developer A → User Story 1 (Icons):        T017-T022
Developer B → User Story 2 (Spacing):      T023-T028  
Developer C → User Story 3 (Filter):       T029-T032
Developer D → User Story 4 (Grouping):     T033-T042

# Or if solo: complete in priority order (P1 → P2 → P3 → P4)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only - Recommended)

1. Complete Phase 1: Setup (T001-T003)
1. Complete Phase 2: Foundational tests (T004-T016) - verify FAIL
1. Complete Phase 3: User Story 1 - Icons (T017-T022)
1. **STOP and VALIDATE**: Test US1 independently (icons display correctly)
1. Deploy/demo if ready (visual improvement delivered)

### Incremental Delivery (Recommended for Solo Developer)

1. Setup + Foundational → Tests ready
1. Add US1 (Icons) → Test → Validate → Commit → **Demo MVP!**
1. Add US2 (Spacing) → Test → Validate → Commit → Demo enhancement
1. Add US3 (Filter) → Test → Validate → Commit → Demo enhancement  
1. Add US4 (Grouping) → Test → Validate → Commit → Demo complete feature
1. Complete Polish → Final validation → Create PR

### Parallel Team Strategy (If Multiple Developers Available)

1. Team completes Setup + Foundational together (T001-T016)
1. Once Foundational done:
   - Dev A: US1 (Icons)
   - Dev B: US2 (Spacing)
   - Dev C: US3 (Filter)
   - Dev D: US4 (Grouping)
1. Each developer tests their story independently
1. Merge stories together
1. Team completes Polish phase together

---

## Task Summary

**Total Tasks**: 53  
**By Phase**:

- Phase 1 (Setup): 3 tasks
- Phase 2 (Foundational/Tests): 13 tasks
- Phase 3 (US1 - Icons): 6 tasks
- Phase 4 (US2 - Spacing): 6 tasks
- Phase 5 (US3 - Filter): 4 tasks
- Phase 6 (US4 - Grouping): 10 tasks
- Phase 7 (Polish): 11 tasks

**Parallel Opportunities**: 23 tasks marked [P] (can run in parallel within their phases)

**Independent Test Criteria**:

- US1: Icons display for all entity types (16×16px, aria-hidden)
- US2: 8-10 items visible without scrolling
- US3: Filter placeholder shows "Filter..."
- US4: Recommended + Separator + Other sections with alphabetical sorting

**MVP Scope**: Just US1 (6 implementation tasks + 3 related tests) delivers immediate visual improvement

**Suggested Execution Order** (Solo Developer): Setup → Foundational → US1 → US2 → US3 → US4 → Polish

---

## Notes

- All tasks follow TDD: tests written in Phase 2 BEFORE implementation
- Each user story independently testable at its checkpoint
- [P] tasks = different files or independent test cases
- [Story] labels (US1-US4) map to spec.md user story priorities (P1-P4)
- File path: All modifications target `libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx` and its test file
- Accessibility validation mandatory (T044-T045) per Constitution Principle III
- Performance validation included (T052-T053) per spec success criteria
- Commit after each user story checkpoint for clean git history
