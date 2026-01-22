# Research: Schema Versioning for WorldEntities

**Feature**: 006-schema-versioning  
**Date**: 2026-01-21  
**Status**: Complete

## Overview

This document captures research findings and technical decisions for adding schema versioning support to WorldEntity documents. All research questions from the feature specification have been resolved through the clarification process.

## Research Questions & Findings

### 1. Schema Version Configuration Storage

**Question**: Where should the current schema version for each entity type be stored in the frontend configuration?

**Finding**: Centralized constants file with version map

**Decision**: Create `src/services/constants/entitySchemaVersions.ts` with a typed constant:

```typescript
export const ENTITY_SCHEMA_VERSIONS: Record<WorldEntityType, number> = {
  Character: 1,
  Location: 1,
  Campaign: 1,
  // ... all entity types
};
```

**Rationale**:

- Simplicity: Single source of truth for schema versions
- Type safety: TypeScript ensures all entity types have versions
- Clear deployment control: Explicit version updates during deployments
- No runtime complexity: No API calls or async configuration loading

**Future Migration**: In a future feature, schema versions will be defined in data-driven schema files per entity type (PropertySchema documents); the constants file approach is an interim solution.

### 2. Backend Schema Version Update Behavior

**Question**: What does "explicitly updated" mean when the spec says "the entity document retains its existing SchemaVersion value unless the schema has been explicitly updated"?

**Finding**: Backend accepts client-provided schema version with validation

**Decision**: Backend always persists the `SchemaVersion` value provided by the client in the request payload, with two validations:

1. `SchemaVersion` must be >= entity's current `SchemaVersion` (prevents downgrades)
1. `SchemaVersion` must be within [min, max] supported range for that entity type (prevents deprecated/future versions)

**Rationale**:

- Frontend controls migration strategy (lazy migration on save)
- Backend enforces data integrity constraints (no downgrades, no invalid versions)
- Keeps backend logic simple (no schema knowledge required beyond min/max versions)

### 3. Default Schema Version for New Entities

**Question**: When should the backend use the default value of `1` for `SchemaVersion`?

**Finding**: Default only when field is completely omitted

**Decision**: Backend defaults `SchemaVersion` to `1` only when the client completely omits the field from the request payload.

**Rationale**:

- Provides backward compatibility for API clients that don't send `SchemaVersion`
- Frontend always sends explicit version (from `ENTITY_SCHEMA_VERSIONS` constant)
- Aligns with "additive field" approach (existing clients without schema version support continue working)

### 4. Error Response Details for Schema Version Validation

**Question**: What information should schema version validation error responses include to help clients debug failures?

**Finding**: Include specific error codes and contextual version details

**Decision**: Error responses include:

- **Error codes**: `SCHEMA_VERSION_INVALID`, `SCHEMA_VERSION_TOO_HIGH`, `SCHEMA_VERSION_TOO_LOW`, `SCHEMA_DOWNGRADE_NOT_ALLOWED`
- **Contextual details**: `requestedVersion`, `currentVersion` (for updates), `minSupportedVersion`, `maxSupportedVersion`, `entityType`

**Rationale**:

- Actionable debugging information for developers
- Clear indication of why validation failed
- Enables frontend to display user-friendly error messages
- Follows standard problem details pattern (RFC 7807-inspired)

### 5. Backend Min/Max Schema Version Validation

**Question**: Should the backend validate that new entities use a specific schema version, or accept any valid version within the supported range?

**Finding**: Backend maintains min/max supported schema versions per entity type

**Decision**: Backend maintains configuration of minimum and maximum supported schema versions per entity type (e.g., `Character: { min: 1, max: 2 }`). Incoming `SchemaVersion` values are validated against this range for both create and update operations.

**Rationale**:

- Prevents use of deprecated schema versions (below min)
- Prevents use of future schema versions not yet supported (above max)
- Enables gradual deprecation of old schemas (increment min version over time)
- Provides clear version boundaries for clients

## Best Practices Research

### Schema Versioning Patterns

**Pattern Evaluated**: Forward-only schema evolution (extension-only)

**Findings**:

- ✅ **Additive changes only**: New fields can be added in later versions
- ❌ **No field removal**: Removing fields would cause data loss for entities at older versions
- ❌ **No field renames**: Renaming is effectively remove + add (data loss)
- ✅ **Lazy migration**: Entities upgraded to latest version only when edited (not in bulk)
- ✅ **Mixed versions supported**: Multiple schema versions can coexist in same database

**References**:

- Cosmos DB schema versioning: [Versioning strategies for Azure Cosmos DB](https://learn.microsoft.com/en-us/azure/cosmos-db/database-versioning)
- Event sourcing patterns: Forward-compatible schema evolution

### EF Core with Cosmos DB

**Question**: How to map schema version to Cosmos DB using EF Core?

**Finding**: Use `HasProperty()` in entity configuration

```csharp
modelBuilder.Entity<WorldEntity>()
    .ToContainer("WorldEntity")
    .HasPartitionKey(e => new { e.WorldId, e.Id })
    .HasProperty(e => e.SchemaVersion)
    .ToJsonProperty("schemaVersion");
```

**Rationale**:

- Standard EF Core property mapping
- JSON property name matches frontend convention (camelCase)
- No special handling required for integer properties

### Frontend Constants Management

**Pattern**: Centralized configuration with type safety

```typescript
// src/services/constants/entitySchemaVersions.ts
import { WorldEntityType } from '../types/worldEntity.types';

export const ENTITY_SCHEMA_VERSIONS: Record<WorldEntityType, number> = {
  [WorldEntityType.Character]: 1,
  [WorldEntityType.Location]: 1,
  [WorldEntityType.Campaign]: 1,
  // TypeScript enforces all types have versions
};

export function getSchemaVersion(entityType: WorldEntityType): number {
  return ENTITY_SCHEMA_VERSIONS[entityType];
}
```

**Benefits**:

- Compile-time verification all entity types have schema versions
- Single import for all version lookups
- Easy to update during deployments

## Technical Decisions Summary

| Decision Point | Choice | Alternative Rejected | Rationale |
|----------------|--------|---------------------|-----------|
| Frontend config storage | Centralized constants file | API endpoint, env vars, per-entity constants | Simplicity, type safety, deployment control |
| Backend validation | Min/max range per entity type | No validation, single max version, exact match | Flexibility + integrity |
| Default version | `1` when omitted | Always require explicit, use `0` | Backward compatibility |
| Error details | Code + context (min/max/current) | Generic message only | Developer experience |
| Migration strategy | Lazy (on edit) | Bulk migration, automatic on read | Non-disruptive, gradual |

## Implementation Considerations

### Database Migration

**Question**: Do existing WorldEntity documents need migration?

**Answer**: No immediate migration required. Treat missing `SchemaVersion` as version `1` (FR-008).

**Future consideration**: If min version is incremented above `1`, a one-time bulk update may be needed to upgrade entities at older versions.

### Testing Strategy

**Unit Tests**:

- Domain: WorldEntity.Create() with/without schema version
- Domain: Schema version validation logic (range checks, downgrade prevention)
- API: Schema version validation in requests

**Integration Tests**:

- Create entity with schema version → persisted to Cosmos DB
- Update entity with newer version → migration successful
- Update entity with older version → validation error

**Frontend Tests**:

- API client includes schema version in requests
- EntityDetailForm uses current schema version from constants
- Redux state preserves schema version

### Performance Impact

**RU Cost**: Adding SchemaVersion field adds ~10 bytes per document (negligible)
**Query Impact**: No impact on queries (not filtered/sorted by schema version)
**Indexing**: No index needed on SchemaVersion (not queried)

## Dependencies

**Frontend**:

- No new dependencies

**Backend**:

- No new dependencies (uses existing EF Core, ASP.NET Core validation)

## Open Questions

*None remaining* - All questions resolved through clarification process (see spec.md Clarifications section)

## Next Steps

Proceed to Phase 1: Data Model and Contracts design
