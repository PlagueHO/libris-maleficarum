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

### Example: WorldEntity Documents

```json
// WorldEntity document (World - root entity)
{
  "id": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": null,  // Root entity has no parent
  "OwnerId": "user-abc",
  "Name": "Eldoria",
  "EntityType": "World",
  "Description": "A high-fantasy realm of magic and mystery...",
  "DescriptionEmbedding": [0.021, -0.015, 0.032, ...],  // 1536-dim vector
  "Children": [
    "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",  // Arcanis continent
    "f9e8d7c6-b5a4-3210-fedc-ba9876543210"   // Another continent
  ],
  "Path": ["Eldoria"],
  "Depth": 0,
  "CreatedDate": "2024-06-01T10:00:00Z",
  "ModifiedDate": "2024-06-01T10:00:00Z",
  "IsDeleted": false
}

// WorldEntity document (Continent, child of World)
{
  "id": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",  // Parent is World
  "OwnerId": "user-abc",
  "Name": "Arcanis",
  "EntityType": "Continent",
  "Description": "A vast landmass of ancient forests and towering mountains...",
  "DescriptionEmbedding": [0.045, -0.023, 0.011, ...],
  "Children": [
    "c9d8e7f6-5a4b-3c2d-1e0f-9a8b7c6d5e4f",  // Valoria country
    "d1e2f3a4-b5c6-7890-1234-567890abcdef"   // Another country
  ],
  "Path": ["Eldoria", "Arcanis"],
  "Depth": 1,
  "CreatedDate": "2024-06-01T10:00:00Z",
  "ModifiedDate": "2024-06-01T10:00:00Z",
  "IsDeleted": false
}

// WorldEntity document (Country, child of Continent)
{Container Structure

**Single Container Design**: The WorldEntity container uses hierarchical partition keys to efficiently store both entities and their relationships. This eliminates the need for a separate hierarchy container, reducing:
- RU costs (no duplicate queries across containers)
- Data synchronization complexity
- Storage costs (no denormalized relationship documents)

**Hierarchy Management**: Parent-child relationships are managed through:
1. **ParentId field**: Each entity references its parent's `id`
2. **Children array**: Denormalized list of child IDs for fast tree traversal
3. **Path array**: Breadcrumb from root for validation and UI display
4. **Depth field**: Hierarchy level for query optimization

**Hierarchical Operations**:
```typescript
// Get all children of an entity (e.g., expand tree node)
SELECT * FROM c 
WHERE c.WorldId = @worldId 
  AND c.ParentId = @parentId
  AND c.IsDeleted = false
// Cost: ~2.5 RUs (single partition query)

// Get entire world tree (e.g., load side panel)
SELECT * FROM c 
WHERE c.WorldId = @worldId 
  AND c.IsDeleted = false
ORDER BY c.Depth, c.Name
// Cost: ~2.5 RUs × (partitions containing world data)

// Move entity to new parent (transactional batch)
// 1. Update entity's ParentId, Path, Depth
// 2. Remove from old parent's Children array
// 3. Add to new parent's Children array
// Cost: ~3-6 RUs (within same world partition)
```

**Change Feed Integration**: The Change Feed processor automatically:
- Updates parent's `Children` array when child created/deleted
- Syncs to Azure AI Search index for cross-world search
- Triggers vector embedding generation for new/updated descriptions
```

**Key Design Points**:
- **Hierarchical Partition Key**: `[WorldId, ParentId, id]` enables efficient queries and infinite scaling
- **ParentId**: `null` for root World entities, otherwise references the parent entity's `id`
- **Children**: Denormalized array of child IDs for efficient tree navigation (updated on child create/delete)
- **Path**: Denormalized breadcrumb array for UI display and hierarchy validation
- **Depth**: Hierarchy level for query optimization and UI rendering
- **DescriptionEmbedding**: Vector for semantic search within world scope (Cosmos DB DiskANN index)
- **Properties**: Type-specific fields stored as flexible object (different EntityTypes have different properties)
- **IsDeleted**: Soft delete flag for undo/recovery functionality

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
    - Document
    - Homebrew

This hierarchy is extensible and can be expanded as needed for different TTRPG systems and campaign needs.

There will need to be some form of rules engine or validation to ensure that WorldEntity heirarchy is valid, e.g. a "Continent" cannot be a child of a "Monster", but a "Monster" can be a child of a "Continent". We could use AI validation for this, or a simple rules engine.
