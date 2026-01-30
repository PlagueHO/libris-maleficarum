# Component Contracts: EntityTypeSelector

**Date**: 2026-01-29  
**Component**: EntityTypeSelector  
**Location**: `libris-maleficarum-app/src/components/shared/EntityTypeSelector/`

---

## Component API

### Props Interface

```typescript
export interface EntityTypeSelectorProps {
  /**
   * Currently selected entity type
   * Empty string '' represents no selection (unselected state)
   */
  value: WorldEntityType | '';

  /**
   * Callback fired when user selects an entity type
   * @param type - The selected WorldEntityType
   */
  onValueChange: (type: WorldEntityType) => void;

  /**
   * Parent entity type for context-aware suggestions.
   * When provided, the component will show recommended child types at the top.
   * When null/undefined, shows common root-level types as recommendations.
   */
  parentType?: WorldEntityType | null;

  /**
   * Whether to bypass parent-based filtering and allow all entity types.
   * When true, all entity types are treated as recommended and shown in the
   * selector, preserving the underlying WorldEntityType registry order and
   * rendering the "Recommended" section with every type.
   * Default: false
   */
  allowAllTypes?: boolean;

  /**
   * Whether the selector is disabled (non-interactive).
   * Default: false
   */
  disabled?: boolean;

  /**
   * Custom placeholder text for the selector button.
   * Default: "Select entity type"
   */
  placeholder?: string;

  /**
   * ARIA label for accessibility.
   * If not provided, defaults to placeholder text.
   */
  'aria-label'?: string;

  /**
   * ARIA invalid state for form validation.
   * Set to true when the selection is invalid/required but empty.
   */
  'aria-invalid'?: boolean;
}
```

### Component Signature

```typescript
export function EntityTypeSelector(props: EntityTypeSelectorProps): JSX.Element;
```

### Usage Examples

#### Basic Usage

```tsx
import { EntityTypeSelector } from'@/components/shared/EntityTypeSelector';
import { WorldEntityType } from '@/services/types/worldEntity.types';

function MyForm() {
  const [entityType, setEntityType] = useState<WorldEntityType | ''>('');

  return (
    <EntityTypeSelector
      value={entityType}
      onValueChange={setEntityType}
      placeholder="Select entity type"
    />
  );
}
```

#### With Parent Context (Recommended Types)

```tsx
function CreateChildEntityForm({ parentEntity }: { parentEntity: WorldEntity }) {
  const [childType, setChildType] = useState<WorldEntityType | ''>('');

  return (
    <EntityTypeSelector
      value={childType}
      onValueChange={setChildType}
      parentType={parentEntity.entityType}  // Shows appropriate child types
      placeholder="Select child entity type"
    />
  );
}
```

#### Allow All Types (No Filtering)

```tsx
function UnrestrictedEntitySelector() {
  const [entityType, setEntityType] = useState<WorldEntityType | ''>('');

  return (
    <EntityTypeSelector
      value={entityType}
      onValueChange={setEntityType}
      allowAllTypes={true}  // Treats all types as recommended, preserves registry order
    />
  );
}
```

#### Form Validation with ARIA

```tsx
function ValidatedForm() {
  const [entityType, setEntityType] = useState<WorldEntityType | ''>('');
  const [touched, setTouched] = useState(false);
  const isInvalid = touched && !entityType;

  return (
    <div>
      <label htmlFor="entity-type-selector">Entity Type *</label>
      <EntityTypeSelector
        value={entityType}
        onValueChange={(type) => {
          setEntityType(type);
          setTouched(true);
        }}
        aria-label="Entity Type (required)"
        aria-invalid={isInvalid}
      />
      {isInvalid && <span role="alert">Entity type is required</span>}
    </div>
  );
}
```

---

## Breaking Changes

**None** - The component props interface remains unchanged from the previous version. All enhancements are internal implementation details (icon rendering, layout, spacing). Existing code using EntityTypeSelector will continue to work without modifications.

### Version Compatibility

- **Previous Version**: Component without icons, category-based grouping
- **Enhanced Version**: Component with icons, Recommended/Other grouping
- **API Compatibility**: 100% backward compatible
- **Migration Required**: None

---

## Internal Implementation Contracts

### Icon Rendering Contract

```typescript
/**
 * Internal: Icon rendering helper
 * Dynamically renders Lucide React icon based on entity type metadata
 */
interface IconRenderProps {
  entityType: WorldEntityType;
  className?: string;
}

function EntityTypeIcon({ entityType, className }: IconRenderProps): JSX.Element {
  const meta = getEntityTypeMeta(entityType);
  const IconComponent = LucideIcons[meta.icon as keyof typeof LucideIcons];
  
  return (
    <IconComponent 
      className={cn("w-4 h-4", className)} 
      aria-hidden="true" 
    />
  );
}
```

### Filtering Contract

```typescript
/**
 * Internal: Filter logic remains unchanged
 * Matches against entity type label and description (case-insensitive)
 */
interface FilterResult {
  recommendedFiltered: WorldEntityType[];
  otherFiltered: WorldEntityType[];
}

function filterEntityTypes(
  search: string,
  recommendedTypes: WorldEntityType[],
  allTypes: WorldEntityType[]
): FilterResult;
```

### Grouping Contract

```typescript
/**
 * Internal: Grouping logic updated
 * BEFORE: Grouped by category (Geography, Characters, Events, etc.)
 * AFTER: Grouped into Recommended and Other sections only
 */
interface GroupedTypes {
  recommended: WorldEntityType[];  // Context-based suggestions
  other: WorldEntityType[];        // All remaining types, alphabetically sorted
}

function groupEntityTypes(
  parentType: WorldEntityType | null,
  allTypes: WorldEntityType[]
): GroupedTypes;
```

---

## Accessibility Contracts

### Keyboard Navigation

| Action | Expected Behavior |
|--------|-------------------|
| Tab | Focus trigger button |
| Enter/Space | Open dropdown |
| Arrow Down/Up | Navigate list items (crosses section boundaries) |
| Enter (on item) | Select item, close dropdown |
| Escape | Close dropdown without selection |
| Type (when open) | Filter list by search term |

### ARIA Roles & States

| Element | Role | Required Attributes |
|---------|------|---------------------|
| Trigger button | `role="combobox"` | `aria-expanded`, `aria-label` |
| Dropdown container | (Radix Popover) | Auto-managed by Radix |
| List item | `role="option"` | `aria-selected` |
| Icon | (decorative) | `aria-hidden="true"` |
| Section headings | (presentational) | None (styled divs, not semantic headings) |

### Screen Reader Announcements

- **Trigger button**: "[placeholder] button, collapsed" or "expanded"
- **Filter input**: "Filter... Search box"
- **List item**: "[Entity Type Label] option, selected" or "not selected"
- **Icons**: Not announced (aria-hidden)

---

## Performance Contracts

### Response Time Guarantees

| Action | Target | Measurement |
|--------|--------|-------------|
| Filter input response | <100ms | Time from keystroke to UI update |
| Dropdown open | <200ms | Time from click to fully rendered list |
| Icon load | <50ms | Icons bundled, no network delay |

### Memory Constraints

- **Entity type list**: ~30 items × ~50 bytes = ~1.5KB (negligible)
- **Icon components**: Lazy-loaded from Lucide React bundle
- **Total component memory**: <5KB including props and state

---

## Testing Contracts

### Unit Test Coverage

**Target**: ≥90% code coverage for `EntityTypeSelector.tsx`

**Required Test Cases**:

- [ ] Renders with default props
- [ ] Renders icons for each entity type
- [ ] Filters entity types by search term
- [ ] Shows Recommended section when parentType provided
- [ ] Shows Other section with alphabetical sorting
- [ ] Handles no recommendations scenario (flat list)
- [ ] Shows empty state message when filter has no matches
- [ ] Maintains keyboard navigation
- [ ] Has zero accessibility violations (jest-axe)
- [ ] Allows selection and calls onValueChange
- [ ] Respects disabled prop

### Integration Test Scenarios

- Component renders within WorldEntityForm
- Selection updates parent form state
- Validation error states display correctly

### Accessibility Test Requirements

```typescript
// Mandatory test using jest-axe
it('should have no accessibility violations', async () => {
  const { container } = render(<EntityTypeSelector {...props} />);
  const results = await axe(container);
  expect(results).toHaveNoViolations();
});
```

---

## Dependencies

### External Packages

- `react` (^19.2.0) - Core framework
- `lucide-react` (^0.563.0) - Icon library
- `@radix-ui/react-popover` (^1.1.15) - Dropdown primitive
- `class-variance-authority` (^0.7.1) - Styling utilities
- `tailwind-merge` (^3.4.0) - className merging  

### Internal Dependencies

- `@/components/ui/popover` - Shadcn/UI Popover
- `@/components/ui/input` - Shadcn/UI Input
- `@/components/ui/button` - Shadcn/UI Button
- `@/components/ui/separator` - Shadcn/UI Separator
- `@/services/types/worldEntity.types` - Type definitions
- `@/lib/utils` - `cn()` utility

---

## Summary

**API Contract**: Unchanged - 100% backward compatible  
**Props Interface**: No modifications to existing props  
**Accessibility Contract**: WCAG 2.2 Level AA compliant  
**Performance Contract**: <100ms filter, <200ms render  
**Testing Contract**: ≥90% coverage, zero axe violations  

**Breaking Changes**: None

**Migration Path**: N/A (no breaking changes)
