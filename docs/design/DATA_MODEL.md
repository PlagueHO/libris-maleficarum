# Data Model & Cosmos DB Design

All TTRPG-related entities are stored in Azure Cosmos DB using a flexible, hierarchical model.

## World Entity Container

- **WorldEntity**: The core document type for all campaign/world data. Each "WorldEntity" can represent a "World" (root), or any nested entity such as "Continent", "Country", "Region", "City", "Character", etc. Entities can be arbitrarily nested to support complex world-building.
- **Ownership**: Every WorldEntity includes an `OwnerId` property (e.g., a user ID) to indicate the owner.
- **Document IDs**: All WorldEntities use a GUID for the `id` property to ensure uniqueness.
- **Hierarchical Partition Key**: The container uses a **3-level hierarchical partition key** (`[/WorldId, /ParentId, /id]`) to:
  - **Level 1** (`/WorldId`): Tenant isolation - ensures all entities for a world are co-located on same physical partitions
  - **Level 2** (`/ParentId`): Hierarchy locality - enables efficient parent-child queries without cross-partition scans
  - **Level 3** (`/id`): Uniqueness guarantee - ensures no logical partition exceeds 20 GB limit, allowing infinite world scaling
- **Query Optimization**: This partition strategy enables:
  - Point reads: 1 RU (when all 3 keys specified)
  - Children queries: ~2.5 RUs (WorldId + ParentId prefix)
  - World tree queries: ~2.5 RUs × minimal partitions (WorldId prefix)
  - Avoids hot partitions during write-heavy sessions
- **Denormalization**: Each entity includes a `Children` array containing child entity IDs for efficient tree traversal without additional queries
- **Vector Indexing**: Entities include a `DescriptionEmbedding` vector field (1536 dimensions for OpenAI embeddings) with DiskANN index for semantic search within world scope
- **External Indexing**: All WorldEntities are also indexed by Azure AI Search for advanced semantic ranking, hybrid search, and cross-world discovery

## Base WorldEntity Schema

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
    public List<string>? Children { get; init; }       // Array of child entity IDs (denormalized)
    public List<string>? Path { get; init; }           // Breadcrumb path from root
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
    
    // === Vector Search ===
    public float[]? DescriptionEmbedding { get; init; } // 1536-dim vector for semantic search
    
    // === Asset Management ===
    public List<AssetReference>? Assets { get; init; } // All assets attached to this entity
    public int? AssetCount { get; init; }              // Denormalized count
    
    // Asset ID references (entity-type specific)
    public string? PortraitAssetId { get; init; }      // Character portraits
    public string? TokenAssetId { get; init; }         // VTT tokens
    public string? BannerAssetId { get; init; }        // Campaign/world banners
    public string? IconAssetId { get; init; }          // List icons
    public string? MapAssetId { get; init; }           // Location maps
    public string? ThumbnailAssetId { get; init; }     // Custom thumbnails
    
    // === Entity Properties (Flexible Schema) ===
    public object? Properties { get; init; }           // Common properties (cross-system)
    public object? SystemProperties { get; init; }     // System-specific properties (D&D 5e, Pathfinder, etc.)
}

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

### Core Property Constraints

**Required Fields** (must be present in every WorldEntity):

- `id` - GUID uniqueness
- `WorldId` - Tenant isolation
- `ParentId` - Hierarchy (null only for root World)
- `EntityType` - Type discrimination
- `Name` - Human-readable identifier
- `OwnerId` - Security/permissions
- `CreatedDate` - Audit trail
- `ModifiedDate` - Change tracking
- `Depth` - Hierarchy level
- `IsDeleted` - Soft delete support (default: false)

**Optional Fields** (commonly used but not required):

- `Description`, `Tags` - Search/discovery
- `Children`, `Path` - Denormalized hierarchy (updated by Change Feed)
- `DescriptionEmbedding` - Semantic search (generated async)
- `Assets`, `AssetCount` - Asset management
- Asset references (`PortraitAssetId`, etc.) - Entity-specific
- `SchemaId` - References which property schema/template to use
- `Properties` - Common properties shared across schemas (flexible object)
- `SystemProperties` - System-specific properties (D&D 5e, Pathfinder, etc.)

**Audit Fields** (populated by backend):

- `CreatedBy`, `ModifiedBy` - User tracking
- `DeletedDate`, `DeletedBy` - Soft delete metadata

### Property Schema System

The property schema system enables flexible entity properties that can vary by game system (D&D 5e, Pathfinder, etc.) or world type (fantasy, sci-fi, modern) without requiring code changes. This hybrid approach balances type safety for common properties with flexibility for system-specific needs.

**Key Concepts**:

- **SchemaId**: Optional reference to a `PropertySchema` document that defines expected properties
- **Properties**: Common properties shared across most schemas (e.g., `Name`, `Level`, `Class`, `Population`)
- **SystemProperties**: System-specific properties that vary by schema (e.g., D&D 5e stats, Pathfinder 2e proficiencies)

**Benefits**:

- **Flexibility**: Support multiple TTRPG systems (D&D 5e, Pathfinder 2e, custom homebrew) without code deployment
- **Type Safety**: Common properties remain strongly-typed in the base schema
- **Validation**: Optional runtime validation via PropertySchema definitions
- **UI Generation**: Frontend can dynamically render forms based on PropertySchema metadata
- **AI Integration**: Microsoft Agent Framework can use schemas to validate and suggest properties

**Schema Storage**:

PropertySchema documents are stored in a dedicated Cosmos DB container with partition key `/EntityType`. Each schema defines expected properties, data types, validation rules, and UI metadata.

**Example Schema Usage**:

```csharp
// D&D 5e Character Schema
var dnd5eCharacterSchema = new PropertySchema
{
    id = "dnd5e-character",
    Name = "D&D 5e Character",
    EntityType = "Character",
    Description = "Properties for D&D 5th Edition characters",
    CommonProperties = new List<PropertyDefinition>
    {
        new() { Name = "Race", DataType = "string", Required = true, DisplayName = "Race/Ancestry" },
        new() { Name = "Class", DataType = "string", Required = true },
        new() { Name = "Level", DataType = "int", Required = true, ValidationRule = "min:1,max:20" },
        new() { Name = "Alignment", DataType = "string", ValidationRule = "enum:LG,NG,CG,LN,N,CN,LE,NE,CE" },
        new() { Name = "Background", DataType = "string" }
    },
    SystemProperties = new List<PropertyDefinition>
    {
        new() { Name = "Stats", DataType = "object", Required = true, Description = "STR, DEX, CON, INT, WIS, CHA" },
        new() { Name = "HP", DataType = "int", Required = true, DisplayName = "Hit Points" },
        new() { Name = "AC", DataType = "int", Required = true, DisplayName = "Armor Class" },
        new() { Name = "Speed", DataType = "int", DefaultValue = 30 },
        new() { Name = "SpellSlots", DataType = "object", Description = "Available spell slots by level" }
    }
};

// Pathfinder 2e Character Schema (different system properties)
var pf2eCharacterSchema = new PropertySchema
{
    id = "pathfinder2e-character",
    Name = "Pathfinder 2e Character",
    EntityType = "Character",
    CommonProperties = new List<PropertyDefinition>
    {
        new() { Name = "Ancestry", DataType = "string", Required = true },
        new() { Name = "Class", DataType = "string", Required = true },
        new() { Name = "Level", DataType = "int", Required = true, ValidationRule = "min:1,max:20" },
        new() { Name = "Background", DataType = "string" }
    },
    SystemProperties = new List<PropertyDefinition>
    {
        new() { Name = "AbilityScores", DataType = "object", Required = true },
        new() { Name = "HP", DataType = "int", Required = true },
        new() { Name = "AC", DataType = "int", Required = true },
        new() { Name = "Proficiencies", DataType = "object", Description = "Trained/Expert/Master/Legendary rankings" },
        new() { Name = "HeroPoints", DataType = "int", DefaultValue = 1 }
    }
};
```

### Example: WorldEntity Documents

```json
// WorldEntity document (World - root entity)
// Demonstrates: All required base properties
{
  // === Core Identity (REQUIRED) ===
  "id": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",  // Same as id for root
  "ParentId": null,                                     // null for root World entity
  "EntityType": "World",
  "SchemaId": "fantasy-world",
  "Name": "Eldoria",
  "OwnerId": "user-abc",
  "Depth": 0,
  
  // === Temporal (REQUIRED) ===
  "CreatedDate": "2024-06-01T10:00:00Z",
  "ModifiedDate": "2024-06-01T10:00:00Z",
  "IsDeleted": false,
  
  // === Optional Base Properties ===
  "Description": "A high-fantasy realm of magic and mystery where ancient powers stir...",
  "Tags": ["fantasy", "high-magic", "campaign-world"],
  "CreatedBy": "user-abc",
  "ModifiedBy": "user-abc",
  
  // === Hierarchy (Denormalized) ===
  "Children": [
    "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
    "i6d7e8f9-0123-4567-8901-23456def0123"
  ],
  "Path": ["Eldoria"],
  
  // === Vector Search ===
  "DescriptionEmbedding": [0.034, -0.012, 0.089, ...],
  
  // === Assets ===
  "BannerAssetId": "world-banner-guid",
  "IconAssetId": "world-icon-guid",
  "Assets": [/* world banner, icon */],
  "AssetCount": 2,
  
  // === World-Specific Properties ===
  "Properties": {
    "Theme": "High Fantasy",
    "Era": "Medieval"
  },
  "SystemProperties": {
    "MagicLevel": "High",
    "TechnologyLevel": "Medieval",
    "CreationSystem": "homebrew"
  }
}

// WorldEntity document (Continent - LocationEntity)
// Demonstrates: Location-specific properties
{
  // === Core Identity (REQUIRED) ===
  "id": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",  // Parent is World
  "EntityType": "Continent",
  "SchemaId": "fantasy-location",
  "Name": "Arcanis",
  "OwnerId": "user-abc",
  "Depth": 1,
  
  // === Temporal (REQUIRED) ===
  "CreatedDate": "2024-06-01T10:00:00Z",
  "ModifiedDate": "2024-06-01T10:00:00Z",
  "IsDeleted": false,
  
  // === Optional Base Properties ===
  "Description": "A vast landmass of ancient forests and towering mountains...",
  "Tags": ["continent", "major-region"],
  "CreatedBy": "user-abc",
  "ModifiedBy": "user-abc",
  
  // === Hierarchy (Denormalized) ===
  "Children": [
    "c9d8e7f6-5a4b-3c2d-1e0f-9a8b7c6d5e4f",
    "d1e2f3a4-b5c6-7890-1234-567890abcdef"
  ],
  "Path": ["Eldoria", "Arcanis"],
  
  // === Vector Search ===
  "DescriptionEmbedding": [0.045, -0.023, 0.011, ...],
  
  // === Assets ===
  "MapAssetId": "continent-map-guid",
  "Assets": [/* continent map */],
  "AssetCount": 1,
  
  // === Location Properties (Hybrid Pattern) ===
  // Properties: Common geographic data
  "Properties": {
    "Population": 45000000,
    "Climate": "Temperate",
    "Terrain": "Mixed (forests, mountains, plains)"
  },
  // SystemProperties: Schema-specific (fantasy-location)
  "SystemProperties": {
    "Resources": ["timber", "iron", "precious gems"],
    "MagicalAnomalies": ["Ley line convergence in eastern mountains"]
  }
}

// WorldEntity document (Country - LocationEntity)
// Demonstrates: Nested location with detailed properties
{
  // === Core Identity (REQUIRED) ===
  "id": "c9d8e7f6-5a4b-3c2d-1e0f-9a8b7c6d5e4f",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",  // Parent is Continent
  "EntityType": "Country",
  "SchemaId": "fantasy-location",
  "Name": "Valoria",
  "OwnerId": "user-abc",
  "Depth": 2,
  
  // === Temporal (REQUIRED) ===
  "CreatedDate": "2024-06-01T10:00:00Z",
  "ModifiedDate": "2024-06-15T14:22:00Z",  // Updated after creation
  "IsDeleted": false,
  
  // === Optional Base Properties ===
  "Description": "A proud maritime nation known for its skilled navigators and democratic traditions...",
  "Tags": ["kingdom", "maritime", "democratic"],
  "CreatedBy": "user-abc",
  "ModifiedBy": "user-xyz",  // Different user updated
  
  // === Hierarchy (Denormalized) ===
  "Children": [
    "e1f2a3b4-c5d6-7890-1234-567890abcdef",  // Capital city
    "f2a3b4c5-d6e7-8901-2345-67890abcdef0"   // Major port
  ],
  "Path": ["Eldoria", "Arcanis", "Valoria"],
  
  // === Vector Search ===
  "DescriptionEmbedding": [0.012, -0.034, 0.056, ...],
  
  // === Assets ===
  "MapAssetId": "country-map-guid",
  "IconAssetId": "country-flag-guid",
  "Assets": [/* map, flag, emblem */],
  "AssetCount": 3,
  
  // === Location Properties (Hybrid Pattern) ===
  "Properties": {
    "Population": 2500000,
    "Government": "Constitutional Monarchy",
    "Climate": "Temperate Maritime",
    "Capital": "e1f2a3b4-c5d6-7890-1234-567890abcdef"  // Link to capital city entity
  },
  "SystemProperties": {
    "Languages": ["Common", "Valorian"],
    "Currency": "Valorian Crown",
    "Coordinates": {
      "latitude": 45.5231,
      "longitude": -122.6765
    }
  }
}

// WorldEntity document (Character - CharacterEntity)
// Demonstrates: Character with portrait, token, and rich properties
{
  // === Core Identity (REQUIRED) ===
  "id": "f3a4b5c6-d7e8-9012-3456-789012abcdef",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "e1f2a3b4-c5d6-7890-1234-567890abcdef",  // Parent is a City
  "EntityType": "Character",
  "SchemaId": "dnd5e-character",
  "Name": "Elara Windwhisper",
  "OwnerId": "user-abc",
  "Depth": 4,
  
  // === Temporal (REQUIRED) ===
  "CreatedDate": "2024-06-10T15:30:00Z",
  "ModifiedDate": "2024-06-20T18:45:00Z",
  "IsDeleted": false,
  
  // === Optional Base Properties ===
  "Description": "A mysterious elven ranger who serves as the forest's guardian...",
  "Tags": ["npc", "ranger", "elf", "quest-giver"],
  "CreatedBy": "user-abc",
  "ModifiedBy": "user-abc",
  
  // === Hierarchy (Denormalized) ===
  "Children": [],  // Characters typically have no children
  "Path": ["Eldoria", "Arcanis", "Valoria", "Silverwood", "Elara Windwhisper"],
  
  // === Vector Search ===
  "DescriptionEmbedding": [0.023, -0.045, 0.078, ...],
  
  // === Assets ===
  "PortraitAssetId": "elara-portrait-guid",
  "TokenAssetId": "elara-token-guid",
  "Assets": [/* portrait for character sheet, token for battle map */],
  "AssetCount": 2,
  
  // === Character Properties (Hybrid Pattern) ===
  // Properties: Common across most systems
  "Properties": {
    "Race": "Elf",
    "Class": "Ranger",
    "Level": 8,
    "Alignment": "Neutral Good",
    "Background": "Outlander"
  },
  // SystemProperties: D&D 5e specific
  "SystemProperties": {
    "Stats": {
      "STR": 12,
      "DEX": 18,
      "CON": 14,
      "INT": 13,
      "WIS": 16,
      "CHA": 10
    },
    "HP": 64,
    "AC": 16,
    "Speed": 35,
    "Relationships": [
      {"EntityId": "g4b5c6d7-e8f9-0123-4567-890123bcdef0", "Type": "ally", "Description": "Mentor"},
      {"EntityId": "h5c6d7e8-f901-2345-6789-01234cdef012", "Type": "rival", "Description": "Former apprentice"}
    ]
  }
}

// WorldEntity document (Campaign - CampaignEntity)
// Demonstrates: Campaign with banner, session management
{
  // === Core Identity (REQUIRED) ===
  "id": "i6d7e8f9-0123-4567-8901-23456def0123",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",  // Parent is the World
  "EntityType": "Campaign",
  "SchemaId": "dnd5e-campaign",
  "Name": "The Shattered Crown",
  "OwnerId": "user-abc",
  "Depth": 1,
  
  // === Temporal (REQUIRED) ===
  "CreatedDate": "2024-05-01T12:00:00Z",
  "ModifiedDate": "2024-06-25T20:00:00Z",
  "IsDeleted": false,
  
  // === Optional Base Properties ===
  "Description": "A high-stakes adventure where heroes must reunite the fragments of an ancient crown to prevent catastrophe...",
  "Tags": ["active", "epic", "multi-arc"],
  "CreatedBy": "user-abc",
  "ModifiedBy": "user-abc",
  
  // === Hierarchy (Denormalized) ===
  "Children": [
    "j7e8f901-2345-6789-0123-4567ef012345",  // Session 1
    "k8f90123-4567-8901-2345-6789f0123456"   // Session 2
  ],
  "Path": ["Eldoria", "The Shattered Crown"],
  
  // === Vector Search ===
  "DescriptionEmbedding": [0.067, -0.012, 0.034, ...],
  
  // === Assets ===
  "BannerAssetId": "campaign-banner-guid",
  "IconAssetId": "campaign-icon-guid",
  "ThumbnailAssetId": "campaign-thumb-guid",
  "Assets": [/* promotional banner, campaign icon, thumbnail for lists */],
  "AssetCount": 3,
  
  // === Campaign Properties (Hybrid Pattern) ===
  // Properties: Common across most systems
  "Properties": {
    "Status": "active",
    "StartDate": "2024-05-15T19:00:00Z",
    "SessionCount": 8,
    "DM": "user-abc",
    "Schedule": "Fridays 7PM EST",
    "NextSession": "2024-06-28T19:00:00Z"
  },
  // SystemProperties: D&D 5e specific
  "SystemProperties": {
    "Players": [
      {"UserId": "player-xyz", "Character": "f3a4b5c6-d7e8-9012-3456-789012abcdef"},
      {"UserId": "player-def", "Character": "l9012345-6789-0123-4567-89012345678"}
    ],
    "Arcs": [
      {
        "Name": "The Lost Fragments",
        "Status": "completed",
        "Sessions": [1, 2, 3]
      },
      {
        "Name": "Rise of the Shadow Court",
        "Status": "in-progress",
        "Sessions": [4, 5, 6, 7, 8]
      }
    ]
  }
}
```

### Container Structure

**Single Container Design**: The WorldEntity container uses hierarchical partition keys to efficiently store both entities and their relationships. This eliminates the need for a separate hierarchy container, reducing:

- RU costs (no duplicate queries across containers)
- Data synchronization complexity
- Storage costs (no denormalized relationship documents)

**Hierarchy Management**: Parent-child relationships are managed through:

1. **ParentId field**: Each entity references its parent's `id`
1. **Children array**: Denormalized list of child IDs for fast tree traversal
1. **Path array**: Breadcrumb from root for validation and UI display
1. **Depth field**: Hierarchy level for query optimization

**Hierarchical Operations**:

```sql
-- Get all children of an entity (e.g., expand tree node)
SELECT * FROM c 
WHERE c.WorldId = @worldId 
  AND c.ParentId = @parentId
  AND c.IsDeleted = false
-- Cost: ~2.5 RUs (single partition query)

-- Get entire world tree (e.g., load side panel)
SELECT * FROM c 
WHERE c.WorldId = @worldId 
  AND c.IsDeleted = false
ORDER BY c.Depth, c.Name
-- Cost: ~2.5 RUs × (partitions containing world data)

-- Move entity to new parent (transactional batch)
-- 1. Update entity's ParentId, Path, Depth
-- 2. Remove from old parent's Children array
-- 3. Add to new parent's Children array
-- Cost: ~3-6 RUs (within same world partition)
```

**Change Feed Integration**: The Change Feed processor automatically:

- Updates parent's `Children` array when child created/deleted
- Syncs to Azure AI Search index for cross-world search
- Triggers vector embedding generation for new/updated descriptions

**Key Design Points**:

- **Hierarchical Partition Key**: `[WorldId, ParentId, id]` enables efficient queries and infinite scaling
- **ParentId**: `null` for root World entities, otherwise references the parent entity's `id`
- **Children**: Denormalized array of child IDs for efficient tree navigation (updated on child create/delete)
- **Path**: Denormalized breadcrumb array for UI display and hierarchy validation
- **Depth**: Hierarchy level for query optimization and UI rendering
- **DescriptionEmbedding**: Vector for semantic search within world scope (Cosmos DB DiskANN index)
- **Properties**: Type-specific fields stored as flexible object (different EntityTypes have different properties)
- **IsDeleted**: Soft delete flag for undo/recovery functionality

## Asset Management & Azure Blob Storage Integration

WorldEntity documents reference external assets (images, audio, video, documents) stored in Azure Blob Storage. This separation follows Azure best practices:

- **Cosmos DB**: Stores metadata, references, and searchable content (optimized for queries, <2KB per asset reference)
- **Blob Storage**: Stores actual binary assets (optimized for large files, streaming, CDN integration)

### Asset Reference Structure

Each WorldEntity can contain multiple assets organized by type and purpose:

```csharp
// Asset metadata stored in WorldEntity documents
public record AssetReference
{
    public required string id { get; init; }              // Unique asset ID (GUID) - lowercase per Cosmos DB convention
    public required string Type { get; init; }            // "image", "audio", "video", "document", "map", "model3d"
    public string? Purpose { get; init; }                 // "portrait", "banner", "icon", "background", "thumbnail"
    public required string Url { get; init; }             // Full blob URL (with SAS token placeholder)
    public required string BlobPath { get; init; }        // Relative path: {worldId}/{entityType}/{entityId}/{assetId}.ext
    public required string FileName { get; init; }        // Original filename
    public required string ContentType { get; init; }     // MIME type: "image/png", "audio/mp3"
    public required long Size { get; init; }              // File size in bytes
    
    // Image-specific metadata
    public ImageDimensions? Dimensions { get; init; }     // Width and height for images
    public string? ThumbnailUrl { get; init; }            // Optional thumbnail for large images
    
    // Audio/Video metadata
    public double? Duration { get; init; }                // Duration in seconds
    public string? Codec { get; init; }                   // Audio/video codec info
    
    // Document metadata
    public int? PageCount { get; init; }                  // For PDFs, etc.
    public string? Format { get; init; }                  // "pdf", "docx", "markdown"
    
    // Metadata
    public required string UploadedBy { get; init; }      // User ID who uploaded
    public required DateTime UploadedDate { get; init; }  // ISO 8601 timestamp
    public List<string>? Tags { get; init; }              // Searchable tags
    public string? Description { get; init; }             // Alt text, caption, or description
    
    // Processing status
    public string? Status { get; init; }                  // "pending", "processing", "ready", "failed"
    public string? ProcessingError { get; init; }         // Error message if processing failed
}

public record ImageDimensions(int Width, int Height);
```

**Design Benefits of Asset ID References**:

- **Flexibility**: Different entity types can feature different assets (characters show portraits, campaigns show banners)
- **No Duplication**: Asset metadata stored once in `assets` array, referenced by ID
- **Multiple Featured Assets**: Characters can have both `portraitAssetId` and `tokenAssetId`, campaigns can have `bannerAssetId` and `iconAssetId`
- **Type Safety**: Frontend knows exactly which asset to display for each context (portrait for character sheet, token for battle map)
- **Easy Validation**: Backend validates that referenced asset IDs exist in the `assets` array
- **Extensibility**: Add new reference fields (e.g., `backgroundAssetId`, `avatarAssetId`) without schema migration

**Usage Pattern**:

```csharp
// Get character portrait asset
var character = await GetEntityAsync<CharacterEntity>(characterId);
var portrait = character.Assets?.FirstOrDefault(a => a.id == character.PortraitAssetId);
if (portrait != null)
{
    var portraitUrl = GenerateSasUrl(portrait.BlobPath);
}

// Set featured asset when uploading
var updatedAssets = existingAssets.Append(newAsset).ToList();
await UpdateEntityAsync(entityId, new
{
    Assets = updatedAssets,
    PortraitAssetId = newAsset.id  // Feature this asset as portrait
});
```

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

### Example: WorldEntity Documents with Assets

```json
// Image entity - primarily a reference to an asset
{
  "id": "img-e1f2a3b4-c5d6-7890-1234-567890abcdef",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "location-continent-guid",
  "EntityType": "Image",
  "SchemaId": "generic-asset",
  "Name": "Map of Arcanis Continent",
  "Description": "Detailed cartographic map showing major cities, roads, and terrain",
  "MapAssetId": "asset-12345678-abcd-efgh-ijkl-mnopqrstuvwx",  // Featured map asset
  "Assets": [
    {
      "id": "asset-12345678-abcd-efgh-ijkl-mnopqrstuvwx",
      "Type": "image",
      "Purpose": "map",
      "Url": "https://librismaleficarum.blob.core.windows.net/world-assets/b8e8e7e2.../location/.../map-asset-12345678.jpg",
      "BlobPath": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f/location/location-continent-guid/map-asset-12345678.jpg",
      "FileName": "arcanis_map_v2.jpg",
      "ContentType": "image/jpeg",
      "Size": 5242880,
      "Dimensions": { "Width": 4096, "Height": 3072 },
      "ThumbnailUrl": "https://librismaleficarum.blob.core.windows.net/world-assets/thumbnails/asset-12345678-thumb.jpg",
      "UploadedBy": "user-abc",
      "UploadedDate": "2024-06-15T14:30:00Z",
      "Tags": ["map", "continent", "official"],
      "Description": "High-resolution map for player reference",
      "Status": "ready"
    }
  ],
  "AssetCount": 1,
  "Path": ["Eldoria", "Arcanis", "Maps", "Continent Map"],
  "Depth": 3
}

// Campaign entity - multiple asset types
{
  "id": "campaign-f2a3b4c5-d6e7-8901-2345-67890abcdef0",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "EntityType": "Campaign",
  "SchemaId": "dnd5e-campaign",
  "Name": "The Shadow Rising",
  "Description": "An epic campaign where heroes must prevent an ancient evil from awakening",
  "BannerAssetId": "banner-98765432-zyxw-vuts-rqpo-nmlkjihgfedcba",  // Featured banner
  "IconAssetId": "icon-11111111-2222-3333-4444-555555555555",      // Campaign icon for lists
  "Assets": [
    {
      "id": "banner-98765432-zyxw-vuts-rqpo-nmlkjihgfedcba",
      "Type": "image",
      "Purpose": "banner",
      "Url": "https://librismaleficarum.blob.core.windows.net/world-assets/.../banner-98765432.jpg",
      "BlobPath": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f/campaign/campaign-f2a3b4c5/banner-98765432.jpg",
      "FileName": "shadow_rising_banner.jpg",
      "ContentType": "image/jpeg",
      "Size": 1048576,
      "Dimensions": { "Width": 1920, "Height": 600 },
      "UploadedBy": "user-abc",
      "UploadedDate": "2024-06-01T10:00:00Z",
      "Status": "ready"
    },
    {
      "id": "session01-11223344-aabb-ccdd-eeff-001122334455",
      "Type": "audio",
      "Purpose": "attachment",
      "Url": "https://librismaleficarum.blob.core.windows.net/world-assets/.../session01-11223344.mp3",
      "BlobPath": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f/campaign/campaign-f2a3b4c5/session01-11223344.mp3",
      "FileName": "session_01_recording.mp3",
      "ContentType": "audio/mpeg",
      "Size": 52428800,
      "Duration": 7200,
      "Codec": "mp3",
      "UploadedBy": "user-abc",
      "UploadedDate": "2024-06-08T22:15:00Z",
      "Tags": ["session-recording", "session-1"],
      "Description": "Recording of first campaign session",
      "Status": "ready"
    },
    {
      "id": "notes-55667788-9900-aabb-ccdd-eeff00112233",
      "Type": "document",
      "Purpose": "attachment",
      "Url": "https://librismaleficarum.blob.core.windows.net/world-assets/.../notes-55667788.pdf",
      "BlobPath": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f/campaign/campaign-f2a3b4c5/notes-55667788.pdf",
      "FileName": "dm_notes_session_1-5.pdf",
      "ContentType": "application/pdf",
      "Size": 2097152,
      "PageCount": 15,
      "Format": "pdf",
      "UploadedBy": "user-abc",
      "UploadedDate": "2024-06-20T09:45:00Z",
      "Tags": ["dm-notes", "sessions-1-5"],
      "Description": "DM notes for first 5 sessions",
      "Status": "ready"
    },
    {
      "id": "battlemap-99887766-5544-3322-1100-ffeeddccbbaa",
      "Type": "image",
      "Purpose": "attachment",
      "Url": "https://librismaleficarum.blob.core.windows.net/world-assets/.../battlemap-99887766.png",
      "BlobPath": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f/campaign/campaign-f2a3b4c5/battlemap-99887766.png",
      "FileName": "temple_final_battle.png",
      "ContentType": "image/png",
      "Size": 8388608,
      "Dimensions": { "Width": 7000, "Height": 5000 },
      "ThumbnailUrl": "https://librismaleficarum.blob.core.windows.net/world-assets/thumbnails/battlemap-99887766-thumb.jpg",
      "UploadedBy": "user-abc",
      "UploadedDate": "2024-07-15T16:00:00Z",
      "Tags": ["battlemap", "temple", "boss-fight"],
      "Description": "High-res battle map for temple climax encounter",
      "Status": "ready"
    },
    {
      "id": "icon-11111111-2222-3333-4444-555555555555",
      "Type": "image",
      "Purpose": "icon",
      "Url": "https://librismaleficarum.blob.core.windows.net/world-assets/.../icon-11111111.png",
      "BlobPath": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f/campaign/campaign-f2a3b4c5/icon-11111111.png",
      "FileName": "shadow_rising_icon.png",
      "ContentType": "image/png",
      "Size": 51200,
      "Dimensions": { "Width": 256, "Height": 256 },
      "UploadedBy": "user-abc",
      "UploadedDate": "2024-06-01T10:05:00Z",
      "Tags": ["icon", "branding"],
      "Description": "Campaign icon for navigation lists",
      "Status": "ready"
    }
  ],
  "AssetCount": 5,
  "Path": ["Eldoria", "The Shadow Rising"],
  "Depth": 1
}

// Character entity - portrait and token
{
  "id": "char-c9d8e7f6-5a4b-3c2d-1e0f-9a8b7c6d5e4f",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "country-valoria-guid",
  "EntityType": "Character",
  "Name": "Elara Silverwind",
  "Description": "A skilled elven ranger and diplomat from Valoria",
  "PortraitAssetId": "portrait-aaaabbbb-cccc-dddd-eeee-ffffgggghhh",  // Featured portrait
  "TokenAssetId": "token-bbbbcccc-dddd-eeee-ffff-gggghhhhhiii",      // VTT token
  "Assets": [
    {
      "id": "portrait-aaaabbbb-cccc-dddd-eeee-ffffgggghhh",
      "Type": "image",
      "Purpose": "portrait",
      "Url": "https://librismaleficarum.blob.core.windows.net/world-assets/.../portrait-aaaabbbb.png",
      "BlobPath": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f/character/char-c9d8e7f6/portrait-aaaabbbb.png",
      "FileName": "elara_portrait.png",
      "ContentType": "image/png",
      "Size": 2097152,
      "Dimensions": { "Width": 1024, "Height": 1024 },
      "ThumbnailUrl": "https://librismaleficarum.blob.core.windows.net/world-assets/thumbnails/portrait-aaaabbbb-thumb.jpg",
      "UploadedBy": "user-abc",
      "UploadedDate": "2024-06-10T11:20:00Z",
      "Tags": ["character-art", "elven", "official"],
      "Description": "Character portrait by commissioned artist",
      "Status": "ready"
    },
    {
      "id": "token-bbbbcccc-dddd-eeee-ffff-gggghhhhhiii",
      "Type": "image",
      "Purpose": "token",
      "Url": "https://librismaleficarum.blob.core.windows.net/world-assets/.../token-bbbbcccc.png",
      "BlobPath": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f/character/char-c9d8e7f6/token-bbbbcccc.png",
      "FileName": "elara_token.png",
      "ContentType": "image/png",
      "Size": 262144,
      "Dimensions": { "Width": 256, "Height": 256 },
      "UploadedBy": "user-abc",
      "UploadedDate": "2024-06-10T11:25:00Z",
      "Tags": ["token", "vtt"],
      "Description": "Character token for virtual tabletop",
      "Status": "ready"
    }
  ],
  "AssetCount": 2,
  "Properties": {
    "Race": "Elf",
    "Class": "Ranger",
    "Level": 7
  },
  "SystemProperties": {
    "HP": 58,
    "AC": 15
  },
  "Path": ["Eldoria", "Arcanis", "Valoria", "NPCs", "Elara Silverwind"],
  "Depth": 4
}
```

### Asset Security & Access Patterns

**Shared Access Signatures (SAS)**:

- URLs in Cosmos DB contain **placeholder tokens** (e.g., `{SAS_TOKEN}`)
- Backend generates time-limited SAS tokens on-demand when serving assets to frontend
- Tokens scoped to specific blob with read-only permissions
- Typical expiry: 1-4 hours for general access, 24 hours for downloads

**Access Control**:

```csharp
// Check permissions before generating SAS
if (await CanUserAccessWorldAsync(userId, worldId))
{
    var sasToken = GenerateSasToken(new SasTokenOptions
    {
        BlobPath = asset.BlobPath,
        Permissions = BlobSasPermissions.Read,
        ExpiryMinutes = 240  // 4 hours
    });
    
    return asset.Url.Replace("{SAS_TOKEN}", sasToken);
}
```

**CDN Integration**:

- Azure CDN in front of Blob Storage for frequently accessed assets
- Cache thumbnails, character portraits, world banners
- Edge caching reduces latency and blob storage egress costs

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

## World Entity Hierarchy Container

A dedicated **World Entity Hierarchy container** is used to efficiently store and manage the relationships between WorldEntities (e.g., parent-child relationships). This container enables:

- Fast traversal of the world structure (e.g., finding all children of a given entity, or the full path to a root).
- Efficient queries for hierarchical operations (e.g., moving, copying, or deleting subtrees).
- **Partition Key**: The partition key for the hierarchy container is `/WorldId-ParentId` (a synthetic/composite key combining `WorldId` and `ParentId`). This ensures efficient queries for all children of a given parent within a world, which is the most common access pattern.

### Example: WorldEntityHierarchy Document

```json
{
  "id": "f1e2d3c4-b5a6-7890-1234-56789abcdef0",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "ChildIds": [],
  "Name": "Valoria",
  "EntityType": "Country",
  "PartitionKey": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f:a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d"
}
```

- Each document in the hierarchy container represents a relationship (e.g., parent-child) between two WorldEntities.
- The `PartitionKey` is a composite of `WorldId` and `ParentId` for efficient access to all children of a parent within a world.

### WorldEntity.EntityType and WorldEntityHierarchy.EntityType Properties

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

There will need to be some form of rules engine or validation to ensure that WorldEntity heirarchy is valid, e.g. a "Continent" cannot be a child of a "Monster", but a "Monster" can be a child of a "Continent". We could use AI validation for this, or a simple rules engine.
