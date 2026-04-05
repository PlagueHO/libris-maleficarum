# Quickstart: User Authentication Mode and User Menu

**Feature**: 017-user-auth-menu
**Date**: 2026-04-05

## Prerequisites

- Node.js 20.x + pnpm
- .NET 10 SDK
- Docker Desktop (for Cosmos DB emulator)
- Aspire CLI (`aspire`)

## Run in Single-User Mode (Default)

No additional configuration needed. This is the default.

### Frontend only (with MSW mocks)

```bash
cd libris-maleficarum-app
pnpm install
pnpm dev
```

The app runs at `https://127.0.0.1:4000`. The user menu shows "Anonymous" in the header.

### Full stack (with Aspire)

```bash
cd libris-maleficarum-service
dotnet run --project src/Orchestration/AppHost
```

Both API and frontend start. No Entra ID configuration present → anonymous mode.
The user menu shows "Anonymous". All data is owned by `_anonymous`.

## Run in Multi-User Mode (Entra ID)

### 1. Create Entra ID App Registration

Register an app in Microsoft Entra ID:
- **Name**: libris-maleficarum-api
- **Supported account types**: Single tenant (or your choice)
- **Redirect URI**: `http://localhost:4000` (SPA)
- **Expose an API**: Set Application ID URI to `api://libris-maleficarum-api`, add scope `access_as_user`

### 2. Configure Aspire User Secrets

```bash
cd libris-maleficarum-service/src/Orchestration/AppHost
dotnet user-secrets set "EntraId:ClientId" "<your-client-id>"
dotnet user-secrets set "EntraId:TenantId" "<your-tenant-id>"
```

### 3. Run with Aspire

```bash
cd libris-maleficarum-service
dotnet run --project src/Orchestration/AppHost
```

The AppHost reads user secrets and passes them to the API (`AzureAd:ClientId`, `AzureAd:TenantId`) and frontend (`ENTRA_CLIENT_ID`, `ENTRA_TENANT_ID`).

The user menu shows a "Sign in" option. After signing in, it shows the user's name and initials.

## Run Tests

### Frontend

```bash
cd libris-maleficarum-app
pnpm test                                                    # All tests
pnpm test src/components/UserMenu/UserMenu.test.tsx          # User menu tests
pnpm test src/components/AuthGuard/AuthGuard.test.tsx        # Auth guard tests
pnpm test src/components/SettingsPage/SettingsPage.test.tsx  # Settings page tests
```

### Backend

```bash
cd libris-maleficarum-service
dotnet build LibrisMaleficarum.slnx
dotnet test --solution LibrisMaleficarum.slnx --filter "TestCategory=Unit"
```

## Verify Auth Mode

### Check Backend Mode

Look for startup log messages:
- Anonymous: `"AzureAd:ClientId is not configured. Running in anonymous single-user mode."`
- Authenticated: Standard Microsoft.Identity.Web startup messages

### Check Frontend Mode

Open browser dev console:
- Anonymous: No MSAL-related logs
- Authenticated: MSAL initialization logs from `@azure/msal-browser`

### Check Data Identity

Query Cosmos DB or inspect API responses:
- Anonymous: `ownerId: "_anonymous"` on all records
- Authenticated: `ownerId: "<entra-object-id>"` on records created by that user
