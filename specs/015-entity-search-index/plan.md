# Implementation Plan: Entity Search Index with Vector Search

**Branch**: `015-entity-search-index` | **Date**: 2026-03-07 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/015-entity-search-index/spec.md`

## Summary

Add Azure AI Search integration with vector embeddings to the existing .NET 10 backend API. This replaces the current LINQ-based `SearchService` with Azure AI Search for hybrid (text + vector) search over WorldEntity documents. The implementation includes: (1) evaluating and implementing the optimal index synchronization method between Cosmos DB and Azure AI Search, (2) generating vector embeddings via Azure AI Services (`text-embedding-3-small`), (3) exposing a search API endpoint at `GET /api/v1/worlds/{worldId}/search`, and (4) adding comprehensive observability (metrics, tracing, logging, health checks). Infrastructure changes use Bicep with Azure Verified Modules. The Aspire AppHost is extended for local development with Azure AI Search emulation/configuration.

## Technical Context

**Language/Version**: C# / .NET 10 (ASP.NET Core, EF Core with Cosmos DB provider)
**Primary Dependencies**: Azure.Search.Documents (AI Search SDK), Azure.AI.OpenAI (embedding generation), Microsoft.Extensions.AI (abstraction layer), Aspire.Hosting.Azure.Search (AppHost), OpenTelemetry
**Storage**: Azure Cosmos DB (WorldEntity container, hierarchical partition key `[/WorldId, /id]`) → Azure AI Search (vector + text index)
**Testing**: MSTest + FluentAssertions (unit), Aspire AppHost integration tests, jest-axe pattern for accessibility (N/A — backend only)
**Target Platform**: Azure Container Apps (production), Aspire AppHost (development)
**Project Type**: Backend API service (Clean Architecture: Api → Domain ← Infrastructure)
**Performance Goals**: Search queries < 2s for 1,000 entities per world; index sync < 60s; zero additional latency on write operations
**Constraints**: Entity writes must not block on indexing (fully async); Azure AI Search Basic SKU (1GB, 5 indexes); configurable embedding model
**Scale/Scope**: Hundreds of worlds, thousands of entities per world; single-world scoped queries only

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Cloud-Native Architecture | PASS | Azure AI Search + Cosmos DB with private endpoints; Bicep IaC with AVM |
| II. Clean Architecture | PASS | Domain interfaces (`ISearchService`) already exist; new `ISearchIndexService` and `IEmbeddingService` in Domain, implementations in Infrastructure |
| III. Test-Driven Development | PASS | Unit tests for services, integration tests for API endpoint and indexing pipeline |
| IV. Framework & Technology Standards | PASS | .NET 10, Aspire.NET, Azure AI Search SDK, OpenTelemetry |
| V. Developer Experience | PASS | Aspire AppHost extended with Azure AI Search; single-command startup |
| VI. Security & Privacy by Default | PASS | Private endpoints for AI Search and AI Services; no secrets in code; managed identity |
| VII. Semantic Versioning | PASS | No breaking changes to existing API; new endpoint addition |

**Infrastructure note**: The existing `infra/main.bicep` provisions AI Search at `standard` SKU. The spec clarification chose `basic` for cost. The Bicep will need to be updated to `basic` SKU, or remain at `standard` if already deployed — this is addressed in the tasks.

## Project Structure

### Documentation (this feature)

```text
specs/015-entity-search-index/
├── plan.md              # This file
├── research.md          # Phase 0: Sync method evaluation
├── data-model.md        # Phase 1: Search index schema + domain models
├── quickstart.md        # Phase 1: Developer quickstart guide
├── contracts/           # Phase 1: Search API OpenAPI contract
│   └── search-api.yaml
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
libris-maleficarum-service/
├── src/
│   ├── Api/
│   │   ├── Controllers/
│   │   │   └── WorldEntitiesController.cs  # Extended: search endpoint action
│   │   └── Models/
│   │       ├── Requests/
│   │       │   └── SearchEntitiesRequest.cs  # NEW: search request model
│   │       └── Responses/
│   │           └── SearchResultResponse.cs   # NEW: search result model
│   ├── Domain/
│   │   ├── Interfaces/
│   │   │   └── Services/
│   │   │       ├── ISearchService.cs          # MODIFIED: updated for AI Search
│   │   │       ├── ISearchIndexService.cs     # NEW: index sync abstraction
│   │   │       ├── IEmbeddingService.cs       # NEW: embedding generation
│   │   │       └── ITelemetryService.cs       # MODIFIED: search metrics
│   │   └── Models/
│   │       ├── SearchIndexDocument.cs          # NEW: search index document
│   │       ├── SearchRequest.cs                # NEW: domain search request
│   │       └── SearchResult.cs                 # NEW: domain search result
│   ├── Infrastructure/
│   │   └── Services/
│   │       ├── AzureAISearchService.cs         # NEW: Azure AI Search impl
│   │       ├── SearchIndexSyncService.cs       # NEW: index sync processor
│   │       ├── EmbeddingService.cs             # NEW: embedding generation
│   │       └── SearchService.cs                # REMOVED: replaced by AzureAISearchService (T042)
│   └── Orchestration/
│       ├── AppHost/
│       │   └── AppHost.cs                      # MODIFIED: add AI Search + AI Services
│       └── ServiceDefaults/
│           └── Extensions.cs                   # MODIFIED: search health checks + meters
├── tests/
│   ├── unit/
│   │   └── Infrastructure.Tests/
│   │       └── Services/
│   │           ├── AzureAISearchServiceTests.cs  # NEW
│   │           ├── SearchIndexSyncServiceTests.cs # NEW
│   │           ├── EmbeddingServiceTests.cs       # NEW
│   │           └── SearchServiceTests.cs          # MODIFIED
│   └── integration/
│       └── Api.IntegrationTests/
│           └── SearchApiIntegrationTests.cs       # MODIFIED: AI Search tests
└── infra/
    └── main.bicep                                 # MODIFIED: AI Search SKU + model deployment
```

**Structure Decision**: Extends the existing Clean Architecture layout. New domain interfaces in `Domain/Interfaces/Services/`, new infrastructure implementations in `Infrastructure/Services/`, new API models in `Api/Models/`. No new projects required — all code fits within the existing Api, Domain, and Infrastructure projects.

## Phase 0: Research Summary

**Output**: [research.md](research.md) — comprehensive sync method evaluation.

### Sync Method Decision: Cosmos DB Change Feed Processor (BackgroundService)

Evaluated four candidate approaches per FR-008:

| Approach | Cost | Simplicity | Reliability | Sync Lag | Observability |
|----------|------|------------|-------------|----------|---------------|
| A: Indexer + Integrated Vectorization | Medium | Medium | Good | 5-10 min | Poor |
| **B: Change Feed Processor** ★ | **Lowest** | **Good** | **Good** | **Seconds** | **Excellent** |
| C: App-Level Eventing | Medium | Poor | Medium | Seconds | Good |
| D: Change Feed + Queue | Highest | Poor | Good | Seconds | Good |

**Rationale**: Option B scores highest on all three FR-007 criteria (cost > simplicity > reliability) and uniquely meets the observability requirements (FR-022/23/24) and sync lag target (SC-001 < 60s). See [research.md](research.md) for full evaluation.

### Additional Decisions

- **AI Search SKU**: Update Bicep from `standard` to `basic` (~$75/month vs ~$250/month). Basic supports vector search, hybrid search, semantic search.
- **Embedding concatenation**: Application code in `SearchIndexSyncService`. Fields: Name + Description + Tags + Attributes.
- **Lease container**: Dedicated `leases` container in Cosmos DB (partition key `/id`, 400 RU/s).
- **Initial population**: Change Feed `StartFromBeginning()` on first deployment.
- **Query vectorization**: Application-level (EmbeddingService generates query vector).

## Phase 1: Design Artifacts

### Generated Artifacts

| Artifact | Path | Description |
|----------|------|-------------|
| Data Model | [data-model.md](data-model.md) | AI Search index schema, domain models, field mappings |
| API Contract | [contracts/search-api.yaml](contracts/search-api.yaml) | OpenAPI 3.0 spec for `GET /api/v1/worlds/{worldId}/search` |
| Quickstart | [quickstart.md](quickstart.md) | Developer setup guide, configuration, architecture diagram |

### Key Design Decisions

1. **Index schema**: 16 fields including `contentVector` (1536-dim HNSW/cosine). Filterable: worldId, entityType, tags, parentId, ownerId, dates. Searchable: name, description, tags, attributes.

2. **ISearchService breaking change**: Existing `SearchEntitiesAsync(worldId, query, sortBy, sortOrder, limit, cursor)` is replaced with `SearchAsync(SearchRequest)` returning `SearchResultSet`. The old LINQ-based `SearchService` implementation is replaced with `AzureAISearchService`.

3. **New domain interfaces**: `ISearchIndexService` (index push/remove), `IEmbeddingService` (embedding generation). Both in `Domain/Interfaces/Services/`.

4. **Search endpoint**: `GET /api/v1/worlds/{worldId}/search` with query params: `q` (required), `mode`, `entityType`, `tags`, `name`, `parentId`, `limit`, `offset`. Returns `SearchResponse` with results + pagination meta.

5. **Background service**: `SearchIndexSyncService` implements `IHostedService`, uses Cosmos DB Change Feed Processor to monitor WorldEntity container. On change: maps to `SearchIndexDocument`, generates embedding via `IEmbeddingService`, pushes via `ISearchIndexService`. On soft-delete: removes from index.

## Constitution Re-Check (Post-Design)

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Cloud-Native Architecture | PASS | Azure AI Search + Cosmos DB Change Feed + Container Apps |
| II. Clean Architecture | PASS | Domain interfaces → Infrastructure implementations. No domain dependency on Azure SDKs. |
| III. Test-Driven Development | PASS | Unit tests for sync service, embedding service, search service. Integration tests for API endpoint. Mocking via interfaces. |
| IV. Framework & Technology Standards | PASS | .NET 10, Azure.Search.Documents SDK, Microsoft.Azure.Cosmos Change Feed, OpenTelemetry |
| V. Developer Experience | PASS | Aspire AppHost integration, single-command startup, connection string injection |
| VI. Security & Privacy by Default | PASS | Private endpoints, managed identity, no secrets in code, world-scoped queries, owner-only access |
| VII. Semantic Versioning | PASS | Additive change (new endpoint), ISearchService is internal — breaking change is acceptable |

All gates pass. No constitutional violations.

## Complexity Tracking

No constitutional violations. All changes follow existing patterns. The ISearchService interface change is an internal breaking change; all consumers are within the same solution and will be updated together.
