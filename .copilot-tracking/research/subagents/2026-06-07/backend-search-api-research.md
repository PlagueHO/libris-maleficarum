# Backend Search API Research — World Entities

Date: 2026-06-07
Scope: `libris-maleficarum-service/` backend + related design docs and `infra/`.

## Research questions

1. Does a backend Search API for World Entities already exist?
2. What is its exact HTTP contract (route, method, request, response)?
3. What is the Azure AI Search index schema (searchable / filterable / facetable fields, semantic config, vector config)?
4. How are World Entities indexed (indexer / change feed wiring)?
5. How does the frontend authenticate / authorize to the API?
6. What are the existing API conventions (controllers, DTOs, JSON, routes, errors)?

---

## VERDICT: Does the Search API exist?

**YES — fully implemented and exposed via HTTP.** A complete, working search stack exists end to end:

- Domain interface `ISearchService` and models (`SearchRequest`, `SearchResult`, `SearchResultSet`, `SearchMode`).
- Infrastructure implementation `AzureAISearchService` that performs text / vector / hybrid search against Azure AI Search.
- Two HTTP GET endpoints (one is effectively a duplicate of the other) on existing controllers.
- A background `SearchIndexWorker` + `SearchIndexSyncService` that keeps the index populated from the Cosmos DB Change Feed.
- Index schema, semantic configuration, and vector/HNSW configuration are all defined in code (`AzureAISearchService.EnsureIndexExistsAsync`).
- Azure AI Search resource provisioned in `infra/main.bicep` (AVM `search/search-service`, basic SKU, semantic standard).

**Important caveats / gaps (see "Gaps and clarifying questions" at bottom):**

- There are **two near-identical search endpoints** for the same purpose — a naming/duplication inconsistency.
- Search is **single-world scoped only**. `WorldId` is `required` on `SearchRequest` and always injected into the filter. There is **no cross-world search**; the brief's "support cross-world filtering parameters" is **not** currently supported.
- **Authorization differs between the two endpoints**: the `entities/search` endpoint enforces world ownership; the `worlds/{worldId}/search` endpoint does **not** check ownership.
- Semantic search is **configured on the index** (`semantic-config`) but the **query path does not use semantic ranking** (`QueryType.Semantic` / `SemanticSearchOptions` are not set in `SearchAsync`). Current modes are Text, Vector (kNN), and Hybrid (text + vector) only.

---

## Endpoint contract

Two endpoints exist. Both bind the same query DTO (`SearchEntitiesRequest`) and return the same response DTO (`SearchResponse`).

### Endpoint A (recommended/aligned with design docs)

- Route: `GET /api/v1/worlds/{worldId:guid}/search`
- Source: libris-maleficarum-service/src/Api/Controllers/WorldsController.cs (attribute at line 116, method `SearchEntities` lines 121-227)
- Matches `docs/design/api.md` line 109: `GET /api/v1/worlds/{worldId}/search`
- **Does NOT enforce world ownership** (no owner check before searching).
- Validates: `q` required (400 `INVALID_SEARCH_QUERY`), `mode` must parse (400 `INVALID_SEARCH_MODE`), `entityType` must parse (400 `INVALID_ENTITY_TYPE`).

### Endpoint B (duplicate, with auth)

- Route: `GET /api/v1/worlds/{worldId:guid}/entities/search`
- Source: libris-maleficarum-service/src/Api/Controllers/WorldEntitiesController.cs (controller route `[Route("api/v1/worlds/{worldId:guid}/entities")]` line 19; attribute `[HttpGet("search")]` line 273; method `SearchEntities` lines 278-323)
- **Enforces world ownership**: loads world via `IWorldRepository`, returns 404 if missing, 403 if `world.OwnerId != currentUserId` (lines 297-315).
- Validates: `q` required (400 `VALIDATION_ERROR`). Mode/entityType parsing is lenient (falls back to hybrid / ignores unknown entity type).

### Request — query parameters (`SearchEntitiesRequest`)

Source: libris-maleficarum-service/src/Api/Models/Requests/SearchEntitiesRequest.cs (lines 7-48)

| Query param | Type | Required | Default | Notes |
| --- | --- | --- | --- | --- |
| `q` | string | yes | `""` | Search query text. Empty/whitespace → 400. |
| `mode` | string | no | `hybrid` | One of `hybrid`, `text`, `vector` (case-insensitive). |
| `entityType` | string | no | — | Parsed to `EntityType` enum (case-insensitive). |
| `tags` | string | no | — | Comma-separated; OR-combined tag filter. |
| `name` | string | no | — | Prefix match filter on `name` field. |
| `parentId` | Guid? | no | — | Filter to children of a specific parent entity. |
| `limit` | int | no | 50 | Clamped to 1..200 in the service. |
| `offset` | int | no | 0 | Clamped to >= 0 in the service. |

Note: `worldId` comes from the route, NOT the query DTO.

Example: `GET /api/v1/worlds/8f.../search?q=ancient ruins&mode=hybrid&entityType=Location&tags=dungeon,ruined&limit=20&offset=0`

### Response — `SearchResponse` (HTTP 200)

Source: libris-maleficarum-service/src/Api/Models/Responses/SearchResultResponse.cs (lines 5-103)

JSON shape (camelCase via global `JsonNamingPolicy.CamelCase`, enums as strings):

```json
{
  "data": [
    {
      "id": "guid",
      "name": "string",
      "entityType": "string",
      "descriptionSnippet": "string|null (first 200 chars + '...')",
      "relevanceScore": 0.0,
      "worldId": "guid",
      "parentId": "guid|null",
      "tags": ["string"],
      "ownerId": "string",
      "createdAt": "2026-06-07T00:00:00+00:00",
      "updatedAt": "2026-06-07T00:00:00+00:00"
    }
  ],
  "meta": {
    "totalCount": 0,
    "offset": 0,
    "limit": 50
  }
}
```

### Error responses

Both endpoints return `ErrorResponse` (envelope `{ "error": { "code", "message", "validationErrors"? } }`).
Status codes documented via `[ProducesResponseType]`: 200, 400, 403, 404.

---

## Domain layer

### `ISearchService`

Source: libris-maleficarum-service/src/Domain/Interfaces/Services/ISearchService.cs (lines 9-17)

```csharp
public interface ISearchService
{
    Task<SearchResultSet> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
}
```

### `SearchRequest` / `SearchMode`

Source: libris-maleficarum-service/src/Domain/Models/SearchRequest.cs (lines 8-76)

```csharp
public class SearchRequest
{
    public required Guid WorldId { get; init; }          // always required — single-world scope
    public required string Query { get; init; }
    public SearchMode Mode { get; init; } = SearchMode.Hybrid;
    public EntityType? EntityTypeFilter { get; init; }
    public List<string>? TagsFilter { get; init; }
    public string? NameFilter { get; init; }             // prefix match
    public Guid? ParentIdFilter { get; init; }
    public int Limit { get; init; } = 50;
    public int Offset { get; init; } = 0;
}

public enum SearchMode { Text, Vector, Hybrid }
```

### `SearchResult` / `SearchResultSet`

Source: libris-maleficarum-service/src/Domain/Models/SearchResult.cs (lines 6-92)

`SearchResult` fields: `Id`, `Name`, `EntityType`, `DescriptionSnippet?`, `RelevanceScore`, `WorldId`, `ParentId?`, `Tags`, `OwnerId`, `CreatedAt`, `UpdatedAt`.
`SearchResultSet` fields: `Results`, `TotalCount`, `Offset`, `Limit`.

---

## Infrastructure layer — `AzureAISearchService`

Source: libris-maleficarum-service/src/Infrastructure/Services/AzureAISearchService.cs

- Implements BOTH `ISearchIndexService` (index push) and `ISearchService` (query). Class declaration line 20.
- Constructor deps (lines 35-50): `SearchIndexClient`, `SearchClient`, `IEmbeddingService`, `ITelemetryService`, `IOptions<SearchOptions>`, `ILogger`. Builds `VectorIndexProfile` from options.

### `SearchAsync` query behavior (lines 207-348)

- Clamps `Limit` to 1..200 and `Offset` to >= 0 (lines 217-218).
- Always adds `worldId eq '{WorldId}'` filter first (line 221) — hard single-world scoping.
- Optional OData filters appended with ` and `:
  - `entityType eq '{value}'` (line 225)
  - tags: `(tags/any(t: t eq '{tag}') or ...)` OR-combined (lines 230-235)
  - name: `search.ismatch('{name}*', 'name')` prefix match (lines 237-240)
  - `parentId eq '{value}'` (line 242)
- `SearchOptions.Select` returns only: `id, worldId, entityType, name, description, tags, parentId, ownerId, createdAt, updatedAt` (line 254).
- `IncludeTotalCount = true` (line 253).
- Mode handling (lines 257-289):
  - `Text` → text query only, no vector.
  - `Vector` → generates embedding, kNN `VectorizedQuery` on `contentVector`, `searchText = "*"`.
  - `Hybrid` (default) → text query + vector query combined.
- OData string injection prevented via `EscapeODataString` (single-quote doubling), line 368.
- **No semantic ranker usage** in the query path (no `QueryType.Semantic`).

### Index definition — `EnsureIndexExistsAsync` (lines 53-138)

Index name from `SearchOptions.IndexName` (default `worldentity-index`).

| Field | Type | Key | Searchable | Filterable | Sortable | Facetable |
| --- | --- | --- | --- | --- | --- | --- |
| `id` | String | yes | — | no | no | — |
| `worldId` | String | — | — | yes | — | — |
| `name` | String (SearchableField) | — | yes | yes | yes | — |
| `description` | String (SearchableField) | — | yes | — | — | — |
| `tags` | Collection(String) Searchable | — | yes | yes | — | yes |
| `entityType` | String | — | — | yes | — | yes |
| `parentId` | String | — | — | yes | — | — |
| `ownerId` | String | — | — | yes | — | — |
| `createdAt` | DateTimeOffset | — | — | yes | yes | — |
| `updatedAt` | DateTimeOffset | — | — | yes | yes | — |
| `path` | Collection(String) | — | — | yes | — | — |
| `depth` | Int32 | — | — | yes | yes | yes |
| `schemaId` | String | — | — | yes | — | yes |
| `properties` | String (SearchableField) | — | yes | — | — | — |
| `systemProperties` | String (SearchableField) | — | yes | — | — | — |
| `schemaVersion` | Int32 | — | — | yes | — | — |
| `contentVector` | Collection(Single) | — | yes (vector) | — | — | — |

- Vector field: `contentVector`, dimensions = `SearchOptions.EmbeddingDimensions` (default 1536), profile `vector-profile`.
- Vector search algorithm: HNSW `hnsw-algorithm` — Cosine metric, M=4, efConstruction=400, efSearch=500 (lines 84-95).
- Vector profile `vector-profile` → `hnsw-algorithm`, optional compression from `VectorIndexProfile` (lines 97-103).
- **Semantic configuration name: `semantic-config`** (lines 106-125):
  - Title field: `name`
  - Content fields: `description`, `properties`, `systemProperties`
  - Keywords field: `tags`
- Created/updated with `CreateOrUpdateIndexAsync(allowIndexDowntime: true)` (line 132).

### Vector compression — `VectorIndexProfile`

Source: libris-maleficarum-service/src/Infrastructure/Search/VectorIndexProfile.cs (lines 18-93)
- Modes: `None`, `ScalarQuantization` (default), `BinaryQuantization` (compression names `scalar-compression` / `binary-compression`).
- Optional rescoring + query-time oversampling driven by `SearchOptions.EnableRescoring` / `DefaultOversampling`.

---

## `SearchOptions` configuration

Source: libris-maleficarum-service/src/Infrastructure/Configuration/SearchOptions.cs (lines 6-72)

| Property | Default | Notes |
| --- | --- | --- |
| `SectionName` | `"Search"` | Config section. |
| `EmbeddingDeploymentName` | `embedding` | Must match AppHost `AddModelDeployment` name. |
| `EmbeddingModelName` | `text-embedding-3-small` | — |
| `EmbeddingDimensions` | `1536` | Vector dims. |
| `IndexName` | `worldentity-index` | Confirms the brief's expected index name. |
| `MaxBatchSize` | `100` | Index batch size. |
| `ChangeFeedPollIntervalMs` | `1000` | Change feed poll interval. |
| `VectorCompression` | `ScalarQuantization` | Deploy-time decision. |
| `EnableRescoring` | `true` | Rescore compressed vectors. |
| `DefaultOversampling` | `10.0` | Query oversampling. |

---

## Indexing pipeline (how entities reach the index)

- Dedicated worker project: libris-maleficarum-service/src/Worker/SearchIndexWorker/Program.cs
  - Registers `SearchIndexClient` (Aspire `AddAzureSearchClient("aiSearch")`), derives `SearchClient` for `IndexName` (lines 36-49).
  - Registers `AzureOpenAIClient` + `EmbeddingClient` (Aspire `AddAzureOpenAIClient("embedding")`) (lines 51-66).
  - Registers `CosmosClient` with `SystemTextJsonCosmosSerializer` (camelCase) — required so Change Feed `JsonElement` populates correctly (lines 68-83).
  - Registers `IEmbeddingService` → `EmbeddingService`, `ISearchIndexService` → `AzureAISearchService` (lines 86-87).
  - Hosts `SearchIndexSyncService` (Cosmos Change Feed → AI Search) (line 96).
- `SearchIndexSyncService` (BackgroundService): libris-maleficarum-service/src/Infrastructure/Services/SearchIndexSyncService.cs
  - Calls `EnsureIndexExistsAsync` on startup (line 79).
  - Maps `WorldEntity` → `SearchIndexDocument` via `MapToSearchDocument` (lines 481-504), serializing `Properties` / `SystemProperties` as JSON strings using `System.Text.Json`.
- `SearchIndexDocument` model: libris-maleficarum-service/src/Domain/Models/SearchIndexDocument.cs (lines 6-91) — mirrors the index fields, `ContentVector` = `IReadOnlyList<float>`.
- Note: the **API** project also registers the AI Search clients and `ISearchService` (Program.cs lines 132-163), so the query path is self-contained in the API; the worker handles write/index sync.

---

## Infrastructure (Azure)

Source: infra/main.bicep
- AI Search name var `aiSearchName` line 84 (`aisrch-{environmentName}`).
- AVM module `br/public:avm/res/search/search-service:0.12.2`, lines 521-561:
  - `sku: 'basic'`, `semanticSearch: 'standard'`, `disableLocalAuth: true` (RBAC only, no API keys).
  - System-assigned managed identity; private endpoint in shared subnet; `publicNetworkAccess: 'Disabled'`.
- Private DNS zone `privatelink.search.windows.net` lines 504-509.
- The **index itself is NOT created by Bicep** — comment lines 495-496 confirm it is created at runtime by `SearchIndexSyncService.EnsureIndexExistsAsync`.
- AI Search role assignments block starts line 919.
- Connection used by Aspire is named `aiSearch` (matches `AddAzureSearchClient("aiSearch")`).

---

## Authentication / Authorization

Design doc: docs/design/authentication.md
- Three modes: Anonymous (default, identity `_anonymous`), Anonymous + Access Code, Entra ID (multi-user, partially supported).
- Access Code mode: frontend calls `GET /api/config/access-status`; sends code in `X-Access-Code` header; backend `AccessCodeMiddleware` validates (constant-time) or returns 401. Health/status endpoints exempt.

API wiring: libris-maleficarum-service/src/Api/Program.cs
- Auth mode auto-detected from `AzureAd:ClientId` (lines 44-58):
  - present → `AddMicrosoftIdentityWebApiAuthentication` (JWT bearer / Entra ID).
  - absent → default JWT bearer + `AnonymousClaimsMiddleware` injects `_anonymous` claims (lines 295-299).
- Pipeline order (lines 277-301): HttpLogging → ExceptionHandling → CORS → `AccessCodeMiddleware` → Authentication → (AnonymousClaims if single-user) → Authorization → MapControllers.
- CORS: dev policy `AllowAnyOrigin/Header/Method` (lines 218-228).
- Current user resolved via `IUserContextService.GetCurrentUserIdAsync()` (e.g. WorldEntitiesController line 246).
- Note: controllers are **not** decorated with `[Authorize]`; access is gated by middleware + per-handler ownership checks (only Endpoint B checks ownership).

---

## API conventions (for building a new/clean endpoint)

- Controllers: `[ApiController]`, attribute routing, route prefix `api/v1/...`. Examples: WorldsController.cs line 16-17, WorldEntitiesController.cs lines 18-19.
- JSON: global `System.Text.Json`, `JsonNamingPolicy.CamelCase`, `JsonStringEnumConverter` (Program.cs lines 198-205). **Newtonsoft is banned** repo-wide.
- Validation: FluentValidation (`AddValidatorsFromAssemblyContaining<Program>`), validators return `ErrorResponse` with `validationErrors` list.
- Error envelope: `ErrorResponse { Error: { Code, Message, ValidationErrors? } }`; codes are SCREAMING_SNAKE strings (`VALIDATION_ERROR`, `NOT_FOUND`, `FORBIDDEN`, `INVALID_SEARCH_QUERY`, etc.).
- Success envelopes: `ApiResponse<T>` (single + `Meta`), `PaginatedApiResponse<T>` (list + cursor `Meta`). Search uses its own `SearchResponse`/`SearchMeta` (offset/limit, not cursor).
- Global exception mapping: `DomainExceptionFilter` (registered Program.cs) + `UseExceptionHandling()` middleware.
- `[ProducesResponseType<T>]` generic attributes used for OpenAPI. OpenAPI doc served in dev (`MapOpenApi`).
- DI lifetimes: services `Scoped`, AI Search/embedding clients `Singleton`.

---

## Gaps and clarifying questions

1. **Cross-world search NOT supported.** `SearchRequest.WorldId` is `required` and always filtered. The brief asks the API to "support cross-world filtering parameters" — this does not exist today and would require: making `WorldId` optional, scoping by `ownerId` instead, and a new route (e.g. `GET /api/v1/search`). CLARIFY: is cross-world search actually required for the frontend, or is single-world sufficient for the current task?
2. **Duplicate endpoints.** `worlds/{worldId}/search` (no ownership check) and `worlds/{worldId}/entities/search` (ownership enforced) do the same thing. CLARIFY: which one is canonical? Recommend consolidating to the design-doc route `worlds/{worldId}/search` and adding the ownership check.
3. **Semantic ranking not wired into queries.** The index has `semantic-config`, but `SearchAsync` never sets `QueryType.Semantic` / `SemanticSearchOptions`. "Semantic search" in the brief is currently only vector/hybrid. CLARIFY: does the frontend need true semantic ranker results, or is hybrid (text+vector) acceptable?
4. **Pagination is offset/limit**, not cursor — differs from the entity list endpoints (cursor-based). Frontend should expect `meta.totalCount/offset/limit`.
5. **No `[Authorize]` attribute** on controllers; ownership is enforced inconsistently. If multi-world or stronger auth is added, this needs hardening.

---

## Key file references

- libris-maleficarum-service/src/Api/Controllers/WorldsController.cs (search endpoint A, lines 116-227)
- libris-maleficarum-service/src/Api/Controllers/WorldEntitiesController.cs (search endpoint B, lines 273-323)
- libris-maleficarum-service/src/Api/Models/Requests/SearchEntitiesRequest.cs
- libris-maleficarum-service/src/Api/Models/Responses/SearchResultResponse.cs
- libris-maleficarum-service/src/Domain/Interfaces/Services/ISearchService.cs
- libris-maleficarum-service/src/Domain/Models/SearchRequest.cs
- libris-maleficarum-service/src/Domain/Models/SearchResult.cs
- libris-maleficarum-service/src/Domain/Models/SearchIndexDocument.cs
- libris-maleficarum-service/src/Infrastructure/Services/AzureAISearchService.cs
- libris-maleficarum-service/src/Infrastructure/Services/SearchIndexSyncService.cs
- libris-maleficarum-service/src/Infrastructure/Search/VectorIndexProfile.cs
- libris-maleficarum-service/src/Infrastructure/Configuration/SearchOptions.cs
- libris-maleficarum-service/src/Worker/SearchIndexWorker/Program.cs
- libris-maleficarum-service/src/Api/Program.cs (DI + pipeline)
- infra/main.bicep (AI Search resource, lines 504-561)
- docs/design/api.md (line 109 search route)
- docs/design/authentication.md
