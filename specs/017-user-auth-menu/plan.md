# Implementation Plan: User Authentication Mode and User Menu

**Branch**: `017-user-auth-menu` | **Date**: 2026-04-05 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/017-user-auth-menu/spec.md`

## Summary

Add dual-mode authentication (single-user anonymous / multi-user Entra ID) across the full stack, with a user menu dropdown in the header toolbar. The backend detects auth mode from `AzureAd:ClientId` configuration, injects `_anonymous` identity when unconfigured, and filters data by user. The frontend detects auth mode from build-time MSAL client ID injection, conditionally wraps the app in an MSAL provider, and renders a `UserMenu` component with mode-appropriate content. A `/settings` route with dark/light mode toggle is also added.

## Technical Context

**Language/Version**: TypeScript 5.9 (frontend), C# / .NET 10 (backend)
**Primary Dependencies**:

- Frontend: React 19, Redux Toolkit, Shadcn/UI (Radix), TailwindCSS v4, `@azure/msal-browser`, `@azure/msal-react`
- Backend: ASP.NET Core 10, Aspire.NET, EF Core (Cosmos), `Microsoft.Identity.Web`
**Storage**: Azure Cosmos DB (World container `/Id`, WorldEntity container `[/WorldId, /id]`, Asset container `[/WorldId, /EntityId]`)
**Testing**: Vitest + Testing Library + jest-axe (frontend), MSTest + FluentAssertions (backend)
**Target Platform**: Azure (Static Web App + Container Apps), local dev via Aspire
**Project Type**: Web application (React frontend + .NET API backend)
**Performance Goals**: User menu interactive within 2 seconds of page load; no flicker on auth mode detection
**Constraints**: Single-user mode must work with zero auth configuration; health checks always public
**Scale/Scope**: Single user initially; future RBAC for multi-user collaboration

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Cloud-Native Architecture | PASS | Auth config flows through Aspire; Entra ID aligns with Azure-first strategy |
| II. Clean Architecture | PASS | `IUserContextService` already abstracts user identity in domain layer; new implementation swaps in without affecting controllers |
| III. Test-Driven Development | PASS | All new components require `.test.tsx` with jest-axe; backend auth behavior needs unit + integration tests |
| IV. Framework & Technology Standards | PASS | React 19 + TypeScript + Shadcn/UI + Redux Toolkit (frontend); .NET 10 + Aspire + EF Core (backend) |
| V. Developer Experience | PASS | Works in anonymous mode by default; no Entra ID setup required for local dev |
| VI. Security & Privacy | PASS | Entra ID for auth; no hardcoded secrets; data isolation by OwnerId; RBAC deferred to future feature |
| VII. Semantic Versioning | PASS | New feature = MINOR bump; no breaking changes to existing API contracts |

**Constitution note on CSS Modules**: The constitution states "CSS Modules MUST be co-located with components" but the project has already migrated to TailwindCSS per spec 005. The frontend uses `cn()` utility + TailwindCSS classes, not CSS Modules. This is an outdated constitution constraint; no CSS Modules will be created.

## Project Structure

### Documentation (this feature)

```text
specs/017-user-auth-menu/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── auth-api.md      # Auth integration contract
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
# Frontend (libris-maleficarum-app/)
src/
├── auth/
│   └── authConfig.ts              # MSAL configuration + isAuthConfigured flag
├── components/
│   ├── TopToolbar/
│   │   └── TopToolbar.tsx         # MODIFIED: add UserMenu
│   ├── UserMenu/
│   │   ├── UserMenu.tsx           # New: mode-switching user menu
│   │   ├── UserMenu.test.tsx      # New: tests + a11y
│   │   └── index.ts               # New: barrel export
│   ├── AuthGuard/
│   │   ├── AuthGuard.tsx          # New: auth wrapper for data pages
│   │   ├── AuthGuard.test.tsx     # New: tests + a11y
│   │   └── index.ts               # New: barrel export
│   ├── SettingsPage/
│   │   ├── SettingsPage.tsx       # New: /settings route page
│   │   ├── SettingsPage.test.tsx  # New: tests + a11y
│   │   └── index.ts               # New: barrel export
│   └── ui/
│       └── dropdown-menu.tsx      # New: Shadcn dropdown-menu component
├── main.tsx                       # MODIFIED: conditional MSAL provider
└── App.tsx                        # MODIFIED: add /settings route + AuthGuard

# Backend (libris-maleficarum-service/)
src/
├── Api/
│   ├── Program.cs                 # MODIFIED: add auth detection + Entra ID config
│   ├── Extensions/
│   │   └── ClaimsPrincipalExtensions.cs  # New: GetUserIdOrAnonymous()
│   └── appsettings.json           # MODIFIED: add AzureAd section
├── Infrastructure/
│   └── Services/
│       └── UserContextService.cs  # MODIFIED: read from ClaimsPrincipal instead of hardcoded GUID
└── Orchestration/
    └── AppHost/
        └── AppHost.cs             # MODIFIED: pass Entra ID env vars to API + frontend
```

**Structure Decision**: This feature modifies existing files across both frontend and backend. The only new directories are `src/auth/`, `src/components/UserMenu/`, `src/components/AuthGuard/`, `src/components/SettingsPage/`, and `src/Api/Extensions/`. All follow existing naming conventions.

## Complexity Tracking

No constitution violations to justify. All patterns follow existing project structure.
