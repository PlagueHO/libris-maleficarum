# Phase 1: Data Model Design

**Feature**: Container Entity Type Support  
**Date**: 2026-01-20

## Overview

This feature extends the existing WorldEntity data model with 14 new entity types but requires **zero schema changes** to the Cosmos DB container. All custom properties leverage the existing flexible `Properties` JSON field in the BaseWorldEntity schema.

## Entity Type Extensions

### Container EntityTypes (10 new types)

Container types are top-level organizational folders used to categorize world content by domain.

| Type | Purpose | Icon | Recommended Children |
|------|---------|------|---------------------|
| `Locations` | Geographic and spatial entities | `FolderRegular` | Continent, GeographicRegion, Country, City, Dungeon, Map |
| `People` | Character, faction, person-related | `PeopleRegular` | Character, Organization, Faction, Family, Race, Culture |
| `Events` | Event and quest-related | `CalendarRegular` | Quest, Encounter, Battle, Festival, HistoricalEvent |
| `Lore` | Lore, mythology, cultural knowledge | `BookRegular` | Religion, Deity, MagicSystem, Artifact, Story, Myth |
| `History` | Historical events and timelines | `BookRegular` | Timeline, Era, Chronicle, HistoricalEvent |
| `Bestiary` | Creature and monster catalog | `PawRegular` | Creature, Monster, Animal |
| `Items` | Item and artifact catalog | `BoxRegular` | Equipment, Weapon, Armor, Item |
| `Adventures` | Campaign and adventure tracking | `CompassRegular` | Campaign, Session, Scene, Quest |
| `Geographies` | Geographic feature catalog | `MapRegular` | Mountain, River, Lake, Forest, Desert, Ocean |
| `Other` | Catch-all (already exists) | `FolderRegular` | Any type |

**Schema Impact**: None. Container types use standard WorldEntity schema.

### Regional EntityTypes with Custom Properties (4 new types)

Regional types support domain-specific properties stored in the `Properties` JSON field.

#### GeographicRegion

Natural geographic areas (climate zones, ecological regions, topographic divisions).

**Custom Properties**:

```typescript
interface GeographicRegionProperties {
  Climate?: string;      // e.g., "Tropical", "Temperate", "Arid"
  Terrain?: string;      // e.g., "Rainforest", "Mountains", "Plains"
  Population?: number;   // Aggregate population (integer, max Number.MAX_SAFE_INTEGER)
  Area?: number;         // Area in km² (decimal allowed, max Number.MAX_SAFE_INTEGER)
}
```

**Example Cosmos DB Document**:

```json
{
  "id": "e5f2a1b3-4c6d-8e9f-0a1b-2c3d4e5f6a7b",
  "worldId": "world-123",
  "parentId": "continent-456",
  "entityType": "GeographicRegion",
  "name": "Western Europe",
  "description": "Temperate geographic region encompassing multiple countries",
  "Properties": {
    "Climate": "Temperate",
    "Terrain": "Mixed",
    "Population": 195000000,
    "Area": 1234567.89
  },
  "tags": ["europe", "geography"],
  "path": ["world-123", "continent-456"],
  "depth": 2,
  "hasChildren": true,
  "ownerId": "user-789",
  "createdAt": "2026-01-20T10:30:00Z",
  "updatedAt": "2026-01-20T10:30:00Z",
  "isDeleted": false
}
```

---

#### PoliticalRegion

Political boundaries and governance structures (alliances, federations, trade zones).

**Custom Properties**:

```typescript
interface PoliticalRegionProperties {
  GovernmentType?: string;        // e.g., "Federation", "Alliance", "Trade Bloc"
  MemberStates?: string[];        // Array of member country names
  EstablishedDate?: string;       // ISO 8601 date string
}
```

**Example Cosmos DB Document**:

```json
{
  "id": "f6a7b8c9-5d7e-9f0a-1b2c-3d4e5f6a7b8c",
  "worldId": "world-123",
  "parentId": "world-123",
  "entityType": "PoliticalRegion",
  "name": "European Union",
  "description": "Political and economic union of member states",
  "Properties": {
    "GovernmentType": "Supranational Union",
    "MemberStates": ["France", "Germany", "Italy", "Spain"],
    "EstablishedDate": "1993-11-01"
  },
  "tags": ["politics", "alliance"],
  "path": ["world-123"],
  "depth": 1,
  "hasChildren": true,
  "ownerId": "user-789",
  "createdAt": "2026-01-20T11:00:00Z",
  "updatedAt": "2026-01-20T11:00:00Z",
  "isDeleted": false
}
```

---

#### CulturalRegion

Cultural boundaries and shared heritage (language zones, religious spheres).

**Custom Properties**:

```typescript
interface CulturalRegionProperties {
  Languages?: string[];           // Array of spoken languages
  Religions?: string[];           // Array of practiced religions
  CulturalTraits?: string;        // Free-text description
}
```

**Example Cosmos DB Document**:

```json
{
  "id": "a1b2c3d4-6e7f-0a1b-2c3d-4e5f6a7b8c9d",
  "worldId": "world-123",
  "parentId": "continent-456",
  "entityType": "CulturalRegion",
  "name": "Nordic Realms",
  "description": "Cultural sphere encompassing Scandinavian traditions",
  "Properties": {
    "Languages": ["Swedish", "Norwegian", "Danish", "Icelandic"],
    "Religions": ["Lutheran Christianity", "Norse Paganism"],
    "CulturalTraits": "Viking heritage, egalitarian values, social welfare tradition"
  },
  "tags": ["culture", "scandinavia"],
  "path": ["world-123", "continent-456"],
  "depth": 2,
  "hasChildren": true,
  "ownerId": "user-789",
  "createdAt": "2026-01-20T11:15:00Z",
  "updatedAt": "2026-01-20T11:15:00Z",
  "isDeleted": false
}
```

---

#### MilitaryRegion

Military zones, command structures, strategic divisions.

**Custom Properties**:

```typescript
interface MilitaryRegionProperties {
  CommandStructure?: string;      // e.g., "Northern Command", "Fleet Division"
  StrategicImportance?: string;   // e.g., "Critical", "High", "Moderate"
  MilitaryAssets?: string[];      // Array of asset descriptions
}
```

**Example Cosmos DB Document**:

```json
{
  "id": "b2c3d4e5-7f8a-1b2c-3d4e-5f6a7b8c9d0e",
  "worldId": "world-123",
  "parentId": "country-789",
  "entityType": "MilitaryRegion",
  "name": "Northern Defense Zone",
  "description": "Strategic military district covering northern border",
  "Properties": {
    "CommandStructure": "Regional Command North",
    "StrategicImportance": "Critical",
    "MilitaryAssets": ["3rd Army Division", "12th Air Wing", "Northern Fleet"]
  },
  "tags": ["military", "defense"],
  "path": ["world-123", "country-789"],
  "depth": 2,
  "hasChildren": false,
  "ownerId": "user-789",
  "createdAt": "2026-01-20T11:30:00Z",
  "updatedAt": "2026-01-20T11:30:00Z",
  "isDeleted": false
}
```

---

## TypeScript Type Definitions

### Updated WorldEntityType Enum

```typescript
// In worldEntity.types.ts
export const WorldEntityType = {
  // Existing types (unchanged)
  Continent: 'Continent',
  Country: 'Country',
  Region: 'Region',
  City: 'City',
  Building: 'Building',
  Room: 'Room',
  Location: 'Location',
  Character: 'Character',
  Faction: 'Faction',
  Event: 'Event',
  Item: 'Item',
  Campaign: 'Campaign',
  Session: 'Session',
  Quest: 'Quest',
  Other: 'Other',
  
  // NEW: Container types (10)
  Locations: 'Locations',
  People: 'People',
  Events: 'Events',
  History: 'History',
  Lore: 'Lore',
  Bestiary: 'Bestiary',
  Items: 'Items',
  Adventures: 'Adventures',
  Geographies: 'Geographies',
  // Other already exists
  
  // NEW: Regional types with custom properties (4)
  GeographicRegion: 'GeographicRegion',
  PoliticalRegion: 'PoliticalRegion',
  CulturalRegion: 'CulturalRegion',
  MilitaryRegion: 'MilitaryRegion',
} as const;

export type WorldEntityType = (typeof WorldEntityType)[keyof typeof WorldEntityType];
```

### Custom Property Interfaces

```typescript
// In worldEntity.types.ts or new file: customProperties.types.ts

export interface GeographicRegionProperties {
  Climate?: string;
  Terrain?: string;
  Population?: number;
  Area?: number;
}

export interface PoliticalRegionProperties {
  GovernmentType?: string;
  MemberStates?: string[];
  EstablishedDate?: string;
}

export interface CulturalRegionProperties {
  Languages?: string[];
  Religions?: string[];
  CulturalTraits?: string;
}

export interface MilitaryRegionProperties {
  CommandStructure?: string;
  StrategicImportance?: string;
  MilitaryAssets?: string[];
}

// Union type for all custom properties
export type CustomEntityProperties =
  | GeographicRegionProperties
  | PoliticalRegionProperties
  | CulturalRegionProperties
  | MilitaryRegionProperties;
```

### Extended WorldEntity Interface

```typescript
// In worldEntity.types.ts - WorldEntity interface remains unchanged
export interface WorldEntity {
  id: string;
  worldId: string;
  parentId: string | null;
  entityType: WorldEntityType;  // Now includes Container and Regional types
  name: string;
  description?: string;
  tags: string[];
  path: string[];
  depth: number;
  hasChildren: boolean;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
  
  // Properties field already exists - now used for custom properties
  Properties?: Record<string, unknown>;  // Flexible JSON
}
```

---

## Entity Type Metadata Extensions

### ENTITY_TYPE_META Updates

```typescript
export const ENTITY_TYPE_META: Record<
  WorldEntityType,
  {
    label: string;
    description: string;
    category: 'Geography' | 'Characters & Factions' | 'Events & Quests' | 'Items' | 'Campaigns' | 'Containers' | 'Other';
    icon: string;  // NEW: Icon name from @fluentui/react-icons
  }
> = {
  // ... existing entries ...
  
  // NEW: Container types
  [WorldEntityType.Locations]: {
    label: 'Locations',
    description: 'Container for geographic and spatial entities',
    category: 'Containers',
    icon: 'FolderRegular',
  },
  [WorldEntityType.People]: {
    label: 'People',
    description: 'Container for characters, factions, and organizations',
    category: 'Containers',
    icon: 'PeopleRegular',
  },
  [WorldEntityType.Events]: {
    label: 'Events',
    description: 'Container for events, quests, and happenings',
    category: 'Containers',
    icon: 'CalendarRegular',
  },
  [WorldEntityType.History]: {
    label: 'History',
    description: 'Container for historical events and timelines',
    category: 'Containers',
    icon: 'BookRegular',
  },
  [WorldEntityType.Lore]: {
    label: 'Lore',
    description: 'Container for lore, mythology, and cultural knowledge',
    category: 'Containers',
    icon: 'BookRegular',
  },
  [WorldEntityType.Bestiary]: {
    label: 'Bestiary',
    description: 'Container for creatures and monsters',
    category: 'Containers',
    icon: 'PawRegular',
  },
  [WorldEntityType.Items]: {
    label: 'Items',
    description: 'Container for items and artifacts',
    category: 'Containers',
    icon: 'BoxRegular',
  },
  [WorldEntityType.Adventures]: {
    label: 'Adventures',
    description: 'Container for campaigns and adventures',
    category: 'Containers',
    icon: 'CompassRegular',
  },
  [WorldEntityType.Geographies]: {
    label: 'Geographies',
    description: 'Container for geographic features',
    category: 'Containers',
    icon: 'MapRegular',
  },
  
  // NEW: Regional types
  [WorldEntityType.GeographicRegion]: {
    label: 'Geographic Region',
    description: 'Natural geographic area with climate and terrain properties',
    category: 'Geography',
    icon: 'GlobeRegular',
  },
  [WorldEntityType.PoliticalRegion]: {
    label: 'Political Region',
    description: 'Political boundary or governance structure',
    category: 'Geography',
    icon: 'ShieldRegular',
  },
  [WorldEntityType.CulturalRegion]: {
    label: 'Cultural Region',
    description: 'Cultural sphere with shared language and heritage',
    category: 'Geography',
    icon: 'PeopleRegular',
  },
  [WorldEntityType.MilitaryRegion]: {
    label: 'Military Region',
    description: 'Military zone or command structure',
    category: 'Geography',
    icon: 'ShieldRegular',
  },
};
```

### ENTITY_TYPE_SUGGESTIONS Updates

```typescript
export const ENTITY_TYPE_SUGGESTIONS: Record<
  WorldEntityType,
  WorldEntityType[]
> = {
  // ... existing entries ...
  
  // NEW: Container type suggestions
  [WorldEntityType.Locations]: [
    WorldEntityType.Continent,
    WorldEntityType.GeographicRegion,
    WorldEntityType.PoliticalRegion,
    WorldEntityType.Country,
    WorldEntityType.Region,
    WorldEntityType.City,
    WorldEntityType.Dungeon,
    WorldEntityType.Building,
    WorldEntityType.Map,
  ],
  [WorldEntityType.People]: [
    WorldEntityType.Character,
    WorldEntityType.Organization,
    WorldEntityType.Faction,
    WorldEntityType.Family,
    WorldEntityType.Race,
    WorldEntityType.Culture,
  ],
  [WorldEntityType.Events]: [
    WorldEntityType.Quest,
    WorldEntityType.Encounter,
    WorldEntityType.Battle,
    WorldEntityType.Festival,
    WorldEntityType.HistoricalEvent,
    WorldEntityType.CurrentEvent,
  ],
  [WorldEntityType.Lore]: [
    WorldEntityType.Religion,
    WorldEntityType.Deity,
    WorldEntityType.MagicSystem,
    WorldEntityType.Artifact,
    WorldEntityType.Story,
    WorldEntityType.Myth,
    WorldEntityType.Legend,
  ],
  [WorldEntityType.Items]: [
    WorldEntityType.Equipment,
    WorldEntityType.Weapon,
    WorldEntityType.Armor,
    WorldEntityType.Item,
  ],
  [WorldEntityType.Adventures]: [
    WorldEntityType.Campaign,
    WorldEntityType.Session,
    WorldEntityType.Scene,
    WorldEntityType.Quest,
  ],
  [WorldEntityType.Geographies]: [
    WorldEntityType.Mountain,
    WorldEntityType.River,
    WorldEntityType.Lake,
    WorldEntityType.Forest,
    WorldEntityType.Desert,
    WorldEntityType.Ocean,
    WorldEntityType.Island,
    WorldEntityType.ClimateZone,
  ],
  [WorldEntityType.History]: [
    WorldEntityType.Timeline,
    WorldEntityType.Era,
    WorldEntityType.Chronicle,
    WorldEntityType.HistoricalEvent,
  ],
  [WorldEntityType.Bestiary]: [
    WorldEntityType.Creature,
    WorldEntityType.Monster,
    WorldEntityType.Animal,
  ],
  
  // NEW: Regional type suggestions
  [WorldEntityType.GeographicRegion]: [
    WorldEntityType.Country,
    WorldEntityType.Province,
    WorldEntityType.Region,
    WorldEntityType.GeographicRegion, // Can nest
  ],
  [WorldEntityType.PoliticalRegion]: [
    WorldEntityType.Country,
    WorldEntityType.PoliticalRegion, // Can nest
  ],
  [WorldEntityType.CulturalRegion]: [
    WorldEntityType.Country,
    WorldEntityType.CulturalRegion, // Can nest
  ],
  [WorldEntityType.MilitaryRegion]: [
    WorldEntityType.MilitaryRegion, // Can nest
  ],
  
  // UPDATE: Existing types to recommend regional types
  [WorldEntityType.Continent]: [
    WorldEntityType.GeographicRegion, // NEW
    WorldEntityType.PoliticalRegion,  // NEW
    WorldEntityType.Country,
    WorldEntityType.Region,
    WorldEntityType.Faction,
    WorldEntityType.Event,
  ],
};

// Update root-level suggestions
export function getEntityTypeSuggestions(
  parentType: WorldEntityType | null,
): WorldEntityType[] {
  if (!parentType) {
    // Root level suggestions (no parent) - add Container types
    return [
      WorldEntityType.Locations,
      WorldEntityType.People,
      WorldEntityType.Events,
      WorldEntityType.Adventures,
      WorldEntityType.Lore,
      WorldEntityType.Continent,
      WorldEntityType.Campaign,
    ];
  }

  return ENTITY_TYPE_SUGGESTIONS[parentType] ?? [];
}
```

---

## Cosmos DB Queries

### Query Examples with Custom Properties

```sql
-- Find all GeographicRegions with population > 10 million
SELECT * FROM c
WHERE c.entityType = 'GeographicRegion'
  AND c.Properties.Population > 10000000
  AND c.isDeleted = false

-- Find all PoliticalRegions established after 1990
SELECT * FROM c
WHERE c.entityType = 'PoliticalRegion'
  AND c.Properties.EstablishedDate >= '1990-01-01'
  AND c.isDeleted = false

-- Find CulturalRegions where English is spoken
SELECT * FROM c
WHERE c.entityType = 'CulturalRegion'
  AND ARRAY_CONTAINS(c.Properties.Languages, 'English')
  AND c.isDeleted = false

-- Get all Container type entities at root level
SELECT * FROM c
WHERE c.parentId = c.worldId
  AND c.entityType IN ('Locations', 'People', 'Events', 'Lore', 'Items', 'Adventures', 'Geographies', 'History', 'Bestiary', 'Other')
  AND c.isDeleted = false
```

**Note**: These queries are for future reference. Current feature scope is frontend-only; backend query optimizations deferred.

---

## Schema Migration Impact

**Required Migrations**: NONE

**Rationale**:

- Container types use standard WorldEntity schema
- Regional types store custom properties in existing `Properties` JSON field
- No new Cosmos DB containers needed
- No index changes required
- Backward compatible: Existing entities unaffected

**Future Considerations**:

- If custom property queries become performance-critical, consider adding dedicated indexes on `Properties.Population`, `Properties.Area`, etc.
- AI Search indexing will handle complex property queries (out of scope for this feature)

---

## Validation Rules

### Numeric Properties

| Property | Type | Min | Max | Precision |
|----------|------|-----|-----|-----------|
| Population | Integer | 0 | Number.MAX_SAFE_INTEGER | None (whole numbers) |
| Area | Decimal | 0 | Number.MAX_SAFE_INTEGER | Up to 2 decimal places |

### Text List Properties

| Property | Type | Min Items | Max Items | Max Item Length |
|----------|------|-----------|-----------|-----------------|
| Languages | string[] | 0 | Unlimited | 50 chars per item |
| Religions | string[] | 0 | Unlimited | 50 chars per item |
| MemberStates | string[] | 0 | Unlimited | 100 chars per item |
| MilitaryAssets | string[] | 0 | Unlimited | 100 chars per item |

**Enforcement**: Frontend validation only (no backend schema validation in this iteration)

---

## Summary

This data model design:

- Extends existing WorldEntity schema with 14 new entity types
- Leverages flexible Properties field for custom properties (zero schema migrations)
- Maintains backward compatibility with existing entities
- Provides clear TypeScript type safety with union types
- Supports future extensibility through JSON schema pattern
- Enables efficient Cosmos DB queries (though not implemented in this feature scope)

All data model decisions align with research findings from Phase 0.
