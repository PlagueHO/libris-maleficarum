# Research: Entity Search Index — Sync Method Evaluation

**Date**: 2026-03-07 | **Feature**: 015-entity-search-index  
**Purpose**: Resolve all NEEDS CLARIFICATION items from the technical context, with focus on the index synchronization method per FR-007/FR-008.

## 1. Index Synchronization Method (FR-007 / FR-008)

### Decision: Cosmos DB Change Feed Processor as a Dedicated Worker Service

The Change Feed Processor running as a `BackgroundService` in a dedicated `SearchIndexWorker` project is the recommended synchronization method. This approach scores highest across all three evaluation criteria in FR-007: (1) lowest operational cost, (2) simplicity, (3) reliability. Hosting the Change Feed Processor in its own service (rather than in the API process) provides independent scaling, fault isolation, and deployment independence.

### Evaluation Criteria (FR-007 Priority Order)

| Criterion | Weight | Description |
|-----------|--------|-------------|
| Operational Cost | 1st | Monthly Azure spend for the sync pipeline (excluding AI Search service itself) |
| Simplicity | 2nd | Lines of code + number of infrastructure components |
| Reliability | 3rd | Fault tolerance, at-least-once delivery, dead-letter handling |

### Candidates Evaluated

#### Option A: Cosmos DB Indexer with Integrated Vectorization

Azure AI Search provides a built-in Cosmos DB indexer that reads the Cosmos DB change feed via the `_ts` high-water mark. Combined with a skillset containing the `AzureOpenAIEmbedding` skill, it can automatically generate vector embeddings during indexing.

**Architecture**: Cosmos DB → AI Search Indexer (scheduled) → Skillset (embedding generation) → Search Index

**Pros**:

- Minimal application code — indexer, skillset, and index defined as Azure resources
- Built-in retry logic for Azure OpenAI throttling
- Built-in change detection via `_ts` and soft-delete detection via `SoftDeleteColumnDeletionDetectionPolicy`
- Managed by Azure — no custom hosting infrastructure

**Cons**:

- **Minimum 5-minute schedule interval** — the AI Search indexer cannot be scheduled more frequently than every 5 minutes, resulting in 5-10 minute sync lag. This exceeds the 60-second target in SC-001.
- **Field concatenation requires a Custom Web API skill** — FR-003 requires embedding Name + Description + Tags + Properties concatenated. The built-in skills do not offer simple field concatenation. A Custom Web API skill (Azure Function) would be needed, adding an extra Azure Function App to the infrastructure.
- **Limited observability** — Cannot emit custom OpenTelemetry metrics (FR-022) or trace activities (FR-023) from within the indexer pipeline. Indexer status is available via REST API only, not integrated with the application's telemetry pipeline.
- **Per-document indexer processing charges** — AI Search charges for document cracking in the indexer pipeline.
- **Less control over error handling** — Dead-letter handling (FR-005) is limited to indexer failure counts; custom dead-letter to Application Insights requires external monitoring.

**Cost estimate**: AI Search indexer processing charges + Azure Function invocations (for custom skill) + embedding API calls.

#### Option B: Cosmos DB Change Feed Processor (Background Service) ★ RECOMMENDED

The Cosmos DB SDK includes a Change Feed Processor that can be hosted as an `IHostedService` / `BackgroundService`. It is deployed as a dedicated `SearchIndexWorker` service, separate from the API. The processor reads changes from the WorldEntity container, generates embeddings via the Azure AI Services SDK, and pushes documents to the Azure AI Search index.

**Architecture**: Cosmos DB Change Feed → SearchIndexWorker Service → Embedding Service → AI Search Index

**Pros**:

- **Lowest operational cost** — minimal additional Azure compute (dedicated Container Apps instance with low resource allocation). No indexer processing charges. Change Feed reads cost ~1 RU per change (negligible). Only embedding API calls are an incremental cost (same as all other options).
- **Near-real-time sync** — Change Feed processor polls at configurable intervals (default: seconds), meeting the 60-second SC-001 target.
- **Full observability** — emits custom OpenTelemetry metrics (FR-022: sync lag histogram, counters), distributed tracing activities (FR-023), structured logging (FR-024) via the existing `ITelemetryService` pattern.
- **Full control over field concatenation** — application code concatenates Name + Description + Tags + Properties per FR-003, no need for external skills.
- **Aspire integration** — Change Feed processor runs as a separate project in the Aspire AppHost developer inner loop. Lease container can be provisioned in the Cosmos DB emulator.
- **Clean Architecture alignment** — `ISearchIndexService` interface in Domain, `SearchIndexSyncService` implementation in Infrastructure, hosted in SearchIndexWorker, following existing patterns.
- **Built-in fault tolerance** — Change Feed Processor provides at-least-once delivery with automatic retry from the last checkpoint. Dead-letter logic can write to Application Insights and trigger alerts per FR-005.
- **Independent scaling** — Worker scales based on change volume independently from API request load.
- **Fault isolation** — Sync failures or resource contention don't affect API availability.
- **Deployment independence** — Indexing pipeline can be deployed, restarted, or rolled back without affecting the API.

**Cons**:

- More application code than the indexer approach (~200-300 lines for the sync service + embedding orchestration + error handling).
- Requires a dedicated Container Apps instance for the worker, though with minimal resource allocation (the worker is I/O-bound, not CPU-bound).
- Requires a lease container in Cosmos DB (minimal additional storage cost).

**Cost estimate**: Cosmos DB Change Feed reads (~1 RU/change) + embedding API calls + minimal Container Apps compute for worker. No additional messaging services.

#### Option C: Application-Level Eventing (Queue-Based)

The API layer publishes entity change events to an Azure Storage Queue or Service Bus queue. A background processor (Azure Function or separate worker) dequeues messages, generates embeddings, and pushes to the AI Search index.

**Architecture**: API → Queue (Storage Queue / Service Bus) → Processor (Function / Worker) → Embedding Service → AI Search Index

**Pros**:

- Decoupled from Cosmos DB internals — works even if Change Feed is unavailable.
- Queue provides durable message storage with built-in dead-letter support.
- Well-understood messaging pattern.

**Cons**:

- **Highest infrastructure complexity** — requires an additional queue service (Storage Queue or Service Bus) + a consumer (Azure Function or worker project). Three new Azure components vs. zero for Option B.
- **Higher operational cost** — queue service costs + function invocations (or additional compute for worker).
- **Explicit publish logic in API layer** — every entity write path (create, update, delete) must explicitly publish to the queue, adding coupling between API logic and the sync pipeline. Risk of missed publishes if a code path is overlooked.
- **Dual-write risk** — entity persistence and queue publish are not atomic. If the queue publish fails after the entity is saved, the change is lost (unless compensated by a periodic Change Feed catchup, which brings us back to Option B).

**Cost estimate**: Queue service (~$0.05/month for Storage Queue) + consumer compute (Azure Function consumption or additional Container Apps instance) + embedding API calls.

#### Option D: Cosmos DB Change Feed + Intermediate Queue + Index Push

Combines the Change Feed for reliable change detection with an intermediate queue for durable message handling.

**Architecture**: Cosmos DB Change Feed → Azure Function → Queue → Second Processor → Embedding Service → AI Search Index

**Pros**:

- Most resilient — queue provides durability between change detection and index push.
- Dead-letter queue for persistent failures.

**Cons**:

- **Most complex** — four infrastructure components in the pipeline.
- **Highest operational cost** — Azure Function + Queue + consumer compute + embedding API.
- **Unnecessary** for this scale — the Change Feed Processor (Option B) already provides at-least-once delivery with checkpoint-based recovery. The intermediate queue adds durability for a failure mode that the Change Feed Processor handles natively.

**Cost estimate**: Azure Function + Queue service + consumer compute + embedding API calls.

### Comparison Matrix

| Criterion | A: Indexer + Integrated Vectorization | B: Change Feed Processor ★ | C: App-Level Eventing | D: Change Feed + Queue |
|-----------|---------------------------------------|----------------------------|----------------------|----------------------|
| **Operational Cost** | Medium (indexer charges + Function) | **Lowest** (no extra compute) | Medium (queue + consumer) | Highest (Function + queue + consumer) |
| **Simplicity** | Medium (less code, but Function + config) | **Good** (more code, fewer components) | Poor (queue + consumer + explicit publish) | Poor (most components) |
| **Reliability** | Good (Azure-managed, built-in retry) | **Good** (at-least-once, checkpoint recovery) | Medium (dual-write risk without Change Feed) | Good (most durable) |
| **Sync Lag** | 5-10 minutes | **Seconds** | Seconds (queue latency) | Seconds (multi-hop) |
| **Observability (FR-022/23/24)** | Poor (no custom OTel metrics) | **Excellent** (full ITelemetryService) | Good (custom code) | Good (custom code) |
| **Aspire Dev Loop** | Poor (no local indexer emulation) | **Excellent** (runs in-process) | Medium (needs local queue) | Poor (multiple local services) |
| **Initial Bulk Index** | Good (indexer runs from beginning) | **Good** (Change Feed start from beginning) | Poor (requires separate migration) | Medium (Change Feed catches up) |

### Rationale

Option B (Change Feed Processor as Background Service) is recommended because:

1. **Lowest operational cost** (FR-007 priority 1): No additional Azure compute or services. Runs in the existing Container Apps instance. Only incremental cost is embedding API calls (common to all options).
2. **Simplest overall** (FR-007 priority 2): Although it requires more application code than the indexer approach, it has fewer infrastructure components (zero new Azure services vs. Azure Function for Option A, queue + consumer for C/D). The code follows existing Clean Architecture patterns and is fully testable.
3. **Reliable** (FR-007 priority 3): Change Feed Processor provides at-least-once delivery with automatic checkpoint recovery. Dead-letter handling writes to Application Insights per FR-005.
4. **Meets observability requirements** (FR-022/23/24): Full control over custom metrics, tracing, and structured logging via the existing `ITelemetryService` pattern. The indexer approach cannot meet these requirements.
5. **Meets sync lag target** (SC-001): Near-real-time sync (seconds) vs. 5-10 minutes for the indexer approach.
6. **Aspire developer experience** (Constitution V): Runs as a separate project in the AppHost, starts with `dotnet run --project AppHost`, full dashboard visibility.\n7. **Separation of concerns**: Dedicated worker ensures sync failures don't impact API availability, and enables independent scaling and deployment.

### Alternatives Considered

- **Hybrid approach** (indexer for initial bulk load + Change Feed for incremental): Adds complexity without clear benefit. The Change Feed Processor supports `StartFromBeginning()` for initial population.
- **Separate worker project**: The Change Feed Processor is hosted in a dedicated `SearchIndexWorker` project for fault isolation, independent scaling, and deployment independence. This is the chosen approach.

## 2. AI Search SKU Discrepancy

### Decision: Update Bicep to Basic SKU

The existing `infra/main.bicep` provisions Azure AI Search at `standard` SKU. The spec clarification chose `basic` SKU (~$75/month, supports vector search, 1GB storage, 5 indexes). The Bicep should be updated to `basic` to align with the spec unless the environment already has a deployed `standard` instance (downgrade is not supported in-place).

**Rationale**: Basic SKU is sufficient for the expected workload (hundreds of worlds, thousands of entities per world). Vector search, hybrid search, and semantic search are supported on Basic tier per Microsoft documentation. The cost difference is significant (~$75/month basic vs. ~$250/month standard).

**Action**: Update `sku: 'standard'` to `sku: 'basic'` in `infra/main.bicep`. Note that `semanticSearch: 'standard'` refers to the semantic search *configuration* (not the service SKU) and can remain as-is on the Basic tier.

**Note**: If the `standard` SKU is already deployed in a production environment, a new AI Search resource at `basic` tier must be provisioned and the old one decommissioned. SKU cannot be changed in-place.

## 3. Embedding Field Concatenation Strategy

### Decision: Application-Code Concatenation in SearchIndexSyncService

The `SearchIndexSyncService` (Change Feed Processor delegate) concatenates the embedding source fields in application code:

```text
embeddingContent = $"{entity.Name} {entity.Description ?? ""} {string.Join(" ", entity.Tags ?? [])} {entity.Attributes ?? ""}"
```

**Rationale**: Simple, testable, and fully controlled. No dependency on Cosmos DB SQL query capabilities or external skills. The concatenation logic is a single line in the sync service, unit-testable, and easy to modify if the embedding content scope changes.

**Fields included** (per FR-003 clarification):

- `Name` — always present (required field)
- `Description` — may be null/empty
- `Tags` — `List<string>`, joined with spaces
- `Attributes` — JSON string representing Properties, may be null

**Fields excluded** (per FR-003 clarification):

- `SystemProperties` — system-specific mechanical data with low semantic search value

## 4. Lease Container for Change Feed Processor

### Decision: Dedicated Lease Container in Same Cosmos DB Database

The Change Feed Processor requires a lease container to track processing state. A dedicated `leases` container will be provisioned in the same Cosmos DB database (`libris-maleficarum`) with partition key `/id`. This aligns with the documented best practice for the Change Feed Processor.

**Cosmos DB provisioning**:

- Container name: `leases`
- Partition key: `/id`
- Throughput: 400 RU/s (minimum, autoscale if needed)
- Provisioned via: Bicep (production), Aspire AppHost (development)

**Cost**: Minimal — lease documents are small and infrequently updated. ~$24/month for 400 RU/s.

## 5. Initial Index Population Strategy

### Decision: Change Feed StartFromBeginning

When the feature is first deployed, all existing WorldEntity documents must be indexed. The Change Feed Processor supports a `WithStartFromBeginning()` configuration option that reads all existing documents from the container, plus any new changes going forward.

**Workflow**:

1. Deploy the API with the new `SearchIndexSyncService`
2. On first startup, the Change Feed Processor starts from the beginning of the WorldEntity container
3. All existing documents are processed (embeddings generated, pushed to index)
4. Once caught up, the processor continues monitoring for new changes

**Consideration**: Initial population of large containers may take time. The processor handles backpressure naturally — it processes in batches and checkpoints progress. If interrupted, it resumes from the last checkpoint.

## 6. Search Query Vectorization

### Decision: Application-Level Query Vectorization

When a search query is submitted to the API, the application generates a vector embedding for the query text using the same embedding model (`text-embedding-3-small`) and sends a hybrid query (text + vector) to Azure AI Search.

**Alternative considered**: Azure AI Search integrated vectorization at query time (vectorizer configured in the index). This would let the search service vectorize the query automatically. However, it requires configuring a vectorizer in the index schema pointing to the Azure OpenAI endpoint, which adds infrastructure coupling. Application-level vectorization gives full control and aligns with the Change Feed Processor approach.

**Update**: If the team later adopts the integrated vectorization approach for queries (to avoid the application making a separate embedding call per query), the index schema already supports adding a vectorizer configuration without reindexing. This is a non-breaking future enhancement.

## Summary of Decisions

| Topic | Decision | Rationale |
|-------|----------|-----------|
| Sync method | Change Feed Processor (BackgroundService) | Lowest cost, best observability, meets sync lag target |
| AI Search SKU | Update to Basic | Cost-effective, sufficient for workload |
| Embedding concatenation | Application code in SearchIndexSyncService | Simple, testable, fully controlled |
| Lease container | Dedicated `leases` container, partition key `/id` | Change Feed Processor requirement |
| Initial population | StartFromBeginning on first deployment | Built-in capability, checkpoint-based resumption |
| Query vectorization | Application-level (EmbeddingService) | Full control, consistent with sync approach |
