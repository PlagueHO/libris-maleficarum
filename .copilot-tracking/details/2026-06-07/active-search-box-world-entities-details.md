<!-- markdownlint-disable-file -->
# Implementation Details: Active Search Box for World Entities

## Context Reference

Sources:
* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md - Full research: backend Search API contract, frontend integration points, cmdk/Popover UX, persistence options, resolved follow-ups.
* docs/design/data_model.md - Authoritative persistence shapes (camelCase; `path`/`depth`/`hasChildren`).
* docs/design/api.md - Canonical search route documentation.
* .github/instructions/accessibility.instructions.md - WCAG 2.2 AA combobox requirements.

## Implementation Phase 1: Backend Search API Hardening

<!-- parallelizable: true -->

Backend-only changes in libris-maleficarum-service. Independent of all frontend phases (different files, different build). In scope: `path` and `depth` are already stored on `WorldEntity`, already pushed by `SearchIndexSyncService`, and already indexed — this is a 4-site projection-only fix with no extra Cosmos queries. Adds the `path` projection needed for hierarchy reveal and closes the ownership-check gap on the canonical search route.

### Step 1.1: Add `path` (and `depth`) to the search result projection

Extend the read path so search results carry the ancestor `path` array, enabling the frontend to expand the sidebar hierarchy without a second round-trip. `path` and `depth` are already indexed (filterable); only the read/projection path drops them.

Files:
* libris-maleficarum-service/src/Infrastructure/Services/AzureAISearchService.cs - Add `path` (and `depth`) to the `Select` list (~line 254) and to the `SearchResult` projection mapping (~lines 296-313).
* libris-maleficarum-service/src/Domain/Models/SearchResult.cs - Add `Path` (`IReadOnlyList<string>`) and optionally `Depth` (`int`) properties.
* libris-maleficarum-service/src/Api/Models/Responses/SearchResultResponse.cs - Add `path` (and `depth`) to `SearchResultItem` (camelCase via System.Text.Json), preserving existing field order conventions.
* libris-maleficarum-service/src/Api/Controllers/WorldsController.cs - Update the duplicated controller-to-response mapping to project `path` (and `depth`).
* libris-maleficarum-service/src/Api/Controllers/WorldEntitiesController.cs - Mirror the mapping change if the duplicate action is retained until Step 1.2 removes it.

Discrepancy references:
* Addresses the `path`-projection gap (research "Key Discoveries" — blocker for hierarchy reveal).

Success criteria:
* `GET /api/v1/worlds/{worldId}/search` response includes a non-null `path` array for entities that have ancestors.
* `depth` included when added; no change to existing fields.
* `hasChildren` intentionally NOT added (not indexed; deferred per research).

Context references:
* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md ("Follow-Up Items — Resolved" first bullet; "Research Executed" AzureAISearchService entry) - exact line sites (70-71 index, 254 Select, 296-313 projection).

Dependencies:
* docs/design/data_model.md field names (`path`, `depth`) confirmed before coding (Data-Shape Gate).

### Step 1.2: Close the ownership gap on the canonical search route and remove the duplicate

Port the verbatim world-ownership block (404/403) into the canonical `WorldsController` search action, then delete the duplicate `/entities/search` action so a single authoritative endpoint remains.

Files:
* libris-maleficarum-service/src/Api/Controllers/WorldsController.cs - Add the ownership check (returns 404 when world missing, 403 when not owned) to the `GET /api/v1/worlds/{worldId:guid}/search` action so behavior matches its XML docs.
* libris-maleficarum-service/src/Api/Controllers/WorldEntitiesController.cs - Delete the duplicate `GET /api/v1/worlds/{worldId:guid}/entities/search` action (lines ~273-323).
* docs/design/api.md - Confirm the documented route matches the surviving endpoint (no contradictory `/entities/search` reference remains).

Discrepancy references:
* Addresses the auth gap (research "Follow-Up Items — Resolved" canonical-endpoint bullet).

Success criteria:
* Canonical route enforces ownership identically to the former `WorldEntitiesController` block (ported verbatim — naming discipline).
* Only one search action exists across both controllers.
* No frontend caller broken (none exists yet).

Context references:
* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md ("Follow-Up Items — Resolved" canonical endpoint; WorldEntitiesController.cs lines 238-263 ownership block, 273-323 duplicate action).

Dependencies:
* Step 1.1 (same files touched — sequence within phase to avoid conflicting edits).

### Step 1.3: Update backend tests for the new projection and ownership behavior

Files:
* libris-maleficarum-service/tests/ - Update/add unit tests asserting: search response carries `path`; canonical route returns 404/403 per ownership; duplicate action removed (route no longer mapped).

Success criteria:
* Tests cover `path` presence and ownership 404/403 paths.
* `service: test (unit)` task passes.

Context references:
* AGENTS.md (MSTest + FluentAssertions backend test pattern; `[TestCategory("Unit")]`).

Dependencies:
* Steps 1.1, 1.2 complete.

### Step 1.4: Validate phase changes

Validation commands:
* `dotnet build LibrisMaleficarum.slnx` (cwd libris-maleficarum-service) - compile backend.
* `service: test (unit)` task - run unit tests scoped to changed projects.

## Implementation Phase 2: Frontend Search Data Layer

<!-- parallelizable: true -->

All-new frontend files; no edits to shared files except adding the search endpoint via `injectEndpoints` (non-conflicting) and an MSW handler. Independent of Phase 1 (uses optional `path`; falls back gracefully if absent).

### Step 2.1: Define search types

Files:
* libris-maleficarum-app/src/services/types/search.types.ts - `SearchResultItem` (incl. optional `path?: string[]`), `SearchResponse` (`{ data, meta }`), `SearchEntitiesArg` (`worldId`, `q`, `mode?`, `entityType?`, `tags?`, `limit?`, `offset?`).

Success criteria:
* Types mirror the live response shape (camelCase) from research.
* `path` typed optional to tolerate pre/post Phase 1 backend.

Context references:
* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md ("Complete Examples" searchApi.ts snippet; "API and Schema Documentation").

Dependencies:
* None.

### Step 2.2: Inject the search endpoint

Files:
* libris-maleficarum-app/src/services/searchApi.ts - `api.injectEndpoints` with `searchEntities` query → `GET /api/v1/worlds/{worldId}/search`, params `{ q, mode, entityType, tags: csv, limit, offset }`, `keepUnusedDataFor: 120`. Export `useSearchEntitiesQuery`.

Success criteria:
* Query builds the correct URL/params; auth headers auto-injected by existing axios interceptor; `signal` cancellation works via `axiosBaseQuery`.

Context references:
* libris-maleficarum-app/src/services/api.ts (lines 28-105 axiosBaseQuery + createApi) - injection target.
* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md ("Complete Examples" searchApi.ts).

Dependencies:
* Step 2.1.

### Step 2.3: Add the debounce hook

Files:
* libris-maleficarum-app/src/hooks/useDebouncedValue.ts - `setTimeout`-based 300ms debounce returning the debounced value; cleans up on unmount/change.

Success criteria:
* Returns the latest value after the delay; no stale timers; typed generically.

Context references:
* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md ("External Research" debounce 300ms / 250-400ms band).

Dependencies:
* None (no existing hook — confirmed absent).

### Step 2.4: Add the search facade hook

Files:
* libris-maleficarum-app/src/hooks/useEntitySearch.ts - Compose `useDebouncedValue` + `useSearchEntitiesQuery`; gate with `MIN_QUERY_LENGTH = 2` and `skip` when inactive or no `worldId`; expose `results`, `isInitialLoading`, `isRefreshing`, `isError`, `isEmptyQuery`, `hasNoResults`.

Success criteria:
* No request fired below min length or without a selected world; SWR states surfaced for the UI.

Context references:
* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md ("Complete Examples" useEntitySearch.ts).
* libris-maleficarum-app/src/store/worldSidebarSlice.ts (`selectSelectedWorldId`).

Dependencies:
* Steps 2.2, 2.3.

### Step 2.5: Add the MSW search handler

Files:
* libris-maleficarum-app/src/__tests__/mocks/handlers.ts - Add `http.get('/api/v1/worlds/:worldId/search', ...)` returning `{ data: SearchResultItem[], meta }` with populated `path` arrays for hierarchy-reveal tests (server registered via src/__tests__/mocks/server.ts).

Success criteria:
* Vitest (MSW mode) resolves the search route; seed data includes `path` ancestors.

Context references:
* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md ("Follow-Up Items — Resolved" Vite proxy/MSW; "Code Search Results" path/MSW seed).

Dependencies:
* Step 2.1.

### Step 2.6: Validate phase changes

Validation commands:
* `pnpm type-check` (cwd libris-maleficarum-app) - type the new layer.
* `pnpm lint` (cwd libris-maleficarum-app) - lint new files.

## Implementation Phase 3: Per-World localStorage Persistence

<!-- parallelizable: true -->

All-new frontend hook + tests. Independent of Phases 1, 2, 4 (own files). Implements v1 persistence (history/recent/pinned) — backend-synced settings are out of scope for v1 (see Planning Log WI-01).

### Step 3.1: Add the search-history persistence hook

Files:
* libris-maleficarum-app/src/hooks/useSearchHistory.ts - Per-world `localStorage` for: search history (cap 8, MRU dedupe), recently opened entities (cap 8, MRU dedupe, denormalized id/worldId/name/entityType), pinned entities (cap 5, block-when-full). Keys follow naming discipline: `lm.search.history.{worldId}`, `lm.search.recent.{worldId}`, `lm.search.pinned.{worldId}`.

Success criteria:
* Caps enforced; MRU dedupe correct; pin returns a "full" signal at cap 5; mirrors `useTheme.ts` localStorage convention; SSR/quota-safe try/catch.

Context references:
* libris-maleficarum-app/src/hooks/useTheme.ts - localStorage convention precedent.
* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md ("Configuration Examples" per-world keys; caps 8/8/5).

Dependencies:
* None.

### Step 3.2: Test the persistence hook

Files:
* libris-maleficarum-app/src/hooks/useSearchHistory.test.ts (co-located) - Assert caps, MRU dedupe, pin-full behavior, per-world key isolation.

Success criteria:
* Tests pass; cover cap and dedupe edge cases.

Context references:
* AGENTS.md (Vitest test pattern).

Dependencies:
* Step 3.1.

### Step 3.3: Validate phase changes

Validation commands:
* `pnpm test src/hooks/useSearchHistory.test.ts` (cwd libris-maleficarum-app) - scoped test run.

## Implementation Phase 4: GlobalSearch UI Component

<!-- parallelizable: false -->

Depends on Phases 2 and 3 (consumes the hooks). New component directory plus the shadcn `command` install.

### Step 4.1: Install the shadcn command (cmdk) component

Files:
* libris-maleficarum-app/src/components/ui/command.tsx - Generated by `pnpm dlx shadcn@latest add command` (cwd libris-maleficarum-app).
* libris-maleficarum-app/package.json / pnpm-lock.yaml - `cmdk` dependency added.

Success criteria:
* `command` component present; `popover`/`input`/`scroll-area`/`badge`/`tooltip`/`skeleton` already present (confirmed).

Context references:
* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md ("Configuration Examples" shadcn add command; "Code Search Results" component inventory).

Dependencies:
* None (CLI install).

### Step 4.2: Add XSS-safe highlight component

Files:
* libris-maleficarum-app/src/components/TopToolbar/GlobalSearch/HighlightedText.tsx - Highlight matched query substrings without `dangerouslySetInnerHTML` (split-on-match, render `<mark>` spans).

Success criteria:
* No raw HTML injection; highlight contrast ≥ 3:1; case-insensitive match.

Context references:
* .github/instructions/accessibility.instructions.md (contrast).

Dependencies:
* Step 4.1.

### Step 4.3: Add the result row

Files:
* libris-maleficarum-app/src/components/TopToolbar/GlobalSearch/SearchResultRow.tsx - Entity icon (`getEntityIcon`) + highlighted name + type label (`ENTITY_TYPE_META[...].label`) + breadcrumb/metadata; hover-revealed pin and edit affordances with accessible names ("Pin {name}", "Edit {name}").

Success criteria:
* Icons/labels match sidebar parity; hover actions keyboard-reachable; voice-access labels include the visible name.

Context references:
* libris-maleficarum-app/src/lib/entityIcons.ts (`getEntityIcon`); src/services/config/entityTypeRegistry.ts (`ENTITY_TYPE_META`).
* libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.tsx (hover Pencil → edit precedent).

Dependencies:
* Steps 4.1, 4.2.

### Step 4.4: Add the empty-state panel

Files:
* libris-maleficarum-app/src/components/TopToolbar/GlobalSearch/SearchEmptyState.tsx - On empty focus, render pinned entities + recently opened + recent searches from `useSearchHistory`.

Success criteria:
* Sections labeled; empty sub-states handled; selecting a recent search refills the input.

Context references:
* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md (Technical Scenario empty-focus state).

Dependencies:
* Steps 4.1, 4.3, Phase 3.

### Step 4.5: Add the GlobalSearch shell

Files:
* libris-maleficarum-app/src/components/TopToolbar/GlobalSearch/GlobalSearch.tsx - `Popover` + `Command` (`shouldFilter={false}`), `PopoverAnchor` on the input, open-on-focus, suppress `onOpenAutoFocus` (caret stays in input), Escape closes. States: disabled when no `selectedWorldId` (hint), skeleton on `isInitialLoading`, dimmed prior rows on `isRefreshing` (SWR), `aria-live="polite"` count region, `role="alert"` error, `aria-busy` while fetching. Wire result select → open + expand; pin/record into history.

Success criteria:
* Combobox a11y from cmdk (`aria-activedescendant`, arrow/Enter/Escape); non-modal panel overlays content; world-scoped gate enforced.

Context references:
* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md (Technical Scenario "Implementation Details"; cmdk `shouldFilter={false}`).
* .github/instructions/accessibility.instructions.md (combobox pattern, aria-live).

Dependencies:
* Steps 4.1-4.4, Phases 2 and 3.

### Step 4.6: Add the barrel export

Files:
* libris-maleficarum-app/src/components/TopToolbar/GlobalSearch/index.ts - Re-export `GlobalSearch`.

Success criteria:
* Single import path `@/components/TopToolbar/GlobalSearch`.

Dependencies:
* Step 4.5.

### Step 4.7: Add the entity-type filter control

Files:
* libris-maleficarum-app/src/components/TopToolbar/GlobalSearch/SearchTypeFilter.tsx - A compact entity-type filter (chip/dropdown) that sets the `entityType` arg consumed by `useEntitySearch`; default "All types". Reuse `ENTITY_TYPE_META` labels/icons.
* libris-maleficarum-app/src/components/TopToolbar/GlobalSearch/GlobalSearch.tsx - Hold selected `entityType` state and pass it into the search hook; clearing the filter returns to "All types".

Discrepancy references:
* Addresses DR-05 (entity-type filter affordance for user requirement 2).

Success criteria:
* Selecting a type narrows results via the existing `entityType` query param; "All types" clears it; the control is keyboard-operable with an accessible name; results may additionally group by type per research UX.

Context references:
* libris-maleficarum-app/src/services/config/entityTypeRegistry.ts (`ENTITY_TYPE_META`).
* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md ("Scope and Success Criteria" entityType param; Technical Scenario "grouped by entity type").

Dependencies:
* Steps 4.5, 2.4.

## Implementation Phase 5: TopToolbar Integration and Tests

<!-- parallelizable: false -->

Depends on Phase 4. Edits the shared `TopToolbar.tsx` and adds component/a11y tests.

### Step 5.1: Insert GlobalSearch into TopToolbar

Files:
* libris-maleficarum-app/src/components/TopToolbar/TopToolbar.tsx - Render `<GlobalSearch />` before the `ml-auto` action cluster (line ~44); read `selectedWorldId` via `useAppSelector(selectSelectedWorldId)` to gate the search.

Success criteria:
* Search box visible in the `h-14` header, left of ThemeToggle/NotificationBell/UserMenu; layout unbroken at `sm`/`md`/`lg`.

Context references:
* libris-maleficarum-app/src/components/TopToolbar/TopToolbar.tsx (lines 21-49 header layout; `ml-auto` cluster).

Dependencies:
* Phase 4.

### Step 5.2: Wire open + expand-on-select dispatches

Files:
* libris-maleficarum-app/src/components/TopToolbar/GlobalSearch/GlobalSearch.tsx - On select: if `result.worldId !== currentWorldId` dispatch `setSelectedWorld`; merge `result.path` into `expandedNodeIds` via `setExpandedNodes`; dispatch `setSelectedEntity(result.id)`; record query + opened entity. Edit affordance dispatches `openEntityFormEdit`.

Success criteria:
* Selecting a deep entity expands all ancestors and opens it in MainPanel view; edit opens the edit form; recent/history updated.

Context references:
* libris-maleficarum-app/src/store/worldSidebarSlice.ts (`setSelectedWorld`, `setSelectedEntity`, `setExpandedNodes`, `openEntityFormEdit`; selectors `selectExpandedNodeIds`).
* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md ("Complete Examples" openSearchResult).

Dependencies:
* Step 5.1.

### Step 5.3: Component and accessibility tests

Files:
* libris-maleficarum-app/src/components/TopToolbar/GlobalSearch/GlobalSearch.test.tsx - Wrap in `<Provider store={store}>`; assert: opens on focus, debounced query fires (MSW), results render with icons, keyboard nav (arrow/Enter), select opens entity + expands ancestors, pin cap behavior, disabled-without-world hint; `await axe(container)` no violations.

Success criteria:
* All tests pass; jest-axe clean; keyboard interaction covered.

Context references:
* AGENTS.md (Vitest + Testing Library + jest-axe template).
* libris-maleficarum-app/src/__tests__/ (existing a11y + interaction test precedents).

Dependencies:
* Steps 5.1, 5.2, Phase 2 MSW handler.

## Implementation Phase 6: Validation

<!-- parallelizable: false -->

### Step 6.1: Run full project validation

Execute:
* `pnpm lint` (cwd libris-maleficarum-app)
* `pnpm type-check` (cwd libris-maleficarum-app)
* `pnpm build` (cwd libris-maleficarum-app)
* `pnpm test` (cwd libris-maleficarum-app)
* `dotnet build LibrisMaleficarum.slnx` + `service: test (unit)` (only if Phase 1 backend changes were made)
* `markdown: lint` if any docs/design/*.md changed (Phase 1)

### Step 6.2: Fix minor validation issues

Iterate on lint errors, type errors, and failing tests. Apply isolated fixes directly.

### Step 6.3: Report blocking issues

Document any failure requiring changes beyond minor fixes; provide next steps and recommend additional research rather than inline large-scale fixes.

## Dependencies

* Node.js 20.x + pnpm (frontend).
* .NET 10 SDK (only if Phase 1 backend changes are made).
* shadcn CLI (`pnpm dlx shadcn@latest add command`) → adds `cmdk`.
* Existing RTK Query + axios stack (auth + cancellation already solved).

## Success Criteria

* World-scoped active search box in TopToolbar consuming `GET /api/v1/worlds/{worldId}/search` with debounce/min-length/cancellation/caching.
* Floating non-modal results panel: live results when typing; pinned + recent items + recent searches when empty.
* Result select opens the entity in MainPanel (view) and expands its hierarchy ancestors; hover reveals edit; pinning capped at 5.
* Per-world `localStorage` persistence for history/recent/pinned.
* WCAG 2.2 AA combobox a11y verified via jest-axe; backend `path` projection delivered (or fallback documented); canonical search route enforces ownership.
