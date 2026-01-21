# Data Model: Component Inventory

**Phase**: 1 (Design & Contracts)  
**Date**: 2026-01-21

## Overview

This document catalogs all components requiring migration from CSS Modules to Tailwind CSS, including complexity assessment, dependencies, and migration priority.

---

## Component Migration Matrix

| Component | CSS Modules | Complexity | Dependencies | Priority | Effort |
|-----------|-------------|------------|--------------|----------|--------|
| **WorldSidebar/EmptyState** | EmptyState.module.css | Simple | None (leaf) | P1 | 1h |
| **WorldSidebar/WorldSelector** | WorldSelector.module.css | Medium | Select (ui) | P1 | 2h |
| **WorldSidebar/EntityTreeNode** | EntityTreeNode.module.css | Medium | ContextMenu (ui) | P2 | 2h |
| **WorldSidebar/EntityTree** | EntityTree.module.css | Medium | EntityTreeNode | P2 | 2h |
| **WorldSidebar/WorldSidebar** | WorldSidebar.module.css | Complex | All above | P3 | 3h |
| **MainPanel/DeleteConfirmationModal** | DeleteConfirmationModal.module.css | Simple | Dialog (ui) | P1 | 1h |
| **MainPanel/WorldDetailForm** | WorldDetailForm.module.css | Complex | Input, Button, Select (ui) | P2 | 3h |
| **Shared components** | TBD (inspect) | Unknown | Various | P2 | 2h |
| **TopToolbar** | TBD (inspect) | Unknown | Button (ui) | P2 | 2h |
| **ChatPanel** | TBD (inspect) | Unknown | Various | P3 | 2h |

**Total Estimated Effort**: 20 hours (includes buffer for unknowns)

---

## Detailed Component Analysis

### 1. WorldSidebar/EmptyState

**Path**: `src/components/WorldSidebar/EmptyState.tsx`  
**CSS Module**: `EmptyState.module.css`  
**Complexity**: Simple  
**Dependencies**: None (pure presentational component)  
**Priority**: P1 (leaf component, no dependencies)  
**Effort**: 1 hour

**Current Styling Patterns**:

- Centered content (flex column, items center)
- Vertical spacing between elements
- Muted text color
- Simple icon/text layout

**Tailwind Approach**:

```tsx
<div className="flex flex-col items-center justify-center gap-4 p-8 text-muted-foreground">
  <Icon className="h-12 w-12" />
  <p className="text-sm">No worlds found</p>
</div>
```

**Test Updates**:

- Remove CSS Module import
- Use ARIA queries: `getByText('No worlds found')`

---

### 2. WorldSidebar/WorldSelector

**Path**: `src/components/WorldSidebar/WorldSelector.tsx`  
**CSS Module**: `WorldSelector.module.css`  
**Complexity**: Medium  
**Dependencies**: Shadcn/UI Select component  
**Priority**: P1 (used by parent components)  
**Effort**: 2 hours

**Current Styling Patterns**:

- Dropdown trigger styling
- Option list styling
- Selected state highlighting
- Responsive width

**Tailwind Approach**:

- Leverage Shadcn/UI `<Select>` component variants
- Custom container: `<div className="w-full px-4 py-2">`
- Use CVA for custom variants if needed

**Test Updates**:

- Remove CSS Module import
- Use `getByRole('combobox')` for select element

---

### 3. WorldSidebar/EntityTreeNode

**Path**: `src/components/WorldSidebar/EntityTreeNode.tsx`  
**CSS Module**: `EntityTreeNode.module.css`  
**Complexity**: Medium  
**Dependencies**: Shadcn/UI ContextMenu  
**Priority**: P2 (depends on EmptyState pattern being established)  
**Effort**: 2 hours

**Current Styling Patterns**:

- Tree node indentation (nested levels)
- Expand/collapse icon positioning
- Hover state for selection
- Context menu trigger area
- Selected/active state highlighting

**Tailwind Approach**:

```tsx
<div 
  className={cn(
    "flex items-center gap-2 px-2 py-1.5 rounded-md cursor-pointer",
    "hover:bg-accent hover:text-accent-foreground",
    "group",
    isSelected && "bg-accent text-accent-foreground"
  )}
  style={{ paddingLeft: `${level * 1.5}rem` }}
>
  <ChevronRight className={cn(
    "h-4 w-4 transition-transform",
    isExpanded && "rotate-90"
  )} />
  <span className="truncate">{name}</span>
</div>
```

**Note**: Indentation uses inline style for dynamic level calculation (acceptable exception to Tailwind-only rule).

**Test Updates**:

- Use `getByRole('treeitem')` for tree nodes
- Use `data-testid` for complex tree interactions

---

### 4. WorldSidebar/EntityTree

**Path**: `src/components/WorldSidebar/EntityTree.tsx`  
**CSS Module**: `EntityTree.module.css`  
**Complexity**: Medium  
**Dependencies**: EntityTreeNode component  
**Priority**: P2 (depends on EntityTreeNode)  
**Effort**: 2 hours

**Current Styling Patterns**:

- Scrollable container
- Tree list layout
- Loading states
- Empty state handling

**Tailwind Approach**:

```tsx
<ScrollArea className="h-full">
  <div className="flex flex-col gap-1 p-2" role="tree">
    {loading ? (
      <div className="flex items-center justify-center py-8">
        <Loader2 className="h-6 w-6 animate-spin" />
      </div>
    ) : items.length === 0 ? (
      <EmptyState />
    ) : (
      items.map(item => <EntityTreeNode key={item.id} {...item} />)
    )}
  </div>
</ScrollArea>
```

**Test Updates**:

- Use `getByRole('tree')` for container
- Remove CSS Module class assertions

---

### 5. WorldSidebar/WorldSidebar

**Path**: `src/components/WorldSidebar/WorldSidebar.tsx`  
**CSS Module**: `WorldSidebar.module.css`  
**Complexity**: Complex  
**Dependencies**: EmptyState, WorldSelector, EntityTree  
**Priority**: P3 (top-level composite, depends on all child components)  
**Effort**: 3 hours

**Current Styling Patterns**:

- Sidebar layout (fixed width, full height)
- Header with selector
- Scrollable content area
- Footer with actions
- Responsive collapse/expand

**Tailwind Approach**:

```tsx
<aside className="flex flex-col w-64 h-full border-r border-border bg-background">
  <div className="flex items-center justify-between p-4 border-b border-border">
    <h2 className="text-lg font-semibold">Worlds</h2>
    <Button variant="ghost" size="icon">
      <Plus className="h-4 w-4" />
    </Button>
  </div>
  
  <div className="p-2">
    <WorldSelector />
  </div>
  
  <div className="flex-1 overflow-hidden">
    <EntityTree />
  </div>
  
  <div className="p-4 border-t border-border">
    {/* Footer actions */}
  </div>
</aside>
```

**Test Updates**:

- Use `getByRole('complementary')` or `data-testid="world-sidebar"`
- Accessibility: ensure proper landmark roles

---

### 6. MainPanel/DeleteConfirmationModal

**Path**: `src/components/MainPanel/DeleteConfirmationModal.tsx`  
**CSS Module**: `DeleteConfirmationModal.module.css`  
**Complexity**: Simple  
**Dependencies**: Shadcn/UI Dialog, Button  
**Priority**: P1 (simple, reusable pattern)  
**Effort**: 1 hour

**Current Styling Patterns**:

- Modal overlay and content centering
- Button group layout (Cancel/Delete)
- Warning icon styling
- Destructive action emphasis

**Tailwind Approach**:

```tsx
<Dialog>
  <DialogContent className="sm:max-w-md">
    <DialogHeader>
      <DialogTitle className="flex items-center gap-2">
        <AlertTriangle className="h-5 w-5 text-destructive" />
        Confirm Deletion
      </DialogTitle>
      <DialogDescription>
        This action cannot be undone.
      </DialogDescription>
    </DialogHeader>
    
    <DialogFooter className="flex gap-2 sm:gap-0">
      <Button variant="outline" onClick={onCancel}>
        Cancel
      </Button>
      <Button variant="destructive" onClick={onConfirm}>
        Delete
      </Button>
    </DialogFooter>
  </DialogContent>
</Dialog>
```

**Test Updates**:

- Use `getByRole('dialog')` and `getByRole('button', { name: /delete/i })`

---

### 7. MainPanel/WorldDetailForm

**Path**: `src/components/MainPanel/WorldDetailForm.tsx`  
**CSS Module**: `WorldDetailForm.module.css`  
**Complexity**: Complex  
**Dependencies**: Input, Button, Select, Textarea (all from ui/)  
**Priority**: P2 (form-heavy, needs careful validation state handling)  
**Effort**: 3 hours

**Current Styling Patterns**:

- Two-column form layout
- Input field groups (label + input + error)
- Validation error styling
- Form actions footer
- Responsive stacking

**Tailwind Approach**:

```tsx
<form className="space-y-6" onSubmit={handleSubmit}>
  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
    <div className="space-y-2">
      <label htmlFor="name" className="text-sm font-medium">
        World Name
      </label>
      <Input 
        id="name" 
        aria-invalid={errors.name ? 'true' : 'false'}
        className={cn(errors.name && "border-destructive")}
      />
      {errors.name && (
        <p className="text-sm text-destructive">{errors.name}</p>
      )}
    </div>
    
    {/* More fields... */}
  </div>
  
  <div className="flex justify-end gap-2 pt-4 border-t border-border">
    <Button type="button" variant="outline" onClick={onCancel}>
      Cancel
    </Button>
    <Button type="submit">
      Save World
    </Button>
  </div>
</form>
```

**Test Updates**:

- Use `getByLabelText('World Name')` for form fields
- Validation: `expect(getByText(/error message/i)).toBeInTheDocument()`

---

### 8. Shared Components

**Path**: `src/components/shared/`  
**CSS Modules**: TBD (need inspection)  
**Complexity**: Unknown  
**Dependencies**: Various  
**Priority**: P2  
**Effort**: 2 hours (estimate)

**Action Required**: Inspect shared/ directory for components and CSS Modules.

**Likely candidates**:

- Loading spinners
- Error boundaries
- Common form elements
- Toast notifications

---

### 9. TopToolbar

**Path**: `src/components/TopToolbar/`  
**CSS Modules**: TBD (need inspection)  
**Complexity**: Unknown  
**Dependencies**: Button (ui)  
**Priority**: P2  
**Effort**: 2 hours (estimate)

**Expected Patterns**:

- Horizontal toolbar layout
- Button groups
- Title/breadcrumb area
- User menu/actions

**Tailwind Approach** (likely):

```tsx
<header className="flex items-center justify-between h-14 px-4 border-b border-border bg-background">
  <div className="flex items-center gap-4">
    <h1 className="text-xl font-semibold">Libris Maleficarum</h1>
  </div>
  
  <div className="flex items-center gap-2">
    {/* Action buttons */}
  </div>
</header>
```

---

### 10. ChatPanel

**Path**: `src/components/ChatPanel/`  
**CSS Modules**: TBD (need inspection)  
**Complexity**: Unknown  
**Dependencies**: Various (likely Input, Button, ScrollArea)  
**Priority**: P3  
**Effort**: 2 hours (estimate)

**Expected Patterns**:

- Chat message list (scrollable)
- Message bubbles (user vs AI styling)
- Input field at bottom
- Typing indicators

**Tailwind Approach** (likely):

```tsx
<div className="flex flex-col h-full">
  <ScrollArea className="flex-1 p-4">
    <div className="space-y-4">
      {messages.map(msg => (
        <div 
          key={msg.id}
          className={cn(
            "flex gap-2",
            msg.role === 'user' && "justify-end"
          )}
        >
          <div className={cn(
            "px-4 py-2 rounded-lg max-w-[80%]",
            msg.role === 'user' 
              ? "bg-primary text-primary-foreground" 
              : "bg-muted"
          )}>
            {msg.content}
          </div>
        </div>
      ))}
    </div>
  </ScrollArea>
  
  <div className="p-4 border-t border-border">
    <Input placeholder="Type a message..." />
  </div>
</div>
```

---

## Migration Order

**Phase 1**: Leaf components (P1)

1. EmptyState
1. DeleteConfirmationModal
1. WorldSelector

**Phase 2**: Mid-level components (P2)
4. EntityTreeNode
5. EntityTree
6. WorldDetailForm
7. Shared components (after inspection)
8. TopToolbar (after inspection)

**Phase 3**: Composite components (P3)
9. WorldSidebar (depends on 1, 3, 4, 5)
10. ChatPanel (after inspection)

---

## Success Validation Checklist

After migration:

- [ ] Zero `*.module.css` files remain: `git ls-files '*.module.css'` returns empty
- [ ] All components use Tailwind classes
- [ ] CVA used for all component variants
- [ ] All tests pass: `pnpm test`
- [ ] Accessibility tests pass: `pnpm test -- --grep "accessibility"`
- [ ] Screenshot tests pass: `pnpm playwright test`
- [ ] Build succeeds: `pnpm build`
- [ ] Dev server works: `pnpm dev`

---

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Unknown components in shared/, TopToolbar, ChatPanel | High (effort estimate) | Inspect directories before starting migration |
| Complex nested CSS selectors | Medium | Use @apply directive as escape hatch |
| Dynamic styling (calculated values) | Low | Use inline styles for truly dynamic values |
| Test breakage from class removal | Medium | Update to ARIA queries before migration |
| Visual regression not caught | High | Capture comprehensive screenshot baselines |

---

## Next: Quickstart Guide

See `quickstart.md` for step-by-step migration instructions and code patterns.
