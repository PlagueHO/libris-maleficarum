<!-- markdownlint-disable-file -->
# Subagent Research: Frontend Consumption of a Backend User-Settings API

## Research Topics / Questions

How would the React 19 + TypeScript + Redux Toolkit + RTK Query frontend (`libris-maleficarum-app/`)
CONSUME a backend user-settings API to persist:

* PINNED World Entities (max 5)
* RECENT SEARCHES
* RECENTLY OPENED entities

…as an alternative/upgrade to the browser-localStorage approach already researched in
`.copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md`.

Specific questions:

1. Existing RTK Query endpoint precedents (injection, `tagTypes`/`providesTags`/`invalidatesTags`, mutation
   structure, optimistic updates). configApi as the closest "settings" precedent.
2. Config / settings precedent — is there any per-user settings concept on the frontend today?
3. Optimistic pin/recent UX — recommended `onQueryStarted` + `updateQueryData` + rollback pattern, with concrete
   `userSettingsApi` slice snippets.
4. Migration from localStorage — adapter that sources pinned/recent from EITHER localStorage OR backend based on
   auth mode (`isAuthConfigured`, MSAL account presence).
5. Data shapes — propose frontend TS types for `UserSettings`, `PinnedEntity`, `RecentSearch`, aligned with
   `docs/design/data_model.md`.
6. Testing — how RTK Query endpoints + optimistic updates would be tested (MSW handlers, jest-axe).

## Status

Complete.

---

## 1. Existing RTK Query Endpoint Precedents

### 1.1 Base API slice and the `injectEndpoints` pattern

There is a SINGLE base `createApi` instance, and every feature slice injects into it via `api.injectEndpoints`.

`libris-maleficarum-app/src/services/api.ts` lines 86-105 — the base slice:

```ts
export const api = createApi({
  reducerPath: 'api',
  baseQuery: axiosBaseQuery(),
  tagTypes: [
    'World',
    'WorldEntity',
    'Character',
    'Location',
    'Organization',
    'DeleteOperation', // Delete operation tracking
  ], // Tag types for cache invalidation
  keepUnusedDataFor: 60, // Cache data for 60 seconds after last component unmounts
  refetchOnMountOrArgChange: 30, // Refetch if data is older than 30 seconds
  endpoints: () => ({}), // Endpoints defined via injectEndpoints in feature slices
});
```

Key facts:

* `axiosBaseQuery()` (api.ts lines 27-78) wraps the configured Axios instance, forwards `signal` (AbortSignal) for
  request cancellation, and special-cases `ERR_CANCELED`. The base query object accepts `{ url, method, data,
  params, headers }`.
* `tagTypes` is a fixed allow-list. A new `UserSettings` tag MUST be added here for cache invalidation to work
  (RTK Query throws/warns on unknown tag types).
* Auth/access-code headers are injected at the Axios interceptor layer
  (`libris-maleficarum-app/src/lib/apiClient.ts` lines 122-156), NOT in the endpoints — so a user-settings endpoint
  automatically gets `Authorization: Bearer` (when `isAuthConfigured` and an MSAL account exists) and/or
  `X-Access-Code`. No per-endpoint auth wiring needed.

### 1.2 `providesTags` / `invalidatesTags` conventions

The codebase uses a consistent list+entity tagging convention. From
`libris-maleficarum-app/src/services/worldApi.ts`:

* Query `getWorlds` (lines 30-46) provides a tag per item plus a `{ type: 'World', id: 'LIST' }` sentinel:

```ts
providesTags: (_result) =>
  _result
    ? [
        ..._result.map(({ id }) => ({ type: 'World' as const, id })),
        { type: 'World', id: 'LIST' },
      ]
    : [{ type: 'World', id: 'LIST' }],
```

* Mutations invalidate the specific id + the `LIST` sentinel, e.g. `updateWorld` (worldApi.ts lines 91-118) →
  `[{ type: 'World', id }, { type: 'World', id: 'LIST' }]`.

`libris-maleficarum-app/src/services/worldEntityApi.ts` uses more elaborate composite ids:

* `LIST_${worldId}` for the per-world list (lines 45-53).
* `PARENT_${worldId}_${parentId ?? 'ROOT'}` for parent-scoped child lists (lines 81-101, used by
  `getEntitiesByParent`).
* Mutations (`createWorldEntity` lines 124-167, `updateWorldEntity` lines 172-202, `moveWorldEntity` lines 232-274)
  recompute and invalidate the affected composite tags.

Convention takeaways for `userSettingsApi`:

* A single per-user settings document is a singleton resource → a single tag id is sufficient, e.g.
  `{ type: 'UserSettings', id: 'CURRENT' }` (mirrors the `'LIST'` sentinel convention, substituting only the
  segment — `'CURRENT'` because there is one settings doc per user, not a list).

### 1.3 Mutation structure

Standard mutation shape across the codebase (worldApi.ts, worldEntityApi.ts, asyncOperationsApi.ts):

```ts
xxx: builder.mutation<ReturnType, ArgType>({
  query: (arg) => ({ url, method, data, params }),
  transformResponse: (response: ApiResponse<T>) => response.data, // unwrap { data, meta }
  invalidatesTags: (result, _error, arg) => [ /* tags */ ],
}),
```

* Responses are consistently wrapped as `{ data, meta }` and unwrapped with `transformResponse`.
  `asyncOperationsApi.ts` defines a reusable `interface ApiResponse<T> { data: T; meta?: Record<string, unknown> }`
  (lines 22-25) — a user-settings response should reuse this `{ data, meta }` envelope.

### 1.4 OPTIMISTIC UPDATE patterns — ABSENT

Verified by repository-wide search (no matches in `libris-maleficarum-app/src/**`):

* `onQueryStarted` — 0 occurrences.
* `updateQueryData` — 0 occurrences.
* `patchResult` / `.undo()` — none.

Conclusion: **No optimistic-update pattern exists in the codebase today.** All mutations rely on
`invalidatesTags` → refetch. The closest thing to "optimistic delete" is a comment in `asyncOperationsApi.ts`
lines 173-176 ("Frontend should optimistically update the UI hierarchy…") but it is NOT implemented; that endpoint
just uses `invalidatesTags`.

This means the `userSettingsApi` would INTRODUCE the first `onQueryStarted` + `updateQueryData` usage. It should be
documented carefully and covered by tests.

### 1.5 configApi — closest "settings"-style precedent (full source)

`libris-maleficarum-app/src/services/configApi.ts` (entire file):

```ts
/**
 * Config API Slice
 *
 * RTK Query endpoints for application configuration state.
 */

import { api } from './api';
import type { AccessControlStatus } from './types';

export const configApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getAccessStatus: builder.query<AccessControlStatus, void>({
      query: () => ({
        url: '/api/config/access-status',
        method: 'GET',
      }),
    }),
  }),
});

export const { useGetAccessStatusQuery, useLazyGetAccessStatusQuery } = configApi;
```

Notes:

* `getAccessStatus` is APP-LEVEL config (whether an access code is required), NOT per-user settings.
* It does NOT use `providesTags`/`transformResponse` (the response is a flat `AccessControlStatus`, not a
  `{ data, meta }` envelope, and it is effectively immutable for the session).
* There is also a non-RTK twin `libris-maleficarum-app/src/services/configService.ts` (plain Axios
  `getAccessStatus`) used outside the React render tree.

So configApi proves the injection mechanics for a settings-style GET but is too thin to model mutations or
per-user state. The richer precedent for mutations + invalidation is worldApi.ts / worldEntityApi.ts.

---

## 2. Config / Settings Precedent and Per-User Settings Concept

* App-level config today = `GET /api/config/access-status` → `AccessControlStatus` (configApi.ts / configService.ts).
  MSW handler exists at `libris-maleficarum-app/src/__tests__/mocks/handlers.ts` line 789.
* The access code is held in a module-scoped variable + injected as `X-Access-Code`
  (`libris-maleficarum-app/src/lib/apiClient.ts` lines 16-32, 125-127). There is NO React hook named
  `useAccessCode`; the access code is set via `setAccessCode()` and read via `getAccessCode()`.
* **There is NO existing per-user settings concept on the frontend.** The only client-persisted user preference is
  the theme, stored in `localStorage` by `libris-maleficarum-app/src/hooks/useTheme.ts` (key `'theme'`, lines
  6, 16, 58). This is the canonical localStorage-hook precedent to mirror for a `useSearchHistory` fallback.
* Auth mode detection lives in `libris-maleficarum-app/src/auth/authConfig.ts`:
  * `isAuthConfigured` (lines 10-13) — true only when an MSAL client id is baked in at build time.
  * `msalInstance.getAllAccounts()` is how the app checks for a signed-in account
    (`libris-maleficarum-app/src/lib/apiClient.ts` line 132).
  * `main.tsx` lines 39-44 branch the whole app tree on `isAuthConfigured`; `AuthGuard.tsx` short-circuits to
    "render children" when `!isAuthConfigured` (anonymous mode).

---

## 3. Optimistic Pin/Recent UX — Recommended Pattern + Proposed `userSettingsApi`

### 3.1 The RTK Query optimistic-update recipe

Pattern (to be introduced; not currently in the repo):

```ts
async onQueryStarted(arg, { dispatch, queryFulfilled }) {
  // 1. Patch the cached getUserSettings result immediately.
  const patch = dispatch(
    userSettingsApi.util.updateQueryData('getUserSettings', undefined, (draft) => {
      // mutate draft (Immer) to reflect the optimistic change
    }),
  );
  try {
    await queryFulfilled; // 2. Wait for the server.
  } catch {
    patch.undo(); // 3. Roll back on failure.
  }
},
```

Key points specific to this codebase:

* `getUserSettings` takes `void` arg → the cache key arg passed to `updateQueryData` is `undefined`.
* `keepUnusedDataFor: 60` (api.ts line 103) means the settings cache can be evicted; optimistic patches only apply
  if the query is currently subscribed. The search panel keeps `useGetUserSettingsQuery()` mounted while open, so
  the patch will land. Pair the optimistic patch with `invalidatesTags` so a later refetch reconciles truth.
* Because the base query special-cases `ERR_CANCELED` as an `error` (api.ts lines 50-62), a cancelled mutation
  would also trigger `patch.undo()`. Mutations are not cancelled on unmount the way queries are, so this is a minor
  edge case, but tests should assert rollback only on real server errors.

### 3.2 Proposed `userSettingsApi.ts` injected slice (concrete)

This is the recommended new file `libris-maleficarum-app/src/services/userSettingsApi.ts`. It assumes a backend
contract of `GET/PUT /api/v1/user-settings` returning `{ data: UserSettings, meta? }` and dedicated mutation
sub-routes for pin/recent (so the server can enforce the max-5 pin cap and recent de-dup/trim). All three mutations
use optimistic updates against the single `getUserSettings` cache entry.

```ts
/**
 * User Settings API Slice
 *
 * RTK Query endpoints for per-user UI preferences: pinned entities, recent searches,
 * recently opened entities. Singleton resource ("current user"). Mutations use
 * optimistic updates so pin/recent actions feel instant.
 *
 * NOTE: 'UserSettings' must be added to tagTypes in services/api.ts.
 */

import { api } from './api';
import type {
  UserSettings,
  PinnedEntity,
  RecentSearch,
  RecentEntity,
} from './types/userSettings.types';

interface ApiResponse<T> {
  data: T;
  meta?: Record<string, unknown>;
}

const MAX_PINNED = 5;
const MAX_RECENT_SEARCHES = 10;
const MAX_RECENT_ENTITIES = 10;

export const userSettingsApi = api.injectEndpoints({
  endpoints: (builder) => ({
    /**
     * GET /api/v1/user-settings — the search panel subscribes to this.
     */
    getUserSettings: builder.query<UserSettings, void>({
      query: () => ({ url: '/api/v1/user-settings', method: 'GET' }),
      transformResponse: (response: ApiResponse<UserSettings>) => response.data,
      providesTags: [{ type: 'UserSettings', id: 'CURRENT' }],
    }),

    /**
     * PUT /api/v1/user-settings/pins — toggle a pinned entity (add or remove).
     * Optimistically updates the cached settings; enforces max 5 client-side.
     */
    togglePin: builder.mutation<UserSettings, PinnedEntity>({
      query: (entity) => ({
        url: '/api/v1/user-settings/pins',
        method: 'PUT',
        data: entity,
      }),
      transformResponse: (response: ApiResponse<UserSettings>) => response.data,
      async onQueryStarted(entity, { dispatch, queryFulfilled }) {
        const patch = dispatch(
          userSettingsApi.util.updateQueryData('getUserSettings', undefined, (draft) => {
            const idx = draft.pinnedEntities.findIndex((p) => p.id === entity.id);
            if (idx >= 0) {
              draft.pinnedEntities.splice(idx, 1); // unpin
            } else if (draft.pinnedEntities.length < MAX_PINNED) {
              draft.pinnedEntities.push(entity); // pin (cap at 5)
            }
          }),
        );
        try {
          await queryFulfilled;
        } catch {
          patch.undo();
        }
      },
      // Reconcile with server truth after the optimistic window.
      invalidatesTags: [{ type: 'UserSettings', id: 'CURRENT' }],
    }),

    /**
     * POST /api/v1/user-settings/recent-searches — record a search term.
     * De-dupes, moves-to-front, trims to MAX_RECENT_SEARCHES.
     */
    recordRecentSearch: builder.mutation<UserSettings, RecentSearch>({
      query: (search) => ({
        url: '/api/v1/user-settings/recent-searches',
        method: 'POST',
        data: search,
      }),
      transformResponse: (response: ApiResponse<UserSettings>) => response.data,
      async onQueryStarted(search, { dispatch, queryFulfilled }) {
        const patch = dispatch(
          userSettingsApi.util.updateQueryData('getUserSettings', undefined, (draft) => {
            draft.recentSearches = [
              search,
              ...draft.recentSearches.filter(
                (s) => s.query.toLowerCase() !== search.query.toLowerCase(),
              ),
            ].slice(0, MAX_RECENT_SEARCHES);
          }),
        );
        try {
          await queryFulfilled;
        } catch {
          patch.undo();
        }
      },
      invalidatesTags: [{ type: 'UserSettings', id: 'CURRENT' }],
    }),

    /**
     * POST /api/v1/user-settings/recent-entities — record a recently opened entity.
     * De-dupes by id, moves-to-front, trims to MAX_RECENT_ENTITIES.
     */
    recordRecentEntity: builder.mutation<UserSettings, RecentEntity>({
      query: (entity) => ({
        url: '/api/v1/user-settings/recent-entities',
        method: 'POST',
        data: entity,
      }),
      transformResponse: (response: ApiResponse<UserSettings>) => response.data,
      async onQueryStarted(entity, { dispatch, queryFulfilled }) {
        const patch = dispatch(
          userSettingsApi.util.updateQueryData('getUserSettings', undefined, (draft) => {
            draft.recentEntities = [
              entity,
              ...draft.recentEntities.filter((e) => e.id !== entity.id),
            ].slice(0, MAX_RECENT_ENTITIES);
          }),
        );
        try {
          await queryFulfilled;
        } catch {
          patch.undo();
        }
      },
      invalidatesTags: [{ type: 'UserSettings', id: 'CURRENT' }],
    }),
  }),
});

export const {
  useGetUserSettingsQuery,
  useTogglePinMutation,
  useRecordRecentSearchMutation,
  useRecordRecentEntityMutation,
} = userSettingsApi;
```

Required base-slice change (`libris-maleficarum-app/src/services/api.ts` lines 88-95): add `'UserSettings'` to
`tagTypes`.

```ts
tagTypes: [
  'World',
  'WorldEntity',
  'Character',
  'Location',
  'Organization',
  'DeleteOperation',
  'UserSettings', // per-user pinned/recent settings
],
```

Design note: `togglePin` as a single PUT keeps the mutation surface small and matches the "substitute only the
differing segment" naming discipline (pin vs unpin is one toggle). If the backend prefers explicit verbs, split
into `pinEntity` (POST) / `unpinEntity` (DELETE) — both still patch the same `getUserSettings` cache entry.

---

## 4. Migration from localStorage — Adapter Strategy by Auth Mode

### 4.1 The problem

Prior research recommended a `useSearchHistory` localStorage hook (mirroring `useTheme.ts`). The backend option
should be additive, not a rewrite. The search panel should consume ONE hook regardless of source.

### 4.2 Recommended approach: an adapter hook that selects backend vs localStorage

Introduce a single facade hook `useUserSettings()` that exposes a stable interface
(`pinnedEntities`, `recentSearches`, `recentEntities`, `togglePin`, `recordRecentSearch`, `recordRecentEntity`,
`isLoading`). Internally it picks an implementation based on auth mode:

* **Anonymous mode** (`!isAuthConfigured`, or `isAuthConfigured && msalInstance.getAllAccounts().length === 0`):
  use the localStorage implementation (`useSearchHistoryLocal`, modeled on `useTheme.ts`). There is no real user,
  so server-side per-user storage has no owner key.
* **Authenticated mode** (`isAuthConfigured && getAllAccounts().length > 0`): use the RTK Query implementation
  (`useGetUserSettingsQuery` + the three mutations).

```ts
// libris-maleficarum-app/src/hooks/useUserSettings.ts
import { isAuthConfigured, msalInstance } from '@/auth/authConfig';
import { useUserSettingsBackend } from './useUserSettingsBackend';
import { useUserSettingsLocal } from './useUserSettingsLocal';

function isAuthenticated(): boolean {
  return isAuthConfigured && msalInstance.getAllAccounts().length > 0;
}

export function useUserSettings() {
  // Hook order must be stable across renders: call BOTH, select one.
  const backend = useUserSettingsBackend({ enabled: isAuthenticated() });
  const local = useUserSettingsLocal({ enabled: !isAuthenticated() });
  return isAuthenticated() ? backend : local;
}
```

Critical React caveat: hooks cannot be called conditionally. Two safe implementations:

1. Call both hooks every render but pass an `enabled`/`skip` flag (RTK Query supports
   `useGetUserSettingsQuery(undefined, { skip: !authed })`; the local hook no-ops when disabled), then return the
   selected one. Simple, but both subscriptions exist.
2. Pick the implementation at a stable boundary: `isAuthConfigured` is a build-time constant, and account presence
   only changes via login/logout which already remount the tree (`main.tsx`/`AuthGuard`). A component-level switch
   that mounts `<SearchPanelBackend/>` vs `<SearchPanelLocal/>` keeps each hook unconditional within its subtree.
   Recommended for clarity.

### 4.3 Trade-offs

| Dimension | localStorage (`useTheme`-style) | Backend (`userSettingsApi`) |
| --- | --- | --- |
| Cross-device sync | No (per browser) | Yes |
| Works anonymously | Yes (no owner needed) | No (needs an owner id) |
| Instant UX | Synchronous, always instant | Instant via optimistic update + rollback |
| Offline | Fully | Degrades; needs optimistic patch to feel responsive |
| Storage limits | ~5 MB, no server cost | Cosmos RU cost per write |
| Privacy | Stays on device | Stored server-side under `ownerId` |
| Backend dependency | None | New endpoint + Cosmos container/partition |

Recommendation: ship localStorage first (no backend dependency, works in anonymous mode), then layer the backend
behind the `useUserSettings` adapter so authenticated users get cross-device sync without changing the search-panel
component. A future "migrate local → backend on first login" one-shot can read the localStorage doc and call the
three mutations.

---

## 5. Proposed Frontend Data Shapes

### 5.1 Data-shape gate

`docs/design/data_model.md` is authoritative for persistence contracts. Today it defines World and WorldEntity
documents with `ownerId`, `createdAt`, `updatedAt`, `isDeleted`, `schemaVersion` conventions (camelCase on the
wire; see WorldEntity at data_model.md lines ~175-213 and the `worldEntity.types.ts` mirror). **There is currently
NO UserSettings document defined in data_model.md** — adding one is a NEW persistence contract that must be added
to data_model.md BEFORE the backend/frontend types are finalized. The shapes below are a PROPOSAL pending that
update.

Convention alignment for a new `UserSettings` Cosmos document (proposed): partition key `/ownerId`, one document
per user (`id == ownerId` or a fixed `"settings"` id), plus the standard `createdAt`/`updatedAt`/`schemaVersion`.

### 5.2 Minimal denormalized fields for a pinned/recent row

A pinned (or recent) row must render WITHOUT a full entity fetch. From `worldEntity.types.ts` (lines 51-103) the
fields needed to render a row + navigate + expand hierarchy are:

* `id` — entity id (for open + cache key).
* `worldId` — required to scope the open + the `getWorldEntityByIdQuery({ worldId, entityId })` call.
* `name` — display label.
* `entityType` — drives the icon (resolved via the entity-type registry).
* `path?: string[]` — ancestor ids, needed for hierarchy expand-to-reveal (the search box's stated UX). Optional
  because the search projection may omit it (the existing `path`-projection gap noted in the prior research). If
  absent, fall back to fetching the entity by id on open to obtain `path`.

### 5.3 Proposed TypeScript types

New file `libris-maleficarum-app/src/services/types/userSettings.types.ts`:

```ts
/**
 * User Settings Types
 *
 * Per-user UI preferences persisted via the backend user-settings API
 * (or localStorage in anonymous mode). NOT yet defined in docs/design/data_model.md —
 * treat as PROPOSAL pending a data-model update.
 *
 * @module services/types/userSettings.types
 */

import type { WorldEntityType } from './worldEntity.types';

/** Minimal denormalized entity reference renderable without a full entity fetch. */
export interface PinnedEntity {
  /** Entity id (GUID). */
  id: string;
  /** Owning world id (scopes open + by-id fetch). */
  worldId: string;
  /** Display name. */
  name: string;
  /** Entity classification (drives icon). */
  entityType: WorldEntityType;
  /** Ancestor ids root→parent for hierarchy expand-to-reveal. Optional (projection gap). */
  path?: string[];
  /** ISO 8601 timestamp the entity was pinned. */
  pinnedAt: string;
}

/** A recently opened entity (same denormalized shape as a pin, minus pinnedAt). */
export interface RecentEntity {
  id: string;
  worldId: string;
  name: string;
  entityType: WorldEntityType;
  path?: string[];
  /** ISO 8601 timestamp it was last opened. */
  openedAt: string;
}

/** A recorded search term (scoped to a world for replay). */
export interface RecentSearch {
  /** The raw query string. */
  query: string;
  /** World the search was run against (for scoped replay). */
  worldId: string;
  /** Optional search mode used (hybrid|text|vector). */
  mode?: 'hybrid' | 'text' | 'vector';
  /** ISO 8601 timestamp the search was run. */
  searchedAt: string;
}

/** The per-user settings document the search panel reads. */
export interface UserSettings {
  /** Owner user id (partition key on the server; absent/anonymous in localStorage mode). */
  ownerId?: string;
  /** Pinned entities, max 5, most-recent-first. */
  pinnedEntities: PinnedEntity[];
  /** Recent searches, most-recent-first (server trims, e.g. 10). */
  recentSearches: RecentSearch[];
  /** Recently opened entities, most-recent-first (server trims, e.g. 10). */
  recentEntities: RecentEntity[];
  /** ISO 8601 timestamps (present in backend mode). */
  createdAt?: string;
  updatedAt?: string;
  /** Schema version for document compatibility. */
  schemaVersion?: number;
}

/** API envelope mirrors the rest of the codebase ({ data, meta }). */
export interface UserSettingsResponse {
  data: UserSettings;
  meta?: Record<string, unknown>;
}
```

Naming-discipline note: `recentEntities` (recently OPENED) parallels `recentSearches` — only the trailing noun
differs, matching the existing `LIST_*` / `PARENT_*` convention of substituting just the differing segment.

---

## 6. Testing

### 6.1 How existing tests mock RTK Query

Two complementary mock setups:

* Shared MSW handlers: `libris-maleficarum-app/src/__tests__/mocks/handlers.ts` (lines 180-790) define handlers for
  every route against a `baseUrl`, backed by in-memory `Map`s (`mockWorlds`, `mockEntities`) seeded by
  `seedMockData()` (lines 40-118). The config route handler is at line 789.
* Per-test `setupServer` with inline handlers + `renderHook` from Testing Library: see
  `libris-maleficarum-app/src/__tests__/services/worldApi.test.tsx` lines 55-110. Pattern:
  * Build a fresh store with `configureStore({ reducer: { [api.reducerPath]: api.reducer }, middleware: ... })`.
  * Wrap in `<Provider store={store}>`.
  * `renderHook(() => useGetWorldsQuery(), { wrapper })` then `await waitFor(() => expect(result.current.isSuccess))`.

### 6.2 Testing the user-settings endpoint + optimistic updates

Add an MSW handler set for the new routes (in `handlers.ts`, mirroring the access-status handler at line 789):

```ts
let mockUserSettings: UserSettings = {
  ownerId: 'test-user@example.com',
  pinnedEntities: [],
  recentSearches: [],
  recentEntities: [],
  schemaVersion: 1,
};

http.get(`${baseUrl}/api/v1/user-settings`, () =>
  HttpResponse.json({ data: mockUserSettings }),
),
http.put(`${baseUrl}/api/v1/user-settings/pins`, async ({ request }) => {
  const entity = (await request.json()) as PinnedEntity;
  const idx = mockUserSettings.pinnedEntities.findIndex((p) => p.id === entity.id);
  if (idx >= 0) mockUserSettings.pinnedEntities.splice(idx, 1);
  else if (mockUserSettings.pinnedEntities.length < 5)
    mockUserSettings.pinnedEntities.push(entity);
  return HttpResponse.json({ data: mockUserSettings });
}),
```

Optimistic-update test outline (`renderHook` style):

* Render `useGetUserSettingsQuery()` and a mutation hook in the same wrapper/store.
* Fire `togglePin(entity)`; SYNCHRONOUSLY (before `await`) assert the `getUserSettings` cache already shows the pin
  (`store.dispatch(userSettingsApi.endpoints.getUserSettings.initiate())` selector, or read `result.current.data`
  on the query hook). This proves the optimistic patch landed before the server responded.
* Make the MSW handler return a 500 for the pin route in a dedicated test; assert the pin is rolled back
  (`patch.undo()` path) after `await waitFor(...)`.
* Assert the max-5 cap: pin 6 entities, expect length 5.

### 6.3 jest-axe / accessibility

The repo extends `toHaveNoViolations` and uses `axe(container)` (per AGENTS.md and existing component tests). For
the search panel rows rendered from pinned/recent settings:

* Render the panel inside `<Provider store={store}>` with seeded settings, then
  `expect(await axe(container)).toHaveNoViolations()`.
* Ensure pin/unpin controls are real `<button>`s with accessible names (e.g. `aria-label="Pin {name}"` /
  `aria-label="Unpin {name}"`), and that the max-5-reached state is announced (e.g., disabled pin button with an
  accessible explanation) per the accessibility instructions (voice-access label must contain the visible label).

---

## Key Discoveries (Evidence)

* Single base `createApi`; all slices inject via `injectEndpoints`. `tagTypes` is a fixed allow-list → must add
  `'UserSettings'`. (api.ts lines 86-105.)
* Auth/access-code headers are injected at the Axios interceptor layer, so a user-settings endpoint needs no
  per-endpoint auth wiring. (apiClient.ts lines 122-156.)
* **No optimistic updates anywhere** (`onQueryStarted`/`updateQueryData`/`patchResult` = 0 matches). The
  user-settings slice would be the first. (Repo-wide search.)
* configApi is the closest settings precedent but is app-level (access-status), not per-user, and lacks
  tags/mutations. (configApi.ts full file.)
* The only client-persisted user preference today is theme via localStorage in useTheme.ts — the model for a
  localStorage fallback hook. (useTheme.ts lines 6, 16, 58.)
* Auth mode = `isAuthConfigured` (build-time) + `msalInstance.getAllAccounts()` (runtime account presence).
  (authConfig.ts lines 10-13; apiClient.ts line 132; main.tsx lines 39-44.)
* `docs/design/data_model.md` has NO UserSettings document — adding one is a new persistence contract that must be
  authored there first (data-shape gate).

## Recommended Next Research (Checklist)

* [ ] Confirm/author the `UserSettings` Cosmos document in `docs/design/data_model.md` (partition key `/ownerId`,
      one doc per user, max-5 pin enforcement location, recent-trim sizes) before finalizing types.
* [ ] Confirm the backend route surface: single `PUT /api/v1/user-settings` (whole-doc) vs granular
      `pins` / `recent-searches` / `recent-entities` sub-routes (assumed granular here).
* [ ] Decide whether `togglePin` is one toggle endpoint or split `pin`(POST)/`unpin`(DELETE).
* [ ] Decide the localStorage→backend migration trigger on first authenticated login (one-shot copy).
* [ ] Confirm whether `path` will be present on pinned/recent rows or always require a by-id fetch on open
      (depends on the search-projection `path` gap from the prior research).
* [ ] Add MSW handlers for the three routes in `src/__tests__/mocks/handlers.ts` for component tests.

## Clarifying Questions / Gaps

1. Does the backend already expose (or plan) a `/api/v1/user-settings` endpoint, and what is its exact route shape
   and envelope? (No such route exists in the frontend today; this research assumes a contract.)
2. In anonymous mode there is no `ownerId`. Confirm anonymous users should stay on localStorage (assumed yes) — or
   should the single-tenant anonymous user get a synthetic owner id server-side?
3. What are the authoritative trim limits for recent searches and recently opened entities? (Assumed 10 each;
   pins fixed at 5 per the task.)
4. Should pin order be user-reorderable (drag), or strictly most-recent-first? (Assumed most-recent-first; affects
   whether `pinnedEntities` needs an explicit `order` field.)
