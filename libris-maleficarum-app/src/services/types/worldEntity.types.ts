/**
 * World Entity Types
 *
 * TypeScript interfaces for hierarchical entities within worlds
 * (continents, countries, characters, campaigns, etc.)
 *
 * @module services/types/worldEntity.types
 */

/**
 * Entity type classification
 */
export const WorldEntityType = {
  Continent: 'Continent',
  Country: 'Country',
  Region: 'Region',
  City: 'City',
  Location: 'Location',
  Character: 'Character',
  Organization: 'Organization',
  Event: 'Event',
  Item: 'Item',
  Campaign: 'Campaign',
} as const;

export type WorldEntityType = (typeof WorldEntityType)[keyof typeof WorldEntityType];

/**
 * Hierarchical entity within a World
 *
 * Stored in Azure Cosmos DB WorldEntity container with hierarchical partition key
 */
export interface WorldEntity {
  /** Unique identifier (GUID) */
  id: string;

  /** Parent world reference (partition key part 1) */
  worldId: string;

  /** Parent entity reference (null for root entities) */
  parentId: string | null;

  /** Entity classification */
  entityType: WorldEntityType;

  /** Display name (1-100 characters) */
  name: string;

  /** Optional description (max 500 characters) */
  description?: string;

  /** User-defined tags for filtering/search */
  tags: string[];

  /** Array of ancestor IDs from root to parent (for hierarchy queries) */
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
 * API request to create a new WorldEntity
 */
export interface CreateWorldEntityRequest {
  /** Parent world ID (required) */
  worldId: string;

  /** Parent entity ID (null for root entities) */
  parentId: string | null;

  /** Entity type */
  entityType: WorldEntityType;

  /** Display name (1-100 characters, required) */
  name: string;

  /** Optional description (max 500 characters) */
  description?: string;

  /** Optional tags */
  tags?: string[];
}

/**
 * API request to update an existing WorldEntity
 */
export interface UpdateWorldEntityRequest {
  /** Updated name (1-100 characters) */
  name?: string;

  /** Updated description (max 500 characters) */
  description?: string;

  /** Updated tags (replaces all existing tags) */
  tags?: string[];

  /** Updated entity type */
  entityType?: WorldEntityType;
}

/**
 * API request to move an entity to a new parent
 */
export interface MoveWorldEntityRequest {
  /** New parent entity ID (null to move to root) */
  newParentId: string | null;
}

/**
 * API response containing a list of WorldEntity items
 */
export interface WorldEntityListResponse {
  /** Array of entities */
  items: WorldEntity[];

  /** Total count of entities matching the query (before pagination) */
  totalCount: number;

  /** Current page number (1-indexed) */
  page: number;

  /** Page size */
  pageSize: number;

  /** True if more pages available */
  hasMore: boolean;
}

/**
 * API response containing a single WorldEntity
 */
export interface WorldEntityResponse {
  /** The entity */
  entity: WorldEntity;
}

/**
 * Query parameters for fetching WorldEntity list
 */
export interface GetWorldEntitiesQueryParams {
  /** World ID (required) */
  worldId: string;

  /** Filter by parent ID (null for root entities, omit to get all) */
  parentId?: string | null;

  /** Filter by entity type */
  entityType?: WorldEntityType;

  /** Filter by tags (comma-separated) */
  tags?: string;

  /** Page number (1-indexed, default: 1) */
  page?: number;

  /** Page size (default: 50, max: 100) */
  pageSize?: number;

  /** Include soft-deleted entities (default: false) */
  includeDeleted?: boolean;
}

/**
 * Context-aware entity type suggestions based on parent type
 *
 * Used by EntityFormModal to suggest relevant entity types when creating children
 */
export const ENTITY_TYPE_SUGGESTIONS: Record<
  WorldEntityType,
  WorldEntityType[]
> = {
  [WorldEntityType.Continent]: [
    WorldEntityType.Country,
    WorldEntityType.Region,
  ],
  [WorldEntityType.Country]: [
    WorldEntityType.Region,
    WorldEntityType.City,
    WorldEntityType.Location,
  ],
  [WorldEntityType.Region]: [
    WorldEntityType.City,
    WorldEntityType.Location,
    WorldEntityType.Character,
  ],
  [WorldEntityType.City]: [
    WorldEntityType.Location,
    WorldEntityType.Character,
    WorldEntityType.Organization,
  ],
  [WorldEntityType.Location]: [
    WorldEntityType.Character,
    WorldEntityType.Item,
    WorldEntityType.Event,
  ],
  [WorldEntityType.Character]: [
    WorldEntityType.Item,
    WorldEntityType.Event,
  ],
  [WorldEntityType.Organization]: [
    WorldEntityType.Character,
    WorldEntityType.Location,
    WorldEntityType.Event,
  ],
  [WorldEntityType.Event]: [
    WorldEntityType.Character,
    WorldEntityType.Location,
  ],
  [WorldEntityType.Item]: [],
  [WorldEntityType.Campaign]: [
    WorldEntityType.Event,
    WorldEntityType.Character,
    WorldEntityType.Location,
  ],
};

/**
 * Get suggested entity types for a given parent entity type
 *
 * @param parentType - The parent entity's type
 * @returns Array of suggested child entity types
 */
export function getEntityTypeSuggestions(
  parentType: WorldEntityType | null,
): WorldEntityType[] {
  if (!parentType) {
    // Root level suggestions (no parent)
    return [WorldEntityType.Continent, WorldEntityType.Campaign];
  }

  return ENTITY_TYPE_SUGGESTIONS[parentType] ?? [];
}
