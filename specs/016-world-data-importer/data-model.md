# Data Model: World Data Importer

**Feature Branch**: `016-world-data-importer`
**Date**: 2026-03-10

## Overview

The World Data Importer introduces models for parsing, validating, and importing world data from JSON files into the backend API. These models live in the `LibrisMaleficarum.Import` library (under `src/Components/Import/`) and are independent of any console or UI concerns. HTTP communication with the API is delegated to the `LibrisMaleficarum.Api.Client` SDK (under `src/Client/Api/`).

## Import Format Models

### WorldImportDefinition

Represents the parsed `world.json` file — the root of an import source.

```csharp
namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// Represents the world definition parsed from world.json.
/// </summary>
public sealed class WorldImportDefinition
{
    public required string Name { get; init; }
    public string? Description { get; init; }
}
```

**Validation rules**:

- `Name` is required, 1–100 characters.
- `Description` is optional, max 2000 characters.

### EntityImportDefinition

Represents a single entity JSON file in the import source.

```csharp
namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// Represents a world entity definition parsed from an entity JSON file.
/// </summary>
public sealed class EntityImportDefinition
{
    public required string LocalId { get; init; }
    public required string EntityType { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? ParentLocalId { get; init; }
    public List<string>? Tags { get; init; }
    public Dictionary<string, object>? Properties { get; init; }

    /// <summary>
    /// The source file path (for error reporting). Not serialized from JSON.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? SourceFilePath { get; set; }
}
```

**Validation rules**:

- `LocalId` is required, must be non-empty, unique across all entities in the import.
- `EntityType` is required, must match a valid `EntityType` enum value (case-insensitive).
- `Name` is required, 1–200 characters.
- `Description` is optional, max 5000 characters.
- `ParentLocalId` if provided, must reference an existing `LocalId` in the import.
- `Tags` if provided, max 20 items, each max 50 characters.
- `Properties` if provided, serialized JSON max 100KB.

### ImportManifest

The computed internal representation of the full entity tree, derived from `localId`/`parentLocalId` references.

```csharp
namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// Computed manifest representing the resolved import hierarchy.
/// </summary>
public sealed class ImportManifest
{
    public required WorldImportDefinition World { get; init; }
    public required IReadOnlyList<ResolvedEntity> Entities { get; init; }
    public required IReadOnlyDictionary<int, IReadOnlyList<ResolvedEntity>> EntitiesByDepth { get; init; }
    public int MaxDepth { get; init; }
    public int TotalEntityCount { get; init; }

    /// <summary>
    /// Entity counts grouped by entity type for summary reporting.
    /// </summary>
    public required IReadOnlyDictionary<string, int> CountsByType { get; init; }
}
```

### ResolvedEntity

An entity with its hierarchy position fully resolved (depth, path, assigned GUID).

```csharp
namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// An entity import definition with resolved hierarchy information.
/// </summary>
public sealed class ResolvedEntity
{
    public required EntityImportDefinition Definition { get; init; }
    public required Guid AssignedId { get; init; }
    public required Guid? ResolvedParentId { get; init; }
    public required List<Guid> Path { get; init; }
    public required int Depth { get; init; }
    public required List<ResolvedEntity> Children { get; init; }
}
```

## Validation Models

### ImportValidationResult

Result of validating an import source.

```csharp
namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// Result of import source validation.
/// </summary>
public sealed class ImportValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public required IReadOnlyList<ImportValidationError> Errors { get; init; }
    public required IReadOnlyList<ImportValidationWarning> Warnings { get; init; }
    public ImportManifest? Manifest { get; init; }
}
```

### ImportValidationError

```csharp
namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// A validation error found during import source analysis.
/// </summary>
public sealed class ImportValidationError
{
    public required string FilePath { get; init; }
    public required string Code { get; init; }
    public required string Message { get; init; }
    public int? LineNumber { get; init; }
}
```

### ImportValidationWarning

```csharp
namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// A non-blocking warning found during import source analysis.
/// </summary>
public sealed class ImportValidationWarning
{
    public required string FilePath { get; init; }
    public required string Code { get; init; }
    public required string Message { get; init; }
}
```

## Import Execution Models

### ImportResult

Result of an import execution.

```csharp
namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// Result of an import execution against the backend API.
/// </summary>
public sealed class ImportResult
{
    public required bool Success { get; init; }
    public required Guid WorldId { get; init; }
    public required int TotalEntitiesCreated { get; init; }
    public required int TotalEntitiesFailed { get; init; }
    public required int TotalEntitiesSkipped { get; init; }
    public required IReadOnlyDictionary<string, int> CreatedByType { get; init; }
    public required IReadOnlyList<EntityImportError> Errors { get; init; }
    public required TimeSpan Duration { get; init; }
}
```

### EntityImportError

```csharp
namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// An error that occurred while importing a specific entity.
/// </summary>
public sealed class EntityImportError
{
    public required string LocalId { get; init; }
    public required string EntityName { get; init; }
    public required string ErrorMessage { get; init; }
    public required string? FilePath { get; init; }
    public required IReadOnlyList<string> SkippedDescendantLocalIds { get; init; }
}
```

### ImportOptions

Configuration options for import execution.

```csharp
namespace LibrisMaleficarum.Import.Models;

/// <summary>
/// Configuration options controlling import behavior.
/// </summary>
public sealed class ImportOptions
{
    public required string ApiBaseUrl { get; init; }
    public required string AuthToken { get; init; }
    public int MaxConcurrency { get; init; } = 10;
    public bool ValidateOnly { get; init; }
    public bool Verbose { get; init; }
}
```

## Service Interfaces

### IImportSourceReader

Reads and parses an import source (folder or zip) into raw definitions.

```csharp
namespace LibrisMaleficarum.Import.Interfaces;

public interface IImportSourceReader
{
    Task<ImportSourceContent> ReadAsync(string sourcePath, CancellationToken cancellationToken = default);
}
```

### IImportValidator

Validates imported definitions and builds the resolved manifest.

```csharp
namespace LibrisMaleficarum.Import.Interfaces;

public interface IImportValidator
{
    ImportValidationResult Validate(ImportSourceContent content);
}
```

### IWorldImportService

Orchestrates the full import workflow (read → validate → execute). Uses `ILibrisApiClient` from the Api.Client SDK.

```csharp
namespace LibrisMaleficarum.Import.Interfaces;

public interface IWorldImportService
{
    Task<ImportResult> ImportAsync(
        string sourcePath,
        ImportOptions options,
        IProgress<ImportProgress>? progress = null,
        CancellationToken cancellationToken = default);

    Task<ImportValidationResult> ValidateAsync(
        string sourcePath,
        CancellationToken cancellationToken = default);
}
```

### ILibrisApiClient

Typed API client interface provided by the `LibrisMaleficarum.Api.Client` SDK. Consumed by the Import library's `WorldImportService` for HTTP communication.

```csharp
namespace LibrisMaleficarum.Api.Client;

public interface ILibrisApiClient
{
    Task<WorldResponse> CreateWorldAsync(
        CreateWorldRequest request,
        CancellationToken cancellationToken = default);

    Task<EntityResponse> CreateEntityAsync(
        Guid worldId, CreateEntityRequest request,
        CancellationToken cancellationToken = default);
}
```

> **Note**: The `ILibrisApiClient` interface and its `LibrisApiClient` implementation live in the `LibrisMaleficarum.Api.Client` project (`src/Client/Api/`), not in the Import library. The Import library references the Api.Client project and consumes the interface via dependency injection.

## Supporting Models

### ImportSourceContent

Raw parsed content from an import source before validation.

```csharp
namespace LibrisMaleficarum.Import.Models;

public sealed class ImportSourceContent
{
    public required WorldImportDefinition? World { get; init; }
    public required IReadOnlyList<EntityImportDefinition> Entities { get; init; }
    public required IReadOnlyList<ImportValidationError> ParseErrors { get; init; }
    public required string SourcePath { get; init; }
    public required ImportSourceType SourceType { get; init; }
}

public enum ImportSourceType
{
    Folder,
    ZipArchive
}
```

### ImportProgress

Progress reporting for the import operation.

```csharp
namespace LibrisMaleficarum.Import.Models;

public sealed class ImportProgress
{
    public required int TotalEntities { get; init; }
    public required int CompletedEntities { get; init; }
    public required int CurrentDepth { get; init; }
    public required string CurrentEntityName { get; init; }
    public required ImportPhase Phase { get; init; }
}

public enum ImportPhase
{
    Reading,
    Validating,
    CreatingWorld,
    CreatingEntities,
    Complete
}
```

### CreateEntityImportRequest

> **REMOVED**: This model was identified as unused during cross-artifact analysis. The `WorldImportService` maps `ResolvedEntity` directly to the API client SDK's `CreateEntityRequest`. No intermediate import-specific request model is needed.

## Field Mapping: Import Format → API Request

| Import Field (EntityImportDefinition) | API Field (CreateWorldEntityRequest) | Notes |
|----------------------------------------|--------------------------------------|-------|
| `localId` | — | Used for relationship resolution only; not sent to API |
| `entityType` | `EntityType` | String → EntityType enum (case-insensitive) |
| `name` | `Name` | Direct mapping |
| `description` | `Description` | Direct mapping |
| `parentLocalId` | `ParentId` | Resolved from localId → assigned GUID |
| `tags` | `Tags` | Direct mapping |
| `properties` | `Attributes` | **Renamed**: import `properties` maps to API `attributes` |
| — | `SchemaVersion` | Always set to `1` by the import tool (FR-039) |

## Validation Error Codes

| Code | Meaning |
|------|---------|
| `WORLD_MISSING` | No `world.json` found at import root |
| `WORLD_INVALID_JSON` | `world.json` contains invalid JSON |
| `WORLD_MISSING_NAME` | `world.json` missing required `name` field |
| `ENTITY_INVALID_JSON` | Entity file contains invalid JSON |
| `ENTITY_MISSING_LOCAL_ID` | Entity file missing `localId` field |
| `ENTITY_DUPLICATE_LOCAL_ID` | Duplicate `localId` found across import files |
| `ENTITY_MISSING_NAME` | Entity file missing `name` field |
| `ENTITY_MISSING_TYPE` | Entity file missing `entityType` field |
| `ENTITY_INVALID_TYPE` | `entityType` does not match a known EntityType |
| `ENTITY_DANGLING_PARENT` | `parentLocalId` references non-existent `localId` |
| `ENTITY_CYCLE_DETECTED` | Circular parent-child reference detected |
| `ENTITY_NAME_TOO_LONG` | Entity name exceeds 200 characters |
| `ENTITY_DESC_TOO_LONG` | Entity description exceeds 5000 characters |
| `ENTITY_TOO_MANY_TAGS` | More than 20 tags |
| `ENTITY_TAG_TOO_LONG` | Tag exceeds 50 characters |
| `ENTITY_PROPS_TOO_LARGE` | Properties JSON exceeds 100KB |
| `ZIP_INVALID` | Zip archive is corrupted or invalid |
| `ZIP_SLIP_DETECTED` | Zip entry path escapes extraction directory |
