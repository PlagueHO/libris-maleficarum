<!-- markdownlint-disable-file -->
# Task Review: Active Search Box for World Entities

## Review Metadata

| Field | Value |
|-------|-------|
| **Review date** | 2026-06-07 |
| **Reviewer mode** | Task Reviewer |
| **Implementation plan** | `.copilot-tracking/plans/2026-06-07/active-search-box-world-entities-plan.instructions.md` |
| **Changes log** | `.copilot-tracking/changes/2026-06-07/active-search-box-world-entities-changes.md` |
| **Research document** | `.copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md` |
| **Details document** | `.copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md` |
| **Validation method** | Direct reviewer validation (file inspection + diagnostics + build/test/lint execution) |

## Overall Status

⚠️ **Needs Rework** — Backend hardening (Phase 1) and the core data layer are solid, but the frontend delivery has a critical contract bug, a flaky/non-isolated test, a failing lint gate, and several unimplemented plan items (pin/edit affordances, highlight, and the entire Phase 5.3 a11y test suite). The changes log and plan mark Phases 2–6 complete, but the validation gates they claim to have passed do not pass.

## Severity Summary

| Severity | Count |
|----------|-------|
| 🔴 Critical | 1 |
| 🟠 Major | 4 |
| 🟡 Minor | 6 |
| ✅ Verified-good | (see per-phase) |

## Validation Command Results

| Command | Scope | Result |
|---------|-------|--------|
| `pnpm build` (app: build) | full app | ✅ Pass — built in ~1.1s, no type errors |
| `get_errors` | changed TS + WorldsController.cs | ✅ Pass — no compile diagnostics |
| `pnpm exec vitest run src/__tests__/services/searchApi.test.tsx` | search API | ✅ Pass — 1/1 |
| `pnpm exec vitest run src/hooks/useSearchHistory.test.ts` (isolated) | persistence hook | ✅ Pass — 3/3 |
| `pnpm exec vitest run useSearchHistory.test.ts searchApi.test.tsx` (combined) | both files | 🔴 **Fail — 1 test fails** (`keeps recent search queries deduplicated and capped per world`: expected length 3, got 1) |
| `pnpm exec eslint` (changed search files) | lint | 🔴 **Fail — 2 errors** in `useSearchHistory.ts` |

## Findings

### 🔴 Critical

#### C-01 — Search `mode` value violates the backend contract; every live request returns HTTP 400

- **Evidence:**
  - [libris-maleficarum-app/src/services/searchApi.ts](libris-maleficarum-app/src/services/searchApi.ts#L7) defaults `mode = 'keyword'` and always sends it in the query params.
  - [libris-maleficarum-app/src/services/types/search.types.ts](libris-maleficarum-app/src/services/types/search.types.ts#L34) types `mode?: 'semantic' | 'keyword'`.
  - Backend [libris-maleficarum-service/src/Domain/Models/SearchRequest.cs](libris-maleficarum-service/src/Domain/Models/SearchRequest.cs#L59) `enum SearchMode { Text, Vector, Hybrid }` — only these three values.
  - [libris-maleficarum-service/src/Api/Controllers/WorldsController.cs](libris-maleficarum-service/src/Api/Controllers/WorldsController.cs#L165) `Enum.TryParse<SearchMode>(request.Mode, ...)` returns 400 `INVALID_SEARCH_MODE` for any unrecognized value.
  - Confirmed in the test run: the request log shows `params: { q: 'Cormyr', mode: 'keyword', ... }`.
- **Impact:** `mode=keyword` (and `semantic`) is rejected by the real API with `400 INVALID_SEARCH_MODE`. The search box is non-functional against the live backend. The MSW handler ignores `mode`, so unit tests pass and mask the defect (false-green). This directly contradicts the research-documented contract (`mode` = `hybrid|text|vector`, default `hybrid`) and Details Step 2.2.
- **Recommended fix:** Change the `searchApi.ts` default to `mode = 'hybrid'` and retype `SearchEntitiesArg.mode` as `'hybrid' | 'text' | 'vector'`. Re-align any `mode` references in `GlobalSearch`/`useEntitySearch` (currently none pass `mode`, so the default governs).

### 🟠 Major

#### M-01 — Persistence hook test is flaky (passes alone, fails in a multi-file run)

- **Evidence:** `useSearchHistory.test.ts` passes 3/3 in isolation but the `keeps recent search queries deduplicated and capped per world` case fails (length 1, only `Query 0`) when run together with `searchApi.test.tsx`.
- **Root cause:** [libris-maleficarum-app/src/hooks/useSearchHistory.ts](libris-maleficarum-app/src/hooks/useSearchHistory.ts#L116) performs a side effect — `writeStoredItems(...)` — **inside** the `setHistory`/`setRecentEntities`/`togglePinnedEntity` updater functions. React state updaters must be pure; impure updaters can be invoked speculatively/repeatedly, producing the observed non-deterministic accumulation under load.
- **Impact:** Non-deterministic suite; the changes log's "tests pass" claim depends on execution grouping. Will fail intermittently in CI under the full `pnpm test`.
- **Recommended fix:** Make updaters pure (compute next state only) and persist to `localStorage` in a `useEffect` keyed on the state values + `worldId` (or via an effect that mirrors each slice).

#### M-02 — Lint gate fails on the new persistence hook (Phase 6 claim is false)

- **Evidence:** `pnpm exec eslint src/hooks/useSearchHistory.ts` reports 2 errors:
  - L23 `@typescript-eslint/no-empty-object-type` — `interface PinnedEntityRecord extends RecentEntityRecord {}` ([useSearchHistory.ts](libris-maleficarum-app/src/hooks/useSearchHistory.ts#L23)).
  - L107 `react-hooks/set-state-in-effect` — `setHistory(...)` called synchronously in `useEffect` ([useSearchHistory.ts](libris-maleficarum-app/src/hooks/useSearchHistory.ts#L107)).
- **Impact:** Plan Phase 6 (`pnpm lint`) is a required gate and currently fails on files introduced by this work. Both the plan and changes log mark validation complete.
- **Recommended fix:** Replace the empty interface with `type PinnedEntityRecord = RecentEntityRecord`. Address the set-state-in-effect by deriving initial state from a key change or restructuring the world-switch reload (e.g., `useSyncExternalStore` or a keyed reset) per the React guidance the rule cites.

#### M-03 — Phase 5.3 component + accessibility tests are entirely missing

- **Evidence:** No `GlobalSearch.test.tsx` exists (only [libris-maleficarum-app/src/components/GlobalSearch/GlobalSearch.tsx](libris-maleficarum-app/src/components/GlobalSearch/GlobalSearch.tsx) is present in the directory).
- **Impact:** Plan Step 5.3 explicitly requires `jest-axe` a11y assertions plus keyboard-nav, select-opens-entity, pin-cap, and disabled-without-world coverage. This is the primary WCAG 2.2 AA verification gate for the feature and it does not exist. The component ships with zero direct test coverage.
- **Recommended fix:** Add `GlobalSearch.test.tsx` wrapped in `<Provider store={store}>` covering open-on-interaction, debounced query via MSW, result render, keyboard nav, select → `setSelectedEntity` + `setExpandedNodes`, and `await axe(container)` clean.

#### M-04 — Pin and Edit affordances are unimplemented; the "Pinned" panel is permanently unreachable

- **Evidence:** [GlobalSearch.tsx](libris-maleficarum-app/src/components/GlobalSearch/GlobalSearch.tsx#L23) destructures only `addRecentEntity, addSearchQuery` from `useSearchHistory`; `togglePinnedEntity` / `canPin` are never consumed. Result rows ([GlobalSearch.tsx](libris-maleficarum-app/src/components/GlobalSearch/GlobalSearch.tsx#L153)) render name + type only — no pin button, no hover edit action, and `openEntityFormEdit` is never dispatched.
- **Impact:** Plan Steps 4.3 and 5.2 (and the originating user requirements: "hover reveals an edit affordance", "pinning supports up to 5 entities") are unmet. Because nothing ever calls `togglePinnedEntity`, the "Pinned" idle section can never be populated — dead UI. DR-05's sibling requirement (edit-on-hover) is not addressed.
- **Recommended fix:** Add hover-revealed, keyboard-reachable Pin and Edit controls on each result row with accessible names (`Pin {name}`, `Edit {name}`); wire Pin → `togglePinnedEntity` (respect `canPin`) and Edit → `dispatch(openEntityFormEdit(...))`.

### 🟡 Minor

#### m-01 — `HighlightedText` (Step 4.2) not implemented

Result names render as plain text ([GlobalSearch.tsx](libris-maleficarum-app/src/components/GlobalSearch/GlobalSearch.tsx#L159)); the planned XSS-safe match-highlight component is absent. Functional but a stated UX requirement.

#### m-02 — Component structure deviates from plan; no barrel export

Implemented as a single [components/GlobalSearch/GlobalSearch.tsx](libris-maleficarum-app/src/components/GlobalSearch/GlobalSearch.tsx) rather than `components/TopToolbar/GlobalSearch/` with `SearchResultRow`/`SearchEmptyState`/`HighlightedText`/`SearchTypeFilter`/`index.ts` (Steps 4.3–4.7). [TopToolbar.tsx](libris-maleficarum-app/src/components/TopToolbar/TopToolbar.tsx#L8) imports the file path directly (Step 4.6 barrel omitted). Acceptable simplification, but undocumented in the changes log.

#### m-03 — Result rows lack entity icons / registry labels (Step 4.3)

`getEntityIcon` is not used; the entity type is shown as the raw enum string instead of `ENTITY_TYPE_META[...].label`, breaking sidebar parity.

#### m-04 — Error and SWR a11y states omitted (Step 4.5)

`useEntitySearch` returns `isError`, but [GlobalSearch.tsx](libris-maleficarum-app/src/components/GlobalSearch/GlobalSearch.tsx#L22) never consumes it — no `role="alert"` error surface. There is no `aria-live` polite count region and no `aria-busy`/skeleton SWR treatment. Search failures fail silently; combobox status is not announced.

#### m-05 — Open-on-focus UX deviation

Implemented as a click-to-open `Button` + `Popover` rather than the open-on-focus input with `PopoverAnchor` (caret stays in input) described in research/Details Step 4.5. Minor UX divergence from the Azure-Portal-style active search.

#### m-06 — `handleSelect` always dispatches `setSelectedWorld`

[GlobalSearch.tsx](libris-maleficarum-app/src/components/GlobalSearch/GlobalSearch.tsx#L40) dispatches `setSelectedWorld(entity.worldId)` unconditionally; research guidance only switches worlds when `result.worldId !== currentWorldId`. Harmless today (single-world scope) but an unnecessary dispatch that clears expansion before re-expanding.

## Per-Phase Validation

### Phase 1 — Backend Search API Hardening ✅ Verified

- Ownership gate (404 `WORLD_NOT_FOUND` / 403 `FORBIDDEN`) present in the canonical `WorldsController.SearchEntities` ([WorldsController.cs](libris-maleficarum-service/src/Api/Controllers/WorldsController.cs#L125)). ✓
- `Path` and `Depth` projected into `SearchResultItem` ([WorldsController.cs](libris-maleficarum-service/src/Api/Controllers/WorldsController.cs#L232)). ✓
- Duplicate `/entities/search` action removed from `WorldEntitiesController` (grep: no `SearchEntities`/`entities/search` matches). ✓
- Mode parse accepts `hybrid|text|vector`, default `Hybrid`. ✓

### Phase 2 — Frontend Search Data Layer ⚠️ Mostly verified (see C-01)

- `searchApi.injectEndpoints`, `search.types.ts`, `useEntitySearch`, `useDebouncedValue`, MSW handler all present; `searchApi.test.tsx` passes; types/build clean.
- 🔴 C-01 (mode contract) originates here.
- Note: `useEntitySearch(query, entityType)` signature and SWR flags (`isSearching`, `hasResults`) differ from the research snippet (`useEntitySearch(rawTerm, worldId)` with `isInitialLoading`/`isRefreshing`/`hasNoResults`). Functionally adequate; structural deviation only.

### Phase 3 — Per-World localStorage Persistence ⚠️ Implemented but defective

- Caps (8/8/5), MRU dedupe, per-world key isolation, and pin-full guard implemented correctly and pass in isolation.
- 🟠 M-01 (impure updaters → flaky test) and 🟠 M-02 (lint errors) originate here.

### Phase 4 — GlobalSearch UI Component ⚠️ Partial

- `command` (cmdk) installed; Popover + Command shell, entity-type filter (Step 4.7), idle history/recent/pinned panels, and live results render. Build clean.
- Missing: HighlightedText (4.2, m-01), icons/labels (4.3, m-03), pin/edit affordances (4.3, M-04), error/SWR a11y states (4.5, m-04), separate sub-components + barrel (m-02).

### Phase 5 — TopToolbar Integration and Tests ⚠️ Partial

- Step 5.1 integration verified: `<GlobalSearch />` rendered in `TopToolbar` ([TopToolbar.tsx](libris-maleficarum-app/src/components/TopToolbar/TopToolbar.tsx#L46)). ✓
- Step 5.2 open-on-select wires `setSelectedEntity` + `setExpandedNodes` (✓) but omits the edit affordance dispatch (M-04) and over-dispatches `setSelectedWorld` (m-06).
- 🟠 M-03 — Step 5.3 component/a11y tests missing entirely.

### Phase 6 — Validation 🔴 Claim not substantiated

- Build passes, but `pnpm lint` (M-02) and the combined `pnpm test` run (M-01) fail. The plan/changes mark Phase 6 complete; the gates it claims to have passed do not all pass.

## Deviations from Plan / Changes Log

- Plan and changes log mark Phases 2–6 `[x]` complete, but: lint fails, a unit test is flaky, and Steps 4.2, 4.3 (icons/pin/edit), 4.5 (a11y states), and 5.3 (tests) are unimplemented or partial.
- Component placed under `components/GlobalSearch/` instead of `components/TopToolbar/GlobalSearch/` (undocumented).
- Frontend `mode` vocabulary (`keyword`/`semantic`) diverges from the documented backend contract (`hybrid`/`text`/`vector`).

## Follow-Up Work

### Deferred from scope (pre-agreed, not defects)

- WI-01 — Backend `UserSettings` persistence for pinned/recent (research "Potential Next Research"); v1 is localStorage-only by design.
- Semantic-ranker opt-in mode; true cross-world search; `hasChildren` index field (all explicitly out of v1 scope).

### Discovered during review (require rework)

1. **C-01** Fix `mode` default/type to `hybrid|text|vector` (blocks real functionality).
2. **M-01** Make `useSearchHistory` updaters pure; persist via effect (fixes flaky test).
3. **M-02** Resolve the 2 lint errors in `useSearchHistory.ts`.
4. **M-03** Add `GlobalSearch.test.tsx` with `jest-axe` + keyboard/select coverage.
5. **M-04** Implement hover Pin + Edit affordances and wire `togglePinnedEntity` / `openEntityFormEdit`.
6. **m-01..m-06** Highlight component, result icons/labels, error/SWR a11y states, optional structure/barrel alignment, open-on-focus UX, conditional world switch.

## Reviewer Notes

- Phase 1 backend work is clean and matches the research-documented hardening (ownership + `path` projection + duplicate removal).
- The most consequential issue is C-01: the feature cannot work against the live API as shipped, and the MSW mock hides it. Recommend an integration-level assertion (or contract test) that exercises a real `mode` value to prevent future false-greens.
- M-01/M-02 share a root cause family (impurity + effect misuse in `useSearchHistory`); fixing them together is the cleanest path.
