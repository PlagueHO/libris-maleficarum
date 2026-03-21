# Quickstart: Entity Search Index with Vector Search

**Feature**: 015-entity-search-index  
**Prerequisites**: .NET 10 SDK, Node.js 20+, pnpm, Docker (for Cosmos DB emulator)

## Developer Setup

### 1. Start the Aspire AppHost

The Aspire AppHost starts all services including the Cosmos DB emulator:

```bash
cd libris-maleficarum-service
dotnet run --project src/Orchestration/AppHost
```

The AppHost provisions:

- Cosmos DB emulator (Linux container) with WorldEntity and leases containers
- Azurite (Azure Storage emulator)
- API service with search query support
- SearchIndexWorker with Change Feed Processor (index sync)
- Frontend (Vite dev server)

### 2. Verify the Search Index Sync

Once the AppHost is running:

1. Create a WorldEntity via the API (POST to `/api/v1/worlds/{worldId}/entities`)
1. The Change Feed Processor detects the change within seconds
1. An embedding is generated for the entity content
1. The document is pushed to the Azure AI Search index
1. The entity is now searchable via `GET /api/v1/worlds/{worldId}/search?q=your+query`

### 3. Search API Usage

```bash
# Hybrid search (text + vector) — default mode
curl "https://127.0.0.1:5001/api/v1/worlds/{worldId}/search?q=ancient+elven+city"

# Text-only search
curl "https://127.0.0.1:5001/api/v1/worlds/{worldId}/search?q=dragon&mode=text"

# Vector-only search
curl "https://127.0.0.1:5001/api/v1/worlds/{worldId}/search?q=mysterious+forest&mode=vector"

# Filtered search
curl "https://127.0.0.1:5001/api/v1/worlds/{worldId}/search?q=warrior&entityType=Character&tags=guild,fighter"

# Paginated search
curl "https://127.0.0.1:5001/api/v1/worlds/{worldId}/search?q=quest&limit=10&offset=20"
```

### 4. Run Tests

```bash
# Unit tests
dotnet test --solution LibrisMaleficarum.slnx --filter "TestCategory=Unit" --configuration Release

# Integration tests (requires AppHost running or Docker)
dotnet test --solution LibrisMaleficarum.slnx --filter "TestCategory=Integration" --configuration Release

# Specific search tests
dotnet test --filter "FullyQualifiedName~Search" --configuration Release
```

## Configuration

### Application Settings

The following settings configure the search feature (set via `appsettings.json`, environment variables, or Aspire resource injection):

| Setting | Default | Description |
|---------|---------|-------------|
| `Search:EmbeddingModelName` | `text-embedding-3-small` | Azure AI Services embedding model |
| `Search:EmbeddingDimensions` | `1536` | Vector dimensions |
| `Search:IndexName` | `worldentity-index` | Azure AI Search index name |
| `Search:MaxBatchSize` | `100` | Maximum documents per index batch |
| `Search:ChangeFeedPollIntervalMs` | `1000` | Change Feed poll interval (milliseconds) |

### Local Development (Aspire AppHost)

In the Aspire AppHost, the API service receives connection strings for:

- **Cosmos DB**: Injected via `.AddAzureCosmosDB()` (emulator in dev)
- **Azure AI Search**: Injected via `.AddConnectionString("search")` (requires local or cloud instance)
- **Azure AI Services**: Injected via `.AddConnectionString("aiservices")` (requires cloud instance or mock)

> **Note**: Azure AI Search does not have a local emulator. For local development, either:
> (a) Use a cloud Basic-tier instance (recommended for integration testing),
> (b) Mock the `ISearchIndexService` and `ISearchService` interfaces for unit testing, or
> (c) Use the Aspire connection string pointing to a shared dev instance.

## Architecture Overview

```text
┌─────────────────────────────────────┐
│           API Service               │
│                                     │
│  ┌──────────────────┐              │
│  │ WorldEntities     │              │
│  │ Controller         │              │
│  │                    │              │
│  │ GET .../search    │              │
│  │   ↓               │              │
│  │ ISearchService    │              │
│  │   ↓               │              │
│  │ AzureAISearch     │              │
│  │ Service            │              │
│  └──────┬───────────┘              │
│          │                           │
└──────────┼───────────────────────────┘
           │
           ▼
    ┌─────────────┐         ┌─────────────────────────────────┐
    │ Azure AI    │         │     SearchIndexWorker Service    │
    │ Search      │◄────────│     (Dedicated Worker Process)   │
    │ (Index)     │         │                                   │
    └─────────────┘         │  SearchIndexSyncService           │
           ▲                │  (BackgroundService)              │
           │                │                                   │
    ┌──────┴──────┐         │  Cosmos DB Change Feed            │
    │ Cosmos DB   │────────►│    ↓                              │
    │ (WorldEntity│         │  IEmbeddingService ──► Azure AI   │
    │  container) │         │    ↓                   Services   │
    └─────────────┘         │  ISearchIndexService              │
                            └───────────────────────────────────┘
```

## Key Files

| File | Purpose |
|------|---------|
| `src/Domain/Interfaces/Services/ISearchService.cs` | Search query abstraction (modified) |
| `src/Domain/Interfaces/Services/ISearchIndexService.cs` | Index sync abstraction (new) |
| `src/Domain/Interfaces/Services/IEmbeddingService.cs` | Embedding generation abstraction (new) |
| `src/Domain/Models/SearchIndexDocument.cs` | Index document model (new) |
| `src/Domain/Models/SearchRequest.cs` | Search query model (new) |
| `src/Domain/Models/SearchResult.cs` | Search result model (new) |
| `src/Infrastructure/Services/AzureAISearchService.cs` | AI Search query implementation (new) |
| `src/Infrastructure/Services/SearchIndexSyncService.cs` | Change Feed Processor + index push (new) |
| `src/Infrastructure/Services/EmbeddingService.cs` | Azure AI Services embedding client (new) |
| `src/Worker/SearchIndexWorker/Program.cs` | Worker host for SearchIndexSyncService (new) |
| `src/Worker/SearchIndexWorker/LibrisMaleficarum.SearchIndexWorker.csproj` | Worker Service project (new) |
| `src/Api/Controllers/WorldEntitiesController.cs` | Search endpoint action (modified) |
| `src/Orchestration/AppHost/AppHost.cs` | Aspire resources for AI Search + SearchIndexWorker (modified) |
| `infra/main.bicep` | AI Search SKU update to basic (modified) |

## Index Extensibility (FR-011)

Azure AI Search supports **additive schema changes** without reindexing. To add a new field:

1. Add the field definition in `AzureAISearchService.EnsureIndexExistsAsync()`
1. Add the property to `SearchIndexDocument`
1. Map the field in `SearchIndexSyncService.MapToSearchDocument()`
1. Call `EnsureIndexExistsAsync()` — it uses `CreateOrUpdateIndex`, which adds new fields to an existing index without affecting existing documents
1. New documents will have the field populated; existing documents will have `null` until re-indexed

**No reindex required** for additive fields. To backfill existing documents, trigger a re-sync by resetting the Change Feed lease checkpoint.

## Knowledge Base Registration (Azure AI Foundry)

The search index schema is designed to be compatible with Azure AI Search as a Knowledge Source for Azure AI Foundry agents.

### Compatibility Checklist

- ✅ Vector field (`contentVector`) uses HNSW algorithm with cosine metric
- ✅ Text-searchable fields (`name`, `description`, `tags`, `attributes`) support full-text search
- ✅ Semantic search configuration defined with `name` as title, `description`/`attributes` as content
- ✅ Metadata fields (`entityType`, `worldId`, `tags`) support filtering
- ✅ Standard index schema compatible with Knowledge Store connector

### Future Registration Steps

1. In Azure AI Foundry portal, navigate to **Knowledge** → **Add Knowledge Source**
1. Select **Azure AI Search** as the source type
1. Provide the search service endpoint and index name (`worldentity-index`)
1. Map fields: content=`description`, title=`name`, vector=`contentVector`
1. Configure filtering on `worldId` for tenant isolation
1. The index is ready for use with Foundry IQ agents
