# Phase 0: Research & Unknowns Resolution

**Feature**: Container Entity Type Support  
**Date**: 2026-01-20

## Research Tasks

### 1. Fluent UI v9 Tag Input Component Patterns

**Question**: What is the best practice for implementing a tag/chip input component using Fluent UI v9?

**Research Findings**:

- Fluent UI v9 provides `TagPicker` component (from `@fluentui/react-components/unstable`) for selecting from predefined tags
- For free-form text input with chips, the recommended pattern is to use:
  - `Field` wrapper for label and validation
  - `Input` for text entry
  - `Tag` components (from `@fluentui/react-components`) to display entered values with dismiss buttons
  - Custom state management for tag array
- Alternative: Could use `Combobox` with `freeform` mode but TagPicker is more specialized

**Decision**: Create custom `TagInput` component using Fluent UI v9 `Tag`, `Input`, and `Field` primitives. This provides:

- Accessibility (ARIA labels, keyboard navigation)
- Consistent Fluent design system
- Full control over validation and formatting
- Reusable across all entity forms

**Rationale**: TagPicker is marked unstable; custom implementation gives stability and control while using stable Fluent primitives.

**Alternatives Considered**:

- Third-party React tag input libraries (rejected: adds dependency, inconsistent with Fluent design)
- Plain HTML inputs with custom styling (rejected: accessibility concerns, reinventing Fluent patterns)

---

### 2. TypeScript Enum Extension Patterns for WorldEntityType

**Question**: How should we extend the existing WorldEntityType const object to add Container and regional types while maintaining type safety?

**Research Findings**:

- Current pattern uses `const` assertion with `as const` for string literals
- Adding new entries requires updating:
  1. The `WorldEntityType` const object (source of truth)
  1. The derived type `type WorldEntityType = (typeof WorldEntityType)[keyof typeof WorldEntityType]`
  1. `ENTITY_TYPE_SUGGESTIONS` mapping
  1. `ENTITY_TYPE_META` mapping
- TypeScript will automatically infer the union type from new const entries

**Decision**: Extend `WorldEntityType` const object with 14 new entries:

- Container types: `Locations`, `People`, `Events`, `History`, `Lore`, `Bestiary`, `Items`, `Adventures`, `Geographies`, `Other` (already exists, reuse)
- Regional types: `GeographicRegion`, `PoliticalRegion`, `CulturalRegion`, `MilitaryRegion`

**Rationale**: Minimal changes to existing pattern. Type safety maintained automatically. No breaking changes for existing code.

**Alternatives Considered**:

- Separate enum for Container types (rejected: creates fragmentation, complicates type checks)
- Using string unions instead of const object (rejected: loses runtime lookup capability)

---

### 3. Conditional Property Rendering in React Forms

**Question**: What is the best pattern for conditionally rendering different property fields based on selected entity type in EntityDetailForm?

**Research Findings**:

- Common React patterns:
  1. Inline conditional rendering with `&&` or ternary operators
  1. Helper functions that return JSX based on entity type
  1. Configuration-driven rendering with property definitions
- React 19 introduces `use` hook for conditional hooks (not applicable here)
- Performance consideration: Re-rendering on type change is acceptable for forms (not a hot path)

**Decision**: Use inline conditional rendering with helper functions:

```typescript
function renderCustomProperties(entityType: WorldEntityType) {
  switch (entityType) {
    case WorldEntityType.GeographicRegion:
      return <GeographicRegionProperties />;
    case WorldEntityType.PoliticalRegion:
      return <PoliticalRegionProperties />;
    // ... etc
    default:
      return null;
  }
}
```

**Rationale**:

- Clear and readable
- Easy to test each property set independently
- Aligns with existing EntityDetailForm pattern of conditional UI
- Helper components keep EntityDetailForm.tsx manageable

**Alternatives Considered**:

- Configuration object with field definitions (rejected: over-engineering for 4 entity types)
- Single monolithic component with all fields (rejected: poor maintainability)

---

### 4. Numeric Input Validation with Number.MAX_SAFE_INTEGER

**Question**: How should we validate and format large numeric inputs (Population, Area) in React forms with Fluent UI?

**Research Findings**:

- JavaScript `Number.MAX_SAFE_INTEGER` = 9,007,199,254,740,991 (2^53 - 1)
- Fluent UI v9 `Input` supports `type="number"` but has accessibility concerns (screen readers announce spinner buttons)
- Better approach: `type="text"` with numeric validation and formatting
- For display: Use `Intl.NumberFormat` for locale-aware formatting (e.g., "195,000,000")
- For input: Allow raw numbers or formatted with commas, parse before validation

**Decision**: Implement numeric validation utility in `lib/validators/numericValidation.ts`:

- Parse function: Strip commas, validate numeric, check bounds
- Format function: Add thousand separators for display
- Validate function: Check non-negative, max Number.MAX_SAFE_INTEGER
- Use `type="text"` inputs with `inputMode="numeric"` for mobile keyboards

**Rationale**:

- Better accessibility (no unwanted spinner controls)
- Flexible input formats (users can type "1000000" or "1,000,000")
- Locale-aware formatting
- Clear validation error messages

**Alternatives Considered**:

- `type="number"` inputs (rejected: accessibility issues, browser inconsistencies)
- BigInt for arbitrary precision (rejected: overkill for this use case, adds complexity)
- String storage (rejected: loses numeric type in database, complicates queries)

---

### 5. Icon Mapping Strategy for Container and Regional Types

**Question**: Which Fluent UI v9 icons should be used for Container types and regional types to ensure visual distinction?

**Research Findings**:

- Fluent UI v9 provides `@fluentui/react-icons` package with 2000+ icons
- Container types should use folder/organizational icons:
  - `FolderRegular` (general container)
  - Specialized: `MapRegular`, `PeopleRegular`, `CalendarRegular`, `BookRegular`, etc.
- Regional types should use map/boundary icons:
  - `GlobeRegular`, `MapRegular`, `LocationRegular`, `ShieldRegular`
- Current WorldSidebar uses icon mapping in `ENTITY_TYPE_META` (doesn't exist yet, only description)

**Decision**: Extend `ENTITY_TYPE_META` with `icon` property:

```typescript
{
  label: string;
  description: string;
  category: '...';
  icon: string; // Icon component name from @fluentui/react-icons
}
```

Icon mappings:

- **Containers**: `FolderRegular` (Locations, People, Events, Items, Adventures), `BookRegular` (Lore, History), `PawRegular` (Bestiary), `MapRegular` (Geographies)
- **Regional**: `GlobeRegular` (GeographicRegion), `ShieldRegular` (PoliticalRegion), `PeopleRegular` (CulturalRegion), `ShieldRegular` (MilitaryRegion)

**Rationale**:

- Clear visual distinction between containers and content
- Semantic meaning conveyed through icons
- Accessible (icons accompanied by labels)
- Consistent with Fluent design language

**Alternatives Considered**:

- Custom SVG icons (rejected: inconsistent with Fluent, adds design work)
- Color coding instead of icons (rejected: fails WCAG color-only information prohibition)

---

### 6. Custom Properties JSON Schema in Cosmos DB

**Question**: How should custom properties be structured in the Cosmos DB `Properties` JSON field to support future extensibility?

**Research Findings**:

- Current WorldEntity schema includes `Properties` field (flexible JSON)
- Options for structure:
  1. Flat object: `{ "Climate": "Tropical", "Population": 1000000 }`
  1. Namespaced: `{ "geographic": { "climate": "Tropical" } }`
  1. Typed arrays: `{ "properties": [{ "key": "Climate", "value": "Tropical" }] }`
- Azure Cosmos DB indexing: Flat properties are easier to query but use more RUs
- Future AI Search integration: Flat structure easier to index

**Decision**: Use flat object structure for custom properties:

```json
{
  "Properties": {
    "Climate": "Tropical",
    "Terrain": "Rainforest", 
    "Population": 500000,
    "Area": 250000,
    "Languages": ["English", "Spanish"],
    "MemberStates": ["Country1", "Country2"]
  }
}
```

**Rationale**:

- Simple serialization/deserialization in TypeScript
- Easy to query specific properties (e.g., `WHERE c.Properties.Population > 1000000`)
- Consistent with PropertySchema example in DATA_MODEL.md
- Text lists as JSON arrays (natural representation)

**Alternatives Considered**:

- Namespaced structure (rejected: adds complexity without clear benefit for current scope)
- Schema validation in backend (rejected: out of scope, frontend validation sufficient)

---

### 7. Redux State Management for Entity Type Selection

**Question**: Does entity type selection require Redux state, or can it be local component state?

**Research Findings**:

- Current `worldSidebarSlice` manages:
  - `editingEntityId`, `newEntityParentId`, `selectedWorldId`
  - `hasUnsavedChanges`, `mainPanelMode`
- Entity type selection is transient form state (only relevant during creation/editing)
- No other components need to read selected entity type
- Redux principle: "Lift state only when shared across components"

**Decision**: Keep entity type selection as local state in EntityDetailForm

- State: `const [entityType, setEntityType] = useState<WorldEntityType | ''>(``);`
- No Redux actions needed
- Pass to API on submit

**Rationale**:

- Follows React best practices (local state for local concerns)
- Reduces Redux boilerplate
- Simplifies testing (no need to mock store for type selection)
- Maintains existing pattern (description and name are local state)

**Alternatives Considered**:

- Add to Redux (rejected: unnecessary complexity, violates separation of concerns)

---

## Summary of Decisions

| Research Area | Decision | Impact |
|---------------|----------|--------|
| Tag Input Component | Custom component using Fluent `Tag` + `Input` primitives | New: `src/components/shared/TagInput/` |
| Type System Extension | Add 14 new entries to `WorldEntityType` const | Modified: `worldEntity.types.ts` |
| Conditional Properties | Switch-based helper function returning property components | Modified: `EntityDetailForm.tsx` |
| Numeric Validation | Text input with custom parsing/formatting utility | New: `lib/validators/numericValidation.ts` |
| Icon Mapping | Extend `ENTITY_TYPE_META` with icon property | Modified: `worldEntity.types.ts`, `WorldSidebar.tsx` |
| JSON Schema | Flat object structure in Properties field | No code changes (design decision) |
| State Management | Local component state for entity type | No Redux changes needed |

All technical unknowns have been resolved. Ready for Phase 1 (Design).
