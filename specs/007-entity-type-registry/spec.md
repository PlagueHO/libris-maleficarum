# Feature Specification: Consolidate Entity Type Metadata into Single Registry

**Feature Branch**: `007-entity-type-registry`  
**Created**: 2026-01-24  
**Status**: Draft  
**Input**: Refactor: Consolidate entity type metadata into single registry  
**Related Issue**: [#90](https://github.com/PlagueHO/libris-maleficarum/issues/90)  
**Related PR**: [#89](https://github.com/PlagueHO/libris-maleficarum/pull/89) (Schema Versioning)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Single Source Definition (Priority: P1)

As a **developer**, when I need to add a new entity type, I want to define all metadata in one place so that I don't miss required properties or create inconsistencies.

**Why this priority**: This is the core value proposition - eliminating scattered definitions and preventing maintenance errors. Without this, the refactor has no benefit.

**Independent Test**: Can be fully tested by adding a new entity type to the registry and verifying all derived constants (WorldEntityType, ENTITY_TYPE_META, ENTITY_TYPE_SUGGESTIONS, ENTITY_SCHEMA_VERSIONS) automatically include it without manual updates elsewhere.

**Acceptance Scenarios**:

1. **Given** I need to add a new entity type "Kingdom", **When** I add one entry to `ENTITY_TYPE_REGISTRY` with all metadata (label, description, category, icon, schemaVersion, suggestedChildren), **Then** all exported constants automatically include "Kingdom" without additional changes
1. **Given** the registry contains 29 entity types, **When** I run the test suite, **Then** all tests pass confirming completeness and validity of all derived constants
1. **Given** I update the schema version for "Continent" from 1 to 2 in the registry, **When** I access `ENTITY_SCHEMA_VERSIONS[WorldEntityType.Continent]`, **Then** it returns 2 without changing any other files

---

### User Story 2 - Type-Safe Access (Priority: P1)

As a **developer**, when I access entity type metadata, I want TypeScript to provide autocomplete and type checking so that I catch errors at compile time rather than runtime.

**Why this priority**: Type safety is critical for preventing bugs. This maintains existing type safety while improving it through consolidation.

**Independent Test**: Can be fully tested by verifying TypeScript compilation succeeds, all derived constants have proper types (Record<WorldEntityType, ...>), and IDE provides autocomplete for all registry-derived values.

**Acceptance Scenarios**:

1. **Given** I'm writing code that uses `WorldEntityType`, **When** I type `WorldEntityType.`, **Then** IDE autocompletes with all 29 entity types
1. **Given** I try to access `ENTITY_SCHEMA_VERSIONS["InvalidType"]`, **When** TypeScript compiles, **Then** it produces a type error
1. **Given** the registry defines all entity types, **When** I try to access `ENTITY_TYPE_META[WorldEntityType.Continent]`, **Then** TypeScript knows the return type includes label, description, category, and icon properties

---

### User Story 3 - Backward Compatibility (Priority: P1)

As a **developer maintaining existing code**, when this refactor is deployed, I want all existing code to continue working without changes so that I don't need to update hundreds of import statements and usages.

**Why this priority**: Breaking changes would require extensive updates across the codebase and could introduce bugs. Backward compatibility is essential for a refactor.

**Independent Test**: Can be fully tested by running the full test suite and verifying all existing components, services, and tests pass without modification.

**Acceptance Scenarios**:

1. **Given** existing code uses `import { WorldEntityType } from '@/services/types/worldEntity.types'`, **When** the refactor is complete, **Then** the import continues to work and provides the same type
1. **Given** existing code accesses `ENTITY_TYPE_META[WorldEntityType.Character]`, **When** the refactor is complete, **Then** it returns the same structure with label, description, category, and icon
1. **Given** existing code calls `getEntityTypeSuggestions(WorldEntityType.City)`, **When** the refactor is complete, **Then** it returns the same array of suggested child types

---

### User Story 4 - Comprehensive Validation (Priority: P2)

As a **developer**, when I make changes to the entity type registry, I want automated tests to validate completeness and correctness so that I catch configuration errors early.

**Why this priority**: Validation prevents configuration errors and ensures data quality, but the registry can function without these tests initially.

**Independent Test**: Can be fully tested by adding registry validation tests that check for: unique types, positive schema versions, valid icon names, no circular suggestions (except allowed hierarchical types), and complete coverage of all entity categories.

**Acceptance Scenarios**:

1. **Given** I accidentally define two entity types with the same `type` value, **When** tests run, **Then** a uniqueness validation test fails
1. **Given** I define an entity type with `schemaVersion: 0`, **When** tests run, **Then** a validation test fails requiring schema version >= 1
1. **Given** a non-hierarchical entity type suggests itself as a child (circular reference), **When** tests run, **Then** a validation test flags the circular dependency
1. **Given** a hierarchical container type (Folder, GeographicRegion, etc.) suggests itself as a child, **When** tests run, **Then** the validation test passes (self-referencing is allowed for nested hierarchies)
1. **Given** all 29 entity types are in the registry, **When** tests run, **Then** coverage tests confirm all expected types are present

---

### User Story 5 - Helper Functions (Priority: P2)

As a **developer**, when I need to work with entity types programmatically, I want helper functions to query the registry so that I can easily get filtered subsets or specific configurations.

**Why this priority**: Helper functions improve developer experience but aren't critical for the core refactor. The registry data is still accessible without them.

**Independent Test**: Can be fully tested by calling `getEntityTypeConfig()`, `getRootEntityTypes()`, and `getAllEntityTypes()` and verifying they return correct data from the registry.

**Acceptance Scenarios**:

1. **Given** I need entity type details, **When** I call `getEntityTypeConfig(WorldEntityType.Continent)`, **Then** it returns the full configuration object with all metadata
1. **Given** I need to display root-level entity type options, **When** I call `getRootEntityTypes()`, **Then** it returns only entity types with `canBeRoot: true`
1. **Given** I need to iterate all entity types, **When** I call `getAllEntityTypes()`, **Then** it returns the complete readonly registry array

---

### User Story 6 - Future API Integration (Priority: P3)

As a **platform administrator**, when entity type management is added in the future, I want the registry structure to support loading from an API so that entity types can be customized without code changes.

**Why this priority**: This is a future enhancement. The current implementation should be structured to enable this, but actual API integration is out of scope.

**Independent Test**: Can be verified by demonstrating the registry structure is a simple array of objects that can be JSON-serialized and matches what an API would return.

**Acceptance Scenarios**:

1. **Given** the registry is defined as an array of `EntityTypeConfig` objects, **When** I serialize it to JSON, **Then** it produces valid JSON matching a potential API response schema
1. **Given** I want to replace the static registry, **When** I fetch data from an API endpoint, **Then** the returned structure can be assigned directly to `ENTITY_TYPE_REGISTRY` type
1. **Given** future admin UI requirements, **When** reviewing the registry structure, **Then** all properties are simple types (string, number, boolean, array) suitable for form editing

---

### Edge Cases

- What happens when a developer forgets to add a new entity type to the registry? **TypeScript will catch missing keys when trying to access derived constants, and validation tests will flag incomplete coverage.**
- What happens if an entity type's `suggestedChildren` references a non-existent type? **Validation tests should flag invalid references; TypeScript can't catch this at compile time since suggestions use string arrays.**
- What happens when the registry is empty? **All derived constants become empty objects; helper functions return empty arrays. This is valid for initial state.**
- What happens if two entity types have the same `type` value? **The last one wins in derived constants; uniqueness validation test should catch this.**
- What happens when accessing metadata for an entity type not in the registry? **`getEntityTypeConfig()` returns undefined; accessing record constants with invalid keys requires type assertion and may return undefined.**

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST consolidate all entity type metadata (type identifier, label, description, category, icon, schema version, suggested children) into a single `ENTITY_TYPE_REGISTRY` array
- **FR-002**: System MUST derive `WorldEntityType` const object from the registry to maintain existing const-object-with-type pattern
- **FR-003**: System MUST derive `ENTITY_SCHEMA_VERSIONS` as `Record<WorldEntityType, number>` from the registry
- **FR-004**: System MUST derive `ENTITY_TYPE_META` as `Record<WorldEntityType, EntityTypeMeta>` from the registry
- **FR-005**: System MUST derive `ENTITY_TYPE_SUGGESTIONS` as `Record<WorldEntityType, WorldEntityType[]>` from the registry
- **FR-006**: System MUST include all 29 existing entity types in the registry:
  - **Geographic (7)**: Continent, Country, Region, City, Building, Room, Location
  - **Characters & Factions (2)**: Character, Faction
  - **Events & Quests (2)**: Event, Quest
  - **Items (1)**: Item
  - **Campaigns (2)**: Campaign, Session
  - **Containers (10)**: Folder, Locations, People, Events, History, Lore, Bestiary, Items, Adventures, Geographies
  - **Regional (4)**: GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion
  - **Other (1)**: Other
  - **Deprecated (6)**: *(Note: These are marked as containers in existing code but named identically to singular types - Events/Event, Items/Item. Verification needed during implementation.)*
- **FR-007**: System MUST maintain backward compatibility with all existing imports and usages of entity type constants
- **FR-008**: System MUST provide `getEntityTypeConfig(type)` helper function to retrieve full configuration for a specific entity type
- **FR-009**: System MUST provide `getRootEntityTypes()` helper function to retrieve entity types that can be created at root level
- **FR-010**: System MUST provide `getAllEntityTypes()` helper function to retrieve the complete registry
- **FR-011**: System MUST delete the `entitySchemaVersions.ts` file after migration is complete
- **FR-012**: System MUST update all imports that reference `entitySchemaVersions.ts` to use the new registry location
- **FR-013**: Registry entries MUST include `canBeRoot` property to identify entity types that can exist without a parent
- **FR-014**: All derived constants MUST maintain strong TypeScript typing with proper type inference

### Key Entities

- **EntityTypeConfig**: Interface defining the structure of each registry entry with properties:
  - `type` (string): Unique identifier matching WorldEntityType values
  - `label` (string): Human-readable display name
  - `description` (string): Descriptive text explaining the entity type
  - `category` (string): One of: Geography, Characters & Factions, Events & Quests, Items, Campaigns, Containers, Other
  - `icon` (string): Lucide icon name for UI display
  - `schemaVersion` (number): Current schema version for properties (>= 1)
  - `suggestedChildren` (string[]): Array of entity type identifiers for parent-child hints
  - `canBeRoot` (boolean, optional): Whether this type can be created without a parent

- **ENTITY_TYPE_REGISTRY**: Readonly array containing all EntityTypeConfig entries, serving as the single source of truth

- **WorldEntityType**: Derived const object providing string literal union type (existing pattern maintained)

- **EntityTypeMeta**: Interface for metadata subset (label, description, category, icon) used in UI components

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developer can add a new entity type by adding exactly one entry to the registry, with all derived constants updating automatically
- **SC-002**: All 29 existing entity types are defined in the registry with complete metadata (100% coverage)
- **SC-003**: TypeScript compilation succeeds with zero errors and all derived constants have correct types
- **SC-004**: All existing unit tests pass without modification (100% backward compatibility)
- **SC-005**: ESLint passes with zero warnings across all modified files
- **SC-006**: Registry validation tests achieve 100% pass rate, covering: uniqueness, schema version validity, icon name format, and completeness
- **SC-007**: All files importing from `entitySchemaVersions.ts` are updated to use new registry location (zero remaining references)
- **SC-008**: Code review confirms single file (`entityTypeRegistry.ts`) contains all entity type definitions
- **SC-009**: Helper functions (`getEntityTypeConfig`, `getRootEntityTypes`, `getAllEntityTypes`) return correct data for 100% of test cases
- **SC-010**: Registry structure is JSON-serializable and can be loaded from external source (future-proof for API integration)

## Dependencies & Assumptions *(optional)*

### Dependencies

- TypeScript 5.x for type inference and const assertions
- Existing `worldEntity.types.ts` file structure
- Existing test framework (Vitest) for validation tests
- No external libraries required

### Assumptions

- All 29 entity types use schema version 1 currently (no version migrations pending)
- Existing code follows the const-object-with-type pattern for WorldEntityType
- No new entity types are being added during this refactor (can be added after)
- Test suite has adequate coverage to validate backward compatibility
- File structure under `src/services/` can be reorganized (adding `config/` directory)

## Out of Scope *(optional)*

- **API-driven entity type loading**: Future enhancement, not implemented in this refactor
- **Admin UI for entity type management**: Future enhancement
- **Schema version migration logic**: Handled by backend, not part of this refactor
- **Entity type deprecation workflow**: Future enhancement (registry supports `isDeprecated` property but not implemented)
- **Custom validation rules per entity type**: Future enhancement
- **Localization of entity type labels**: Future enhancement
- **Performance optimization**: Registry is small (29 entries), no optimization needed

## Technical Considerations *(optional)*

### File Organization

**New File**:

- `src/services/config/entityTypeRegistry.ts` - Contains `EntityTypeConfig` interface and `ENTITY_TYPE_REGISTRY` array

**Modified Files**:

- `src/services/types/worldEntity.types.ts` - Imports registry, derives constants, re-exports for backward compatibility

**Deleted Files**:

- `src/services/constants/entitySchemaVersions.ts` - Replaced by registry

### Implementation Strategy

1. **Phase 1**: Create registry structure
   - Define `EntityTypeConfig` interface
   - Create `ENTITY_TYPE_REGISTRY` with all 29 types
   - Export as readonly array with const assertion

1. **Phase 2**: Derive constants
   - Implement derivation logic using `Object.fromEntries()` and `map()`
   - Add type assertions to ensure correct typing
   - Validate output types match existing patterns

1. **Phase 3**: Add helper functions
   - Implement `getEntityTypeConfig()` with undefined handling
   - Implement `getRootEntityTypes()` with filtering
   - Implement `getAllEntityTypes()` returning readonly registry

1. **Phase 4**: Update imports
   - Find all imports from `entitySchemaVersions.ts`
   - Update to import from `worldEntity.types.ts`
   - Remove `entitySchemaVersions.ts` file

1. **Phase 5**: Add validation tests
   - Test registry uniqueness
   - Test schema version >= 1
   - Test icon name format
   - Test completeness (all expected types present)
   - Test helper function correctness

### Type Safety Approach

Use TypeScript's type inference to maintain safety:

```typescript
// Registry with const assertion
export const ENTITY_TYPE_REGISTRY = [...] as const;

// Derived with proper typing
export const WorldEntityType = Object.fromEntries(
  ENTITY_TYPE_REGISTRY.map(c => [c.type, c.type])
) as const;

// Type derived from const
export type WorldEntityType = typeof WorldEntityType[keyof typeof WorldEntityType];

// Record types ensure exhaustiveness
export const ENTITY_SCHEMA_VERSIONS: Record<WorldEntityType, number> = ...;
```

### Migration Checklist

Files importing `entitySchemaVersions.ts` (to be updated):

- Search for: `from '@/services/constants/entitySchemaVersions'`
- Replace with: `from '@/services/types/worldEntity.types'` or `from '@/services/config/entityTypeRegistry'`
- Verify: No remaining references to deleted file

## Risks & Mitigations *(optional)*

### Risk 1: Breaking Changes During Migration

**Impact**: Medium - Could break existing functionality if derivation logic is incorrect

**Mitigation**:

- Maintain all existing exports with exact same types
- Run full test suite before and after migration
- Compare derived constant outputs with original definitions
- Use TypeScript strict mode to catch type mismatches

### Risk 2: Performance Impact from Derivation

**Impact**: Low - Deriving constants at module load could add initialization time

**Mitigation**:

- Registry is small (29 entries), derivation is O(n)
- Derivation happens once at module load, not per usage
- Modern JavaScript engines optimize array operations
- Can benchmark if needed, but unlikely to be measurable

### Risk 3: Incomplete Migration of Imports

**Impact**: Medium - Missing import updates could cause runtime errors

**Mitigation**:

- Use IDE's "Find All References" to locate all usages
- Run `grep` search across codebase
- Rely on TypeScript compilation errors to catch missing imports
- Add test to verify `entitySchemaVersions.ts` is deleted

### Risk 4: Validation Tests Not Comprehensive

**Impact**: Low - Invalid registry entries could slip through

**Mitigation**:

- Test uniqueness of type identifiers
- Test all required properties are present
- Test schema versions are positive integers
- Test suggested children reference valid types
- Test icon names match expected format

## Related Work

- **#89**: Schema Versioning PR - This refactor complements schema versioning by consolidating version metadata
- **Future**: API-driven entity types - Registry structure enables this future enhancement
- **Future**: Admin UI for entity type management - Registry provides the data structure
