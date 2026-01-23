# Implementation Plan: Consolidate Entity Type Metadata into Single Registry

**Branch**: `007-entity-type-registry` | **Date**: 2026-01-24 | **Spec**: [spec.md](./spec.md)  
**Related Issue**: [#90](https://github.com/PlagueHO/libris-maleficarum/issues/90)

## Summary

Refactor scattered entity type metadata (WorldEntityType, ENTITY_TYPE_META, ENTITY_TYPE_SUGGESTIONS, ENTITY_SCHEMA_VERSIONS) into a single `ENTITY_TYPE_REGISTRY` array that serves as the source of truth for all 35 entity types. Derive all existing constants from this registry to maintain backward compatibility while enabling future data-driven entity type management.

**Core Goal**: Single-point-of-maintenance for entity type metadata without breaking existing code.

## Technical Context

**Language/Version**: TypeScript 5.x (existing)  
**Primary Dependencies**: React 19, Redux Toolkit, Vitest, jest-axe (all existing)  
**Storage**: N/A (refactor of in-memory constants)  
**Testing**: Vitest + Testing Library + jest-axe (existing framework)  
**Target Platform**: Web browser (React SPA in libris-maleficarum-app/)  
**Project Type**: Web application (frontend-only refactor)  
**Performance Goals**: Negligible impact (<1ms overhead at module load from derivation)  
**Constraints**: 100% backward compatibility, zero breaking changes  
**Scale/Scope**: 35 entity types, 2 files to update, 1 file to create, 1 file to delete

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

✅ **PASS**: All constitutional principles satisfied

| Principle | Compliance | Notes |
|-----------|------------|-------|
| I. Cloud-Native Architecture | ✅ N/A | Frontend-only refactor, no infrastructure changes |
| II. Clean Architecture | ✅ Compliant | Maintains separation: config layer → types layer → components |
| III. Test-Driven Development | ✅ Compliant | Tests required for registry validation and helper functions |
| IV. Framework Standards | ✅ Compliant | Uses TypeScript 5.x, Redux Toolkit, Vitest (all mandated) |
| V. Developer Experience | ✅ Enhanced | Single source of truth improves developer experience |
| VI. Security by Default | ✅ N/A | No security implications (metadata only) |
| VII. Semantic Versioning | ✅ N/A | Internal refactor, no API changes |

**No violations to justify.** This refactor aligns with constitutional principles by improving code organization and maintainability.

## Project Structure

### Documentation (this feature)

```text
specs/007-entity-type-registry/
├── spec.md                       # Feature specification (complete)
├── plan.md                       # This file
├── research.md                   # Not needed (no unknowns, spec is complete)
├── data-model.md                 # Phase 1 output (EntityTypeConfig structure)
├── quickstart.md                 # Phase 1 output (developer guide for using registry)
├── contracts/                    # Not applicable (no API contracts)
│   └── EntityTypeConfig.schema.json  # TypeScript interface as JSON schema
└── checklists/
    └── requirements.md           # Complete (all checks passed)
```

### Source Code (libris-maleficarum-app/)

```text
libris-maleficarum-app/src/
├── services/
│   ├── config/                              # NEW DIRECTORY
│   │   └── entityTypeRegistry.ts            # NEW FILE (Phase 1) - Source of truth
│   ├── constants/
│   │   └── entitySchemaVersions.ts          # DELETE (Phase 4) - Replaced by registry
│   └── types/
│       └── worldEntity.types.ts             # MODIFIED (Phase 2) - Derive from registry
│
├── components/
│   └── MainPanel/
│       └── EntityDetailForm.tsx             # MODIFIED (Phase 4) - Update import
│
└── __tests__/
    ├── services/
    │   └── entityTypeRegistry.test.ts       # NEW FILE (Phase 3) - Validation tests
    └── config/
        └── entityTypeRegistry.test.ts       # NEW FILE (Phase 3) - Helper function tests
```

**Structure Decision**: Frontend-only changes within existing `libris-maleficarum-app/` React application. New `config/` directory under `src/services/` to house configuration-as-data (registry). Modified `types/` to derive constants from config. No backend or infrastructure changes required.

## Phase 0: Research & Design Decisions

**SKIP**: No research needed. Spec is complete with no NEEDS CLARIFICATION markers. All design decisions already made in spec:

- ✅ Registry structure (EntityTypeConfig interface) defined in spec
- ✅ Derivation approach (Object.fromEntries + map) defined in spec
- ✅ File locations determined based on existing structure
- ✅ Backward compatibility strategy documented in spec
- ✅ All 29 entity types enumerated in spec (FR-006)

**Output**: N/A (skipped - proceed directly to Phase 1)

## Phase 1: Data Model & Contracts

### 1.1 Data Model Definition

Create `data-model.md` documenting `EntityTypeConfig` interface:

**EntityTypeConfig Interface**:

```typescript
interface EntityTypeConfig {
  /** Unique identifier (matches WorldEntityType values) */
  type: string;
  
  /** Human-readable display name */
  label: string;
  
  /** Descriptive text for UI hints */
  description: string;
  
  /** Category for grouping in selectors */
  category: 'Geography' | 'Characters & Factions' | 'Events & Quests' | 'Items' | 'Campaigns' | 'Containers' | 'Other';
  
  /** Lucide icon component name */
  icon: string;
  
  /** Current schema version for properties field */
  schemaVersion: number;
  
  /** Suggested child entity types (for context-aware UI) */
  suggestedChildren: string[];
  
  /** Can this type exist without a parent? (default: false) */
  canBeRoot?: boolean;
}
```

**Derived Types**:

- `ENTITY_TYPE_REGISTRY: readonly EntityTypeConfig[]` - Array of all configurations
- `WorldEntityType` - Const object derived from registry (maintains existing pattern)
- `WorldEntityType` type - String literal union type
- `ENTITY_SCHEMA_VERSIONS: Record<WorldEntityType, number>` - Derived map
- `ENTITY_TYPE_META: Record<WorldEntityType, EntityTypeMeta>` - Derived map
- `ENTITY_TYPE_SUGGESTIONS: Record<WorldEntityType, WorldEntityType[]>` - Derived map

**Validation Rules**:

- `type` must be unique across all entries
- `schemaVersion` must be >= 1
- `icon` must be valid Lucide icon name (PascalCase)
- `suggestedChildren` array entries must reference valid types (no circular refs)
- All 29 entity types from FR-006 must be present

### 1.2 Contracts/Schemas

Create `contracts/EntityTypeConfig.schema.json` (JSON Schema representation for documentation):

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "EntityTypeConfig",
  "description": "Configuration for a world entity type",
  "type": "object",
  "required": ["type", "label", "description", "category", "icon", "schemaVersion", "suggestedChildren"],
  "properties": {
    "type": { "type": "string", "pattern": "^[A-Z][a-zA-Z]+$" },
    "label": { "type": "string", "minLength": 1 },
    "description": { "type": "string", "minLength": 1 },
    "category": { 
      "type": "string",
      "enum": ["Geography", "Characters & Factions", "Events & Quests", "Items", "Campaigns", "Containers", "Other"]
    },
    "icon": { "type": "string", "pattern": "^[A-Z][a-zA-Z]+$" },
    "schemaVersion": { "type": "integer", "minimum": 1 },
    "suggestedChildren": { 
      "type": "array",
      "items": { "type": "string" }
    },
    "canBeRoot": { "type": "boolean" }
  }
}
```

### 1.3 Quickstart Guide

Create `quickstart.md` with developer instructions:

**Adding a New Entity Type**:

1. Open `src/services/config/entityTypeRegistry.ts`
2. Add entry to `ENTITY_TYPE_REGISTRY` array with all required properties
3. All derived constants update automatically
4. Run tests: `pnpm test entityTypeRegistry`
5. TypeScript will catch any missing/invalid references

**Using Entity Type Metadata**:

- **Get config**: `getEntityTypeConfig(WorldEntityType.Continent)`
- **List root types**: `getRootEntityTypes()`
- **Iterate all**: `getAllEntityTypes()`
- **Schema version**: `ENTITY_SCHEMA_VERSIONS[type]` (backward compatible)
- **Type metadata**: `ENTITY_TYPE_META[type]` (backward compatible)
- **Suggestions**: `ENTITY_TYPE_SUGGESTIONS[type]` (backward compatible)

### 1.4 Update Agent Context

Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot` to update Copilot context with:

- New `EntityTypeConfig` interface in type definitions
- New `entityTypeRegistry.ts` file location
- Deprecation notice for `entitySchemaVersions.ts`
- Registry-based entity type management pattern

**Agent Context Changes**:

```markdown
## Entity Type Management

Entity type metadata is defined in a single registry:
- **Source**: `src/services/config/entityTypeRegistry.ts`
- **Interface**: `EntityTypeConfig` with type, label, description, category, icon, schemaVersion, suggestedChildren, canBeRoot
- **Derived Constants**: WorldEntityType, ENTITY_SCHEMA_VERSIONS, ENTITY_TYPE_META, ENTITY_TYPE_SUGGESTIONS
- **Helper Functions**: getEntityTypeConfig(), getRootEntityTypes(), getAllEntityTypes()
- **Adding Types**: Add one entry to ENTITY_TYPE_REGISTRY array, all constants update automatically
```

### Phase 1 Deliverables

- [ ] `data-model.md` - EntityTypeConfig interface documentation
- [ ] `contracts/EntityTypeConfig.schema.json` - JSON schema for validation
- [ ] `quickstart.md` - Developer guide for using registry
- [ ] Updated Copilot agent context with registry patterns

## Phase 2: Implementation Breakdown

### Task 2.1: Create Entity Type Registry

**File**: `src/services/config/entityTypeRegistry.ts` (NEW)

**Implementation**:

1. Define `EntityTypeConfig` interface with JSDoc comments
2. Create `ENTITY_TYPE_REGISTRY` array with all 35 entity types:
   - **Geographic (7)**: Continent, Country, Region, City, Building, Room, Location
   - **Characters & Factions (2)**: Character, Faction
   - **Events & Quests (2)**: Event, Quest
   - **Items (1)**: Item
   - **Campaigns (2)**: Campaign, Session
   - **Containers (10)**: Folder, Locations, People, Events, History, Lore, Bestiary, Items, Adventures, Geographies
   - **Regional (4)**: GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion
   - **Other (1)**: Other
   - **Note**: Verify container types during implementation (Events/Event, Items/Item naming collision)
3. Mark array as `as const` for type inference
4. Export as `readonly EntityTypeConfig[]`

**Acceptance Criteria**:

- [ ] EntityTypeConfig interface matches spec (8 properties)
- [ ] All 35 entity types from FR-006 present in registry
- [ ] Each entry has complete metadata (no optional fields except canBeRoot)
- [ ] TypeScript compilation succeeds with strict mode
- [ ] File has module-level JSDoc comment

**Estimated Effort**: 2 hours (data entry + validation)

### Task 2.2: Derive Constants from Registry

**File**: `src/services/types/worldEntity.types.ts` (MODIFY)

**Implementation**:

1. Import `ENTITY_TYPE_REGISTRY` from `../config/entityTypeRegistry`
2. Replace existing `WorldEntityType` const with derived version:
   ```typescript
   export const WorldEntityType = Object.fromEntries(
     ENTITY_TYPE_REGISTRY.map(c => [c.type, c.type])
   ) as const;
   ```
3. Keep existing type derivation: `export type WorldEntityType = typeof WorldEntityType[keyof typeof WorldEntityType];`
4. Derive `ENTITY_SCHEMA_VERSIONS`:
   ```typescript
   export const ENTITY_SCHEMA_VERSIONS: Record<WorldEntityType, number> = Object.fromEntries(
     ENTITY_TYPE_REGISTRY.map(c => [c.type, c.schemaVersion])
   );
   ```
5. Derive `ENTITY_TYPE_META`:
   ```typescript
   export const ENTITY_TYPE_META: Record<WorldEntityType, EntityTypeMeta> = Object.fromEntries(
     ENTITY_TYPE_REGISTRY.map(c => [c.type, { label: c.label, description: c.description, category: c.category, icon: c.icon }])
   );
   ```
6. Derive `ENTITY_TYPE_SUGGESTIONS`:
   ```typescript
   export const ENTITY_TYPE_SUGGESTIONS: Record<WorldEntityType, WorldEntityType[]> = Object.fromEntries(
     ENTITY_TYPE_REGISTRY.map(c => [c.type, c.suggestedChildren as WorldEntityType[]])
   );
   ```
7. Keep existing helper functions `getEntityTypeSuggestions()` and `getEntityTypeMeta()` (no changes)

**Acceptance Criteria**:

- [ ] All derived constants have correct types (Record<WorldEntityType, ...>)
- [ ] Existing exports remain (WorldEntityType, ENTITY_TYPE_META, etc.)
- [ ] TypeScript compilation succeeds
- [ ] No changes to existing helper functions
- [ ] File size increases minimally (derivation logic is compact)

**Estimated Effort**: 1 hour

### Task 2.3: Add Helper Functions

**File**: `src/services/config/entityTypeRegistry.ts` (MODIFY)

**Implementation**:

1. Add `getEntityTypeConfig()` function:
   ```typescript
   export function getEntityTypeConfig(type: WorldEntityType): EntityTypeConfig | undefined {
     return ENTITY_TYPE_REGISTRY.find(c => c.type === type);
   }
   ```
2. Add `getRootEntityTypes()` function:
   ```typescript
   export function getRootEntityTypes(): WorldEntityType[] {
     return ENTITY_TYPE_REGISTRY
       .filter(c => c.canBeRoot === true)
       .map(c => c.type as WorldEntityType);
   }
   ```
3. Add `getAllEntityTypes()` function:
   ```typescript
   export function getAllEntityTypes(): readonly EntityTypeConfig[] {
     return ENTITY_TYPE_REGISTRY;
   }
   ```
4. Export all functions with JSDoc comments

**Acceptance Criteria**:

- [ ] All three helper functions exported
- [ ] Return types match spec (EntityTypeConfig | undefined, WorldEntityType[], readonly EntityTypeConfig[])
- [ ] JSDoc comments present with @param and @returns tags
- [ ] TypeScript compilation succeeds

**Estimated Effort**: 30 minutes

### Task 2.4: Create Validation Tests

**File**: `src/__tests__/services/entityTypeRegistry.test.ts` (NEW)

**Implementation**:

1. Test registry uniqueness:
   ```typescript
   it('should have unique type identifiers', () => {
     const types = ENTITY_TYPE_REGISTRY.map(c => c.type);
     expect(new Set(types).size).toBe(types.length);
   });
   ```
2. Test schema versions >= 1:
   ```typescript
   it('should have valid schema versions', () => {
     ENTITY_TYPE_REGISTRY.forEach(config => {
       expect(config.schemaVersion).toBeGreaterThanOrEqual(1);
     });
   });
   ```
3. Test icon name format (PascalCase):
   ```typescript
   it('should have valid icon names', () => {
     ENTITY_TYPE_REGISTRY.forEach(config => {
       expect(config.icon).toMatch(/^[A-Z][a-zA-Z]+$/);
     });
   });
   ```
4. Test completeness (all 35 types present):
   ```typescript
   it('should contain all 35 entity types', () => {
     expect(ENTITY_TYPE_REGISTRY).toHaveLength(35);
     const expectedTypes = ['Continent', 'Country', /* ... all 35 */];
     const actualTypes = ENTITY_TYPE_REGISTRY.map(c => c.type);
     expect(actualTypes.sort()).toEqual(expectedTypes.sort());
   });
   ```
5. Test no circular suggestions:
   ```typescript
   it('should not have circular suggestions', () => {
     ENTITY_TYPE_REGISTRY.forEach(config => {
       expect(config.suggestedChildren).not.toContain(config.type);
     });
   });
   ```

**Acceptance Criteria**:

- [ ] All 5 validation tests implemented
- [ ] Tests pass at 100% success rate
- [ ] Coverage includes all registry entries
- [ ] Test file follows Vitest + AAA pattern

**Estimated Effort**: 1 hour

### Task 2.5: Create Helper Function Tests

**File**: `src/__tests__/config/entityTypeRegistry.test.ts` (NEW)

**Implementation**:

1. Test `getEntityTypeConfig()`:
   ```typescript
   it('should return config for valid type', () => {
     const config = getEntityTypeConfig(WorldEntityType.Continent);
     expect(config).toBeDefined();
     expect(config?.type).toBe('Continent');
     expect(config?.label).toBe('Continent');
   });
   
   it('should return undefined for invalid type', () => {
     const config = getEntityTypeConfig('InvalidType' as WorldEntityType);
     expect(config).toBeUndefined();
   });
   ```
2. Test `getRootEntityTypes()`:
   ```typescript
   it('should return only types with canBeRoot true', () => {
     const rootTypes = getRootEntityTypes();
     rootTypes.forEach(type => {
       const config = getEntityTypeConfig(type);
       expect(config?.canBeRoot).toBe(true);
     });
   });
   ```
3. Test `getAllEntityTypes()`:
   ```typescript
   it('should return complete registry', () => {
     const all = getAllEntityTypes();
     expect(all).toHaveLength(35);
     expect(all).toBe(ENTITY_TYPE_REGISTRY); // Same reference
   });
   ```

**Acceptance Criteria**:

- [ ] All 3 helper functions tested
- [ ] Edge cases covered (undefined returns, empty arrays)
- [ ] 100% test pass rate
- [ ] Test file follows Vitest + AAA pattern

**Estimated Effort**: 45 minutes

### Task 2.6: Update Import References

**Files**: 
- `src/services/worldEntityApi.ts` (MODIFY)
- `src/components/MainPanel/EntityDetailForm.tsx` (MODIFY)

**Implementation**:

1. Find all imports: `grep -r "from.*entitySchemaVersions" src/`
2. Replace imports:
   - FROM: `import { getSchemaVersion } from './constants/entitySchemaVersions';`
   - TO: `import { ENTITY_SCHEMA_VERSIONS } from './types/worldEntity.types';` or `import { getEntityTypeConfig } from './config/entityTypeRegistry';`
3. Update usage:
   - FROM: `getSchemaVersion(entityType)`
   - TO: `ENTITY_SCHEMA_VERSIONS[entityType]` or `getEntityTypeConfig(entityType)?.schemaVersion ?? 1`
4. Verify TypeScript compilation succeeds
5. Run affected tests to confirm backward compatibility

**Files to Update** (from grep results):
- `src/services/worldEntityApi.ts` (line 11)
- `src/components/MainPanel/EntityDetailForm.tsx` (line 11)

**Acceptance Criteria**:

- [ ] Zero references to `entitySchemaVersions` remain
- [ ] TypeScript compilation succeeds
- [ ] All existing tests pass (backward compatibility)
- [ ] No runtime errors in affected components

**Estimated Effort**: 30 minutes

### Task 2.7: Delete entitySchemaVersions.ts

**File**: `src/services/constants/entitySchemaVersions.ts` (DELETE)

**Implementation**:

1. Verify zero references remain (Task 2.6 complete)
2. Delete file: `rm src/services/constants/entitySchemaVersions.ts`
3. Run full test suite to confirm no breakage
4. Verify TypeScript compilation succeeds
5. Add test to prevent accidental recreation:
   ```typescript
   it('should not have entitySchemaVersions.ts file', () => {
     expect(() => require('../services/constants/entitySchemaVersions')).toThrow();
   });
   ```

**Acceptance Criteria**:

- [ ] File deleted from repository
- [ ] No TypeScript errors
- [ ] All tests pass (100% suite)
- [ ] Negative test added to catch file recreation

**Estimated Effort**: 15 minutes

### Task 2.8: Run Full Test Suite

**Command**: `pnpm test`

**Validation**:

1. All existing tests pass (100% backward compatibility - SC-004)
2. New validation tests pass (100% - SC-006)
3. Helper function tests pass (100% - SC-009)
4. TypeScript compilation succeeds (zero errors - SC-003)
5. ESLint passes (zero warnings - SC-005)
6. Coverage maintains or improves current levels

**Acceptance Criteria**:

- [ ] Test suite: 100% pass rate
- [ ] TypeScript: Zero compilation errors
- [ ] ESLint: Zero warnings
- [ ] Coverage: >= baseline (check with `pnpm test -- --coverage`)

**Estimated Effort**: 15 minutes

## Phase 3: Verification Checklist

### Functional Requirements Verification

- [ ] **FR-001**: All metadata consolidated in `ENTITY_TYPE_REGISTRY` array
- [ ] **FR-002**: `WorldEntityType` const derived from registry
- [ ] **FR-003**: `ENTITY_SCHEMA_VERSIONS` as `Record<WorldEntityType, number>` derived
- [ ] **FR-004**: `ENTITY_TYPE_META` as `Record<WorldEntityType, EntityTypeMeta>` derived
- [ ] **FR-005**: `ENTITY_TYPE_SUGGESTIONS` as `Record<WorldEntityType, WorldEntityType[]>` derived
- [ ] **FR-006**: All 35 entity types present in registry
- [ ] **FR-007**: Backward compatibility maintained (all existing imports work)
- [ ] **FR-008**: `getEntityTypeConfig(type)` implemented and tested
- [ ] **FR-009**: `getRootEntityTypes()` implemented and tested
- [ ] **FR-010**: `getAllEntityTypes()` implemented and tested
- [ ] **FR-011**: `entitySchemaVersions.ts` deleted
- [ ] **FR-012**: All imports updated to new locations
- [ ] **FR-013**: `canBeRoot` property present in EntityTypeConfig
- [ ] **FR-014**: Strong TypeScript typing maintained

### Success Criteria Verification

- [ ] **SC-001**: Can add entity type with one registry entry
- [ ] **SC-002**: 100% coverage of 35 entity types
- [ ] **SC-003**: TypeScript compilation zero errors
- [ ] **SC-004**: 100% existing test pass rate
- [ ] **SC-005**: ESLint zero warnings
- [ ] **SC-006**: Registry validation tests 100% pass
- [ ] **SC-007**: Zero references to `entitySchemaVersions.ts`
- [ ] **SC-008**: Single file `entityTypeRegistry.ts` contains definitions
- [ ] **SC-009**: Helper functions return correct data (100% test coverage)
- [ ] **SC-010**: Registry is JSON-serializable (can be exported/imported)

### Constitution Re-Check

*Run after Phase 2 implementation complete.*

All constitutional principles remain satisfied (same as initial check). No new violations introduced. Refactor improves code organization without adding complexity.

## Rollout Strategy

**Deployment Type**: Low-risk refactor (internal code organization)

**Rollout Plan**:

1. **Merge to feature branch** (`007-entity-type-registry`)
2. **Run CI pipeline**: Verify tests pass in clean environment
3. **Create Pull Request** to `main`
4. **Code Review**: Verify all checklist items complete
5. **Merge to main**: After approval
6. **Monitor**: No user-facing changes, but verify app loads correctly

**Rollback Plan**: 

If issues discovered post-merge, revert commit. Registry pattern is self-contained; reverting restores original scattered constants without data loss.

**Risk Assessment**: **LOW**

- No API changes
- No database schema changes
- No user-facing UI changes
- Backward compatible (all existing imports work)
- Comprehensive test coverage prevents regressions

## Metrics & Monitoring

**Build Metrics**:
- TypeScript compilation time (should be <1 second increase)
- Test suite execution time (should be <5 second increase)
- Bundle size (should be negligible change, derivation is compact)

**Post-Deployment Validation**:
- App loads successfully in browser
- Entity creation/editing works (uses ENTITY_SCHEMA_VERSIONS)
- Entity type selector shows all 29 types (uses ENTITY_TYPE_META)
- No console errors related to entity types

**Success Indicators**:
- All FR and SC checkboxes complete
- CI pipeline green
- Code review approval
- Zero production incidents post-merge

## Estimated Timeline

| Phase | Tasks | Effort | Completed |
|-------|-------|--------|-----------|
| Phase 0 | Research | SKIPPED | ✅ |
| Phase 1 | Data Model & Contracts | 3 hours | ⬜ |
| Phase 2.1 | Create Registry | 2 hours | ⬜ |
| Phase 2.2 | Derive Constants | 1 hour | ⬜ |
| Phase 2.3 | Add Helpers | 0.5 hours | ⬜ |
| Phase 2.4 | Validation Tests | 1 hour | ⬜ |
| Phase 2.5 | Helper Tests | 0.75 hours | ⬜ |
| Phase 2.6 | Update Imports | 0.5 hours | ⬜ |
| Phase 2.7 | Delete Old File | 0.25 hours | ⬜ |
| Phase 2.8 | Full Test Suite | 0.25 hours | ⬜ |
| Phase 3 | Verification | 0.5 hours | ⬜ |
| **TOTAL** | | **~10 hours** | |

**Target Completion**: 1-2 days for single developer

## Next Steps

1. ✅ Spec created and validated
2. ✅ Plan created (this document)
3. ⬜ Execute Phase 1 (data-model.md, contracts/, quickstart.md)
4. ⬜ Execute Phase 2 (implementation tasks 2.1-2.8)
5. ⬜ Execute Phase 3 (verification checklist)
6. ⬜ Create PR and request review
7. ⬜ Merge to main after approval

**Command to Start Implementation**:
```bash
# Checkout feature branch (already on 007-entity-type-registry)
git status

# Create Phase 1 deliverables
# (See Phase 1 section for details)

# Then proceed with Phase 2 implementation tasks
```
