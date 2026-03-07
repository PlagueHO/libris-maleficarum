# Data Model: Entity Search Index

**Date**: 2026-03-07 | **Feature**: 015-entity-search-index  
**Purpose**: Define the Azure AI Search index schema, new domain models, and mapping from WorldEntity to search index documents.

## Azure AI Search Index Schema

### Index Name

`worldentity-index`

### Fields

| Field Name | Type | Key | Searchable | Filterable | Sortable | Facetable | Retrievable | Notes |
|------------|------|-----|------------|------------|----------|-----------|-------------|-------|
| `id` | `Edm.String` | Yes | No | No | No | No | Yes | WorldEntity.Id (GUID as string) |
| `worldId` | `Edm.String` | No | No | Yes | No | No | Yes | Partition filter — required on all queries |
| `entityType` | `Edm.String` | No | No | Yes | No | Yes | Yes | EntityType enum value as string |
| `name` | `Edm.String` | No | Yes | Yes | Yes | No | Yes | Full-text searchable + filterable |
| `description` | `Edm.String` | No | Yes | No | No | No | Yes | Full-text searchable |
| `tags` | `Collection(Edm.String)` | No | Yes | Yes | No | Yes | Yes | Tags collection — searchable + filterable |
| `parentId` | `Edm.String` | No | No | Yes | No | No | Yes | Nullable — null for root entities |
| `ownerId` | `Edm.String` | No | No | Yes | No | No | Yes | Owner filter |
| `createdDate` | `Edm.DateTimeOffset` | No | No | Yes | Yes | No | Yes | UTC timestamp |
| `modifiedDate` | `Edm.DateTimeOffset` | No | No | Yes | Yes | No | Yes | UTC timestamp |
| `path` | `Collection(Edm.String)` | No | No | Yes | No | No | Yes | Ancestor IDs for hierarchy queries |
| `depth` | `Edm.Int32` | No | No | Yes | Yes | No | Yes | Hierarchy level |
| `attributes` | `Edm.String` | No | Yes | No | No | No | Yes | JSON string — full-text searchable |
| `schemaVersion` | `Edm.Int32` | No | No | Yes | No | No | Yes | Schema version for compatibility |
| `contentVector` | `Collection(Edm.Single)` | No | No | No | No | No | No | 1536-dimension vector embedding |

### Vector Search Configuration

```json
{
  "vectorSearch": {
    "algorithms": [
      {
        "name": "hnsw-algorithm",
        "kind": "hnsw",
        "hnswParameters": {
          "metric": "cosine",
          "m": 4,
          "efConstruction": 400,
          "efSearch": 500
        }
      }
    ],
    "profiles": [
      {
        "name": "vector-profile",
        "algorithmConfigurationName": "hnsw-algorithm"
      }
    ]
  }
}
```

### Vector Field Configuration

The `contentVector` field uses:

- **Algorithm**: HNSW (Hierarchical Navigable Small World)
- **Metric**: Cosine similarity
- **Dimensions**: 1536 (matches `text-embedding-3-small` default; configurable)
- **Profile**: `vector-profile`

### Semantic Configuration (Optional Enhancement)

For hybrid search with semantic ranking:

```json
{
  "semantic": {
    "configurations": [
      {
        "name": "semantic-config",
        "prioritizedFields": {
          "titleField": { "fieldName": "name" },
          "contentFields": [
            { "fieldName": "description" },
            { "fieldName": "attributes" }
          ],
          "keywordsFields": [
            { "fieldName": "tags" }
          ]
        }
      }
    ]
  }
}
```

## Domain Models

### SearchIndexDocument (Domain/Models/)

Represents a WorldEntity document mapped for the search index. Used by `ISearchIndexService` to push documents to Azure AI Search.

```csharp
namespace LibrisMaleficarum.Domain.Models;

public class SearchIndexDocument
{
    public required string Id { get; init; }
    public required string WorldId { get; init; }
    public required string EntityType { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required List<string> Tags { get; init; }
    public string? ParentId { get; init; }
    public required string OwnerId { get; init; }
    public required DateTimeOffset CreatedDate { get; init; }
    public required DateTimeOffset ModifiedDate { get; init; }
    public required List<string> Path { get; init; }
    public required int Depth { get; init; }
    public string? Attributes { get; init; }
    public required int SchemaVersion { get; init; }
    public required ReadOnlyMemory<float> ContentVector { get; init; }
}
```

### SearchRequest (Domain/Models/)

Represents a search query from the API layer to the search service.

```csharp
namespace LibrisMaleficarum.Domain.Models;

public class SearchRequest
{
    public required Guid WorldId { get; init; }
    public required string Query { get; init; }
    public SearchMode Mode { get; init; } = SearchMode.Hybrid;
    public EntityType? EntityTypeFilter { get; init; }
    public List<string>? TagsFilter { get; init; }
    public string? NameFilter { get; init; }
    public Guid? ParentIdFilter { get; init; }
    public int Limit { get; init; } = 50;
    public int Offset { get; init; } = 0;
}

public enum SearchMode
{
    Text,
    Vector,
    Hybrid
}
```

### SearchResult (Domain/Models/)

Represents a single search result returned by the search service.

```csharp
namespace LibrisMaleficarum.Domain.Models;

public class SearchResult
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string EntityType { get; init; }
    public string? DescriptionSnippet { get; init; }
    public required double RelevanceScore { get; init; }
    public required Guid WorldId { get; init; }
    public Guid? ParentId { get; init; }
    public required List<string> Tags { get; init; }
    public required string OwnerId { get; init; }
    public required DateTimeOffset CreatedDate { get; init; }
    public required DateTimeOffset ModifiedDate { get; init; }
}
```

### SearchResultSet (Domain/Models/)

Represents a paginated set of search results.

```csharp
namespace LibrisMaleficarum.Domain.Models;

public class SearchResultSet
{
    public required List<SearchResult> Results { get; init; }
    public required int TotalCount { get; init; }
    public required int Offset { get; init; }
    public required int Limit { get; init; }
}
```

## Domain Interfaces

### ISearchIndexService (Domain/Interfaces/Services/)

Abstraction for index synchronization operations.

```csharp
namespace LibrisMaleficarum.Domain.Interfaces.Services;

using LibrisMaleficarum.Domain.Models;

public interface ISearchIndexService
{
    Task IndexDocumentAsync(SearchIndexDocument document, CancellationToken cancellationToken = default);
    Task IndexDocumentsBatchAsync(IEnumerable<SearchIndexDocument> documents, CancellationToken cancellationToken = default);
    Task RemoveDocumentAsync(string documentId, CancellationToken cancellationToken = default);
    Task RemoveDocumentsBatchAsync(IEnumerable<string> documentIds, CancellationToken cancellationToken = default);
}
```

### IEmbeddingService (Domain/Interfaces/Services/)

Abstraction for vector embedding generation.

```csharp
namespace LibrisMaleficarum.Domain.Interfaces.Services;

public interface IEmbeddingService
{
    Task<ReadOnlyMemory<float>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReadOnlyMemory<float>>> GenerateEmbeddingsBatchAsync(IEnumerable<string> texts, CancellationToken cancellationToken = default);
}
```

### ISearchService (Modified)

The existing `ISearchService` interface is updated to support the new search models:

```csharp
namespace LibrisMaleficarum.Domain.Interfaces.Services;

using LibrisMaleficarum.Domain.Models;

public interface ISearchService
{
    Task<SearchResultSet> SearchAsync(SearchRequest request, CancellationToken cancellationToken = default);
}
```

Note: This is a breaking change to the existing interface. The old `SearchEntitiesAsync` method signature is replaced. The `WorldEntitiesController` and tests must be updated accordingly.

## Mapping: WorldEntity → SearchIndexDocument

The `SearchIndexSyncService` (Change Feed Processor delegate) maps `WorldEntity` to `SearchIndexDocument`:

| WorldEntity Field | SearchIndexDocument Field | Transformation |
|-------------------|---------------------------|----------------|
| `Id` | `Id` | `Guid.ToString()` |
| `WorldId` | `WorldId` | `Guid.ToString()` |
| `EntityType` | `EntityType` | `Enum.ToString()` |
| `Name` | `Name` | Direct |
| `Description` | `Description` | Direct (nullable) |
| `Tags` | `Tags` | Direct |
| `ParentId` | `ParentId` | `Guid?.ToString()` (nullable) |
| `OwnerId` | `OwnerId` | Direct |
| `CreatedDate` | `CreatedDate` | `DateTime` to `DateTimeOffset` |
| `ModifiedDate` | `ModifiedDate` | `DateTime` to `DateTimeOffset` |
| `Path` | `Path` | `List<Guid>` to `List<string>` via `.Select(g => g.ToString())` |
| `Depth` | `Depth` | Direct |
| `Attributes` | `Attributes` | Direct (JSON string) |
| `SchemaVersion` | `SchemaVersion` | Direct |
| (computed) | `ContentVector` | Embedding of concatenated `Name + Description + Tags + Attributes` |

### Embedding Content Concatenation

```text
embeddingContent = "{Name} {Description ?? ""} {string.Join(" ", Tags ?? [])} {Attributes ?? ""}"
```

Fields are space-separated. Null/empty fields are replaced with empty strings to avoid "null" in the embedding text.

## Cosmos DB Lease Container

The Change Feed Processor requires a lease container:

| Property | Value |
|----------|-------|
| Container name | `leases` |
| Partition key | `/id` |
| Throughput | 400 RU/s (shared with autoscale) |
| Database | Same as WorldEntity (`libris-maleficarum`) |

## Soft-Delete Handling

When the Change Feed Processor detects an entity with `IsDeleted = true`, it calls `ISearchIndexService.RemoveDocumentAsync()` to remove the document from the search index. Soft-deleted entities must never appear in search results (FR-004).

The Change Feed reads the latest version of each document. If a document is soft-deleted, the processor sees `IsDeleted = true` and removes it from the index. When the TTL expires and Cosmos DB purges the document, no further action is needed — the document is already absent from the index.
