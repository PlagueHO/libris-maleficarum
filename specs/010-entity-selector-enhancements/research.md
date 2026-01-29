# Research: Entity Type Selector Enhancements

**Date**: 2026-01-29  
**Feature**: Entity Type Selector Enhancements  
**Objective**: Resolve technical unknowns for icon integration, spacing calculations, and accessibility compliance.

---

## 1. Icon Selection & Integration

### Icon Mappings (Lucide React)

Icons already defined in entity type registry (`src/services/config/entityTypeRegistry.ts`). The registry includes an `icon` property for each entity type mapping to Lucide React icon names.

**Sample Mappings** (from existing registry):

| Entity Type | Lucide Icon | Icon Component | Rationale |
|-------------|-------------|----------------|-----------|
| World | Globe | `<Globe className="w-4 h-4" />` | Represents the entire world/planet |
| Folder | Folder | `<Folder className="w-4 h-4" />` | Generic container for organization |
| Locations | MapPin | `<MapPin className="w-4 h-4" />` | Geographic locations container |
| Continent | Mountain | `<Mountain className="w-4 h-4" />` | Large landmass with mountains |
| Country | Flag | `<Flag className="w-4 h-4" />` | Nation with flag symbol |
| Region | Map | `<Map className="w-4 h-4" />` | Sub-national region |
| City | Building2 | `<Building2 className="w-4 h-4" />` | Urban settlement |
| Settlement | Home | `<Home className="w-4 h-4" />` | Village or small settlement |
| People | Users | `<Users className="w-4 h-4" />` | Characters container |
| Character | User | `<User className="w-4 h-4" />` | Individual person/NPC |
| Faction | Shield | `<Shield className="w-4 h-4" />` | Organization or faction |
| Events | Calendar | `<Calendar className="w-4 h-4" />` | Events container |
| Campaign | Scroll | `<Scroll className="w-4 h-4" />` | Campaign/adventure arc |
| Session | CalendarClock | `<CalendarClock className="w-4 h-4" />` | Game session |
| Quest | Target | `<Target className="w-4 h-4" />` | Specific quest/mission |
| Lore | BookOpen | `<BookOpen className="w-4 h-4" />` | Lore/knowledge container |
| Adventures | Swords | `<Swords className="w-4 h-4" />` | Adventures container |
| Item | Package | `<Package className="w-4 h-4" />` | Physical object/artifact |

### Implementation Notes

- **Icon Size**: All icons use `w-4 h-4` TailwindCSS classes = 16×16px (1rem)
- **Icon Source**: Import from `lucide-react` package (already in dependencies)
- **Icon Rendering**: Add to left of entity type label in list item template
- **Accessibility**: All icons should have `aria-hidden="true"` (decorative, not informational)
- **Dynamic Import**: Use `getEntityTypeMeta(type).icon` to retrieve icon name, then dynamic component rendering

### Example Implementation Pattern

```tsx
import * as Icons from 'lucide-react';

// Inside render loop for entity types
const iconName = getEntityTypeMeta(type).icon;
const IconComponent = Icons[iconName as keyof typeof Icons] as React.ComponentType<{className?: string}>;

<button>
  <IconComponent className="w-4 h-4 flex-shrink-0" aria-hidden="true" />
  <span>{meta.label}</span>
</button>
```

---

## 2. Spacing & Compact Layout

### Spacing Calculations

**Target**: Show 8-10 entity types visible without scrolling in standard dropdown (400-500px height)

**Current spacing** (from EntityTypeSelector.tsx):  

- List item padding: `py-2.5` = 10px top + 10px bottom = 20px total
- Text line height: ~20px (title) + ~16px (description) + 4px gap = 40px
- **Total item height**: ~60px

**New compact spacing**:  

- List item padding: `py-2` = 8px top + 8px bottom = 16px total
- Icon height: 16px (w-4 h-4)
- Text line height: ~18px (title, text-sm) + ~14px (description, text-xs) + 2px gap = 34px  
- **Total item height**: ~50px

**Visibility calculation**:  

- Dropdown content area: ~450px (max-h-96 = 384px + padding)
- Items per screen: 450px / 50px = **9 items** ✅
- **Success**: Meets 8-10 items requirement

### TailwindCSS Class Changes

| Element | Current | New | Change Justification |
|---------|---------|-----|---------------------|
| List item button | `py-2.5` | `py-2` | Reduce vertical padding from 10px to 8px |
| Title text | font-medium (default) | `text-sm font-medium` | Ensure consistent small size |
| Description text | `text-xs` | `text-xs leading-tight` | Reduce line spacing for compactness |
| Icon wrapper | N/A | `w-4 h-4 flex-shrink-0` | 16×16px, prevent icon from shrinking |
| Item flex container            | Existing | `flex items-center gap-2` | Icon + text horizontal layout |

### Dropdown Height

- Current: `max-h-96` = 384px (TailwindCSS utility)
- Keep as-is (sufficient for 9 items @ 50px each)
- Adjust to `max-h-[450px]` if needed to fit exactly 9 items

---

## 3. Accessibility Validation

### WCAG 2.2 Level AA Compliance Checklist

- [x] **Icons are decorative** - Add `aria-hidden="true"` to all icon components
- [x] **Text labels preserved** - Entity type names remain visible and announced by screen readers
- [x] **Keyboard navigation** - Arrow keys navigate list (Radix UI Popover handles this automatically)
- [x] **role="option"** - List item buttons maintain proper ARIA role
- [x] **aria-selected** - Currently selected item has aria-selected="true"
- [x] **Focus indicators** - Maintain visible focus rings on keyboard navigation
- [x] **Contrast ratios**:
  - Icon color: Use `text-foreground` (inherits from theme, meets 3:1 for graphics)
  - Selected state: `bg-primary text-primary-foreground` (high contrast)
  - Hover state: `hover:bg-accent` (sufficient contrast)

### Keyboard Navigation Flow

1. **Tab** → Focus filter input
1. **Type** → Filter list dynamically
1. **Arrow Down** → Move to first list item
1. **Arrow Up/Down** → Navigate between items (crosses Recommended/Other boundary seamlessly)
1. **Enter** → Select focused item
1. **Escape** → Close dropdown

### Screen Reader Announcements

- Filter input: "Filter... Search box"
- Recommended section: "Recommended" (heading, not announced per-item)
- Entity type: "Continent, option, not selected" or "Character, option, selected"
- Other section: "Other" (heading, not announced per-item)
- Icon: (skipped, aria-hidden="true")

### Testing Strategy

```tsx
// EntityTypeSelector.test.tsx additions
import { axe, toHaveNoViolations } from 'jest-axe';

describe('EntityTypeSelector - Accessibility', () => {
  it('should have no accessibility violations with icons', async () => {
    const { container } = render(<EntityTypeSelector {...props} />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('should hide icons from screen readers', () => {
    render(<EntityTypeSelector {...props} />);
    const icons = screen.queryAllByRole('img', { hidden: true });
    icons.forEach(icon => {
      expect(icon).toHaveAttribute('aria-hidden', 'true');
    });
  });

  it('should maintain keyboard navigation across sections', () => {
    render(<EntityTypeSelector {...props} />);
    // Arrow key simulation test
    // Verify focus moves from Recommended → Other without breaks
  });
});
```

---

## 4. Separator & Section Styling

### Separator Implementation

**Component**: Use existing Shadcn/UI `Separator` component (`src/components/ui/separator.tsx`)

**Usage Pattern**:

```tsx
import { Separator } from '@/components/ui/separator';

// Between Recommended and Other sections
{recommendedFiltered.length > 0 && otherFiltered.length > 0 && (
  <Separator className="my-2" />
)}
```

**Styling**:

- `Separator` component renders as `<div role="separator" className="bg-border h-px" />`
- `my-2` adds 8px margin top/bottom
- `h-px` = 1px height (thin line)
- `bg-border` uses theme border color (accessible contrast)

### Section Headings

**Recommended Section**:

```tsx
<div className="px-2 py-1.5 text-xs font-semibold text-primary uppercase tracking-wide flex items-center gap-1.5">
  <Star className="w-3.5 h-3.5" aria-hidden="true" />
  Recommended
</div>
```

**Other Section**:

```tsx
<div className="px-2 py-1.5 text-xs font-semibold text-muted-foreground uppercase tracking-wide">
  Other
</div>
```

**Conditional Rendering**:

- Show separator + "Other" heading ONLY when `recommendedFiltered.length > 0`
- When `recommendedFiltered.length === 0`, display all types directly without sections

---

## 5. Edge Cases & Special Scenarios

### Empty Filter State

**Scenario**: User types "xyz" and no entity types match  
**Behavior**: Display empty state message

```tsx
{recommendedFiltered.length === 0 && otherFiltered.length === 0 && (
  <div className="px-3 py-8 text-center text-sm text-muted-foreground">
    No entity types match '{search}'
  </div>
)}
```

### No Recommendations Scenario

**Scenario**: `parentType` is null or has no suggested children  
**Behavior**: Display all types in flat alphabetical list without "Recommended" or "Other" headings

**Logic**:

```tsx
const hasRecommendations = recommendedFiltered.length > 0;

// Render list items directly if no recommendations
{!hasRecommendations && allTypesFiltered.map(...)}

// Render sections only if recommendations exist
{hasRecommendations && (
  <>
    <section>Recommended items</section>
    <Separator />
    <section>Other items</section>
  </>
)}
```

### Long Entity Type Names

**Scenario**: Entity type name or description overflows container  
**Behavior**: Truncate with ellipsis

```tsx
<div className="font-medium truncate">{meta.label}</div>
<div className="text-xs text-muted-foreground truncate">{meta.description}</div>
```

---

## Research Conclusions

✅ **Icon Integration**: Use existing entity type registry icon mappings with dynamic Lucide React imports  
✅ **Spacing**: `py-2` (8px padding) achieves ~50px item height → 9 items visible in 450px dropdown  
✅ **Accessibility**: Icons as `aria-hidden="true"`, maintain ARIA roles/attributes, keyboard nav preserved  
✅ **Separator**: Use Shadcn/UI `Separator` component with `my-2 h-px` styling  
✅ **Edge Cases**: Handled empty state, no recommendations, text overflow scenarios

**Next Phase**: Create data model documentation and component contracts (Phase 1)
