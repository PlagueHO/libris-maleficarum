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
  // Geographic types
  Continent: 'Continent',
  Country: 'Country',
  Region: 'Region',
  City: 'City',
  Building: 'Building',
  Room: 'Room',
  Location: 'Location',
  
  // Character & faction types
  Character: 'Character',
  Faction: 'Faction',
  
  // Event & quest types
  Event: 'Event',
  Quest: 'Quest',
  
  // Item types
  Item: 'Item',
  
  // Campaign types
  Campaign: 'Campaign',
  Session: 'Session',
  
  // Container types - organizational top-level categories
  Folder: 'Folder',
  Locations: 'Locations',
  People: 'People',
  Events: 'Events',
  History: 'History',
  Lore: 'Lore',
  Bestiary: 'Bestiary',
  Items: 'Items',
  Adventures: 'Adventures',
  Geographies: 'Geographies',
  
  // Regional types with custom properties
  GeographicRegion: 'GeographicRegion',
  PoliticalRegion: 'PoliticalRegion',
  CulturalRegion: 'CulturalRegion',
  MilitaryRegion: 'MilitaryRegion',
  
  // Other (catch-all)
  Other: 'Other',
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
 * Context-aware entity type suggestions based on parent type
 *
 * Used by EntityDetailForm (in MainPanel) to suggest relevant entity types when creating children
 */
export const ENTITY_TYPE_SUGGESTIONS: Record<
  WorldEntityType,
  WorldEntityType[]
> = {
  [WorldEntityType.Continent]: [
    WorldEntityType.GeographicRegion,
    WorldEntityType.PoliticalRegion,
    WorldEntityType.Country,
    WorldEntityType.Region,
    WorldEntityType.Faction,
    WorldEntityType.Event,
  ],
  [WorldEntityType.Country]: [
    WorldEntityType.Region,
    WorldEntityType.City,
    WorldEntityType.Location,
    WorldEntityType.Faction,
    WorldEntityType.Event,
  ],
  [WorldEntityType.Region]: [
    WorldEntityType.City,
    WorldEntityType.Location,
    WorldEntityType.Character,
    WorldEntityType.Faction,
  ],
  [WorldEntityType.City]: [
    WorldEntityType.Building,
    WorldEntityType.Location,
    WorldEntityType.Character,
    WorldEntityType.Faction,
    WorldEntityType.Event,
  ],
  [WorldEntityType.Building]: [
    WorldEntityType.Room,
    WorldEntityType.Character,
    WorldEntityType.Item,
    WorldEntityType.Event,
  ],
  [WorldEntityType.Room]: [
    WorldEntityType.Character,
    WorldEntityType.Item,
    WorldEntityType.Event,
  ],
  [WorldEntityType.Location]: [
    WorldEntityType.Character,
    WorldEntityType.Item,
    WorldEntityType.Event,
    WorldEntityType.Quest,
  ],
  [WorldEntityType.Character]: [
    WorldEntityType.Item,
    WorldEntityType.Quest,
  ],
  [WorldEntityType.Faction]: [
    WorldEntityType.Character,
    WorldEntityType.Location,
    WorldEntityType.Event,
    WorldEntityType.Quest,
  ],
  [WorldEntityType.Event]: [
    WorldEntityType.Character,
    WorldEntityType.Location,
    WorldEntityType.Item,
  ],
  [WorldEntityType.Item]: [],
  [WorldEntityType.Campaign]: [
    WorldEntityType.Session,
    WorldEntityType.Quest,
    WorldEntityType.Event,
    WorldEntityType.Character,
    WorldEntityType.Location,
    WorldEntityType.Faction,
  ],
  [WorldEntityType.Session]: [
    WorldEntityType.Event,
    WorldEntityType.Location,
  ],
  [WorldEntityType.Quest]: [
    WorldEntityType.Character,
    WorldEntityType.Item,
    WorldEntityType.Location,
    WorldEntityType.Event,
  ],
  // NEW: Container type suggestions
  [WorldEntityType.Folder]: [
    WorldEntityType.Folder, // Folders can nest
    WorldEntityType.Continent,
    WorldEntityType.Country,
    WorldEntityType.Region,
    WorldEntityType.City,
    WorldEntityType.Location,
    WorldEntityType.Character,
    WorldEntityType.Faction,
    WorldEntityType.Event,
    WorldEntityType.Quest,
    WorldEntityType.Campaign,
    WorldEntityType.Item,
  ],
  [WorldEntityType.Locations]: [
    WorldEntityType.Continent,
    WorldEntityType.GeographicRegion,
    WorldEntityType.PoliticalRegion,
    WorldEntityType.Country,
    WorldEntityType.Region,
    WorldEntityType.City,
    WorldEntityType.Location,
    WorldEntityType.Building,
  ],
  [WorldEntityType.People]: [
    WorldEntityType.Character,
    WorldEntityType.Faction,
  ],
  [WorldEntityType.Events]: [
    WorldEntityType.Event,
    WorldEntityType.Quest,
  ],
  [WorldEntityType.History]: [
    WorldEntityType.Event,
  ],
  [WorldEntityType.Lore]: [
    WorldEntityType.Item,
  ],
  [WorldEntityType.Bestiary]: [
    WorldEntityType.Character,
  ],
  [WorldEntityType.Items]: [
    WorldEntityType.Item,
  ],
  [WorldEntityType.Adventures]: [
    WorldEntityType.Campaign,
    WorldEntityType.Session,
    WorldEntityType.Quest,
  ],
  [WorldEntityType.Geographies]: [
    WorldEntityType.Location,
  ],
  // NEW: Regional type suggestions (can nest)
  [WorldEntityType.GeographicRegion]: [
    WorldEntityType.Country,
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
  [WorldEntityType.Other]: [],
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
 * Entity type metadata: description, category, and icon hints
 *
 * Provides human-readable descriptions and categorization for entity types
 * to improve discoverability in the entity type selector
 */
export const ENTITY_TYPE_META: Record<
  WorldEntityType,
  {
    label: string;
    description: string;
    category: 'Geography' | 'Characters & Factions' | 'Events & Quests' | 'Items' | 'Campaigns' | 'Containers' | 'Other';
    icon: string; // Lucide icon name
  }
> = {
  [WorldEntityType.Continent]: {
    label: 'Continent',
    description: 'A large continental landmass',
    category: 'Geography',
    icon: 'Globe',
  },
  [WorldEntityType.Country]: {
    label: 'Country',
    description: 'A nation or political territory',
    category: 'Geography',
    icon: 'Map',
  },
  [WorldEntityType.Region]: {
    label: 'Region',
    description: 'A geographic area within a country',
    category: 'Geography',
    icon: 'MapPin',
  },
  [WorldEntityType.City]: {
    label: 'City',
    description: 'A city, town, or settlement',
    category: 'Geography',
    icon: 'Building',
  },
  [WorldEntityType.Building]: {
    label: 'Building',
    description: 'A building or structure',
    category: 'Geography',
    icon: 'Building',
  },
  [WorldEntityType.Room]: {
    label: 'Room',
    description: 'A room or interior space',
    category: 'Geography',
    icon: 'Home',
  },
  [WorldEntityType.Location]: {
    label: 'Location',
    description: 'A generic location or landmark',
    category: 'Geography',
    icon: 'MapPin',
  },
  [WorldEntityType.Character]: {
    label: 'Character',
    description: 'A person, NPC, or creature',
    category: 'Characters & Factions',
    icon: 'User',
  },
  [WorldEntityType.Faction]: {
    label: 'Faction',
    description: 'An organization, guild, or political group',
    category: 'Characters & Factions',
    icon: 'Users',
  },
  [WorldEntityType.Event]: {
    label: 'Event',
    description: 'A happening, battle, or historical moment',
    category: 'Events & Quests',
    icon: 'Calendar',
  },
  [WorldEntityType.Quest]: {
    label: 'Quest',
    description: 'A mission, objective, or adventure hook',
    category: 'Events & Quests',
    icon: 'Scroll',
  },
  [WorldEntityType.Item]: {
    label: 'Item',
    description: 'An object, artifact, or piece of equipment',
    category: 'Items',
    icon: 'Package',
  },
  [WorldEntityType.Campaign]: {
    label: 'Campaign',
    description: 'A campaign, story arc, or adventure series',
    category: 'Campaigns',
    icon: 'Scroll',
  },
  [WorldEntityType.Session]: {
    label: 'Session',
    description: 'A game session or play session',
    category: 'Campaigns',
    icon: 'Calendar',
  },
  // NEW: Container types
  [WorldEntityType.Folder]: {
    label: 'Folder',
    description: 'General organizational container for any entity types',
    category: 'Containers',
    icon: 'Folder',
  },
  [WorldEntityType.Locations]: {
    label: 'Locations',
    description: 'Container for geographic and spatial entities',
    category: 'Containers',
    icon: 'FolderOpen',
  },
  [WorldEntityType.People]: {
    label: 'People',
    description: 'Container for characters, factions, and organizations',
    category: 'Containers',
    icon: 'Users',
  },
  [WorldEntityType.Events]: {
    label: 'Events',
    description: 'Container for events, quests, and happenings',
    category: 'Containers',
    icon: 'CalendarDays',
  },
  [WorldEntityType.History]: {
    label: 'History',
    description: 'Container for historical events and timelines',
    category: 'Containers',
    icon: 'BookOpen',
  },
  [WorldEntityType.Lore]: {
    label: 'Lore',
    description: 'Container for lore, mythology, and cultural knowledge',
    category: 'Containers',
    icon: 'BookMarked',
  },
  [WorldEntityType.Bestiary]: {
    label: 'Bestiary',
    description: 'Container for creatures and monsters',
    category: 'Containers',
    icon: 'Bug',
  },
  [WorldEntityType.Items]: {
    label: 'Items',
    description: 'Container for items and artifacts',
    category: 'Containers',
    icon: 'Box',
  },
  [WorldEntityType.Adventures]: {
    label: 'Adventures',
    description: 'Container for campaigns and adventures',
    category: 'Containers',
    icon: 'Compass',
  },
  [WorldEntityType.Geographies]: {
    label: 'Geographies',
    description: 'Container for geographic features',
    category: 'Containers',
    icon: 'Mountain',
  },
  // NEW: Regional types with custom properties
  [WorldEntityType.GeographicRegion]: {
    label: 'Geographic Region',
    description: 'Natural geographic area with climate and terrain properties',
    category: 'Geography',
    icon: 'Globe',
  },
  [WorldEntityType.PoliticalRegion]: {
    label: 'Political Region',
    description: 'Political boundary or governance structure',
    category: 'Geography',
    icon: 'Shield',
  },
  [WorldEntityType.CulturalRegion]: {
    label: 'Cultural Region',
    description: 'Cultural sphere with shared language and heritage',
    category: 'Geography',
    icon: 'Users',
  },
  [WorldEntityType.MilitaryRegion]: {
    label: 'Military Region',
    description: 'Military zone or command structure',
    category: 'Geography',
    icon: 'Shield',
  },
  [WorldEntityType.Other]: {
    label: 'Other',
    description: 'Any other entity type',
    category: 'Other',
    icon: 'HelpCircle',
  },
};

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
