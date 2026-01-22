/**
 * World Entity Types
 *
 * TypeScript interfaces for World entities and related API request/response models.
 * These types provide compile-time safety for all World-related API interactions.
 */

/**
 * World Entity
 *
 * Root entity representing a fictional world in the system.
 * Matches the backend World entity schema.
 */
export interface World {
  /**
   * Unique identifier (GUID format)
   */
  id: string;

  /**
   * World name (unique per owner)
   */
  name: string;

  /**
   * Optional detailed description of the world
   */
  description?: string;

  /**
   * Owner user ID (matched against authenticated user)
   */
  ownerId: string;

  /**
   * UTC timestamp when the world was created
   */
  createdAt: string;

  /**
   * UTC timestamp when the world was last updated
   */
  updatedAt: string;

  /**
   * Soft delete flag (true if world is deleted)
   */
  isDeleted: boolean;
}

/**
 * Create World Request
 *
 * Payload for POST /api/worlds
 */
export interface CreateWorldRequest {
  /**
   * World name (1-100 characters, required)
   */
  name: string;

  /**
   * World description (optional, max 2000 characters)
   */
  description?: string;
}

/**
 * Update World Request
 *
 * Payload for PUT /api/worlds/{id}
 */
export interface UpdateWorldRequest {
  /**
   * Updated world name (1-100 characters, required)
   */
  name: string;

  /**
   * Updated world description (optional, max 2000 characters)
   */
  description?: string;
}

/**
 * World Response
 *
 * Single world response from GET /api/worlds/{id} or POST/PUT /api/worlds
 */
export interface WorldResponse {
  /**
   * World data
   */
  data: World;

  /**
   * Response metadata
   */
  meta: {
    /**
     * Unique request identifier for tracing
     */
    requestId: string;

    /**
     * Response timestamp (ISO 8601 format)
     */
    timestamp: string;
  };
}

/**
 * World List Response
 *
 * Paginated list of worlds from GET /api/worlds
 */
export interface WorldListResponse {
  /**
   * Array of worlds
   */
  data: World[];

  /**
   * Pagination metadata
   */
  pagination?: {
    /**
     * Number of items per page
     */
    limit: number;

    /**
     * Whether more items exist
     */
    hasMore: boolean;

    /**
     * Cursor for next page (opaque string)
     */
    nextCursor?: string;
  };

  /**
   * Response metadata
   */
  meta: {
    /**
     * Unique request identifier for tracing
     */
    requestId: string;

    /**
     * Response timestamp (ISO 8601 format)
     */
    timestamp: string;
  };
}

/**
 * World Entity Type Enum
 *
 * Hierarchical entity types that can exist within a world.
 * Used for filtering and type-specific operations.
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

export type WorldEntityType = typeof WorldEntityType[keyof typeof WorldEntityType];

/**
 * Base World Entity (for future hierarchical entities)
 *
 * Base structure for all entities within a world (continents, characters, etc.).
 * Currently out of scope per YAGNI, but defined for type completeness.
 */
export interface WorldEntity {
  /**
   * Unique identifier (GUID format)
   */
  id: string;

  /**
   * World this entity belongs to
   */
  worldId: string;

  /**
   * Entity type discriminator
   */
  entityType: WorldEntityType;

  /**
   * Entity name
   */
  name: string;

  /**
   * Optional description
   */
  description?: string;

  /**
   * Parent entity ID (null for top-level entities)
   */
  parentId?: string;

  /**
   * UTC timestamp when created
   */
  createdAt: string;

  /**
   * UTC timestamp when last updated
   */
  updatedAt: string;

  /**
   * Soft delete flag
   */
  isDeleted: boolean;

  /**
   * Schema version for document compatibility
   */
  schemaVersion: number;
}
