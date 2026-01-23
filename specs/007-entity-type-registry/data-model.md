# Data Model: Entity Type Registry

**Feature**: 007-entity-type-registry  
**Created**: 2026-01-24  
**Status**: Active

## Overview

The Entity Type Registry is the single source of truth for all entity type metadata in Libris Maleficarum. It consolidates previously scattered definitions (WorldEntityType, ENTITY_TYPE_META, ENTITY_TYPE_SUGGESTIONS, ENTITY_SCHEMA_VERSIONS) into a unified data structure.

## Core Entities

### EntityTypeConfig

The fundamental configuration object for each entity type.

**TypeScript Interface**:

```typescript
interface EntityTypeConfig {
  /** Unique identifier (matches WorldEntityType values) */
  type: string;
  
  /** Human-readable display name */
  label: string;
  
  /** Descriptive text for UI hints */
  description: string;
  
  /** Category for grouping in selectors */
  category: 'Geography' | 'Characters & Factions' | 'Events & Quests' | 'Items' | 'Campaigns' | 'Containers' | 'Other';
  
  /** Lucide icon component name */
  icon: string;
  
  /** Current schema version for properties field */
  schemaVersion: number;
  
  /** Suggested child entity types (for context-aware UI) */
  suggestedChildren: string[];
  
  /** Can this type exist without a parent? (default: false) */
  canBeRoot?: boolean;
}
```

**Properties**:

| Property | Type | Required | Description | Constraints |
|----------|------|----------|-------------|-------------|
| `type` | `string` | Yes | Unique identifier matching WorldEntityType | PascalCase, must be unique |
| `label` | `string` | Yes | Human-readable display name | Min length: 1 |
| `description` | `string` | Yes | Descriptive text for UI hints | Min length: 1 |
| `category` | `string` | Yes | Category for grouping | One of 7 allowed values |
| `icon` | `string` | Yes | Lucide icon component name | PascalCase, valid icon name |
| `schemaVersion` | `number` | Yes | Current schema version | Integer >= 1 |
| `suggestedChildren` | `string[]` | Yes | Suggested child types | Array of type identifiers |
| `canBeRoot` | `boolean` | No | Can exist without parent | Default: `false` |

**Categories**:

1. **Geography**: Geographic and spatial entities (Continent, Country, Region, City, Building, Room, Location, GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion)
1. **Characters & Factions**: People and organizations (Character, Faction)
1. **Events & Quests**: Happenings and missions (Event, Quest)
1. **Items**: Objects and artifacts (Item)
1. **Campaigns**: Game sessions and arcs (Campaign, Session)
1. **Containers**: Organizational folders (Folder, Locations, People, Events, History, Lore, Bestiary, Items, Adventures, Geographies)
1. **Other**: Miscellaneous catch-all (Other)

### ENTITY_TYPE_REGISTRY

The complete registry of all entity type configurations.

**TypeScript Type**:

```typescript
const ENTITY_TYPE_REGISTRY: readonly EntityTypeConfig[] = [
  // ... 35 entity type configurations
] as const;
```

**Characteristics**:

- **Readonly**: Immutable at runtime to prevent accidental modifications
- **Complete**: Contains all 35 entity types used in the application
- **Single Source**: All derived constants pull from this registry
- **Const Assertion**: Enables TypeScript type inference for derived types

**Current Count**: 35 entity types

## Derived Types

All existing constants are derived from `ENTITY_TYPE_REGISTRY` to maintain backward compatibility:

### WorldEntityType (const object)

```typescript
export const WorldEntityType = Object.fromEntries(
  ENTITY_TYPE_REGISTRY.map(c => [c.type, c.type])
) as const;

export type WorldEntityType = typeof WorldEntityType[keyof typeof WorldEntityType];
```

**Purpose**: String literal union type with const object pattern (existing pattern maintained)

### ENTITY_SCHEMA_VERSIONS

```typescript
export const ENTITY_SCHEMA_VERSIONS: Record<WorldEntityType, number> = Object.fromEntries(
  ENTITY_TYPE_REGISTRY.map(c => [c.type, c.schemaVersion])
);
```

**Purpose**: Maps entity types to their current schema versions for property validation

### ENTITY_TYPE_META

```typescript
export const ENTITY_TYPE_META: Record<WorldEntityType, EntityTypeMeta> = Object.fromEntries(
  ENTITY_TYPE_REGISTRY.map(c => [
    c.type,
    { label: c.label, description: c.description, category: c.category, icon: c.icon }
  ])
);
```

**Purpose**: Provides UI metadata (label, description, category, icon) for entity type selectors

### ENTITY_TYPE_SUGGESTIONS

```typescript
export const ENTITY_TYPE_SUGGESTIONS: Record<WorldEntityType, WorldEntityType[]> = Object.fromEntries(
  ENTITY_TYPE_REGISTRY.map(c => [c.type, c.suggestedChildren as WorldEntityType[]])
);
```

**Purpose**: Context-aware suggestions for child entity types based on parent type

## Validation Rules

The registry MUST satisfy these constraints:

1. **Uniqueness**: Each `type` value must be unique across all entries
1. **Schema Version**: `schemaVersion` must be an integer >= 1
1. **Icon Format**: `icon` must be a valid Lucide icon name (PascalCase)
1. **No Circular References**: `suggestedChildren` must not include the same type as `type`
1. **Valid References**: All entries in `suggestedChildren` should reference valid types in the registry
1. **Completeness**: All 35 expected entity types must be present

## Example Registry Entry

```typescript
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
    'Country',
    'Region',
    'Faction',
    'Event'
  ],
  canBeRoot: true
}
```

## Entity Type Categories & Complete List

### Geographic (11 types)

- **Continent**: Large continental landmass (root entity)
- **Country**: Nation or political territory
- **Region**: Geographic area within a country
- **City**: City, town, or settlement
- **Building**: Building or structure
- **Room**: Room or interior space
- **Location**: Generic location or landmark
- **GeographicRegion**: Natural geographic area with climate/terrain
- **PoliticalRegion**: Political boundary or governance structure
- **CulturalRegion**: Cultural sphere with shared language/heritage
- **MilitaryRegion**: Military zone or command structure

### Characters & Factions (2 types)

- **Character**: Person, NPC, or creature
- **Faction**: Organization, guild, or political group

### Events & Quests (2 types)

- **Event**: Happening, battle, or historical moment
- **Quest**: Mission, objective, or adventure hook

### Items (1 type)

- **Item**: Object, artifact, or piece of equipment

### Campaigns (2 types)

- **Campaign**: Campaign, story arc, or adventure series (root entity)
- **Session**: Game session or play session

### Containers (10 types)

- **Folder**: General organizational container (root entity)
- **Locations**: Container for geographic entities (root entity)
- **People**: Container for characters/factions (root entity)
- **Events**: Container for events/quests (root entity)
- **History**: Container for historical events (root entity)
- **Lore**: Container for lore/mythology (root entity)
- **Bestiary**: Container for creatures (root entity)
- **Items**: Container for items/artifacts (root entity)
- **Adventures**: Container for campaigns/adventures (root entity)
- **Geographies**: Container for geographic features (root entity)

### Other (1 type)

- **Other**: Any other entity type (root entity)

**Note**: Container types "Events" and "Items" share names with singular types "Event" and "Item" but serve different purposes (organizational vs. content entities).

## Usage Patterns

### Adding a New Entity Type

1. Add one entry to `ENTITY_TYPE_REGISTRY` with all required properties
1. All derived constants automatically include the new type
1. No other file modifications needed

### Accessing Registry Data

```typescript
// Get full configuration
const config = getEntityTypeConfig(WorldEntityType.Continent);

// Get all root entity types
const rootTypes = getRootEntityTypes();

// Iterate all types
const allTypes = getAllEntityTypes();

// Backward-compatible access
const version = ENTITY_SCHEMA_VERSIONS[WorldEntityType.Continent];
const meta = ENTITY_TYPE_META[WorldEntityType.Character];
const suggestions = ENTITY_TYPE_SUGGESTIONS[WorldEntityType.City];
```

## Future Considerations

### API-Driven Entity Types

The registry structure is designed to support future API-driven entity type management:

```typescript
// Current: Static registry
const ENTITY_TYPE_REGISTRY = [ /* ... */ ] as const;

// Future: API-loaded registry
const ENTITY_TYPE_REGISTRY = await loadEntityTypesFromApi();
```

**JSON Serialization**: All properties use simple types (string, number, boolean, array) suitable for JSON transport and form editing.

**Schema Validation**: The registry structure can be validated against JSON Schema for API responses.

## References

- **Specification**: [spec.md](./spec.md)
- **Implementation Plan**: [plan.md](./plan.md)
- **Tasks**: [tasks.md](./tasks.md)
- **JSON Schema**: [contracts/EntityTypeConfig.schema.json](./contracts/EntityTypeConfig.schema.json)
