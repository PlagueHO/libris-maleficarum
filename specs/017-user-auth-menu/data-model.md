# Data Model: User Authentication Mode and User Menu

**Feature**: 017-user-auth-menu
**Date**: 2026-04-05

## Overview

This feature does not introduce new CosmosDB containers or entities. It modifies how existing entities populate their user identity fields (`OwnerId`, `CreatedBy`, `ModifiedBy`, `DeletedBy`) based on the active authentication mode.

## Entities Affected

### World (existing — World container, partition key `/Id`)

| Field | Type | Change | Description |
|-------|------|--------|-------------|
| `OwnerId` | `string` | **Populated from auth** | Previously hardcoded GUID; now set from `IUserContextService` which reads authenticated user or `_anonymous` |

No schema change. The field type is already `string` and accepts any user identifier.

### WorldEntity (existing — WorldEntity container, partition key `[/WorldId, /id]`)

| Field | Type | Change | Description |
|-------|------|--------|-------------|
| `OwnerId` | `string` | **Populated from auth** | Set from authenticated user or `_anonymous` |
| `CreatedBy` | `string?` | **Now populated** | Was always `null` — now set to user ID on entity creation |
| `ModifiedBy` | `string?` | **Now populated** | Was always `null` — now set to user ID on entity update |
| `DeletedBy` | `string?` | **Already populated** | Already set on soft delete — no change needed |

No schema change. Fields already exist in the data model but `CreatedBy` and `ModifiedBy` were not being written.

### Asset (existing — Asset container, partition key `[/WorldId, /EntityId]`)

| Field | Type | Change | Description |
|-------|------|--------|-------------|
| `OwnerId` | `string` | **Populated from auth** | Same as World/WorldEntity |

No schema change.

## User Identity Values

| Auth Mode | `OwnerId` / `CreatedBy` / `ModifiedBy` | Source |
|-----------|----------------------------------------|--------|
| Single-user (anonymous) | `_anonymous` | Synthetic claims principal injected by middleware |
| Multi-user (Entra ID) | Entra ID object ID (GUID string, e.g., `a1b2c3d4-...`) | JWT token `oid` claim via `ClaimsPrincipal` |

## Interface Change

### IUserContextService

```csharp
// BEFORE (current)
public interface IUserContextService
{
    Task<Guid> GetCurrentUserIdAsync();
}

// AFTER
public interface IUserContextService
{
    Task<string> GetCurrentUserIdAsync();
}
```

**Reason**: `_anonymous` is not a valid GUID. The return type must accommodate both string-based user identifiers.

**Impact**: All call sites in controllers currently call `.GetCurrentUserIdAsync()` and pass the result to repository methods. The repositories accept `string` OwnerId (matching the domain model). The main impact is:
- Remove any `.ToString()` calls on the GUID result
- Update the interface, implementation, and test mocks

## Data Isolation Queries

### Single-User Mode

All data has `OwnerId = "_anonymous"`. Queries naturally return all data since there is only one user identity.

### Multi-User Mode

| Query | Filter | Notes |
|-------|--------|-------|
| List user's worlds | `WHERE OwnerId = @currentUserId AND IsDeleted = false` | Already filtered by OwnerId; just needs real user ID |
| Get world by ID | Point read + validate `OwnerId` matches caller | Authorization check |
| List world entities | Scoped by WorldId (which is owned by user) | Transitively secure via World ownership |
| Entity CRUD | Validate World ownership before any entity operation | Same as current pattern |

## Frontend Auth State

No Redux slice needed for auth state. MSAL React manages auth state internally via `useMsal()`, `useIsAuthenticated()`, and `useAccount()` hooks. The `isAuthConfigured` flag is a build-time constant from `authConfig.ts`.

| State | Source | Scope |
|-------|--------|-------|
| Auth mode (configured?) | `isAuthConfigured` from `auth/authConfig.ts` | Build-time constant |
| Is signed in? | `useIsAuthenticated()` from `@azure/msal-react` | MSAL managed |
| User info (name, email) | `useMsal().accounts[0]` from `@azure/msal-react` | MSAL managed |
| Theme preference | Existing `useTheme()` hook | Local storage |
