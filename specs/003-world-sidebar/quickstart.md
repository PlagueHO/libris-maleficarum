# Quickstart: World Sidebar Navigation

**Feature**: World Sidebar Navigation  
**Version**: 1.0.0  
**Last Updated**: 2026-01-13

## Overview

The **WorldSidebar** component provides hierarchical navigation for TTRPG world entities (continents, countries, characters, campaigns, etc.) with:

- World selection dropdown
- Lazy-loaded entity tree with expand/collapse
- sessionStorage caching (5-minute TTL)
- Context menu actions (add, edit, delete, move)
- Entity selection (drives main panel display)
- Full keyboard navigation + screen reader support

This guide shows developers how to integrate and use WorldSidebar in their React applications.

---

## Installation

### Prerequisites

- **React**: 19+
- **Redux Toolkit**: 2.11+
- **Shadcn/UI**: Installed with Dialog, ContextMenu, Select components
- **Lucide React**: 0.562+ (icons)
- **TypeScript**: 5.9+

All dependencies are already present in `package.json` (see plan.md Appendix).

### Component Location

```
libris-maleficarum-app/src/components/WorldSidebar/
├── WorldSidebar.tsx          # Container component
├── WorldSelector.tsx         # World dropdown
├── EntityTree.tsx            # Recursive tree view
├── EntityTreeNode.tsx        # Individual tree node
├── EntityContextMenu.tsx     # Right-click menu
├── WorldFormModal.tsx        # World create/edit form
├── EntityFormModal.tsx       # Entity create/edit form
├── EmptyState.tsx            # "Create Your First World" prompt
└── index.ts                  # Barrel export
```

---

## Basic Usage

### 1. Import WorldSidebar

```tsx
import { WorldSidebar } from '@/components/WorldSidebar';
```

### 2. Add to App Layout

```tsx
// App.tsx
import { WorldSidebar } from '@/components/WorldSidebar';
import { MainPanel } from '@/components/MainPanel';

function App() {
  return (
    <div className="flex h-screen">
      {/* WorldSidebar on the left */}
      <WorldSidebar className="w-80 border-r" />

      {/* MainPanel on the right */}
      <MainPanel className="flex-1" />
    </div>
  );
}
```

### 3. Ensure Redux Store is Configured

The WorldSidebar requires `worldSidebarSlice` to be registered in the Redux store:

```tsx
// store/store.ts
import { configureStore } from '@reduxjs/toolkit';
import worldSidebarReducer from './worldSidebarSlice';

export const store = configureStore({
  reducer: {
    // ...other slices
    worldSidebar: worldSidebarReducer,
  },
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
```

---

## User Interactions

### Empty State (First-Time User)

When a user has no worlds:

1. **WorldSidebar** detects empty world list via `useGetWorldsQuery()`
1. **EmptyState** component displays: "Create Your First World" CTA
1. User clicks button → **WorldFormModal** opens
1. User fills name + description → submits form
1. **worldApi.createWorld** mutation creates world
1. **WorldSelector** updates to show new world
1. **EntityTree** displays empty hierarchy (root level)

### World Selection

User selects different world from dropdown:

1. User clicks **WorldSelector** dropdown
1. Dropdown lists all worlds alphabetically with current highlighted
1. User selects world → `dispatch(setSelectedWorld(worldId))`
1. **EntityTree** loads root entities for new world (cache-first)
1. Previous world's expanded state saved to sessionStorage
1. New world's expanded state restored from cache (if exists)

### Entity Hierarchy Navigation

User expands entity to see children:

1. User clicks chevron icon on **EntityTreeNode**
1. Check cache: `sessionCache.get('sidebar_hierarchy_{worldId}_{parentId}')`
1. **If cache hit**: Render children instantly (<100ms per FR-002)
1. **If cache miss**: Fetch via `worldEntityApi.getEntitiesByParent(parentId)`
1. Cache response: `sessionCache.set(key, children, 300000)` (5 min TTL)
1. Update Redux: `dispatch(toggleNodeExpanded(parentId))`
1. Render children with indentation and connecting lines

### Entity Selection (Main Panel Integration)

User clicks entity to display details:

1. User clicks **EntityTreeNode** name/icon
1. `dispatch(setSelectedEntity(entityId))`
1. **EntityTreeNode** adds selection highlight styling
1. **MainPanel** component listens to `selectedEntityId` from Redux:

   ```tsx
   // MainPanel.tsx
   const selectedEntityId = useSelector((state: RootState) => 
     state.worldSidebar.selectedEntityId
   );

   useEffect(() => {
     if (selectedEntityId) {
       // Fetch entity details and render
     }
   }, [selectedEntityId]);
   ```

### Entity Creation (Multi-Entry Point)

**Option 1: Inline "+" Button** (hover on entity)

1. User hovers over **EntityTreeNode**
1. Inline "+" button appears (progressive disclosure)
1. User clicks → `dispatch(openEntityForm({ parentId: entityId }))`
1. **EntityFormModal** opens with context-aware type suggestions
1. User selects type, enters name/description → submits
1. **worldEntityApi.createEntity** mutation creates entity
1. Cache invalidated: `invalidate('sidebar_hierarchy_{worldId}_{parentId}')`
1. Parent node refreshes to show new child

**Option 2: Context Menu** (right-click entity)

1. User right-clicks **EntityTreeNode**
1. **EntityContextMenu** appears with actions: Add Child, Edit, Delete, Move
1. User clicks "Add Child Entity" → same flow as Option 1

**Option 3: "Add Root Entity" Button** (world level)

1. User clicks button at bottom of **WorldSidebar**
1. `dispatch(openEntityForm({ parentId: selectedWorldId }))`
1. **EntityFormModal** opens with root-level type suggestions (Continent, Campaign, Character)
1. Same creation flow

### World Metadata Editing

User edits world name/description:

1. User clicks edit icon next to world name in **WorldSelector**
1. `dispatch(openWorldForm(worldId))`
1. **WorldFormModal** opens in edit mode (fields pre-populated)
1. User modifies name/description → submits
1. **worldApi.updateWorld** mutation updates world
1. **WorldSelector** updates optimistically (no page refresh required)

---

## API Integration

### RTK Query Endpoints

**worldApi.ts** (existing):

```tsx
export const worldApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getWorlds: builder.query<WorldListResponse, void>({ ... }),
    createWorld: builder.mutation<World, CreateWorldRequest>({ ... }),
    updateWorld: builder.mutation<World, UpdateWorldRequest>({ ... }),
    // ...
  }),
});
```

**worldEntityApi.ts** (new):

```tsx
export const worldEntityApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getEntitiesByParent: builder.query<WorldEntity[], string>({
      query: (parentId) => ({
        url: `/worlds/${worldId}/entities`,
        params: { parentId },
      }),
      providesTags: (result, error, parentId) => [
        { type: 'WorldEntity', id: parentId },
      ],
    }),
    createEntity: builder.mutation<WorldEntity, CreateWorldEntityRequest>({
      invalidatesTags: (result, error, arg) => [
        { type: 'WorldEntity', id: arg.parentId },
      ],
    }),
    // ...
  }),
});
```

---

## Styling & Theming

### CSS Modules

Each component has a co-located CSS Module:

```tsx
// EntityTreeNode.tsx
import styles from './EntityTreeNode.module.css';

<div className={styles.treeNode}>
  <button className={styles.chevron}>...</button>
  <span className={styles.name}>{entity.name}</span>
</div>
```

### Tailwind Classes

Components use utility-first Tailwind classes for layout:

```tsx
<div className="flex items-center gap-2 p-2 hover:bg-accent">
  {/* ... */}
</div>
```

### Dark Theme Support

All components support dark theme via Tailwind's `dark:` variants:

```css
.treeNode {
  @apply bg-background text-foreground;
  @apply dark:bg-gray-800 dark:text-gray-100;
}
```

---

## Accessibility

### Keyboard Navigation

- **Tab**: Focus next/previous tree node
- **Arrow Up/Down**: Navigate between siblings
- **Arrow Right**: Expand node (if collapsed)
- **Arrow Left**: Collapse node (if expanded) or move to parent
- **Enter**/**Space**: Select entity
- **Shift+F10**: Open context menu

### ARIA Attributes

**EntityTree**:

```tsx
<div role="tree" aria-label="World entity hierarchy">
  {/* ... */}
</div>
```

**EntityTreeNode**:

```tsx
<div
  role="treeitem"
  aria-expanded={isExpanded}
  aria-level={depth}
  aria-selected={isSelected}
  aria-label={entity.name}
>
  {/* ... */}
</div>
```

### Screen Reader Announcements

- Expanded node: "Expanded {entityName}"
- Collapsed node: "Collapsed {entityName}"
- Selected entity: "Selected {entityName}, {entityType}"

---

## Testing

### Component Tests

```tsx
// EntityTreeNode.test.tsx
import { render, screen } from '@testing-library/react';
import { EntityTreeNode } from './EntityTreeNode';

test('renders entity name', () => {
  const entity = { id: '123', name: 'Faerûn', entityType: 'Continent', ... };
  render(<EntityTreeNode entity={entity} />);
  expect(screen.getByText('Faerûn')).toBeInTheDocument();
});
```

### Accessibility Tests

```tsx
import { axe } from 'jest-axe';

test('has no accessibility violations', async () => {
  const { container } = render(<WorldSidebar />);
  const results = await axe(container);
  expect(results).toHaveNoViolations();
});
```

### Integration Tests with MSW

```tsx
// hierarchyCaching.test.tsx
import { server } from '../mocks/server';
import { http, HttpResponse } from 'msw';

test('caches entity children and restores on re-expand', async () => {
  // Mock API response
  server.use(
    http.get('/api/v1/worlds/:worldId/entities', () => {
      return HttpResponse.json({ data: [/* entities */] });
    })
  );

  // First expansion → API call
  const { rerender } = render(<EntityTree />);
  await userEvent.click(screen.getByLabelText('Expand'));
  expect(await screen.findByText('Child Entity')).toBeInTheDocument();

  // Collapse and re-expand → no API call (cached)
  await userEvent.click(screen.getByLabelText('Collapse'));
  await userEvent.click(screen.getByLabelText('Expand'));
  expect(screen.getByText('Child Entity')).toBeInTheDocument();
});
```

---

## Performance Tips

### Cache Hit Rate Optimization

Monitor cache hits in development:

```tsx
// sessionCache.ts
if (process.env.NODE_ENV === 'development') {
  console.log(`[Cache] ${cached ? 'HIT' : 'MISS'} for key: ${key}`);
}
```

Target: **>80% cache hit rate** (per FR-008)

### Virtual Scrolling (Optional)

For worlds with >500 entities, add virtual scrolling:

```tsx
import { useVirtualizer } from '@tanstack/react-virtual';

// In EntityTree.tsx
const virtualizer = useVirtualizer({
  count: entities.length,
  getScrollElement: () => scrollRef.current,
  estimateSize: () => 40, // px per item
});
```

### Lazy Loading Threshold

Default: Load children only when expanded  
Optimization: Prefetch children of visible nodes (future enhancement)

---

## Troubleshooting

### Cache Not Working

**Symptom**: Entities refetched on every expansion

**Causes**:

1. sessionStorage disabled (private browsing mode)
1. TTL expired (>5 minutes since last cache)
1. Cache invalidation triggered unexpectedly

**Fix**: Check browser console for cache logs, verify sessionStorage availability

### Entity Tree Not Rendering

**Symptom**: Blank sidebar after world selection

**Causes**:

1. Redux slice not registered in store
1. API endpoint returning empty array
1. Frontend filtering excluding all entities

**Fix**: Check Redux DevTools, Network tab for API response, verify `isDeleted=false` filter

### Keyboard Navigation Not Working

**Symptom**: Arrow keys don't navigate tree

**Causes**:

1. ARIA roles missing (`role="tree"`, `role="treeitem"`)
1. `tabindex` not set correctly
1. Event handlers not attached

**Fix**: Run `jest-axe` tests, verify `onKeyDown` handlers in EntityTree

---

## Migration Guide

### From SidePanel to WorldSidebar

If replacing existing sidebar:

1. **Import**: Replace `import { SidePanel }` with `import { WorldSidebar }`
1. **Props**: WorldSidebar has no required props (uses Redux)
1. **Redux**: Register `worldSidebarSlice` in store
1. **Cleanup**: Remove old SidePanel state management

---

## Additional Resources

- **Design Spec**: [spec.md](../spec.md)
- **Implementation Plan**: [plan.md](../plan.md)
- **Data Model**: [data-model.md](../data-model.md)
- **API Contract**: [contracts/worldEntity.openapi.yaml](../contracts/worldEntity.openapi.yaml)
- **Research**: [research.md](../research.md)

---

**Version**: 1.0.0  
**Author**: Libris Maleficarum Development Team  
**License**: MIT
