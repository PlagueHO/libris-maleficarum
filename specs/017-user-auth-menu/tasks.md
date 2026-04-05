# Tasks: User Authentication Mode and User Menu

**Input**: Design documents from `/specs/017-user-auth-menu/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/auth-api.md, quickstart.md

**Tests**: Included — constitution mandates TDD and plan.md specifies test files for all new components.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Install dependencies and add shared UI components needed by multiple stories

- [X] T001 Install `@azure/msal-browser` and `@azure/msal-react` packages in `libris-maleficarum-app/`
- [X] T001b [P] Install `react-router-dom` package in `libris-maleficarum-app/`
- [X] T002 [P] Add Shadcn dropdown-menu component via `npx shadcn@latest add dropdown-menu` generating `libris-maleficarum-app/src/components/ui/dropdown-menu.tsx`
- [X] T003 [P] Add `Microsoft.Identity.Web` and `Microsoft.Identity.Web.UI` package references to `libris-maleficarum-service/Directory.Packages.props` and add `PackageReference` to `libris-maleficarum-service/src/Api/LibrisMaleficarum.Api.csproj`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Interface changes, extension methods, and configuration modules that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Change `IUserContextService.GetCurrentUserIdAsync()` return type from `Task<Guid>` to `Task<string>` in `libris-maleficarum-service/src/Domain/Interfaces/Services/IUserContextService.cs`
- [X] T005 Update all call sites for `Guid` → `string` user ID change: (a) `IAssetRepository` — 5 methods with `Guid userId` parameter in `src/Domain/Interfaces/Repositories/IAssetRepository.cs`, (b) `UnauthorizedWorldAccessException` — constructor `Guid userId` and property `Guid? UserId` in `src/Domain/Exceptions/UnauthorizedWorldAccessException.cs`, (c) corresponding implementations in `src/Infrastructure/Repositories/AssetRepository.cs`, (d) `IDeleteOperationRepository.CountActiveByUserAsync` already uses `string userId` — verify no regression, (e) all test mocks returning `Guid` from `IUserContextService` across `libris-maleficarum-service/tests/`
- [X] T005b [US6] Change `World.OwnerId` from `Guid` to `string`, update `World.Create()` signature from `Guid ownerId` to `string ownerId`, and update all `World` repository interfaces/implementations that reference `Guid ownerId` in `libris-maleficarum-service/src/Domain/Entities/World.cs` and `libris-maleficarum-service/src/Domain/Interfaces/Repositories/IWorldRepository.cs`
- [X] T007 [P] Add `AzureAd` configuration section with empty `Instance`, `TenantId`, `ClientId`, `Audience`, and `Scopes` values to `libris-maleficarum-service/src/Api/appsettings.json`
- [X] T008 [P] Create `authConfig.ts` with MSAL `PublicClientApplication` instance, `isAuthConfigured` flag (checks `__MSAL_CLIENT_ID__`), and `loginRequest` (scopes: `api://libris-maleficarum-api/access_as_user`) in `libris-maleficarum-app/src/auth/authConfig.ts`
- [X] T009 [P] Add `__MSAL_CLIENT_ID__` and `__MSAL_TENANT_ID__` to the Vite `define` block, reading from `process.env.ENTRA_CLIENT_ID` and `process.env.ENTRA_TENANT_ID` in `libris-maleficarum-app/vite.config.ts`
- [X] T010 [P] Add TypeScript global type declarations for `__MSAL_CLIENT_ID__` and `__MSAL_TENANT_ID__` as `string` in `libris-maleficarum-app/src/vite-env.d.ts`
- [X] T010b Wrap the app root in `<BrowserRouter>` in `libris-maleficarum-app/src/main.tsx` (or `App.tsx`) to enable URL-based routing for `/settings` and future routes

**Checkpoint**: Foundation ready — user story implementation can now begin

---

## Phase 3: User Story 3 — Backend Auth Mode Detection (Priority: P1)

**Goal**: The API detects auth mode from `AzureAd:ClientId` configuration and either requires JWT bearer tokens (multi-user) or injects anonymous claims (single-user).

**Independent Test**: Start API without `AzureAd:ClientId` → requests succeed without tokens, user identity is `_anonymous`. Start with `AzureAd:ClientId` → unauthenticated requests get 401.

### Tests for User Story 3

- [X] T011 [P] [US3] Create unit tests for `GetUserIdOrAnonymous()` extension method (authenticated and anonymous paths) in `libris-maleficarum-service/tests/unit/Api.Tests/Extensions/ClaimsPrincipalExtensionsTests.cs`
- [X] T012 [P] [US3] Create unit tests for anonymous claims middleware verifying synthetic `_anonymous` identity injection in `libris-maleficarum-service/tests/unit/Api.Tests/Middleware/AnonymousClaimsMiddlewareTests.cs`

### Implementation for User Story 3

- [X] T006 [US3] Create `ClaimsPrincipalExtensions.cs` with `GetUserIdOrAnonymous()` extension method returning the `oid` claim value or `_anonymous` in `libris-maleficarum-service/src/Api/Extensions/ClaimsPrincipalExtensions.cs`
- [X] T013 [US3] Add auth mode detection to `Program.cs` — when `AzureAd:ClientId` is present and non-empty, call `AddMicrosoftIdentityWebApiAuthentication()` and add `app.UseAuthentication()` + `app.UseAuthorization()`; otherwise register anonymous middleware in `libris-maleficarum-service/src/Api/Program.cs`
- [X] T014 [US3] Implement `AnonymousClaimsMiddleware` that injects a synthetic `ClaimsPrincipal` with `oid = _anonymous` and `scp = access_as_user` claims on every request in `libris-maleficarum-service/src/Api/Middleware/AnonymousClaimsMiddleware.cs`
- [X] T015 [US3] Ensure health check endpoints (`/health`, `/alive`) mapped by `MapDefaultEndpoints()` remain publicly accessible with `.AllowAnonymous()` in `libris-maleficarum-service/src/Api/Program.cs`
- [X] T016 [US3] Add startup log messages — log warning for anonymous single-user mode when `AzureAd:ClientId` is absent, log info for authenticated mode when present in `libris-maleficarum-service/src/Api/Program.cs`

**Checkpoint**: Backend correctly detects auth mode and processes requests accordingly

---

## Phase 4: User Story 6 — CosmosDB User Identity on Data Records (Priority: P1)

**Goal**: All data records (`World`, `WorldEntity`, `Asset`) have `OwnerId`, `CreatedBy`, `ModifiedBy` populated from the authenticated or anonymous user identity via `IUserContextService`.

**Independent Test**: Create a World via API in anonymous mode → inspect Cosmos DB record, `OwnerId` is `_anonymous`. Create a WorldEntity → `CreatedBy` is `_anonymous`. Modify entity → `ModifiedBy` is `_anonymous`.

### Tests for User Story 6

- [X] T017 [P] [US6] Create unit tests for `UserContextService` reading user ID from `ClaimsPrincipal` via `IHttpContextAccessor` (anonymous claims, authenticated claims, missing claims) in `libris-maleficarum-service/tests/unit/Infrastructure.Tests/Services/UserContextServiceTests.cs`
- [X] T018 [P] [US6] Update existing test mocks for `IUserContextService` to return `string` (e.g., `_anonymous`) instead of `Guid` across all test projects in `libris-maleficarum-service/tests/`

### Implementation for User Story 6

- [X] T019 [US6] Update `UserContextService` to inject `IHttpContextAccessor`, read `ClaimsPrincipal` from `HttpContext.User`, and return user ID using `GetUserIdOrAnonymous()` in `libris-maleficarum-service/src/Infrastructure/Services/UserContextService.cs`
- [X] T020 [US6] Register `IHttpContextAccessor` in DI container via `builder.Services.AddHttpContextAccessor()` in `libris-maleficarum-service/src/Api/Program.cs`
- [X] T020b [US6] Add `CreatedBy` (`string?`) and `ModifiedBy` (`string?`) properties to `WorldEntity` domain entity, update `WorldEntity.Create()` factory to accept and set `createdBy`, and add an `UpdateModifiedBy(string modifiedBy)` method in `libris-maleficarum-service/src/Domain/Entities/WorldEntity.cs`
- [X] T021 [US6] Ensure `CreatedBy` is populated from `IUserContextService` on entity creation and `ModifiedBy` on entity update in repository methods across `libris-maleficarum-service/src/Infrastructure/Repositories/`
- [X] T021b [US6] Verify/implement OwnerId-based query filtering in `IWorldRepository.ListWorldsAsync()` and related methods to ensure multi-user mode returns only the caller's data — review `libris-maleficarum-service/src/Infrastructure/Repositories/WorldRepository.cs` and `WorldEntityRepository.cs`

**Checkpoint**: All data operations correctly populate user identity fields from claims; query filtering enforces data isolation

---

## Phase 5: User Story 2 — User Menu Dropdown in Header (Priority: P1) 🎯 MVP

**Goal**: A fully accessible user menu dropdown appears in the top-right of the header toolbar with identity display, settings link, and mode-appropriate sign-in/sign-out actions.

**Independent Test**: Render the header component — verify dropdown trigger, menu items, keyboard navigation (Tab, Enter, Arrow, Escape), ARIA attributes, and correct display for anonymous state.

### Tests for User Story 2

- [X] T022 [P] [US2] Create `UserMenu.test.tsx` with rendering tests (anonymous mode display, authenticated mode display), keyboard navigation, ARIA attribute assertions, and jest-axe accessibility checks in `libris-maleficarum-app/src/components/UserMenu/UserMenu.test.tsx`
- [X] T023 [P] [US2] Create `SettingsPage.test.tsx` with rendering tests (ThemeToggle present, heading, layout) and jest-axe accessibility checks in `libris-maleficarum-app/src/components/SettingsPage/SettingsPage.test.tsx`

### Implementation for User Story 2

- [X] T024 [US2] Create `UserMenu.tsx` component using Shadcn `DropdownMenu` with three display modes: (1) anonymous (user icon + "Anonymous", disabled sign-in, settings link, Entra ID not configured note per FR-007), (2) authenticated (initials avatar, display name, email, sign-out, settings link per FR-008), and (3) unauthenticated-multi-user (sign-in button, settings link per FR-009) in `libris-maleficarum-app/src/components/UserMenu/UserMenu.tsx`
- [X] T025 [P] [US2] Create barrel export `index.ts` in `libris-maleficarum-app/src/components/UserMenu/index.ts`
- [X] T026 [US2] Create `SettingsPage.tsx` component with page heading, dark/light mode toggle (reusing existing `ThemeToggle` component), and accessible layout in `libris-maleficarum-app/src/components/SettingsPage/SettingsPage.tsx`
- [X] T027 [P] [US2] Create barrel export `index.ts` in `libris-maleficarum-app/src/components/SettingsPage/index.ts`
- [X] T028 [US2] Integrate `UserMenu` into `TopToolbar.tsx` — add to the right side after ThemeToggle and NotificationBell in `libris-maleficarum-app/src/components/TopToolbar/TopToolbar.tsx`
- [X] T029 [US2] Add `/settings` route to `App.tsx` rendering `SettingsPage` component in `libris-maleficarum-app/src/App.tsx`

**Checkpoint**: User menu renders in header with correct content for both modes, keyboard accessible, settings page at /settings

---

## Phase 6: User Story 1 — Anonymous Single-User Mode (Priority: P1)

**Goal**: End-to-end validation that single-user anonymous mode works seamlessly — no Entra ID config needed, user menu shows "Anonymous", all API operations use `_anonymous`.

**Independent Test**: Launch the full app with no Entra ID config → user menu shows "Anonymous Mode", create/read/update world entities, verify all records have `OwnerId = _anonymous`.

**Note**: Most implementation is shared with US3 (backend auth detection), US6 (data identity), and US2 (user menu UI). This phase captures integration-level verification.

### Implementation for User Story 1

- [X] T030 [US1] Create integration test verifying anonymous mode renders correctly in the full app context (user menu shows "Anonymous", settings link navigates to /settings) in `libris-maleficarum-app/src/__tests__/anonymous-mode.test.tsx`
- [X] T031 [US1] Verify all existing frontend tests pass with anonymous user context — fix any tests broken by auth-related changes across `libris-maleficarum-app/src/`

**Checkpoint**: Anonymous single-user mode works end-to-end without any Entra ID configuration

---

## Phase 7: User Story 4 — Frontend Auth Mode Detection (Priority: P2)

**Goal**: The frontend conditionally wraps the app in an MSAL provider when Entra ID is configured, and provides an auth guard that prompts unauthenticated users to sign in on data pages.

**Independent Test**: Build frontend with `ENTRA_CLIENT_ID` set → MSAL provider wraps app, data pages show "Sign in to continue" when unauthenticated. Build without → no MSAL provider, auth guard bypassed.

### Tests for User Story 4

- [X] T032 [P] [US4] Create `AuthGuard.test.tsx` with rendering tests (bypassed in anonymous mode, shows sign-in prompt in multi-user unauthenticated mode, renders children when authenticated) and jest-axe accessibility checks in `libris-maleficarum-app/src/components/AuthGuard/AuthGuard.test.tsx`

### Implementation for User Story 4

- [X] T033 [US4] Modify `main.tsx` to conditionally wrap `<App />` with `<MsalProvider instance={msalInstance}>` when `isAuthConfigured` is true, otherwise render `<App />` directly in `libris-maleficarum-app/src/main.tsx`
- [X] T034 [US4] Create `AuthGuard.tsx` component that checks `isAuthConfigured` and `useIsAuthenticated()` — when multi-user and unauthenticated, display "Sign in to continue" prompt; when single-user or authenticated, render children in `libris-maleficarum-app/src/components/AuthGuard/AuthGuard.tsx`
- [X] T035 [P] [US4] Create barrel export `index.ts` in `libris-maleficarum-app/src/components/AuthGuard/index.ts`
- [X] T036 [US4] Wrap data-bearing content area in `App.tsx` with `<AuthGuard>` so layout/header remain visible but data pages require auth in multi-user mode in `libris-maleficarum-app/src/App.tsx`

**Checkpoint**: Frontend correctly detects auth mode, MSAL provider conditionally active, data pages gated when unauthenticated

---

## Phase 8: User Story 5 — Entra ID Multi-User Sign-In (Priority: P2)

**Goal**: Users can sign in via Entra ID popup from the user menu, see their identity displayed, and sign out.

**Independent Test**: Configure Entra ID credentials → open user menu → activate "Sign in" → complete Entra ID popup → verify user menu shows display name and initials → sign out → verify revert to sign-in state.

### Implementation for User Story 5

- [X] T037 [US5] Add MSAL popup sign-in handler to `UserMenu.tsx` — call `instance.loginPopup(loginRequest)` on sign-in action in `libris-maleficarum-app/src/components/UserMenu/UserMenu.tsx`
- [X] T038 [US5] Add MSAL sign-out handler to `UserMenu.tsx` — call `instance.logoutPopup()` on sign-out action in `libris-maleficarum-app/src/components/UserMenu/UserMenu.tsx`
- [X] T039 [P] [US5] Add bearer token acquisition interceptor for API calls — use `msalInstance.acquireTokenSilent()` to attach `Authorization: Bearer <token>` to Axios requests when authenticated in `libris-maleficarum-app/src/services/apiClient.ts` or equivalent

**Checkpoint**: Full sign-in/sign-out flow works with Entra ID; API calls include bearer tokens when authenticated

---

## Phase 9: User Story 7 — Aspire AppHost Auth Configuration (Priority: P2)

**Goal**: The Aspire AppHost propagates Entra ID configuration to API and frontend services via environment variables when configured, enabling multi-user mode end-to-end.

**Independent Test**: Set `EntraId:ClientId` and `EntraId:TenantId` in AppHost user secrets → run Aspire → verify API receives `AzureAd:ClientId` and frontend receives `ENTRA_CLIENT_ID`. Run without → anonymous mode.

### Implementation for User Story 7

- [X] T040 [US7] Update `AppHost.cs` to read optional Entra ID config from AppHost configuration/user secrets and pass via `.WithEnvironment()` to API service (`AzureAd__ClientId`, `AzureAd__TenantId`, `AzureAd__Audience`) in `libris-maleficarum-service/src/Orchestration/AppHost/AppHost.cs`
- [X] T041 [US7] Update `AppHost.cs` to pass `ENTRA_CLIENT_ID` and `ENTRA_TENANT_ID` environment variables to frontend Vite app resource via `.WithEnvironment()` in `libris-maleficarum-service/src/Orchestration/AppHost/AppHost.cs`

**Checkpoint**: Both API and frontend services receive correct auth configuration from Aspire AppHost

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, cleanup, and full validation across all stories

- [X] T042 [P] Update `CHANGELOG.md` with feature entry for user authentication mode and user menu under the next version
- [X] T043 [P] Run `pnpm lint` in `libris-maleficarum-app/` and fix any linting issues introduced by new code
- [X] T044 Run all frontend tests (`pnpm test`) in `libris-maleficarum-app/` and fix any failures
- [X] T045 Run all backend unit tests (`dotnet test --solution LibrisMaleficarum.slnx --filter "TestCategory=Unit"`) in `libris-maleficarum-service/` and fix any failures
- [X] T046 Run quickstart.md validation — verify single-user mode startup and multi-user mode startup instructions both work end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **US3 (Phase 3)**: Depends on Foundational (T007 appsettings); T006 now in this phase (TDD: T011 test first, then T006 implementation)
- **US6 (Phase 4)**: Depends on Foundational (T004/T005/T005b interface + entity changes) and US3 (T014 anonymous middleware, T006 extensions)
- **US2 (Phase 5)**: Depends on Foundational (T002 dropdown-menu, T008 authConfig, T010b BrowserRouter) — frontend only
- **US1 (Phase 6)**: Depends on US3, US6, and US2 — integration validation
- **US4 (Phase 7)**: Depends on Foundational (T008 authConfig, T009 Vite defines) — frontend only
- **US5 (Phase 8)**: Depends on US4 (T033 MSAL provider wrapping)
- **US7 (Phase 9)**: Depends on US3 and US4 — both services must handle auth config
- **Polish (Phase 10)**: Depends on all desired user stories being complete

### User Story Dependencies

- **US3 (P1)**: Can start after Foundational — no dependencies on other stories
- **US6 (P1)**: Depends on US3 for anonymous middleware to be in place
- **US2 (P1)**: Can start after Foundational — **frontend only**, runs in parallel with US3/US6
- **US1 (P1)**: Integration validation — depends on US3 + US6 + US2 all being complete
- **US4 (P2)**: Can start after Foundational — may run in parallel with US2
- **US5 (P2)**: Depends on US4 for MSAL provider
- **US7 (P2)**: Can start after US3 and US4 are complete

### Within Each User Story

- Tests should be written first and FAIL before implementation (TDD per constitution)
- Core components before integration points
- Story fully complete before dependent stories begin

### Parallel Opportunities

- **Setup**: T001b, T002, and T003 can run in parallel (different projects)
- **Foundational**: T007, T008, T009, T010 can all run in parallel (after T004/T005/T005b)
- **Backend + Frontend tracks**: US3/US6 (backend) and US2 (frontend) can run in parallel
- **US4 can run in parallel with US2** (both frontend, different files)
- All test tasks marked [P] can run in parallel within their phase
- Barrel exports marked [P] can run in parallel with any task

---

## Implementation Strategy

### MVP Scope (Recommended First Delivery)

**Phases 1–6** (Setup + Foundational + US3 + US6 + US2 + US1) deliver a fully functional anonymous single-user mode with the user menu dropdown and settings page. This is the default experience and ships without any Entra ID configuration.

### Incremental Delivery

1. **MVP**: Phases 1–6 — Anonymous mode + user menu + settings page + backend identity
1. **Multi-user support**: Phases 7–9 — MSAL provider wrapping + auth guard + Entra ID sign-in/sign-out + Aspire config propagation
1. **Polish**: Phase 10 — CHANGELOG, lint, full test suite, quickstart validation

### Task Count Summary

| Phase | Story | Tasks | Parallelizable |
|-------|-------|-------|----------------|
| 1 — Setup | — | 4 | 3 |
| 2 — Foundational | — | 8 | 4 |
| 3 — Backend Auth | US3 | 7 | 2 |
| 4 — CosmosDB Identity | US6 | 7 | 2 |
| 5 — User Menu | US2 | 8 | 4 |
| 6 — Anonymous Mode | US1 | 2 | 0 |
| 7 — Frontend Auth | US4 | 5 | 2 |
| 8 — Entra ID Sign-In | US5 | 3 | 1 |
| 9 — Aspire Config | US7 | 2 | 0 |
| 10 — Polish | — | 5 | 2 |
| **Total** | | **51** | **20** |
