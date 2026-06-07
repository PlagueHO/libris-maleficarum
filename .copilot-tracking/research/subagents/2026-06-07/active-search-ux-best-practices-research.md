# Active Search / Instant-Search Box — Front-End Best Practices Research

> Research date: 2026-06-07
> Scope: React 19 + TypeScript + Redux Toolkit + Shadcn/UI (Radix + cmdk) instant-search box that mirrors the
> Azure Portal global search: a floating overlay panel that opens on focus and shows live results, search history,
> recent items, and pinned items.

## Research Topics / Questions

1. Request management to avoid overloading the Search API (debounce vs throttle, min query length, cancellation,
   out-of-order responses, client caching/dedup, stale-while-revalidate).
2. RTK Query vs TanStack Query (React Query) for debounced search with caching + cancellation in a RTK app.
3. Shadcn/UI + cmdk implementation (Command, CommandDialog, Popover; disabling cmdk's built-in filtering for
   server results via `shouldFilter={false}`; floating panel that opens on focus and is keyboard navigable).
4. Accessibility (WCAG 2.2 AA): WAI-ARIA combobox pattern, roles, `aria-activedescendant` vs roving tabindex,
   `aria-expanded`, keyboard interactions, focus management, `aria-live` result-count announcements.
5. UX patterns (Azure Portal, Linear, VS Code palette, Algolia): recent/recent-items/pinned on empty focus,
   grouped results, row layout, term highlighting, loading/empty/error states, pinning (max 5) + persistence.
6. Persistence for history / recent / pinned (localStorage vs redux-persist vs backend) for a per-user, per-world list.
7. Scoping search to a single selected world while keeping API params flexible for cross-world search.

---

## Project Grounding (what already exists in this repo)

These findings change the recommendations from "generic best practice" to "what fits this codebase":

- **RTK Query is already the standard data layer.** `libris-maleficarum-app/src/services/api.ts` defines a single
  `createApi` slice (`reducerPath: 'api'`) with a **custom `axiosBaseQuery`** that already forwards the RTK Query
  `signal` (an `AbortSignal`) into Axios and already handles `ERR_CANCELED`. Cancellation plumbing exists today.
- **Store wiring** in `libris-maleficarum-app/src/store/store.ts` uses `configureStore` with `createSlice` feature
  slices (`sidePanel`, `worldSidebar`, `notifications`) plus `api.reducer` / `api.middleware`. Typed hooks
  `useAppDispatch` / `useAppSelector` are exported there.
- **Persistence convention is plain `localStorage`**, not redux-persist. See
  `libris-maleficarum-app/src/hooks/useTheme.ts` (reads/writes a `STORAGE_KEY`). There is **no** redux-persist
  dependency anywhere in the app.
- **Search endpoint already designed.** `docs/design/api.md` defines `GET /api/v1/worlds/{worldId}/search`
  ("Search entities by name/tags"). `docs/design/data_model.md` lists a **Search entities** access pattern keyed by
  `worldId` + `searchTerm` (5–14 RUs) and notes **all WorldEntities are also indexed by Azure AI Search for
  semantic ranking, hybrid search, and cross-world discovery**. So per-world is the primary path; cross-world is an
  AI Search concern.
- **Shadcn `command` component is NOT yet installed.** `file_search` found no
  `src/components/ui/command.tsx`. It must be added via `pnpm dlx shadcn@latest add command` (installs `cmdk`).
- **Accessibility is a hard requirement.** `.github/instructions/accessibility.instructions.md` mandates WCAG 2.2 AA,
  the combobox pattern, `aria-activedescendant`-based focus management for composites, visible focus, and
  `aria-live` announcements; every component test asserts a11y with `jest-axe`.

---

## 1. Request Management (avoid overloading the Search API)

### 1.1 Debounce vs throttle — use DEBOUNCE

- **Debounce** waits until the user pauses typing, then fires once. This is the correct primitive for keystroke
  search because you only care about the *final* query in a burst, not intermediate states.
- **Throttle** fires at a fixed max rate during continuous input — good for scroll/resize, wrong for search (it
  would fire mid-word and waste RUs on partial terms).
- **Recommended delay: 300 ms.** This is the widely-adopted sweet spot (Algolia's own guidance and most autocomplete
  libraries land in the 250–400 ms band; 300 ms balances perceived responsiveness against request volume). Go to
  250 ms only if the backend is very fast and cheap; do not exceed 400 ms (feels laggy).
- Sources:
  - [MDN — debounce vs throttle concepts (EventTarget / input events)](https://developer.mozilla.org/en-US/docs/Web/API/Element/input_event)
  - [Algolia — How to add debounce to an autocomplete search box](https://www.algolia.com/doc/ui-libraries/autocomplete/guides/debouncing/)
  - [web.dev — Debounce your input handlers](https://web.dev/articles/debounce-your-input-handlers)

### 1.2 Minimum query length

- **Fire on query length ≥ 2 characters** (after `.trim()`). Single-character searches are low-signal and
  high-cost (they match almost everything). Below the threshold, show history/recent/pinned instead of calling the
  API.
- Always `trim()` before measuring length and before sending (cmdk also trims values internally).

### 1.3 Request cancellation + out-of-order responses

- **Use `AbortController`.** Each new keystroke-driven request should abort the previous in-flight request. This
  both saves backend load and prevents stale responses from "winning".
- In this repo, RTK Query already passes an `AbortSignal` into `axiosBaseQuery`, and Axios supports `signal`
  natively. **When the query arg changes, RTK Query aborts the superseded request automatically** — you get
  out-of-order protection for free as long as the latest arg drives the hook.
- If you ever hand-roll a fetch (outside RTK Query), guard against races by either (a) aborting the previous
  controller, or (b) tagging each request with a sequence id and ignoring any response whose id is not the latest.
- Axios cancel-token API is **deprecated**; use `AbortController` / `signal`.
- Sources:
  - [MDN — AbortController](https://developer.mozilla.org/en-US/docs/Web/API/AbortController)
  - [Axios — Cancellation (use AbortController; CancelToken deprecated)](https://axios-http.com/docs/cancellation)
  - [RTK Query — Customizing queries (signal in baseQuery)](https://redux-toolkit.js.org/rtk-query/usage/customizing-queries)

### 1.4 Client-side caching, dedup, stale-while-revalidate

- **RTK Query dedupes and caches by `queryCacheKey`** (a serialization of the endpoint + args). Two components (or
  two keystroke bursts that resolve to the same term) requesting the same query share one in-flight request and one
  cache entry. ([RTK Query — Queries: Query Cache Keys / Avoiding unnecessary requests](https://redux-toolkit.js.org/rtk-query/usage/queries))
- **Stale-while-revalidate** is built in: `keepUnusedDataFor` (this repo: 60 s) keeps results cached after unmount,
  and `refetchOnMountOrArgChange: 30` re-validates stale entries. When a user re-types a recent term, the cached
  result renders **instantly** while a background refetch (if stale) keeps it fresh. Use `isFetching` (not
  `isLoading`) to show a subtle "refreshing" affordance without blanking the list.
- The distinction matters for UX: `isLoading` = first-ever load for this arg (show skeleton); `isFetching` = any
  in-flight request, including background revalidation (keep old rows, dim slightly).

---

## 2. RTK Query vs TanStack Query — RECOMMENDATION: **RTK Query**

**Recommendation: use RTK Query.** Rationale:

1. **It is already the app's data layer.** `src/services/api.ts` + store middleware are wired. Adding TanStack Query
   would mean two caching systems, two devtools, two mental models, and a second provider — net complexity with no
   benefit here.
2. **Cancellation is already solved** via the existing `axiosBaseQuery` `signal` passthrough and `ERR_CANCELED`
   handling.
3. **Caching, dedup, and stale-while-revalidate** (`keepUnusedDataFor`, `refetchOnMountOrArgChange`, `queryCacheKey`
   dedup) cover every requirement in section 1.4 out of the box.
4. **Debounce is orthogonal** to the data layer. You debounce the *query arg* in a small hook and feed the debounced
   value to `useSearchEntitiesQuery(debouncedTerm, { skip: term.length < 2 })`. RTK Query then handles
   request lifecycle. You do **not** need React Query's `keepPreviousData` because RTK Query's `data` already
   retains the previous arg's result while `isFetching` is true.

**When TanStack Query would win (not the case here):** if the app had no Redux store, or needed infinite/paginated
search with `keepPreviousData` + `placeholderData` ergonomics and per-query `staleTime` tuning, or wanted built-in
query-level retry/backoff without a custom baseQuery. None of these outweigh the integration cost given RTK Query is
already in place.

### 2.1 RTK Query pattern (recommended)

```typescript
// src/services/searchApi.ts
import { api } from '@/services/api';

export interface SearchResultItem {
  id: string;
  worldId: string;
  entityType: string;        // 'Character' | 'Location' | ...
  name: string;
  breadcrumb?: string;       // e.g. "Aldoria / Capital City"
  snippet?: string;          // matched-context preview from the server
}

export interface SearchEntitiesArg {
  worldId: string;
  searchTerm: string;
  // Flexible filter design — see section 7. Omit `worldId` filter for cross-world (AI Search) scope.
  scope?: 'world' | 'all';
  entityTypes?: string[];
  top?: number;
}

export const searchApi = api.injectEndpoints({
  endpoints: (build) => ({
    searchEntities: build.query<SearchResultItem[], SearchEntitiesArg>({
      query: ({ worldId, searchTerm, scope = 'world', entityTypes, top = 20 }) => ({
        url: `/api/v1/worlds/${worldId}/search`,
        method: 'GET',
        params: {
          q: searchTerm,
          scope,                                   // 'world' (default) or 'all' for cross-world
          entityTypes: entityTypes?.join(','),     // optional CSV filter
          top,
        },
      }),
      // Each distinct arg object becomes its own cache entry (queryCacheKey) — automatic dedup + SWR.
      keepUnusedDataFor: 120, // search results are cheap to keep; speeds up re-typed terms
    }),
  }),
});

export const { useSearchEntitiesQuery } = searchApi;
```

### 2.2 Debounced + cancellable search hook (drop-in)

This hook debounces the *term*; RTK Query handles fetch + abort/dedup/cache. `skip` enforces the min-length gate.

```typescript
// src/hooks/useDebouncedValue.ts
import { useEffect, useState } from 'react';

/** Returns `value` only after it has stopped changing for `delayMs`. */
export function useDebouncedValue<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const id = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(id); // cancels the pending update on each keystroke
  }, [value, delayMs]);

  return debounced;
}
```

```typescript
// src/hooks/useEntitySearch.ts
import { useMemo } from 'react';
import { useDebouncedValue } from '@/hooks/useDebouncedValue';
import { useSearchEntitiesQuery } from '@/services/searchApi';

const MIN_QUERY_LENGTH = 2;
const DEBOUNCE_MS = 300;

export function useEntitySearch(rawTerm: string, worldId: string, scope: 'world' | 'all' = 'world') {
  const term = rawTerm.trim();
  const debouncedTerm = useDebouncedValue(term, DEBOUNCE_MS);

  // Only treat the search as "active" when the debounced term meets the threshold.
  const isActiveSearch = debouncedTerm.length >= MIN_QUERY_LENGTH;

  const arg = useMemo(
    () => ({ worldId, searchTerm: debouncedTerm, scope }),
    [worldId, debouncedTerm, scope],
  );

  // RTK Query: changing `arg` aborts the prior in-flight request (out-of-order safe),
  // dedupes identical terms, and serves cached results stale-while-revalidate.
  const { data, isLoading, isFetching, isError, error } = useSearchEntitiesQuery(arg, {
    skip: !isActiveSearch,
  });

  return {
    results: data ?? [],
    // `isLoading` = first load for this term (show skeleton); `isFetching` = background revalidation (dim rows).
    isInitialLoading: isLoading,
    isRefreshing: isFetching && !isLoading,
    isError,
    error,
    // True when the user has typed something below the threshold or nothing at all -> show history/recent/pinned.
    isEmptyQuery: term.length < MIN_QUERY_LENGTH,
    hasNoResults: isActiveSearch && !isFetching && (data?.length ?? 0) === 0,
  };
}
```

> Constants `DEBOUNCE_MS = 300` and `MIN_QUERY_LENGTH = 2` are the recommended values from sections 1.1–1.2.

---

## 3. Shadcn/UI + cmdk Implementation

### 3.1 Install

```bash
# from libris-maleficarum-app/
pnpm dlx shadcn@latest add command   # adds src/components/ui/command.tsx and the cmdk dependency
pnpm dlx shadcn@latest add popover   # for the floating overlay panel
```

### 3.2 Critical: DISABLE cmdk's built-in filtering for server results

cmdk filters and sorts items **client-side** by default — it scores each `CommandItem`'s `value` against the input.
For **server-driven** results that is wrong (it would re-filter already-filtered server data and can hide valid
rows). Per the cmdk docs:

> "Filter/sort items manually? Yes. Pass `shouldFilter={false}` to Command. Better memory usage and performance."

So set **`shouldFilter={false}`** on `<Command>` (or `<CommandDialog>`) whenever results come from the API, and
render the server rows yourself. Keep client filtering **on** only for the static empty-state sections (recent /
pinned) if you render those inside a separate `<Command>` instance, or just render them as plain rows too.

Other relevant cmdk facts:
- `<CommandLoading>` (Shadcn surfaces `cmdk`'s `Command.Loading`) renders a loading affordance inside the list.
- `useCommandState((s) => s.search)` lets a child read the live input (useful for a "No results for X" empty state).
- cmdk is **accessible by design**: it manages `role="combobox"`/`listbox`/`option`, `aria-activedescendant`,
  arrow-key navigation, and `aria-selected`. Using it gives you the combobox keyboard model without hand-rolling it.
- `loop` prop makes arrow navigation wrap.
- Items need a unique `key` and `value`.
- Sources:
  - [cmdk README — `shouldFilter`, Loading, useCommandState, Popover, async results, FAQ](https://github.com/pacocoursey/cmdk)
  - [Shadcn/UI — Command component](https://ui.shadcn.com/docs/components/command)

### 3.3 Azure-Portal-style floating panel (opens on focus, overlays content)

Two viable shells:

| Shell | Behavior | Use when |
| --- | --- | --- |
| `CommandDialog` (modal) | Centered modal overlay, focus-trapped, ⌘K-style | Global "command palette" launched by shortcut |
| `Popover` + inline `Command` | Anchored dropdown under the input, **opens on focus**, non-modal | **Azure Portal global search box** (recommended here) |

For the Azure-Portal look (a box in the top toolbar whose results float beneath it), use **Popover + Command** with
the popover anchored to the input and opened on focus. Keep it **non-modal** so the page behind stays visible (Azure
Portal does not trap focus for its search box).

```tsx
// src/components/TopToolbar/GlobalSearch/GlobalSearch.tsx
'use client';

import { useId, useRef, useState } from 'react';
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
  CommandLoading,
  CommandSeparator,
} from '@/components/ui/command';
import { Popover, PopoverAnchor, PopoverContent } from '@/components/ui/popover';
import { cn } from '@/lib/utils';
import { useEntitySearch } from '@/hooks/useEntitySearch';
import { useAppSelector } from '@/store/store';
// Persistence hook from section 5:
import { useSearchHistory } from '@/hooks/useSearchHistory';

export function GlobalSearch() {
  const worldId = useAppSelector((s) => s.worldSidebar.selectedWorldId); // adjust selector to actual slice
  const [open, setOpen] = useState(false);
  const [term, setTerm] = useState('');
  const listboxId = useId();

  const {
    results,
    isInitialLoading,
    isRefreshing,
    isError,
    isEmptyQuery,
    hasNoResults,
  } = useEntitySearch(term, worldId ?? '');

  const { history, recent, pinned, recordSearch, togglePin } = useSearchHistory(worldId ?? '');

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverAnchor asChild>
        {/* The input is the combobox; cmdk wires role/aria for us. shouldFilter=false => server-driven. */}
        <div className="w-full max-w-xl">
          <Command shouldFilter={false} label="Search world" className="rounded-lg border">
            <CommandInput
              value={term}
              onValueChange={(v) => {
                setTerm(v);
                setOpen(true); // keep panel open while typing
              }}
              onFocus={() => setOpen(true)} // OPEN ON FOCUS (Azure Portal behavior)
              placeholder="Search this world…"
              aria-controls={listboxId}
            />
            <PopoverContent
              // Non-modal: do not steal focus from the input; align under it; match width.
              onOpenAutoFocus={(e) => e.preventDefault()}
              align="start"
              sideOffset={4}
              className="w-[var(--radix-popover-trigger-width)] p-0"
            >
              <CommandList id={listboxId} aria-busy={isRefreshing || isInitialLoading}>
                {/* EMPTY-QUERY STATE: history + recent + pinned */}
                {isEmptyQuery && (
                  <>
                    {pinned.length > 0 && (
                      <CommandGroup heading="Pinned">
                        {pinned.map((item) => (
                          <ResultRow key={`pin-${item.id}`} item={item} pinned onTogglePin={togglePin} />
                        ))}
                      </CommandGroup>
                    )}
                    {recent.length > 0 && (
                      <CommandGroup heading="Recent items">
                        {recent.map((item) => (
                          <ResultRow key={`recent-${item.id}`} item={item} onTogglePin={togglePin} />
                        ))}
                      </CommandGroup>
                    )}
                    {history.length > 0 && (
                      <CommandGroup heading="Recent searches">
                        {history.map((q) => (
                          <CommandItem key={`q-${q}`} value={q} onSelect={() => setTerm(q)}>
                            {q}
                          </CommandItem>
                        ))}
                      </CommandGroup>
                    )}
                  </>
                )}

                {/* ACTIVE-SEARCH STATES */}
                {!isEmptyQuery && isInitialLoading && (
                  <CommandLoading>
                    <SearchSkeletonRows />
                  </CommandLoading>
                )}

                {!isEmptyQuery && isError && (
                  <div role="alert" className="p-3 text-sm text-destructive">
                    Search failed. Try again.
                  </div>
                )}

                {!isEmptyQuery && hasNoResults && (
                  <CommandEmpty>No results for “{term.trim()}”.</CommandEmpty>
                )}

                {!isEmptyQuery && results.length > 0 && (
                  <GroupedResults
                    results={results}
                    term={term}
                    dimmed={isRefreshing}
                    onSelect={(item) => {
                      recordSearch(term, item); // persist history + recent
                      setOpen(false);
                      // navigate to item…
                    }}
                    onTogglePin={togglePin}
                  />
                )}
              </CommandList>

              {/* Screen-reader-only live region for result counts (section 4.4) */}
              <p aria-live="polite" className="sr-only">
                {isEmptyQuery
                  ? ''
                  : isRefreshing || isInitialLoading
                    ? 'Searching…'
                    : `${results.length} result${results.length === 1 ? '' : 's'}`}
              </p>
            </PopoverContent>
          </Command>
        </div>
      </PopoverAnchor>
    </Popover>
  );
}
```

> Note: group server results by `entityType` in `GroupedResults`, rendering each as a `CommandGroup` of
> `ResultRow` (`CommandItem`) elements. Because `shouldFilter={false}`, cmdk renders every row you give it and still
> provides arrow-key navigation + `aria-activedescendant`.

---

## 4. Accessibility (WCAG 2.2 AA — WAI-ARIA Combobox Pattern)

Source of truth: [W3C WAI-ARIA APG — Combobox Pattern](https://www.w3.org/WAI/ARIA/apg/patterns/combobox/) and the
repo's `.github/instructions/accessibility.instructions.md`.

### 4.1 Roles & properties

- The text input has **`role="combobox"`** (cmdk applies this to `CommandInput`).
- The popup list has **`role="listbox"`**; each result has **`role="option"`** (cmdk applies these to
  `CommandList` / `CommandItem`).
- `combobox` has **`aria-expanded`** = `true` when the popup is visible, `false` when collapsed.
- `combobox` has **`aria-controls`** pointing at the listbox id (wire it as shown — `aria-controls={listboxId}`).
- `combobox` has **`aria-autocomplete="list"`** (suggestions correspond to typed characters; cmdk sets this).
- The currently highlighted option carries **`aria-selected="true"`**; selection follows focus in the listbox.
- The combobox must have an accessible name (`aria-label="Search world"` via cmdk's `label`, or a visible `<label>`).

### 4.2 Focus management — use `aria-activedescendant` (NOT roving tabindex)

- **Recommendation: `aria-activedescendant`.** For a combobox, **DOM focus stays on the input** while the visually
  "active" option is indicated by `combobox[aria-activedescendant]` referencing the option's id. This keeps the
  caret in the text field (users keep typing) while arrow keys move the highlight. The APG explicitly specifies this
  model for listbox-popup comboboxes.
- Roving tabindex (moving real DOM focus between options) is the right model for composite widgets like menus,
  toolbars, and tree grids — **not** for an editable combobox, because moving focus out of the input would break
  typing.
- **cmdk already implements the `aria-activedescendant` model**, so adopting cmdk satisfies this requirement; do not
  re-implement it.

### 4.3 Keyboard interaction (combobox + listbox popup)

| Key | Behavior |
| --- | --- |
| Down Arrow | Open popup if closed; move highlight to next option (via `aria-activedescendant`) |
| Up Arrow | Move highlight to previous option |
| Enter | Activate the highlighted option (navigate / select); if none highlighted, run/keep search |
| Escape | Close the popup; second Escape (optional) clears the input |
| Tab | Leaves the combobox; popup descendants are excluded from the tab sequence |
| Printable chars | Type into the input (never intercept normal text-editing keys) |
| Home / End | Optional: move caret to start/end of input |

cmdk provides arrow navigation, Enter (`onSelect`), and (with `loop`) wrap-around. Wire **Escape** to close the
Popover, and **focus-on-open** suppression (`onOpenAutoFocus={(e) => e.preventDefault()}`) so focus stays in the
input.

### 4.4 Announcing results to screen readers

- Add a visually hidden **`aria-live="polite"`** region that announces the result count after each settled search
  (e.g. "7 results", "No results for orc"). Update it only when not fetching to avoid chatter. Shown in the snippet
  above (`<p aria-live="polite" className="sr-only">`).
- Use `role="alert"` for the error state so failures are announced assertively.
- Set `aria-busy` on the list while fetching.

### 4.5 Visual & contrast requirements (from accessibility.instructions.md)

- Visible focus/highlight on the active option at all times; highlight must have ≥ 3:1 contrast against adjacent
  rows.
- Text contrast ≥ 4.5:1 (≥ 3:1 for large text). Do not rely on color alone to convey the active row — cmdk's
  `data-selected` should drive a background **and** a clear visual treatment.
- Term highlighting (section 5) must not reduce contrast below thresholds (avoid pale yellow on white).

### 4.6 Accessible combobox markup (reference — what cmdk produces)

If hand-rolling (e.g., for a test fixture), the markup must look like this. Prefer cmdk over hand-rolling.

```html
<!-- Editable combobox with list autocomplete, aria-activedescendant focus model -->
<label for="world-search">Search world</label>
<input
  id="world-search"
  role="combobox"
  type="text"
  aria-expanded="true"
  aria-controls="world-search-listbox"
  aria-autocomplete="list"
  aria-activedescendant="world-search-opt-2"
  autocomplete="off"
/>
<ul id="world-search-listbox" role="listbox" aria-label="Search results">
  <li id="world-search-opt-1" role="option" aria-selected="false">Aldoria (Continent)</li>
  <li id="world-search-opt-2" role="option" aria-selected="true">Aldric the Bold (Character)</li>
  <li id="world-search-opt-3" role="option" aria-selected="false">Aldwych Keep (Location)</li>
</ul>
<p aria-live="polite" class="sr-only">3 results</p>
```

---

## 5. UX Patterns (Azure Portal / Linear / VS Code palette / Algolia)

### 5.1 Empty-focus state — history + recent items + pinned

When the box is focused but empty (or below min length), show, in this order:
1. **Pinned** (max 5) — user-curated shortcuts, always first.
2. **Recent items** — last N entities the user opened from search (most-recent-first).
3. **Recent searches** — last N query strings; selecting one re-runs the search.

This mirrors Azure Portal (recent resources + favorites), Linear, and VS Code's command palette MRU behavior.

### 5.2 Grouped results + row layout

- **Group by entity type** (Characters, Locations, Organizations, …) using `CommandGroup heading=…`. Matches the
  Azure Portal "Services / Resources / Resource groups" grouping and Algolia federated sections.
- **Row layout:** `[type icon] [title (highlighted match)] [breadcrumb / metadata, muted] [hover action: pin]`.
  Keep one line; truncate breadcrumb with ellipsis. The pin toggle appears on hover/focus and is keyboard reachable.

```tsx
// ResultRow — a single CommandItem row
function ResultRow({ item, pinned, onTogglePin, onSelect, term }: ResultRowProps) {
  return (
    <CommandItem value={item.id} onSelect={() => onSelect?.(item)} className="flex items-center gap-2">
      <EntityTypeIcon type={item.entityType} aria-hidden="true" className="size-4 shrink-0" />
      <span className="truncate">
        <HighlightedText text={item.name} query={term} />
      </span>
      {item.breadcrumb && (
        <span className="ml-1 truncate text-xs text-muted-foreground">{item.breadcrumb}</span>
      )}
      <button
        type="button"
        // Stop cmdk from treating this as item selection:
        onPointerDown={(e) => e.preventDefault()}
        onClick={(e) => {
          e.stopPropagation();
          onTogglePin(item);
        }}
        aria-pressed={pinned}
        aria-label={pinned ? `Unpin ${item.name}` : `Pin ${item.name}`}
        className="ml-auto opacity-0 group-hover:opacity-100 focus-visible:opacity-100"
      >
        <PinIcon aria-hidden="true" className="size-4" />
      </button>
    </CommandItem>
  );
}
```

### 5.3 Highlighting matched terms

- Highlight the matched substring(s) in the title. Prefer **server-provided match offsets** if the search API
  returns them (Azure AI Search can return highlights); otherwise do a safe client-side highlight that **escapes
  text** and never uses `dangerouslySetInnerHTML` with raw user/query input (XSS guard).

```tsx
function HighlightedText({ text, query }: { text: string; query: string }) {
  const q = query.trim();
  if (!q) return <>{text}</>;
  // Escape regex metacharacters in the user query before building the pattern.
  const escaped = q.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
  const parts = text.split(new RegExp(`(${escaped})`, 'ig'));
  return (
    <>
      {parts.map((part, i) =>
        part.toLowerCase() === q.toLowerCase() ? (
          <mark key={i} className="rounded-sm bg-primary/20 text-foreground">
            {part}
          </mark>
        ) : (
          <span key={i}>{part}</span>
        ),
      )}
    </>
  );
}
```

> Use `<mark>` (semantic) with a background that still meets contrast; never pale-on-white.

### 5.4 Loading / empty / error states

- **Loading (first time):** skeleton rows inside `CommandLoading` (`isInitialLoading`).
- **Refreshing (revalidation):** keep prior rows, dim them (`isRefreshing` → `opacity-60`), do **not** blank the
  list. (RTK Query `isFetching` semantics.)
- **No results:** `CommandEmpty` with the echoed query — "No results for ‘orc’."
- **Error:** `role="alert"` inline message + optional retry button.

### 5.5 Pinning (max 5) + persistence

- Cap pinned at **5**; when full, either disable further pin buttons (with a tooltip "Unpin one to add more") or
  evict the oldest. Recommended: **block + tooltip** (predictable, matches Azure favorites behavior).
- Pin state and lists persist per-user, per-world (section 6).

---

## 6. Persistence — RECOMMENDATION: **`localStorage`, keyed per world**

**Recommendation: plain `localStorage`** wrapped in a small hook, matching the existing `useTheme.ts` convention.
Rationale:

- The app already persists UI preferences with `localStorage` and has **no** redux-persist dependency; adding one
  for a search MRU list would be over-engineering.
- History/recent/pinned are **device-local convenience data**, not authoritative domain data — they don't need to
  round-trip to the backend (a future enhancement could sync pinned items server-side per user, but start local).
- A backend store would add API surface, RU cost, and latency for data that is fine to keep client-side.

Design:
- **One key per world**: `lm.search.history.{worldId}`, `lm.search.recent.{worldId}`, `lm.search.pinned.{worldId}`
  (follow the user's naming-discipline: same prefix, only the trailing segment differs). Keying by world keeps lists
  scoped to the selected world.
- **Caps**: history ≤ 8 queries, recent ≤ 8 items, pinned ≤ 5.
- **Dedupe**: when recording a query/item, remove any existing entry with the same id/string, then unshift to front
  (MRU). Trim to cap.
- **Guard JSON parse** and ignore corrupt values (like `useTheme` ignores invalid values).

```typescript
// src/hooks/useSearchHistory.ts
import { useCallback, useEffect, useState } from 'react';
import type { SearchResultItem } from '@/services/searchApi';

const MAX_HISTORY = 8;
const MAX_RECENT = 8;
const MAX_PINNED = 5;

const keys = (worldId: string) => ({
  history: `lm.search.history.${worldId}`,
  recent: `lm.search.recent.${worldId}`,
  pinned: `lm.search.pinned.${worldId}`,
});

function read<T>(key: string, fallback: T): T {
  try {
    const raw = localStorage.getItem(key);
    return raw ? (JSON.parse(raw) as T) : fallback;
  } catch {
    return fallback; // ignore corrupt values
  }
}

function write<T>(key: string, value: T): void {
  try {
    localStorage.setItem(key, JSON.stringify(value));
  } catch {
    /* storage full / disabled — fail silently */
  }
}

export function useSearchHistory(worldId: string) {
  const k = keys(worldId);
  const [history, setHistory] = useState<string[]>(() => read(k.history, []));
  const [recent, setRecent] = useState<SearchResultItem[]>(() => read(k.recent, []));
  const [pinned, setPinned] = useState<SearchResultItem[]>(() => read(k.pinned, []));

  // Re-read when the selected world changes.
  useEffect(() => {
    setHistory(read(k.history, []));
    setRecent(read(k.recent, []));
    setPinned(read(k.pinned, []));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [worldId]);

  const recordSearch = useCallback(
    (query: string, opened?: SearchResultItem) => {
      const q = query.trim();
      if (q.length >= 2) {
        setHistory((prev) => {
          const next = [q, ...prev.filter((x) => x.toLowerCase() !== q.toLowerCase())].slice(0, MAX_HISTORY);
          write(k.history, next);
          return next;
        });
      }
      if (opened) {
        setRecent((prev) => {
          const next = [opened, ...prev.filter((x) => x.id !== opened.id)].slice(0, MAX_RECENT);
          write(k.recent, next);
          return next;
        });
      }
    },
    [k.history, k.recent],
  );

  const togglePin = useCallback(
    (item: SearchResultItem) => {
      setPinned((prev) => {
        const exists = prev.some((x) => x.id === item.id);
        let next: SearchResultItem[];
        if (exists) {
          next = prev.filter((x) => x.id !== item.id);
        } else {
          if (prev.length >= MAX_PINNED) return prev; // block at cap (show tooltip in UI)
          next = [item, ...prev];
        }
        write(k.pinned, next);
        return next;
      });
    },
    [k.pinned],
  );

  return { history, recent, pinned, recordSearch, togglePin };
}
```

> Future option: mirror `pinned` to the backend per user for cross-device sync — but ship `localStorage` first.

---

## 7. Scoping to a Single World + Flexible Cross-World Params

- **Primary path is per-world.** The route is already `GET /api/v1/worlds/{worldId}/search`, and Cosmos is
  partitioned by `/worldId`, so single-world search is a cheap partition query (5–14 RUs). Drive the `worldId` from
  the selected-world Redux state (`worldSidebar` slice).
- **Keep params flexible for cross-world.** Add a `scope` query param (`'world'` default | `'all'`) plus optional
  `entityTypes` filter (CSV). When `scope='all'`, the backend routes to **Azure AI Search** (the documented
  cross-world / semantic / hybrid index) instead of a single Cosmos partition. The frontend arg shape
  (`SearchEntitiesArg` in section 2.1) already models this — the UI can offer a "Search all worlds" toggle later
  without changing the contract.
- **Filter param design (recommended):**
  - `q` — the trimmed search term (required, ≥ 2 chars).
  - `scope` — `'world' | 'all'` (default `'world'`).
  - `entityTypes` — optional CSV (`Character,Location`) for type filtering; omit for all types.
  - `top` — result cap (default 20) to bound payload + render cost.
- This honours the data-model source of truth (`docs/design/data_model.md`): per-world Cosmos path for the common
  case, AI Search for cross-world discovery, with `worldId` always present in the URL even when `scope='all'`
  (the partition is the *anchor* world; AI Search broadens beyond it).

---

## Key Decisions Summary (one recommendation each)

| Decision | Recommendation | Why |
| --- | --- | --- |
| Debounce vs throttle | **Debounce, 300 ms** | Fire once per typing pause; 300 ms is the responsiveness/load sweet spot |
| Min query length | **≥ 2 chars (after trim)** | Avoid high-cost, low-signal single-char queries |
| Cancellation | **AbortController via existing RTK Query `signal`** | Already wired in `axiosBaseQuery`; out-of-order safe on arg change |
| Caching / SWR | **RTK Query `keepUnusedDataFor` + `refetchOnMountOrArgChange`** | Built-in dedup + stale-while-revalidate |
| Data layer | **RTK Query (not React Query)** | Already the app standard; cancellation/caching solved; no second cache system |
| Component | **Shadcn `Command` (cmdk) with `shouldFilter={false}`** + Popover anchored to input | Server-driven results; combobox a11y for free; non-modal floating panel opens on focus |
| Focus model | **`aria-activedescendant`** (provided by cmdk) | Keeps caret in input while arrows move highlight; APG-specified for comboboxes |
| Result announcement | **`aria-live="polite"` count region + `role="alert"` errors** | WCAG 2.2 AA, repo a11y instructions |
| Persistence | **`localStorage`, per-world keys, caps (history 8 / recent 8 / pinned 5), MRU dedupe** | Matches `useTheme` convention; no redux-persist needed; device-local convenience data |
| Scope param | **`scope: 'world' \| 'all'` + optional `entityTypes` CSV + `top`** | Per-world Cosmos default; `'all'` → Azure AI Search; contract stays stable |

---

## Clarifying Questions / Gaps

1. **Search response shape.** `docs/design/api.md` lists the endpoint but I did not find the documented response
   body for `/search`. Does it return match highlights / offsets (for section 5.3) and a breadcrumb/path per item?
   If not, breadcrumb must be derived client-side.
2. **Selected-world selector.** Confirm the exact Redux path for the currently selected world (I assumed
   `state.worldSidebar.selectedWorldId`); the snippet's selector must match the real `worldSidebarSlice` shape.
3. **Cross-world UX priority.** Is a "Search all worlds" toggle in scope for v1, or is per-world sufficient initially
   (the param design supports both either way)?
4. **Pinned sync.** Should pinned items eventually sync to the backend per user (cross-device), or is device-local
   `localStorage` acceptable long-term?
5. **Trigger affordance.** Azure-Portal-style box (always-visible input in the toolbar) vs ⌘K command-palette modal —
   the research recommends the Popover+Command box, but confirm whether a global ⌘K shortcut should also open it.
6. **cmdk version / React 19.** cmdk requires React 18+ hooks (`useId`, `useSyncExternalStore`) and is React-18-safe;
   confirm the installed cmdk version is compatible with React 19 in this app (latest `cmdk` v1.1.x is).

---

## Sources

- [Shadcn/UI — Command](https://ui.shadcn.com/docs/components/command)
- [cmdk (pacocoursey) — README: shouldFilter, Loading, Popover, async, FAQ](https://github.com/pacocoursey/cmdk)
- [W3C WAI-ARIA APG — Combobox Pattern](https://www.w3.org/WAI/ARIA/apg/patterns/combobox/)
- [W3C WAI-ARIA APG — Managing Focus in Composites Using aria-activedescendant](https://www.w3.org/WAI/ARIA/apg/practices/keyboard-interface/#kbd_focus_activedescendant)
- [RTK Query — Queries (cache keys, dedup, isLoading vs isFetching, SWR)](https://redux-toolkit.js.org/rtk-query/usage/queries)
- [RTK Query — Customizing queries (signal/AbortSignal in baseQuery)](https://redux-toolkit.js.org/rtk-query/usage/customizing-queries)
- [RTK Query — Conditional fetching (skip)](https://redux-toolkit.js.org/rtk-query/usage/conditional-fetching)
- [MDN — AbortController](https://developer.mozilla.org/en-US/docs/Web/API/AbortController)
- [Axios — Cancellation (AbortController; CancelToken deprecated)](https://axios-http.com/docs/cancellation)
- [Algolia — Debouncing an autocomplete search box](https://www.algolia.com/doc/ui-libraries/autocomplete/guides/debouncing/)
- [web.dev — Debounce your input handlers](https://web.dev/articles/debounce-your-input-handlers)
- Repo files (workspace-relative):
  - libris-maleficarum-app/src/services/api.ts
  - libris-maleficarum-app/src/store/store.ts
  - libris-maleficarum-app/src/hooks/useTheme.ts
  - docs/design/api.md
  - docs/design/data_model.md
  - .github/instructions/accessibility.instructions.md
