# Quickstart: Testing Entity Type Selector Enhancements

**Date**: 2026-01-29  
**Feature**: Entity Type Selector Enhancements  
**Objective**: Step-by-step guide for local testing and validation of enhanced component.

---

## Prerequisites

- Node.js 20.x installed
- pnpm package manager installed
- Repository cloned and dependencies installed (`pnpm install` in `libris-maleficarum-app/`)

---

## Local Development

### 1. Start Development Server

```bash
cd libris-maleficarum-app
pnpm dev
```

**Expected**: Vite dev server starts on `https://127.0.0.1:4000` (first run generates self-signed SSL cert)

### 2. Navigate to Component

Open browser and navigate to a page that uses the EntityTypeSelector component:

- **World Entity Form** (`MainPanel/WorldEntityForm.tsx`) - primary usage location
- **Component Storybook** (if available) - isolated component testing

### 3. Visual Verification Checklist

Open the EntityTypeSelector dropdown and verify:

- [ ] **Icons displayed** - Each entity type shows unique icon to the left of name
- [ ] **Icon size** - Icons are 16×16px (not too large or too small)
- [ ] **Icon alignment** - Icons vertically centered with text
- [ ] **Filter placeholder** - Input shows "Filter..." instead of "Search entity types..."
- [ ] **Compact spacing** - List items have minimal vertical padding
- [ ] **Visibility** - 8-10 entity types visible without scrolling (on 1920x1080 viewport)
- [ ] **Recommended section** - Star icon next to "Recommended" heading
- [ ] **Separator** - Thin horizontal line between Recommended and Other sections
- [ ] **Other section** - "Other" heading displayed
- [ ] **Alphabetical order** - Entity types in Other section sorted A-Z by name
- [ ] **Descriptions** - Entity type descriptions displayed below titles

### 4. Functional Testing

#### Filter Functionality

1. Click EntityTypeSelector to open dropdown
1. Type "cont" in filter input
1. **Verify**: Only entity types containing "cont" in name/description appear (e.g., Continent, Country)
1. Clear filter (delete text)
1. **Verify**: All entity types reappear

#### Keyboard Navigation

1. Tab to EntityTypeSelector button
1. Press Enter/Space to open dropdown
1. **Verify**: Filter input auto-focuses
1. Type to filter (optional)
1. Press Arrow Down
1. **Verify**: First entity type receives focus
1. Press Arrow Down repeatedly
1. **Verify**: Focus moves through Recommended items, crosses separator, continues into Other items seamlessly
1. Press Arrow Up
1. **Verify**: Focus moves backward correctly
1. Press Enter on focused item
1. **Verify**: Item is selected, dropdown closes
1. Re-open dropdown, press Escape
1. **Verify**: Dropdown closes without selection

#### Edge Cases

1. **No Recommendations Test**:
   - Set `allowAllTypes={true}` or `parentType={null}`
   - **Verify**: No "Recommended" section, no separator, entity types shown in flat alphabetical list

1. **Empty Filter Test**:
   - Type "zzz" in filter input (no matches)
   - **Verify**: Message displays: "No entity types match 'zzz'"
   - Clear filter
   - **Verify**: Entity types reappear

1. **Long Name Test**:
   - Resize browser window to narrow viewport (~800px)
   - **Verify**: Long entity type names/descriptions truncate with ellipsis

---

## Automated Testing

### Run Unit Tests

```bash
cd libris-maleficarum-app
pnpm test EntityTypeSelector.test.tsx
```

**Expected Output**:

```text
✓ EntityTypeSelector - renders correctly
✓ EntityTypeSelector - shows icons for each entity type
✓ EntityTypeSelector - maintains accessibility (no axe violations)
✓ EntityTypeSelector - filters entity types by search term
✓ EntityTypeSelector - displays Recommended and Other sections
✓ EntityTypeSelector - handles no recommendations scenario
✓ EntityTypeSelector - shows empty state message when no matches
✓ EntityTypeSelector - maintains keyboard navigation
```

### Run Tests with Coverage

```bash
pnpm test --coverage
```

**Target**: ≥90% coverage for `EntityTypeSelector.tsx`

**Check coverage report**:

```bash
# Open coverage report in browser
open coverage/index.html  # macOS
start coverage/index.html  # Windows
xdg-open coverage/index.html  # Linux
```

### Run Accessibility Tests Only

```bash
pnpm test EntityTypeSelector.test.tsx --grep "accessibility"
```

**Expects**: Zero axe violations reported

---

## Accessibility Testing (Manual)

### Screen Reader Testing

**Optional but Recommended**

#### Windows (NVDA)

1. Install NVDA (free screen reader)
1. Start NVDA
1. Navigate to EntityTypeSelector with Tab
1. Activate with Enter
1. **Listen**: Filter input announced as "Filter... Search box"
1. Arrow down through list
1. **Verify**: Entity type names announced without icon descriptions
1. **Verify**: "Recommended" and "Other" headings announced

#### macOS (VoiceOver)

1. Enable VoiceOver (Cmd+F5)
1. Navigate to EntityTypeSelector
1. Activate selector
1. Use Ctrl+Opt+Arrow to navigate
1. **Verify**: Similar announcements as NVDA above

#### Expected Screen Reader Behavior

- Icons **NOT** announced (should have `aria-hidden="true"`)
- Entity type labels announced clearly
- "Recommended" section heading announced
- "Other" section heading announced (if present)
- Selected state announced ("selected" or "not selected")

### Keyboard-Only Testing

**Unplug mouse** or **ignore mouse** and complete a full workflow:

1. Tab to EntityTypeSelector
1. Open with Enter/Space
1. Filter by typing
1. Navigate with Arrow keys
1. Select with Enter
1. Close with Escape

**Success Criteria**: All interactions possible without mouse

### Contrast Testing

**Use browser DevTools or contrast checker**:

1. Inspect icon color
1. **Verify**: Contrast ratio ≥3:1 against background (WCAG 2.2 AA for graphics)
1. Inspect selected item background
1. **Verify**: Contrast ratio ≥4.5:1 for text

---

## Performance Testing

### Filter Response Time

1. Open browser DevTools → Performance tab
1. Start recording
1. Open EntityTypeSelector dropdown
1. Type multiple characters quickly
1. Stop recording
1. **Analyze**: Filter updates should occur within 100ms of each keystroke

### Dropdown Render Time

1. Open DevTools → Performance tab
1. Record opening dropdown
1. **Target**: Initial render <200ms

### Icon Loading Time

1. Open DevTools → Network tab
1. Clear cache
1. Open dropdown
1. **Verify**: Icons load from bundle (no network requests)
1. **Target**: Icons appear within 50ms

---

## Common Issues & Troubleshooting

### Icons Not Appearing

- **Check**: Lucide React package installed (`pnpm list lucide-react`)
- **Check**: Import statement in component (`import { IconName } from 'lucide-react'`)
- **Check**: Entity type registry has icon property

### Spacing Too Tight

- **Check**: `py-2` class applied to list item buttons
- **Measure**: Inspect element, verify padding is 8px top/bottom

### Filter Not Working

- **Check**: `search` state variable updating correctly
- **Check**: Filter logic includes icon property or not (should filter by label/description only)

### Keyboard Navigation Broken

- **Check**: Radix UI Popover `onOpenAutoFocus` handler preserved
- **Check**: List items have `role="option"` attribute
- **Check**: Arrow key handlers not overridden

### Accessibility Violations

- **Run**: `pnpm test EntityTypeSelector.test.tsx --grep "axe"`
- **Review**: jest-axe output for specific violations
- **Fix**: Address ARIA attributes, contrast ratios, keyboard focus

---

## Success Criteria

✅ **Visual**: Icons, compact spacing, Recommended/Other sections, alphabetical ordering  
✅ **Functional**: Filter works, keyboard navigation intact, selection works  
✅ **Accessible**: Zero axe violations, screen reader compatible, keyboard-only operable  
✅ **Performance**: <100ms filter response, <200ms dropdown render  
✅ **Tests**: 90%+ coverage, all tests passing

---

## Next Steps

After local testing validation:

1. **Run full test suite**: `pnpm test`
1. **Run lint**: `pnpm lint`
1. **Build check**: `pnpm build`
1. **Commit changes**: Follow conventional commits format
1. **Create PR**: Reference spec ticket number (#010)

**Ready for Code Review** when all above criteria met.
