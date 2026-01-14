# Data Model: World Sidebar Navigation

**Feature**: World Sidebar Navigation (`003-world-sidebar`)  
**Created**: 2026-01-13  
**Status**: Design Document  

## Overview

This document defines the data structures, TypeScript interfaces, and caching mechanisms for the World Sidebar Navigation feature. It serves as the authoritative reference for implementing WorldEntity hierarchies, client-side caching, and API integration.

---

## WorldEntity Schema

### TypeScript Interface

```typescript
/**
 * Hierarchical entity within a World (continents, countries, characters, etc.)
 * Stored in Azure Cosmos DB WorldEntity container with hierarchical partition key
 */
export interface WorldEntity {
  /** Unique identifier (GUID) */
  id: string;

  /** Parent world reference (partition key part 1) */
  worldId: string;

  /** Parent entity reference (null for root entities, equals worldId for root-level) */
  parentId: string | null;

  /** Entity classification */
  entityType: WorldEntityType;

  /** Display name (1-100 characters) */
  name: string;

  /** Optional description (max 500 characters) */
  description?: string;

  /** User-defined tags for filtering/search */
  tags: string[];

  /** Array of ancestor IDs (for hierarchy queries) */
  path: string[];

  /** Hierarchy level (0 = root) */
  depth: number;

  /** Optimization flag: skip children query if false */
  hasChildren: boolean;

  /** Owner user identifier */
  ownerId: string;

  /** ISO 8601 timestamp */
  createdAt: string;

  /** ISO 8601 timestamp */
  updatedAt: string;

  /** Soft delete flag */
  isDeleted: boolean;
}

/**
 * Entity type enumeration
 */
export enum WorldEntityType {
  Continent = 'Continent',
  Country = 'Country',
  Region = 'Region',
  City = 'City',
  Location = 'Location',
  Character = 'Character',
  Organization = 'Organization',
  Event = 'Event',
  Item = 'Item',
  Campaign = 'Campaign',
}
```

### Cosmos DB Container Configuration

**Container Name**: `WorldEntity`  
**Partition Key**: `/WorldId` (hierarchical with `/id` for efficient parent-child queries)  
**Indexing Policy**: Default (all paths indexed), custom index on `/path/*` for ancestor queries

**Example Document**:

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "worldId": "123e4567-e89b-12d3-a456-426614174000",
  "parentId": null,
  "entityType": "Continent",
  "name": "Faerûn",
  "description": "A continent in the world of Toril",
  "tags": ["forgotten-realms", "primary-setting"],
  "path": [],
  "depth": 0,
  "hasChildren": true,
  "ownerId": "user@example.com",
  "createdAt": "2026-01-13T12:00:00Z",
  "updatedAt": "2026-01-13T12:00:00Z",
  "isDeleted": false
}
```

---

## Cache Structure (sessionStorage)

### Overview

The sidebar uses **three separate cache keys** per world to store hierarchy data, expanded state, and selected entity. All caches use a **5-minute TTL** and are cleared on tab/browser close (sessionStorage behavior).

### Cache Keys

#### 1. Hierarchy Data Cache

**Key Pattern**: `sidebar_hierarchy_{worldId}`

**Purpose**: Cache fetched entity children to avoid redundant API calls

**Structure**:

```typescript
interface HierarchyCacheEntry {
  data: WorldEntity[];     // Array of entities (children of a parent)
  timestamp: number;       // Date.now() when cached
  ttl: 300000;            // 5 minutes in milliseconds
}
```

**Example** (`sidebar_hierarchy_123e4567-e89b-12d3-a456-426614174000_550e8400-e29b-41d4-a716-446655440001`):

```json
{
  "data": [
    {
      "id": "abcd-1234",
      "worldId": "123e4567",
      "parentId": "550e8400",
      "entityType": "Country",
      "name": "Cormyr",
      "hasChildren": true,
      ...
    }
  ],
  "timestamp": 1736769600000,
  "ttl": 300000
}
```

#### 2. Expanded Nodes Cache

**Key Pattern**: `sidebar_expanded_{worldId}`

**Purpose**: Remember which entity nodes are expanded to restore UI state

**Structure**:

```typescript
interface ExpandedCacheEntry {
  expandedIds: string[];   // Array of expanded entity IDs
  timestamp: number;
  ttl: 300000;
}
```

**Example** (`sidebar_expanded_123e4567-e89b-12d3-a456-426614174000`):

```json
{
  "expandedIds": ["550e8400-e29b-41d4-a716-446655440001", "abcd-1234-5678-90ef"],
  "timestamp": 1736769600000,
  "ttl": 300000
}
```

#### 3. Selected Entity Cache

**Key Pattern**: `sidebar_selected_{worldId}`

**Purpose**: Remember which entity is currently selected (drives main panel display)

**Structure**:

```typescript
interface SelectedCacheEntry {
  selectedEntityId: string | null;
  timestamp: number;
  ttl: 300000;
}
```

**Example** (`sidebar_selected_123e4567-e89b-12d3-a456-426614174000`):

```json
{
  "selectedEntityId": "abcd-1234-5678-90ef",
  "timestamp": 1736769600000,
  "ttl": 300000
}
```

---

## Hierarchy Navigation Patterns

### Loading Root Entities

**Scenario**: User selects a world in WorldSelector

**Flow**:
1. Check cache: `get('sidebar_hierarchy_{worldId}_root', [])`
2. If cache hit → render immediately
3. If cache miss → API call: `GET /api/v1/worlds/{worldId}/entities?parentId={worldId}`
4. Cache response: `set('sidebar_hierarchy_{worldId}_root', entities)`
5. Render entities in EntityTree

### Expanding Entity Node

**Scenario**: User clicks chevron to expand entity with children

**Flow**:
1. Check `hasChildren` flag → if false, skip (leaf node)
2. Check cache: `get('sidebar_hierarchy_{worldId}_{parentId}', [])`
3. If cache hit → render children instantly (<100ms per FR-002)
4. If cache miss → API call: `GET /api/v1/worlds/{worldId}/entities?parentId={parentId}`
5. Cache response: `set('sidebar_hierarchy_{worldId}_{parentId}', children)`
6. Update expanded state: `add parentId to expandedIds array`
7. Cache expanded state: `set('sidebar_expanded_{worldId}', expandedIds)`
8. Render children

### Switching Worlds

**Scenario**: User selects different world from WorldSelector dropdown

**Flow**:
1. Clear current world UI state (collapse all nodes, deselect)
2. Load new world: follow "Loading Root Entities" flow
3. Restore expanded state from cache: `get('sidebar_expanded_{newWorldId}', [])`
4. For each cached expandedId, follow "Expanding Entity Node" flow (should hit cache)
5. Restore selected entity: `get('sidebar_selected_{newWorldId}', null)`

### Cache Invalidation on Mutation

**Scenario**: User creates, updates, deletes, or moves an entity

**Triggers**:
- **Create**: Invalidate parent's children cache (`sidebar_hierarchy_{worldId}_{parentId}`)
- **Update**: Invalidate entity's cache entry + parent's children cache
- **Delete**: Invalidate entity's cache + parent's children cache + all descendant caches
- **Move**: Invalidate old parent's cache + new parent's cache + entity's cache

**Implementation** (RTK Query middleware):

```typescript
// worldEntityApi.ts
createEntity: builder.mutation({
  query: (newEntity) => ({ ... }),
  invalidatesTags: (result, error, arg) => [
    { type: 'WorldEntity', id: arg.parentId }, // Invalidate parent's children
  ],
  onQueryStarted: async (arg, { dispatch, queryFulfilled }) => {
    await queryFulfilled;
    // Invalidate sessionStorage cache
    invalidate(`sidebar_hierarchy_${arg.worldId}_${arg.parentId}`);
  },
}),
```

---

## API Response Types

### GET /api/v1/worlds/{worldId}/entities

**Query Parameters**:
- `parentId`: Filter by parent (null or worldId for root entities)
- `type`: Filter by entity type (optional)
- `tags`: Comma-separated tags filter (optional)
- `limit`: Pagination limit (default 50, max 100)
- `cursor`: Pagination cursor (optional)

**Response**:

```typescript
interface WorldEntityListResponse {
  data: WorldEntity[];
  pagination?: {
    cursor: string | null;
    hasMore: boolean;
  };
}
```

### POST /api/v1/worlds/{worldId}/entities

**Request Body**:

```typescript
interface CreateWorldEntityRequest {
  parentId: string | null;      // null for root, worldId for root-level, entityId for child
  entityType: WorldEntityType;
  name: string;
  description?: string;
  tags?: string[];
}
```

**Response**: `WorldEntity` (created entity)

### PUT /api/v1/worlds/{worldId}/entities/{entityId}

**Request Body**:

```typescript
interface UpdateWorldEntityRequest {
  name?: string;
  description?: string;
  tags?: string[];
  entityType?: WorldEntityType;
}
```

**Response**: `WorldEntity` (updated entity)

---

## Redux State Structure

### worldSidebarSlice

```typescript
interface WorldSidebarState {
  // World selection
  selectedWorldId: string | null;

  // Entity navigation
  selectedEntityId: string | null;  // Drives main panel display
  expandedNodeIds: string[];        // Array of expanded entity IDs

  // Modal state
  isWorldFormOpen: boolean;
  isEntityFormOpen: boolean;
  editingWorldId: string | null;    // null = create mode
  editingEntityId: string | null;   // null = create mode
  parentEntityId: string | null;    // For creating child entities
}
```

**Actions**:
- `setSelectedWorld(worldId)`: Change active world
- `setSelectedEntity(entityId)`: Select entity (update main panel)
- `toggleNodeExpanded(entityId)`: Expand/collapse entity
- `openWorldForm(worldId?)`: Open world modal (create/edit)
- `closeWorldForm()`: Close world modal
- `openEntityForm({ parentId?, entityId? })`: Open entity modal
- `closeEntityForm()`: Close entity modal

---

## Performance Considerations

### Cache Size Limits

- **sessionStorage limit**: ~5MB per origin
- **Estimated cache size per world**: ~10KB (50 entities @ 200 bytes each)
- **Max worlds before issues**: ~500 worlds (unlikely)

### Cache Hit Rate Targets

- **Target**: >80% cache hit rate (per FR-008)
- **Measurement**: Log cache hits/misses in development mode
- **Optimization**: Increase TTL if users frequently re-expand same nodes

### Lazy Loading Strategy

- **Root entities**: Always loaded on world selection
- **Children**: Loaded only when parent expanded for first time
- **Prefetching**: NOT implemented (would violate lazy loading principle)

---

## Security & Privacy

- **sessionStorage isolation**: Data NOT shared across tabs/windows
- **No PII in cache**: Only entity metadata (names, types, IDs)
- **Auto-cleanup**: Cache cleared on tab close (no persistent local storage)
- **Authorization**: Backend validates OwnerId on all API calls

---

## Migration Notes

If migrating from localStorage to sessionStorage or changing cache structure:

1. **Breaking change**: Old cache keys incompatible (different structure)
2. **No migration script needed**: sessionStorage clears on tab close
3. **User impact**: First load after deploy will be cache miss (acceptable)

---

**Version**: 1.0.0  
**Last Updated**: 2026-01-13  
**Status**: Ready for Implementation
