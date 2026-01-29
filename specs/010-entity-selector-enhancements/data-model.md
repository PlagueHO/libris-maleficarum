# Data Model: Entity Type Selector Enhancements

**Date**: 2026-01-29  
**Feature**: Entity Type Selector Enhancements  
**Objective**: Document entity type metadata structure and confirm no database schema changes required.

---

## Overview

The EntityTypeSelector component uses **client-side entity type metadata** from the Entity Type Registry. No database schema changes or API modifications are required for this enhancement.

## Entity Type Registry

**Location**: `libris-maleficarum-app/src/services/config/entityTypeRegistry.ts`

**Purpose**: Central configuration for all world entity types including visual presentation (icons, labels, descriptions) and behavioral rules (suggested children, schema versions).

### Registry Schema

```typescript
interface EntityTypeConfig {
  type: string;                    // Entity type identifier (e.g., "Continent", "Character")
  label: string;                   // Display name (e.g., "Continent")
  description: string;             // User-facing description
  icon: string;                    // Lucide React icon name (e.g., "Mountain", "User")
  category: string;                // Category for grouping (e.g., "Geography", "Characters & Factions")
  suggestedChildren: string[];     // Array of recommended child types
  schemaVersion: number;           // Current schema version for this entity type
}

export const ENTITY_TYPE_REGISTRY: ReadonlyArray<EntityTypeConfig> = [
  // ... all entity type configurations
];
```

### Example Registry Entry

```typescript
{
  type: 'Continent',
  label: 'Continent',
  description: 'A large landmass',
  icon: 'Mountain',  // ← Used by EntityTypeSelector for icon rendering
  category: 'Geography',
  suggestedChildren: ['Country', 'Region', 'Ocean'],
  schemaVersion: 1
}
```

## Icon Property

### Current State

✅ **Icon property already exists** in entity type registry  
✅ **All entity types have icon values** (Lucide React icon names)  
✅ **No schema migration needed** - this is client-side configuration  

### Icon Mapping

Icons are stored as **string identifiers** matching Lucide React component names:

| Entity Type | Icon String | Renders As |
|-------------|-------------|------------|
| Continent | "Mountain" | `<Mountain />` |
| Character | "User" | `<User />` |
| Settlement | "Home" | `<Home />` |
| Campaign | "Scroll" | `<Scroll />` |
| Quest | "Target" | `<Target />` |

### Icon Rendering Strategy

```typescript
// 1. Get icon name from metadata
const meta = getEntityTypeMeta(entityType);
const iconName = meta.icon;  // e.g., "Mountain"

// 2. Dynamic import from Lucide React
import * as Icons from 'lucide-react';
const IconComponent = Icons[iconName as keyof typeof Icons] as React.ComponentType<{className?: string}>;

// 3. Render with size and accessibility attributes
<IconComponent className="w-4 h-4" aria-hidden="true" />
```

## Type Definitions

**Location**: `libris-maleficarum-app/src/services/types/worldEntity.types.ts`

**Relevant Types**:

```typescript
/**
 * Entity type literal union (derived from registry)
 */
export type WorldEntityType = 
  | "World"
  | "Folder"
  | "Locations"
  | "Continent"
  | "Country"
  | ... // all entity types

/**
 * Entity type metadata including icon information
 */
export const ENTITY_TYPE_META: Record<
  WorldEntityType,
  {
    label: string;
    description: string;
    category: string;
    icon: string;  // ← Icon name for rendering
  }
>;

/**
 * Get metadata for an entity type (includes icon)
 */
export function getEntityTypeMeta(type: WorldEntityType): {
  label: string;
  description: string;
  category: string;
  icon: string;
};
```

### Usage in Component

```typescript
// EntityTypeSelector.tsx
const meta = getEntityTypeMeta(WorldEntityType.Continent);
// meta = {
//   label: "Continent",
//   description: "A large landmass",
//   category: "Geography",
//   icon: "Mountain"
// }
```

## Data Flow

```text
┌─────────────────────────────────────┐
│   ENTITY_TYPE_REGISTRY              │  Static configuration
│   (entityTypeRegistry.ts)           │  (no API dependency)
└──────────┬──────────────────────────┘
           │
           ▼
┌─────────────────────────────────────┐
│   ENTITY_TYPE_META                  │  Derived types
│   Generated from registry           │  (worldEntity.types.ts)
└──────────┬──────────────────────────┘
           │
           ▼
┌─────────────────────────────────────┐
│   getEntityTypeMeta(type)           │  Helper function
│   Returns: {label, desc, icon}      │
└──────────┬──────────────────────────┘
           │
           ▼
┌─────────────────────────────────────┐
│   EntityTypeSelector Component      │  UI rendering
│   Renders icons + labels            │
└─────────────────────────────────────┘
```

## No Database Changes Required

✅ **Entity Type Registry** - Client-side TypeScript configuration file  
✅ **Icon Metadata** - Already present in registry entries  
✅ **Cosmos DB Schema** - Unchanged (World Entity documents don't store icon data)  
✅ **API Contracts** - Unchanged (EntityTypeSelector is a presentation component)  

## Component State Changes

### Before Enhancement

```tsx
// Component receives entity type as string
<EntityTypeSelector
  value={entityType}
  onValueChange={setEntityType}
  parentType={parentType || null}
/>

// Renders list items with text only
<button>
  <div>{meta.label}</div>
  <div>{meta.description}</div>
</button>
```

### After Enhancement

```tsx
// Component API unchanged (same props)
<EntityTypeSelector
  value={entityType}
  onValueChange={setEntityType}
  parentType={parentType || null}
/>

// Renders list items with icon + text
<button>
  <IconComponent className="w-4 h-4" aria-hidden="true" />
  <div>{meta.label}</div>
  <div>{meta.description}</div>
</button>
```

## Summary

**No data model changes required**. The enhancement uses existing entity type metadata (icon property already in registry). All changes are presentational (UI rendering logic). No API modifications, no database migrations, no schema updates needed.

**Data Dependencies**:

- ✅ Entity Type Registry (existing)
- ✅ Icon property in metadata (existing)
- ✅ Lucide React package (already installed)

**Next Phase**: Create component contracts and quickstart guide (Phase 1 continued)
