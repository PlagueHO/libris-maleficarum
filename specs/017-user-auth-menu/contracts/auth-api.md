# Auth Integration Contract

**Feature**: 017-user-auth-menu
**Date**: 2026-04-05

## Backend API Authentication

### Configuration Schema (appsettings.json)

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "",
    "ClientId": "",
    "Audience": "api://libris-maleficarum-api",
    "Scopes": "access_as_user"
  }
}
```

**Auth mode detection**: If `AzureAd:ClientId` is non-empty → multi-user mode. Otherwise → single-user anonymous mode.

### Anonymous Mode Middleware

When auth is not configured, inject synthetic claims on every request:

```text
ClaimsIdentity("Anonymous"):
  - Claim: "http://schemas.microsoft.com/identity/claims/objectidentifier" = "_anonymous"
  - Claim: "scp" = "access_as_user"
```

### Authenticated Mode

JWT bearer tokens validated against Entra ID. Token must include:

- `aud` = `api://libris-maleficarum-api`
- `scp` = `access_as_user`
- `oid` = user's Entra ID object ID

### User Identity Extraction

```csharp
// Extension method on ClaimsPrincipal
string GetUserIdOrAnonymous(this ClaimsPrincipal user)
    => user.GetObjectId() ?? "_anonymous";
```

### Health Check Endpoints

The following endpoints remain publicly accessible regardless of auth mode:

- `GET /health`
- `GET /alive`

These are mapped by `app.MapDefaultEndpoints()` from Aspire ServiceDefaults and must have `.AllowAnonymous()` applied.

---

## Frontend MSAL Configuration

### Build-Time Injection (vite.config.ts)

```typescript
define: {
  '__MSAL_CLIENT_ID__': JSON.stringify(process.env.ENTRA_CLIENT_ID || ''),
  '__MSAL_TENANT_ID__': JSON.stringify(process.env.ENTRA_TENANT_ID || ''),
  // ... existing defines
}
```

### Auth Config Module (auth/authConfig.ts)

```typescript
export const isAuthConfigured: boolean;  // true if __MSAL_CLIENT_ID__ is non-empty
export const msalInstance: PublicClientApplication;
export const loginRequest: { scopes: string[] };  // ['api://libris-maleficarum-api/access_as_user']
```

### Conditional MSAL Provider (main.tsx)

```text
If isAuthConfigured:
  Initialize msalInstance → wrap <App /> in <MsalProvider>
Else:
  Render <App /> directly (no MSAL)
```

---

## Aspire AppHost Environment Variables

### To API Service

| Variable | Value Source | Maps To |
|----------|-------------|---------|
| `AzureAd__ClientId` | AppHost config / user secrets | `AzureAd:ClientId` |
| `AzureAd__TenantId` | AppHost config / user secrets | `AzureAd:TenantId` |
| `AzureAd__Audience` | Constant | `AzureAd:Audience` |

### To Frontend (Vite)

| Variable | Value Source | Used By |
|----------|-------------|---------|
| `ENTRA_CLIENT_ID` | AppHost config / user secrets | `vite.config.ts` define → `__MSAL_CLIENT_ID__` |
| `ENTRA_TENANT_ID` | AppHost config / user secrets | `vite.config.ts` define → `__MSAL_TENANT_ID__` |

### No Auth Configuration = Anonymous Mode

When AppHost has no Entra ID values in config/secrets:

- API receives empty `AzureAd:ClientId` → anonymous mode
- Frontend receives empty `__MSAL_CLIENT_ID__` → anonymous mode

---

## Component Contracts

### UserMenu Component

```typescript
// No props — reads auth state from MSAL hooks and isAuthConfigured flag
export function UserMenu(): JSX.Element;
```

**Renders**:

- `AnonymousUserMenu` when `!isAuthConfigured`
- `AuthenticatedUserMenu` when `isAuthConfigured` (handles both signed-in and not-signed-in states)

### AuthGuard Component

```typescript
interface AuthGuardProps {
  children: ReactNode;
  message?: string;  // Custom "sign in" prompt message
}

export function AuthGuard({ children, message }: AuthGuardProps): JSX.Element;
```

**Behavior**:

- If `!isAuthConfigured` → render children (bypass)
- If authenticated → render children
- If not authenticated → render sign-in prompt

### SettingsPage Component

```typescript
// No props — standalone page component for /settings route
export function SettingsPage(): JSX.Element;
```

**Contains**: Theme toggle (dark/light mode) using existing `useTheme()` hook.
