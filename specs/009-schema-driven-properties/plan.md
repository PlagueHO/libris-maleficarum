# Implementation Plan: Schema-Driven Entity Properties (Phase 1)

**Feature**: [spec.md](spec.md)  
**Branch**: `009-schema-driven-properties`  
**Target**: libris-maleficarum-app (React + TypeScript)  
**Created**: 2026-01-26

## Architecture Overview

### Core Components

```
libris-maleficarum-app/src/
├── services/config/
│   └── entityTypeRegistry.ts          # Extended with PropertyFieldSchema + propertySchema
├── components/MainPanel/
│   ├── DynamicPropertyField.tsx       # NEW: Renders single field based on schema
│   ├── DynamicPropertiesForm.tsx      # NEW: Edit mode renderer (uses DynamicPropertyField)
│   ├── DynamicPropertiesView.tsx      # NEW: Read-only mode renderer
│   ├── WorldEntityForm.tsx            # UPDATED: Use DynamicPropertiesForm
│   └── EntityDetailReadOnlyView.tsx   # UPDATED: Use DynamicPropertiesView
└── lib/validators/
    └── propertyValidation.ts          # NEW: Schema-based validation logic
```

### Data Flow

```
EntityTypeRegistry
  ↓ (propertySchema)
WorldEntityForm / EntityDetailReadOnlyView
  ↓
DynamicPropertiesForm / DynamicPropertiesView
  ↓ (iterates schema)
DynamicPropertyField (× N fields)
  ↓
Input/Textarea/TagInput components
```

## Implementation Phases

### Phase 1: Schema Foundation (Priority: Critical)

Define the schema type system and add schemas to existing Regional types.

**Tasks**:

1. **Define PropertyFieldSchema interface** in `entityTypeRegistry.ts`
   - Add field types enum/union: `text`, `textarea`, `integer`, `decimal`, `tagArray`
   - Add validation rules interface: `required`, `min`, `max`, `pattern`
   - Add optional attributes: `placeholder`, `description`, `maxLength`
   
2. **Extend EntityTypeConfig** with optional `propertySchema` array

3. **Add propertySchema to 4 Regional types** in ENTITY_TYPE_REGISTRY:
   - **GeographicRegion**: Climate (textarea), Terrain (textarea), Population (integer), Area (decimal)
   - **PoliticalRegion**: GovernmentType (textarea), MemberStates (tagArray), EstablishedDate (text)
   - **CulturalRegion**: Languages (tagArray), Religions (tagArray), CulturalTraits (textarea)
   - **MilitaryRegion**: CommandStructure (textarea), StrategicImportance (textarea), MilitaryAssets (tagArray)

**Acceptance Criteria**:
- TypeScript compilation succeeds with new schema types
- All 4 Regional types have complete propertySchema definitions
- Schema definitions match existing component field definitions exactly

**Files Modified**:
- `src/services/config/entityTypeRegistry.ts`

---

### Phase 1: Dynamic Field Renderer (Priority: Critical)

Build the core dynamic field renderer that handles all 5 field types.

**Tasks**:

1. **Create DynamicPropertyField component** (`src/components/MainPanel/DynamicPropertyField.tsx`)
   - Props: `fieldSchema`, `value`, `onChange`, `disabled`, `readOnly`
   - Switch on field type to render appropriate input component
   - **text**: Render `<Input>` with validation
   - **textarea**: Render `<Textarea>` with character counter
   - **integer**: Render `<Input type="text" inputMode="numeric">` with integer validation
   - **decimal**: Render `<Input type="text" inputMode="decimal">` with decimal validation
   - **tagArray**: Render `<TagInput>` with duplicate prevention
   - Handle validation errors per field type
   - Support required, min/max, pattern validation rules
   - Apply type coercion for numeric fields (string "123" → number 123)

2. **Create propertyValidation utility** (`src/lib/validators/propertyValidation.ts`)
   - Function: `validateField(schema, value): { valid: boolean; error?: string; coercedValue?: unknown }`
   - Validation rules: required, min/max (numbers), pattern (text), type checking
   - Type coercion logic for integer/decimal fields
   - Reuse existing `validateInteger`/`validateDecimal` from `numericValidation.ts`

3. **Add duplicate tag prevention** to TagInput (if not already present)
   - Prevent adding duplicate tags
   - Visual feedback: briefly highlight existing tag (use Tailwind animation)

**Acceptance Criteria**:
- DynamicPropertyField renders all 5 field types correctly
- Validation rules (required, min/max, pattern) work for each type
- Type coercion works: "123" → 123, but "abc" → validation error
- TagInput prevents duplicates with visual feedback
- Accessibility: All fields have proper labels, aria-invalid, aria-describedby

**Files Created**:
- `src/components/MainPanel/DynamicPropertyField.tsx`
- `src/lib/validators/propertyValidation.ts`

**Files Modified**:
- `src/components/shared/TagInput/TagInput.tsx` (if duplicate prevention needed)

---

### Phase 2: Edit Mode Integration (Priority: Critical)

Create the dynamic form renderer and integrate into WorldEntityForm.

**Tasks**:

1. **Create DynamicPropertiesForm component** (`src/components/MainPanel/DynamicPropertiesForm.tsx`)
   - Props: `entityType`, `value`, `onChange`, `disabled`
   - Fetch propertySchema from registry using `getEntityTypeConfig(entityType)`
   - If no schema, render nothing (FR-008)
   - Iterate over schema fields and render `<DynamicPropertyField>` for each
   - Aggregate field values into single properties object
   - Handle section header: "Geographic Properties", "Political Properties", etc.
   - Wrap in `<div className="border-t pt-6 mt-6">` for visual separation

2. **Update WorldEntityForm** to use DynamicPropertiesForm
   - Replace `renderCustomProperties()` switch statement
   - Call `<DynamicPropertiesForm>` with entityType, customProperties, onChange
   - Remove imports for GeographicRegionProperties, etc.
   - Keep customProperties state as `Record<string, unknown> | null`

3. **Handle empty/undefined properties** (FR-017)
   - On save, only serialize properties if object has keys
   - Filter out undefined/null values before JSON.stringify

**Acceptance Criteria**:
- WorldEntityForm renders GeographicRegion properties dynamically
- All 4 Regional types render correctly in edit mode
- Entity types without propertySchema render no custom properties section
- Form saves property values correctly to properties field
- Empty properties not saved (undefined instead of `{}`)

**Files Created**:
- `src/components/MainPanel/DynamicPropertiesForm.tsx`

**Files Modified**:
- `src/components/MainPanel/WorldEntityForm.tsx`

---

### Phase 3: Read-Only Mode Integration (Priority: Critical)

Create the dynamic read-only renderer and integrate into EntityDetailReadOnlyView.

**Tasks**:

1. **Create DynamicPropertiesView component** (`src/components/MainPanel/DynamicPropertiesView.tsx`)
   - Props: `entityType`, `value`
   - Fetch propertySchema from registry
   - If no schema but has properties data → fallback to generic Object.entries() renderer (clarification answer #1)
   - Iterate over schema fields and render formatted values
   - **text/textarea**: Display as `<div className="text-sm">`
   - **integer/decimal**: Format with `formatNumericDisplay()` from existing util
   - **tagArray**: Render as Shadcn badges (like existing components)
   - Section header with entity type-specific title
   - Handle missing property values (display field as empty placeholder)

2. **Update EntityDetailReadOnlyView**
   - Replace generic custom properties rendering with `<DynamicPropertiesView>`
   - Keep fallback for entities without schema (use generic Object.entries())
   - Remove hardcoded "Custom Properties" heading (DynamicPropertiesView handles it)

**Acceptance Criteria**:
- Read-only view displays all 4 Regional type properties correctly
- Numeric values formatted with thousand separators
- Tag arrays displayed as badges
- Entity types without schema fall back to generic key-value renderer
- Entities with properties but no schema display using Object.entries()

**Files Created**:
- `src/components/MainPanel/DynamicPropertiesView.tsx`

**Files Modified**:
- `src/components/MainPanel/EntityDetailReadOnlyView.tsx`

---

### Phase 4: Testing & Validation (Priority: High)

Comprehensive testing for all dynamic rendering paths.

**Tasks**:

1. **Unit tests for DynamicPropertyField** (`DynamicPropertyField.test.tsx`)
   - Test all 5 field types render correctly
   - Test validation rules (required, min/max, pattern)
   - Test type coercion for integer/decimal
   - Test duplicate tag prevention
   - Accessibility testing with jest-axe

2. **Unit tests for DynamicPropertiesForm** (`DynamicPropertiesForm.test.tsx`)
   - Test rendering with schema
   - Test rendering without schema (no section displayed)
   - Test onChange aggregation
   - Test save with empty properties (not stored)

3. **Unit tests for DynamicPropertiesView** (`DynamicPropertiesView.test.tsx`)
   - Test formatted display for all field types
   - Test fallback to generic renderer
   - Test missing property values display

4. **Integration tests**
   - Test edit flow: open GeographicRegion, modify Climate, save
   - Test create flow: create MilitaryRegion, fill properties, save
   - Test view flow: view PoliticalRegion with properties
   - Test entity without schema (Character) shows no custom properties
   - Test existing entity data displays correctly (SC-003)

5. **Update existing tests**
   - Update `WorldEntityForm.test.tsx` (remove component-specific tests)
   - Update `EntityDetailReadOnlyView.test.tsx` (use dynamic renderer expectations)

**Acceptance Criteria**:
- All new components have >90% test coverage
- All tests pass
- Accessibility tests pass with no violations
- Integration tests cover all 5 user stories

**Files Created**:
- `src/components/MainPanel/__tests__/DynamicPropertyField.test.tsx`
- `src/components/MainPanel/__tests__/DynamicPropertiesForm.test.tsx`
- `src/components/MainPanel/__tests__/DynamicPropertiesView.test.tsx`

**Files Modified**:
- `src/components/MainPanel/WorldEntityForm.test.tsx`
- `src/components/MainPanel/EntityDetailReadOnlyView.test.tsx`

---

### Phase 5: Migration & Cleanup (Priority: Medium)

Remove old custom property components and verify backward compatibility.

**Tasks**:

1. **Verify existing entity data compatibility**
   - Manual testing: Load existing GeographicRegion entities
   - Verify properties display correctly in both view and edit modes
   - Test entities created before migration still work

2. **Delete old custom property components**
   - Delete `src/components/MainPanel/customProperties/GeographicRegionProperties.tsx`
   - Delete `src/components/MainPanel/customProperties/PoliticalRegionProperties.tsx`
   - Delete `src/components/MainPanel/customProperties/CulturalRegionProperties.tsx`
   - Delete `src/components/MainPanel/customProperties/MilitaryRegionProperties.tsx`
   - Delete `src/components/MainPanel/customProperties/index.ts`
   - Delete test files: `*Properties.test.tsx` (4 files)

3. **Update imports**
   - Remove unused imports from WorldEntityForm
   - Remove unused type exports

4. **Code quality checks**
   - Run `pnpm lint`
   - Run `pnpm type-check`
   - Run `pnpm test`
   - Run `pnpm find-deadcode`

**Acceptance Criteria**:
- All old custom property components deleted (~500+ lines removed)
- No TypeScript errors
- No ESLint errors
- No dead code detected
- All tests pass

**Files Deleted**:
- `src/components/MainPanel/customProperties/GeographicRegionProperties.tsx`
- `src/components/MainPanel/customProperties/GeographicRegionProperties.test.tsx`
- `src/components/MainPanel/customProperties/PoliticalRegionProperties.tsx`
- `src/components/MainPanel/customProperties/PoliticalRegionProperties.test.tsx`
- `src/components/MainPanel/customProperties/CulturalRegionProperties.tsx`
- `src/components/MainPanel/customProperties/CulturalRegionProperties.test.tsx`
- `src/components/MainPanel/customProperties/MilitaryRegionProperties.tsx`
- `src/components/MainPanel/customProperties/MilitaryRegionProperties.test.tsx`
- `src/components/MainPanel/customProperties/index.ts`

---

## Technical Considerations

### Type Safety

```typescript
// PropertyFieldSchema definition
export interface PropertyFieldSchema {
  key: string;
  label: string;
  type: 'text' | 'textarea' | 'integer' | 'decimal' | 'tagArray';
  placeholder?: string;
  description?: string;
  maxLength?: number;
  validation?: {
    required?: boolean;
    min?: number;        // For integer/decimal only
    max?: number;        // For integer/decimal only
    pattern?: string;    // Regex string for text/textarea
  };
}

// Property values typed as Record<string, unknown>
type PropertyValues = Record<string, unknown> | null;
```

### Validation Strategy

1. **Type-level validation**: Field type determines base validation (integer vs decimal vs text)
2. **Schema-level validation**: Optional validation rules from schema (required, min/max, pattern)
3. **Coercion strategy**: Attempt coercion for numeric types; show error if coercion fails
4. **Timing**: Validate on change (immediate feedback) and on blur (format numbers)

### Accessibility

- All fields MUST have `<label>` with `htmlFor`
- Error messages MUST use `aria-describedby` and `aria-invalid`
- Character counters MUST be announced to screen readers
- Tag inputs MUST maintain keyboard navigation support
- Dynamic sections MUST have proper heading hierarchy

### Performance

- Memoize DynamicPropertyField components with React.memo
- Use useMemo for schema lookups
- Minimize re-renders during typing (debounce validation?)

## Success Metrics Mapping

| Success Criterion | Verification Method |
|-------------------|---------------------|
| SC-001: All 4 Regional types use dynamic renderer | Manual testing + integration tests |
| SC-002: New entity types require only config changes | Attempt to add new entity type with properties (post-implementation) |
| SC-003: 100% existing data displays correctly | Test with seed data / existing entities |
| SC-004: Equivalent or better UX | Manual testing + user feedback |
| SC-005: 500+ lines removed | Git diff statistics |
| SC-006: All field types pass a11y testing | jest-axe tests for each field type |

## Rollout Strategy

1. **Phase 1**: Build foundation (schema + field renderer) without breaking changes
2. **Phase 2-3**: Integrate into forms (can coexist with old components during development)
3. **Phase 4-5**: Validate with comprehensive tests and validation enhancements
4. **Final Phase**: Remove old components once validation complete

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Type coercion breaks existing data | Test with real data early; add comprehensive type tests |
| TagInput duplicate prevention breaks UX | Reuse existing TagInput if already supports it; add visual feedback |
| Schema complexity grows | Keep Phase 1 scope limited to 5 field types; defer complex types |
| Performance issues with many fields | Profile rendering; use React.memo and useMemo |
| Accessibility regressions | Run jest-axe on every field type; manual screen reader testing |

## Dependencies

- Existing components: `Input`, `Textarea`, `TagInput`
- Existing utilities: `validateInteger`, `validateDecimal`, `formatNumericDisplay`
- Existing types: `WorldEntityType`, `EntityTypeConfig`

## Out of Scope (Deferred to Future Phases)

- Backend schema storage/fetching
- Schema versioning
- Custom field types (date, select, relationship)
- Admin UI for schema editing
- Schema migration tooling
