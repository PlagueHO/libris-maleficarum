# API Contracts for Container Entity Type Support

**Feature**: Container Entity Type Support  
**Date**: 2026-01-20

## Overview

This feature **does not introduce new API endpoints**. It extends existing WorldEntity API endpoints to support 14 new entity types (10 Container types + 4 regional types with custom properties).

## Affected Endpoints

### POST /api/v1/worlds/{worldId}/entities

**Change**: Request body `entityType` field now accepts 14 additional values.

**Request Body** (unchanged structure, extended enum):

```typescript
interface CreateWorldEntityRequest {
  parentId: string | null;
  entityType: WorldEntityType;  // NOW INCLUDES: Locations, People, Events, History, Lore, Bestiary, Items, Adventures, Geographies, GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion
  name: string;
  description?: string;
  tags?: string[];
  Properties?: Record<string, unknown>;  // OPTIONAL: Custom properties for regional types
}
```

**Example: Creating a Container Entity**:

```json
POST /api/v1/worlds/world-123/entities

{
  "parentId": "world-123",
  "entityType": "Locations",
  "name": "World Locations",
  "description": "Container for all geographic entities",
  "tags": ["container", "geography"]
}
```

**Example: Creating a Regional Entity with Custom Properties**:

```json
POST /api/v1/worlds/world-123/entities

{
  "parentId": "continent-456",
  "entityType": "GeographicRegion",
  "name": "Western Europe",
  "description": "Temperate geographic region",
  "tags": ["europe", "geography"],
  "Properties": {
    "Climate": "Temperate",
    "Terrain": "Mixed",
    "Population": 195000000,
    "Area": 1234567.89
  }
}
```

**Response** (unchanged):

```json
{
  "data": {
    "id": "e5f2a1b3-4c6d-8e9f-0a1b-2c3d4e5f6a7b",
    "worldId": "world-123",
    "parentId": "continent-456",
    "entityType": "GeographicRegion",
    "name": "Western Europe",
    "description": "Temperate geographic region",
    "tags": ["europe", "geography"],
    "Properties": {
      "Climate": "Temperate",
      "Terrain": "Mixed",
      "Population": 195000000,
      "Area": 1234567.89
    },
    "path": ["world-123", "continent-456"],
    "depth": 2,
    "hasChildren": false,
    "ownerId": "user-789",
    "createdAt": "2026-01-20T10:30:00Z",
    "updatedAt": "2026-01-20T10:30:00Z",
    "isDeleted": false
  }
}
```

**Validation**:

- `entityType` MUST be one of the 29 valid WorldEntityType values (15 existing + 14 new)
- `Properties` field is optional and accepts any JSON object (backend does NOT validate custom property schemas)
- Numeric properties (Population, Area) should be non-negative numbers, but backend validation is optional (frontend enforces)
- Text list properties (Languages, MemberStates) should be string arrays, but backend accepts any JSON

---

### GET /api/v1/worlds/{worldId}/entities/{entityId}

**Change**: Response `entityType` field can now return 14 additional values. Response `Properties` field may contain custom properties.

**Request** (unchanged):

```http
GET /api/v1/worlds/world-123/entities/e5f2a1b3-4c6d-8e9f-0a1b-2c3d4e5f6a7b
```

**Response** (extended):

```json
{
  "data": {
    "id": "e5f2a1b3-4c6d-8e9f-0a1b-2c3d4e5f6a7b",
    "worldId": "world-123",
    "parentId": "continent-456",
    "entityType": "GeographicRegion",
    "name": "Western Europe",
    "description": "Temperate geographic region",
    "tags": ["europe", "geography"],
    "Properties": {
      "Climate": "Temperate",
      "Terrain": "Mixed",
      "Population": 195000000,
      "Area": 1234567.89
    },
    "path": ["world-123", "continent-456"],
    "depth": 2,
    "hasChildren": true,
    "ownerId": "user-789",
    "createdAt": "2026-01-20T10:30:00Z",
    "updatedAt": "2026-01-20T10:30:00Z",
    "isDeleted": false
  }
}
```

---

### GET /api/v1/worlds/{worldId}/entities

**Change**: Query parameter `entityType` can now filter by 14 additional types.

**Request** (extended):

```http
GET /api/v1/worlds/world-123/entities?entityType=GeographicRegion&parentId=continent-456
```

**Response** (unchanged structure, may include entities with Properties):

```json
{
  "data": [
    {
      "id": "entity-1",
      "worldId": "world-123",
      "parentId": "continent-456",
      "entityType": "GeographicRegion",
      "name": "Western Europe",
      "Properties": { "Climate": "Temperate", "Population": 195000000 },
      "...": "..."
    },
    {
      "id": "entity-2",
      "worldId": "world-123",
      "parentId": "continent-456",
      "entityType": "GeographicRegion",
      "name": "Eastern Europe",
      "Properties": { "Climate": "Continental", "Population": 150000000 },
      "...": "..."
    }
  ],
  "meta": {
    "count": 2,
    "nextCursor": null
  }
}
```

---

### PUT /api/v1/worlds/{worldId}/entities/{entityId}

**Change**: Request body `Properties` field can be updated for regional types.

**Request Body** (extended):

```json
PUT /api/v1/worlds/world-123/entities/e5f2a1b3-4c6d-8e9f-0a1b-2c3d4e5f6a7b

{
  "name": "Western Europe (Updated)",
  "description": "Updated description",
  "Properties": {
    "Climate": "Temperate Oceanic",
    "Terrain": "Mixed Coastal",
    "Population": 200000000,
    "Area": 1250000.00
  }
}
```

**Note**: This feature **does NOT implement editing custom properties** in the frontend. Backend MUST support this for future iterations, but frontend will only display Properties in read-only mode.

**Response** (unchanged structure):

```json
{
  "data": {
    "id": "e5f2a1b3-4c6d-8e9f-0a1b-2c3d4e5f6a7b",
    "worldId": "world-123",
    "parentId": "continent-456",
    "entityType": "GeographicRegion",
    "name": "Western Europe (Updated)",
    "description": "Updated description",
    "Properties": {
      "Climate": "Temperate Oceanic",
      "Terrain": "Mixed Coastal",
      "Population": 200000000,
      "Area": 1250000.00
    },
    "...": "...",
    "updatedAt": "2026-01-20T12:00:00Z"
  }
}
```

---

## Backend Requirements

While this feature is **frontend-only**, the backend MUST support the following to avoid breaking changes:

1. **Entity Type Validation**: Accept 14 new WorldEntityType values without throwing validation errors
1. **Properties Field**: Store and retrieve `Properties` JSON field without modification or validation
1. **Query Filtering**: Support filtering by new entity types in GET /entities endpoint
1. **No Schema Changes**: No Cosmos DB schema migrations required (Properties field already exists)

**Recommendation**: Backend team should add the 14 new entity types to the C# WorldEntityType enum in a separate PR before frontend changes are merged.

**C# Enum Update Example**:

```csharp
public enum WorldEntityType
{
    // Existing types
    Continent,
    Country,
    Region,
    City,
    Building,
    Room,
    Location,
    Character,
    Faction,
    Event,
    Item,
    Campaign,
    Session,
    Quest,
    Other,
    
    // NEW: Container types
    Locations,
    People,
    Events,
    History,
    Lore,
    Bestiary,
    Items,
    Adventures,
    Geographies,
    
    // NEW: Regional types
    GeographicRegion,
    PoliticalRegion,
    CulturalRegion,
    MilitaryRegion
}
```

---

## TypeScript Type Definitions (Frontend)

### Updated Request/Response Types

```typescript
// In worldEntity.types.ts

export const WorldEntityType = {
  // Existing types (15)
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
  
  // NEW: Container types (9 + 1 existing 'Other')
  Locations: 'Locations',
  People: 'People',
  Events: 'Events',  // Note: Different from Event (singular)
  History: 'History',
  Lore: 'Lore',
  Bestiary: 'Bestiary',
  Items: 'Items',    // Note: Different from Item (singular)
  Adventures: 'Adventures',
  Geographies: 'Geographies',
  
  // NEW: Regional types (4)
  GeographicRegion: 'GeographicRegion',
  PoliticalRegion: 'PoliticalRegion',
  CulturalRegion: 'CulturalRegion',
  MilitaryRegion: 'MilitaryRegion',
} as const;

export type WorldEntityType = (typeof WorldEntityType)[keyof typeof WorldEntityType];

// WorldEntity interface ALREADY supports Properties field
export interface WorldEntity {
  id: string;
  worldId: string;
  parentId: string | null;
  entityType: WorldEntityType;
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
  Properties?: Record<string, unknown>;  // Already exists, no changes needed
}

// CreateWorldEntityRequest ALREADY supports Properties field
export interface CreateWorldEntityRequest {
  parentId: string | null;
  entityType: WorldEntityType;  // Type automatically includes new values
  name: string;
  description?: string;
  tags?: string[];
  Properties?: Record<string, unknown>;  // Already exists, no changes needed
}
```

---

## Backward Compatibility

**Frontend**:

- All existing WorldEntity API calls continue to work
- Existing entity types unaffected
- Properties field was already optional, so adding it to requests is non-breaking

**Backend**:

- MUST add new entity types to enum (non-breaking if backend defaults to accepting unknown strings)
- MUST NOT validate Properties field schema (already flexible JSON)
- MUST NOT reject unknown entity type values during transition period

**Migration Strategy**:

1. Backend PR: Add 14 new entity types to C# enum
1. Deploy backend changes to Test environment
1. Frontend PR: Add 14 new types to TypeScript enum and implement UI
1. Deploy frontend changes
1. No data migration needed (new types only)

---

## Testing

### Contract Tests

**Test: Create Container Entity**

```typescript
test('POST /entities with Container type', async () => {
  const response = await api.post('/api/v1/worlds/world-123/entities', {
    parentId: 'world-123',
    entityType: 'Locations',
    name: 'World Locations',
    description: 'Container for geography',
  });
  
  expect(response.status).toBe(201);
  expect(response.data.data.entityType).toBe('Locations');
});
```

**Test: Create Regional Entity with Properties**

```typescript
test('POST /entities with GeographicRegion and Properties', async () => {
  const response = await api.post('/api/v1/worlds/world-123/entities', {
    parentId: 'continent-456',
    entityType: 'GeographicRegion',
    name: 'Western Europe',
    Properties: {
      Climate: 'Temperate',
      Population: 195000000,
    },
  });
  
  expect(response.status).toBe(201);
  expect(response.data.data.Properties.Climate).toBe('Temperate');
  expect(response.data.data.Properties.Population).toBe(195000000);
});
```

**Test: Get Entity with Properties**

```typescript
test('GET /entities/{id} returns Properties field', async () => {
  const response = await api.get('/api/v1/worlds/world-123/entities/entity-123');
  
  expect(response.status).toBe(200);
  expect(response.data.data.Properties).toBeDefined();
  expect(response.data.data.Properties.Climate).toBe('Temperate');
});
```

**Test: Filter by Container Type**

```typescript
test('GET /entities?entityType=Locations', async () => {
  const response = await api.get('/api/v1/worlds/world-123/entities?entityType=Locations');
  
  expect(response.status).toBe(200);
  expect(response.data.data.every(e => e.entityType === 'Locations')).toBe(true);
});
```

---

## Summary

This feature introduces **zero new API endpoints** and **zero breaking changes**. It extends existing endpoints to support 14 new entity types and leverages the existing flexible `Properties` JSON field for custom properties.

**API Impact**: Minimal (enum extension only)  
**Breaking Changes**: None  
**Backend Work Required**: Add 14 entity types to C# enum (est. 30 minutes)  
**Frontend Work Required**: Full implementation (this feature scope)
