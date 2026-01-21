# Data Model: Schema Versioning for WorldEntities

**Feature**: 006-schema-versioning  
**Date**: 2026-01-21  
**Status**: Complete

## Overview

This document defines the data model changes required to support schema versioning in WorldEntity documents. The primary change is adding a `SchemaVersion` integer property to track which version of the entity type's property schema was used when the entity was created or last migrated.

## Entity Model Changes

### WorldEntity (Domain Entity)

**File**: `libris-maleficarum-service/src/Domain/Entities/WorldEntity.cs`

**New Property**:

```csharp
/// <summary>
/// Gets the schema version for this entity type's property structure.
/// Indicates which version of the entity type's schema was used when created/last migrated.
/// Enables forward schema evolution without requiring bulk updates.
/// </summary>
public int SchemaVersion { get; private set; }
```

**Property Characteristics**:
- **Type**: `int` (32-bit signed integer)
- **Range**: >= 1 (validated by domain logic)
- **Default**: `1` (when omitted in requests)
- **Immutability**: Can only increase or stay the same (no downgrades)

**Updated Methods**:

```csharp
// Create method - add schemaVersion parameter with default
public static WorldEntity Create(
    Guid worldId,
    EntityType entityType,
    string name,
    string ownerId,
    string? description = null,
    Guid? parentId = null,
    List<string>? tags = null,
    Dictionary<string, object>? attributes = null,
    List<Guid>? parentPath = null,
    int parentDepth = -1,
    int schemaVersion = 1)  // NEW PARAMETER
{
    // ... existing logic
    
    var entity = new WorldEntity
    {
        // ... existing properties
        SchemaVersion = schemaVersion,  // NEW
    };
    
    entity.Validate();
    return entity;
}

// Update method - add schemaVersion parameter
public void Update(
    string name,
    string? description,
    EntityType entityType,
    Guid? parentId,
    List<string>? tags,
    Dictionary<string, object>? attributes,
    int schemaVersion)  // NEW PARAMETER
{
    // ... existing assignments
    SchemaVersion = schemaVersion;  // NEW
    ModifiedDate = DateTime.UtcNow;
    
    Validate();
}

// Validate method - add schema version validation
public void Validate()
{
    // ... existing validations
    
    if (SchemaVersion < 1)  // NEW
    {
        throw new ArgumentException("Schema version must be at least 1.");
    }
}
```

### WorldEntity Configuration (Infrastructure)

**File**: `libris-maleficarum-service/src/Infrastructure/Data/Configurations/WorldEntityConfiguration.cs`

**Property Mapping**:

```csharp
public void Configure(EntityTypeBuilder<WorldEntity> builder)
{
    // ... existing mappings
    
    builder.Property(e => e.SchemaVersion)
        .ToJsonProperty("schemaVersion")
        .IsRequired();
}
```

**Cosmos DB Document Structure**:

```json
{
  "id": "c3d4e5f6-a7b8-6c7d-0e9f-1a0b9c8d7e6f",
  "worldId": "a1b2c3d4-e5f6-4a5b-8c7d-9e8f7a6b5c4d",
  "entityType": "Character",
  "name": "Gandalf",
  "schemaVersion": 1,
  "createdDate": "2025-01-15T10:30:00Z",
  "modifiedDate": "2025-01-20T14:45:00Z",
  "_rid": "...",
  "_self": "...",
  "_etag": "..."
}
```

## Configuration Model

### EntitySchemaVersionConfig (Backend)

**File**: `libris-maleficarum-service/src/Domain/Configuration/EntitySchemaVersionConfig.cs` (NEW)

```csharp
namespace LibrisMaleficarum.Domain.Configuration;

/// <summary>
/// Configuration for supported schema version ranges per entity type.
/// Used to validate incoming schema versions in API requests.
/// </summary>
public class EntitySchemaVersionConfig
{
    /// <summary>
    /// Gets or sets the schema version ranges per entity type.
    /// </summary>
    public Dictionary<string, SchemaVersionRange> EntityTypes { get; set; } = new();
    
    /// <summary>
    /// Gets the schema version range for a given entity type.
    /// </summary>
    /// <param name="entityType">The entity type name.</param>
    /// <returns>The version range, or { Min: 1, Max: 1 } if not configured.</returns>
    public SchemaVersionRange GetVersionRange(string entityType)
    {
        return EntityTypes.TryGetValue(entityType, out var range) 
            ? range 
            : new SchemaVersionRange { MinVersion = 1, MaxVersion = 1 };
    }
}

/// <summary>
/// Represents the minimum and maximum supported schema versions for an entity type.
/// </summary>
public class SchemaVersionRange
{
    /// <summary>
    /// Gets or sets the minimum supported schema version (inclusive).
    /// </summary>
    public int MinVersion { get; set; } = 1;
    
    /// <summary>
    /// Gets or sets the maximum supported schema version (inclusive).
    /// </summary>
    public int MaxVersion { get; set; } = 1;
}
```

**Configuration File** (`appsettings.json`):

```json
{
  "EntitySchemaVersions": {
    "EntityTypes": {
      "Character": { "MinVersion": 1, "MaxVersion": 1 },
      "Location": { "MinVersion": 1, "MaxVersion": 1 },
      "Campaign": { "MinVersion": 1, "MaxVersion": 1 },
      "GeographicRegion": { "MinVersion": 1, "MaxVersion": 1 }
    }
  }
}
```

### ENTITY_SCHEMA_VERSIONS (Frontend)

**File**: `libris-maleficarum-app/src/services/constants/entitySchemaVersions.ts` (NEW)

```typescript
import { WorldEntityType } from '../types/worldEntity.types';

/**
 * Current schema versions for each WorldEntity type.
 * 
 * These versions indicate the latest property schema definition supported
 * by the frontend. When creating or updating entities, the frontend sends
 * the current schema version to enable automatic schema migration.
 * 
 * Update these versions when deploying new entity property schemas.
 */
export const ENTITY_SCHEMA_VERSIONS: Record<WorldEntityType, number> = {
  // Geographic types
  [WorldEntityType.Continent]: 1,
  [WorldEntityType.Country]: 1,
  [WorldEntityType.Region]: 1,
  [WorldEntityType.City]: 1,
  [WorldEntityType.Building]: 1,
  [WorldEntityType.Room]: 1,
  [WorldEntityType.Location]: 1,
  
  // Character & faction types
  [WorldEntityType.Character]: 1,
  [WorldEntityType.Faction]: 1,
  
  // Event & quest types
  [WorldEntityType.Event]: 1,
  [WorldEntityType.Quest]: 1,
  
  // Item types
  [WorldEntityType.Item]: 1,
  
  // Campaign types
  [WorldEntityType.Campaign]: 1,
  [WorldEntityType.Session]: 1,
  
  // Container types
  [WorldEntityType.Folder]: 1,
  [WorldEntityType.Locations]: 1,
  [WorldEntityType.People]: 1,
  [WorldEntityType.Events]: 1,
  [WorldEntityType.History]: 1,
  [WorldEntityType.Lore]: 1,
  [WorldEntityType.Bestiary]: 1,
  [WorldEntityType.Items]: 1,
  [WorldEntityType.Adventures]: 1,
  [WorldEntityType.Geographies]: 1,
  
  // Regional types
  [WorldEntityType.GeographicRegion]: 1,
  [WorldEntityType.PoliticalRegion]: 1,
  [WorldEntityType.CulturalRegion]: 1,
  [WorldEntityType.MilitaryRegion]: 1,
  
  // Other
  [WorldEntityType.Other]: 1,
};

/**
 * Get the current schema version for an entity type.
 * 
 * @param entityType - The entity type
 * @returns The current schema version (>= 1)
 */
export function getSchemaVersion(entityType: WorldEntityType): number {
  return ENTITY_SCHEMA_VERSIONS[entityType];
}
```

## TypeScript Interface Changes

### WorldEntity Interface

**File**: `libris-maleficarum-app/src/services/types/worldEntity.types.ts`

**Updated Interface**:

```typescript
export interface WorldEntity {
  id: string;
  worldId: string;
  parentId: string | null;
  entityType: WorldEntityType;
  name: string;
  description?: string;
  tags: string[];
  path: string[];
  depth: number;
  hasChildren: boolean;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
  properties?: string;
  schemaVersion: number;  // NEW: Schema version (>= 1)
}
```

### Request DTOs

**CreateWorldEntityRequest**:

```typescript
export interface CreateWorldEntityRequest {
  parentId: string | null;
  entityType: WorldEntityType;
  name: string;
  description?: string;
  tags?: string[];
  properties?: string;
  schemaVersion?: number;  // NEW: Optional (backend defaults to 1 if omitted)
}
```

**UpdateWorldEntityRequest**:

```typescript
export interface UpdateWorldEntityRequest {
  name?: string;
  description?: string;
  tags?: string[];
  entityType?: WorldEntityType;
  properties?: string;
  schemaVersion?: number;  // NEW: Optional (uses current version from constants if omitted)
}
```

## Validation Rules

### Backend Validation

**SchemaVersionValidator** (NEW):

```csharp
public class SchemaVersionValidator
{
    private readonly EntitySchemaVersionConfig _config;
    
    public void ValidateCreate(string entityType, int? requestedVersion)
    {
        var version = requestedVersion ?? 1;
        var range = _config.GetVersionRange(entityType);
        
        if (version < 1)
            throw new SchemaVersionException("SCHEMA_VERSION_INVALID", 
                "Schema version must be a positive integer");
        
        if (version < range.MinVersion)
            throw new SchemaVersionException("SCHEMA_VERSION_TOO_LOW", 
                $"Schema version {version} is below minimum supported version {range.MinVersion}");
        
        if (version > range.MaxVersion)
            throw new SchemaVersionException("SCHEMA_VERSION_TOO_HIGH", 
                $"Schema version {version} exceeds maximum supported version {range.MaxVersion}");
    }
    
    public void ValidateUpdate(string entityType, int currentVersion, int requestedVersion)
    {
        var range = _config.GetVersionRange(entityType);
        
        if (requestedVersion < currentVersion)
            throw new SchemaVersionException("SCHEMA_DOWNGRADE_NOT_ALLOWED", 
                $"Cannot downgrade entity from schema version {currentVersion} to {requestedVersion}");
        
        if (requestedVersion > range.MaxVersion)
            throw new SchemaVersionException("SCHEMA_VERSION_TOO_HIGH", 
                $"Schema version {requestedVersion} exceeds maximum supported version {range.MaxVersion}");
    }
}
```

### Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `SCHEMA_VERSION_INVALID` | 400 | Version is not a positive integer |
| `SCHEMA_VERSION_TOO_LOW` | 400 | Version below minimum supported |
| `SCHEMA_VERSION_TOO_HIGH` | 400 | Version above maximum supported |
| `SCHEMA_DOWNGRADE_NOT_ALLOWED` | 400 | Attempted to downgrade version on update |

## Migration Strategy

### Backward Compatibility

**Existing entities without SchemaVersion**:
- Treated as version `1` when read (FR-008)
- When updated, frontend sends current version (auto-migration)

**Implementation**:

```csharp
// In repository read operations
public WorldEntity GetById(Guid id)
{
    var entity = _dbContext.WorldEntities.Find(id);
    
    // Backward compatibility: treat missing schema version as 1
    if (entity.SchemaVersion == 0 || entity.SchemaVersion == default)
    {
        entity.SchemaVersion = 1;
    }
    
    return entity;
}
```

### Lazy Migration

**Frontend behavior**:
1. Load entity from API (may have old schema version)
2. User edits entity
3. Save with current schema version from `ENTITY_SCHEMA_VERSIONS` constant
4. Backend validates and persists new version

**No bulk migration required**: Entities upgraded gradually as users interact with them.

## Database Considerations

### Indexing

**No index needed on SchemaVersion**:
- Not queried for filtering/sorting
- Not used in partition key
- Adds ~10 bytes per document (negligible)

### RU Impact

**Negligible RU increase**:
- Point reads: No change (schema version included in document)
- Writes: +1-2 RUs (slightly larger document)
- Queries: No change (not filtered by schema version)

## Summary

**Key Changes**:
- Add `int SchemaVersion` property to WorldEntity domain model
- Add validation logic for schema version ranges (min/max per entity type)
- Add frontend constants file for current schema versions
- Update all DTOs and API contracts to include `schemaVersion` field
- Implement backward compatibility (missing version = 1)

**No Breaking Changes**:
- SchemaVersion is optional in requests (defaults to 1)
- Existing API clients continue working without modification
- Existing entities treated as version 1

**Next Steps**: Proceed to implementation (tasks.md generation via `/speckit.tasks`)
