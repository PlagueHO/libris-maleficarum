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
import { ENTITY_TYPE_REGISTRY as GENERATED_ENTITY_TYPE_REGISTRY } from './entityTypeRegistry.generated';

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
  type: 'text' | 'textarea' | 'integer' | 'decimal' | 'tagArray' | 'date' | 'datetime' | 'time';

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
export const ENTITY_TYPE_REGISTRY =
  GENERATED_ENTITY_TYPE_REGISTRY satisfies readonly EntityTypeConfig[];

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
