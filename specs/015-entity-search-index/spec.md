# Feature Specification: Entity Search Index with Vector Search

**Feature Branch**: `015-entity-search-index`  
**Created**: 2026-03-07  
**Status**: Draft  
**Input**: User description: "Add entity Search Index support with vector indexes to the backend API"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Entity Changes Automatically Indexed (Priority: P1)

When a user creates, updates, or soft-deletes a WorldEntity through the API, the search index is automatically updated to reflect the change. This happens asynchronously so the user does not experience additional latency on write operations. The index contains vector embeddings of entity content (Name, Description, Tags, Attributes) alongside filterable metadata fields.

**Why this priority**: Without index synchronization, search results become stale and unreliable. This is the foundational capability that all search features depend on.

**Independent Test**: Can be tested by creating/updating/deleting entities via the API and verifying the search index reflects the changes within a reasonable time window.

**Acceptance Scenarios**:

1. **Given** a new WorldEntity is created via the API, **When** the indexing process runs, **Then** the entity appears in the search index with correct vector embeddings and filterable metadata within the defined synchronization window.
1. **Given** an existing WorldEntity is updated via the API, **When** the indexing process runs, **Then** the search index reflects the updated Name, Description, Tags, and re-generated vector embeddings.
1. **Given** a WorldEntity is soft-deleted via the API, **When** the indexing process runs, **Then** the entity is removed from the search index (soft-deleted entities must not appear in search results).
1. **Given** the indexing process encounters a transient failure, **When** a retry is attempted, **Then** the entity change is eventually indexed without data loss.
1. **Given** multiple entity changes occur in rapid succession, **When** the indexing process runs, **Then** all changes are reflected in the index without missed updates.

---

### User Story 2 - Search Entities by Text and Vector Similarity (Priority: P2)

A backend API consumer (initially an AI agent, later the UI) can search for entities within a specific world using a natural language query. The search uses hybrid retrieval combining full-text search and vector similarity to return the most relevant entities. Results are scoped to a single world and can be further filtered by entity type, name, and other metadata fields.

**Why this priority**: This is the primary consumer-facing search capability that delivers value from the indexed data. It enables AI agents to discover relevant world content for context-aware assistance.

**Independent Test**: Can be tested by populating the index with known entities and issuing search queries through the API, verifying relevance and filtering.

**Acceptance Scenarios**:

1. **Given** entities exist in the search index for a specific world, **When** a user searches with a natural language query scoped to that world, **Then** relevant entities are returned ranked by relevance.
1. **Given** a search query is issued, **When** the `worldId` filter is applied, **Then** only entities belonging to that world are returned (no cross-world leakage).
1. **Given** a search query is issued with an entity type filter, **When** results are returned, **Then** only entities of the specified type appear.
1. **Given** a search query is issued with multiple filters (world, entity type, tags), **When** results are returned, **Then** all filters are applied conjunctively.
1. **Given** a world has no matching entities for the query, **When** the search is executed, **Then** an empty result set is returned with a 200 status.
1. **Given** a search query is issued, **When** results are returned, **Then** each result includes the entity ID, name, entity type, description snippet, relevance score, and other key metadata.

---

### User Story 3 - Search Index Supports Filterable Fields (Priority: P3)

The search index schema includes a set of filterable, sortable, and facetable fields that enable efficient narrowing of search results. The schema is designed to be extensible so additional filter fields can be added in the future without reindexing existing data.

**Why this priority**: Filtering is essential for efficient retrieval in worlds with many entities, but the core search functionality (P1/P2) must work first. Extensibility ensures the design accommodates future requirements.

**Independent Test**: Can be tested by querying the index with various filter combinations and verifying correct results.

**Acceptance Scenarios**:

1. **Given** the search index is populated, **When** a filter on `WorldId` is applied, **Then** only entities for that world are returned.
1. **Given** the search index is populated, **When** a filter on `EntityType` is applied, **Then** only entities of that type are returned.
1. **Given** the search index is populated, **When** a filter on `Name` is applied (exact or prefix match), **Then** matching entities are returned.
1. **Given** a new filterable field is needed in the future, **When** the index schema is extended, **Then** existing indexed documents remain valid and new documents include the new field.

---

### User Story 4 - Knowledge Base Readiness for Azure AI Foundry (Priority: P4)

The search index is structured and configured so that it can be registered as an Azure AI Search Knowledge Source within an Azure AI Search Knowledge Base. This enables future integration with Microsoft Foundry as a Foundry IQ connection for agentic retrieval.

**Why this priority**: This is a forward-looking requirement. The P1-P3 capabilities must work first, but the index design should not preclude this integration.

**Independent Test**: Can be tested by verifying the index schema meets the documented requirements for Azure AI Search Knowledge Base registration.

**Acceptance Scenarios**:

1. **Given** the search index exists with vector and text fields, **When** it is evaluated against Azure AI Search Knowledge Base requirements, **Then** it meets the requirements for registration as a Knowledge Source.
1. **Given** the index uses vector profiles and scoring, **When** an agentic retrieval query is issued, **Then** the index returns grounded results suitable for AI agent consumption.

---

### Edge Cases

- What happens when the embedding generation service is unavailable? The system should queue the change and retry, not block the entity write operation.
- What happens when an entity has no description or very minimal text? The system should still index it with whatever content is available (Name at minimum).
- What happens when a very large batch of entities is created at once (e.g., world import)? The system should handle backpressure without losing messages.
- What happens if the search index service is temporarily unavailable? Changes should be buffered and applied once the service recovers.
- What happens when a soft-deleted entity's TTL expires and it is purged from Cosmos DB? The entity should already have been removed from the search index at soft-delete time.

## Requirements *(mandatory)*

### Functional Requirements

#### Index Synchronization

- **FR-001**: System MUST asynchronously update the Azure AI Search index when a WorldEntity is created, updated, or soft-deleted.
- **FR-002**: System MUST NOT add additional latency to entity write operations — indexing happens out of band from the API response.
- **FR-003**: System MUST generate vector embeddings for entity content (Name, Description, Tags, and Attributes concatenated) using an Azure AI Services embedding model. SystemProperties are excluded from the embedding to avoid indexing highly system-specific mechanical data with low semantic search value.
- **FR-004**: System MUST remove soft-deleted entities from the search index so they do not appear in search results.
- **FR-005**: System MUST handle transient failures in index synchronization with automatic retries and dead-letter handling for persistent failures. Dead-lettered items MUST be logged to Application Insights and trigger an Azure Monitor alert to operators for investigation.
- **FR-006**: System MUST support eventual consistency — the search index may lag behind Cosmos DB by a bounded amount of time, but all changes must eventually be reflected.

#### Index Synchronization Method

- **FR-007**: The index synchronization method MUST be evaluated based on these priorities (in order): (1) lowest operational cost, (2) simplicity of infrastructure and code, (3) reliability.
- **FR-008**: The chosen synchronization approach MUST be documented with a cost/complexity comparison against alternatives considered. Candidate approaches to evaluate include:
  - **Cosmos DB Indexer for Azure AI Search**: Built-in indexer that reads Cosmos DB change feed. Minimal code required but has per-document indexer costs and limited control over embedding generation.
  - **Cosmos DB Change Feed + Queue/Event Processing**: Cosmos DB Change Feed triggers a function or processor that generates embeddings and pushes to the search index. More control but more infrastructure.
  - **Application-Level Eventing**: The API itself publishes entity change events to a queue (Azure Storage Queue, Service Bus) and a background processor handles indexing. Simple but requires explicit publish logic in the API layer.
  - **Cosmos DB Change Feed + Direct Index Push**: Cosmos DB Change Feed processor that directly generates embeddings and pushes to the search index without an intermediate queue. Simpler than queue-based but less resilient to failures.
  - Other approaches the implementer identifies.
- **FR-009**: System MUST use `text-embedding-3-small` at 1536 dimensions as the default embedding model. The embedding model name and vector dimensions MUST be configurable (via application configuration) to allow switching to `text-embedding-3-large` or another model in future without code changes.

#### Search Index Schema

- **FR-010**: The search index MUST include the following fields at minimum:
  - `id` (key field, entity ID)
  - `worldId` (filterable, required for all queries)
  - `entityType` (filterable, facetable)
  - `name` (searchable, filterable, sortable)
  - `description` (searchable)
  - `tags` (searchable, filterable, facetable, collection)
  - `parentId` (filterable)
  - `ownerId` (filterable)
  - `createdDate` (filterable, sortable)
  - `modifiedDate` (filterable, sortable)
  - `contentVector` (vector field for semantic search, using HNSW or similar algorithm)
- **FR-011**: The search index schema MUST be extensible — additional filterable/searchable fields can be added without requiring reindexing of existing documents.
- **FR-012**: The search index MUST use a vector search configuration (vector profile with HNSW algorithm) that supports hybrid retrieval (combined text + vector search).

#### Search API

- **FR-013**: System MUST expose a search endpoint at `GET /api/v1/worlds/{worldId}/search` in the backend API.
- **FR-014**: The search endpoint MUST accept a natural language query parameter and return entities ranked by relevance.
- **FR-015**: The search endpoint MUST support filtering by `entityType`, `tags`, `name`, and `parentId` via query parameters.
- **FR-016**: The search endpoint MUST support pagination (offset/limit or continuation token).
- **FR-017**: The search endpoint MUST return results that include: entity ID, name, entity type, description snippet, relevance score, and key metadata.
- **FR-018**: The search endpoint MUST enforce world-level isolation — only entities belonging to the specified `worldId` are searchable.
- **FR-019**: The search endpoint MUST support both pure text search, pure vector search, and hybrid (text + vector) search modes.
- **FR-020**: The search endpoint MUST validate that the requesting user has access to the specified world before executing the search. For this feature, access is determined by world ownership (owner-only), consistent with the existing API authorization pattern. A future RBAC model will allow granting roles to other users for specific worlds or entities.

#### Knowledge Base Compatibility

- **FR-021**: The search index MUST be structured to be compatible with Azure AI Search Knowledge Base registration as a Knowledge Source, following the documented schema requirements for agentic retrieval.

#### Observability

- **FR-022**: System MUST emit custom OpenTelemetry metrics via the existing `ITelemetryService` pattern, including at minimum: counters for documents indexed, indexing failures, and search queries executed; histograms for embedding generation latency, search query latency, and indexing synchronization lag (`indexing.sync_lag_seconds` — time between entity change and index update completion) to enable real-time monitoring and Azure Monitor alerting when the sync lag exceeds the 60-second target.
- **FR-023**: System MUST create distributed tracing activities (via `ActivitySource`) for indexing pipeline operations (change detection, embedding generation, index push) and search query execution to enable end-to-end trace correlation in Application Insights and the Aspire Dashboard.
- **FR-024**: System MUST use structured logging via `ILogger` with OpenTelemetry export to Application Insights in production. Log levels: `Information` for indexing lifecycle events (document indexed, search query executed), `Warning` for transient retry attempts, `Error` for persistent failures and dead-lettering. All log entries MUST include structured properties (e.g., worldId, entityId, entityType, operationName) for queryability in Application Insights. Search query log entries at `Information` level MUST include worldId, query text length (not raw query content), filters applied, search mode, result count, and latency. Raw query text MUST NOT be logged at `Information` level to avoid capturing sensitive user content; it MAY be logged at `Debug` level for temporary relevance debugging.
- **FR-025**: System MUST register health checks for Azure AI Search connectivity and embedding model endpoint availability, integrated with the existing Aspire `AddServiceDefaults()` health check infrastructure.

### Key Entities

- **Search Index Document**: Represents a WorldEntity in the search index. Contains key metadata fields (worldId, entityType, name, tags, etc.), searchable text content, and a vector embedding of the entity content. One-to-one mapping with non-deleted WorldEntity documents in Cosmos DB.
- **Search Request**: Represents a search query with a query string, optional filters (worldId required, entity type, tags, name), search mode (text/vector/hybrid), and pagination parameters.
- **Search Result**: Represents a ranked search result containing entity ID, name, entity type, description snippet, relevance score, and metadata. Returned as a paginated list.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: When an entity is created, updated, or soft-deleted, the search index reflects the change within a defined synchronization window (target: under 60 seconds for the chosen approach, documented with actual measured latency).
- **SC-002**: Search queries scoped to a world with 1,000 entities return results in under 2 seconds.
- **SC-003**: Search results for natural language queries return semantically relevant entities in the top 5 results at least 80% of the time (measured by manual relevance evaluation on a representative test set). **Validation**: Requires a curated test set of at least 20 query/expected-result pairs evaluated post-deployment. See tasks.md T043b for the evaluation task.
- **SC-004**: No entity write operation (create/update/delete) experiences more than 50ms of added latency due to indexing (indexing is fully asynchronous).
- **SC-005**: The monthly cost of the search indexing infrastructure (excluding the Azure AI Search service itself, which is already provisioned) remains proportional to update volume — idle worlds incur minimal to no indexing cost.
- **SC-006**: The search index is registered as a valid Knowledge Source in an Azure AI Search Knowledge Base without modification to the index schema.
- **SC-007**: The Search API endpoint passes integration tests covering: basic query, filtered query, empty results, pagination, and world-level isolation.

## Clarifications

### Session 2026-03-07

- Q: Which Azure AI Search SKU should be used? → A: Basic tier (~$75/month, supports vector search, 1GB storage, 5 indexes). Serverless tier planned for Azure AI Search will be leveraged in future when available.
- Q: What should happen to dead-lettered indexing failures? → A: Log to Application Insights AND trigger an Azure Monitor alert to operators.
- Q: Should Properties/SystemProperties be included in the vector embedding? → A: Embed Name + Description + Tags + Properties (common domain content). Exclude SystemProperties (system-specific mechanical data).
- Q: Which embedding model and dimensions should be used? → A: `text-embedding-3-small` at 1536 dimensions as default. Model name and dimensions must be configurable to allow switching to `text-embedding-3-large` or another model in future.
- Q: What authorization model for the search endpoint? → A: Owner-only access for now (matches existing API pattern). Future RBAC model planned to grant roles to other users for specific worlds or entities.
- Q: Which custom metrics should the search feature emit? → A: Standard set — counters for documents indexed, indexing failures, search queries executed + histograms for embedding latency and search latency. Extends existing ITelemetryService pattern.
- Q: What structured logging level for search/indexing events? → A: Standard — `Information` for lifecycle events (indexed, searched), `Warning` for retries, `Error` for persistent failures. Must use structured logging via OpenTelemetry to Application Insights in production.
- Q: Should the feature register health checks for search dependencies? → A: Yes — add health checks for Azure AI Search connectivity and embedding model endpoint availability.
- Q: How should operators monitor indexing sync lag? → A: Emit a histogram metric (`indexing.sync_lag_seconds`) for real-time monitoring and alerting via Azure Monitor.
- Q: What search-specific data to capture per query? → A: Parameters only — worldId, query text length (not content), filters applied, search mode, result count, latency. Raw query text excluded to avoid logging sensitive content.

## Assumptions

- Azure AI Search service is already provisioned in the infrastructure (confirmed in `infra/main.bicep`) using the **Basic** SKU tier, which is the minimum tier supporting vector search. A future migration to the Azure AI Search Serverless tier is planned when it becomes available.
- Azure AI Services (for embedding generation) is available or will be provisioned as part of the existing cognitive services infrastructure in `infra/cognitive-services/`.
- The backend API is implemented in .NET 10 with ASP.NET Core and EF Core (Cosmos DB provider) as described in the architecture docs.
- The Cosmos DB WorldEntity container uses a hierarchical partition key `[/WorldId, /id]`.
- Authentication and authorization are handled by existing API middleware — the search endpoint follows the same owner-only access pattern. A future RBAC model for world/entity-level role grants is planned but out of scope for this feature.
- The search index does not need to support cross-world queries — all searches are scoped to a single world via the `worldId` filter.
- Soft-deleted entities (IsDeleted = true) must not appear in search results.
- The UI will not be modified in this feature — the Search API is consumed by backend services and AI agents initially.
- The embedding dimensionality and model default to `text-embedding-3-small` at 1536 dimensions, but are configurable to allow future model upgrades without code changes.
- The synchronization approach will be finalized during implementation based on the cost/complexity evaluation required by FR-007/FR-008.

## Scope Boundaries

### In Scope

- Azure AI Search index schema definition with vector and filterable fields
- Index synchronization mechanism (evaluated and implemented per FR-007/FR-008)
- Vector embedding generation for entity content
- Backend Search API endpoint (`GET /api/v1/worlds/{worldId}/search`)
- Integration tests for the Search API
- Knowledge Base compatibility validation

### Out of Scope

- Frontend/UI search interface (future feature)
- Cross-world search
- Microsoft Foundry IQ connection setup (future — design only needs to be compatible)
- Real-time search-as-you-type or autocomplete
- Asset content indexing (only entity metadata is indexed)
- Search analytics or query logging dashboards
- Custom ranking or boosting profiles (default relevance ranking is sufficient)

## Dependencies

- Azure AI Search service (already provisioned)
- Azure AI Services embedding model (may need provisioning or configuration)
- Cosmos DB WorldEntity container (already implemented)
- Backend API infrastructure (.NET 10, ASP.NET Core — already implemented)
- Entity CRUD operations (already implemented — change events need to be connected to indexing pipeline)
