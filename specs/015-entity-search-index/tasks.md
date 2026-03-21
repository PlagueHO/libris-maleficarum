# Tasks: Entity Search Index with Vector Search

**Input**: Design documents from `/specs/015-entity-search-index/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/search-api.yaml, quickstart.md

**Tests**: Included — plan.md explicitly lists test files and Constitution Check enforces TDD (Principle III).

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

All paths are relative to `libris-maleficarum-service/` unless prefixed with `infra/` (repository root).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add NuGet dependencies, configuration models, and project wiring needed by all subsequent phases.

- [x] T001 Add NuGet packages to Infrastructure project: `Azure.Search.Documents`, `Azure.AI.OpenAI`, `Microsoft.Extensions.AI` in src/Infrastructure/LibrisMaleficarum.Infrastructure.csproj
- [x] T002 [P] Add NuGet packages to Api project: `AspNetCore.HealthChecks.AzureSearch` (or equivalent) in src/Api/LibrisMaleficarum.Api.csproj
- [x] T003 [P] Add Search configuration options class (`SearchOptions` with `EmbeddingModelName`, `EmbeddingDimensions`, `IndexName`, `MaxBatchSize`, `ChangeFeedPollIntervalMs`) in src/Infrastructure/Configuration/SearchOptions.cs
- [x] T004 [P] Add NuGet packages to unit test project if needed for Azure.Search.Documents mocking in tests/unit/Infrastructure.Tests/LibrisMaleficarum.Infrastructure.Tests.csproj

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain models and interfaces that ALL user stories depend on. No user story work can begin until this phase is complete.

**CRITICAL**: These are the Clean Architecture abstractions that decouple Domain from Infrastructure. All implementation phases depend on these.

- [x] T005 [P] Create SearchIndexDocument domain model in src/Domain/Models/SearchIndexDocument.cs per data-model.md (16 fields including ContentVector as ReadOnlyMemory\<float\>)
- [x] T006 [P] Create SearchRequest domain model with SearchMode enum (Text, Vector, Hybrid) in src/Domain/Models/SearchRequest.cs per data-model.md
- [x] T007 [P] Create SearchResult and SearchResultSet domain models in src/Domain/Models/SearchResult.cs per data-model.md
- [x] T008 [P] Create ISearchIndexService interface (IndexDocumentAsync, IndexDocumentsBatchAsync, RemoveDocumentAsync, RemoveDocumentsBatchAsync) in src/Domain/Interfaces/Services/ISearchIndexService.cs per data-model.md
- [x] T009 [P] Create IEmbeddingService interface (GenerateEmbeddingAsync, GenerateEmbeddingsBatchAsync) in src/Domain/Interfaces/Services/IEmbeddingService.cs per data-model.md
- [x] T011 [P] Create SearchEntitiesRequest API model (query params: q, mode, entityType, tags, name, parentId, limit, offset) in src/Api/Models/Requests/SearchEntitiesRequest.cs per contracts/search-api.yaml
- [x] T012 [P] Create SearchResultResponse API model (SearchResultItem + SearchMeta) in src/Api/Models/Responses/SearchResultResponse.cs per contracts/search-api.yaml
- [x] T013 Add search-related metric and tracing methods to ITelemetryService interface (RecordDocumentIndexed, RecordIndexingFailure, RecordSearchQuery, RecordSyncLag, RecordEmbeddingLatency, RecordSearchLatency, StartIndexingActivity, StartSearchActivity) in src/Domain/Interfaces/Services/ITelemetryService.cs per FR-022/FR-023

**Checkpoint**: All domain models, interfaces, and API models defined — implementation can begin.

---

## Phase 3: User Story 1 — Entity Changes Automatically Indexed (Priority: P1) MVP

**Goal**: When a WorldEntity is created, updated, or soft-deleted, the search index is automatically updated via Cosmos DB Change Feed Processor. Embeddings are generated for entity content and pushed to Azure AI Search asynchronously.

**Independent Test**: Create/update/delete entities via the API and verify the search index reflects changes within 60 seconds. Verify soft-deleted entities are removed from the index.

### Tests for User Story 1 (write FIRST, ensure they FAIL before implementation)

- [x] T014 [P] [US1] Create EmbeddingServiceTests: test embedding generation, null/empty text handling, batch generation, cancellation in tests/unit/Infrastructure.Tests/Services/EmbeddingServiceTests.cs
- [x] T015 [P] [US1] Create SearchIndexSyncServiceTests: test WorldEntity-to-SearchIndexDocument mapping, embedding content concatenation, soft-delete removal, batch processing, transient failure retry, dead-letter logging in tests/unit/Infrastructure.Tests/Services/SearchIndexSyncServiceTests.cs
- [x] T016 [P] [US1] Create AzureAISearchServiceTests for index operations: test IndexDocumentAsync, RemoveDocumentAsync, batch operations, index creation/ensure in tests/unit/Infrastructure.Tests/Services/AzureAISearchServiceTests.cs

### Implementation for User Story 1

- [x] T017 [P] [US1] Implement EmbeddingService using Azure.AI.OpenAI SDK with configurable model name and dimensions (from SearchOptions) in src/Infrastructure/Services/EmbeddingService.cs per FR-003/FR-009
- [x] T018 [US1] Implement AzureAISearchService index operations: EnsureIndexExistsAsync (create index with schema from data-model.md including vector search config and semantic config), IndexDocumentAsync, IndexDocumentsBatchAsync, RemoveDocumentAsync, RemoveDocumentsBatchAsync in src/Infrastructure/Services/AzureAISearchService.cs (implements ISearchIndexService)
- [x] T019 [US1] Implement SearchIndexSyncService as BackgroundService: Cosmos DB Change Feed Processor watching WorldEntity container, maps entities to SearchIndexDocument (per data-model.md mapping table), generates embeddings via IEmbeddingService, pushes via ISearchIndexService, removes soft-deleted entities (FR-004), handles transient failures with retry and dead-letter logging to ILogger (FR-005) in src/Infrastructure/Services/SearchIndexSyncService.cs
- [x] T020 [US1] Add sync-specific OpenTelemetry instrumentation to SearchIndexSyncService: counters (documents indexed, indexing failures), histogram (sync lag seconds, embedding latency), distributed tracing activities for change detection, embedding generation, index push via ITelemetryService in src/Infrastructure/Services/SearchIndexSyncService.cs per FR-022/FR-023
- [x] T021 [US1] Add structured logging to SearchIndexSyncService: Information for document indexed events, Warning for retry attempts, Error for persistent failures and dead-lettering; include worldId, entityId, entityType structured properties per FR-024 in src/Infrastructure/Services/SearchIndexSyncService.cs
- [x] T022 [US1] Implement search-related metrics in TelemetryService (counters and histograms for indexing, search, embedding, sync lag) in src/Infrastructure/Services/TelemetryService.cs per FR-022
- [x] T023 [US1] Register DI services in Api: EmbeddingService as IEmbeddingService (scoped), AzureAISearchService as ISearchIndexService (scoped), bind SearchOptions from configuration in src/Api/Program.cs per existing DI pattern (line ~72). **Note**: SearchIndexSyncService is NOT registered here — it runs in the dedicated SearchIndexWorker project.
- [x] T023b [US1] Create SearchIndexWorker project: new Worker Service project at src/Worker/SearchIndexWorker/LibrisMaleficarum.SearchIndexWorker.csproj referencing Infrastructure and ServiceDefaults projects. Include Program.cs that registers SearchIndexSyncService as IHostedService, CosmosClient for Change Feed, EmbeddingService, AzureAISearchService, SearchOptions configuration, and AddServiceDefaults(). Include appsettings.json and appsettings.Development.json.
- [x] T023c [US1] Add SearchIndexWorker to solution: add src/Worker/SearchIndexWorker/LibrisMaleficarum.SearchIndexWorker.csproj to LibrisMaleficarum.slnx
- [x] T023d [US1] Remove Change Feed CosmosClient and SearchIndexSyncService registration from Api/Program.cs — these now live in the SearchIndexWorker. API retains only search query services (ISearchService, ISearchIndexService for EnsureIndexExists, IEmbeddingService for query vectorization).
- [ ] T024 [US1] Register health checks for Azure AI Search connectivity and embedding model endpoint availability in src/Orchestration/ServiceDefaults/Extensions.cs per FR-025. **Current state**: placeholder returning `Healthy` without actual connectivity check. Must implement real `SearchIndexClient` probe and embedding endpoint probe.

**Checkpoint**: Entity creates/updates/deletes trigger async index sync with embeddings. Index push verified via unit tests. The search index contains documents but search queries are not yet exposed via API.

---

## Phase 4: User Story 2 — Search Entities by Text and Vector Similarity (Priority: P2)

**Goal**: Expose a search endpoint at `GET /api/v1/worlds/{worldId}/search` that performs hybrid (text + vector), pure text, or pure vector search over the Azure AI Search index. Results are scoped to a single world and ranked by relevance.

**Independent Test**: Populate the index with known entities, issue search queries through the API, verify relevance ranking, world isolation, and filtering.

### Tests for User Story 2 (write FIRST, ensure they FAIL before implementation)

- [ ] T025 [P] [US2] Add SearchAsync tests to AzureAISearchServiceTests: test hybrid/text/vector modes, world isolation filter, entity type filter, tags filter, name filter, parentId filter, pagination, empty results in tests/unit/Infrastructure.Tests/Services/AzureAISearchServiceTests.cs
- [ ] T026 [P] [US2] Update SearchApiIntegrationTests for new search endpoint: test query, filtered query, empty results, pagination, world-level isolation, authorization (403 for non-owner), query validation (400 for empty q) in tests/integration/Api.IntegrationTests/SearchApiIntegrationTests.cs per SC-007

### Implementation for User Story 2

- [x] T010 [US2] Update ISearchService interface: replace SearchEntitiesAsync with SearchAsync(SearchRequest) returning SearchResultSet in src/Domain/Interfaces/Services/ISearchService.cs per data-model.md (breaking change — applied atomically with consumer updates below)
- [x] T027 [US2] Implement SearchAsync in AzureAISearchService: build search query with world filter, apply entityType/tags/name/parentId filters, support hybrid/text/vector modes via SearchOptions, generate query vector via IEmbeddingService for vector/hybrid modes, map results to SearchResultSet in src/Infrastructure/Services/AzureAISearchService.cs (implements ISearchService)
- [x] T028 [US2] Update WorldEntitiesController: refactor existing list endpoint (HttpGet) to use IWorldEntityRepository directly instead of ISearchService.SearchEntitiesAsync. Add new search endpoint action `[HttpGet("search")]` that accepts SearchEntitiesRequest, validates world access (FR-020), maps to SearchRequest, calls ISearchService.SearchAsync, returns SearchResultResponse in src/Api/Controllers/WorldEntitiesController.cs per contracts/search-api.yaml
- [x] T028b [US2] Refactor WorldsController search endpoint (~line 143) to call ISearchService.SearchAsync(SearchRequest) instead of SearchEntitiesAsync; update constructor injection and any response mapping in src/Api/Controllers/WorldsController.cs
- [x] T029 [US2] Update DI registration: register AzureAISearchService as ISearchService (replacing old SearchService registration) in src/Api/Program.cs (line ~72)
- [x] T030 [US2] Add search query OpenTelemetry instrumentation: counter (search queries executed), histogram (search query latency), distributed tracing activity for search execution via ITelemetryService in src/Infrastructure/Services/AzureAISearchService.cs per FR-022/FR-023
- [x] T031 [US2] Add structured logging for search queries: Information level with worldId, query text length (not raw query content), filters applied, search mode, result count, latency; Debug level for raw query text per FR-024 in src/Infrastructure/Services/AzureAISearchService.cs
- [x] T032 [US2] Update existing SearchServiceTests for new ISearchService.SearchAsync signature: update mocks and assertions to use SearchRequest/SearchResultSet in tests/unit/Infrastructure.Tests/Services/SearchServiceTests.cs
- [x] T032b [US2] Update WorldsControllerTests: update mocked ISearchService calls to use SearchAsync(SearchRequest) signature and verify WorldsController search endpoint works with new interface in tests/unit/Api.Tests/Controllers/WorldsControllerTests.cs

**Checkpoint**: Full search API operational — hybrid, text, and vector search modes work through `GET /api/v1/worlds/{worldId}/search`. World isolation enforced. Existing list endpoint unbroken.

---

## Phase 5: User Story 3 — Search Index Supports Filterable Fields (Priority: P3)

**Goal**: Ensure the index schema includes all filterable, sortable, and facetable fields per FR-010 and that the schema is extensible without reindexing (FR-011).

**Independent Test**: Query the index with various filter combinations and verify correct results. Validate schema definition matches FR-010 field requirements.

- [x] T033 [US3] Verify AzureAISearchService.EnsureIndexExistsAsync creates all fields per FR-010 with correct attributes (filterable: worldId, entityType, name, tags, parentId, ownerId, createdDate, modifiedDate, path, depth, schemaVersion; sortable: name, createdDate, modifiedDate, depth; facetable: entityType, tags; searchable: name, description, tags, attributes) in src/Infrastructure/Services/AzureAISearchService.cs
- [ ] T034 [P] [US3] Add filter combination integration tests: filter by entityType alone, tags alone, name prefix, parentId, multiple filters conjunctively per spec acceptance scenarios in tests/integration/Api.IntegrationTests/SearchApiIntegrationTests.cs
- [x] T035 [US3] Document index extensibility approach per FR-011: adding a new field to the index schema does not require reindexing — Azure AI Search supports additive schema changes in specs/015-entity-search-index/quickstart.md

**Checkpoint**: All filter fields operational and verified. Schema extensibility documented.

---

## Phase 6: User Story 4 — Knowledge Base Readiness for Azure AI Foundry (Priority: P4)

**Goal**: Validate the search index schema is compatible with Azure AI Search Knowledge Base registration as a Knowledge Source for future Foundry IQ integration.

**Independent Test**: Evaluate the index schema against Azure AI Search Knowledge Base requirements and document compliance.

- [x] T036 [US4] Validate index schema compatibility with Azure AI Search Knowledge Base requirements: verify vector field configuration, text fields, metadata fields meet Knowledge Source registration criteria per FR-021 and SC-006 in src/Infrastructure/Services/AzureAISearchService.cs
- [x] T037 [P] [US4] Document Knowledge Base registration steps as a future guide: Foundry IQ connection instructions, required index schema properties, and any additional configuration needed in specs/015-entity-search-index/quickstart.md

**Checkpoint**: Index schema validated for Knowledge Base compatibility. Registration guide documented for future use.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Infrastructure updates, Aspire integration, and documentation that span multiple user stories.

- [x] T038 [P] Update infra/main.bicep: change Azure AI Search SKU from `standard` to `basic` per research.md decision (note: in-place SKU downgrade is not supported — new resource may be needed if standard is already deployed) in infra/main.bicep
- [x] T039 [P] Add Cosmos DB leases container provisioning to infra/main.bicep: container name `leases`, partition key `/id`, 400 RU/s in infra/main.bicep per research.md
- [x] T040 [P] Add AI Services embedding model deployment configuration to infra/main.bicep or infra/cognitive-services/ if not already provisioned (text-embedding-3-small) in infra/
- [x] T040b [P] Add Azure Monitor alert rule for dead-letter indexing failures (FR-005): provision metric alert that fires when indexing failures exceed threshold, targeting Application Insights or custom metric, using AVM module `br/public:avm/res/insights/metric-alert` in infra/main.bicep
- [x] T041 Update Aspire AppHost: add Azure AI Search resource (connection string injection), add Azure AI Services resource (connection string injection), add leases container to Cosmos DB emulator provisioning. Add SearchIndexWorker as a separate project (`builder.AddProject<Projects.LibrisMaleficarum_SearchIndexWorker>("search-index-worker")`) with references to cosmosDb, aiSearch, and embeddingDeployment. Remove SearchIndexSyncService registration from the API service. in src/Orchestration/AppHost/AppHost.cs per quickstart.md. **Completed**: All Aspire AppHost changes applied — SearchIndexWorker at `src/Worker/SearchIndexWorker/` registered as `search-index-worker`.
- [x] T042 [P] Remove or archive old SearchService.cs (LINQ-based implementation replaced by AzureAISearchService) in src/Infrastructure/Services/SearchService.cs — ensure no remaining references
- [ ] T043 Run quickstart.md end-to-end validation: start Aspire AppHost, create entities, verify index sync, execute search queries, validate filters and pagination in specs/015-entity-search-index/quickstart.md
- [ ] T043b Create relevance evaluation test set (minimum 20 query/expected-result pairs) and execute manual evaluation against deployed index to validate SC-003 (80% semantic relevance in top 5). Document results in specs/015-entity-search-index/quickstart.md
- [ ] T044 [P] Update quickstart with corrections discovered during implementation in specs/015-entity-search-index/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational phase — can start after Phase 2 complete
- **US2 (Phase 4)**: Depends on Foundational phase AND US1 (needs index with documents to search). Can partially parallelize tests (T025-T026) while US1 implementation completes.
- **US3 (Phase 5)**: Depends on US1 (index must exist) and US2 (search endpoint must work for filter validation)
- **US4 (Phase 6)**: Depends on US1 (index schema must exist). Can proceed in parallel with US2/US3 for documentation tasks.
- **Polish (Phase 7)**: Depends on US1 and US2 being complete. Bicep/Aspire tasks (T038-T041) can start in parallel with US2 after US1 is complete.

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) — No dependencies on other stories. This is the MVP.
- **User Story 2 (P2)**: Depends on US1 (index must contain documents to search meaningfully). Can start test writing (T025-T026) immediately after Phase 2.
- **User Story 3 (P3)**: Depends on US2 (needs search endpoint to validate filters). Schema definition is completed as part of US1 (T018).
- **User Story 4 (P4)**: Depends on US1 (index schema). Documentation-only — can proceed in parallel with US2/US3.

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Domain models/interfaces before infrastructure implementations
- Infrastructure implementations before API endpoint
- Core implementation before observability/logging
- Story complete before moving to next priority

### Parallel Opportunities

- All Foundational (Phase 2) tasks marked [P] (T005-T009, T011-T012) can run in parallel
- US1 tests (T014-T016) can all run in parallel
- US1 implementation: T017 (EmbeddingService) and T018 (AzureAISearchService index ops) can run in parallel; T019 (SyncService) depends on both
- US2 tests (T025-T026) can run in parallel
- US4 tasks (T036-T037) can run in parallel with US2/US3
- Polish Bicep tasks (T038-T040) can run in parallel with each other and with US2/US3

---

## Parallel Example: User Story 1

```text
# Phase 2 parallel batch (all [P] tasks can run together):
T005: Create SearchIndexDocument model in src/Domain/Models/SearchIndexDocument.cs
T006: Create SearchRequest model in src/Domain/Models/SearchRequest.cs
T007: Create SearchResult models in src/Domain/Models/SearchResult.cs
T008: Create ISearchIndexService in src/Domain/Interfaces/Services/ISearchIndexService.cs
T009: Create IEmbeddingService in src/Domain/Interfaces/Services/IEmbeddingService.cs
T011: Create SearchEntitiesRequest in src/Api/Models/Requests/SearchEntitiesRequest.cs
T012: Create SearchResultResponse in src/Api/Models/Responses/SearchResultResponse.cs

# Then sequential (Phase 2):
T013: Update ITelemetryService (depends on domain model awareness)

# US1 test parallel batch:
T014: EmbeddingServiceTests
T015: SearchIndexSyncServiceTests
T016: AzureAISearchServiceTests (index ops)

# US1 implementation parallel batch:
T017: EmbeddingService implementation
T018: AzureAISearchService index operations

# US1 sequential (depends on T017 + T018):
T019: SearchIndexSyncService (depends on IEmbeddingService + ISearchIndexService)
T020: Sync observability (same file as T019)
T021: Sync structured logging (same file as T019)
T022: TelemetryService metrics implementation
T023: API DI registration (search query services only)
T023b: Create SearchIndexWorker project (hosts SearchIndexSyncService)
T023c: Add SearchIndexWorker to solution
T023d: Remove sync services from Api/Program.cs
T024: Health checks
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (NuGet packages, configuration)
1. Complete Phase 2: Foundational (domain models, interfaces, API models)
1. Complete Phase 3: User Story 1 (index sync pipeline)
1. **STOP and VALIDATE**: Create entities via API, verify they appear in the search index within 60 seconds. Verify soft-deleted entities are removed.
1. Deploy/demo if ready — index sync is operational.

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
1. Add User Story 1 → Test independently → Deploy/Demo (MVP — index sync works)
1. Add User Story 2 → Test independently → Deploy/Demo (search API operational)
1. Add User Story 3 → Test independently → Deploy/Demo (all filters verified)
1. Add User Story 4 → Test independently → Deploy/Demo (KB readiness documented)
1. Complete Polish → Bicep + Aspire + documentation finalized
1. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
1. Once Foundational is done:
   - Developer A: User Story 1 (index sync pipeline)
   - Developer B: User Story 4 (documentation — can start immediately)
1. After US1 complete:
   - Developer A: User Story 2 (search API)
   - Developer B: Polish Bicep/Aspire tasks (T038-T041)
1. After US2 complete:
   - Developer A: User Story 3 (filter validation)
   - Developer B: Remaining polish tasks

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks in same phase
- [Story] label maps task to specific user story for traceability (US1-US4)
- The ISearchService breaking change (T010) is in Phase 4 (US2) — applied atomically with all consumer updates (T027, T028, T028b, T029, T032, T032b) to keep the project compilable between phases
- The old LINQ-based SearchService.cs is archived/removed in Polish (T042) after AzureAISearchService fully replaces it
- AzureAISearchService implements BOTH ISearchIndexService (for index push, US1) and ISearchService (for search queries, US2)
- WorldsController.cs (~line 143) also consumes ISearchService — updated in T028b, tested in T032b
- Index schema creation (T018 EnsureIndexExistsAsync) happens on SearchIndexWorker startup, not as a separate migration step
- SearchIndexSyncService lives in Infrastructure but is hosted in the SearchIndexWorker project (not the API) for independent scaling, fault isolation, and deployment independence
- The SearchIndexWorker (T023b) references Infrastructure and ServiceDefaults projects — it does NOT reference the Api project
- Commit after each task or logical group. Stop at any checkpoint to validate independently.
