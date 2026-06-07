<!-- markdownlint-disable-file -->
# Search Integration Follow-ups — Subagent Research

Status: Complete

Date: 2026-06-07

Scope: Five open questions blocking the "Active Search box" frontend feature. All findings reference exact workspace-relative paths and line numbers with diff-style snippets.

---

## 1. SEARCH PROJECTION GAP — adding `path` / `depth` / `hasChildren` to search results

### What the index actually contains

The index schema is defined in `EnsureIndexExistsAsync` in
libris-maleficarum-service/src/Infrastructure/Services/AzureAISearchService.cs (lines 56-84).

Relevant fields already in the index:

```csharp
// libris-maleficarum-service/src/Infrastructure/Services/AzureAISearchService.cs:70-71
new SimpleField("path", SearchFieldDataType.Collection(SearchFieldDataType.String)) { IsFilterable = true },
new SimpleField("depth", SearchFieldDataType.Int32) { IsFilterable = true, IsSortable = true, IsFacetable = true },
```

Confirmed:

- `path` IS indexed — typed `Collection(Edm.String)` (a list of ancestor GUID strings).
- `depth` IS indexed — typed `Edm.Int32`.
- `hasChildren` is **NOT** in the index schema. There is no `hasChildren` field anywhere in `EnsureIndexExistsAsync`.

The index-document model carries the same two fields:
libris-maleficarum-service/src/Domain/Models/SearchIndexDocument.cs (lines 61-67):

```csharp
/// <summary>Gets the ancestor IDs for hierarchy queries.</summary>
public required List<string> Path { get; init; }

/// <summary>Gets the hierarchy depth level.</summary>
public required int Depth { get; init; }
```

There is NO `HasChildren` property on `SearchIndexDocument`.

The change-feed mapper that populates the index confirms only `Path`/`Depth` are synced:
libris-maleficarum-service/src/Infrastructure/Services/SearchIndexSyncService.cs (lines 497-498, inside `MapToSearchDocument`):

```csharp
Path = entity.Path?.Select(g => g.ToString()).ToList() ?? [],
Depth = entity.Depth,
```

`MapToSearchDocument` never reads `entity.HasChildren` even though the domain entity has it
(libris-maleficarum-service/src/Domain/Entities/WorldEntity.cs:65 `public bool HasChildren { get; private set; }`).

### Why the projection is "missing" today

The search read path explicitly selects a fixed field set that excludes `path` and `depth`:
libris-maleficarum-service/src/Infrastructure/Services/AzureAISearchService.cs (line 254, inside `SearchAsync`):

```csharp
Select = { "id", "worldId", "entityType", "name", "description", "tags", "parentId", "ownerId", "createdAt", "updatedAt" }
```

The `SearchResult` projection loop (lines 296-313) likewise maps only those 10 fields and never sets `Path`/`Depth`.

### Required minimal changes

#### 1a. `AzureAISearchService.SearchAsync` — add fields to `Select` and the projection

libris-maleficarum-service/src/Infrastructure/Services/AzureAISearchService.cs line 254:

```diff
-            Select = { "id", "worldId", "entityType", "name", "description", "tags", "parentId", "ownerId", "createdAt", "updatedAt" }
+            Select = { "id", "worldId", "entityType", "name", "description", "tags", "parentId", "ownerId", "createdAt", "updatedAt", "path", "depth" }
```

libris-maleficarum-service/src/Infrastructure/Services/AzureAISearchService.cs lines 296-313 (the `results.Add(new SearchResult { ... })` block):

```diff
             results.Add(new SearchResult
             {
                 Id = Guid.Parse(doc.Id),
                 Name = doc.Name,
                 EntityType = doc.EntityType,
                 DescriptionSnippet = doc.Description is { Length: > 200 }
                     ? doc.Description[..200] + "..."
                     : doc.Description,
                 RelevanceScore = result.Score ?? 0,
                 WorldId = Guid.Parse(doc.WorldId),
                 ParentId = string.IsNullOrEmpty(doc.ParentId) ? null : Guid.Parse(doc.ParentId),
                 Tags = doc.Tags,
                 OwnerId = doc.OwnerId,
                 CreatedAt = doc.CreatedAt,
-                UpdatedAt = doc.UpdatedAt
+                UpdatedAt = doc.UpdatedAt,
+                Path = doc.Path?.Select(Guid.Parse).ToList() ?? [],
+                Depth = doc.Depth
             });
```

Note: `doc` is `SearchIndexDocument`, whose `Path` is `List<string>` (ancestor GUID strings) and `Depth` is `int`. The conversion `doc.Path.Select(Guid.Parse)` mirrors the inverse of the sync-time `entity.Path.Select(g => g.ToString())` at SearchIndexSyncService.cs:497. If the frontend prefers raw strings, keep `Path` as `List<string>` on `SearchResult` instead (decision noted under clarifying questions).

#### 1b. `SearchResult` domain model — add `Path` and `Depth`

libris-maleficarum-service/src/Domain/Models/SearchResult.cs — append after `UpdatedAt` (currently ends at line 62):

```diff
     /// <summary>
     /// Gets the last modification timestamp.
     /// </summary>
     public required DateTimeOffset UpdatedAt { get; init; }
+
+    /// <summary>
+    /// Gets the ancestor entity identifiers (root-to-parent order) for hierarchy expansion.
+    /// </summary>
+    public required List<Guid> Path { get; init; }
+
+    /// <summary>
+    /// Gets the hierarchy depth level (root = 0).
+    /// </summary>
+    public required int Depth { get; init; }
 }
```

If you do not want to break existing test construction sites, make them non-`required` (`public List<Guid> Path { get; init; } = [];` and `public int Depth { get; init; }`). `required` is stricter and matches the existing style of the model, but forces every `new SearchResult { ... }` to set them.

#### 1c. API response item — add `path` and `depth`

libris-maleficarum-service/src/Api/Models/Responses/SearchResultResponse.cs — `SearchResultItem` class, append after `UpdatedAt` (line 76):

```diff
     /// <summary>
     /// Gets or sets the last modification timestamp.
     /// </summary>
     public DateTimeOffset UpdatedAt { get; set; }
+
+    /// <summary>
+    /// Gets or sets the ancestor entity identifiers (root-to-parent order).
+    /// </summary>
+    public required List<Guid> Path { get; set; }
+
+    /// <summary>
+    /// Gets or sets the hierarchy depth level (root = 0).
+    /// </summary>
+    public int Depth { get; set; }
 }
```

#### 1d. API mapping — both controllers

The `SearchResult -> SearchResultItem` projection is duplicated in BOTH controllers (this duplication is itself part of item 2). Both must be updated.

libris-maleficarum-service/src/Api/Controllers/WorldsController.cs lines 196-209 (inside `SearchEntities`):

```diff
             Data = resultSet.Results.Select(r => new SearchResultItem
             {
                 Id = r.Id,
                 Name = r.Name,
                 EntityType = r.EntityType,
                 DescriptionSnippet = r.DescriptionSnippet,
                 RelevanceScore = r.RelevanceScore,
                 WorldId = r.WorldId,
                 ParentId = r.ParentId,
                 Tags = r.Tags,
                 OwnerId = r.OwnerId,
                 CreatedAt = r.CreatedAt,
-                UpdatedAt = r.UpdatedAt
+                UpdatedAt = r.UpdatedAt,
+                Path = r.Path,
+                Depth = r.Depth
             }).ToList(),
```

libris-maleficarum-service/src/Api/Controllers/WorldEntitiesController.cs lines ~289-302 (identical block inside `SearchEntities`):

```diff
             Data = resultSet.Results.Select(r => new SearchResultItem
             {
                 Id = r.Id,
                 Name = r.Name,
                 EntityType = r.EntityType,
                 DescriptionSnippet = r.DescriptionSnippet,
                 RelevanceScore = r.RelevanceScore,
                 WorldId = r.WorldId,
                 ParentId = r.ParentId,
                 Tags = r.Tags,
                 OwnerId = r.OwnerId,
                 CreatedAt = r.CreatedAt,
-                UpdatedAt = r.UpdatedAt
+                UpdatedAt = r.UpdatedAt,
+                Path = r.Path,
+                Depth = r.Depth
             }).ToList(),
```

(Consolidating the two endpoints — item 2 — removes one of these blocks.)

### `hasChildren` — cannot be returned from search without indexing it

`hasChildren` is a real domain concept (WorldEntity.cs:65 `HasChildren`, set via `SetHasChildren` at line 391) and is exposed on `EntityResponse`
(libris-maleficarum-service/src/Client/Api/Models/EntityResponse.cs:61 `public required bool HasChildren { get; init; }`),
but it is **not** present in:

- the search index schema (`EnsureIndexExistsAsync`),
- `SearchIndexDocument`,
- the sync mapper `MapToSearchDocument`.

Therefore search results **cannot** return `hasChildren` as-is. To add it, ALL of the following would be required:

1. Index schema — add a filterable simple field in `EnsureIndexExistsAsync` (AzureAISearchService.cs ~line 71):

   ```csharp
   new SimpleField("hasChildren", SearchFieldDataType.Boolean) { IsFilterable = true, IsFacetable = true },
   ```

2. `SearchIndexDocument` (SearchIndexDocument.cs) — add `public required bool HasChildren { get; init; }`.
3. `MapToSearchDocument` (SearchIndexSyncService.cs ~line 498) — add `HasChildren = entity.HasChildren,`.
4. `SearchAsync` `Select` (AzureAISearchService.cs:254) — add `"hasChildren"`, and set it in the `SearchResult` projection.
5. `SearchResult` + `SearchResultItem` — add the property + mapping.
6. **Re-index** — because `CreateOrUpdateIndexAsync(..., allowIndexDowntime: true)` is called on startup (AzureAISearchService.cs:132), adding a new field is non-breaking, but existing documents will have `hasChildren = false/null` until they are re-synced through the change feed. A full re-index (touch all entities or reset the lease) is needed for accurate values.

Important data-integrity caveat: `WorldEntity.HasChildren` is maintained by the repository after child-count checks (`SetHasChildren`, WorldEntity.cs:388-393). The change-feed mapper rebuilds entities via `TryMapToWorldEntity` (SearchIndexSyncService.cs:402+) using `WorldEntity.Create(...)`, which initializes `HasChildren = false` (WorldEntity.cs:206). The reconstructed entity does **not** read `hasChildren` from the Cosmos JSON, so even after wiring it through, the synced value would be wrong unless `TryMapToWorldEntity` is also updated to parse the persisted `hasChildren` field. This is a meaningful extra change, not a one-liner.

Recommendation: For the Active Search box, return `path` + `depth` only. The frontend can expand the sidebar hierarchy using `path` (ancestor chain) without `hasChildren`; a node that appears in someone's `path` is by definition a parent. Defer `hasChildren` indexing unless a concrete UI need emerges.

---

## 2. CANONICAL SEARCH ENDPOINT

### The two endpoints

| Route | Controller | Ownership check | Documented in api.md |
| --- | --- | --- | --- |
| `GET /api/v1/worlds/{worldId}/search` | WorldsController | **NO** | **YES** |
| `GET /api/v1/worlds/{worldId}/entities/search` | WorldEntitiesController | **YES** | No |

- WorldsController route prefix: `[Route("api/v1/worlds")]` (WorldsController.cs:17) + `[HttpGet("{worldId:guid}/search")]` (line 115). Method `SearchEntities` spans lines 120-220.
- WorldEntitiesController route prefix: `[Route("api/v1/worlds/{worldId:guid}/entities")]` (WorldEntitiesController.cs:19) + `[HttpGet("search")]` (line 214). Method `SearchEntities` spans lines 219-305.

### Ownership gap in WorldsController

WorldsController.`SearchEntities` validates only the query string and filters, then calls `_searchService.SearchAsync` directly (WorldsController.cs:120-195). It performs NO world-existence or owner check — despite its XML doc claiming `403 Forbidden` / `404 Not Found` responses (lines 112-114) and the `[ProducesResponseType(... 403/404)]` attributes (lines 118-119). This is a real authorization bug: any authenticated user can search any world's entities.

WorldsController already injects the dependencies needed to fix it:

- `IWorldRepository _worldRepository` (line 20)
- `IUserContextService _userContextService` (line 22)

### Exact ownership-check code to port (from WorldEntitiesController)

This is the verbatim block from WorldEntitiesController.cs lines 238-263 that WorldsController is missing:

```csharp
var userId = await _userContextService.GetCurrentUserIdAsync();
var world = await _worldRepository.GetByIdAsync(worldId, cancellationToken);

if (world is null)
{
    return NotFound(new ErrorResponse
    {
        Error = new ErrorDetail
        {
            Code = "NOT_FOUND",
            Message = $"World {worldId} not found."
        }
    });
}

if (!string.Equals(world.OwnerId, userId, StringComparison.Ordinal))
{
    return StatusCode(StatusCodes.Status403Forbidden, new ErrorResponse
    {
        Error = new ErrorDetail
        {
            Code = "FORBIDDEN",
            Message = "Access denied."
        }
    });
}
```

(For style parity with the rest of WorldsController, note that `GetWorld` uses error code `WORLD_NOT_FOUND` and message `$"World with ID '{worldId}' was not found."` at WorldsController.cs:280-281 — pick one convention; the WorldEntitiesController uses `NOT_FOUND`.)

### Recommendation

Consolidate to a single canonical endpoint. Two viable options:

- **Option A (recommended) — keep the documented path, port the security.** Make `GET /api/v1/worlds/{worldId}/search` (WorldsController) canonical because it is the one documented in docs/design/api.md:109. Insert the ownership-check block above into WorldsController.`SearchEntities` immediately after the empty-query guard (after WorldsController.cs:136), then DELETE the duplicate `[HttpGet("search")]` action from WorldEntitiesController (lines 207-305). No frontend search client exists yet (see §3 / §4 — `grep` of libris-maleficarum-app/src found no `/search` caller), so removing the `/entities/search` route breaks nothing client-side.

- **Option B — keep `/entities/search`, update docs.** Delete WorldsController.`SearchEntities`, keep the already-secure WorldEntitiesController action, and change docs/design/api.md:109 to `GET /api/v1/worlds/{worldId}/entities/search`. This keeps search grouped with entity routes but diverges from the currently-published API contract.

Option A is preferred: it preserves the documented contract, fixes the auth bug, and removes the unsecured duplicate. Whichever is chosen, the duplicated `SearchResult -> SearchResultItem` projection (item 1d) collapses to one site.

---

## 3. VITE DEV PROXY

`/api` IS proxied to the backend in dev. Config lives in libris-maleficarum-app/vite.config.ts `server.proxy` (lines 84-135).

Verbatim proxy config:

```ts
server: {
  proxy:
    process.env.VITE_API_BASE_URL === 'http://localhost:5000'
      ? {} // Empty proxy config lets MSW intercept directly
      : {
          '/api/v1': {
            target:
              process.env.services__api__http__0 ||
              process.env.services__api__https__0 ||
              process.env.VITE_API_BASE_URL ||
              'http://localhost:5077', // Point to Aspire backend
            changeOrigin: true,
            secure: false,
            // Keep /api/v1 prefix - backend expects it
            rewrite: (path) => path,
          },
          '/api/config': {
            target:
              process.env.services__api__http__0 ||
              process.env.services__api__https__0 ||
              process.env.VITE_API_BASE_URL ||
              'http://localhost:5077',
            changeOrigin: true,
            secure: false,
            rewrite: (path) => path,
          },
        },
},
```

Key facts:

- Proxied path prefixes: `/api/v1` and `/api/config`. The search endpoints (`/api/v1/worlds/{worldId}/search` and `/api/v1/worlds/{worldId}/entities/search`) both match `/api/v1` — **no proxy change is required** for the Active Search box.
- Target resolution order: `services__api__http__0` (Aspire HTTP) → `services__api__https__0` (Aspire HTTPS) → `VITE_API_BASE_URL` → fallback `http://localhost:5077`. HTTP is intentionally preferred to avoid Aspire dev-cert TLS issues.
- `secure: false` — accepts self-signed certs when the target is HTTPS.
- `changeOrigin: true` — rewrites the Host header to the target.
- `rewrite: (path) => path` — the `/api/v1` prefix is preserved (backend routes include `/api/v1`).
- MSW bypass: when `VITE_API_BASE_URL === 'http://localhost:5000'`, the proxy is disabled (`{}`) so MSW intercepts requests in-browser.

How the AppHost exposes the API URL: vite.config.ts relies on Aspire's service-discovery env-var convention `services__<servicename>__<protocol>__<index>`. For the service named `api`, Aspire injects `services__api__http__0` / `services__api__https__0` into the frontend (Node) process at launch. The same convention drives `VITE_HAS_ASPIRE_BACKEND` in the `define` block (vite.config.ts:26-28), which toggles MSW off when a real backend is present.

vitest.config.ts (read in full) defines NO proxy and NO server block — it only sets the jsdom test env, `@` alias, and excludes Playwright specs. Tests use MSW, not the proxy.

Conclusion: Dev proxy is present and already covers the search routes. Nothing to add.

---

## 4. GLOBAL HOTKEY CONFLICTS

A full scan of libris-maleficarum-app/src for `keydown`, `keyup`, `addEventListener`, `metaKey`, `ctrlKey`, `hotkey`, `onKeyDown`, and `key === 'k'/'K'` found **NO global keyboard handler and NO existing ⌘K / Ctrl-K binding**. There is no hotkey hook (no `useHotkey`/`useHotkeys`).

Every keyboard handler found is a **local React `onKeyDown`** scoped to a specific element (not a `window`/`document` listener):

- libris-maleficarum-app/src/components/ChatPanel/ChatPanel.tsx:78 — `onKeyDown={(e) => e.key === 'Enter' && handleSend()}` (Enter to send, scoped to the chat input).
- libris-maleficarum-app/src/components/shared/AccessCodeDialog/AccessCodeDialog.tsx:47 `handleKeyDown` (Enter to submit, scoped to input, wired at line 89); line 64 `onEscapeKeyDown={(e) => e.preventDefault()}` (Radix Dialog — Escape is suppressed for this modal).
- libris-maleficarum-app/src/components/shared/TagInput/TagInput.tsx:143 `handleKeyDown` (Enter/comma/Backspace for tag editing, wired at line 234).
- libris-maleficarum-app/src/components/WorldSidebar/EntityTree.tsx:60 `handleKeyDown` (arrow-key tree navigation, wired at line 144).
- libris-maleficarum-app/src/components/WorldSidebar/EntityTreeNode.tsx:63 `handleKeyDown` + line 75 `handleExpandKeyDown` (tree node activation/expand, wired at lines 112/121).

The only `window.addEventListener` usages are non-keyboard:

- libris-maleficarum-app/src/components/MainPanel/WorldEntityForm.tsx:172 — `beforeunload` (unsaved-changes guard).
- libris-maleficarum-app/src/components/MainPanel/WorldDetailForm.tsx:95 — `beforeunload`.
- libris-maleficarum-app/src/hooks/useTheme.ts:49 — `media.addEventListener('change', ...)` (prefers-color-scheme).

No NotificationCenter keyboard handler was found.

Conclusion: Adding a global `Ctrl/Cmd+K` listener (e.g., a `useEffect` with `window.addEventListener('keydown', ...)` calling `e.preventDefault()` then focusing the search input) is **safe — no conflict**. Two minor cautions:

1. Radix Dialog/Popover components capture Escape internally and (for `AccessCodeDialog`) suppress it. A global search overlay built on Radix Dialog will get Escape-to-close for free; ensure the global `Ctrl/Cmd+K` handler does not double-handle Escape.
2. The existing local `onKeyDown` handlers (chat Enter, tag Enter, tree arrows) do not listen on `window`, so a `window`-level Ctrl/Cmd+K will not interfere with them, and they will not swallow Ctrl/Cmd+K (those handlers only act on Enter / arrows / comma / Backspace).

---

## 5. SEMANTIC RANKER WIRING

### Current state

`SearchAsync` (AzureAISearchService.cs:209-340) builds `searchOptions` with `Filter`, `Size`, `Skip`, `IncludeTotalCount`, and `Select`, then switches on `request.Mode` (Text / Vector / Hybrid) to optionally add a `VectorSearch` query (lines 256-289). It **never** sets `QueryType` or `SemanticSearch`, so Azure AI Search uses the default `QueryType.Simple` with BM25 scoring — the semantic ranker is never invoked.

The semantic configuration **does exist** in the index: `EnsureIndexExistsAsync` defines `SemanticSearch` with a single `SemanticConfiguration("semantic-config", ...)` (AzureAISearchService.cs:106-127) using `name` as title, `description`/`properties`/`systemProperties` as content fields, and `tags` as keywords. So the config named `"semantic-config"` is ready to use.

### SKU confirmation

infra/main.bicep:521-529 provisions the search service via AVM module `avm/res/search/search-service:0.12.2` with:

```bicep
sku: 'basic'
semanticSearch: 'standard'
```

`semanticSearch: 'standard'` (infra/main.bicep:529) means the semantic ranker is **enabled and billable** on this Basic service. Semantic ranking is supported on Basic and above when `semanticSearch` is set to `free` or `standard`. So the infra already supports turning on `QueryType.Semantic` — no infra change required.

### Exact code change

In `SearchAsync`, after the `Select` is built and within / after the mode switch, set the query type and semantic options. Semantic ranking applies to the text (BM25) leg, so it is only meaningful for `Text` and `Hybrid` modes (vector-only has no text query to re-rank).

libris-maleficarum-service/src/Infrastructure/Services/AzureAISearchService.cs — add to the `searchOptions` initializer (after line 254) or set after the mode switch:

```diff
         // For text and hybrid modes, use the query string; vector-only uses "*" for match-all
         var searchText = request.Mode == Domain.Models.SearchMode.Vector ? "*" : request.Query;
+
+        // Enable Azure AI Search semantic ranking for text/hybrid modes.
+        // Requires the index 'semantic-config' (see EnsureIndexExistsAsync) and a
+        // semantic-capable SKU (infra: semanticSearch: 'standard').
+        if (request.Mode != Domain.Models.SearchMode.Vector)
+        {
+            searchOptions.QueryType = SearchQueryType.Semantic;
+            searchOptions.SemanticSearch = new SemanticSearchOptions
+            {
+                SemanticConfigurationName = "semantic-config"
+            };
+        }
```

Type/namespace notes:

- `SearchQueryType.Semantic` lives in `Azure.Search.Documents.Models` (already imported at AzureAISearchService.cs:6). Note the enum is `SearchQueryType`, not `QueryType`.
- `SemanticSearchOptions` and `SemanticConfigurationName` are in `Azure.Search.Documents.Models` as well.
- The literal `"semantic-config"` should ideally be promoted to a `const` shared with `EnsureIndexExistsAsync` (which currently hard-codes the same string at line 110) to avoid drift.

Optional (richer results): semantic mode can also populate captions/answers and a re-rank score:

```csharp
searchOptions.SemanticSearch = new SemanticSearchOptions
{
    SemanticConfigurationName = "semantic-config",
    QueryCaption = new QueryCaption(QueryCaptionType.Extractive)
};
// result.SemanticSearch.RerankerScore is then available per hit (0–4 scale)
```

### Interaction with existing SearchMode (Text / Vector / Hybrid)

- `Text` → semantic ranker re-ranks the BM25 results. Best quality gain.
- `Hybrid` → semantic ranker re-ranks the textual leg; vector recall still contributes via RRF fusion. Supported and recommended.
- `Vector` → leave semantic OFF (no text query to re-rank; `searchText` is `"*"`). The guard `if (request.Mode != Vector)` handles this.

`QueryType.Semantic` is orthogonal to `VectorSearch` — they coexist (Azure supports semantic ranking over hybrid results). The `Size`/`Skip` paging still applies; semantic re-ranking operates on the top ~50 candidates returned by the initial retrieval before paging.

### Cost / latency notes

- **Cost**: Semantic ranking is billed per 1,000 queries beyond the free monthly allotment (1,000 free/month on the `free` plan tier; `standard` is metered). Every Text/Hybrid search would incur a semantic-ranking charge if enabled unconditionally.
- **Latency**: Adds tens to low-hundreds of milliseconds per query (the L2 re-ranker runs a transformer over the top candidates). For an "as-you-type" Active Search box this can matter — debounce aggressively or only apply semantic on submit.
- **Result ceiling**: Semantic re-ranking only re-orders the top ~50 documents from initial retrieval; it does not change recall.

### Recommendation

Do **not** enable semantic ranking unconditionally for the type-ahead Active Search box (cost + per-keystroke latency). Instead add an explicit opt-in:

- Preferred: extend the request with a boolean/flag (e.g., `SearchRequest.UseSemanticRanking`) or a new `SearchMode.SemanticHybrid`, defaulting OFF. Apply the snippet above only when the flag is set (and mode != Vector). This keeps fast keystroke search on BM25/hybrid and reserves semantic ranking for an explicit "smart search"/submit action.
- If a single default is required, enable it only for `Hybrid` on explicit submit (not per-keystroke), and ensure the frontend debounces.

Infra is already correct (`semanticSearch: 'standard'`), so this is a code-only change.

---

## Summary checklist

- [x] §1 `path`/`depth` are indexed; add to `Select`, `SearchResult`, `SearchResultItem`, and both controller mappings (diffs above).
- [x] §1 `hasChildren` is NOT indexed; returning it requires index field + `SearchIndexDocument` + `MapToSearchDocument` + `TryMapToWorldEntity` parse + re-index. Recommend deferring; use `path` for hierarchy expansion.
- [x] §2 Two endpoints; `/worlds/{worldId}/search` is documented but unsecured; `/worlds/{worldId}/entities/search` is secured. Recommend Option A: port the ownership block into WorldsController and delete the WorldEntitiesController duplicate.
- [x] §3 Vite proxy exists; `/api/v1` (and `/api/config`) → Aspire `services__api__http(s)__0` → fallback `http://localhost:5077`; `secure:false`, prefix preserved. No change needed for search.
- [x] §4 No global hotkeys / no existing ⌘K. All keyboard handlers are local React `onKeyDown`. Adding global Ctrl/Cmd+K is safe; mind Radix Escape handling.
- [x] §5 `semantic-config` exists in index but never queried. Add `QueryType = SearchQueryType.Semantic` + `SemanticSearchOptions { SemanticConfigurationName = "semantic-config" }` for non-vector modes. SKU confirmed `semanticSearch: 'standard'` (infra/main.bicep:529). Recommend opt-in flag, not per-keystroke default.

## Clarifying questions / gaps

1. `SearchResult.Path` type: return as `List<Guid>` (typed, requires parse at projection) or `List<string>` (raw, zero-cost passthrough matching the index)? The frontend hierarchy-expansion consumer should dictate this. Default assumption above: `List<Guid>` for type-safety.
2. `required` vs optional on the new `SearchResult.Path`/`Depth`: `required` is stricter and matches the model's style but forces updates to every existing `new SearchResult { ... }` construction site (incl. tests). Confirm whether breaking those is acceptable.
3. Endpoint consolidation direction (Option A vs B) needs a product/owner decision; both fix the security gap, but A preserves the published api.md contract.
4. Semantic ranking activation policy (opt-in flag vs new `SemanticHybrid` mode vs default-on-submit) — needs a decision before wiring, given cost/latency implications for as-you-type search.
