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
- API service with Change Feed Processor (BackgroundService)
- Frontend (Vite dev server)

### 2. Verify the Search Index Sync

Once the AppHost is running:

1. Create a WorldEntity via the API (POST to `/api/v1/worlds/{worldId}/entities`)
2. The Change Feed Processor detects the change within seconds
3. An embedding is generated for the entity content
4. The document is pushed to the Azure AI Search index
5. The entity is now searchable via `GET /api/v1/worlds/{worldId}/search?q=your+query`

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
┌─────────────────────────────────────────────────────┐
│                   API Service                        │
│                                                      │
│  ┌──────────────────┐    ┌────────────────────────┐ │
│  │ WorldEntities     │    │ SearchIndexSyncService  │ │
│  │ Controller         │    │ (BackgroundService)     │ │
│  │                    │    │                          │ │
│  │ GET .../search    │    │ Cosmos DB Change Feed    │ │
│  │   ↓               │    │   ↓                      │ │
│  │ ISearchService    │    │ IEmbeddingService        │ │
│  │   ↓               │    │   ↓                      │ │
│  │ AzureAISearch     │    │ ISearchIndexService      │ │
│  │ Service            │    │   ↓                      │ │
│  └──────┬───────────┘    └──────┬─────────────────┘ │
│          │                        │                    │
└──────────┼────────────────────────┼────────────────────┘
           │                        │
           ▼                        ▼
    ┌─────────────┐         ┌─────────────┐
    │ Azure AI    │         │ Azure AI    │
    │ Search      │◄────────│ Services    │
    │ (Index)     │         │ (Embeddings)│
    └─────────────┘         └─────────────┘
           ▲
           │
    ┌──────┴──────┐
    │ Cosmos DB   │
    │ (WorldEntity│
    │  container) │
    └─────────────┘
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
| `src/Api/Controllers/WorldEntitiesController.cs` | Search endpoint action (modified) |
| `src/Orchestration/AppHost/AppHost.cs` | Aspire resources for AI Search (modified) |
| `infra/main.bicep` | AI Search SKU update to basic (modified) |
