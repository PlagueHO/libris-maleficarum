# Quickstart: Schema Versioning for WorldEntities

**Feature**: 006-schema-versioning  
**Date**: 2026-01-21

## Overview

This quickstart guide provides step-by-step instructions for implementing schema versioning support in WorldEntity documents. Follow the phases in order to ensure tests pass at each step.

## Prerequisites

- Branch `006-schema-versioning` checked out
- Backend: .NET 10 SDK installed, Aspire workload installed
- Frontend: Node.js 20+, pnpm installed
- Cosmos DB Emulator running (for integration tests)

## Implementation Phases

### Phase 1: Backend Domain Model (30 min)

**Goal**: Add `SchemaVersion` property to WorldEntity domain model with validation.

**Files to modify**:
1. `libris-maleficarum-service/src/Domain/Entities/WorldEntity.cs`

**Steps**:

1. **Add SchemaVersion property**:

```csharp
/// <summary>
/// Gets the schema version for this entity type's property structure.
/// </summary>
public int SchemaVersion { get; private set; }
```

2. **Update Create method** - add `schemaVersion` parameter with default `1`:

```csharp
public static WorldEntity Create(
    // ... existing parameters
    int schemaVersion = 1)  // NEW
{
    // ... existing logic
    SchemaVersion = schemaVersion,  // NEW in initialization
}
```

3. **Update Update method** - add `schemaVersion` parameter:

```csharp
public void Update(
    // ... existing parameters
    int schemaVersion)  // NEW
{
    // ... existing assignments
    SchemaVersion = schemaVersion;  // NEW
}
```

4. **Add validation** in `Validate()` method:

```csharp
if (SchemaVersion < 1)
{
    throw new ArgumentException("Schema version must be at least 1.");
}
```

**Test**:

```bash
cd libris-maleficarum-service
dotnet test --filter TestCategory=Unit --filter FullyQualifiedName~WorldEntityTests
```

**Expected**: All WorldEntity unit tests pass (update test data to include `schemaVersion: 1`).

---

### Phase 2: Backend Configuration (20 min)

**Goal**: Add schema version range configuration per entity type.

**Files to create**:
1. `libris-maleficarum-service/src/Domain/Configuration/EntitySchemaVersionConfig.cs`
2. `libris-maleficarum-service/src/Domain/Exceptions/SchemaVersionException.cs`

**Files to modify**:
1. `libris-maleficarum-service/src/Infrastructure/appsettings.json`

**Steps**:

1. **Create EntitySchemaVersionConfig.cs**:

```csharp
namespace LibrisMaleficarum.Domain.Configuration;

public class EntitySchemaVersionConfig
{
    public Dictionary<string, SchemaVersionRange> EntityTypes { get; set; } = new();
    
    public SchemaVersionRange GetVersionRange(string entityType)
    {
        return EntityTypes.TryGetValue(entityType, out var range) 
            ? range 
            : new SchemaVersionRange { MinVersion = 1, MaxVersion = 1 };
    }
}

public class SchemaVersionRange
{
    public int MinVersion { get; set; } = 1;
    public int MaxVersion { get; set; } = 1;
}
```

2. **Create SchemaVersionException.cs**:

```csharp
namespace LibrisMaleficarum.Domain.Exceptions;

public class SchemaVersionException : Exception
{
    public string ErrorCode { get; }
    public int? RequestedVersion { get; set; }
    public int? CurrentVersion { get; set; }
    public int? MinSupportedVersion { get; set; }
    public int? MaxSupportedVersion { get; set; }
    public string? EntityType { get; set; }
    
    public SchemaVersionException(string errorCode, string message) 
        : base(message)
    {
        ErrorCode = errorCode;
    }
}
```

3. **Update appsettings.json** - add configuration section:

```json
{
  "EntitySchemaVersions": {
    "EntityTypes": {
      "Character": { "MinVersion": 1, "MaxVersion": 1 },
      "Location": { "MinVersion": 1, "MaxVersion": 1 },
      "Campaign": { "MinVersion": 1, "MaxVersion": 1 }
    }
  }
}
```

**Test**: Build succeeds.

```bash
dotnet build
```

---

### Phase 3: Backend Infrastructure (30 min)

**Goal**: Map SchemaVersion to Cosmos DB and handle backward compatibility.

**Files to modify**:
1. `libris-maleficarum-service/src/Infrastructure/Data/Configurations/WorldEntityConfiguration.cs`
2. Repository classes (if SchemaVersion defaulting needed)

**Steps**:

1. **Update WorldEntityConfiguration.cs** - add property mapping:

```csharp
builder.Property(e => e.SchemaVersion)
    .ToJsonProperty("schemaVersion")
    .IsRequired();
```

2. **Backward compatibility** - in repository read operations, treat missing/zero schema version as 1:

```csharp
// After fetching from database
if (entity.SchemaVersion == 0 || entity.SchemaVersion == default)
{
    // Backward compatibility: missing schema version = version 1
    // Note: Use reflection or EF property access to set private setter
}
```

**Test**:

```bash
dotnet test --filter TestCategory=Integration
```

**Expected**: Integration tests pass (update test data as needed).

---

### Phase 4: Backend API Layer (45 min)

**Goal**: Add schema version validation in API controllers and update DTOs.

**Files to modify**:
1. `libris-maleficarum-service/src/Api/DTOs/CreateWorldEntityRequest.cs`
2. `libris-maleficarum-service/src/Api/DTOs/UpdateWorldEntityRequest.cs`
3. `libris-maleficarum-service/src/Api/DTOs/WorldEntityResponse.cs`
4. `libris-maleficarum-service/src/Api/Controllers/WorldEntityController.cs`

**Files to create**:
1. `libris-maleficarum-service/src/Api/Validators/SchemaVersionValidator.cs`

**Steps**:

1. **Update CreateWorldEntityRequest.cs**:

```csharp
public int? SchemaVersion { get; set; }
```

2. **Update UpdateWorldEntityRequest.cs**:

```csharp
public int? SchemaVersion { get; set; }
```

3. **Update WorldEntityResponse.cs**:

```csharp
public int SchemaVersion { get; set; }
```

4. **Create SchemaVersionValidator.cs**:

```csharp
public class SchemaVersionValidator
{
    private readonly EntitySchemaVersionConfig _config;
    
    public SchemaVersionValidator(IOptions<EntitySchemaVersionConfig> config)
    {
        _config = config.Value;
    }
    
    public void ValidateCreate(string entityType, int? requestedVersion)
    {
        var version = requestedVersion ?? 1;
        var range = _config.GetVersionRange(entityType);
        
        if (version < 1)
            throw new SchemaVersionException("SCHEMA_VERSION_INVALID", 
                "Schema version must be a positive integer")
            {
                RequestedVersion = version,
                EntityType = entityType
            };
        
        if (version < range.MinVersion)
            throw new SchemaVersionException("SCHEMA_VERSION_TOO_LOW", 
                $"Schema version {version} is below minimum {range.MinVersion}")
            {
                RequestedVersion = version,
                MinSupportedVersion = range.MinVersion,
                MaxSupportedVersion = range.MaxVersion,
                EntityType = entityType
            };
        
        if (version > range.MaxVersion)
            throw new SchemaVersionException("SCHEMA_VERSION_TOO_HIGH", 
                $"Schema version {version} exceeds maximum {range.MaxVersion}")
            {
                RequestedVersion = version,
                MinSupportedVersion = range.MinVersion,
                MaxSupportedVersion = range.MaxVersion,
                EntityType = entityType
            };
    }
    
    public void ValidateUpdate(string entityType, int currentVersion, int requestedVersion)
    {
        var range = _config.GetVersionRange(entityType);
        
        if (requestedVersion < currentVersion)
            throw new SchemaVersionException("SCHEMA_DOWNGRADE_NOT_ALLOWED", 
                $"Cannot downgrade from {currentVersion} to {requestedVersion}")
            {
                RequestedVersion = requestedVersion,
                CurrentVersion = currentVersion,
                EntityType = entityType
            };
        
        if (requestedVersion > range.MaxVersion)
            throw new SchemaVersionException("SCHEMA_VERSION_TOO_HIGH", 
                $"Schema version {requestedVersion} exceeds maximum {range.MaxVersion}")
            {
                RequestedVersion = requestedVersion,
                CurrentVersion = currentVersion,
                MinSupportedVersion = range.MinVersion,
                MaxSupportedVersion = range.MaxVersion,
                EntityType = entityType
            };
    }
}
```

5. **Update WorldEntityController.cs**:

```csharp
// Inject validator
private readonly SchemaVersionValidator _schemaVersionValidator;

[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateWorldEntityRequest request)
{
    // Validate schema version
    _schemaVersionValidator.ValidateCreate(
        request.EntityType.ToString(), 
        request.SchemaVersion);
    
    // Create entity with schema version
    var entity = WorldEntity.Create(
        // ... existing parameters
        schemaVersion: request.SchemaVersion ?? 1);
    
    // ... rest of creation logic
}

[HttpPut("{id}")]
public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorldEntityRequest request)
{
    var existing = await _repository.GetByIdAsync(id);
    
    // Validate schema version
    if (request.SchemaVersion.HasValue)
    {
        _schemaVersionValidator.ValidateUpdate(
            existing.EntityType.ToString(),
            existing.SchemaVersion,
            request.SchemaVersion.Value);
    }
    
    // Update with new schema version
    existing.Update(
        // ... existing parameters
        schemaVersion: request.SchemaVersion ?? existing.SchemaVersion);
    
    // ... rest of update logic
}
```

6. **Add global exception handler** for SchemaVersionException → 400 with error details:

```csharp
// In exception middleware
if (exception is SchemaVersionException sve)
{
    context.Response.StatusCode = 400;
    await context.Response.WriteAsJsonAsync(new
    {
        error = new
        {
            code = sve.ErrorCode,
            message = sve.Message,
            details = new
            {
                entityType = sve.EntityType,
                requestedVersion = sve.RequestedVersion,
                currentVersion = sve.CurrentVersion,
                minSupportedVersion = sve.MinSupportedVersion,
                maxSupportedVersion = sve.MaxSupportedVersion
            }
        }
    });
}
```

**Test**:

```bash
dotnet test --filter TestCategory=Unit --filter FullyQualifiedName~WorldEntityControllerTests
```

**Expected**: API controller tests pass with schema version validation.

---

### Phase 5: Frontend Types (20 min)

**Goal**: Add `schemaVersion` to TypeScript interfaces and create constants file.

**Files to modify**:
1. `libris-maleficarum-app/src/services/types/worldEntity.types.ts`

**Files to create**:
1. `libris-maleficarum-app/src/services/constants/entitySchemaVersions.ts`

**Steps**:

1. **Update worldEntity.types.ts** - add `schemaVersion` to interfaces:

```typescript
export interface WorldEntity {
  // ... existing properties
  schemaVersion: number;  // NEW
}

export interface CreateWorldEntityRequest {
  // ... existing properties
  schemaVersion?: number;  // NEW (optional)
}

export interface UpdateWorldEntityRequest {
  // ... existing properties
  schemaVersion?: number;  // NEW (optional)
}
```

2. **Create entitySchemaVersions.ts**:

```typescript
import { WorldEntityType } from '../types/worldEntity.types';

export const ENTITY_SCHEMA_VERSIONS: Record<WorldEntityType, number> = {
  [WorldEntityType.Character]: 1,
  [WorldEntityType.Location]: 1,
  [WorldEntityType.Campaign]: 1,
  [WorldEntityType.Continent]: 1,
  [WorldEntityType.Country]: 1,
  [WorldEntityType.Region]: 1,
  [WorldEntityType.City]: 1,
  [WorldEntityType.Building]: 1,
  [WorldEntityType.Room]: 1,
  [WorldEntityType.Faction]: 1,
  [WorldEntityType.Event]: 1,
  [WorldEntityType.Quest]: 1,
  [WorldEntityType.Item]: 1,
  [WorldEntityType.Session]: 1,
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
  [WorldEntityType.GeographicRegion]: 1,
  [WorldEntityType.PoliticalRegion]: 1,
  [WorldEntityType.CulturalRegion]: 1,
  [WorldEntityType.MilitaryRegion]: 1,
  [WorldEntityType.Other]: 1,
};

export function getSchemaVersion(entityType: WorldEntityType): number {
  return ENTITY_SCHEMA_VERSIONS[entityType];
}
```

**Test**:

```bash
cd libris-maleficarum-app
pnpm type-check
```

**Expected**: No TypeScript errors.

---

### Phase 6: Frontend API Client (25 min)

**Goal**: Include schema version in API requests using constants.

**Files to modify**:
1. `libris-maleficarum-app/src/services/worldEntityApi.ts`

**Steps**:

1. **Import constants**:

```typescript
import { getSchemaVersion } from './constants/entitySchemaVersions';
```

2. **Update create request** - include schema version:

```typescript
export async function createWorldEntity(
  worldId: string,
  request: CreateWorldEntityRequest
): Promise<WorldEntity> {
  const response = await apiClient.post(`/worlds/${worldId}/entities`, {
    ...request,
    schemaVersion: getSchemaVersion(request.entityType),  // NEW
  });
  return response.data.data;
}
```

3. **Update update request** - include current schema version:

```typescript
export async function updateWorldEntity(
  worldId: string,
  entityId: string,
  request: UpdateWorldEntityRequest
): Promise<WorldEntity> {
  // Get entity type from existing entity (or pass as parameter)
  const current = await getWorldEntity(worldId, entityId);
  
  const response = await apiClient.put(`/worlds/${worldId}/entities/${entityId}`, {
    ...request,
    schemaVersion: getSchemaVersion(
      request.entityType ?? current.entityType
    ),  // NEW
  });
  return response.data.data;
}
```

**Test**:

```bash
pnpm test src/services/worldEntityApi.test.ts
```

**Expected**: API client tests pass (update MSW mocks to include `schemaVersion: 1`).

---

### Phase 7: Frontend Components (30 min)

**Goal**: Verify Redux state preserves schema version and forms use current version.

**Files to modify**:
1. `libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.tsx`
2. `libris-maleficarum-app/src/components/MainPanel/__tests__/EntityDetailForm.test.tsx`

**Steps**:

1. **EntityDetailForm.tsx** - ensure schema version is passed:

```typescript
import { getSchemaVersion } from '@/services/constants/entitySchemaVersions';

// In handleSave
const request: UpdateWorldEntityRequest = {
  name: formData.name,
  description: formData.description,
  tags: formData.tags,
  entityType: formData.entityType,
  schemaVersion: getSchemaVersion(formData.entityType),  // NEW
};
```

2. **Update tests** - verify schema version in requests:

```typescript
it('includes schema version in create request', async () => {
  // ... render form
  // ... fill form
  // ... submit
  
  expect(mockCreateEntity).toHaveBeenCalledWith(
    expect.anything(),
    expect.objectContaining({
      schemaVersion: 1,  // NEW assertion
    })
  );
});
```

**Test**:

```bash
pnpm test EntityDetailForm.test.tsx
```

**Expected**: Component tests pass.

---

### Phase 8: Documentation (15 min)

**Goal**: Update design documentation with schema versioning details.

**Files to modify**:
1. `docs/design/DATA_MODEL.md`
2. `docs/design/API.md`

**Steps**:

1. **Update DATA_MODEL.md** - add `SchemaVersion` to BaseWorldEntity schema:

```markdown
### WorldEntity Base Schema

```csharp
public record BaseWorldEntity
{
    // ... existing properties
    public int SchemaVersion { get; init; }  // NEW: Schema version (>= 1)
}
```

2. **Update API.md** - add `schemaVersion` to example payloads:

```markdown
### Example: Create Entity Request

```json
{
  "entityType": "Character",
  "name": "Gandalf",
  "schemaVersion": 1
}
```

**Test**: Review changes for accuracy.

---

## Verification

### Full Test Suite

**Backend**:

```bash
cd libris-maleficarum-service
dotnet build
dotnet test
dotnet format --verify-no-changes
```

**Frontend**:

```bash
cd libris-maleficarum-app
pnpm build
pnpm lint
pnpm test
pnpm type-check
```

### Manual Testing

1. **Start Aspire AppHost**:

```bash
cd libris-maleficarum-service
dotnet run --project src/Orchestration/AppHost
```

2. **Start frontend dev server**:

```bash
cd libris-maleficarum-app
pnpm dev
```

3. **Test scenarios**:
   - Create new entity → verify `schemaVersion: 1` in response
   - Update entity → verify schema version preserved
   - Try invalid version (e.g., -1) → verify 400 error with details
   - Load existing entity without schema version → verify treated as version 1

## Troubleshooting

**Issue**: Tests fail with "SchemaVersion required"  
**Solution**: Update test fixtures to include `schemaVersion: 1`

**Issue**: TypeScript errors on ENTITY_SCHEMA_VERSIONS  
**Solution**: Ensure all WorldEntityType values have entries in the constant map

**Issue**: Backend validation not triggered  
**Solution**: Verify SchemaVersionValidator is registered in DI container

## Next Steps

After implementation:
1. Create PR for review
2. Run full CI pipeline
3. Update CHANGELOG.md
4. Deploy to test environment
5. Verify schema versioning in production-like scenario
