# Tasks: Container Entity Type Support

**Feature Branch**: `004-entity-type-support`  
**Input**: Design documents from `/specs/004-entity-type-support/`  
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/API.md ✅, quickstart.md ✅

**Tests**: All new components require `.test.tsx` files with jest-axe accessibility testing (TDD mandatory per project standards)

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `- [ ] [ID] [P?] [Story?] Description with file path`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3, US4)
- All paths are absolute from repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization - no feature-specific work yet

- [ ] T001 Ensure Node.js 20.x installed and pnpm available in libris-maleficarum-app/
- [ ] T002 Verify Fluent UI v9 packages installed: @fluentui/react-components, @fluentui/react-icons
- [ ] T003 [P] Create directory structure: libris-maleficarum-app/src/components/shared/TagInput/
- [ ] T004 [P] Create directory structure: libris-maleficarum-app/src/lib/validators/
- [ ] T005 [P] Create directory structure: libris-maleficarum-app/src/components/MainPanel/EntityDetailForm/ (may exist)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core type system and reusable components that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Type System Extensions (MUST complete first)

- [ ] T006 Extend WorldEntityType const object with 14 new types in libris-maleficarum-app/src/services/types/worldEntity.types.ts
- [ ] T007 Extend ENTITY_TYPE_META with metadata for 14 new types (label, description, category, icon) in libris-maleficarum-app/src/services/types/worldEntity.types.ts
- [ ] T008 Extend ENTITY_TYPE_SUGGESTIONS with mappings for 9 Container types and 4 Regional types in libris-maleficarum-app/src/services/types/worldEntity.types.ts
- [ ] T009 Update getEntityTypeSuggestions function to recommend Container types for root-level entities in libris-maleficarum-app/src/services/types/worldEntity.types.ts
- [ ] T010 [P] Create unit tests for new WorldEntityType enum values in libris-maleficarum-app/src/services/types/worldEntity.types.test.ts
- [ ] T011 [P] Create unit tests for ENTITY_TYPE_META additions in libris-maleficarum-app/src/services/types/worldEntity.types.test.ts
- [ ] T012 [P] Create unit tests for getEntityTypeSuggestions with Container/Regional types in libris-maleficarum-app/src/services/types/worldEntity.types.test.ts

### Reusable Components (Can start after type system)

- [ ] T013 [P] Create TagInput component interface (TagInputProps) in libris-maleficarum-app/src/components/shared/TagInput/TagInput.tsx
- [ ] T014 [P] Implement TagInput component with Fluent UI Tag, Input, Field primitives in libris-maleficarum-app/src/components/shared/TagInput/TagInput.tsx
- [ ] T015 [P] Add keyboard handling (Enter to add tag, dismiss to remove) in libris-maleficarum-app/src/components/shared/TagInput/TagInput.tsx
- [ ] T016 [P] Create TagInput styles in libris-maleficarum-app/src/components/shared/TagInput/TagInput.module.css
- [ ] T017 [P] Create TagInput barrel export in libris-maleficarum-app/src/components/shared/TagInput/index.ts
- [ ] T018 Create TagInput accessibility tests with jest-axe in libris-maleficarum-app/src/components/shared/TagInput/TagInput.test.tsx
- [ ] T019 [P] Create TagInput functionality tests (add tag, remove tag, prevent duplicates) in libris-maleficarum-app/src/components/shared/TagInput/TagInput.test.tsx

### Validation Utilities

- [ ] T020 [P] Create parseNumericInput function in libris-maleficarum-app/src/lib/validators/numericValidation.ts
- [ ] T021 [P] Create formatNumericDisplay function in libris-maleficarum-app/src/lib/validators/numericValidation.ts
- [ ] T022 [P] Create validateInteger function in libris-maleficarum-app/src/lib/validators/numericValidation.ts
- [ ] T023 [P] Create validateDecimal function in libris-maleficarum-app/src/lib/validators/numericValidation.ts
- [ ] T024 Create unit tests for parseNumericInput in libris-maleficarum-app/src/lib/validators/numericValidation.test.ts
- [ ] T025 [P] Create unit tests for formatNumericDisplay in libris-maleficarum-app/src/lib/validators/numericValidation.test.ts
- [ ] T026 [P] Create unit tests for validateInteger (bounds, non-negative, whole number) in libris-maleficarum-app/src/lib/validators/numericValidation.test.ts
- [ ] T027 [P] Create unit tests for validateDecimal (bounds, non-negative, decimals allowed) in libris-maleficarum-app/src/lib/validators/numericValidation.test.ts

**Checkpoint**: Foundation ready - type system extended, TagInput reusable, validators ready. User story implementation can now begin in parallel.

---

## Phase 3: User Story 1 - Organize World Content with Container Types (Priority: P1) 🎯 MVP

**Goal**: Users can create Container entities (Locations, People, Events, etc.) to organize world content into logical top-level categories

**Independent Test**: Create a new world → Add Locations container as root entity → Add Continent as child of Locations → Verify hierarchy displays with folder icon and correct recommendations

### Implementation for User Story 1

- [ ] T028 [P] [US1] Update WorldSidebar icon mapping to include all 14 new types in libris-maleficarum-app/src/components/SidePanel/WorldSidebar.tsx
- [ ] T029 [P] [US1] Import Fluent UI icons (FolderRegular, PeopleRegular, CalendarRegular, BookRegular, PawRegular, BoxRegular, CompassRegular, MapRegular, GlobeRegular, ShieldRegular) in libris-maleficarum-app/src/components/SidePanel/WorldSidebar.tsx
- [ ] T030 [US1] Create getEntityIcon function using ENTITY_TYPE_META.icon property in libris-maleficarum-app/src/components/SidePanel/WorldSidebar.tsx
- [ ] T031 [US1] Update entity rendering to use getEntityIcon with aria-hidden="true" in libris-maleficarum-app/src/components/SidePanel/WorldSidebar.tsx
- [ ] T032 [US1] Add CSS for Container icon styling in libris-maleficarum-app/src/components/SidePanel/WorldSidebar.module.css
- [ ] T033 [P] [US1] Create accessibility test for icon display in libris-maleficarum-app/src/components/SidePanel/WorldSidebar.test.tsx
- [ ] T034 [P] [US1] Create visual regression test for Container vs Standard entity icons in libris-maleficarum-app/src/components/SidePanel/WorldSidebar.test.tsx
- [ ] T035 [US1] Update EntityTypeSelector to categorize Container types separately in libris-maleficarum-app/src/components/EntityTypeSelector/EntityTypeSelector.tsx (if component exists; adjust path as needed)
- [ ] T036 [US1] Verify EntityTypeSelector shows Container types in recommended section for root-level entities in libris-maleficarum-app/src/components/EntityTypeSelector/EntityTypeSelector.tsx
- [ ] T037 [US1] Create tests for Container type recommendations in EntityTypeSelector in libris-maleficarum-app/src/components/EntityTypeSelector/EntityTypeSelector.test.tsx

**Checkpoint**: At this point, Container entities can be created, display with correct icons, and are recommended appropriately. This is the MVP.

---

## Phase 4: User Story 2 - Create Entities with Custom Properties (Priority: P1)

**Goal**: Users can create Regional entities (GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion) with domain-specific properties stored in the Properties JSON field

**Independent Test**: Create Continent → Add GeographicRegion child → Fill Climate, Terrain, Population, Area → Submit → Verify entity saved with Properties → Reload page → Verify properties display in read-only mode

### Custom Property Components

- [ ] T038 [P] [US2] Create GeographicRegionProperties component interface in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm/GeographicRegionProperties.tsx
- [ ] T039 [P] [US2] Implement GeographicRegionProperties with Climate, Terrain, Population, Area fields in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm/GeographicRegionProperties.tsx
- [ ] T040 [P] [US2] Add numeric validation for Population (integer) and Area (decimal) in GeographicRegionProperties in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm/GeographicRegionProperties.tsx
- [ ] T041 [P] [US2] Create PoliticalRegionProperties component with GovernmentType, MemberStates (TagInput), EstablishedDate in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm/PoliticalRegionProperties.tsx
- [ ] T042 [P] [US2] Create CulturalRegionProperties component with Languages (TagInput), Religions (TagInput), CulturalTraits in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm/CulturalRegionProperties.tsx
- [ ] T043 [P] [US2] Create MilitaryRegionProperties component with CommandStructure, StrategicImportance, MilitaryAssets (TagInput) in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm/MilitaryRegionProperties.tsx
- [ ] T044 [P] [US2] Create accessibility tests for GeographicRegionProperties in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm/GeographicRegionProperties.test.tsx
- [ ] T045 [P] [US2] Create accessibility tests for PoliticalRegionProperties in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm/PoliticalRegionProperties.test.tsx
- [ ] T046 [P] [US2] Create accessibility tests for CulturalRegionProperties in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm/CulturalRegionProperties.test.tsx
- [ ] T047 [P] [US2] Create accessibility tests for MilitaryRegionProperties in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm/MilitaryRegionProperties.test.tsx

### EntityDetailForm Integration

- [ ] T048 [US2] Add customProperties state to EntityDetailForm in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.tsx
- [ ] T049 [US2] Create renderCustomProperties helper function with switch-based conditional rendering in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.tsx
- [ ] T050 [US2] Add renderCustomProperties() call in EntityDetailForm JSX layout in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.tsx
- [ ] T051 [US2] Update form submit handler to include Properties field in request payload in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.tsx
- [ ] T052 [US2] Update form load handler to deserialize Properties from API response in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.tsx
- [ ] T053 [US2] Add CSS for custom properties section layout in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.module.css
- [ ] T054 [P] [US2] Create test for GeographicRegion custom properties rendering in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.test.tsx
- [ ] T055 [P] [US2] Create test for PoliticalRegion custom properties rendering in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.test.tsx
- [ ] T056 [P] [US2] Create test for entity submission with Properties field in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.test.tsx
- [ ] T057 [P] [US2] Create test for entity loading with Properties deserialization in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.test.tsx

**Checkpoint**: At this point, Regional entities can be created with custom properties, properties persist to Cosmos DB, and display correctly in read-only mode.

---

## Phase 5: User Story 3 - Flexible Entity Placement Without Restrictions (Priority: P1)

**Goal**: Users can organize entities in unconventional ways (Character under World, City under Campaign) without system-imposed restrictions

**Independent Test**: Attempt unusual entity relationships (Monster → Continent, Character → World, Locations → Character) → Verify all combinations allowed without validation errors

### Implementation for User Story 3

- [ ] T058 [US3] Verify EntityTypeSelector allows ALL entity types regardless of parent (no restrictions) in libris-maleficarum-app/src/components/EntityTypeSelector/EntityTypeSelector.tsx
- [ ] T059 [US3] Ensure form validation does NOT reject unconventional parent-child combinations in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.tsx
- [ ] T060 [US3] Update WorldSidebar hierarchy rendering to support arbitrary nesting depth in libris-maleficarum-app/src/components/SidePanel/WorldSidebar.tsx
- [ ] T061 [P] [US3] Create test for creating Character directly under World (bypassing containers) in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.test.tsx
- [ ] T062 [P] [US3] Create test for nesting GeographicRegion → GeographicRegion → GeographicRegion (deep nesting) in libris-maleficarum-app/src/components/SidePanel/WorldSidebar.test.tsx
- [ ] T063 [P] [US3] Create test for placing Campaign-specific entities under non-Campaign parents in libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.test.tsx

**Checkpoint**: All entity type combinations are allowed. System provides smart suggestions but never enforces restrictions.

---

## Phase 6: User Story 4 - Context-Aware Type Recommendations (Priority: P2)

**Goal**: Users benefit from intelligent type suggestions based on parent entity type (Continent → suggests GeographicRegion, Country)

**Independent Test**: Create child entities under various parents (Continent, City, Campaign, Character) → Verify recommended types appear in top slots → Verify all types remain accessible via scroll/search

### Implementation for User Story 4

- [ ] T064 [US4] Verify ENTITY_TYPE_SUGGESTIONS updated for existing types (Continent → GeographicRegion, PoliticalRegion) in libris-maleficarum-app/src/services/types/worldEntity.types.ts (may be done in T008)
- [ ] T065 [US4] Update EntityTypeSelector to display recommended types in top section in libris-maleficarum-app/src/components/EntityTypeSelector/EntityTypeSelector.tsx
- [ ] T066 [US4] Ensure EntityTypeSelector maintains search functionality across all types in libris-maleficarum-app/src/components/EntityTypeSelector/EntityTypeSelector.tsx
- [ ] T067 [US4] Add CSS for recommended vs non-recommended type visual distinction in libris-maleficarum-app/src/components/EntityTypeSelector/EntityTypeSelector.module.css
- [ ] T068 [P] [US4] Create test for Continent parent suggesting GeographicRegion in top 5 recommendations in libris-maleficarum-app/src/components/EntityTypeSelector/EntityTypeSelector.test.tsx
- [ ] T069 [P] [US4] Create test for City parent suggesting Building, Location, Character in top recommendations in libris-maleficarum-app/src/components/EntityTypeSelector/EntityTypeSelector.test.tsx
- [ ] T070 [P] [US4] Create test for EntityTypeSelector search returning results <100ms in libris-maleficarum-app/src/components/EntityTypeSelector/EntityTypeSelector.test.tsx
- [ ] T071 [P] [US4] Create test for non-recommended types remaining accessible via scroll in libris-maleficarum-app/src/components/EntityTypeSelector/EntityTypeSelector.test.tsx

**Checkpoint**: Smart recommendations improve UX for common workflows while maintaining full flexibility for creative structures.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T072 [P] Update quickstart.md validation: manually test all 7 steps in libris-maleficarum-app/ against actual implementation
- [ ] T073 [P] Code cleanup: Remove console.log statements, unused imports across all modified files
- [ ] T074 [P] Refactor: Extract common validation logic from custom property components to shared utility
- [ ] T075 [P] Documentation: Add JSDoc comments to public TagInput API in libris-maleficarum-app/src/components/shared/TagInput/TagInput.tsx
- [ ] T076 [P] Documentation: Add JSDoc comments to numeric validation functions in libris-maleficarum-app/src/lib/validators/numericValidation.ts
- [ ] T077 Run full test suite: `pnpm test` and verify >80% coverage for new code
- [ ] T078 Run accessibility audit: `pnpm test --grep "accessibility"` and verify zero violations
- [ ] T079 Performance: Profile EntityTypeSelector search with 29 types and optimize if >100ms
- [ ] T080 [P] Create integration test for complete entity creation flow (World → Locations → Continent → GeographicRegion with properties) in libris-maleficarum-app/tests/integration/entityCreation.test.tsx
- [ ] T081 Update CHANGELOG.md with feature summary and user-facing changes
- [ ] T082 Update cleanup checklist in plan.md: Remove "Active Feature" section from .github/copilot-instructions.md after merge

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - **BLOCKS all user stories**
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - User stories CAN proceed in parallel (different components)
  - Or sequentially in priority order (P1 → P2)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Container icons and recommendations - Can start after Foundational (Phase 2) - **No dependencies on other stories**
- **User Story 2 (P1)**: Custom properties - Can start after Foundational (Phase 2) - **No dependencies on other stories** (uses TagInput from Phase 2)
- **User Story 3 (P1)**: Flexible placement - Can start after Foundational (Phase 2) - **Minimal dependencies** (may need US1 icon rendering complete)
- **User Story 4 (P2)**: Context-aware recommendations - Can start after Foundational (Phase 2) - **No dependencies on other stories**

### Within Each User Story

- **US1**: Icon imports → getEntityIcon function → WorldSidebar rendering → Tests
- **US2**: Property components (parallel) → EntityDetailForm integration → Tests
- **US3**: Validation removal → Hierarchy rendering updates → Tests
- **US4**: Recommendation logic (may be done in Phase 2) → EntityTypeSelector UI → Tests

### Parallel Opportunities

#### Foundational Phase (After T006-T009 type system complete)

- TagInput component (T013-T019) can run in parallel with Validators (T020-T027)

#### User Story Phase (After Foundational complete)

- All 4 user stories can be worked on in parallel by different team members:
  - Developer A: User Story 1 (WorldSidebar icons)
  - Developer B: User Story 2 (Custom properties)
  - Developer C: User Story 3 (Flexible placement)
  - Developer D: User Story 4 (Recommendations)

#### Within User Story 2

All 4 property components can be built in parallel:

- T038-T040 (GeographicRegionProperties)
- T041 (PoliticalRegionProperties)
- T042 (CulturalRegionProperties)
- T043 (MilitaryRegionProperties)
- T044-T047 (Tests for all 4 components)

#### Polish Phase

- T072 (quickstart validation), T073 (code cleanup), T074 (refactor), T075-T076 (docs) can run in parallel

---

## Parallel Example: User Story 2 Custom Properties

```bash
# Launch all 4 property components together:
Task: "Create GeographicRegionProperties component with Climate, Terrain, Population, Area"
Task: "Create PoliticalRegionProperties component with GovernmentType, MemberStates, EstablishedDate"
Task: "Create CulturalRegionProperties component with Languages, Religions, CulturalTraits"
Task: "Create MilitaryRegionProperties component with CommandStructure, StrategicImportance, MilitaryAssets"

# Then integrate all into EntityDetailForm
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T005)
1. Complete Phase 2: Foundational (T006-T027) - **CRITICAL**
1. Complete Phase 3: User Story 1 (T028-T037)
1. **STOP and VALIDATE**: Test Container entities display with correct icons
1. Deploy/demo MVP

**Estimated Time**: 8-12 hours (per quickstart.md)

### Incremental Delivery

1. Foundation ready (Setup + Foundational) → ~4 hours
1. Add User Story 1 → Test independently → Deploy/Demo (MVP!) → ~2 hours
1. Add User Story 2 → Test independently → Deploy/Demo → ~4 hours
1. Add User Story 3 → Test independently → Deploy/Demo → ~1 hour
1. Add User Story 4 → Test independently → Deploy/Demo → ~2 hours
1. Polish → ~2 hours

**Total**: 15 hours with tests

### Parallel Team Strategy

With 2 developers after Foundational phase:

- **Developer A**: User Story 1 (icons) + User Story 3 (flexibility)
- **Developer B**: User Story 2 (custom properties) + User Story 4 (recommendations)

**Total Time**: 8-10 hours with parallelization

---

## Summary

- **Total Tasks**: 82 tasks
- **Parallel Tasks**: 44 tasks marked [P] can run concurrently within their phase
- **User Stories**: 4 (3 at P1 priority, 1 at P2 priority)
- **MVP Scope**: User Story 1 (Container types with icons) = ~6 hours
- **Full Feature**: All 4 user stories + polish = ~15 hours
- **Test Coverage**: 27 test tasks ensure TDD compliance and accessibility
- **Independent Testing**: Each user story has clear test criteria and can be validated independently

**Next Steps**: Start with Phase 1 (Setup), proceed to Phase 2 (Foundational - BLOCKS ALL), then begin MVP (User Story 1).
