# Data Model and Persistence Strategy

The purpose of this document is to describe the structure and strategy for persisting TTRPG-related data in Azure Cosmos DB. It covers the main entity types, their properties, relationships, and indexing strategies to support efficient querying, scalability, and flexibility for various TTRPG systems.

This document is intended for backend developers, database architects, and system designers. An API specification document will complement this by detailing RESTful endpoints.

> [!NOTE]
> This document does not contain implementation code but provides detailed data models and design rationale to guide development. It focuses on Cosmos DB schema design, partitioning strategies, and common query patterns. It will contain sample SQL statements to illustrate querying approaches and estimate RU costs for common operations.

## Overview of Data Model

The core data model revolves around the concept of a "World" or "Campaign", which serves as a container for all related entities such as locations, characters, items, and sessions. Each World is owned by a user and can contain an arbitrary hierarchy of nested entities. A user can own multiple Worlds, but will only interact with one World at a time in the application.

## Cosmos DB Containers

Four containers store all TTRPG data, each with optimized partition keys for their access patterns:

| Container | Purpose | Partition Key | Key Characteristics |
| --------- | ------- | ------------- | ------------------- |
| **WorldEntity** | Active world/campaign data | `[/WorldId, /id]` | All entities (worlds, locations, characters, campaigns, etc.) with hierarchical relationships via `ParentId` |
| **WorldMetadata** | Materialized world hierarchy and metadata | `/WorldId` | Single document per world; lightweight hierarchy tree for efficient UI rendering (1 RU point read); max 5000 entities per world |
| **Asset** | Asset metadata (images, audio, video, documents) | `[/WorldId, /EntityId]` | References to Azure Blob Storage; separate from entities to prevent document bloat; partitioned by entity for efficient entity-scoped queries |
| **DeletedWorldEntity** | Soft-deleted entities | `[/WorldId, /id]` | Moved from WorldEntity after 30-day grace period; TTL auto-purges after 90 days |

> [!IMPORTANT]
> Partition key design for each container is critical to ensure RU usage is kept as low as possible. Hot partitions must also be avoided. Hierarchical partition keys (e.g., `[/WorldId, /id]`, `[/WorldId, /EntityId]`) enable efficient queries while distributing load across partitions. WorldMetadata uses simple key `/WorldId` since one document per world is optimal.

## Common Operations

This section outlines common operations and their estimated RU costs for each container. It is used to guide design decisions and optimize for cost efficiency, performance and to reduce RU usage and limit hot partitions.

| Operation | Container | Parameters | RU Cost | Notes |
| --------- | --------- | ---------- | ------- | ----- |
| **Get entity by ID** | WorldEntity | `worldId`, `entityId` | 1 RU | Point read with both partition keys |
| **Get world children** | WorldEntity | `worldId`, `parentId` | 2-5 RUs | Single-partition query |
| **Get world hierarchy** | WorldMetadata | `worldId` | 1 RU | Point read; lightweight tree structure for UI navigation |
| **List all world entities** | WorldEntity | `worldId` | 5-15 RUs | Partition prefix query for full entity data; use WorldMetadata for hierarchy-only queries |
| **List entities by type** | WorldEntity | `worldId`, `entityType` | 3-8 RUs | Filtered partition prefix query |
| **Search entities** | WorldEntity | `worldId`, `searchTerm` | 5-12 RUs | Name/tag search within world |
| **Count children** | WorldEntity | `worldId`, `parentId` | 2-3 RUs | Aggregate query |
| **Get entities by owner** | WorldEntity | `ownerId` | 10-50 RUs | Cross-partition (use sparingly) |
| **Create entity** | WorldEntity | `entity` | 5-8 RUs | Includes indexing overhead |
| **Update entity** | WorldEntity | `worldId`, `entityId`, `updates` | 6-12 RUs | Read (1 RU) + write (5-8 RUs) |
| **Soft delete entity** | WorldEntity | `worldId`, `entityId` | 6-10 RUs | Mark `IsDeleted = true` |
| **Move entity** | WorldEntity | `worldId`, `entityId`, `newParentId` | 6-10 RUs | Update `ParentId`, `Path`, `Depth` |
| **Get entity assets** | Asset | `worldId`, `entityId` | 2-5 RUs | Filtered partition prefix query |
| **Create asset** | Asset | `asset` | 5-7 RUs | Asset metadata only |
| **Delete asset** | Asset | `worldId`, `assetId` | 6-8 RUs | Soft delete + blob delete |
| **Get deleted entities** | DeletedWorldEntity | `worldId` | 5-10 RUs | Partition prefix query |
| **Restore entity** | DeletedWorldEntity | `worldId`, `entityId` | 12-20 RUs | Move back to WorldEntity |
| **Purge entity** | DeletedWorldEntity | `worldId`, `entityId` | 1 RU | Permanent delete (auto via TTL) |

## Container Details

## WorldEntity Container

World Entity documents store all active world/campaign data with hierarchical partition key `[/WorldId, /id]` for hot partition prevention.

- **WorldEntity**: The core document type for all campaign/world data. Each "WorldEntity" can represent a "World" (root), or any nested entity such as "Continent", "Country", "Region", "City", "Character", etc. Entities can be arbitrarily nested to support complex world-building.
- **Ownership**: Every WorldEntity includes an `OwnerId` property (e.g., a user ID) to indicate the owner.
- **Document IDs**: All WorldEntities use a GUID for the `id` property to ensure uniqueness.
- **Partition Key**: The container uses a **hierarchical partition key** (`[/WorldId, /id]`) to:
  - **Distribution**: Each entity is its own logical partition (avoids hot partitions during active sessions)
  - **Flexible Queries**: Support both point reads (WorldId + id) and prefix queries (WorldId only)
  - **Scalability**: No per-world partition limits; scales to unlimited entities per world
  - **Hot Partition Prevention**: Active worlds with concurrent users distribute load across partitions
- **Query Optimization**: This partition strategy enables:
  - Point reads: 1 RU (WorldId + id specified)
  - World tree queries: ~5-15 RUs (partition key prefix query: `/WorldId/*`)
  - Children queries: ~2-5 RUs (prefix query with ParentId filter)
  - No 10K RU/s per-partition limit for busy worlds
- **Hierarchy Navigation**: Parent-child relationships maintained through `ParentId` references only - no denormalized arrays to keep in sync
- **External Indexing**: All WorldEntities are also indexed by Azure AI Search for advanced semantic ranking, hybrid search, and cross-world discovery

### WorldEntity Base Schema

All WorldEntity documents inherit from a base schema that defines core required properties. This ensures consistency and enables generic operations across all entity types.

```csharp
// Base entity for all world documents in Cosmos DB
public record BaseWorldEntity
{
    // === Core Identity (REQUIRED) ===
    public required string id { get; init; }           // GUID - unique identifier
    public required string WorldId { get; init; }      // GUID - root world/tenant ID (partition key level 1)
    public required string? ParentId { get; init; }    // GUID - parent entity ID, null for root World (partition key level 2)
    public required string EntityType { get; init; }   // Discriminator for entity-specific properties
    public string? SchemaId { get; init; }             // References property schema (e.g., "dnd5e-character", "fantasy-location")
    
    // === Hierarchy ===
    public List<string>? Path { get; init; }           // Breadcrumb path from root (for UI display)
    public required int Depth { get; init; }           // Hierarchy depth (0 = root World)
    
    // === Core Metadata (REQUIRED) ===
    public required string Name { get; init; }         // Display name (indexed)
    public string? Description { get; init; }          // Rich text description
    public List<string>? Tags { get; init; }           // User-defined tags for search/filtering
    
    // === Ownership & Permissions (REQUIRED) ===
    public required string OwnerId { get; init; }      // User ID who owns this entity
    public string? CreatedBy { get; init; }            // User ID who created
    public string? ModifiedBy { get; init; }           // User ID who last modified
    
    // === Temporal (REQUIRED) ===
    public required DateTime CreatedDate { get; init; }   // ISO 8601 timestamp
    public required DateTime ModifiedDate { get; init; }  // ISO 8601 timestamp
    
    // === Soft Delete (REQUIRED) ===
    public required bool IsDeleted { get; init; }      // Soft delete flag (default: false)
    public DateTime? DeletedDate { get; init; }        // ISO 8601 timestamp when deleted
    public string? DeletedBy { get; init; }            // User ID who deleted
    
    // === Entity Properties (Flexible Schema) ===
    public object? Properties { get; init; }           // Common properties (cross-system)
    public object? SystemProperties { get; init; }     // System-specific properties (D&D 5e, Pathfinder, etc.)
}
```

### WorldEntity Subclass Schema

Subclasses of WorldEntity define specific entity types with their own property sets. The `Properties` and `SystemProperties` fields store flexible JSON objects based on the selected `SchemaId`.

```csharp
// Entity-specific records inherit from base
// Properties store common cross-system data; SystemProperties store system-specific data
// SchemaId references which property template/schema to use for validation and UI generation

// Example: Character entity with hybrid property system
public record CharacterEntity : BaseWorldEntity
{
    // EntityType: "Character", "PlayerCharacter", or "NonPlayerCharacter"
    // SchemaId: "dnd5e-character", "pathfinder2e-character", "fantasy-character", etc.
    
    // Properties (common across most systems):
    // - Name, Level, Class, Ancestry/Race, Background, etc.
    
    // SystemProperties (D&D 5e example):
    // - Stats: { STR: 12, DEX: 18, CON: 14, INT: 13, WIS: 16, CHA: 10 }
    // - ArmorClass: 16
    // - HitPoints: { Current: 45, Max: 52 }
    // - SpellSlots: { "1st": 4, "2nd": 3 }
}

// Example: Location entity with hybrid property system
public record LocationEntity : BaseWorldEntity
{
    // EntityType: "Continent", "Country", "City", "Dungeon", etc.
    // SchemaId: "fantasy-location", "scifi-location", "modern-location", etc.
    
    // Properties (common):
    // - Population, Climate, Terrain, Government, etc.
    
    // SystemProperties (fantasy example):
    // - MagicLevel: "High"
    // - Resources: ["mithril", "ancient artifacts"]
    // - Coordinates: { Latitude: 45.5, Longitude: -122.6 }
}

// Example: Campaign entity with hybrid property system
public record CampaignEntity : BaseWorldEntity
{
    // EntityType: "Campaign", "Scenario", "Session", "Scene"
    // SchemaId: "dnd5e-campaign", "generic-campaign", etc.
    
    // Properties (common):
    // - Status: "active", StartDate, PlayerCount, SessionCount, etc.
    
    // SystemProperties (system-specific):
    // - Players: [{ UserId: "user1", CharacterId: "char1", JoinedDate: "2024-01-15" }]
    // - CampaignArcs: [{ Name: "Arc 1", Status: "completed" }]
    // - HouseRules: { "CriticalHits": "maximize damage" }
}
```

### Property Schema for System Property Validation

System property schemas/templates are stored in a separate container or configuration store. Each schema defines the expected properties, data types, validation rules, and default values for a given system/entity type.

```csharp
// Property Schema/Template Document (stored separately in Cosmos DB)
public record PropertySchema
{
    public required string id { get; init; }           // "dnd5e-character"
    public required string Name { get; init; }         // "D&D 5e Character"
    public required string EntityType { get; init; }   // "Character"
    public string? Description { get; init; }          // "Properties for D&D 5th Edition characters"
    public List<PropertyDefinition>? CommonProperties { get; init; }  // Common property definitions
    public List<PropertyDefinition>? SystemProperties { get; init; }  // System-specific definitions
    public DateTime CreatedDate { get; init; }
    public DateTime ModifiedDate { get; init; }
}

public record PropertyDefinition
{
    public required string Name { get; init; }         // "Level", "ArmorClass", etc.
    public required string DataType { get; init; }     // "string", "int", "object", "array"
    public bool Required { get; init; }                // Is this property required?
    public object? DefaultValue { get; init; }         // Default value if not specified
    public string? ValidationRule { get; init; }       // "min:1,max:20", "enum:Good,Neutral,Evil"
    public string? DisplayName { get; init; }          // "Armor Class" (for UI)
    public string? Description { get; init; }          // Help text for UI
}
```

## WorldMetadata Container

The WorldMetadata container stores a materialized view of each world's entity hierarchy for efficient UI rendering. This follows the CQRS pattern, separating read-optimized hierarchy queries from write-optimized entity storage.

**Purpose**: Avoid expensive partition prefix queries when loading world navigation trees.

**Performance**: 
- **Without WorldMetadata**: Loading hierarchy for 1000-entity world = 50-200 RUs (query all entities)
- **With WorldMetadata**: Loading hierarchy = 1 RU (point read single document)

**Partition Key**: `/WorldId` (simple key - one document per world)

**Hard Limit**: Maximum 5000 entities per world to keep document size <2MB (Cosmos DB limit)

### WorldMetadata Schema

```csharp
// WorldMetadata Container - Read model for world hierarchy
public record WorldMetadata
{
    // === Identity ===
    public required string id { get; init; }              // Same as WorldId (enables point reads)
    public required string WorldId { get; init; }         // Partition key
    
    // === Hierarchy (Materialized View) ===
    public required WorldHierarchyNode HierarchyRoot { get; init; }
    
    // === Metadata ===
    public required DateTime LastUpdated { get; init; }
    public required string LastUpdatedBy { get; init; }
    public required int Version { get; init; }            // Optimistic concurrency control
    
    // Future: Could include aggregated statistics (TotalEntities, EntitiesByType, etc.)
}

// Lightweight hierarchy node - no Properties/SystemProperties to minimize size
public record WorldHierarchyNode
{
    public required string Id { get; init; }              // Entity ID
    public required string Name { get; init; }            // Display name
    public required string EntityType { get; init; }      // For UI icons/styling
    public int Depth { get; init; }                       // Hierarchy depth
    public string? IconAssetId { get; init; }             // Custom icon reference
    public bool IsDeleted { get; init; }                  // Show/hide in tree
    public List<WorldHierarchyNode>? Children { get; init; }  // Nested child entities
}
```

### Synchronization Strategy

WorldMetadata documents are kept in sync with WorldEntity changes via **Azure Functions Change Feed Processor**:

1. **Change Detection**: Change Feed on WorldEntity container triggers when entities are created, updated, or deleted
2. **Hierarchy Update**: Function reads current WorldMetadata document, applies incremental changes to hierarchy tree
3. **Write Back**: Updated WorldMetadata document written with optimistic concurrency (Version/ETag)
4. **Consistency**: Eventual consistency (~1 second typical latency) - acceptable for UI navigation

**Update Operations**:
- Entity created/updated: Upsert node in hierarchy tree (update Name, EntityType, IconAssetId)
- Entity deleted: Mark node as `IsDeleted = true` or remove from tree
- Entity moved: Update parent reference in tree structure

**Change Feed Configuration**:
- Lease container: Tracks processing state across function instances
- Start from: Beginning (on first run) or continuation token (on restart)
- Batch size: 100-500 changes per batch for efficiency

### Query Patterns

```sql
-- Get full world hierarchy (point read - 1 RU)
SELECT * FROM m
WHERE m.id = @worldId
  AND m.WorldId = @worldId

-- Example result structure
{
  "id": "world-guid",
  "WorldId": "world-guid",
  "HierarchyRoot": {
    "Id": "world-guid",
    "Name": "Eldoria",
    "EntityType": "World",
    "Depth": 0,
    "Children": [
      {
        "Id": "continent-guid",
        "Name": "Arcanis",
        "EntityType": "Continent",
        "Depth": 1,
        "Children": [
          {
            "Id": "country-guid",
            "Name": "Valoria",
            "EntityType": "Country",
            "Depth": 2,
            "Children": []
          }
        ]
      }
    ]
  },
  "LastUpdated": "2024-06-25T10:30:00Z",
  "LastUpdatedBy": "change-feed-processor",
  "Version": 142
}
```

### Size Estimation

- **Average node size**: ~200 bytes (id, name, type, depth, children array references)
- **1000 entities**: ~200 KB document
- **5000 entities** (hard limit): ~1 MB document (safe margin below 2 MB Cosmos limit)

**Document Size Management**:
- Monitor document size via Application Insights
- Warn users approaching 4500 entities (90% of limit)
- Block entity creation at 5000 entities per world
- Recommend splitting into multiple worlds for very large campaigns

### Cost Analysis

| Operation | Without WorldMetadata | With WorldMetadata | Savings |
|-----------|----------------------|-------------------|--------|
| Load world tree (1000 entities) | 50-200 RUs | 1 RU | 98-99% |
| Update single entity | 6-12 RUs | 6-12 RUs (entity) + 6-10 RUs (hierarchy) | -50% overhead |
| Typical session (10 loads, 5 updates) | 500-2000 RUs | 10 + 30-60 + 30-50 = 70-120 RUs | 85-95% |

**Net Benefit**: Read-heavy workloads (typical for world navigation) see massive RU savings despite write amplification.

## Asset Container

Asset metadata is stored in a dedicated `Asset` container (separate from WorldEntity) to:

- **Avoid Document Bloat**: WorldEntity documents remain small and efficient (no large asset arrays)
- **Reduce Write Costs**: Adding/updating assets doesn't require reading/writing entire entity document
- **Enable Asset Queries**: Query assets across entities without scanning all WorldEntities
- **Simplify Lifecycle**: Assets can be managed independently (cleanup, migration, archival)

**Partition Key Strategy**: The Asset container uses hierarchical partition key `[/WorldId, /EntityId]` to:
- **Efficient Entity Queries**: All assets for an entity in single partition (2-5 RUs)
- **Hot Partition Prevention**: Assets distributed by entity, not aggregated per world
- **Scalability**: No per-world partition limits; scales with entity count
- **Point Reads**: 1 RU when querying by WorldId + EntityId + id

### Asset Containe Schema

```csharp
public record Asset
{
    // === Core Identity (REQUIRED) ===
    public required string id { get; init; }              // Unique asset ID (GUID)
    public required string WorldId { get; init; }         // Partition key level 1 - tenant isolation
    public required string EntityId { get; init; }        // Partition key level 2 - parent WorldEntity ID
    public required string EntityType { get; init; }      // Entity type (for context)
    
    // === Asset Metadata (REQUIRED) ===
    public required string Type { get; init; }            // "image", "audio", "video", "document", "map", "model3d"
    public string? Purpose { get; init; }                 // "portrait", "banner", "icon", "background", "thumbnail"
    public required string FileName { get; init; }        // Original filename
    public required string ContentType { get; init; }     // MIME type: "image/png", "audio/mp3"
    public required long Size { get; init; }              // File size in bytes
    
    // === Blob Storage Reference (REQUIRED) ===
    public required string BlobPath { get; init; }        // Relative path: {worldId}/{entityType}/{entityId}/{assetId}.ext
    public required string Url { get; init; }             // Full blob URL (with SAS token placeholder)
    public string? ThumbnailBlobPath { get; init; }       // Thumbnail path (for images)
    public string? ThumbnailUrl { get; init; }            // Thumbnail URL
    
    // === Type-Specific Metadata ===
    // Images
    public ImageDimensions? Dimensions { get; init; }     // Width and height for images
    // Audio/Video
    public double? Duration { get; init; }                // Duration in seconds
    public string? Codec { get; init; }                   // Audio/video codec info
    // Documents
    public int? PageCount { get; init; }                  // For PDFs, etc.
    public string? Format { get; init; }                  // "pdf", "docx", "markdown"
    
    // === Metadata (REQUIRED) ===
    public required string UploadedBy { get; init; }      // User ID who uploaded
    public required DateTime UploadedDate { get; init; }  // ISO 8601 timestamp
    public List<string>? Tags { get; init; }              // Searchable tags
    public string? Description { get; init; }             // Alt text, caption, or description
    
    // === Processing ===
    public string? Status { get; init; }                  // "pending", "processing", "ready", "failed"
    public string? ProcessingError { get; init; }         // Error message if processing failed
    
    // === Soft Delete (REQUIRED) ===
    public required bool IsDeleted { get; init; }         // Soft delete flag (default: false)
    public DateTime? DeletedDate { get; init; }           // When deleted
    public string? DeletedBy { get; init; }               // User who deleted
}

public record ImageDimensions(int Width, int Height);
```

**Partition Key**: `[/WorldId, /EntityId]` - assets partitioned by entity for efficient entity-scoped queries and hot partition prevention.

**WorldEntity Reference Pattern**:

Instead of embedding full asset metadata, WorldEntity documents reference assets by ID:

```csharp
// In BaseWorldEntity - minimal asset references
public string? PrimaryAssetId { get; init; }         // Primary/featured asset for this entity
public string? PortraitAssetId { get; init; }        // Character portraits
public string? TokenAssetId { get; init; }           // VTT tokens
public string? BannerAssetId { get; init; }          // Campaign/world banners
public string? IconAssetId { get; init; }            // List icons
public string? MapAssetId { get; init; }             // Location maps
```

**Query Patterns**:

```sql
-- Get all assets for an entity (single partition query)
SELECT * FROM a
WHERE a.WorldId = @worldId
  AND a.EntityId = @entityId
  AND a.IsDeleted = false
-- Cost: ~2-5 RUs (single partition with WorldId + EntityId)

-- Get specific asset type for entity (single partition query)
SELECT * FROM a
WHERE a.WorldId = @worldId
  AND a.EntityId = @entityId
  AND a.Type = 'image'
  AND a.IsDeleted = false
-- Cost: ~2-5 RUs (filtered within partition)

-- Get asset by ID (point read)
SELECT * FROM a
WHERE a.WorldId = @worldId
  AND a.EntityId = @entityId
  AND a.id = @assetId
-- Cost: 1 RU (point read with full partition key + id)

-- Get all assets for a world (cross-partition query)
SELECT * FROM a
WHERE a.WorldId = @worldId
  AND a.IsDeleted = false
-- Cost: ~10-30 RUs (partition prefix query across all entities)
```

**Cost Benefits**:

- **Add asset**: 5 RUs (write to Asset container only, no entity update needed unless setting as featured)
- **Update asset metadata**: 5 RUs (update Asset document only)
- **Get all entity assets**: 2-5 RUs (query Asset container by EntityId)
- **No document bloat**: Entity documents remain small regardless of asset count

### Azure Blob Storage Integration

Binary assets stored in Azure Blob Storage, referenced by Asset documents in Cosmos DB. This separation follows Azure best practices:

- **Cosmos DB**: Stores metadata, references, and searchable content (Asset documents ~1-2KB each)
- **Blob Storage**: Stores actual binary assets (optimized for large files, streaming, CDN integration)

### Blob Storage Organization

Assets are organized in a hierarchical folder structure for multi-tenancy and efficient management:

```text
Container: world-assets
│
├── {worldId}/                          # Tenant isolation by world
│   ├── world/
│   │   └── {worldId}/
│   │       ├── banner-{assetId}.jpg
│   │       └── icon-{assetId}.png
│   │
│   ├── character/
│   │   └── {characterId}/
│   │       ├── portrait-{assetId}.png
│   │       ├── token-{assetId}.png
│   │       └── bio-{assetId}.pdf
│   │
│   ├── location/
│   │   └── {locationId}/
│   │       ├── map-{assetId}.jpg
│   │       ├── ambience-{assetId}.mp3
│   │       └── description-{assetId}.md
│   │
│   ├── campaign/
│   │   └── {campaignId}/
│   │       ├── session01-{assetId}.mp3
│   │       ├── notes-{assetId}.pdf
│   │       ├── handout-{assetId}.jpg
│   │       └── battlemap-{assetId}.png
│   │
│   └── thumbnails/                     # Auto-generated thumbnails
│       └── {assetId}-thumb.jpg
│
└── shared/                             # Shared assets (icons, templates)
    ├── entity-icons/
    └── templates/
```

**Path Construction**:

```csharp
// Primary asset path
var blobPath = $"{worldId}/{entityType.ToLower()}/{entityId}/{purpose}-{assetId}.{extension}";

// Example
// "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f/character/c9d8e7f6-5a4b-3c2d-1e0f-9a8b7c6d5e4f/portrait-a1b2c3d4.png"
```

### Example: Asset Documents in Asset Container

```json
// Asset document for map
{
  "id": "asset-12345678-abcd-efgh-ijkl-mnopqrstuvwx",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "EntityId": "location-continent-guid",
  "EntityType": "Location",
  "Type": "image",
  "Purpose": "map",
  "FileName": "arcanis_map_v2.jpg",
  "ContentType": "image/jpeg",
  "Size": 5242880,
  "BlobPath": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f/location/location-continent-guid/map-asset-12345678.jpg",
  "Url": "https://librismaleficarum.blob.core.windows.net/world-assets/b8e8e7e2.../location/.../map-asset-12345678.jpg",
  "ThumbnailBlobPath": "thumbnails/asset-12345678-thumb.jpg",
  "ThumbnailUrl": "https://librismaleficarum.blob.core.windows.net/world-assets/thumbnails/asset-12345678-thumb.jpg",
  "Dimensions": { "Width": 4096, "Height": 3072 },
  "UploadedBy": "user-abc",
  "UploadedDate": "2024-06-15T14:30:00Z",
  "Tags": ["map", "continent", "official"],
  "Description": "High-resolution map for player reference",
  "Status": "ready",
  "IsDeleted": false
}
```

### Example: WorldEntity Documents Referencing Assets

```json
// Location entity - references map asset by ID
{
  "id": "location-continent-guid",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "EntityType": "Continent",
  "SchemaId": "fantasy-location",
  "Name": "Arcanis",
  "OwnerId": "user-abc",
  "Depth": 1,
  "CreatedDate": "2024-06-01T10:00:00Z",
  "ModifiedDate": "2024-06-15T14:30:00Z",
  "IsDeleted": false,
  "Description": "A vast landmass of ancient forests and towering mountains...",
  "Tags": ["continent", "major-region"],
  "Path": ["Eldoria", "Arcanis"],
  
  // Asset reference - just the ID
  "MapAssetId": "asset-12345678-abcd-efgh-ijkl-mnopqrstuvwx",
  
  "Properties": {
    "Population": 45000000,
    "Climate": "Temperate",
    "Terrain": "Mixed (forests, mountains, plains)"
  }
}

// Campaign entity - references multiple assets by ID
{
  "id": "campaign-f2a3b4c5-d6e7-8901-2345-67890abcdef0",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "EntityType": "Campaign",
  "SchemaId": "dnd5e-campaign",
  "Name": "The Shadow Rising",
  "OwnerId": "user-abc",
  "Depth": 1,
  "CreatedDate": "2024-05-01T12:00:00Z",
  "ModifiedDate": "2024-06-25T20:00:00Z",
  "IsDeleted": false,
  "Description": "An epic campaign where heroes must prevent an ancient evil from awakening",
  "Tags": ["active", "epic", "multi-arc"],
  "Path": ["Eldoria", "The Shadow Rising"],
  
  // Asset references - just the IDs (assets stored in separate Asset container)
  "BannerAssetId": "banner-98765432-zyxw-vuts-rqpo-nmlkjihgfedcba",
  "IconAssetId": "icon-11111111-2222-3333-4444-555555555555",
  
  "Properties": {
    "Status": "active",
    "StartDate": "2024-05-15T19:00:00Z",
    "SessionCount": 8,
    "DM": "user-abc"
  }
}

// Character entity - references portrait and token assets by ID
{
  "id": "char-c9d8e7f6-5a4b-3c2d-1e0f-9a8b7c6d5e4f",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "country-valoria-guid",
  "EntityType": "Character",
  "SchemaId": "dnd5e-character",
  "Name": "Elara Silverwind",
  "OwnerId": "user-abc",
  "Depth": 4,
  "CreatedDate": "2024-06-10T15:30:00Z",
  "ModifiedDate": "2024-06-20T18:45:00Z",
  "IsDeleted": false,
  "Description": "A skilled elven ranger and diplomat from Valoria",
  "Tags": ["npc", "ranger", "elf", "quest-giver"],
  "Path": ["Eldoria", "Arcanis", "Valoria", "Silverwood", "Elara Silverwind"],
  
  // Asset references - just the IDs
  "PortraitAssetId": "portrait-aaaabbbb-cccc-dddd-eeee-ffffgggghhh",
  "TokenAssetId": "token-bbbbcccc-dddd-eeee-ffff-gggghhhhhiii",
  
  "Properties": {
    "Race": "Elf",
    "Class": "Ranger",
    "Level": 7
  },
  "SystemProperties": {
    "HP": 58,
    "AC": 15
  }
}
```

**Note**: To get entity with full asset details, application performs two queries:

1. Query WorldEntity by ID (1 RU)
1. Query Asset container by EntityId to get all assets (2-5 RUs)

Total cost: 3-6 RUs (vs 5-15 RUs if assets were embedded and entity had many assets)

### Asset Security & Access Patterns

**Shared Access Signatures (SAS)**:

- URLs in Cosmos DB contain **placeholder tokens** (e.g., `{SAS_TOKEN}`)
- Backend generates time-limited SAS tokens on-demand when serving assets to frontend
- Tokens scoped to specific blob with read-only permissions
- Typical expiry: 1-4 hours for general access, 24 hours for downloads

### Asset Processing Pipeline

**Upload Flow**:

1. Frontend uploads to temporary staging container
1. Azure Function triggered by blob creation:
   - Validates file type, size, content
   - Scans for malware (Microsoft Defender for Storage)
   - Generates thumbnails for images
   - Extracts metadata (dimensions, duration, etc.)
   - Moves to permanent location in world-assets container
   - Updates WorldEntity document via Cosmos DB SDK
1. Frontend polls entity document for `status: 'ready'`

**Change Feed Processor**:

- Monitors WorldEntity changes
- Deletes orphaned blobs when entity deleted
- Updates Azure AI Search index with asset metadata for cross-world asset search
- Triggers vector embedding for image descriptions

### Query Patterns

```sql
-- Get all assets for a campaign (Cosmos DB SQL API)
SELECT VALUE c.Assets 
FROM c 
WHERE c.WorldId = @worldId 
  AND c.id = @campaignId
  AND c.IsDeleted = false

-- Find all entities with video assets
SELECT c.id, c.Name, c.EntityType, c.Assets
FROM c
WHERE c.WorldId = @worldId
  AND ARRAY_CONTAINS(c.Assets, {"Type": "video"}, true)
  AND c.IsDeleted = false

-- Count assets by type across a world
SELECT 
  asset.Type,
  COUNT(1) as count,
  SUM(asset.Size) as totalSize
FROM c
JOIN asset IN c.Assets
WHERE c.WorldId = @worldId
  AND c.IsDeleted = false
GROUP BY asset.Type
```

```csharp
// Example: Query using Cosmos SDK (simplified)
var query = container.GetItemLinqQueryable<BaseWorldEntity>()
    .Where(e => e.WorldId == worldId && e.id == campaignId && !e.IsDeleted)
    .Select(e => e.Assets);

var assets = await query.ToListAsync();
```

### Storage Optimization

**Lifecycle Management**:

- Soft-deleted entities: Move blobs to cool tier after 30 days
- Hard-deleted entities: Purge blobs after 90 days
- Unused thumbnails: Delete after 180 days
- Large videos: Archive tier for recordings >1 year old

**Cost Optimization**:

- Store thumbnails in hot tier (frequent access)
- Campaign assets in cool tier after campaign completion
- Historical recordings in archive tier
- Use blob versioning for important assets (maps, official art)

## DeletedWorldEntity Container

**Separate Deleted Storage**: Soft-deleted WorldEntities are moved to a dedicated `DeletedWorldEntity` container after a retention grace period (e.g., 30 days). This design:

- **Reduces Query Overhead**: Active queries don't filter through deleted items (no `WHERE IsDeleted = false` needed)
- **Optimizes Active Container**: WorldEntity container remains lean and fast
- **Enables Different Retention**: Apply different TTL/lifecycle policies to deleted items
- **Simplifies Recovery**: All deleted items in one place for admin restore operations
- **Reduces Costs**: Deleted items can use lower throughput provisioning

**DeletedWorldEntity Container Schema**:

```csharp
public record DeletedWorldEntity : BaseWorldEntity
{
    // Inherits all fields from BaseWorldEntity
    // Additional deletion metadata:
    public required string DeletionReason { get; init; }      // "user_deleted", "cascade_delete", "purge"
    public required DateTime HardDeleteDate { get; init; }    // When to permanently purge (TTL)
    public string? OriginalWorldId { get; init; }             // For cross-world restore scenarios
}
```

**Partition Key**: `[/WorldId, /id]` - maintains same hierarchical partition structure as WorldEntity for efficient world-scoped queries and hot partition prevention.

**Lifecycle Flow**:

1. **Soft Delete** (instant): Set `IsDeleted = true` on WorldEntity, set `DeletedDate` and `DeletedBy`
1. **Grace Period** (e.g., 30 days): Entity remains in WorldEntity container for quick undo
1. **Move to Deleted Container** (Change Feed trigger after grace period):
   - Copy to DeletedWorldEntity container
   - Remove from WorldEntity container
   - Set TTL for permanent deletion (e.g., +90 days)
1. **Hard Delete** (automatic via TTL): Cosmos DB auto-purges after TTL expires

**Query Patterns**:

```sql
-- Get all deleted entities for a world (for admin restore)
SELECT * FROM d
WHERE d.WorldId = @worldId
ORDER BY d.DeletedDate DESC
-- Cost: ~2-5 RUs (single partition query)

-- Get recently deleted entities (undo candidates)
SELECT * FROM c
WHERE c.WorldId = @worldId
  AND c.IsDeleted = true
  AND c.DeletedDate > @cutoffDate
-- Cost: ~2-5 RUs (in WorldEntity container during grace period)

-- Restore deleted entity
-- 1. Copy from DeletedWorldEntity back to WorldEntity
-- 2. Set IsDeleted = false, clear DeletedDate/DeletedBy
-- 3. Remove from DeletedWorldEntity
```

**Cost Benefits**:

- **Active queries**: No filtering deleted items (no extra RU cost)
- **Deletion operations**: Async move via Change Feed (doesn't block user operations)
- **Lower provisioned RU**: DeletedWorldEntity container can run at minimal throughput (100 RU/s)
- **Automatic cleanup**: TTL handles hard deletes (no manual purge jobs)

**Change Feed Processor Logic**:

```csharp
// Pseudo-code for Change Feed processor
foreach (var change in feed.Changes)
{
    if (change.IsDeleted == true && 
        change.DeletedDate < DateTime.UtcNow.AddDays(-30))
    {
        // Grace period expired - move to deleted container
        var deletedEntity = new DeletedWorldEntity
        {
            // Copy all fields from change
            DeletionReason = "retention_expired",
            HardDeleteDate = DateTime.UtcNow.AddDays(90) // TTL for final purge
        };
        
        await deletedContainer.CreateItemAsync(deletedEntity, 
            new PartitionKey(deletedEntity.WorldId));
        
        await worldEntityContainer.DeleteItemAsync<WorldEntity>(
            change.id, new PartitionKey(change.WorldId));
    }
}
```

## WorldEntity.EntityType Hierarchy

The `EntityType` property defines the type of each WorldEntity. The following is a suggested hierarchy for TTRPG world/campaign/setting/adventure/scenario management:

- **World** (root)
  - **Locations**
    - Continent
    - Country
    - Region
    - City
    - Settlement
    - Landmark
    - Dungeon
    - Building
    - Room
    - Map
  - **Geographies**
    - Mountain
    - River
    - Lake
    - Forest
    - Desert
    - Ocean
    - Island
    - ClimateZone
  - **People**
    - Character
      - PlayerCharacter
      - NonPlayerCharacter
    - Organization
    - Faction
    - Family
    - Race
    - Culture
  - **Events**
    - HistoricalEvent
    - CurrentEvent
    - Quest
    - Encounter
    - Battle
    - Festival
    - Disaster
  - **History**
    - Timeline
    - Era
    - Chronicle
    - Legend
    - Myth
  - **Lore**
    - Religion
    - Deity
    - MagicSystem
    - Artifact
    - Technology
    - Language
    - Law
    - Custom
    - Story
  - **Bestiary**
    - Creature
    - Monster
    - Animal
  - **Items**
    - Equipment
    - Weapon
    - Armor
    - Treasure
    - Consumable
  - **Adventures**
    - Campaign
    - Scenario
    - Session
    - Scene
    - PlotHook
    - Secret
  - **Other**
    - Note
    - Image
    - Audio
    - Video
    - Document
    - Homebrew

This hierarchy is extensible and can be expanded as needed for different TTRPG systems and campaign needs.

**Hierarchy Validation**: A rules engine (or AI-powered validation via Microsoft Agent Framework) validates parent-child relationships. For example:

- ✅ Valid: Monster child of Continent (monsters live in continents)
- ❌ Invalid: Continent child of Monster (continents cannot be inside monsters)
- Rules defined in PropertySchema or separate validation configuration
- Validation performed at application layer (compile-time for common rules, runtime for system-specific rules)
