# Frontend-Backend Compatibility Report

## Async Entity Operations Feature (Spec 012)

**Report Date**: February 3, 2026  
**Frontend Feature**: Spec 012 - Async Entity Operations with Notification Center  
**Backend Feature**: Spec 011 - Soft Delete World Entities API  
**Status**: ⚠️ **SIGNIFICANT INCOMPATIBILITIES IDENTIFIED**

---

## Executive Summary

The frontend async operations feature (spec 012) and backend soft-delete implementation (spec 011) have **significant endpoint and contract mismatches** that will block integration. The backend was implemented to spec 011's synchronous-style API contract (202 Accepted with polling), while the frontend was built assuming a different endpoint structure and additional capabilities (retry/cancel).

**Severity**: **HIGH** - Integration is blocked without backend modifications or frontend adapter layer.

**Critical Finding**: Backend delete IS async (returns 202 Accepted, processes in background), which aligns with frontend expectations. However, endpoint paths, HTTP methods, URL structures, and available operations differ significantly.

---

## Endpoint Analysis

### ✅ Compatible Endpoints (With Mapping)

| Frontend Expectation | Backend Implementation | Compatibility |
|---------------------|------------------------|---------------|
| GET operations list | GET `/api/v1/worlds/{worldId}/delete-operations?limit=20` | ⚠️ **Needs URL mapping** |
| GET single operation | GET `/api/v1/worlds/{worldId}/delete-operations/{operationId}` | ⚠️ **Needs URL mapping** |

### ❌ Missing Endpoints

**Frontend expects but backend does NOT provide:**

1. **POST `/api/async-operations/{id}/retry`**
   - Frontend: `retryAsyncOperation` mutation
   - Backend: **NOT IMPLEMENTED**
   - Impact: **HIGH** - Users cannot retry failed operations (FR-014)
   - Resolution: Backend must implement retry endpoint

1. **POST `/api/async-operations/{id}/cancel`**
   - Frontend: `cancelAsyncOperation` mutation
   - Backend: **NOT IMPLEMENTED**
   - Impact: **MEDIUM** - Users cannot cancel in-progress operations
   - Resolution: Backend must implement cancel endpoint OR frontend removes feature

### ⚠️ Needs Adaptation

**POST initiate delete:**

| Aspect | Frontend (Spec 012) | Backend (Spec 011) | Resolution |
|--------|--------------------|--------------------|-----------|
| URL | `/api/world-entities/{id}/async-delete` | `/api/v1/worlds/{worldId}/entities/{entityId}` | Frontend adapter layer |
| Method | `POST` | `DELETE` | Frontend adapter layer |
| Parameters | `entityId` only | `worldId` + `entityId` + optional `cascade` query param | Frontend must provide worldId |
| Response | `{ operationId, targetEntityId, targetEntityName, estimatedCount }` | `{ data: DeleteOperationResponse }` with full operation details | Need response mapping |

**GET operations list:**

| Aspect | Frontend (Spec 012) | Backend (Spec 011) | Resolution |
|--------|--------------------|--------------------|-----------|
| URL | `/api/async-operations` | `/api/v1/worlds/{worldId}/delete-operations` | Frontend adapter layer |
| Filters | `status`, `type`, `limit` | `limit` only | Frontend filters client-side OR backend adds filters |
| Scope | User-scoped (all worlds) | World-scoped | Frontend must iterate worlds OR backend adds user-scoped endpoint |

**GET single operation:**

| Aspect | Frontend (Spec 012) | Backend (Spec 011) | Resolution |
|--------|--------------------|--------------------|-----------|
| URL | `/api/async-operations/{operationId}` | `/api/v1/worlds/{worldId}/delete-operations/{operationId}` | Frontend adapter layer |
| Parameters | `operationId` only | `worldId` + `operationId` | Frontend must provide worldId |

---

## Data Model Compatibility

### Type Mappings Required

| Frontend Type | Backend Type | Field Name | Mapping Needed |
|--------------|--------------|------------|----------------|
| `AsyncOperation.id` | `DeleteOperationResponse.Id` | ✅ Match | None |
| `AsyncOperation.type` | N/A | ❌ Not in backend | Frontend sets to `'DELETE'` |
| `AsyncOperation.targetEntityId` | `DeleteOperationResponse.RootEntityId` | ⚠️ Different name | Map `RootEntityId` → `targetEntityId` |
| `AsyncOperation.targetEntityName` | `DeleteOperationResponse.RootEntityName` | ⚠️ Different name | Map `RootEntityName` → `targetEntityName` |
| `AsyncOperation.targetEntityType` | N/A | ❌ Not in backend | Frontend must derive or omit |
| `AsyncOperation.status` | `DeleteOperationResponse.Status` | ⚠️ Different values | See status mapping below |
| `AsyncOperation.progress` | Computed from backend fields | ❌ Structure mismatch | See progress mapping below |
| `AsyncOperation.result` | Computed from backend fields | ❌ Structure mismatch | See result mapping below |
| `AsyncOperation.startTimestamp` | `DeleteOperationResponse.CreatedAt` | ⚠️ Different semantics | CreatedAt ≠ StartedAt |
| `AsyncOperation.completionTimestamp` | `DeleteOperationResponse.CompletedAt` | ✅ Match | None |

### Status Value Mapping

| Frontend Status | Backend Status | Mapping Logic |
|----------------|---------------|---------------|
| `'pending'` | `"pending"` | ✅ Direct match |
| `'in-progress'` | `"in_progress"` | ✅ Direct match |
| `'completed'` | `"completed"` | ✅ Direct match |
| `'failed'` | `"failed"` OR `"partial"` | ⚠️ Backend has `"partial"` status frontend doesn't handle |
| `'cancelled'` | N/A | ❌ Backend doesn't support cancellation |

**Issue**: Backend has `"partial"` status (some entities failed, some succeeded). Frontend only has `'failed'` and `'completed'`.

**Resolution**: Frontend should map `"partial"` → `'failed'` with partial success indicator in result.

### Progress Mapping

**Frontend expects:**

```typescript
interface AsyncOperationProgress {
  percentComplete: number;      // 0-100
  itemsProcessed: number;
  itemsTotal: number;
  currentItem?: string;
}
```

**Backend provides:**

```csharp
public class DeleteOperationResponse {
  public int TotalEntities { get; set; }     // itemsTotal
  public int DeletedCount { get; set; }      // itemsProcessed
  public int FailedCount { get; set; }       // Not in frontend type
}
```

**Mapping logic:**

```typescript
progress: {
  percentComplete: (DeletedCount / TotalEntities) * 100,
  itemsProcessed: DeletedCount,
  itemsTotal: TotalEntities,
  currentItem: null  // Backend doesn't provide this
}
```

### Result Mapping

**Frontend expects:**

```typescript
interface AsyncOperationResult {
  success: boolean;
  affectedCount: number;
  errorMessage?: string;
  errorDetails?: { code: string; failedAtItem?: string; stackTrace?: string };
  retryCount: number;
}
```

**Backend provides:**

```csharp
public class DeleteOperationResponse {
  public string Status { get; set; }          // "completed"/"partial"/"failed"
  public int DeletedCount { get; set; }       // affectedCount
  public int FailedCount { get; set; }
  public List<Guid>? FailedEntityIds { get; set; }
  public string? ErrorDetails { get; set; }   // errorMessage
}
```

**Mapping logic:**

```typescript
result: {
  success: Status === 'completed',
  affectedCount: DeletedCount,
  errorMessage: ErrorDetails,
  errorDetails: FailedCount > 0 ? {
    code: 'PARTIAL_FAILURE',
    failedAtItem: FailedEntityIds?.[0]?.toString()
  } : undefined,
  retryCount: 0  // Backend doesn't track this
}
```

---

## Critical Issues

### 1. **URL Structure Incompatibility**

**Impact**: **BLOCKER**

**Description**: Frontend expects flat URL structure without `worldId` parameter, backend requires `worldId` in path.

**Frontend**:

```typescript
GET /api/async-operations
GET /api/async-operations/{operationId}
POST /api/world-entities/{entityId}/async-delete
```

**Backend**:

```
GET /api/v1/worlds/{worldId}/delete-operations
GET /api/v1/worlds/{worldId}/delete-operations/{operationId}
DELETE /api/v1/worlds/{worldId}/entities/{entityId}
```

**Resolution Options**:

**Option A**: Frontend adapter layer (RECOMMENDED)

- Create API adapter in frontend that maps URLs and injects `worldId`
- Frontend must track current `worldId` in Redux state
- Minimal backend changes

**Option B**: Backend adds user-scoped endpoints

- Add `/api/v1/async-operations` that queries across all user's worlds
- Add `/api/v1/async-operations/{operationId}` that looks up world automatically
- More backend work, cleaner frontend

**Recommendation**: **Option A** - Less backend disruption, frontend already has world context.

---

### 2. **HTTP Method Mismatch for Delete Initiation**

**Impact**: **BLOCKER**

**Description**: Frontend uses POST method, backend uses DELETE method.

**Frontend**:

```typescript
initiateAsyncDelete: builder.mutation<...>({
  query: (entityId) => ({
    url: `/api/world-entities/${entityId}/async-delete`,
    method: 'POST',  // ← Expects POST
  }),
})
```

**Backend**:

```csharp
[HttpDelete("{entityId:guid}")]  // ← Uses DELETE
public async Task<IActionResult> DeleteEntity(...)
```

**Resolution Options**:

**Option A**: Frontend changes to DELETE method

- Change `method: 'POST'` to `method: 'DELETE'`
- Aligns with RESTful semantics (DELETE for delete operation)
- **RECOMMENDED**

**Option B**: Backend adds POST endpoint

- Add `[HttpPost("{entityId:guid}/async-delete")]` route
- Duplicates logic, maintains two endpoints

**Recommendation**: **Option A** - Frontend change is trivial, RESTful DELETE makes more semantic sense.

---

### 3. **Missing Retry/Cancel Endpoints**

**Impact**: **HIGH** (Retry), **MEDIUM** (Cancel)

**Description**: Frontend implements retry and cancel features, but backend doesn't provide these endpoints.

**Frontend**:

```typescript
retryAsyncOperation: builder.mutation<AsyncOperation, string>({
  query: (operationId) => ({
    url: `/api/async-operations/${operationId}/retry`,
    method: 'POST',
  }),
}),

cancelAsyncOperation: builder.mutation<AsyncOperation, string>({
  query: (operationId) => ({
    url: `/api/async-operations/${operationId}/cancel`,
    method: 'POST',
  }),
}),
```

**Backend**: ❌ NOT IMPLEMENTED

**Resolution Options**:

**Option A**: Backend implements retry/cancel (RECOMMENDED for retry)

- Add retry endpoint that re-initiates failed operation from checkpoint
- Add cancel endpoint that marks operation as cancelled
- Aligns with spec 012 FR-014

**Option B**: Frontend removes retry/cancel features

- Remove UI buttons and mutations
- Simpler but degrades UX
- **ONLY for cancel** (retry is critical per FR-014)

**Recommendation**:

- **Retry**: Backend MUST implement (`POST /api/v1/worlds/{worldId}/delete-operations/{operationId}/retry`)
- **Cancel**: Frontend can remove cancel feature for initial release

---

### 4. **Operation Type Field Missing**

**Impact**: **MEDIUM**

**Description**: Frontend expects `type` field (DELETE, CREATE, UPDATE, etc.) for extensibility, backend only handles deletes.

**Frontend**:

```typescript
interface AsyncOperation {
  type: AsyncOperationType;  // 'DELETE' | 'CREATE' | 'UPDATE' | 'IMPORT' | 'EXPORT'
}
```

**Backend**:

- DeleteOperation entity has no `type` field (only deletes supported)

**Resolution**: Frontend hardcodes `type: 'DELETE'` in response mapping for now. Backend can add type field when supporting other async operations.

---

### 5. **Partial Status Not Handled by Frontend**

**Impact**: **LOW**

**Description**: Backend has `"partial"` status for operations that partially succeeded. Frontend only handles `'completed'` and `'failed'`.

**Backend statuses**: `pending`, `in_progress`, `completed`, `partial`, `failed`  
**Frontend statuses**: `pending`, `in-progress`, `completed`, `failed`, `cancelled`

**Resolution**: Frontend maps `"partial"` → `'failed'` status but shows partial success in result details (affectedCount shows how many succeeded).

---

### 6. **EstimatedCompletion Field Missing**

**Impact**: **LOW**

**Description**: Spec 012 contract includes `estimatedCompletion` timestamp field, but backend doesn't provide it.

**Frontend contract (spec 012)**:

```yaml
estimatedCompletion:
  type: string
  format: date-time
  nullable: true
  description: Estimated completion timestamp (optional)
```

**Backend**: Does not implement estimated completion time.

**Resolution**: Frontend sets `estimatedCompletion: null` for all operations. Feature can be added to backend later.

---

## Error Handling Alignment

### ✅ Compatible

Both frontend and backend use **RFC 7807 Problem Details** format:

**Frontend expects**:

```typescript
interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
}
```

**Backend returns** (from spec 011):

```json
{
  "error": {
    "code": "ENTITY_NOT_FOUND",
    "message": "Entity not found"
  }
}
```

**Issue**: Backend uses custom `ErrorResponse` wrapper, not standard RFC 7807.

**Resolution**: Backend should migrate to RFC 7807 Problem Details OR frontend adapts to backend's error format.

---

## Authentication & Authorization

### ✅ Assumed Compatible

**Frontend**: Uses Axios client with interceptors for auth token injection (assumed)  
**Backend**: Validates world ownership via `IUserContextService` and `IWorldRepository`

**Assumption**: Auth tokens are passed via `Authorization: Bearer {token}` header (standard pattern).

**Action Required**: Verify auth token flow during integration testing.

---

## Integration Checklist

### Priority 1 (Blocking Integration)

- [ ] **Backend**: Implement retry endpoint  
  `POST /api/v1/worlds/{worldId}/delete-operations/{operationId}/retry`  
  Returns: Updated `DeleteOperationResponse` with status reset to `pending`

- [ ] **Frontend**: Create API adapter layer  
  - Maps frontend URLs → backend URLs
  - Injects `worldId` from Redux state
  - Maps backend responses → frontend types

- [ ] **Frontend**: Change delete initiation HTTP method  
  `POST` → `DELETE`

- [ ] **Frontend**: Update API base path  
  `/api/` → `/api/v1/` (if using absolute paths)

### Priority 2 (Production Readiness)

- [ ] **Frontend**: Implement response mapping utility  
  Maps `DeleteOperationResponse` → `AsyncOperation` with all field transformations

- [ ] **Frontend**: Handle `partial` status  
  Map to `'failed'` with partial success indicator in result

- [ ] **Backend**: Add `type` field to DeleteOperation entity  
  - Add `string Type { get; set; }` property
  - Default to `"DELETE"` for backwards compatibility
  - Include in DTO mapping

- [ ] **Frontend**: Client-side filtering for operation lists  
  Since backend only supports `limit`, implement status/type filters in frontend

- [ ] **Integration Test**: Verify auth token flow  
  Ensure Bearer token is correctly passed and validated

### Priority 3 (Nice-to-Have)

- [ ] **Backend**: Add cancel endpoint  
  `POST /api/v1/worlds/{worldId}/delete-operations/{operationId}/cancel`  
  Sets status to `cancelled`, stops processing

- [ ] **Backend**: Add user-scoped operation list endpoint  
  `GET /api/v1/async-operations` (queries all worlds for current user)

- [ ] **Backend**: Add estimated completion time calculation  
  Based on entity count and average processing time

- [ ] **Backend**: Migrate to RFC 7807 Problem Details  
  Replace `ErrorResponse` with standard `ProblemDetails` format

---

## Recommendations

### Immediate Actions (Before Integration)

1. **Backend Team**:
   - ✅ Implement retry endpoint (Priority 1)
   - ✅ Add `type` field to DeleteOperation entity/DTO
   - ⏸️ Defer cancel endpoint to Phase 2 (frontend can remove feature)

1. **Frontend Team**:
   - ✅ Implement API adapter layer in `src/services/asyncOperationsAdapter.ts`
   - ✅ Change HTTP method to DELETE for initiate delete
   - ✅ Implement response mapping utility
   - ✅ Handle `partial` status as failed with partial success indicator
   - ✅ Remove cancel feature from UI (defer to Phase 2)

### Short-Term (Post-Integration)

1. Add integration tests verifying:
   - URL mapping works correctly
   - Response mapping preserves all data
   - Polling updates work with actual backend
   - Error handling for all error codes

1. Performance testing:
   - Verify polling interval (3 seconds) doesn't overload backend
   - Test with 10+ concurrent operations
   - Measure time to visual update after backend state change

### Long-Term (Future Enhancements)

1. **Backend**: Add user-scoped endpoints to avoid worldId in frontend URLs
1. **Backend**: Implement cancel operation support
1. **Backend**: Add estimated completion time
1. **Both**: Migrate to Server-Sent Events or WebSocket for push-based updates (remove polling)

---

## Example: Frontend API Adapter

Here's a recommended implementation pattern:

```typescript
// src/services/adapters/asyncOperationsAdapter.ts

import type { DeleteOperationResponse } from './types/backend';
import type { AsyncOperation } from './types/asyncOperations';
import { useSelector } from 'react-redux';
import { selectCurrentWorldId } from '@/store/worldSlice';

/**
 * Maps backend DeleteOperationResponse to frontend AsyncOperation
 */
export function mapDeleteOperationToAsyncOperation(
  backendOp: DeleteOperationResponse
): AsyncOperation {
  const percentComplete =
    backendOp.TotalEntities > 0
      ? (backendOp.DeletedCount / backendOp.TotalEntities) * 100
      : 0;

  return {
    id: backendOp.Id,
    type: 'DELETE', // Hardcoded for now
    targetEntityId: backendOp.RootEntityId,
    targetEntityName: backendOp.RootEntityName,
    targetEntityType: 'Unknown', // Backend doesn't provide this
    status: mapBackendStatus(backendOp.Status),
    progress:
      backendOp.Status === 'in_progress' || backendOp.Status === 'pending'
        ? {
            percentComplete,
            itemsProcessed: backendOp.DeletedCount,
            itemsTotal: backendOp.TotalEntities,
          }
        : null,
    result:
      backendOp.Status === 'completed' ||
      backendOp.Status === 'failed' ||
      backendOp.Status === 'partial'
        ? {
            success: backendOp.Status === 'completed',
            affectedCount: backendOp.DeletedCount,
            errorMessage: backendOp.ErrorDetails,
            errorDetails:
              backendOp.FailedCount > 0
                ? {
                    code: 'PARTIAL_FAILURE',
                    failedAtItem: backendOp.FailedEntityIds?.[0],
                  }
                : undefined,
            retryCount: 0, // Backend doesn't track this
          }
        : null,
    startTimestamp: backendOp.CreatedAt, // Note: CreatedAt ≠ StartedAt
    completionTimestamp: backendOp.CompletedAt,
  };
}

function mapBackendStatus(
  backendStatus: string
): AsyncOperationStatus {
  if (backendStatus === 'partial') {
    return 'failed'; // Map partial to failed with result.success = false
  }
  return backendStatus as AsyncOperationStatus;
}

/**
 * Builds backend URL from frontend parameters
 */
export function buildBackendUrl(
  frontendUrl: string,
  worldId: string
): string {
  // Transform frontend URLs to backend URLs
  if (frontendUrl.startsWith('/api/async-operations')) {
    const operationId = frontendUrl.split('/')[3];
    if (operationId) {
      return `/api/v1/worlds/${worldId}/delete-operations/${operationId}`;
    }
    return `/api/v1/worlds/${worldId}/delete-operations`;
  }

  if (frontendUrl.includes('/async-delete')) {
    const entityId = frontendUrl.split('/')[3];
    return `/api/v1/worlds/${worldId}/entities/${entityId}`;
  }

  return frontendUrl;
}
```

---

## Conclusion

✅ **Backend delete IS async** - Both systems align on async processing pattern (202 Accepted → polling).

❌ **Significant endpoint and contract mismatches** - URL structures, HTTP methods, and response shapes differ.

⚠️ **Integration is BLOCKED** without implementing:

1. Backend retry endpoint (Priority 1)
1. Frontend API adapter layer (Priority 1)
1. Frontend HTTP method change to DELETE (Priority 1)

**Estimated Effort**:

- Backend retry endpoint: **4-8 hours** (implementation + tests)
- Frontend adapter layer: **8-16 hours** (implementation + mapping + tests)
- Integration testing: **4-8 hours**
- **Total**: ~2-4 days of focused work

**Recommendation**: Implement Priority 1 items immediately. Defer Priority 2+ to post-integration phase.

---

**Next Steps**:

1. [ ] Backend team confirms retry endpoint design and timeline
1. [ ] Frontend team implements adapter layer as spec'd above
1. [ ] Both teams align on integration test plan
1. [ ] Schedule integration verification session after Priority 1 items complete

---

**Report Version**: 1.0  
**Generated**: February 3, 2026  
**Authors**: GitHub Copilot AI Agent  
**Review Required**: Backend Lead + Frontend Lead
