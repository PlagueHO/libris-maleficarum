# Research: Player Character Entity Type

**Feature**: 014-player-character-entity
**Date**: 2026-02-18

## Research Tasks & Findings

### R1: Icon Availability — "UserCheck" in Lucide React

**Task**: Verify that the "UserCheck" icon exists in the lucide-react package.

**Finding**: The project uses lucide-react for all entity icons. Icon names in the registry are strings (e.g., `'User'`, `'Globe'`) mapped to Lucide components in two locations:

- `src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx` — `iconMap: Record<string, LucideIcon>`
- `src/lib/entityIcons.ts` — `entityIconMap: Record<EntityType, LucideIcon>` + `EntityType` union type

`UserCheck` is a standard Lucide icon ([lucide.dev/icons/user-check](https://lucide.dev/icons/user-check)). It is **not currently imported** in either file — both need updating.

**Decision**: Use `UserCheck` (confirmed available in lucide-react).
**Rationale**: Visually distinct from `User` (NPC) — adds a checkmark overlay to indicate a "verified/player" character.
**Alternatives considered**: `UserPlus` (too similar to "add user"), `UserCog` (implies settings), `Shield` (already used).

---

### R2: Backend EntityType Enum — Adding PlayerCharacter

**Task**: Determine how to add `PlayerCharacter` to the backend `EntityType` enum and related config.

**Finding**: The backend uses:

1. `Domain/ValueObjects/EntityType.cs` — C# enum with all entity types grouped by category
1. `Api/appsettings.json` — `EntitySchemaVersions.EntityTypes` section mapping entity type names to `{ MinVersion, MaxVersion }` ranges
1. `Domain/Configuration/EntitySchemaVersionConfig.cs` — Reads the appsettings config, defaults to `{ Min: 1, Max: 1 }` for unconfigured types
1. `Api/Validators/CreateWorldEntityRequestValidator.cs` — Validates `EntityType` via `.IsInEnum()`

Entity type is stored as a **string** in Cosmos DB (via `HasConversion<string>()` in `WorldEntityConfiguration.cs`). Adding a new enum value is additive and non-breaking: existing documents are unaffected, the validator uses `IsInEnum()` which automatically includes new values.

**Decision**: Add `PlayerCharacter` to the enum between `Character` and `Faction` in the "Character & faction types" group. Add schema version config to appsettings.
**Rationale**: Follows existing grouping convention; additive change to enum; schema version range { Min: 1, Max: 1 } matches all other types.
**Alternatives considered**: Using existing `Character` type with a flag (rejected — spec explicitly requires distinct types with different schemas).

---

### R3: Frontend Entity Registry — Pattern for New Entity Types

**Task**: Confirm the registry-driven pattern for adding entity types and what derived constants update automatically.

**Finding**: The Entity Type Registry (`entityTypeRegistry.ts`) is the single source of truth. Adding a new entry to `ENTITY_TYPE_REGISTRY` automatically:

- Adds to `WorldEntityType` union type (derived via `reduce()` in `worldEntity.types.ts`)
- Adds to `ENTITY_SCHEMA_VERSIONS` map
- Adds to `ENTITY_TYPE_META` lookup
- Adds to `ENTITY_TYPE_SUGGESTIONS` lookup
- Makes it available in `EntityTypeSelector` (reads from registry)
- Makes `DynamicPropertiesForm` / `DynamicPropertiesView` render its `propertySchema` fields

No new component files are needed — the extensibility test (`entityTypeRegistry.extensibility.test.tsx`) explicitly validates this pattern.

**Decision**: Add `PlayerCharacter` config entry to `ENTITY_TYPE_REGISTRY` array.
**Rationale**: Follows established pattern — single config entry drives the entire UI.
**Alternatives considered**: None — the registry-driven pattern is the correct and tested approach.

---

### R4: Test Impact — Existing Tests with Hardcoded Counts

**Task**: Identify tests that will break when adding a new entity type.

**Finding**: The following tests have hardcoded values that need updating:

1. `src/__tests__/services/entityTypeRegistry.test.ts`:
   - T026: `expect(ENTITY_TYPE_REGISTRY).toHaveLength(29)` → must become 30
1. `src/__tests__/config/entityTypeRegistry.test.ts`:
   - T033: `expect(allTypes).toHaveLength(29)` → must become 30
   - T032: `expectedRootTypes` array must include `'PlayerCharacter'` (since `canBeRoot: true`)
1. `src/lib/entityIcons.ts`:
   - `EntityType` union type must include `'PlayerCharacter'`
   - `entityIconMap` must include `PlayerCharacter: UserCheck`

**Decision**: Update all hardcoded counts and lists as part of the implementation.
**Rationale**: These tests validate registry integrity — they must reflect the new entity type.

---

### R5: suggestedChildren Updates — Which Parent Types Need PlayerCharacter

**Task**: Identify all entity types whose `suggestedChildren` must include `PlayerCharacter`.

**Finding**: Per spec FR-014/015/016 and reviewing the registry:

- `Campaign` — currently suggests `['Session', 'Quest', 'Event', 'Character', 'Location', 'Faction']`
- `Session` — currently suggests `['Event', 'Location']`
- `Faction` — currently suggests `['Character', 'Location', 'Event', 'Quest']`
- `City` — currently suggests `['Building', 'Location', 'Character', 'Faction', 'Event']`
- `People` (container) — currently suggests `['Character', 'Faction']`
- `Folder` (container) — currently suggests 12 types including `'Character'`

**Decision**: Add `'PlayerCharacter'` to all six entity types' `suggestedChildren` arrays.
**Rationale**: Matches spec requirements; ensures PCs are discoverable as children of Campaign, Session, Faction, City, People, and Folder.

---

### R6: Deployment Order — Backend vs Frontend

**Task**: Determine if there's a deployment order concern.

**Finding**: Per the Schema Version Matrix (`SCHEMA_VERSION_MATRIX.md`), backend should be deployed first. However, for this feature:

- The backend stores `EntityType` as a string in Cosmos DB
- The `IsInEnum()` validator means the backend must have `PlayerCharacter` in the enum before the frontend can create entities of this type
- The `EntitySchemaVersionConfig` defaults to `{ Min: 1, Max: 1 }` for unconfigured types, so the appsettings entry is optional but recommended

**Decision**: Deploy backend (enum + appsettings) before frontend (registry entry).
**Rationale**: Prevents 400 validation errors if frontend deploys first.
