/**
 * Current schema versions for each WorldEntity type.
 *
 * These versions indicate the latest property schema definition supported
 * by the frontend. When creating or updating entities, the frontend sends
 * the current schema version to enable automatic schema migration.
 *
 * Update these versions when deploying new entity property schemas.
 */
export const ENTITY_SCHEMA_VERSIONS: Record<string, number> = {
  // Geographic types
  Continent: 1,
  Country: 1,
  Region: 1,
  City: 1,
  Building: 1,
  Room: 1,
  Location: 1,

  // Character & faction types
  Character: 1,
  Faction: 1,

  // Event & quest types
  Event: 1,
  Quest: 1,

  // Item types
  Item: 1,

  // Campaign types
  Campaign: 1,
  Session: 1,

  // Container types
  Folder: 1,
  Locations: 1,
  People: 1,
  Events: 1,
  History: 1,
  Lore: 1,
  Bestiary: 1,
  Items: 1,
  Adventures: 1,
  Geographies: 1,

  // Regional types
  GeographicRegion: 1,
  PoliticalRegion: 1,
  CulturalRegion: 1,
  MilitaryRegion: 1,

  // Other
  Other: 1,
};

/**
 * Get the current schema version for an entity type.
 *
 * @param entityType - The entity type
 * @returns The current schema version (>= 1)
 */
export function getSchemaVersion(entityType: string): number {
  return ENTITY_SCHEMA_VERSIONS[entityType] ?? 1;
}
