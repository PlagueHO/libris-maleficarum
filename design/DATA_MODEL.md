# Data Model & Cosmos DB Design

All TTRPG-related entities are stored in Azure Cosmos DB using a flexible, hierarchical model.

## World Entity Container

- **WorldEntity**: The core document type for all campaign/world data. Each "WorldEntity" can represent a "World" (root), or any nested entity such as "Continent", "Country", "Region", "City", "Character", etc. Entities can be arbitrarily nested to support complex world-building.
- **Ownership**: Every WorldEntity includes an `OwnerId` property (e.g., a user ID) to indicate the owner, though this may not be used yet.
- **Document IDs**: All WorldEntities use a GUID for the `id` property to ensure uniqueness.
- **Partition Key**: The partition key for the WorldEntity container is `/WorldId`. This ensures efficient lookups and RU/s usage, as most queries will be scoped to a single world.
- **Indexing**: All WorldEntities are indexed by Azure AI Search for advanced semantic and full-text search.

### Example: WorldEntity Documents

```json

// WorldEntity document (World)
{
  "id": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "OwnerId": "user-abc",
  "Name": "Eldoria",
  "EntityType": "World",
  "CreatedDate": "2024-06-01T10:00:00Z",
  "ModifiedDate": "2024-06-01T10:00:00Z"
  // ... properties for WorldEntity (e.g., description, lore, etc.)
}

// WorldEntity document (Continent, child of World)
{
  "id": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "OwnerId": "user-abc",
  "Name": "Arcanis",
  "EntityType": "Continent",
  "CreatedDate": "2024-06-01T10:00:00Z",
  "ModifiedDate": "2024-06-01T10:00:00Z"
  // ... properties for WorldEntity (e.g., description, lore, etc.)

}

// WorldEntity document (Country, child of Continent)
{
  "id": "c9d8e7f6-5a4b-3c2d-1e0f-9a8b7c6d5e4f",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "OwnerId": "user-abc",
  "Name": "Valoria",
  "EntityType": "Country",
  "CreatedDate": "2024-06-01T10:00:00Z",
  "ModifiedDate": "2024-06-01T10:00:00Z"
  // ... properties for WorldEntity (e.g., description, lore, etc.)
}
```

- Each WorldEntity includes a reference to its parent (`ParentId`), the root world (`WorldId`), and an `OwnerId`.
- The `id` for every WorldEntity is a GUID.
- The partition key is `/WorldId` for efficient partitioning and query performance.

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
