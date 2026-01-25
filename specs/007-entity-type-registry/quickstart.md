# Quickstart Guide: Entity Type Registry

**Feature**: 007-entity-type-registry  
**Audience**: Developers working with entity types  
**Last Updated**: 2026-01-24

## Overview

The Entity Type Registry is the single source of truth for all entity type metadata. Instead of updating multiple files when adding or modifying entity types, you now only need to update one location: `ENTITY_TYPE_REGISTRY` in `src/services/config/entityTypeRegistry.ts`.

## Adding a New Entity Type

### Step 1: Add Registry Entry

Open `libris-maleficarum-app/src/services/config/entityTypeRegistry.ts` and add an entry to the `ENTITY_TYPE_REGISTRY` array:

```typescript
export const ENTITY_TYPE_REGISTRY = [
  // ... existing entries ...
  
  // Your new entity type
  {
    type: 'Kingdom',
    label: 'Kingdom',
    description: 'A kingdom or realm under monarchical rule',
    category: 'Geography',
    icon: 'Crown',
    schemaVersion: 1,
    suggestedChildren: ['Country', 'Region', 'City', 'Faction'],
    canBeRoot: true,
  },
] as const;
```

**That's it!** All derived constants (`WorldEntityType`, `ENTITY_SCHEMA_VERSIONS`, `ENTITY_TYPE_META`, `ENTITY_TYPE_SUGGESTIONS`) automatically include your new type.

### Step 2: Verify TypeScript Compilation

```bash
cd libris-maleficarum-app
pnpm type-check
```

TypeScript will catch any missing references or type errors.

### Step 3: Run Tests

```bash
pnpm test entityTypeRegistry
```

Validation tests will verify:

- ✅ Unique type identifier
- ✅ Schema version >= 1
- ✅ Valid icon name format
- ✅ No circular suggestions (except hierarchical container types)
- ✅ Complete registry coverage

## Using Entity Type Data

### Get Full Configuration

```typescript
import { getEntityTypeConfig } from '@/services/config/entityTypeRegistry';
import { WorldEntityType } from '@/services/types/worldEntity.types
import { WorldEntityType } from '@/services/types/worldEntity.types';

const config = getEntityTypeConfig(WorldEntityType.Continent);
if (config) {
  console.log(config.label);           // "Continent"
  console.log(config.description);     // "A large continental landmass"
  console.log(config.category);        // "Geography"
  console.log(config.icon);            // "Globe"
  console.log(config.schemaVersion);   // 1
  console.log(config.suggestedChildren); // ["GeographicRegion", ...]
  console.log(config.canBeRoot);       // true
}
```

### Get Root Entity Types

```typescript
import { getRootEntityTypes } from '@/services/config/entityTypeRegistry';

const rootTypes = getRootEntityTypes();
// Returns only types with canBeRoot: true
// Example: ["Continent", "Campaign", "Folder", "Locations", ...]
```

### Iterate All Entity Types

```typescript
import { getAllEntityTypes } from '@/services/config/entityTypeRegistry';

const allTypes = getAllEntityTypes();
allTypes.forEach(config => {
  console.log(`${config.type}: ${config.label}`);
});
```

### Backward-Compatible Access

Existing code continues to work without changes:

```typescript
import { 
  WorldEntityType,
  ENTITY_SCHEMA_VERSIONS,
  ENTITY_TYPE_META,
  ENTITY_TYPE_SUGGESTIONS 
} from '@/services/types/worldEntity.types';

// Schema version (now derived from registry)
const version = ENTITY_SCHEMA_VERSIONS[WorldEntityType.Continent]; // 1

// Metadata (now derived from registry)
const meta = ENTITY_TYPE_META[WorldEntityType.Character];
console.log(meta.label);       // "Character"
console.log(meta.description); // "A person, NPC, or creature"
console.log(meta.category);    // "Characters & Factions"
console.log(meta.icon);        // "User"

// Suggestions (now derived from registry)
const suggestions = ENTITY_TYPE_SUGGESTIONS[WorldEntityType.City];
// Returns: ["Building", "Location", "Character", "Faction", "Event"]
```

## Property Guidelines

### `type` (required)

- **Format**: PascalCase (e.g., `Continent`, `GeographicRegion`)
- **Must be unique** across all entries
- **Matches TypeScript identifier** rules

### `label` (required)

- **Format**: Human-readable display name
- **Examples**: "Continent", "Character", "Campaign"
- **Used in**: UI selectors, form labels

### `description` (required)

- **Format**: Short descriptive phrase
- **Examples**: "A large continental landmass", "A person, NPC, or creature"
- **Used in**: Tooltips, help text

### `category` (required)

Choose one of these 7 categories:

1. **`Geography`**: Geographic and spatial entities (Continent, Country, Region, City, Building, Room, Location, GeographicRegion, etc.)
1. **`Characters & Factions`**: People and organizations (Character, Faction)
1. **`Events & Quests`**: Happenings and missions (Event, Quest)
1. **`Items`**: Objects and artifacts (Item)
1. **`Campaigns`**: Game sessions and arcs (Campaign, Session)
1. **`Containers`**: Organizational folders (Folder, Locations, People, Events, etc.)
1. **`Other`**: Miscellaneous catch-all (Other)

**Used in**: Entity type selector grouping

### `icon` (required)

- **Format**: Lucide icon name in PascalCase
- **Examples**: `Globe`, `User`, `Scroll`, `Package`, `Building`
- **Reference**: [Lucide Icons](https://lucide.dev/icons/)
- **Used in**: UI icons throughout the application

### `schemaVersion` (required)

- **Format**: Positive integer (>= 1)
- **Current**: All types use version `1`
- **Purpose**: Enables future schema migrations for entity properties field
- **Increment when**: Changing the structure of the `properties` JSON field for this type

### `suggestedChildren` (required)

- **Format**: Array of entity type identifiers
- **Examples**: `["Country", "Region"]`, `["Item", "Quest"]`, `[]`
- **Purpose**: Context-aware UI suggestions when creating child entities
- **Can be empty**: Use `[]` if no suggestions (e.g., `Item`, `Other`)
- **Used in**: EntityDetailForm dropdown

### `canBeRoot` (optional)

- **Format**: Boolean (default: `false`)
- **Purpose**: Indicates entity type can exist without a parent
- **Examples**:
  - `true`: Continent, Campaign, Folder, Locations, etc.
  - `false` (default): Room, Session, Quest, etc.
- **Used in**: Entity creation validation, root entity type selector

## Common Patterns

### Geographic Hierarchy

```typescript
{
  type: 'Continent',
  // ...
  suggestedChildren: ['GeographicRegion', 'PoliticalRegion', 'Country', 'Region'],
  canBeRoot: true,
}
```

### Character Relationship

```typescript
{
  type: 'Character',
  // ...
  suggestedChildren: ['Item', 'Quest'],
  canBeRoot: false, // Characters usually belong to a location or faction
}
```

### Container Type

```typescript
{
  type: 'Locations',
  label: 'Locations',
  description: 'Container for geographic and spatial entities',
  category: 'Containers',
  icon: 'FolderOpen',
  schemaVersion: 1,
  suggestedChildren: ['Continent', 'Country', 'Region', 'City', 'Location', 'Building'],
  canBeRoot: true, // Containers are always root entities
}
```

### Leaf Entity (No Children)

```typescript
{
  type: 'Item',
  label: 'Item',
  description: 'An object, artifact, or piece of equipment',
  category: 'Items',
  icon: 'Package',
  schemaVersion: 1,
  suggestedChildren: [], // Items typically don't have children
  canBeRoot: false,
}
```

## Testing Your Changes

### Unit Tests

Run validation tests to ensure registry integrity:

```bash
pnpm test entityTypeRegistry
```

**Tests verify**:

- Unique type identifiers (no duplicates)
- Schema versions are >= 1
- Icon names match PascalCase format
- No circular suggestions (type suggesting itself), except for hierarchical container types (Folder, GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion)
- All expected types present

### Integration Tests

Verify derived constants work correctly:

```bash
pnpm test worldEntity.types
```

### Full Test Suite

```bash
pnpm test
```

All existing tests should pass with 100% success rate (backward compatibility).

## Troubleshooting

### TypeScript Error: "Type 'X' is not assignable"

**Cause**: You likely have a typo in `type` or `suggestedChildren`

**Fix**: Verify all identifiers match exactly (PascalCase)

### Test Failure: "Duplicate type identifier"

**Cause**: Two entries have the same `type` value

**Fix**: Ensure each `type` is unique across the registry

### Test Failure: "Invalid icon name format"

**Cause**: Icon name doesn't match PascalCase pattern (e.g., `globe` instead of `Globe`)

**Fix**: Use PascalCase Lucide icon names

### UI Not Showing New Type

**Cause**: Development server may need restart

**Fix**:

```bash
# Stop dev server (Ctrl+C)
pnpm dev
```

## Migration Notes

### Before Refactor (Old Pattern)

Adding a new entity type required updating **4 files**:

1. `worldEntity.types.ts` - Add to `WorldEntityType` const
1. `worldEntity.types.ts` - Add to `ENTITY_TYPE_META`
1. `worldEntity.types.ts` - Add to `ENTITY_TYPE_SUGGESTIONS`
1. `entitySchemaVersions.ts` - Add to `ENTITY_SCHEMA_VERSIONS`

### After Refactor (New Pattern)

Adding a new entity type requires updating **1 file**:

1. `entityTypeRegistry.ts` - Add one entry to `ENTITY_TYPE_REGISTRY`

All four constants are now **automatically derived** from the registry.

## References

- **Data Model**: [data-model.md](./data-model.md)
- **JSON Schema**: [contracts/EntityTypeConfig.schema.json](./contracts/EntityTypeConfig.schema.json)
- **Specification**: [spec.md](./spec.md)
- **Implementation Plan**: [plan.md](./plan.md)

## Questions?

For architectural questions about the registry pattern, see [spec.md](./spec.md) User Stories and [plan.md](./plan.md) Technical Context.
