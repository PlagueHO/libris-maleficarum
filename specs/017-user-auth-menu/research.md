# Research: User Authentication Mode and User Menu

**Feature**: 017-user-auth-menu
**Date**: 2026-04-05

## R1: Authentication Mode Detection — Backend

**Decision**: Detect auth mode by checking `AzureAd:ClientId` in app configuration. If present and non-empty, enable Entra ID JWT authentication. Otherwise, run in anonymous single-user mode.

**Rationale**: This is the exact pattern used in `PlagueHO/prompt-babbler` (see `Program.cs`). It requires zero additional configuration flags — the presence of the Entra ID app registration details is the switch. The `AzureAd:*` configuration section follows the standard `Microsoft.Identity.Web` convention.

**Alternatives considered**:
- Explicit `AuthMode` toggle (e.g., `AuthMode:Enabled = true`): Rejected because it adds a redundant config value that must be kept in sync with the actual Entra ID configuration.
- Environment variable `AUTH_MODE=single|multi`: Rejected for the same reason — the Entra ID client ID presence is sufficient and self-documenting.

**Implementation pattern (from prompt-babbler)**:
```csharp
var azureAdClientId = builder.Configuration["AzureAd:ClientId"];
var isAuthEnabled = !string.IsNullOrEmpty(azureAdClientId);

if (isAuthEnabled)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));
    builder.Services.AddAuthorization();
}
else
{
    // Anonymous mode: allow all requests, inject _anonymous identity via middleware
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer();
    builder.Services.AddAuthorization(options =>
    {
        options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true)
            .Build();
        options.FallbackPolicy = null;
    });
}
```

---

## R2: Anonymous Identity Injection — Backend

**Decision**: In single-user mode, inject a synthetic `ClaimsPrincipal` via middleware containing the object ID `_anonymous` and the expected scope. This allows all controller code to use the same `ClaimsPrincipal` extension methods regardless of auth mode.

**Rationale**: Controllers already use `IUserContextService.GetCurrentUserIdAsync()` to get the current user. The updated `UserContextService` will read from `HttpContext.User` claims. In anonymous mode, the middleware injects claims so the same code path works in both modes.

**Alternatives considered**:
- Keep using `IUserContextService` with a stub returning `_anonymous` string: Viable but doesn't support the multi-user path. The claims-based approach unifies both modes.
- Check auth mode in every controller action: Rejected — duplicates logic across all controllers and is error-prone.

**Key change**: The current `IUserContextService` returns `Task<Guid>`. This must change to `Task<string>` because `_anonymous` is not a GUID. The `OwnerId` field type in the domain model is already `string`, so this is compatible.

---

## R3: Authentication Mode Detection — Frontend

**Decision**: Detect auth mode by checking whether MSAL client ID is injected at Vite build time via `define`. Use the same pattern as prompt-babbler: `__MSAL_CLIENT_ID__` declared via `define` in `vite.config.ts`, sourced from Aspire-injected environment variables.

**Rationale**: Aspire AppHost sets environment variables on the frontend process. Vite's `define` makes them available at build time. If the MSAL client ID is empty/undefined, auth is not configured.

**Alternatives considered**:
- Runtime API call to check auth mode: Rejected — adds a bootstrap dependency and loading state before the app can render.
- Environment variable checked at runtime (`VITE_AUTH_ENABLED`): Viable but redundant when the MSAL client ID presence is sufficient.

**Implementation pattern**:
```typescript
// vite.config.ts define additions
'__MSAL_CLIENT_ID__': JSON.stringify(process.env.ENTRA_CLIENT_ID || ''),
'__MSAL_TENANT_ID__': JSON.stringify(process.env.ENTRA_TENANT_ID || ''),

// auth/authConfig.ts
const clientId = typeof __MSAL_CLIENT_ID__ !== 'undefined' ? __MSAL_CLIENT_ID__ : '';
export const isAuthConfigured = !!clientId;
```

---

## R4: MSAL Integration — Frontend

**Decision**: Use `@azure/msal-browser` and `@azure/msal-react` for Entra ID authentication. Conditionally wrap the app with `<MsalProvider>` only when auth is configured. Initialize the MSAL instance before rendering.

**Rationale**: MSAL React is the official Microsoft library for Entra ID in React apps. The prompt-babbler pattern of conditional wrapping ensures the app works without MSAL when not configured (no import errors, no provider requirement).

**Alternatives considered**:
- Always import MSAL and use a no-op provider: Rejected — adds unnecessary bundle size in single-user mode.
- Custom OAuth implementation: Rejected — MSAL handles token lifecycle, refresh, and popup/redirect flows.

**Key pattern (from prompt-babbler `main.tsx`)**:
```typescript
function renderApp() {
  const app = isAuthConfigured ? (
    <MsalProvider instance={msalInstance}>
      <App />
    </MsalProvider>
  ) : (
    <App />
  );
  createRoot(document.getElementById('root')!).render(<StrictMode>{app}</StrictMode>);
}

if (isAuthConfigured) {
  msalInstance.initialize().then(renderApp);
} else {
  renderApp();
}
```

---

## R5: User Menu Component — Frontend

**Decision**: Create a `UserMenu` component using Shadcn/UI `DropdownMenu` (Radix UI primitive). The component renders three variants based on auth state: `AnonymousUserMenu`, `AuthenticatedUserMenu` (signed in), and unauthenticated sign-in prompt.

**Rationale**: Shadcn/UI `DropdownMenu` provides keyboard navigation, ARIA attributes, and focus management out of the box. The three-variant pattern matches the prompt-babbler implementation and covers all auth states.

**New dependency**: `dropdown-menu.tsx` must be added to `src/components/ui/` via `npx shadcn@latest add dropdown-menu`. This component is not yet installed.

**Alternatives considered**:
- Custom dropdown: Rejected — would need to reimplement keyboard navigation and ARIA from scratch.
- Use existing `select.tsx` or `dialog.tsx`: Rejected — wrong semantic for a user menu.

---

## R6: UserContextService Changes — Backend

**Decision**: Change `IUserContextService.GetCurrentUserIdAsync()` return type from `Task<Guid>` to `Task<string>`. Update the implementation to read from `HttpContext.User` claims via `IHttpContextAccessor`. In anonymous mode, the injected claims contain `_anonymous`. In authenticated mode, the claims contain the Entra ID object ID.

**Rationale**: The current interface returns `Guid`, but `_anonymous` is a string. The domain model already uses `string` for `OwnerId`, so the interface was misaligned. All controller code calls `.GetCurrentUserIdAsync()` — the change is localized to the interface, implementation, and call sites that convert to `Guid`.

**Migration impact**: All controller code already passes the result to repository methods that accept `string` OwnerId. The main change is removing `.ToString()` calls if any exist.

**Alternatives considered**:
- Use a well-known GUID for anonymous (e.g., `00000000-0000-0000-0000-00000000ANON`): Rejected — `_anonymous` is more readable and matches the prompt-babbler convention.
- Keep `Guid` return type and use a constant GUID for anonymous: Rejected — loses the semantic of `_anonymous` when inspecting Cosmos DB data.

---

## R7: Settings Page — Frontend

**Decision**: Add a `/settings` route with a basic `SettingsPage` component containing the existing `ThemeToggle` component (dark/light mode). The settings page will use standard page layout patterns consistent with the app.

**Rationale**: The user menu needs a settings destination. Currently the theme toggle lives in the header toolbar. The settings page centralizes user preferences and provides room for future settings (language, notification preferences, etc.).

**Alternatives considered**:
- Dialog-based settings: Rejected — `/settings` route is more typical for React apps and supports deep linking.
- Keep settings only in the header: Rejected — the user menu needs a settings link target.

---

## R8: Data Isolation — Backend

**Decision**: In multi-user mode, API queries filter results by the authenticated user's `OwnerId`. In single-user mode, all data has `OwnerId = _anonymous` so the same filter works naturally. Cross-user access (RBAC) is deferred to a future feature.

**Rationale**: The World repository already queries by `OwnerId` in the `ListWorldsAsync` method. WorldEntity queries are scoped by `WorldId`, which is owned by a user. The data isolation boundary is at the World level.

**Alternatives considered**:
- No filtering (all users see everything): Rejected — violates data isolation requirement.
- Per-entity RBAC: Deferred — adds significant complexity without immediate need.

---

## R9: Aspire AppHost Configuration Propagation

**Decision**: The AppHost passes Entra ID configuration to the API via `.WithEnvironment()` for `AzureAd__ClientId`, `AzureAd__TenantId`, etc. (ASP.NET Core's `__` environment variable separator maps to `:` in configuration). For the frontend, pass `ENTRA_CLIENT_ID` and `ENTRA_TENANT_ID` so Vite's `define` can inject them.

**Rationale**: This follows the standard Aspire pattern of injecting configuration via environment variables. The AppHost reads from its own configuration (e.g., `appsettings.json` or user secrets) and forwards to services.

**Source of truth**: The Entra ID values are optional in AppHost configuration. When absent, both services run in anonymous mode automatically.

---

## R10: Health Check Endpoints

**Decision**: Health check endpoints mapped by `app.MapDefaultEndpoints()` (from Aspire ServiceDefaults) remain publicly accessible even in authenticated mode. This is achieved by the Aspire ServiceDefaults health endpoint mappings which do not require authorization, or by explicitly allowing anonymous access on health routes.

**Rationale**: Container orchestrators (Azure Container Apps, Kubernetes) need to probe health endpoints without tokens. The prompt-babbler project explicitly addressed this.
