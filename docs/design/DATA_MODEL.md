# Data Model and Persistence Strategy

The purpose of this document is to describe the structure and strategy for persisting TTRPG-related world entities, assets and hierarchies. It covers the main entity types, their properties, relationships, and indexing strategies to support efficient querying, scalability, and flexibility for various TTRPG systems.

This document is intended for backend developers, database architects, and system designers. An API specification document will complement this by detailing RESTful endpoints.

> [!NOTE]
> This document does not contain implementation code but provides detailed data models and design rationale to guide development. It focuses on Cosmos DB schema design, partitioning strategies, and common query patterns. It will contain sample SQL statements to illustrate querying approaches and estimate RU costs for common operations.

## Overview of Data Model

The core data model revolves around the concept of a "World" or "Campaign", which serves as a container for all related entities such as locations, characters, items, and sessions. Each World is owned by a user and can contain an arbitrary hierarchy of nested entities. A user can own multiple Worlds, but will only interact with one World at a time in the application.

## Cosmos DB Containers

Three core containers store all TTRPG data, each with optimized partition keys for their access patterns:

| Container | Purpose | Partition Key | Key Characteristics |
| --------- | ------- | ------------- | ------------------- |
| **World** | World/campaign metadata | `/Id` | Top-level world documents; efficient user-owned world queries; isolated from entity hierarchy |
| **WorldEntity** | World entity hierarchy | `[/WorldId, /id]` | All entities within worlds (locations, characters, campaigns, etc.) with hierarchical relationships via `ParentId`; soft-deleted entities remain in-place with TTL for automatic purge |
| **Asset** | Asset metadata (images, audio, video, documents) | `[/WorldId, /EntityId]` | References to Azure Blob Storage; separate from entities to prevent document bloat; partitioned by entity for efficient entity-scoped queries |

> [!IMPORTANT]
> Partition key design for each container is critical to ensure RU usage is kept as low as possible. Hot partitions must also be avoided. Hierarchical partition keys (e.g., `[/WorldId, /id]`, `[/WorldId, /EntityId]`) enable efficient queries while distributing load across partitions.

## Common Operations

This section outlines common operations and their estimated RU costs for each container. It is used to guide design decisions and optimize for cost efficiency, performance and to reduce RU usage and limit hot partitions.

| Operation | Container | Parameters | RU Cost | Notes |
| --------- | --------- | ---------- | ------- | ----- |
| **List user's worlds** | World | `ownerId` | 5-15 RUs | Cross-partition query (use sparingly); cache results |
| **Get world by ID** | World | `worldId` | 1 RU | Point read with partition key |
| **Create world** | World | `world` | 5-8 RUs | Includes indexing overhead |
| **Update world** | World | `worldId`, `updates` | 6-12 RUs | Read (1 RU) + write (5-8 RUs) |
| **Soft delete world** | World | `worldId` | 6-10 RUs | Mark `IsDeleted = true`, set TTL |
| **Get entity by ID** | WorldEntity | `worldId`, `entityId` | 1 RU | Point read with both partition keys; excludes deleted |
| **Get entity children** | WorldEntity | `worldId`, `parentId` | 2-6 RUs | Filtered partition prefix query with `IsDeleted` filter; used for lazy-loading tree nodes |
| **List all world entities** | WorldEntity | `worldId` | 6-18 RUs | Partition prefix query with `IsDeleted` filter; ~10-20% overhead from deleted items |
| **List entities by type** | WorldEntity | `worldId`, `entityType` | 3-10 RUs | Filtered partition prefix query with `IsDeleted` filter |
| **Search entities** | WorldEntity | `worldId`, `searchTerm` | 5-14 RUs | Name/tag search with `IsDeleted` filter |
| **Count children** | WorldEntity | `worldId`, `parentId` | 2-4 RUs | Aggregate query with `IsDeleted` filter |
| **Get entities by owner** | WorldEntity | `ownerId` | 10-60 RUs | Cross-partition (use sparingly); includes deleted filter overhead |
| **Create entity** | WorldEntity | `entity` | 5-8 RUs | Includes indexing overhead |
| **Update entity** | WorldEntity | `worldId`, `entityId`, `updates` | 6-12 RUs | Read (1 RU) + write (5-8 RUs) |
| **Soft delete entity** | WorldEntity | `worldId`, `entityId` | 6-10 RUs | Mark `IsDeleted = true`, set TTL (90 days) |
| **Move entity** | WorldEntity | `worldId`, `entityId`, `newParentId` | 6-10 RUs | Update `ParentId`, `Path`, `Depth` |
| **Get entity assets** | Asset | `worldId`, `entityId` | 2-5 RUs | Filtered partition prefix query |
| **Create asset** | Asset | `asset` | 5-7 RUs | Asset metadata only |
| **Delete asset** | Asset | `worldId`, `assetId` | 6-8 RUs | Soft delete + blob delete |
| **Auto-purge deleted** | WorldEntity | N/A | 0 RUs | Cosmos DB TTL handles cleanup automatically (free) |

## Container Details

## World Container

World documents store top-level world/campaign metadata with partition key `/Id` for efficient owner-based queries.

- **World**: Root-level world/campaign documents containing core metadata, settings, and ownership information
- **Ownership**: Every World includes an `OwnerId` property to indicate the owner (user ID)
- **Document IDs**: All Worlds use a GUID for the `id` property to ensure uniqueness
- **Partition Key**: The container uses a **simple partition key** (`/Id`) to:
  - **Efficient Point Reads**: 1 RU when querying by world ID
  - **Owner Queries**: Cross-partition query for user's worlds (5-15 RUs, cache results)
  - **Isolation**: World metadata separated from entity hierarchy for cleaner operations
- **Query Optimization**: This partition strategy enables:
  - Point reads: 1 RU (Id specified)
  - Owner queries: 5-15 RUs (filter by OwnerId across all partitions)
  - Simple CRUD operations without hierarchical complexity
- **Design Rationale**: Separating World from WorldEntity provides:
  - **Clear Separation**: World metadata vs. world content (entities)
  - **Simpler Queries**: "List user's worlds" doesn't require filtering WorldEntity by `ParentId == null`
  - **Isolated Operations**: World-level operations don't impact entity queries
  - **Better Consistency**: Domain rules enforce World existence before creating entities

### World Schema

```csharp
public record World
{
    // === Core Identity (REQUIRED) ===
    public required string Id { get; init; }              // GUID - unique world identifier
    public required string OwnerId { get; init; }         // User ID who owns this world
    
    // === Metadata (REQUIRED) ===
    public required string Name { get; init; }            // Display name (max 100 chars)
    public string? Description { get; init; }             // Rich text description (max 2000 chars)
    
    // === Temporal (REQUIRED) ===
    public required DateTime CreatedDate { get; init; }   // ISO 8601 timestamp
    public required DateTime ModifiedDate { get; init; }  // ISO 8601 timestamp
    
    // === Soft Delete (REQUIRED) ===
    public required bool IsDeleted { get; init; }         // Soft delete flag (default: false)
    public DateTime? DeletedDate { get; init; }           // ISO 8601 timestamp when deleted
    public string? DeletedBy { get; init; }               // User ID who deleted
    public int? Ttl { get; init; }                        // Time-to-live in seconds (Cosmos DB reserved field)
                                                          // null = omit property (use container default, requires DefaultTimeToLive set)
                                                          // -1 = never expire (override container TTL)
                                                          // >0 = expire N seconds after last modified (_ts)
                                                          // Set to 7776000 (90 days) on soft delete for automatic purge
}
```

### World Query Patterns

```sql
-- Get user's worlds (cross-partition query, cache results)
SELECT * FROM w
WHERE w.OwnerId = @ownerId
  AND w.IsDeleted = false
ORDER BY w.ModifiedDate DESC
-- Cost: ~5-15 RUs (cross-partition)

-- Get world by ID (point read)
SELECT * FROM w
WHERE w.id = @worldId
-- Cost: 1 RU (point read with partition key)

-- Create world
INSERT INTO w VALUE {
  "id": @worldId,
  "OwnerId": @ownerId,
  "Name": @name,
  "Description": @description,
  "CreatedDate": @now,
  "ModifiedDate": @now,
  "IsDeleted": false
}
-- Cost: ~5-8 RUs
```

## WorldEntity Container

WorldEntity documents store all entities within worlds with hierarchical partition key `[/WorldId, /id]` for hot partition prevention.

- **WorldEntity**: Entities within a world such as "Continent", "Country", "Region", "City", "Character", "Campaign", "Session", etc. Entities can be arbitrarily nested to support complex world-building.
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
    
    // === Schema Versioning (REQUIRED) ===
    public required int SchemaVersion { get; init; }   // Entity schema version (1-based integer, default: 1)
                                                       // Enables lazy migration: entities are upgraded to current version on save
    
    // === Soft Delete (REQUIRED) ===
    public required bool IsDeleted { get; init; }      // Soft delete flag (default: false)
    public DateTime? DeletedDate { get; init; }        // ISO 8601 timestamp when deleted
    public string? DeletedBy { get; init; }            // User ID who deleted
    public int? Ttl { get; init; }                     // Time-to-live in seconds (Cosmos DB reserved field)
                                                       // null = omit property (use container default, requires DefaultTimeToLive set)
                                                       // -1 = never expire (override container TTL)
                                                       // >0 = expire N seconds after last modified (_ts)
                                                       // Set to 7776000 (90 days) on soft delete for automatic purge
    
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
// SchemaId references which property template/schema to use for property value validation and UI generation

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
    // EntityType: "Continent", "GeographicRegion", "PoliticalRegion", "Country", "City", "Dungeon", etc.
    // SchemaId: "fantasy-location", "scifi-location", "modern-location", etc.
    
    // Properties (common):
    // - Population, Climate, Terrain, Government, etc.
    
    // SystemProperties (fantasy example):
    // - MagicLevel: "High"
    // - Resources: ["mithril", "ancient artifacts"]
    // - Coordinates: { Latitude: 45.5, Longitude: -122.6 }
}

// Example: GeographicRegion entity for organizational grouping
public record GeographicRegionEntity : BaseWorldEntity
{
    // EntityType: "GeographicRegion"
    // SchemaId: "geographic-region"
    
    // Properties (common):
    // - Area: 2500000 (km²)
    // - Climate: "Temperate"
    // - Terrain: "Mixed"
    // - Population: 195000000 (aggregate)
    
    // SystemProperties (optional):
    // - DefiningFeatures: ["Atlantic coastline", "Major river systems"]
    // - EconomicZone: "Industrial"
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

### Schema Evolution Guidelines

The `SchemaVersion` field enables lazy migration of entity schemas without requiring database-wide migrations. Entities are upgraded to the current schema version when saved, allowing gradual migration of the entire world as users interact with their data.

#### Version Strategy

- **Version Numbers**: 1-based integers (initial version: 1)
- **Current Version**: Defined per entity type in `EntitySchemaVersionConfig` (backend) and `ENTITY_SCHEMA_VERSIONS` map (frontend)
- **Lazy Migration**: Entities retain their schema version until saved; on save, they upgrade to the current version
- **Validation Rules**:
  - `schemaVersion` must be a positive integer
  - `schemaVersion` cannot be 0 (reserved for testing/invalid states)
  - `schemaVersion` cannot exceed the current version for that entity type
  - `schemaVersion` cannot be downgraded (prevents data loss from removed fields)

#### Backend Implementation

**Domain Layer** (`WorldEntity.cs`):

```csharp
public int SchemaVersion { get; private set; }

public void UpdateSchemaVersion(int schemaVersion)
{
    var currentVersion = EntitySchemaVersionConfig.GetSchemaVersion(EntityType);
    SchemaVersionValidator.Validate(schemaVersion, currentVersion);
    SchemaVersion = schemaVersion;
}
```

**Infrastructure Layer** (`WorldEntityConfiguration.cs`):

```csharp
// EF Core value converter ensures backward compatibility with existing documents
builder.Property(e => e.SchemaVersion)
    .HasConversion(
        v => v,                    // To DB: store as-is
        v => v == 0 ? 1 : v)       // From DB: convert 0 to 1 for pre-versioning documents
    .IsRequired();
```

**Validation** (`SchemaVersionValidator.cs`):

- Throws `SchemaVersionException` for validation errors
- Error codes: `SCHEMA_VERSION_INVALID`, `SCHEMA_VERSION_TOO_LOW`, `SCHEMA_VERSION_TOO_HIGH`, `SCHEMA_VERSION_DOWNGRADE_NOT_ALLOWED`

#### Frontend Implementation

**Type Definitions** (`worldEntity.types.ts`):

```typescript
export interface BaseWorldEntity {
  id: string;
  worldId: string;
  schemaVersion: number;  // Required field for all entities
  // ... other fields
}
```

**API Client** (`worldEntityApi.ts`):

```typescript
import { getSchemaVersion } from './constants/entitySchemaVersions';

// Auto-inject current schema version on create
createWorldEntity: builder.mutation({
  query: (data) => ({
    method: 'POST',
    body: {
      ...data,
      schemaVersion: data.schemaVersion ?? getSchemaVersion(data.entityType),
    },
  }),
}),
```

**Component Layer** (`EntityDetailForm.tsx`):

```typescript
const handleSubmit = async (data: FormData) => {
  const payload = {
    ...data,
    schemaVersion: getSchemaVersion(entityType as WorldEntityType),
  };
  await createWorldEntity(payload);
};
```

#### Version Increment Strategy

When to increment schema versions:

1. **Breaking Changes** (REQUIRED):
   - Field renames (e.g., `level` → `characterLevel`)
   - Field type changes (e.g., `hitPoints: number` → `hitPoints: { current: number, max: number }`)
   - Required field additions (e.g., new `alignment` required field)
   - Field removals (e.g., deprecated `legacyField` removed)

1. **Non-Breaking Changes** (OPTIONAL):
   - Optional field additions (e.g., new `proficiencyBonus` optional field)
   - Value constraint changes (e.g., `level` max increased from 20 to 30)
   - Validation rule changes (e.g., new spell slot calculation logic)

1. **Migration Implementation**:
   - Add migration logic to handle conversion from old version to new version
   - Test migration with representative data samples
   - Update unit tests to cover both old and new schema versions
   - Document migration steps in CHANGELOG.md

#### Backward Compatibility

- **Pre-Versioning Documents**: Documents without `SchemaVersion` field (created before versioning feature) default to version 1 via EF Core converter
- **Version 0 Handling**: Reserved for invalid/test states; EF Core converts 0 to 1 when reading from database
- **Client-Server Sync**: Frontend always sends current version; backend validates and upgrades on save

### Property Schema Templates

Property schemas/templates are stored in a separate container or configuration store. Each schema defines the expected properties, data types, value validation rules, and default values for a given system/entity type.

> [!NOTE]
> The `ValidationRule` field validates **property values** (e.g., ensuring Level is 1-20), not parent-child entity relationships. Entity hierarchy has no validation—see Hierarchy Recommendation Patterns section.

```csharp
// Property Schema/Template Document (stored separately in Cosmos DB)
public record PropertySchema
{
    public required string id { get; init; }           // "dnd5e-character", "geographic-region"
    public required string Name { get; init; }         // "D&D 5e Character", "Geographic Region"
    public required string EntityType { get; init; }   // "Character", "GeographicRegion"
    public string? Description { get; init; }          // "Properties for D&D 5th Edition characters"
    public List<PropertyDefinition>? CommonProperties { get; init; }  // Common property definitions
    public List<PropertyDefinition>? SystemProperties { get; init; }  // System-specific definitions
    public List<string>? RecommendedChildTypes { get; init; }  // UI hint: recommended child EntityTypes (shown first in selector)
    public DateTime CreatedDate { get; init; }
    public DateTime ModifiedDate { get; init; }
}

public record PropertyDefinition
{
    public required string Name { get; init; }         // "Level", "ArmorClass", "Area", etc.
    public required string DataType { get; init; }     // "string", "int", "object", "array"
    public bool Required { get; init; }                // Is this property required?
    public object? DefaultValue { get; init; }         // Default value if not specified
    public string? ValidationRule { get; init; }       // "min:1,max:20", "enum:Good,Neutral,Evil"
    public string? DisplayName { get; init; }          // "Armor Class" (for UI)
    public string? Description { get; init; }          // Help text for UI
}

// Example: PropertySchema for GeographicRegion
{
    "id": "geographic-region",
    "Name": "Geographic Region",
    "EntityType": "GeographicRegion",
    "Description": "Organizational grouping for geographic areas",
    "CommonProperties": [
        { "Name": "Area", "DataType": "number", "DisplayName": "Area (km²)", "Description": "Total area in square kilometers" },
        { "Name": "Climate", "DataType": "string", "DisplayName": "Climate", "ValidationRule": "enum:Tropical,Arid,Temperate,Continental,Polar" },
        { "Name": "Terrain", "DataType": "string", "DisplayName": "Terrain", "ValidationRule": "enum:Mountains,Plains,Forest,Desert,Mixed" },
        { "Name": "Population", "DataType": "number", "DisplayName": "Population (aggregate)" }
    ],
    "SystemProperties": [],
    "RecommendedChildTypes": ["Country", "Province", "Region", "GeographicRegion"],
    "CreatedDate": "2024-01-01T00:00:00Z",
    "ModifiedDate": "2024-01-01T00:00:00Z"
}
```

## Hierarchy Navigation Strategy

The application uses **on-demand lazy loading** to build the entity hierarchy tree. This approach queries children as tree nodes are expanded, providing scalability and fresh data without entity count limits.

### Query Pattern

```csharp
// Get children of a specific entity
[HttpGet("worlds/{worldId}/entities/{parentId}/children")]
public async Task<IEnumerable<EntityNode>> GetChildren(
    string worldId, 
    string parentId)
{
    var query = container.GetItemLinqQueryable<BaseWorldEntity>()
        .Where(e => e.WorldId == worldId && e.ParentId == parentId && !e.IsDeleted)
        .OrderBy(e => e.Name)
        .Select(e => new EntityNode 
        { 
            Id = e.id, 
            Name = e.Name, 
            EntityType = e.EntityType,
            IconAssetId = e.IconAssetId 
        });
    
    return await query.ToListAsync();
}
// Cost: 2-5 RUs per node expansion
```

### Client-Side Implementation

**Tree Loading Flow**:

1. Initial load: Query root-level entities (`ParentId = WorldId`)
1. User expands node: Query children of that entity (`ParentId = expandedEntityId`)
1. Cache results client-side with 5-minute TTL
1. Refresh on demand or via real-time notifications

**Benefits**:

- ✅ **Scalable**: No entity count limits per world
- ✅ **Fresh data**: Always queries latest state
- ✅ **Simple architecture**: No Change Feed processor needed
- ✅ **Memory efficient**: Only expanded branches loaded
- ✅ **Multi-user friendly**: Real-time updates via SignalR

**Performance**:

- Root load: 2-5 RUs (typically 5-20 root entities)
- Node expansion: 2-5 RUs per expanded node
- Typical session (expand 5-10 nodes): 15-30 RUs

### Real-Time Synchronization

```typescript
// SignalR notification when entities change
signalR.on('EntityChanged', (change) => {
  if (change.ChangeType === 'created' || change.ChangeType === 'deleted') {
    // Invalidate parent's children cache
    hierarchyCache.invalidate(change.ParentId);
    
    // Auto-refresh if node is currently expanded
    if (isNodeExpanded(change.ParentId)) {
      loadChildren(change.ParentId, { forceRefresh: true });
    }
  }
});
```

### Future Optimization: WorldMetadata Container

For worlds with very frequent full-tree loads and minimal updates, a **WorldMetadata** container could be added as an optimization:

- Single document per world containing pre-materialized hierarchy tree
- 1 RU initial load vs 15-30 RUs for on-demand loading
- Trade-offs: 5000 entity limit, eventual consistency, write amplification, added complexity
- **Recommendation**: Only implement if profiling shows hierarchy queries are a bottleneck

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

### Asset Container Schema

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
    public int? Ttl { get; init; }                        // Time-to-live in seconds (Cosmos DB reserved field)
                                                          // null = omit property (use container default, requires DefaultTimeToLive set)
                                                          // -1 = never expire (override container TTL)
                                                          // >0 = expire N seconds after last modified (_ts)
                                                          // Set to 7776000 (90 days) on soft delete for automatic purge
}

public record ImageDimensions(int Width, int Height);
```

**Partition Key**: `[/WorldId, /EntityId]` - assets partitioned by entity for efficient entity-scoped queries and hot partition prevention.

### Query Patterns

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
// Continent entity - parent of GeographicRegions
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

// GeographicRegion entity - organizational grouping within continent
{
  "id": "region-western-arcanis-guid",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "location-continent-guid",
  "EntityType": "GeographicRegion",
  "SchemaId": "geographic-region",
  "Name": "Western Arcanis",
  "OwnerId": "user-abc",
  "Depth": 2,
  "CreatedDate": "2024-06-01T10:30:00Z",
  "ModifiedDate": "2024-06-15T14:30:00Z",
  "IsDeleted": false,
  "Description": "The western territories of Arcanis, known for coastal trade cities",
  "Tags": ["geographic-region", "coastal", "trade-hub"],
  "Path": ["Eldoria", "Arcanis", "Western Arcanis"],
  "Properties": {
    "Area": 850000,
    "Climate": "Temperate",
    "Terrain": "Coastal",
    "Population": 12000000
  }
}

// Country entity - child of GeographicRegion, with cross-cutting Tags
{
  "id": "country-valoria-guid",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "region-western-arcanis-guid",
  "EntityType": "Country",
  "SchemaId": "fantasy-location",
  "Name": "Valoria",
  "OwnerId": "user-abc",
  "Depth": 3,
  "CreatedDate": "2024-06-01T11:00:00Z",
  "ModifiedDate": "2024-06-15T14:30:00Z",
  "IsDeleted": false,
  "Description": "A maritime nation famous for its shipbuilding and naval prowess",
  "Tags": ["country", "naval-power", "trade-alliance", "magic-friendly"],
  "Path": ["Eldoria", "Arcanis", "Western Arcanis", "Valoria"],
  
  "Properties": {
    "Population": 8000000,
    "Government": "Constitutional Monarchy",
    "Climate": "Mediterranean",
    "Terrain": "Coastal plains and hills"
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
  "Path": ["Eldoria", "Arcanis", "Western Arcanis", "Valoria", "Elara Silverwind"],
  
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

Total cost: 3-6 RUs

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
   - Creates Asset document in Cosmos DB
1. Frontend polls Asset document for `status: 'ready'`

**Change Feed Processor**:

- Monitors WorldEntity changes
- Deletes orphaned blobs when entity deleted
- Updates Azure AI Search index with asset metadata for cross-world asset search
- Triggers vector embedding for image descriptions

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

## Soft Delete Strategy: In-Place with TTL-Based Automatic Cleanup

**In-Place Soft Delete**: Deleted WorldEntities remain in the `WorldEntity` container with `IsDeleted = true` until automatically purged by Cosmos DB's TTL feature. This simplified approach:

- **Simple Implementation**: No Change Feed processor or secondary container needed
- **Automatic Cleanup**: Cosmos DB TTL handles permanent deletion (zero RU cost)
- **Single Source of Truth**: All entities (active and deleted) in one container
- **Restore Capability**: Deleted entities can be restored within retention period
- **Trade-off**: Active queries include deleted item filtering (~10-20% RU overhead)

**Soft Delete Lifecycle**:

1. **Soft Delete** (instant): Set `IsDeleted = true`, `DeletedDate`, `DeletedBy`, and `Ttl = 7776000` (90 days)
   - TTL countdown starts from the item's `_ts` (last modified timestamp)
   - Item becomes eligible for deletion 90 days after soft delete operation

1. **Retention Period** (90 days): Entity queryable with `IsDeleted = true` filter for restore operations
   - Restore before TTL expires: Set `Ttl = null` to remove expiration (property omitted from JSON)
   - Item remains in container until background purge cycle runs after TTL expiration

1. **Automatic Purge** (after TTL expires): Cosmos DB background process deletes document
   - Zero RU cost for deletion (handled by Cosmos DB internally)
   - Deletion may not be instantaneous; occurs during next background purge cycle
   - No manual cleanup or scheduled jobs required

**TTL Configuration**:

```csharp
// WorldEntity.cs - TTL property for automatic cleanup
public int? Ttl { get; private set; }  // Time-to-live in seconds
                                        // null = property omitted (uses container default)
                                        // -1 = never expire (override container TTL)
                                        // >0 = expire after N seconds from last modified time

public void SoftDelete(string deletedBy)
{
    IsDeleted = true;
    DeletedDate = DateTime.UtcNow;
    DeletedBy = deletedBy;
    Ttl = 7776000; // 90 days in seconds (90 * 24 * 60 * 60)
    ModifiedDate = DateTime.UtcNow;
}

public void Restore()
{
    IsDeleted = false;
    DeletedDate = null;
    DeletedBy = null;
    Ttl = null; // Omit TTL property (don't serialize to JSON)
    ModifiedDate = DateTime.UtcNow;
}

// WorldEntityConfiguration.cs - EF Core Cosmos DB provider mapping
builder.Property(e => e.Ttl)
    .ToJsonProperty("ttl")  // Cosmos DB reserved TTL field name (lowercase)
    .IsRequired(false);     // Omit property from JSON when null

// IMPORTANT: Container-level TTL configuration required
// The WorldEntity container must have DefaultTimeToLive enabled (set to -1)
// This allows per-item TTL to work. Without container TTL enabled, item TTL is ignored.
// 
// Azure Portal > Container Settings > Time to Live: On (no default)
// OR via SDK:
// ContainerProperties containerProperties = new ContainerProperties
// {
//     Id = "WorldEntity",
//     PartitionKeyPath = "/WorldId",
//     DefaultTimeToLive = -1  // Enable TTL, but no default expiration
// };
```

**Query Patterns**:

```sql
-- Get all entities (excludes deleted by default)
SELECT * FROM c
WHERE c.WorldId = @worldId
  AND c.IsDeleted = false
-- Cost: ~6-18 RUs (includes filter overhead from deleted items)

-- Get deleted entities for restore UI
SELECT * FROM c
WHERE c.WorldId = @worldId
  AND c.IsDeleted = true
  AND c.DeletedDate > @cutoffDate
ORDER BY c.DeletedDate DESC
-- Cost: ~3-8 RUs

-- Restore deleted entity (C# code, not SQL)
// In repository/service:
entity.IsDeleted = false;
entity.DeletedDate = null;
entity.DeletedBy = null;
entity.Ttl = null;  // Omit ttl from JSON (EF Core won't serialize null properties)
entity.ModifiedDate = DateTime.UtcNow;
await context.SaveChangesAsync();
// Cost: ~6-10 RUs (read + write)
```

**Performance Characteristics**:

| Scenario | In-Place TTL Approach | Notes |
|----------|----------------------|-------|
| **Query overhead** | +10-20% RU cost | All queries filter `IsDeleted = false` |
| **Delete operation** | 6-10 RUs | Mark deleted + set TTL |
| **Restore operation** | 6-10 RUs | Clear delete flags + remove TTL |
| **Automatic purge** | 0 RUs | Cosmos DB TTL is free |
| **Storage overhead** | Minimal | Deleted items purged after 90 days |
| **Implementation complexity** | Low | No Change Feed processor needed |

**Container-Level TTL Configuration**:

For item-level TTL to work, the container must have TTL enabled:

```csharp
// Infrastructure/Persistence/DbInitializer.cs - Container creation
var containerProperties = new ContainerProperties
{
    Id = "WorldEntity",
    PartitionKeyPath = "/WorldId",
    DefaultTimeToLive = -1  // Enable TTL; -1 = items don't expire by default
                            // Items with ttl property will override this
};

// Alternative: Set to positive number (e.g., 7776000) to make ALL items 
// expire after 90 days unless they have their own ttl value
```

**TTL Behavior Summary** (per Microsoft documentation):

| Container `DefaultTimeToLive` | Item `ttl` | Result |
|-------------------------------|-----------|--------|
| `null` (not set) | Any value | TTL disabled; items never expire |
| `-1` | Not present | Item never expires |
| `-1` | `-1` | Item never expires |
| `-1` | `7776000` | Item expires after 90 days |
| `7776000` | Not present | Item expires after 90 days (container default) |
| `7776000` | `-1` | Item never expires (override) |
| `7776000` | `3600` | Item expires after 1 hour (override) |

**Critical Implementation Notes**:

1. **Property omission**: When `Ttl` is `null` in C#, the `ttl` property must be **omitted** from the JSON document (not serialized as `"ttl": null`). The EF Core Cosmos provider handles this automatically for nullable properties.

1. **Container configuration**: The `DefaultTimeToLive` must be set to `-1` (or a positive number) on the container for item-level TTL to work. If not set, all item `ttl` values are ignored.

1. **TTL countdown**: Starts from the item's `_ts` (last modified timestamp), not from creation time if the item was updated.

1. **Value constraints**: Item TTL must be:
   - Omitted entirely (null in C#, property not in JSON), OR
   - `-1` (never expire), OR  
   - Positive integer ≤ `2147483647` seconds (~68 years max)

**When to Consider Two-Container Migration**:

The current in-place approach is suitable for initial release and typical usage patterns. Consider migrating to a separate `DeletedWorldEntity` container if:

- Average >500 soft-deleted entities per world (causes >20% query overhead)
- High query volume (>10,000 queries/day) where 10-20% overhead becomes significant
- Customer demand for longer retention periods (>6 months)
- Need for different access patterns on deleted vs active entities

**Future Optimization Path**:

If metrics show query overhead becoming a bottleneck, implement a Change Feed processor to:

1. Monitor WorldEntity changes
1. Move entities with `IsDeleted = true` and `DeletedDate > 30 days` to separate container
1. Remove from WorldEntity container
1. Apply different retention/TTL policies on deleted container

This migration can be implemented without API changes, as the delete/restore logic remains the same from the client perspective.

## WorldEntity.EntityType Hierarchy

The `EntityType` property defines the type of each WorldEntity. The hierarchy below provides **UI recommendations and defaults** for creating entities—it is purely informational and **does not enforce any restrictions**. Any EntityType can have any other EntityType as a parent.

**Purpose of Hierarchy:**

- **UI Defaults**: When creating a child entity, the UI shows recommended EntityTypes first in the selector/dropdown
- **AI Suggestions**: Microsoft Agent Framework uses recommendations to suggest likely entity placements
- **User Freedom**: All EntityTypes remain accessible via search or scrolling—users can choose any type regardless of recommendations

**Two Categories of EntityTypes:**

1. **Container EntityTypes** (e.g., `Locations`, `People`, `Events`, `Lore`, `Items`): Top-level organizational folders typically used as direct children of World
1. **Standard EntityTypes** (e.g., `Country`, `Character`, `Quest`, `GeographicRegion`, `PoliticalRegion`): All other entity types representing actual world content. Some Standard types (like GeographicRegion, PoliticalRegion) define custom properties stored in the `Properties` JSON field, but functionally they behave identically to other Standard types.

> [!IMPORTANT]
> There are **no validation rules** for parent-child relationships. The hierarchy below lists EntityTypes grouped by category for organizational purposes. Recommendations affect only UI presentation order—never restrictions.

### Hierarchy Structure

- **World** (root)
  - **Locations**
    - Continent
    - **GeographicRegion** (regional grouping by geography: "West Europe", "Balkans", "Scandinavia")
    - **PoliticalRegion** (regional grouping by politics: "European Union", "Commonwealth", "Trade Alliance")
    - **CulturalRegion** (regional grouping by culture: "Latin Lands", "Slavic Territories", "Nordic Realms")
    - **MilitaryRegion** (regional grouping by military zones: "Northern Defense Zone", "Border Territories")
    - Country
    - Province
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

### Container Entity Types

**Container EntityTypes** (e.g., `Locations`, `People`, `Events`, `Lore`, `Items`, `Adventures`) provide organizational structure as top-level folders directly under World. These containers help organize entities by domain area (locations, characters, items, etc.) and influence which EntityTypes appear first in UI dropdowns when creating children.

**Example:**

```text
World: Eldoria
├── Locations (recommends: Continent, Country, City, Dungeon)
│   ├── Continent: Arcanis
│   ├── City: Waterdeep
│   ├── Character: Town Guard              (user chose unrecommended type)
│   └── Dungeon: Undermountain
├── People (recommends: Character, Organization, Faction)
│   ├── Character: Elminster
│   ├── Organization: Harpers
│   └── City: Skullport                    (secret thieves' city)
└── Events (recommends: Quest, Battle, Festival)
    ├── Quest: Retrieve the Lost Artifact
    └── HistoricalEvent: Fall of Myth Drannor
```

### Organizational Entity Types

**Regional EntityTypes** (GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion) are Standard EntityTypes that support custom properties stored in the flexible `Properties` JSON field. They follow the same BaseWorldEntity schema as all other entity types (Country, Character, etc.), with their custom properties (Climate, Population, Government Type, etc.) defined via PropertySchema templates and stored as JSON.

**Key Differences from Containers:**

- **Properties**: Support domain properties (Climate, Population, Area)
- **Semantic Meaning**: Type name conveys organizational intent ("GeographicRegion" vs generic "Locations")
- **Nestable**: Can contain other organizational types (e.g., GeographicRegion within Continent)
- **Queryable**: Enable meaningful queries like "Show all GeographicRegions in Europe"

**Example:**

```text
World: Eldoria
├── Continent: Europe
│   ├── GeographicRegion: Western Europe
│   │   ├── Country: France
│   │   ├── Country: Belgium
│   │   └── Country: Netherlands
│   ├── GeographicRegion: Eastern Europe
│   │   ├── Country: Poland
│   │   └── Country: Czech Republic
│   └── PoliticalRegion: European Alliance
│       └── (cross-cutting: references countries via Tags)
```

**Cross-cutting organization**: Use `Tags` for entities belonging to multiple groupings (e.g., Country tagged with `["eu-member", "nato", "schengen"]`).

### Hierarchy Recommendation Patterns

Recommendations provide **UI defaults and AI suggestions only**—no enforcement. Any EntityType can be a child of any other EntityType.

**How It Works:**

1. UI queries parent's `RecommendedChildTypes` from PropertySchema when creating child entity
1. Recommended types appear first in the EntityType selector dropdown
1. All other types accessible via search or scrolling below recommended types
1. Microsoft Agent Framework uses recommendations for contextual suggestions
1. Users can freely choose any EntityType regardless of recommendation status

**Common Recommendation Patterns:**

| Parent EntityType | Recommended Children |
|------------------|---------------------|
| `Continent` | `GeographicRegion`, `PoliticalRegion`, `CulturalRegion`, `MilitaryRegion`, `Country`, `Region` |
| `GeographicRegion` | `Country`, `Province`, `Region`, `GeographicRegion` (nested) |
| `Country` | `Province`, `Region`, `City`, `Landmark`, `Character`, `Organization` |
| `City` | `Settlement`, `Building`, `Landmark`, `Character`, `Organization` |
| `Campaign` | `Quest`, `Session`, `Scene`, `PlotHook` |
| `Character` | `Equipment`, `Weapon`, `Armor` |

**Unusual But Supported Examples:**

- `Monster` → `Continent` (world-turtle carrying a continent)
- `Locations` → `Character` (flat organization preference)
- `Character` → `City` (city in a bag of holding)

**Technical Implementation:**

- Recommendations stored in PropertySchema `RecommendedChildTypes` field
- UI queries parent schema to order dropdown selector
- No validation—all combinations permitted
