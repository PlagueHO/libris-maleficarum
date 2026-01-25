/**
 * Entity Type Helper Functions
 *
 * Utilities for formatting and displaying entity types.
 *
 * @module lib/entityTypeHelpers
 */

import { WorldEntityType } from '@/services/types/worldEntity.types';

/**
 * Formats an entity type for display by converting PascalCase to Title Case with spaces.
 *
 * @param entityType - The entity type to format (e.g., "GeographicRegion")
 * @returns Formatted entity type (e.g., "Geographic Region")
 *
 * @example
 * ```ts
 * formatEntityType(WorldEntityType.GeographicRegion) // "Geographic Region"
 * formatEntityType(WorldEntityType.Character) // "Character"
 * ```
 */
export function formatEntityType(entityType: WorldEntityType | string): string {
  // Convert PascalCase to Title Case with spaces
  // e.g., "GeographicRegion" → "Geographic Region"
  return entityType.replace(/([A-Z])/g, ' $1').trim();
}
