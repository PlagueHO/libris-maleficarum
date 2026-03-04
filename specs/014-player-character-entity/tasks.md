# Tasks: Player Character Entity Type

**Input**: Design documents from `/specs/014-player-character-entity/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contracts.md, quickstart.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing. User Stories 1–5 (all property fields) map to a single registry entry and are combined in Phase 3. User Story 6 (hierarchy) is Phase 4.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US6)
- Include exact file paths in descriptions

## Path Conventions

- **Backend**: `libris-maleficarum-service/src/`
- **Frontend**: `libris-maleficarum-app/src/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: No setup tasks required — project structure, build tooling, and all dependencies already exist.

*(No tasks — proceed to Phase 2)*

---

## Phase 2: Foundational — Backend Entity Type Support (Blocking)

**Purpose**: Add `PlayerCharacter` to the backend enum and schema config. The backend **MUST** be updated before the frontend can create PlayerCharacter entities (the `IsInEnum()` validator would reject the new type otherwise).

**⚠️ CRITICAL**: No frontend work can begin until this phase is complete.

- [X] T001 Add `PlayerCharacter` enum value between `Character` and `Faction` in the "Character & faction types" group in `libris-maleficarum-service/src/Domain/ValueObjects/EntityType.cs`
- [X] T002 [P] Add `"PlayerCharacter": { "MinVersion": 1, "MaxVersion": 1 }` to `EntitySchemaVersions.EntityTypes` in `libris-maleficarum-service/src/Api/appsettings.json`
- [X] T003 Run backend unit tests to verify zero regressions: `dotnet test --filter TestCategory=Unit --configuration Release` in `libris-maleficarum-service/`

**Checkpoint**: Backend accepts `PlayerCharacter` as a valid entity type. All existing backend tests pass.

---

## Phase 3: US1 + US2 + US3 + US4 + US5 — Create Player Character Entity (P1) 🎯 MVP

**Goal**: Register the PlayerCharacter entity type with all 18 property schema fields, icon mapping, and test updates. This single registry entry satisfies US1 (create PC), US2 (personality/backstory fields), US3 (class/race/level), US4 (factions/relationships), and US5 (equipment/abilities) since all are properties in one config object.

**Independent Test**: Create a new PlayerCharacter from the entity type selector, verify all 18 form fields render, fill them in, save, and confirm data round-trips. Verify the PC uses the UserCheck icon and appears under "Characters & Factions" category.

- [X] T004 [US1] Add `PlayerCharacter` registry entry with all 18 `propertySchema` fields (PlayerName, Race, Class, Subclass, Level, Background, Alignment, PersonalityTraits, Ideals, Bonds, Flaws, Backstory, Appearance, Factions, NotableEquipment, SignatureAbilities, Languages, Relationships) after the `Character` entry in `libris-maleficarum-app/src/services/config/entityTypeRegistry.ts` — set category `'Characters & Factions'`, icon `'UserCheck'`, schemaVersion `1`, suggestedChildren `['Item', 'Quest']`, canBeRoot `true`
- [X] T005 [P] [US1] Add `'PlayerCharacter'` to the `EntityType` union type and add `PlayerCharacter: UserCheck` to `entityIconMap` (importing `UserCheck` from `lucide-react`) in `libris-maleficarum-app/src/lib/entityIcons.ts`
- [X] T006 [P] [US1] Import `UserCheck` from `lucide-react` and add `PlayerCharacter: UserCheck` to the `iconMap` Record in `libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx`
- [X] T007 [US1] Update entity type count from 29 to 30 in `libris-maleficarum-app/src/__tests__/services/entityTypeRegistry.test.ts` (line 108) and `libris-maleficarum-app/src/__tests__/config/entityTypeRegistry.test.ts` (line 192), and add `'PlayerCharacter'` to the `expectedRootTypes` array in `libris-maleficarum-app/src/__tests__/config/entityTypeRegistry.test.ts` (line 143)

**Checkpoint**: PlayerCharacter entity type is fully registered. The entity type selector shows "Player Character" under "Characters & Factions" with the UserCheck icon. All 18 property form fields render. All registry tests pass with updated counts.

---

## Phase 4: US6 — Player Character Appears in Entity Hierarchy (P1)

**Goal**: PlayerCharacter appears as a suggested child type when creating entities under Campaign, Session, Faction, City, People, and Folder parent types.

**Independent Test**: Navigate to a Campaign entity, create a child → "Player Character" appears in the entity type selector. Repeat for Session, Faction, City, People, and Folder parents.

- [X] T008 [US6] Add `'PlayerCharacter'` to `suggestedChildren` arrays for `Campaign`, `Session`, `Faction`, `City`, `People`, and `Folder` entity types in `libris-maleficarum-app/src/services/config/entityTypeRegistry.ts`

**Checkpoint**: PlayerCharacter is discoverable as a child entity under all 6 specified parent types. All existing tests still pass.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Final validation across both frontend and backend

- [X] T009 Run all frontend tests (`pnpm test` in `libris-maleficarum-app/`) to verify zero regressions across registry, accessibility, and component tests
- [X] T010 Run quickstart.md verification checklist: confirm PlayerCharacter in entity type selector, UserCheck icon distinct from Character's User icon, all 18 fields render in form, entity saves and round-trips, PC appears under Campaign/Session/Faction/City/People/Folder parents

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No tasks — skip
- **Foundational (Phase 2)**: No dependencies — start immediately. **BLOCKS all frontend work.**
- **US1–US5 (Phase 3)**: Depends on Phase 2 completion (backend must accept PlayerCharacter)
- **US6 (Phase 4)**: Depends on Phase 3 T004 (registry entry must exist before suggestedChildren make sense)
- **Polish (Phase 5)**: Depends on Phases 3 and 4 completion

### User Story Dependencies

- **US1+US2+US3+US4+US5 (Phase 3)**: Can start after Phase 2. All map to a single registry config object — cannot be split.
- **US6 (Phase 4)**: Can start after T004 in Phase 3 (needs the PlayerCharacter type to exist in registry)

### Within Each Phase

**Phase 2** (Backend):

- T001 and T002 can run in parallel (different files: `EntityType.cs` vs `appsettings.json`)
- T003 runs after T001 and T002 (validation step)

**Phase 3** (Frontend — US1–US5):

- T004 first (adds the registry entry that T005, T006, T007 reference)
- T005 and T006 can run in parallel (different files: `entityIcons.ts` vs `EntityTypeSelector.tsx`)
- T007 after T004 (updates test counts to match new registry size)

**Phase 4** (Frontend — US6):

- T008 after T004 (adds to suggestedChildren in same file as registry entry)

### Parallel Opportunities

```text
Phase 2 — parallel batch:
  T001 (EntityType.cs) || T002 (appsettings.json)
  → T003 (verify)

Phase 3 — parallel batch:
  T004 (entityTypeRegistry.ts — add entry)
  → T005 (entityIcons.ts) || T006 (EntityTypeSelector.tsx)
  → T007 (test files)

Phase 4 — sequential:
  T008 (entityTypeRegistry.ts — suggestedChildren)

Phase 5 — parallel batch:
  T009 (run tests) || T010 (manual verification)
```

---

## Implementation Strategy

### MVP First (User Stories 1–5)

1. Complete Phase 2: Backend foundational (T001–T003)
1. Complete Phase 3: Registry entry + icons + tests (T004–T007)
1. **STOP and VALIDATE**: PlayerCharacter is creatable with all 18 fields
1. Deploy backend, then frontend

### Incremental Delivery

1. Phase 2 → Backend ready (PlayerCharacter accepted by API)
1. Phase 3 → MVP: PC entity type fully functional (create, edit, save, load)
1. Phase 4 → Full hierarchy integration (PC appears under parent types)
1. Phase 5 → Validated delivery

### Suggested MVP Scope

Phase 2 + Phase 3 = **7 tasks** deliver a fully functional PlayerCharacter entity type. Phase 4 adds hierarchy integration (1 task). Total: **10 tasks**.

---

## Notes

- All 18 property fields are in a single `propertySchema` array within one registry config object — they cannot be practically split across tasks
- US1–US5 are combined because they all map to properties in the same config entry
- No new files are created — all changes modify existing files
- Backend change is additive (new enum value) and non-breaking for existing data
- Entity type count in tests: 29 → 30
- Expected root types in tests: add `'PlayerCharacter'` (canBeRoot: true)
