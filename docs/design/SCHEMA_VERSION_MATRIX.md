# Schema Version Matrix Analysis

**Purpose**: Evaluate different schema version handling strategies for WorldEntity CRUD operations between frontend and backend.

## Design Constraints

1. ❌ **No Client/Backend schema version mismatches**
1. ❌ **No rollback to older schema versions**
1. ❌ **Avoid tight coupling between frontend/backend**
1. ✅ **Support additive-only schema updates** (no breaking changes)
1. ⚠️ **Potential type conversion** (with AI assistance, some data loss acceptable)
1. ✅ **Frontend upgrades independent of backend upgrades**
1. ✅ **Shared data-driven schema store** (frontend and backend read from common source)

---

## Matrix 1: CREATE Operations

| Frontend Sends | Backend Receives | Backend Action | Stored Value | Validation | Notes |
|----------------|------------------|----------------|--------------|------------|-------|
| **schemaVersion=2** (explicit) | schemaVersion=2 | Validate 2 ≤ MAX_SUPPORTED | 2 | ✅ Pass if valid<br>❌ Reject if > MAX | Current implementation (FR-007) |
| **schemaVersion=null/undefined** | null/undefined | Default to 1 | 1 | ✅ Always pass | Backward compatibility approach |
| **schemaVersion=null/undefined** | null/undefined | Use MAX_SUPPORTED for type | MAX | ✅ Always pass | **Risky**: Future backend can't read old client data |
| **schemaVersion omitted** | undefined | Default to 1 | 1 | ✅ Always pass | Legacy support |
| **schemaVersion=0** | 0 | Reject (invalid) | N/A | ❌ Reject: invalid value | Error handling |
| **schemaVersion=-1** | -1 | Reject (invalid) | N/A | ❌ Reject: invalid value | Error handling |
| **schemaVersion=999** | 999 | Reject (> MAX) | N/A | ❌ Reject: SCHEMA_VERSION_TOO_HIGH | Prevents future schemas |

### Recommendations for CREATE

**Current Approach (FR-007 Compliant)**:

- ✅ Frontend MUST send schemaVersion explicitly using config value
- ✅ Backend validates within [MIN, MAX] range per entity type
- ✅ Stored value = what frontend sends (after validation)

**Why**: Ensures frontend controls schema version explicitly, preventing mismatches.

---

## Matrix 2: UPDATE Operations

| Scenario | Frontend Sends | Backend Receives | Current schemaVersion | Backend Action | Stored Value | Validation | Risk Level |
|----------|----------------|------------------|----------------------|----------------|--------------|------------|------------|
| **A. Explicit upgrade** | schemaVersion=2 | 2 | 1 | Validate 2 ≥ 1 AND 2 ≤ MAX | 2 | ✅ Pass | 🟢 Low |
| **B. Explicit same version** | schemaVersion=1 | 1 | 1 | Validate 1 ≥ 1 AND 1 ≤ MAX | 1 | ✅ Pass | 🟢 Low |
| **C. Rollback attempt** | schemaVersion=1 | 1 | 2 | Reject (downgrade) | N/A | ❌ Reject: SCHEMA_DOWNGRADE_NOT_ALLOWED | 🟢 Protected |
| **D. Omitted (implicit current)** | schemaVersion=null | null | 1 | Use current (1) | 1 | ✅ Pass | 🟡 Medium |
| **E. Omitted (lazy migration)** | schemaVersion=null | null | 1 | Use MAX_SUPPORTED (2) | 2 | ✅ Pass | 🔴 High |
| **F. Frontend ahead** | schemaVersion=3 | 3 | 2 | Reject (> MAX) | N/A | ❌ Reject: SCHEMA_VERSION_TOO_HIGH | 🟢 Protected |
| **G. Type change + version** | schemaVersion=2<br>entityType=Character | 2, Character | 1 (Location) | Validate type conversion + version | 2 | ✅ Pass (if type change allowed) | 🟡 Medium |
| **H. Malformed request** | schemaVersion="abc" | "abc" | 1 | Reject (invalid type) | N/A | ❌ Reject: SCHEMA_VERSION_INVALID | 🟢 Protected |

### Recommendations for UPDATE

**Current Approach (FR-007 Compliant - Scenario A/B)**:

- ✅ Frontend MUST send schemaVersion explicitly using config value
- ✅ Backend validates schemaVersion ≥ current AND schemaVersion ≤ MAX
- ✅ Prevents rollback (Scenario C)
- ✅ Stored value = what frontend sends (after validation)

**Alternative: Allow Omission (Scenario D)**:

- ⚠️ Frontend MAY omit schemaVersion
- ⚠️ Backend preserves current schemaVersion if omitted
- ⚠️ Lazy migration never happens
- ❌ **Violates FR-007**

**Alternative: Lazy Migration (Scenario E)**:

- ⚠️ Frontend MAY omit schemaVersion
- ⚠️ Backend upgrades to MAX_SUPPORTED if omitted
- ❌ **High Risk**: Backend can make entity unreadable by older frontend
- ❌ **Violates Constraint #1** (client/backend mismatch)
- ❌ **Violates FR-007**

---

## Matrix 3: Version Compatibility

| Frontend Schema Config | Backend MAX_SUPPORTED | Create Behavior | Update Behavior | Risk Assessment |
|-------------------------|----------------------|-----------------|-----------------|-----------------|
| **v1** | v1 | ✅ Creates v1 | ✅ Keeps v1 | 🟢 Perfect sync |
| **v2** | v1 | ❌ Rejects (v2 > MAX) | ❌ Rejects (v2 > MAX) | 🔴 **Deployment Order Required** |
| **v1** | v2 | ✅ Creates v1 | ✅ Keeps v1 | 🟢 Backward compatible |
| **v2** | v2 | ✅ Creates v2 | ✅ Upgrades to v2 | 🟢 Perfect sync |
| **v2** | v3 | ✅ Creates v2 | ✅ Upgrades to v2 | 🟢 Backend ahead (safe) |
| **v3** | v2 | ❌ Rejects (v3 > MAX) | ❌ Rejects (v3 > MAX) | 🔴 **Frontend ahead (unsafe)** |

### Deployment Order Implications

| Scenario | Deployment Order | Outcome | Recommendation |
|----------|------------------|---------|----------------|
| **Schema v1 → v2 upgrade** | Backend first, then Frontend | ✅ Safe: Backend accepts v1 & v2, Frontend sends v1 → v2 | **PREFERRED** |
| **Schema v1 → v2 upgrade** | Frontend first, then Backend | ❌ BREAKS: Frontend sends v2, Backend MAX=v1 rejects | **AVOID** |
| **Schema rollback v2 → v1** | Any order | ❌ IMPOSSIBLE: Existing v2 entities can't downgrade | **NOT SUPPORTED** |

**Recommendation**: Always deploy backend schema changes before frontend.

---

## Matrix 4: Schema Store Architecture

### Option A: Hardcoded in Frontend + Backend (Current)

**Frontend**: `entitySchemaVersions.ts`

```typescript
export const ENTITY_SCHEMA_VERSIONS = {
  World: 1,
  Continent: 1,
  Character: 2,  // Frontend knows v2
};
```

**Backend**: `SchemaVersionConfiguration.cs`

```csharp
private static readonly Dictionary<string, (int Min, int Max)> SchemaVersionRanges = new()
{
    { "World", (1, 1) },
    { "Continent", (1, 1) },
    { "Character", (1, 2) },  // Backend supports v1-v2
};
```

| Aspect | Rating | Notes |
|--------|--------|-------|
| Coupling | 🔴 High | Must update both codebases |
| Deployment | 🔴 Order-dependent | Backend first, always |
| Flexibility | 🔴 Low | Requires code deploy for schema change |
| Consistency Risk | 🔴 High | Can get out of sync |

### Option B: Shared Configuration API

**Architecture**:

```text
Schema Config Service (Azure Storage Table / Cosmos DB)
    ↓
Frontend reads on app load → caches in memory
    ↓
Backend reads on startup → validates against
```

**Schema Config Document**:

```json
{
  "entityType": "Character",
  "currentVersion": 2,
  "supportedVersions": [1, 2],
  "minVersion": 1,
  "maxVersion": 2,
  "schema": {
    "v1": { "properties": ["name", "description", "tags"] },
    "v2": { "properties": ["name", "description", "tags", "race", "class"] }
  }
}
```

| Aspect | Rating | Notes |
|--------|--------|-------|
| Coupling | 🟢 Low | Single source of truth |
| Deployment | 🟢 Independent | Config change triggers both to refresh |
| Flexibility | 🟢 High | Schema changes without code deploy |
| Consistency Risk | 🟢 Low | Always in sync |
| Complexity | 🟡 Medium | Requires caching strategy |

**Recommended Caching Strategy**:

- Frontend: `sessionStorage` + ETag/If-None-Match
- Backend: In-memory cache + background refresh every 5 minutes
- Cache invalidation: `X-Schema-Version-Changed` header or SignalR

### Option C: Backend-Driven Schema Discovery

**Architecture**:

```text
Frontend → GET /api/v1/schema/versions → Backend returns supported versions
Frontend uses MAX version for that entity type
```

**API Response**:

```json
{
  "Character": { "min": 1, "max": 2, "current": 2 },
  "Location": { "min": 1, "max": 1, "current": 1 }
}
```

| Aspect | Rating | Notes |
|--------|--------|-------|
| Coupling | 🟢 Low | Backend is source of truth |
| Deployment | 🟢 Backend-first safe | Frontend auto-adapts |
| Flexibility | 🟢 High | Backend controls rollout |
| Consistency Risk | 🟢 Low | Always in sync |
| Complexity | 🟢 Low | Simple API |
| Drawback | 🟡 Medium | Frontend depends on backend availability for config |

---

## Matrix 5: Additive Schema Changes

### Scenario: Character v1 → v2 (Add "Race" field)

| Operation | Frontend Version | Backend MAX | Entity Current Version | Behavior | Data Integrity |
|-----------|------------------|-------------|------------------------|----------|----------------|
| **Read v1 entity** | v2 Frontend | v2 Backend | 1 | Frontend displays, "Race" field empty | ✅ Safe |
| **Update v1 entity** | v2 Frontend | v2 Backend | 1 → 2 | Frontend sends schemaVersion=2, adds "Race" | ✅ Safe (lazy migration) |
| **Read v2 entity** | v1 Frontend | v2 Backend | 2 | Frontend ignores "Race" field | ✅ Safe (forward compatible) |
| **Update v2 entity** | v1 Frontend | v2 Backend | 2 → ❌ | Frontend sends schemaVersion=1 | ❌ **Rejected: downgrade** |
| **Create new** | v2 Frontend | v2 Backend | N/A → 2 | Frontend sends schemaVersion=2 with "Race" | ✅ Safe |

**Key Insight**: Additive-only changes are forward/backward compatible for reads, but updates must not downgrade.

---

## Matrix 6: Type Conversion Scenarios

### Scenario: Convert Location → Character

| Current Entity | Frontend Request | Backend Validation | Stored Result | Data Loss Risk |
|----------------|------------------|-------------------|---------------|----------------|
| **Location v1** (name, coords) | entityType=Character, schemaVersion=2 | Validate type change allowed | Character v2 (name, race=null, class=null) | 🟡 Medium (coords lost) |
| **Character v2** (name, race, class) | entityType=Location, schemaVersion=1 | Validate type change allowed | Location v1 (name, coords=null) | 🔴 High (race/class lost) |

**Recommendation**: Require explicit type conversion API endpoint with confirmation:

```text
POST /api/v1/worlds/{worldId}/entities/{entityId}/convert
{
  "targetType": "Character",
  "targetSchemaVersion": 2,
  "confirmDataLoss": true
}
```

---

## Final Recommendations

### ✅ Current Approach (FR-007) is BEST for Now

**Rationale**:

1. ✅ Prevents schema version mismatches (Constraint #1)
1. ✅ Prevents rollbacks (Constraint #2)
1. ✅ Frontend explicitly controls version (Constraint #3 - mild coupling)
1. ✅ Supports additive changes (Constraint #4)
1. ✅ Deployment order: Backend first → Frontend (Constraint #6)

### 🚀 Future Enhancement: Shared Schema Config Service

**Phase 1 (Current - FR-007)**:

- Frontend: Hardcoded `ENTITY_SCHEMA_VERSIONS`
- Backend: Hardcoded `SchemaVersionConfiguration`
- Deployment: Backend first, then Frontend

**Phase 2 (6-12 months)**:

- Shared Schema Config API (Azure Table Storage or Cosmos DB)
- Frontend: Fetch config on app load, cache in sessionStorage
- Backend: Fetch config on startup, cache in memory
- Benefits: Decoupled, single source of truth, no code deploy for schema changes

**Phase 3 (12+ months)**:

- Data-driven schema validation (JSON Schema or similar)
- Backend validates entity properties against schema definition
- Frontend generates forms dynamically from schema

### ⚠️ Do NOT Allow Omitted schemaVersion in Updates

**Why**:

- Scenario D (preserve current): Prevents lazy migration, defeats purpose
- Scenario E (upgrade to MAX): High risk of frontend reading entity it can't handle
- FR-007 compliance: Explicit is better than implicit

### 🎯 Deployment Workflow

1. **Backend Deploy**: Update `SchemaVersionConfiguration` MAX version
1. **Validation Period**: 24-48 hours, monitor for errors
1. **Frontend Deploy**: Update `ENTITY_SCHEMA_VERSIONS` current version
1. **Lazy Migration**: Existing entities upgrade on next edit

### 📊 Monitoring Metrics

Track these metrics to detect version drift:

```typescript
// Frontend telemetry
{
  "metric": "schema_version_sent",
  "entityType": "Character",
  "version": 2,
  "operation": "create"
}
```

```csharp
// Backend telemetry
{
  "metric": "schema_version_validation",
  "entityType": "Character",
  "requestedVersion": 2,
  "currentVersion": 1,
  "maxSupportedVersion": 2,
  "result": "accepted"
}
```

### 🔒 Summary Decision Matrix

| Requirement | Current Approach | Alternative (Omitted OK) | Verdict |
|-------------|------------------|-------------------------|---------|
| No mismatches | ✅ Explicit version | ❌ Backend guesses | **Keep Current** |
| No rollbacks | ✅ Backend validates ≥ current | ✅ Backend validates ≥ current | **Either OK** |
| Low coupling | 🟡 Hardcoded config | 🟡 Hardcoded config | **Future: Shared Config** |
| Additive only | ✅ Supports | ✅ Supports | **Either OK** |
| Type conversion | 🟡 Via update | 🟡 Via update | **Future: Dedicated API** |
| Independent deploys | ✅ Backend first | ✅ Backend first | **Either OK** |
| Shared schema store | ❌ Not yet | ❌ Not yet | **Future Enhancement** |

**Conclusion**: Continue with FR-007 (explicit schemaVersion required). Plan migration to shared schema config service in Phase 2.
