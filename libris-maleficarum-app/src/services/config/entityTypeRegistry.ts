/**
 * Entity Type Registry
 *
 * Single source of truth for all entity type metadata.
 * All entity type constants (WorldEntityType, ENTITY_TYPE_META, ENTITY_TYPE_SUGGESTIONS,
 * ENTITY_SCHEMA_VERSIONS) are derived from this registry.
 *
 * @module services/config/entityTypeRegistry
 */

import type { WorldEntityType } from '../types/worldEntity.types';

/**
 * Validation rules for a property field
 */
export interface PropertyFieldValidation {
  /** Field is required (cannot be empty) */
  required?: boolean;

  /** Minimum value (for integer/decimal fields) */
  min?: number;

  /** Maximum value (for integer/decimal fields) */
  max?: number;

  /** Regex pattern (for text/textarea fields) */
  pattern?: string;
}

/**
 * Schema definition for a single custom property field
 */
export interface PropertyFieldSchema {
  /** Unique identifier for this property (used as object key) */
  key: string;

  /** Human-readable label for display */
  label: string;

  /** Field type determines input control and validation */
  type: 'text' | 'textarea' | 'integer' | 'decimal' | 'tagArray';

  /** Optional placeholder text for input fields */
  placeholder?: string;

  /** Optional help text or description */
  description?: string;

  /** Maximum character length (for text/textarea fields) */
  maxLength?: number;

  /** Optional validation rules */
  validation?: PropertyFieldValidation;
}

/**
 * Configuration for a world entity type
 */
export interface EntityTypeConfig {
  /** Unique identifier (matches WorldEntityType values) */
  readonly type: string;

  /** Human-readable display name */
  readonly label: string;

  /** Descriptive text for UI hints */
  readonly description: string;

  /** Category for grouping in selectors */
  readonly category:
    | 'Geography'
    | 'Characters & Factions'
    | 'Events & Quests'
    | 'Items'
    | 'Campaigns'
    | 'Containers'
    | 'Other';

  /** Lucide icon component name */
  readonly icon: string;

  /** Current schema version for properties field */
  readonly schemaVersion: number;

  /** Suggested child entity types (for context-aware UI) */
  readonly suggestedChildren: readonly string[];

  /** Can this type exist without a parent? (default: false) */
  readonly canBeRoot?: boolean;

  /** Optional schema for custom properties (dynamic field rendering) */
  readonly propertySchema?: readonly PropertyFieldSchema[];
}

/**
 * Complete registry of all entity type configurations
 *
 * This is the single source of truth for entity type metadata.
 * All derived constants (WorldEntityType, ENTITY_SCHEMA_VERSIONS, etc.)
 * are generated from this registry to ensure consistency.
 */
export const ENTITY_TYPE_REGISTRY = [
  // ===== Geographic Types (7) =====
  {
    type: 'Continent',
    label: 'Continent',
    description: 'A large continental landmass',
    category: 'Geography',
    icon: 'Globe',
    schemaVersion: 1,
    suggestedChildren: [
      'GeographicRegion',
      'PoliticalRegion',
      'MilitaryRegion',
      'Country',
      'Region',
      'Faction',
      'Character',
      'Event',
    ],
    canBeRoot: true,
  },
  {
    type: 'Country',
    label: 'Country',
    description: 'A nation or political territory',
    category: 'Geography',
    icon: 'Map',
    schemaVersion: 1,
    suggestedChildren: ['Region', 'City', 'Location', 'Faction', 'Event'],
  },
  {
    type: 'Region',
    label: 'Region',
    description: 'A geographic area within a country',
    category: 'Geography',
    icon: 'MapPin',
    schemaVersion: 1,
    suggestedChildren: ['City', 'Location', 'Character', 'Faction'],
  },
  {
    type: 'City',
    label: 'City',
    description: 'A city, town, or settlement',
    category: 'Geography',
    icon: 'Building',
    schemaVersion: 1,
    suggestedChildren: [
      'Building',
      'Location',
      'Character',
      'Faction',
      'Event',
    ],
  },
  {
    type: 'Building',
    label: 'Building',
    description: 'A building or structure',
    category: 'Geography',
    icon: 'Building',
    schemaVersion: 1,
    suggestedChildren: ['Room', 'Character', 'Item', 'Event'],
  },
  {
    type: 'Room',
    label: 'Room',
    description: 'A room or interior space',
    category: 'Geography',
    icon: 'Home',
    schemaVersion: 1,
    suggestedChildren: ['Character', 'Item', 'Event'],
  },
  {
    type: 'Location',
    label: 'Location',
    description: 'A generic location or landmark',
    category: 'Geography',
    icon: 'MapPin',
    schemaVersion: 1,
    suggestedChildren: ['Character', 'Item', 'Event', 'Quest'],
  },

  // ===== Characters & Factions (2) =====
  {
    type: 'Character',
    label: 'Character',
    description: 'A person, NPC, or creature',
    category: 'Characters & Factions',
    icon: 'User',
    schemaVersion: 1,
    suggestedChildren: ['Item', 'Quest'],
  },
  {
    type: 'Faction',
    label: 'Faction',
    description: 'An organization, guild, or political group',
    category: 'Characters & Factions',
    icon: 'Users',
    schemaVersion: 1,
    suggestedChildren: ['Character', 'Location', 'Event', 'Quest'],
  },

  // ===== Events & Quests (2) =====
  {
    type: 'Event',
    label: 'Event',
    description: 'A happening, battle, or historical moment',
    category: 'Events & Quests',
    icon: 'Calendar',
    schemaVersion: 1,
    suggestedChildren: ['Character', 'Location', 'Item'],
  },
  {
    type: 'Quest',
    label: 'Quest',
    description: 'A mission, objective, or adventure hook',
    category: 'Events & Quests',
    icon: 'Scroll',
    schemaVersion: 1,
    suggestedChildren: ['Character', 'Item', 'Location', 'Event'],
  },

  // ===== Items (1) =====
  {
    type: 'Item',
    label: 'Item',
    description: 'An object, artifact, or piece of equipment',
    category: 'Items',
    icon: 'Package',
    schemaVersion: 1,
    suggestedChildren: [],
  },

  // ===== Campaigns (2) =====
  {
    type: 'Campaign',
    label: 'Campaign',
    description: 'A campaign, story arc, or adventure series',
    category: 'Campaigns',
    icon: 'Scroll',
    schemaVersion: 1,
    suggestedChildren: [
      'Session',
      'Quest',
      'Event',
      'Character',
      'Location',
      'Faction',
    ],
    canBeRoot: true,
  },
  {
    type: 'Session',
    label: 'Session',
    description: 'A game session or play session',
    category: 'Campaigns',
    icon: 'Calendar',
    schemaVersion: 1,
    suggestedChildren: ['Event', 'Location'],
  },

  // ===== Container Types (10) =====
  {
    type: 'Folder',
    label: 'Folder',
    description: 'General organizational container for any entity types',
    category: 'Containers',
    icon: 'Folder',
    schemaVersion: 1,
    suggestedChildren: [
      'Folder',
      'Continent',
      'Country',
      'Region',
      'City',
      'Location',
      'Character',
      'Faction',
      'Event',
      'Quest',
      'Campaign',
      'Item',
    ],
    canBeRoot: true,
  },
  {
    type: 'Locations',
    label: 'Locations',
    description: 'Container for geographic and spatial entities',
    category: 'Containers',
    icon: 'FolderOpen',
    schemaVersion: 1,
    suggestedChildren: [
      'Continent',
      'GeographicRegion',
      'PoliticalRegion',
      'Country',
      'Region',
      'City',
      'Location',
      'Building',
    ],
    canBeRoot: true,
  },
  {
    type: 'People',
    label: 'People',
    description: 'Container for characters, factions, and organizations',
    category: 'Containers',
    icon: 'Users',
    schemaVersion: 1,
    suggestedChildren: ['Character', 'Faction'],
    canBeRoot: true,
  },
  {
    type: 'Events',
    label: 'Events',
    description: 'Container for events, quests, and happenings',
    category: 'Containers',
    icon: 'CalendarDays',
    schemaVersion: 1,
    suggestedChildren: ['Event', 'Quest'],
    canBeRoot: true,
  },
  {
    type: 'History',
    label: 'History',
    description: 'Container for historical events and timelines',
    category: 'Containers',
    icon: 'BookOpen',
    schemaVersion: 1,
    suggestedChildren: ['Event'],
    canBeRoot: true,
  },
  {
    type: 'Lore',
    label: 'Lore',
    description: 'Container for lore, mythology, and cultural knowledge',
    category: 'Containers',
    icon: 'BookMarked',
    schemaVersion: 1,
    suggestedChildren: ['Item'],
    canBeRoot: true,
  },
  {
    type: 'Bestiary',
    label: 'Bestiary',
    description: 'Container for creatures and monsters',
    category: 'Containers',
    icon: 'Bug',
    schemaVersion: 1,
    suggestedChildren: ['Character'],
    canBeRoot: true,
  },
  {
    type: 'Items',
    label: 'Items',
    description: 'Container for items and artifacts',
    category: 'Containers',
    icon: 'Box',
    schemaVersion: 1,
    suggestedChildren: ['Item'],
    canBeRoot: true,
  },
  {
    type: 'Adventures',
    label: 'Adventures',
    description: 'Container for campaigns and adventures',
    category: 'Containers',
    icon: 'Compass',
    schemaVersion: 1,
    suggestedChildren: ['Campaign', 'Session', 'Quest'],
    canBeRoot: true,
  },
  {
    type: 'Geographies',
    label: 'Geographies',
    description: 'Container for geographic features',
    category: 'Containers',
    icon: 'Mountain',
    schemaVersion: 1,
    suggestedChildren: ['Location'],
    canBeRoot: true,
  },

  // ===== Regional Types (4) =====
  {
    type: 'GeographicRegion',
    label: 'Geographic Region',
    description: 'Natural geographic area with climate and terrain properties',
    category: 'Geography',
    icon: 'Globe',
    schemaVersion: 1,
    suggestedChildren: ['Country', 'Region', 'GeographicRegion', 'MilitaryRegion', 'Character'],
    propertySchema: [
      {
        key: 'Climate',
        label: 'Climate',
        type: 'textarea',
        placeholder: 'Describe the climate (e.g., temperate, tropical, arid)...',
        maxLength: 200,
      },
      {
        key: 'Terrain',
        label: 'Terrain',
        type: 'textarea',
        placeholder: 'Describe the terrain (e.g., mountainous, coastal, plains)...',
        maxLength: 200,
      },
      {
        key: 'Population',
        label: 'Population',
        type: 'integer',
        placeholder: 'e.g., 1,000,000',
        description: 'Whole number only',
      },
      {
        key: 'Area',
        label: 'Area (sq km)',
        type: 'decimal',
        placeholder: 'e.g., 150,000.50',
        description: 'Decimal values allowed',
      },
    ],
  },
  {
    type: 'PoliticalRegion',
    label: 'Political Region',
    description: 'Political boundary or governance structure',
    category: 'Geography',
    icon: 'Shield',
    schemaVersion: 1,
    suggestedChildren: ['Country', 'PoliticalRegion'],
    propertySchema: [
      {
        key: 'GovernmentType',
        label: 'Government Type',
        type: 'textarea',
        placeholder: 'e.g., Democracy, Monarchy, Federation, Empire...',
        maxLength: 200,
      },
      {
        key: 'MemberStates',
        label: 'Member States',
        type: 'tagArray',
        placeholder: 'Add a member state...',
        description: 'States, provinces, or territories within this political region',
        maxLength: 50,
      },
      {
        key: 'EstablishedDate',
        label: 'Established Date',
        type: 'text',
        placeholder: 'e.g., Year 1456, 3rd Age, Spring 2024...',
        description: 'Free-form date (supports fantasy calendars)',
        maxLength: 100,
      },
    ],
  },
  {
    type: 'CulturalRegion',
    label: 'Cultural Region',
    description: 'Cultural sphere with shared language and heritage',
    category: 'Geography',
    icon: 'Users',
    schemaVersion: 1,
    suggestedChildren: ['Country', 'CulturalRegion'],
    propertySchema: [
      {
        key: 'Languages',
        label: 'Languages',
        type: 'tagArray',
        placeholder: 'Add a language...',
        description: 'Spoken or written languages in this cultural region',
        maxLength: 50,
      },
      {
        key: 'Religions',
        label: 'Religions',
        type: 'tagArray',
        placeholder: 'Add a religion...',
        description: 'Religious beliefs and practices in this region',
        maxLength: 50,
      },
      {
        key: 'CulturalTraits',
        label: 'Cultural Traits',
        type: 'textarea',
        placeholder: 'Describe unique cultural characteristics, customs, traditions, arts...',
        maxLength: 500,
      },
    ],
  },
  {
    type: 'MilitaryRegion',
    label: 'Military Region',
    description: 'Military zone or command structure',
    category: 'Geography',
    icon: 'Shield',
    schemaVersion: 1,
    suggestedChildren: ['MilitaryRegion'],
    propertySchema: [
      {
        key: 'CommandStructure',
        label: 'Command Structure',
        type: 'textarea',
        placeholder: 'Describe the military hierarchy and leadership (e.g., General → Colonel → Captain)...',
        maxLength: 300,
      },
      {
        key: 'StrategicImportance',
        label: 'Strategic Importance',
        type: 'textarea',
        placeholder: 'Explain why this region is strategically important (e.g., border defense, resource control)...',
        maxLength: 300,
      },
      {
        key: 'MilitaryAssets',
        label: 'Military Assets',
        type: 'tagArray',
        placeholder: 'Add a military asset...',
        description: 'Fortifications, bases, units, equipment, or other military resources',
        maxLength: 50,
      },
    ],
  },

  // ===== Other (1) =====
  {
    type: 'Other',
    label: 'Other',
    description: 'Any other entity type',
    category: 'Other',
    icon: 'HelpCircle',
    schemaVersion: 1,
    suggestedChildren: [],
    canBeRoot: true,
  },
] as const;

/**
 * Get full configuration for a specific entity type
 *
 * @param type - The entity type to look up
 * @returns The entity type configuration, or undefined if not found
 *
 * @example
 * ```typescript
 * const config = getEntityTypeConfig(WorldEntityType.Continent);
 * if (config) {
 *   console.log(config.label); // "Continent"
 *   console.log(config.schemaVersion); // 1
 * }
 * ```
 */
export function getEntityTypeConfig(
  type: WorldEntityType,
): EntityTypeConfig | undefined {
  return ENTITY_TYPE_REGISTRY.find((c) => c.type === type);
}

/**
 * Get all entity types that can be created at root level (without a parent)
 *
 * @returns Array of entity types with canBeRoot: true
 *
 * @example
 * ```typescript
 * const rootTypes = getRootEntityTypes();
 * // Returns: ["Continent", "Campaign", "Folder", "Locations", ...]
 * ```
 */
export function getRootEntityTypes(): WorldEntityType[] {
  return (ENTITY_TYPE_REGISTRY as readonly EntityTypeConfig[]).filter((c) => c.canBeRoot === true).map(
    (c) => c.type as WorldEntityType,
  );
}

/**
 * Get the complete entity type registry
 *
 * @returns Readonly array of all entity type configurations
 *
 * @example
 * ```typescript
 * const allTypes = getAllEntityTypes();
 * allTypes.forEach(config => {
 *   console.log(`${config.type}: ${config.label}`);
 * });
 * ```
 */
export function getAllEntityTypes(): readonly EntityTypeConfig[] {
  return ENTITY_TYPE_REGISTRY;
}
