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
    propertySchema: [
      {
        key: 'Area',
        label: 'Area (sq km)',
        type: 'decimal',
        placeholder: 'e.g., 25,000,000.00',
        description: 'Total landmass area',
      },
      {
        key: 'Population',
        label: 'Population',
        type: 'integer',
        placeholder: 'e.g., 50,000,000',
        description: 'Total population across the continent',
      },
      {
        key: 'ClimateZones',
        label: 'Climate Zones',
        type: 'tagArray',
        placeholder: 'Add a climate zone...',
        description: 'Major climate zones (e.g., Tropical, Temperate, Arctic)',
        maxLength: 50,
      },
      {
        key: 'MajorLandmarks',
        label: 'Major Landmarks',
        type: 'textarea',
        placeholder: 'Notable geographic features, monuments, or legendary locations...',
        maxLength: 500,
      },
      {
        key: 'DominantRaces',
        label: 'Dominant Races',
        type: 'tagArray',
        placeholder: 'Add a race...',
        description: 'Most common ancestries/races',
        maxLength: 50,
      },
    ],
  },
  {
    type: 'Country',
    label: 'Country',
    description: 'A nation or political territory',
    category: 'Geography',
    icon: 'Map',
    schemaVersion: 1,
    suggestedChildren: ['Region', 'City', 'Location', 'Faction', 'Event'],
    propertySchema: [
      {
        key: 'GovernmentType',
        label: 'Government Type',
        type: 'text',
        placeholder: 'e.g., Monarchy, Republic, Theocracy, Empire...',
        maxLength: 100,
      },
      {
        key: 'Capital',
        label: 'Capital City',
        type: 'text',
        placeholder: 'Name of the capital city',
        maxLength: 100,
      },
      {
        key: 'Population',
        label: 'Population',
        type: 'integer',
        placeholder: 'e.g., 5,000,000',
        description: 'Total population',
      },
      {
        key: 'Languages',
        label: 'Languages',
        type: 'tagArray',
        placeholder: 'Add a language...',
        description: 'Official or commonly spoken languages',
        maxLength: 50,
      },
      {
        key: 'Currency',
        label: 'Currency',
        type: 'text',
        placeholder: 'e.g., Gold Pieces, Crown, Dragon...',
        maxLength: 100,
      },
      {
        key: 'MajorCities',
        label: 'Major Cities',
        type: 'tagArray',
        placeholder: 'Add a city...',
        description: 'Important cities and settlements',
        maxLength: 50,
      },
      {
        key: 'Allies',
        label: 'Allied Nations',
        type: 'tagArray',
        placeholder: 'Add an ally...',
        description: 'Nations with formal alliances',
        maxLength: 50,
      },
      {
        key: 'Rivals',
        label: 'Rival Nations',
        type: 'tagArray',
        placeholder: 'Add a rival...',
        description: 'Nations with conflicts or tensions',
        maxLength: 50,
      },
    ],
  },
  {
    type: 'Region',
    label: 'Region',
    description: 'A geographic area within a country',
    category: 'Geography',
    icon: 'MapPin',
    schemaVersion: 1,
    suggestedChildren: ['City', 'Location', 'Character', 'Faction'],
    propertySchema: [
      {
        key: 'Climate',
        label: 'Climate',
        type: 'text',
        placeholder: 'e.g., Temperate, Tropical, Arid, Cold...',
        maxLength: 100,
      },
      {
        key: 'Terrain',
        label: 'Terrain',
        type: 'textarea',
        placeholder: 'Describe the terrain (e.g., mountainous, forested, coastal, plains)...',
        maxLength: 300,
      },
      {
        key: 'Population',
        label: 'Population',
        type: 'integer',
        placeholder: 'e.g., 100,000',
        description: 'Total population in this region',
      },
      {
        key: 'NotableLocations',
        label: 'Notable Locations',
        type: 'tagArray',
        placeholder: 'Add a location...',
        description: 'Important landmarks, dungeons, or points of interest',
        maxLength: 50,
      },
      {
        key: 'Resources',
        label: 'Natural Resources',
        type: 'tagArray',
        placeholder: 'Add a resource...',
        description: 'Minerals, crops, or other valuable resources',
        maxLength: 50,
      },
      {
        key: 'Dangers',
        label: 'Dangers',
        type: 'tagArray',
        placeholder: 'Add a danger...',
        description: 'Monsters, hazards, or threats in the region',
        maxLength: 50,
      },
    ],
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
    propertySchema: [
      {
        key: 'Population',
        label: 'Population',
        type: 'integer',
        placeholder: 'e.g., 25,000',
        description: 'Number of residents',
      },
      {
        key: 'GovernmentType',
        label: 'Government Type',
        type: 'text',
        placeholder: 'e.g., Mayor, Council, Lord, Guild-run...',
        maxLength: 100,
      },
      {
        key: 'Demographics',
        label: 'Demographics',
        type: 'textarea',
        placeholder: 'Racial makeup and social classes (e.g., 60% Human, 20% Elf, 15% Dwarf, 5% Other)...',
        maxLength: 300,
      },
      {
        key: 'Economy',
        label: 'Economy',
        type: 'textarea',
        placeholder: 'Main industries, trade goods, and economic status...',
        maxLength: 300,
      },
      {
        key: 'Districts',
        label: 'Districts/Quarters',
        type: 'tagArray',
        placeholder: 'Add a district...',
        description: 'Named neighborhoods or districts',
        maxLength: 50,
      },
      {
        key: 'NotableLocations',
        label: 'Notable Locations',
        type: 'tagArray',
        placeholder: 'Add a location...',
        description: 'Important buildings, landmarks, or establishments',
        maxLength: 50,
      },
      {
        key: 'Defenses',
        label: 'Defenses',
        type: 'textarea',
        placeholder: 'Walls, guards, magical wards, or other protective measures...',
        maxLength: 200,
      },
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
    propertySchema: [
      {
        key: 'BuildingType',
        label: 'Building Type',
        type: 'text',
        placeholder: 'e.g., Tavern, Temple, Shop, Manor, Warehouse...',
        maxLength: 100,
      },
      {
        key: 'Owner',
        label: 'Owner',
        type: 'text',
        placeholder: 'Name of the owner or controlling entity',
        maxLength: 100,
      },
      {
        key: 'Condition',
        label: 'Condition',
        type: 'text',
        placeholder: 'e.g., Pristine, Well-maintained, Worn, Ruined...',
        maxLength: 100,
      },
      {
        key: 'Floors',
        label: 'Number of Floors',
        type: 'integer',
        placeholder: 'e.g., 2',
        description: 'Total floors including basement',
      },
      {
        key: 'Purpose',
        label: 'Purpose',
        type: 'textarea',
        placeholder: 'Primary function and activities conducted here...',
        maxLength: 300,
      },
      {
        key: 'NotableFeatures',
        label: 'Notable Features',
        type: 'tagArray',
        placeholder: 'Add a feature...',
        description: 'Architectural details, hidden passages, magical wards, etc.',
        maxLength: 50,
      },
      {
        key: 'Occupants',
        label: 'Occupants',
        type: 'tagArray',
        placeholder: 'Add an occupant...',
        description: 'People or creatures who live or work here',
        maxLength: 50,
      },
    ],
  },
  {
    type: 'Room',
    label: 'Room',
    description: 'A room or interior space',
    category: 'Geography',
    icon: 'Home',
    schemaVersion: 1,
    suggestedChildren: ['Character', 'Item', 'Event'],
    propertySchema: [
      {
        key: 'Purpose',
        label: 'Purpose',
        type: 'text',
        placeholder: 'e.g., Bedroom, Kitchen, Armory, Throne Room...',
        maxLength: 100,
      },
      {
        key: 'Dimensions',
        label: 'Dimensions',
        type: 'text',
        placeholder: 'e.g., 20ft x 30ft, 6m x 9m',
        maxLength: 100,
      },
      {
        key: 'Lighting',
        label: 'Lighting',
        type: 'text',
        placeholder: 'e.g., Well-lit, Dim, Dark, Magical illumination...',
        maxLength: 100,
      },
      {
        key: 'Contents',
        label: 'Contents',
        type: 'textarea',
        placeholder: 'Furniture, decorations, and other items in the room...',
        maxLength: 500,
      },
      {
        key: 'Exits',
        label: 'Exits',
        type: 'tagArray',
        placeholder: 'Add an exit...',
        description: 'Doors, windows, or other ways to leave',
        maxLength: 50,
      },
      {
        key: 'SpecialFeatures',
        label: 'Special Features',
        type: 'tagArray',
        placeholder: 'Add a feature...',
        description: 'Traps, treasures, magical effects, or hidden elements',
        maxLength: 50,
      },
    ],
  },
  {
    type: 'Location',
    label: 'Location',
    description: 'A generic location or landmark',
    category: 'Geography',
    icon: 'MapPin',
    schemaVersion: 1,
    suggestedChildren: ['Character', 'Item', 'Event', 'Quest'],
    propertySchema: [
      {
        key: 'LocationType',
        label: 'Location Type',
        type: 'text',
        placeholder: 'e.g., Dungeon, Forest, Cave, Ruins, Temple, Battlefield...',
        maxLength: 100,
      },
      {
        key: 'Accessibility',
        label: 'Accessibility',
        type: 'text',
        placeholder: 'e.g., Easy to reach, Requires climbing, Hidden, Magically sealed...',
        maxLength: 100,
      },
      {
        key: 'Significance',
        label: 'Significance',
        type: 'textarea',
        placeholder: 'Historical importance, current relevance, or why adventurers would visit...',
        maxLength: 300,
      },
      {
        key: 'Environment',
        label: 'Environment',
        type: 'textarea',
        placeholder: 'Terrain, weather, atmosphere, and physical characteristics...',
        maxLength: 300,
      },
      {
        key: 'Dangers',
        label: 'Dangers',
        type: 'tagArray',
        placeholder: 'Add a danger...',
        description: 'Monsters, traps, hazards, or other threats',
        maxLength: 50,
      },
      {
        key: 'Treasures',
        label: 'Treasures',
        type: 'tagArray',
        placeholder: 'Add a treasure...',
        description: 'Loot, artifacts, or valuable resources',
        maxLength: 50,
      },
      {
        key: 'Inhabitants',
        label: 'Inhabitants',
        type: 'tagArray',
        placeholder: 'Add an inhabitant...',
        description: 'Creatures or NPCs that live here',
        maxLength: 50,
      },
    ],
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
    propertySchema: [
      {
        key: 'Race',
        label: 'Race/Species',
        type: 'text',
        placeholder: 'e.g., Human, Elf, Dwarf, Dragon, Goblin...',
        maxLength: 100,
      },
      {
        key: 'Class',
        label: 'Class/Profession',
        type: 'text',
        placeholder: 'e.g., Fighter, Wizard, Rogue, Merchant, Noble...',
        maxLength: 100,
      },
      {
        key: 'Level',
        label: 'Level/CR',
        type: 'integer',
        placeholder: 'e.g., 5',
        description: 'Character level or Challenge Rating',
      },
      {
        key: 'Alignment',
        label: 'Alignment',
        type: 'text',
        placeholder: 'e.g., Lawful Good, Chaotic Neutral, Neutral Evil...',
        maxLength: 50,
      },
      {
        key: 'Faction',
        label: 'Faction',
        type: 'text',
        placeholder: 'Organization or group affiliation',
        maxLength: 100,
      },
      {
        key: 'PersonalityTraits',
        label: 'Personality Traits',
        type: 'textarea',
        placeholder: 'Key personality characteristics, quirks, or behavior patterns...',
        maxLength: 300,
      },
      {
        key: 'Background',
        label: 'Background',
        type: 'textarea',
        placeholder: 'History, motivations, and important life events...',
        maxLength: 500,
      },
      {
        key: 'AbilitiesAndSkills',
        label: 'Abilities & Skills',
        type: 'tagArray',
        placeholder: 'Add an ability...',
        description: 'Special abilities, spells, or notable skills',
        maxLength: 50,
      },
      {
        key: 'Equipment',
        label: 'Equipment',
        type: 'tagArray',
        placeholder: 'Add an item...',
        description: 'Weapons, armor, and important possessions',
        maxLength: 50,
      },
      {
        key: 'Relationships',
        label: 'Relationships',
        type: 'tagArray',
        placeholder: 'Add a relationship...',
        description: 'Allies, enemies, family, or other connections',
        maxLength: 50,
      },
    ],
  },
  {
    type: 'Faction',
    label: 'Faction',
    description: 'An organization, guild, or political group',
    category: 'Characters & Factions',
    icon: 'Users',
    schemaVersion: 1,
    suggestedChildren: ['Character', 'Location', 'Event', 'Quest'],
    propertySchema: [
      {
        key: 'FactionType',
        label: 'Faction Type',
        type: 'text',
        placeholder: 'e.g., Guild, Religious Order, Political Party, Criminal Organization...',
        maxLength: 100,
      },
      {
        key: 'Alignment',
        label: 'Alignment',
        type: 'text',
        placeholder: 'e.g., Lawful Good, Neutral, Chaotic Evil...',
        maxLength: 50,
      },
      {
        key: 'Goals',
        label: 'Goals',
        type: 'textarea',
        placeholder: 'Primary objectives, ambitions, or mission statement...',
        maxLength: 300,
      },
      {
        key: 'Leadership',
        label: 'Leadership',
        type: 'textarea',
        placeholder: 'Leaders, hierarchy, and decision-making structure...',
        maxLength: 300,
      },
      {
        key: 'Membership',
        label: 'Membership Size',
        type: 'integer',
        placeholder: 'e.g., 500',
        description: 'Approximate number of members',
      },
      {
        key: 'Resources',
        label: 'Resources',
        type: 'tagArray',
        placeholder: 'Add a resource...',
        description: 'Wealth, properties, assets, or capabilities',
        maxLength: 50,
      },
      {
        key: 'Reputation',
        label: 'Reputation',
        type: 'textarea',
        placeholder: 'How this faction is viewed by the public and other organizations...',
        maxLength: 300,
      },
      {
        key: 'Allies',
        label: 'Allied Factions',
        type: 'tagArray',
        placeholder: 'Add an ally...',
        description: 'Friendly or cooperative organizations',
        maxLength: 50,
      },
      {
        key: 'Enemies',
        label: 'Enemy Factions',
        type: 'tagArray',
        placeholder: 'Add an enemy...',
        description: 'Rival or hostile organizations',
        maxLength: 50,
      },
    ],
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
    propertySchema: [
      {
        key: 'EventType',
        label: 'Event Type',
        type: 'text',
        placeholder: 'e.g., Battle, Festival, Assassination, Natural Disaster, Ritual...',
        maxLength: 100,
      },
      {
        key: 'Date',
        label: 'Date',
        type: 'text',
        placeholder: 'e.g., 15th of Mirtul, Year 1492 DR, 3rd Age...',
        description: 'Free-form date (supports fantasy calendars)',
        maxLength: 100,
      },
      {
        key: 'Location',
        label: 'Location',
        type: 'text',
        placeholder: 'Where the event took place',
        maxLength: 100,
      },
      {
        key: 'Participants',
        label: 'Participants',
        type: 'tagArray',
        placeholder: 'Add a participant...',
        description: 'Key individuals, factions, or groups involved',
        maxLength: 50,
      },
      {
        key: 'Outcome',
        label: 'Outcome',
        type: 'textarea',
        placeholder: 'What happened as a result of this event...',
        maxLength: 300,
      },
      {
        key: 'Significance',
        label: 'Significance',
        type: 'textarea',
        placeholder: 'Long-term impact, historical importance, or consequences...',
        maxLength: 300,
      },
      {
        key: 'Casualties',
        label: 'Casualties',
        type: 'integer',
        placeholder: 'e.g., 1000',
        description: 'Number of deaths or losses (if applicable)',
      },
    ],
  },
  {
    type: 'Quest',
    label: 'Quest',
    description: 'A mission, objective, or adventure hook',
    category: 'Events & Quests',
    icon: 'Scroll',
    schemaVersion: 1,
    suggestedChildren: ['Character', 'Item', 'Location', 'Event'],
    propertySchema: [
      {
        key: 'QuestGiver',
        label: 'Quest Giver',
        type: 'text',
        placeholder: 'Name of the NPC or entity providing the quest',
        maxLength: 100,
      },
      {
        key: 'QuestType',
        label: 'Quest Type',
        type: 'text',
        placeholder: 'e.g., Main Quest, Side Quest, Fetch, Rescue, Investigation...',
        maxLength: 100,
      },
      {
        key: 'Difficulty',
        label: 'Difficulty',
        type: 'text',
        placeholder: 'e.g., Easy, Medium, Hard, Deadly',
        maxLength: 50,
      },
      {
        key: 'RecommendedLevel',
        label: 'Recommended Level',
        type: 'integer',
        placeholder: 'e.g., 5',
        description: 'Suggested party level',
      },
      {
        key: 'Objectives',
        label: 'Objectives',
        type: 'textarea',
        placeholder: 'What the party needs to accomplish...',
        maxLength: 500,
      },
      {
        key: 'Rewards',
        label: 'Rewards',
        type: 'tagArray',
        placeholder: 'Add a reward...',
        description: 'Gold, items, experience, reputation, or other benefits',
        maxLength: 50,
      },
      {
        key: 'Status',
        label: 'Status',
        type: 'text',
        placeholder: 'e.g., Not Started, In Progress, Completed, Failed...',
        maxLength: 50,
      },
      {
        key: 'TimeLimit',
        label: 'Time Limit',
        type: 'text',
        placeholder: 'e.g., 3 days, Before the full moon, None...',
        maxLength: 100,
      },
      {
        key: 'Complications',
        label: 'Complications',
        type: 'textarea',
        placeholder: 'Obstacles, twists, or challenges the party might face...',
        maxLength: 300,
      },
    ],
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
    propertySchema: [
      {
        key: 'ItemType',
        label: 'Item Type',
        type: 'text',
        placeholder: 'e.g., Weapon, Armor, Potion, Wondrous Item, Tool...',
        maxLength: 100,
      },
      {
        key: 'Rarity',
        label: 'Rarity',
        type: 'text',
        placeholder: 'e.g., Common, Uncommon, Rare, Very Rare, Legendary, Artifact...',
        maxLength: 50,
      },
      {
        key: 'Value',
        label: 'Value (GP)',
        type: 'integer',
        placeholder: 'e.g., 500',
        description: 'Estimated value in gold pieces',
      },
      {
        key: 'Weight',
        label: 'Weight (lbs)',
        type: 'decimal',
        placeholder: 'e.g., 10.5',
        description: 'Weight in pounds',
      },
      {
        key: 'RequiresAttunement',
        label: 'Requires Attunement',
        type: 'text',
        placeholder: 'e.g., Yes, No, Yes (by a spellcaster)...',
        maxLength: 100,
      },
      {
        key: 'MagicProperties',
        label: 'Magic Properties',
        type: 'textarea',
        placeholder: 'Magical effects, bonuses, or special abilities...',
        maxLength: 500,
      },
      {
        key: 'History',
        label: 'History',
        type: 'textarea',
        placeholder: 'Origin, previous owners, or legendary tales...',
        maxLength: 500,
      },
      {
        key: 'Appearance',
        label: 'Appearance',
        type: 'textarea',
        placeholder: 'Physical description and distinguishing features...',
        maxLength: 300,
      },
    ],
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
    propertySchema: [
      {
        key: 'Setting',
        label: 'Setting',
        type: 'text',
        placeholder: 'e.g., Forgotten Realms, Homebrew World, Eberron...',
        maxLength: 100,
      },
      {
        key: 'GameSystem',
        label: 'Game System',
        type: 'text',
        placeholder: 'e.g., D&D 5e, Pathfinder 2e, Custom Rules...',
        maxLength: 100,
      },
      {
        key: 'PartySize',
        label: 'Party Size',
        type: 'integer',
        placeholder: 'e.g., 4',
        description: 'Number of player characters',
      },
      {
        key: 'StartDate',
        label: 'Start Date',
        type: 'text',
        placeholder: 'e.g., January 2026, Year 1492 DR...',
        maxLength: 100,
      },
      {
        key: 'Theme',
        label: 'Theme',
        type: 'textarea',
        placeholder: 'Main themes, tone, or story focus (e.g., political intrigue, dungeon crawl, epic quest)...',
        maxLength: 300,
      },
      {
        key: 'PlotHooks',
        label: 'Plot Hooks',
        type: 'tagArray',
        placeholder: 'Add a plot hook...',
        description: 'Main storylines or adventure threads',
        maxLength: 50,
      },
      {
        key: 'DMNotes',
        label: 'DM Notes',
        type: 'textarea',
        placeholder: 'Private notes, future plans, or important reminders...',
        maxLength: 1000,
      },
    ],
  },
  {
    type: 'Session',
    label: 'Session',
    description: 'A game session or play session',
    category: 'Campaigns',
    icon: 'Calendar',
    schemaVersion: 1,
    suggestedChildren: ['Event', 'Location'],
    propertySchema: [
      {
        key: 'SessionNumber',
        label: 'Session Number',
        type: 'integer',
        placeholder: 'e.g., 15',
        description: 'Sequential session number',
      },
      {
        key: 'Date',
        label: 'Date',
        type: 'text',
        placeholder: 'e.g., January 28, 2026',
        maxLength: 100,
      },
      {
        key: 'Duration',
        label: 'Duration (hours)',
        type: 'decimal',
        placeholder: 'e.g., 4.5',
        description: 'Session length in hours',
      },
      {
        key: 'Summary',
        label: 'Summary',
        type: 'textarea',
        placeholder: 'What happened during this session...',
        maxLength: 1000,
      },
      {
        key: 'Attendance',
        label: 'Attendance',
        type: 'tagArray',
        placeholder: 'Add a player...',
        description: 'Players who attended',
        maxLength: 50,
      },
      {
        key: 'XPAwarded',
        label: 'XP Awarded',
        type: 'integer',
        placeholder: 'e.g., 1500',
        description: 'Experience points awarded',
      },
      {
        key: 'TreasureFound',
        label: 'Treasure Found',
        type: 'tagArray',
        placeholder: 'Add a treasure...',
        description: 'Gold, items, or other rewards obtained',
        maxLength: 50,
      },
      {
        key: 'NextSessionNotes',
        label: 'Next Session Notes',
        type: 'textarea',
        placeholder: 'Plot threads, cliffhangers, or preparation needed for next time...',
        maxLength: 500,
      },
    ],
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
