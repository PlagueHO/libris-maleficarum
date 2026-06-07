# Frontend Structure & State Research — Active Search Box Integration

Status: Complete
Date: 2026-06-07
Scope: libris-maleficarum-app/ (React 19 + TS + Vite + Redux Toolkit + RTK Query + Shadcn/Radix)

## Goal

Map how to add a top-of-app Active Search box whose results, when selected:

1. Open a World Entity in the MainPanel (view mode, with hover→edit affordance), and
2. Expand collapsed ancestor nodes in the WorldSidebar hierarchy so the entity is visible.

---

## 1. App layout / region structure & where the search box goes

File: libris-maleficarum-app/src/App.tsx

- Top-level layout is a vertical flex column: `TopToolbar` (header) on top, then a horizontal flex row containing `WorldSidebar` (left), `MainPanel`/`SettingsPage` (center), `ChatPanel` (right). See App.tsx lines 178-205.
- Layout shell (App.tsx:182-204):

```tsx
<div className="h-screen flex flex-col bg-background text-foreground">
  <TopToolbar onOpenSettings={() => setSettingsOpen(true)} />
  <AuthGuard>
    <WorldProvider key={selectedWorldId || 'no-world-selected'} initialWorldId={selectedWorldId || ''} initialWorldName="">
      <div className="flex-1 flex overflow-hidden">
        <WorldSidebar optimisticallyDeletedIds={optimisticallyDeletedIds} />
        {settingsOpen ? <SettingsPage onClose={...} /> : <MainPanel />}
        <ChatPanel />
      </div>
      <DeleteConfirmationModal />
    </WorldProvider>
  </AuthGuard>
</div>
```

- Current world selection lives in Redux: `const selectedWorldId = useAppSelector(selectSelectedWorldId);` (App.tsx:24).

RECOMMENDED PLACEMENT: Inside `TopToolbar.tsx`, in the center of the header bar (between the app title button and the right-aligned action cluster). The TopToolbar header is `h-14` with a flex row (TopToolbar.tsx:23-49). Add the search box in the open space before the `ml-auto` action group. It must be disabled / hidden when no world is selected (search is world-scoped). The TopToolbar currently does NOT read `selectedWorldId`, so add that selector if gating is needed.

---

## 2. TopToolbar — full current contents

File: libris-maleficarum-app/src/components/TopToolbar/TopToolbar.tsx (entire file)

```tsx
import { useState } from 'react';
import { Menu } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { useDispatch } from 'react-redux';
import { resetToHome } from '@/store/worldSidebarSlice';
import { NotificationBell, NotificationCenter } from '@/components/NotificationCenter';
import { ThemeToggle } from '@/components/shared/ThemeToggle';
import { UserMenu } from '@/components/UserMenu';
import appIcon from '@/assets/libris-maleficarum-icon.png';

interface TopToolbarProps {
  onOpenSettings: () => void;
}

export function TopToolbar({ onOpenSettings }: TopToolbarProps) {
  const dispatch = useDispatch();
  const [notificationCenterOpen, setNotificationCenterOpen] = useState(false);

  return (
    <>
      <header data-testid="top-toolbar" className="border-b border-border bg-card">
        <div className="flex h-14 items-center px-4 gap-2">
          <Button variant="ghost" size="icon" onClick={() => dispatch(resetToHome())} aria-label="Go to home">
            <Menu className="h-5 w-5" />
          </Button>
          <Separator orientation="vertical" className="h-6" />
          <button type="button" className="flex items-center gap-2 ..." onClick={() => dispatch(resetToHome())}>
            <img src={appIcon} alt="" className="h-5 w-5 rounded-sm" aria-hidden="true" />
            <h1 className="text-lg font-semibold">Libris Maleficarum</h1>
          </button>
          {/* <-- INSERT SEARCH BOX HERE (before ml-auto cluster) --> */}
          <div className="ml-auto flex items-center gap-2">
            <ThemeToggle />
            <NotificationBell onClick={() => setNotificationCenterOpen(true)} />
            <UserMenu onOpenSettings={onOpenSettings} />
          </div>
        </div>
      </header>
      <NotificationCenter open={notificationCenterOpen} onOpenChange={setNotificationCenterOpen} />
    </>
  );
}
```

- Props: `{ onOpenSettings: () => void }`. Uses `useDispatch` directly (not `useAppDispatch`). Uses `resetToHome` action.

---

## 3. WorldSidebar hierarchy — render + expand/collapse + select mechanism

Files:
- libris-maleficarum-app/src/components/WorldSidebar/WorldSidebar.tsx
- libris-maleficarum-app/src/components/WorldSidebar/EntityTree.tsx
- libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.tsx

### How the tree renders (LAZY, per-level)

- `EntityTree` (EntityTree.tsx:31) renders a recursive `EntityTreeLevel` starting at `parentId={null}` (EntityTree.tsx:155-162).
- Each `EntityTreeLevel` fetches ONLY its direct children via `useGetEntitiesByParentQuery({ worldId, parentId })` (EntityTree.tsx:185-194). It is lazy: children are fetched/rendered only when the parent is in `expandedNodeIds`.
- A node renders its children sub-level only when expanded (EntityTree.tsx:285-292 and 305-312):

```tsx
<EntityTreeNode key={entity.id} entity={entity} level={level}>
  {entity.hasChildren && expandedNodeIds.includes(entity.id) && (
    <EntityTreeLevel worldId={worldId} parentId={entity.id} level={level + 1} ... />
  )}
</EntityTreeNode>
```

IMPLICATION FOR SEARCH→REVEAL: Because the tree is lazy and per-level, to reveal a deep entity you MUST expand EVERY ancestor ID so each intermediate level mounts and fetches. The `WorldEntity.path` field (array of ancestor IDs root→parent) gives exactly this list.

### Expansion state (Redux)

- Controlled entirely by `expandedNodeIds: string[]` in the `worldSidebar` slice.
- Per-node expansion read via `selectIsNodeExpanded(entity.id)` (EntityTreeNode.tsx:48).
- Toggle on chevron click via `dispatch(toggleNodeExpanded(entity.id))` (EntityTreeNode.tsx:55-58).
- Relevant actions (worldSidebarSlice.ts): `toggleNodeExpanded`, `setExpandedNodes(string[])`, `expandNode(string)`, `collapseNode(string)`, `collapseAllNodes()`.

### Selection / open mechanism

- Node click → `dispatch(setSelectedEntity(entity.id))` (EntityTreeNode.tsx:60-62, handler `handleSelect`).
- `setSelectedEntity` (worldSidebarSlice.ts:117-124) sets `selectedEntityId` AND sets `mainPanelMode = 'viewing_entity'` (or `'empty'` if null). This single action is what opens an entity in the MainPanel in VIEW mode.

### Hover edit affordance (already exists in tree)

- Each node renders an Edit (Pencil) button that appears on hover: `opacity-0 group-hover:opacity-100` → `dispatch(openEntityFormEdit(entity.id))` (EntityTreeNode.tsx:84-90, 158-167). Same pattern for a "+" quick-create.

---

## 4. MainPanel — view vs edit open mechanism

File: libris-maleficarum-app/src/components/MainPanel/MainPanel.tsx

- MainPanel is a pure function of Redux state: it reads `selectMainPanelMode`, `selectSelectedEntityId`, `selectSelectedWorldId`, `selectEditingWorldId` (MainPanel.tsx:12-16).
- Mode routing (MainPanel.tsx:35-130):
  - `creating_world` → `WorldDetailForm mode="create"`
  - `editing_world` → `WorldDetailForm mode="edit"`
  - `creating_entity` / `editing_entity` → `WorldEntityForm` (exported as `EntityDetailForm`)
  - no `selectedEntityId` → Welcome card
  - else (`viewing_entity`) → fetch entity via `useGetWorldEntityByIdQuery({ worldId, entityId })` and render `EntityDetailReadOnlyView` (MainPanel.tsx:122-127).
- The read-only view receives `onEditClick={handleEditClick}` which does `dispatch(openEntityFormEdit(selectedEntityId))` (MainPanel.tsx:28-32). `EntityDetailReadOnlyView` renders an explicit "Edit" button (EntityDetailReadOnlyView.tsx:65-76).

OPEN ENTITY IN VIEW MODE = `dispatch(setSelectedEntity(entityId))`. The MainPanel then auto-fetches and renders the read-only view. No routing involved (no React Router; all state-driven).

---

## 5. Store + all slices

File: libris-maleficarum-app/src/store/store.ts

```ts
export const store = configureStore({
  reducer: {
    sidePanel: sidePanelSlice.reducer,        // { isExpanded: boolean } — actions: toggle, setExpanded
    worldSidebar: worldSidebarReducer,        // primary UI state (see below)
    notifications: notificationsReducer,      // async delete-operation notifications
    [api.reducerPath]: api.reducer,           // RTK Query cache
  },
  middleware: (gDM) => gDM().concat(api.middleware),
});
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
export const useAppDispatch: () => AppDispatch = useDispatch;
export const useAppSelector: TypedUseSelectorHook<RootState> = useSelector;
```

Slices:

- `sidePanel` (inline in store.ts): `{ isExpanded }` — not relevant to search.
- `worldSidebar` (worldSidebarSlice.ts): THE relevant slice. State shape:
  - `selectedWorldId: string | null` — current world
  - `selectedEntityId: string | null` — open/selected entity
  - `expandedNodeIds: string[]` — expanded tree nodes
  - `mainPanelMode: 'empty' | 'viewing_entity' | 'editing_world' | 'creating_world' | 'creating_entity' | 'editing_entity' | 'moving_entity'`
  - plus form/modal flags: `isWorldFormOpen`, `editingWorldId`, `editingEntityId`, `newEntityParentId`, `hasUnsavedChanges`, `deletingEntityId`, `deletingEntityName`, `showDeleteConfirmation`, `movingEntityId`, `creatingEntityParentId`.
  - Actions: `setSelectedWorld`, `setSelectedEntity`, `toggleNodeExpanded`, `setExpandedNodes`, `expandNode`, `collapseNode`, `collapseAllNodes`, `openWorldFormCreate`, `openWorldFormEdit`, `closeWorldForm`, `setUnsavedChanges`, `openEntityFormCreate`, `openEntityFormEdit`, `closeEntityForm`, `openDeleteConfirmation`, `closeDeleteConfirmation`, `openMoveEntity`, `closeMoveEntity`, `resetToHome`.
  - Selectors: `selectSelectedWorldId`, `selectSelectedEntityId`, `selectMainPanelMode`, `selectExpandedNodeIds`, `selectIsNodeExpanded(nodeId)`, `selectIsWorldFormOpen`, `selectEditingWorldId`, `selectEditingEntityId`, `selectNewEntityParentId`, `selectHasUnsavedChanges`.
- `notifications` (notificationsSlice.ts) + `notificationSelectors.ts`: async delete-operation polling; not relevant to search.

NOTE on `setSelectedWorld`: it CLEARS `selectedEntityId` and `expandedNodeIds`. So a cross-world search-navigate must dispatch `setSelectedWorld` FIRST, then expand, then select.

---

## 6. API client layer

- Data fetching = RTK Query (NOT React Query). Base slice: libris-maleficarum-app/src/services/api.ts.
  - Custom `axiosBaseQuery` wraps the configured axios instance (`apiClient`), supports `AbortSignal` cancellation, maps errors to `ProblemDetails` (api.ts:28-78).
  - `createApi({ reducerPath: 'api', tagTypes: ['World','WorldEntity','Character','Location','Organization','DeleteOperation'], keepUnusedDataFor: 60, refetchOnMountOrArgChange: 30 })` (api.ts:89-105). Endpoints added via `injectEndpoints`.
- Axios instance: libris-maleficarum-app/src/lib/apiClient.ts.
  - `baseURL = VITE_API_BASE_URL || (dev ? '' : 'http://localhost:5000')` (apiClient.ts:53-56). Empty in dev = use Vite proxy → Aspire.
  - 10s timeout; JSON headers (apiClient.ts:66-73).
  - `axios-retry`: 3 retries, exponential backoff, respects `Retry-After` on 429, retries network/5xx/429 only (apiClient.ts:78-124).
  - Request interceptor (apiClient.ts:128-160): injects `X-Access-Code` header from module-scoped `currentAccessCode` (set via `setAccessCode`), and `Authorization: Bearer <token>` via `msalInstance.acquireTokenSilent(loginRequest)` when `isAuthConfigured`. Also logs requests.
  - Response interceptor logs responses/errors.
- Feature endpoint slices via `api.injectEndpoints`:
  - worldApi.ts: `useGetWorldsQuery`, `useGetWorldByIdQuery`, create/update/delete.
  - worldEntityApi.ts: `useGetWorldEntitiesQuery`, `useGetEntitiesByParentQuery`, `useGetWorldEntityByIdQuery`, create/update/delete/move. Cache tags use ids `LIST_<worldId>` and `PARENT_<worldId>_<parentId|ROOT>`.
  - asyncOperationsApi.ts, configApi.ts.

### Pattern to add a SEARCH endpoint (mirror existing style)

No search endpoint exists in the frontend yet. The backend API design (docs/design/api.md:104-117) DEFINES one:

```text
GET /api/v1/worlds/{worldId}/search   # Search entities by name/tags
```

Add via a new injected endpoint mirroring worldEntityApi.ts, e.g.:

```ts
searchWorldEntities: builder.query<WorldEntity[], { worldId: string; query: string }>({
  query: ({ worldId, query }) => ({
    url: `/api/v1/worlds/${worldId}/search`,
    method: 'GET',
    params: { q: query },
  }),
  transformResponse: (response: WorldEntityListResponse) => response.data,
}),
```

(Confirm the exact query param name `q` and response envelope `{ data, meta }` against the backend when implemented.)

### MSW mocking

- Browser worker: libris-maleficarum-app/src/__mocks__/browser.ts → `setupWorker(...handlers)`.
- Handlers: libris-maleficarum-app/src/__tests__/mocks/handlers.ts (in-memory `mockWorlds`/`mockEntities` Maps; seeded entities include populated `path` arrays, e.g. Suzail `path: [continentId, countryId]`). Node test server: src/__tests__/mocks/server.ts.
- To mock search, add an `http.get('/api/v1/worlds/:worldId/search', ...)` handler filtering `mockEntities` by name/tags and returning `{ data, meta }`.

---

## 7 & 8. Shadcn/UI inventory + components.json + deps

components.json: style `new-york`, `iconLibrary: lucide`, baseColor `stone`, aliases `@/components`, `@/lib/utils`, `@/components/ui`, `@/lib`, `@/hooks`. `cn()` utility = libris-maleficarum-app/src/lib/utils.ts (clsx + tailwind-merge).

Installed UI components (libris-maleficarum-app/src/components/ui/):

- badge, button, calendar, card, context-menu, date-picker, datetime-picker, dialog, drawer, dropdown-menu, form-actions, form-layout, input, popover, scroll-area, select, separator, sheet, skeleton, sonner, textarea, time-picker, tooltip.

CRITICAL GAPS for a typical "Active Search / command palette":

- `command` (Shadcn Command) is NOT installed (no ui/command.tsx).
- `cmdk` is NOT a dependency (not in package.json).
- `popover` IS installed (ui/popover.tsx; `@radix-ui/react-popover@^1.1.15`).
- `dialog` IS installed (`@radix-ui/react-dialog`).
- `input`, `button`, `badge`, `scroll-area`, `separator`, `tooltip` all present.

Relevant deps (package.json): `@reduxjs/toolkit@^2.12.0`, `react-redux@^9.3.0`, `axios@^1.17.0`, `axios-retry@^4.5.0`, `lucide-react@^1.17.0`, `radix-ui@^1.4.3` (+ individual `@radix-ui/react-*`), `class-variance-authority`, `clsx`, `tailwind-merge`, `@azure/msal-browser`/`msal-react`. NO `cmdk`, NO `react-query`, NO `react-router`.

IMPLEMENTATION OPTIONS for the search UI:

1. Add Shadcn `command` + `cmdk` (`pnpm dlx shadcn@latest add command`; adds cmdk dep). Gives a polished combobox/command palette with built-in filtering & keyboard nav. Preferred for an "Active Search" experience.
2. Reuse the existing Popover + Input + results-list pattern already proven in EntityTypeSelector.tsx (shared/EntityTypeSelector) — no new deps. EntityTypeSelector uses `<Popover>` + `<Input placeholder="Filter...">` + scrollable filtered list with recommended/grouped sections and lucide `Search` icon (EntityTypeSelector.tsx:191-235). This is the existing in-repo precedent for a searchable popover.

---

## 9. Entity type → icon mapping

- Primary map: libris-maleficarum-app/src/lib/entityIcons.ts — `entityIconMap: Record<EntityType, LucideIcon>` and `getEntityIcon(entityType)` (used by EntityTreeNode via `createElement(getEntityIcon(entity.entityType), {...})`, EntityTreeNode.tsx:140-145). Icon library = `lucide-react`.
- Registry source of truth: libris-maleficarum-app/src/services/config/entityTypeRegistry.ts (+ `.generated.ts`), generated from registries/entity-types.json by scripts/generate-registry.mjs (`pnpm gen:registry`). Provides `getEntityTypeMeta`, `getEntityTypeSuggestions`, `ENTITY_TYPE_REGISTRY`. EntityTypeSelector maps registry icon-name strings → lucide components via its own `iconMap` (EntityTypeSelector.tsx:46-76).
- For search results, reuse `getEntityIcon(entity.entityType as EntityType)` for consistency with the sidebar.

---

## 10. Auth / context layers

- libris-maleficarum-app/src/auth/authConfig.ts: `isAuthConfigured` (compile-time MSAL client id define), `msalInstance` (PublicClientApplication, sessionStorage cache), `loginRequest.scopes = ['api://libris-maleficarum-api/access_as_user']`. main.tsx wraps `<App>` in `<MsalProvider>` only when configured (main.tsx:44-50).
- Auth header injection is automatic in apiClient.ts request interceptor — search calls made through `apiClient`/RTK Query inherit bearer + access-code headers with NO extra work.
- libris-maleficarum-app/src/contexts/WorldContext.tsx: app-scope context exposing `{ worldId, worldName, setWorld }` (NOT server state). `useWorld()` hook. The authoritative current-world value for data fetching is still Redux `selectSelectedWorldId` (WorldProvider is keyed/seeded from it in App.tsx:186-189).
- Access-code gate: useAccessCode hook + AccessCodeDialog (App.tsx:18-19, 160-176); `setAccessCode` feeds the `X-Access-Code` header.

---

## Synthesis — exact wiring for the Active Search feature

(a) Get current world: `const worldId = useAppSelector(selectSelectedWorldId)` from store/worldSidebarSlice. Disable/hide search when null.

(b) Open entity in MainPanel (view mode): `dispatch(setSelectedEntity(result.id))`. MainPanel auto-fetches via `useGetWorldEntityByIdQuery` and renders `EntityDetailReadOnlyView` (with its Edit button → `openEntityFormEdit`). Hover-edit affordance also exists on the tree node.

(c) Expand hierarchy ancestors so the entity is visible: the selected `WorldEntity` has `path: string[]` (ancestor IDs root→parent, confirmed populated in types and MSW seed). Merge those into expansion state, e.g.:

```ts
dispatch(setExpandedNodes(Array.from(new Set([...expandedNodeIds, ...result.path]))));
// or loop: result.path.forEach(id => dispatch(expandNode(id)));
```

Because the tree is lazy per-level, expanding every `path` id causes each `EntityTreeLevel` to mount and fetch, revealing the node. Do this BEFORE/with `setSelectedEntity`.

Cross-world case: if `result.worldId !== currentWorldId`, dispatch `setSelectedWorld(result.worldId)` first (it clears entity + expansion), then on next tick dispatch `setExpandedNodes(result.path)` then `setSelectedEntity(result.id)`.

(d) Data: add `searchWorldEntities` query to a new/existing injected endpoint slice hitting `GET /api/v1/worlds/{worldId}/search` (debounce the input ~250-300ms; RTK Query handles caching/cancellation via the AbortSignal in axiosBaseQuery).

(e) UI: place in TopToolbar.tsx center. Either add Shadcn `command`/`cmdk` (new dep) OR reuse the existing Popover+Input+list pattern from shared/EntityTypeSelector (no new dep). Use `getEntityIcon` + `formatEntityType` (lib/entityTypeHelpers) + `Badge` for each result row. Use `cn()` from `@/lib/utils`.

---

## Clarifying questions / gaps

1. SEARCH ENDPOINT NOT IMPLEMENTED in frontend (only specified in docs/design/api.md as `GET /api/v1/worlds/{worldId}/search`). Need the exact query-param name (`q`? `query`?), response envelope (`{ data: WorldEntity[], meta }` assumed), and whether it returns full `WorldEntity` objects incl. `path` (required for ancestor expansion). Is the backend endpoint live, or should MSW-only mocking be added now?
2. SEARCH SCOPE: world-scoped only (per current world), or global across all worlds? Current sidebar/expansion model is single-world; cross-world navigation requires the `setSelectedWorld`-first sequence and has a render-timing consideration.
3. UI DEPENDENCY DECISION: OK to add `cmdk` + Shadcn `command` (recommended for a true active-search/command-palette UX), or must we stay dependency-free and reuse the Popover+Input pattern from EntityTypeSelector?
4. RESULT FIELDS: does search return `path` and `worldId`? If `path` is absent, revealing deep nodes needs an alternative (e.g., repeated parent fetches or a dedicated ancestors endpoint).
5. KEYBOARD UX: is a global hotkey (e.g., Ctrl/Cmd-K) desired to focus the search box?

---

## Recommended next research (not done)

- [ ] Confirm backend `GET /worlds/{worldId}/search` contract (params, response, whether `path` is included) once implemented in libris-maleficarum-service.
- [ ] Inspect ChatPanel/NotificationCenter for any existing global-hotkey handling to avoid conflicts with a Ctrl/Cmd-K shortcut.
- [ ] Review existing a11y test patterns in src/__tests__/ and EntityTypeSelector tests to model the search box's jest-axe + keyboard tests.
- [ ] Verify Vite proxy config (vite.config.ts) routes `/api` to Aspire so the new search call works in dev.
