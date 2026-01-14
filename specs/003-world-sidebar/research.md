# Phase 0: Research & Technology Decisions

**Feature**: World Sidebar Navigation  
**Date**: 2026-01-13  
**Status**: Completed

## Research Questions

This document consolidates research findings for all technical unknowns identified during specification and planning. Each decision includes rationale, alternatives considered, and references to best practices.

---

## 1. sessionStorage vs localStorage for Hierarchy Caching

**Decision**: Use **sessionStorage** for all hierarchy cache data

**Rationale**:

- **Session Isolation**: Each browser tab has independent cache, preventing stale data issues when user opens multiple tabs with different worlds
- **Automatic Cleanup**: Cache cleared on tab close, eliminating need for manual cleanup logic
- **Privacy**: Sensitive world data not persisted across sessions
- **Size Sufficient**: 5MB sessionStorage limit accommodates ~500 worlds @ ~10KB cached hierarchy each

**Alternatives Considered**:

- ❌ **localStorage**: Persists across sessions → stale data risk, privacy concerns, requires manual expiry
- ❌ **IndexedDB**: Overcomplicated for simple key-value cache; async API adds complexity
- ❌ **Redux-only**: Lost on page refresh; sessionStorage provides resilience

**Implementation Pattern**:

```typescript
// Cache key format
const cacheKey = `sidebar_hierarchy_${worldId}`;
const expandedKey = `sidebar_expanded_${worldId}`;

// TTL structure
interface CacheEntry<T> {
  data: T;
  timestamp: number;
  ttl: number; // milliseconds (300000 = 5 minutes)
}

// Cache utility
function getCachedHierarchy(worldId: string): WorldEntity[] | null {
  const entry = sessionStorage.getItem(`sidebar_hierarchy_${worldId}`);
  if (!entry) return null;
  
  const parsed: CacheEntry<WorldEntity[]> = JSON.parse(entry);
  if (Date.now() - parsed.timestamp > parsed.ttl) {
    sessionStorage.removeItem(`sidebar_hierarchy_${worldId}`);
    return null;
  }
  
  return parsed.data;
}
```

**References**:

- MDN Web Docs: [Window.sessionStorage](https://developer.mozilla.org/en-US/docs/Web/API/Window/sessionStorage)
- Web Storage Best Practices: Session vs Local Storage

---

## 2. Modal Dialogs vs Inline Forms for World/Entity Creation

**Decision**: Use **modal dialogs** (Shadcn Dialog component) for all create/edit operations

**Rationale**:

- **Focus Lock**: Prevents accidental navigation away from unsaved form data
- **Visual Hierarchy**: Clear separation between navigation (sidebar) and data entry (modal)
- **Existing Pattern**: Project already uses Shadcn/UI Dialog component (consistent UX)
- **Accessibility**: Built-in ARIA roles, keyboard traps, focus management
- **Mobile-Friendly**: Overlays work well on small screens vs. inline expansion

**Alternatives Considered**:

- ❌ **Inline Expansion**: Pushes hierarchy down → disorienting scroll jumps, difficult mobile UX
- ❌ **Slide-out Panel**: Requires additional state management, conflicts with potential right-panel usage
- ❌ **Full-Page Navigation**: Loses sidebar context, requires back-navigation

**Implementation Pattern**:

```tsx
// WorldFormModal.tsx
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';

export function WorldFormModal({ isOpen, onClose, worldId }: WorldFormModalProps) {
  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{worldId ? 'Edit World' : 'Create World'}</DialogTitle>
        </DialogHeader>
        <WorldForm worldId={worldId} onSuccess={onClose} />
      </DialogContent>
    </Dialog>
  );
}
```

**References**:

- Radix UI Dialog: [Accessibility Features](https://www.radix-ui.com/primitives/docs/components/dialog)
- ARIA Authoring Practices: Modal Dialog Pattern

---

## 3. Entity Click Behavior: Master-Detail Pattern

**Decision**: Clicking entity **selects it and displays details in main panel**

**Rationale**:

- **Standard Master-Detail UX**: Sidebar = navigation/master list, main panel = detail view (see: Gmail, VS Code, Slack)
- **Persistent Context**: Sidebar remains visible for quick entity switching
- **Scalable**: Accommodates complex entity details (properties, assets, history) in main panel
- **Multi-Panel Ready**: Future feature can add right panel for additional context without conflicts

**Alternatives Considered**:

- ❌ **Visual Selection Only**: Useless without action → confuses users
- ❌ **Modal Dialog**: Blocks navigation, poor for browsing multiple entities
- ❌ **Full-Page Navigation**: Loses sidebar context, requires back button

**Implementation Pattern**:

```typescript
// Redux slice
interface WorldSidebarState {
  selectedWorldId: string | null;
  selectedEntityId: string | null; // 🔑 Drives main panel display
  expandedNodeIds: string[];
}

// EntityTreeNode.tsx
function handleEntityClick(entityId: string) {
  dispatch(setSelectedEntity(entityId)); // Main panel listens to this
}
```

**References**:

- Nielsen Norman Group: [Master-Detail Pattern](https://www.nngroup.com/articles/split-screen-layouts/)
- Material Design: Navigation Patterns

---

## 4. Context Menu Actions: Basic Operations

**Decision**: Provide **right-click context menu** with actions: Add Child, Edit, Delete, Move

**Rationale**:

- **Efficiency**: Power users can perform common actions without navigating to main panel
- **Discoverability**: Right-click is familiar pattern for tree operations (see: File Explorer, VS Code)
- **Scope Appropriate**: Basic actions suit sidebar context; complex operations remain in main panel
- **Accessibility**: Can be triggered via keyboard (Shift+F10 or dedicated button)

**Actions Included**:

1. **Add Child Entity**: Opens EntityFormModal with parent pre-selected
1. **Edit Entity**: Opens EntityFormModal with entity data pre-populated
1. **Delete Entity**: Confirmation dialog → soft delete API call
1. **Move Entity**: Opens MoveTo dialog with target parent selector

**Implementation Pattern**:

```tsx
// EntityContextMenu.tsx
import { ContextMenu, ContextMenuContent, ContextMenuItem } from '@/components/ui/context-menu';

<ContextMenu>
  <ContextMenuTrigger>{children}</ContextMenuTrigger>
  <ContextMenuContent>
    <ContextMenuItem onClick={() => handleAddChild(entityId)}>
      <Plus className="mr-2 h-4 w-4" />
      Add Child Entity
    </ContextMenuItem>
    <ContextMenuItem onClick={() => handleEdit(entityId)}>
      <Pencil className="mr-2 h-4 w-4" />
      Edit
    </ContextMenuItem>
    <ContextMenuItem onClick={() => handleMove(entityId)}>
      <Move className="mr-2 h-4 w-4" />
      Move To...
    </ContextMenuItem>
    <ContextMenuItem onClick={() => handleDelete(entityId)} className="text-red-600">
      <Trash className="mr-2 h-4 w-4" />
      Delete
    </ContextMenuItem>
  </ContextMenuContent>
</ContextMenu>
```

**References**:

- Radix UI ContextMenu: [Documentation](https://www.radix-ui.com/primitives/docs/components/context-menu)
- WCAG 2.2: Keyboard Accessibility

---

## 5. Entity Creation UI: Multi-Entry Point Strategy

**Decision**: Combine **three entry points** for creating entities:

1. Inline "+" button (hover on entity) → Add child
1. Context menu "Add Child Entity" → Add child
1. "Add Root Entity" button (world level) → Add to world root

**Rationale**:

- **Progressive Disclosure**: Inline "+" visible only on hover → clean UI when idle
- **Redundancy**: Multiple paths accommodate different user preferences (mouse vs keyboard, discoverable vs efficient)
- **Context-Aware**: Button appears where needed (entity hover, world level), no global "Add" button confusion

**Implementation Pattern**:

```tsx
// EntityTreeNode.tsx
<div className="group flex items-center" onMouseEnter={() => setHovered(true)}>
  <button onClick={handleExpand}>
    {hasChildren ? <ChevronRight /> : <span className="w-4" />}
  </button>
  <span>{entity.name}</span>
  
  {/* Inline + button (visible on hover) */}
  {hovered && (
    <button 
      className="ml-auto opacity-0 group-hover:opacity-100"
      onClick={() => openEntityModal(entity.id)}
    >
      <Plus className="h-4 w-4" />
    </button>
  )}
</div>

// WorldSidebar.tsx (root level button)
<div className="p-2 border-t">
  <Button variant="ghost" onClick={() => openEntityModal(selectedWorldId)}>
    <Plus className="mr-2 h-4 w-4" />
    Add Root Entity
  </Button>
</div>
```

**References**:

- Ant Design Tree Component: Inline Actions Pattern
- Fluent UI Tree: Progressive Disclosure

---

## 6. Entity Type Selection: Context-Aware Suggestions

**Decision**: Modal displays **entity type dropdown with intelligent suggestions** based on parent type

**Rationale**:

- **Guided Experience**: New users see common child types first (e.g., Continent → Country/Region)
- **Flexibility Preserved**: Full type list available via "Show All" or scrolling
- **Domain Knowledge Encoded**: Suggestions reflect TTRPG world-building patterns

**Suggestion Rules** *(Addresses FR-032 - Context-aware type suggestions with deterministic algorithm)*:

```typescript
const entityTypeSuggestions: Record<WorldEntityType, WorldEntityType[]> = {
  // Geographic hierarchy
  "Continent": ["Country", "Region"],
  "Country": ["Region", "City", "Location"],
  "Region": ["City", "Location"],
  "City": ["Location", "Character", "Organization"],
  "Location": ["Character", "Item", "Event"],
  
  // Character/organizational hierarchy
  "Character": ["Item", "Event"],
  "Organization": ["Character", "Location", "Event"],
  
  // Campaign/narrative hierarchy
  "Campaign": ["Event", "Character", "Location", "Item"],
  "Event": ["Character", "Location", "Item"],
  
  // Items rarely have children
  "Item": []
};

// For root entities (parent = World), no suggestions—show all types alphabetically
```

**Implementation Pattern**:

```tsx
// EntityFormModal.tsx
<Select value={entityType} onValueChange={setEntityType}>
  <SelectTrigger>
    <SelectValue placeholder="Select entity type" />
  </SelectTrigger>
  <SelectContent>
    {/* Suggested types (based on parent) */}
    <SelectGroup>
      <SelectLabel>Suggested</SelectLabel>
      {suggestedTypes.map(type => (
        <SelectItem key={type} value={type}>
          {getEntityIcon(type)} {type}
        </SelectItem>
      ))}
    </SelectGroup>
    
    {/* All types */}
    <SelectSeparator />
    <SelectGroup>
      <SelectLabel>All Types</SelectLabel>
      {allTypes.map(type => (
        <SelectItem key={type} value={type}>
          {getEntityIcon(type)} {type}
        </SelectItem>
      ))}
    </SelectGroup>
  </SelectContent>
</Select>
```

**References**:

- Shadcn/UI Select: [Grouped Options](https://ui.shadcn.com/docs/components/select)
- UX Pattern: Contextual Suggestions

---

## 7. Accessibility: Keyboard Navigation & Screen Readers

**Decision**: Implement full keyboard navigation following **ARIA Tree View Pattern**

**Keyboard Shortcuts**:

- `Tab`: Focus next/previous tree node
- `Arrow Up/Down`: Navigate between siblings
- `Arrow Right`: Expand node (if collapsed)
- `Arrow Left`: Collapse node (if expanded) or move to parent
- `Enter`/`Space`: Select entity (display in main panel)
- `Shift+F10`: Open context menu
- `Home/End`: First/last node at current level

**ARIA Roles**:

```tsx
<div role="tree" aria-label="World entity hierarchy">
  <div
    role="treeitem"
    aria-expanded={isExpanded}
    aria-selected={isSelected}
    aria-level={depth}
    tabIndex={isSelected ? 0 : -1}
  >
    {entity.name}
  </div>
</div>
```

**Screen Reader Announcements**:

- "Continent, North America, level 1, collapsed, 3 of 5"
- "Country, United States, level 2, expanded, has 10 children"

**Implementation**:

- `EntityTree.tsx`: Manages focus and keyboard event handlers
- `EntityTreeNode.tsx`: ARIA attributes and announcements
- `WorldSidebar.test.tsx`: jest-axe validation

**References**:

- WAI-ARIA Authoring Practices: [Tree View](https://www.w3.org/WAI/ARIA/apg/patterns/treeview/)
- React ARIA: useTreeState

---

## 8. Caching Strategy: TTL + Invalidation

**Decision**: **5-minute TTL** with **mutation-based invalidation**

**TTL Rationale**:

- 5 minutes balances freshness (typical session activity) with performance (reduces API calls)
- Long enough for multi-branch exploration, short enough to catch external changes (AI agents, other tabs)

**Invalidation Triggers**:

```typescript
// RTK Query cache invalidation
worldEntityApi.endpoints.createEntity.onQueryStarted(async (arg, { dispatch, queryFulfilled }) => {
  await queryFulfilled;
  
  // Invalidate parent's children cache
  dispatch(worldEntityApi.util.invalidateTags([
    { type: 'WorldEntity', id: arg.parentId },
  ]));
  
  // Clear sessionStorage cache
  sessionStorage.removeItem(`sidebar_hierarchy_${arg.worldId}`);
});
```

**Cache Miss Behavior**:

- Check sessionStorage → if expired/missing, fetch from API
- Show skeleton loader during fetch
- Cache successful response with new TTL

**References**:

- RTK Query: [Cache Invalidation](https://redux-toolkit.js.org/rtk-query/usage/automated-refetching)
- HTTP Caching Best Practices

---

## Summary of Technology Decisions

| Question | Decision | Key Rationale |
|----------|----------|---------------|
| Cache Storage | sessionStorage | Session isolation, auto-cleanup |
| Form UI | Modal dialogs | Focus lock, consistent pattern |
| Entity Click | Select + main panel | Master-detail UX |
| Context Menu | Yes (4 actions) | Power user efficiency |
| Creation UI | Multi-entry point | Redundancy for discovery + efficiency |
| Type Selection | Context-aware suggestions | Guided but flexible |
| Accessibility | Full keyboard nav | WCAG 2.2 AA compliance |
| Cache TTL | 5 minutes + invalidation | Balance freshness & performance |

---

**Phase 0 Status**: ✅ **COMPLETE** - All technical unknowns resolved, ready for Phase 1 design.
