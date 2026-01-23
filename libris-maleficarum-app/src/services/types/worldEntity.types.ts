/**
 * World Entity Types
 *
 * TypeScript interfaces for hierarchical entities within worlds
 * (continents, countries, characters, campaigns, etc.)
 *
 * All entity type constants are now derived from the Entity Type Registry
 * for consistency and maintainability.
 *
 * @module services/types/worldEntity.types
 */

import { ENTITY_TYPE_REGISTRY } from '../config/entityTypeRegistry';

/**
 * Entity type classification (derived from registry)
 */
export const WorldEntityType = Object.fromEntries(
  ENTITY_TYPE_REGISTRY.map((c) => [c.type, c.type]),
) as Record<string, string>;

export type WorldEntityType =
  (typeof WorldEntityType)[keyof typeof WorldEntityType];

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

  /** Optional custom properties (JSON string) for entity-specific data */
  properties?: string;

  /** Schema version for document compatibility (default: 1) */
  schemaVersion: number;
}

/**
 * API request to create a new WorldEntity
 */
export interface CreateWorldEntityRequest {
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

  /** Optional custom properties (JSON string) for entity-specific data */
  properties?: string;

  /** Schema version for document compatibility (default: current version for entityType) */
  schemaVersion?: number;
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

  /** Optional custom properties (JSON string) for entity-specific data */
  properties?: string;

  /** Schema version for document compatibility (must be >= current version) */
  schemaVersion?: number;
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
  data: WorldEntity[];

  /** Metadata for pagination */
  meta: {
    /** Total count of items in current page */
    count: number;
    /** Cursor for next page */
    nextCursor: string | null;
  };
}

/**
 * API response containing a single WorldEntity
 */
export interface WorldEntityResponse {
  /** The entity data */
  data: WorldEntity;
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
 * Schema versions for entity types (derived from registry)
 *
 * Maps entity types to their current schema version for property validation
 */
export const ENTITY_SCHEMA_VERSIONS: Record<WorldEntityType, number> =
  Object.fromEntries(
    ENTITY_TYPE_REGISTRY.map((c) => [c.type, c.schemaVersion]),
  ) as Record<WorldEntityType, number>;

/**
 * Context-aware entity type suggestions based on parent type (derived from registry)
 *
 * Used by EntityDetailForm (in MainPanel) to suggest relevant entity types when creating children
 */
export const ENTITY_TYPE_SUGGESTIONS: Record<
  WorldEntityType,
  WorldEntityType[]
> = Object.fromEntries(
  ENTITY_TYPE_REGISTRY.map((c) => [
    c.type,
    [...c.suggestedChildren] as WorldEntityType[],
  ]),
) as Record<WorldEntityType, WorldEntityType[]>;

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
    // Root level suggestions (no parent) - prioritize Container types
    return [
      WorldEntityType.Folder,
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

/**
 * Entity type metadata: description, category, and icon hints (derived from registry)
 *
 * Provides human-readable descriptions and categorization for entity types
 * to improve discoverability in the entity type selector
 */
export const ENTITY_TYPE_META: Record<
  WorldEntityType,
  {
    label: string;
    description: string;
    category:
      | 'Geography'
      | 'Characters & Factions'
      | 'Events & Quests'
      | 'Items'
      | 'Campaigns'
      | 'Containers'
      | 'Other';
    icon: string; // Lucide icon name
  }
> = Object.fromEntries(
  ENTITY_TYPE_REGISTRY.map((c) => [
    c.type,
    {
      label: c.label,
      description: c.description,
      category: c.category,
      icon: c.icon,
    },
  ]),
) as Record<
  WorldEntityType,
  {
    label: string;
    description: string;
    category:
      | 'Geography'
      | 'Characters & Factions'
      | 'Events & Quests'
      | 'Items'
      | 'Campaigns'
      | 'Containers'
      | 'Other';
    icon: string;
  }
>;

/**
 * Get metadata for an entity type
 *
 * @param type - The entity type
 * @returns Metadata including label, description, and category
 */
export function getEntityTypeMeta(
  type: WorldEntityType,
): (typeof ENTITY_TYPE_META)[WorldEntityType] {
  return ENTITY_TYPE_META[type];
}
