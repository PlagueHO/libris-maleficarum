# Feature Specification: User Authentication Mode and User Menu

**Feature Branch**: `017-user-auth-menu`
**Created**: 2026-04-05
**Status**: Draft
**Input**: User description: "Add single-user/multi-user authentication mode with user menu dropdown in the header. Single-user mode uses _anonymous. Multi-user mode uses Entra ID. Follow prompt-babbler implementation patterns. CosmosDB containers must include user ID. APIs and infrastructure must support both modes."

## User Scenarios & Testing

### User Story 1 - Anonymous Single-User Mode (Priority: P1)

A person using the application in its default single-user mode sees a user avatar/icon in the top-right of the header. When they interact with it, a dropdown menu opens showing their identity as "Anonymous", access to application settings, and a disabled sign-in option (since Entra ID is not configured). All data operations on the backend use `_anonymous` as the user identifier. This is the default mode when no Entra ID configuration is provided.

**Why this priority**: This is the default operational mode and must work out of the box for all users. Without this, the application has no user identity model.

**Independent Test**: Can be fully tested by launching the application without any Entra ID configuration and verifying the user menu appears, shows "Anonymous", and all CRUD operations use `_anonymous` as the user ID.

**Acceptance Scenarios**:

1. **Given** the application is running without Entra ID configuration, **When** a person loads the application, **Then** the user menu appears in the top-right of the header showing an anonymous user icon and the label "Anonymous".
1. **Given** the application is in single-user mode, **When** a person opens the user menu dropdown, **Then** they see their identity as "Anonymous Mode", an informational note that Entra ID SSO is not configured, a link to application settings, and a disabled sign-in option.
1. **Given** the application is in single-user mode, **When** any data is created or modified via the API, **Then** the `UserId` field on all relevant data records is set to `_anonymous`.
1. **Given** the application is in single-user mode, **When** the API receives a request, **Then** the request is processed without requiring an authentication token and the backend injects `_anonymous` as the user identity.

---

### User Story 2 - User Menu Dropdown in Header (Priority: P1)

A person using the application sees a user menu dropdown button in the top-right area of the header toolbar. The dropdown contains the currently logged-in user identity, a link to application settings, and a sign-out option (when authenticated). The menu follows standard modern application conventions and is keyboard accessible.

**Why this priority**: The user menu is the primary UI surface for identity, settings, and sign-out. It must be present for both single-user and multi-user modes.

**Independent Test**: Can be tested by rendering the header component and verifying the dropdown trigger, menu items, and keyboard navigation work correctly in both authenticated and anonymous states.

**Acceptance Scenarios**:

1. **Given** the header is rendered, **When** a person looks at the top-right area, **Then** they see a user menu trigger button to the right of the theme toggle and notification bell.
1. **Given** the user menu trigger is visible, **When** a person activates it (click or keyboard), **Then** a dropdown menu opens with the appropriate items for the current auth mode.
1. **Given** the user menu is open, **When** a person navigates with the keyboard, **Then** they can move through menu items with arrow keys, activate items with Enter, and close the menu with Escape.
1. **Given** the user menu is open in authenticated mode, **When** a person views the dropdown, **Then** they see their display name, email address, a settings link, and a sign-out option.
1. **Given** the user menu is open in anonymous mode, **When** a person views the dropdown, **Then** they see "Anonymous Mode", a note about Entra ID not being configured, a settings link, and a disabled sign-in option.

---

### User Story 3 - Backend Authentication Mode Detection (Priority: P1)

The backend API detects whether authentication is enabled based on the presence of Entra ID configuration (specifically `AzureAd:ClientId` in app settings). When configured, the API requires JWT bearer tokens validated against Entra ID. When not configured, the API runs in anonymous single-user mode and injects `_anonymous` as the user identity for all requests.

**Why this priority**: The backend must correctly determine auth mode to enforce or bypass authentication. This is foundational for all data operations.

**Independent Test**: Can be tested by starting the API with and without `AzureAd:ClientId` configuration and verifying that authentication behavior matches the expected mode.

**Acceptance Scenarios**:

1. **Given** the API is started without `AzureAd:ClientId` configured, **When** any endpoint is called without a token, **Then** the request succeeds and the user identity is `_anonymous`.
1. **Given** the API is started with `AzureAd:ClientId` configured, **When** an endpoint is called without a valid token, **Then** the request is rejected with a 401 Unauthorized response.
1. **Given** the API is started with `AzureAd:ClientId` configured, **When** an endpoint is called with a valid Entra ID token, **Then** the request succeeds and the user identity is extracted from the token's object ID claim.
1. **Given** the API is in anonymous mode, **When** the application starts, **Then** a warning is logged indicating anonymous single-user mode is active.

---

### User Story 4 - Frontend Authentication Mode Detection (Priority: P2)

The frontend application detects whether Entra ID authentication is configured based on whether MSAL client configuration values are injected at build/dev time (via Vite environment variables from Aspire). When configured, the app wraps the component tree with an MSAL provider. When not configured, the app operates without MSAL, and the user context defaults to anonymous.

**Why this priority**: Frontend auth detection drives how the user menu renders and whether MSAL hooks are available. Depends on the backend auth mode being established first.

**Independent Test**: Can be tested by building/running the frontend with and without MSAL client ID environment variables and verifying the app renders correctly in both modes.

**Acceptance Scenarios**:

1. **Given** the frontend is built without MSAL client ID, **When** the application loads, **Then** it renders without the MSAL provider and the `isAuthConfigured` flag is `false`.
1. **Given** the frontend is built with valid MSAL client ID and tenant ID, **When** the application loads, **Then** it wraps the app in an MSAL provider and the `isAuthConfigured` flag is `true`.
1. **Given** the frontend is in anonymous mode, **When** any component checks auth state, **Then** it receives a consistent anonymous user context.

---

### User Story 5 - Entra ID Multi-User Sign-In (Priority: P2)

A person using the application in multi-user mode (Entra ID configured) can sign in with their organizational account via the user menu. After signing in, their display name and initials appear in the user menu. They can sign out via the dropdown menu.

**Why this priority**: Multi-user mode is a future capability that needs architectural support now but is not the default mode. Building the foundation for it alongside single-user ensures the architecture supports both from the start.

**Independent Test**: Can be tested by configuring Entra ID credentials and verifying the sign-in flow, user display, and sign-out work correctly.

**Acceptance Scenarios**:

1. **Given** the application has Entra ID configured and the person is not signed in, **When** they open the user menu, **Then** they see a "Sign in" option.
1. **Given** the person activates the sign-in option, **When** the Entra ID popup completes, **Then** the user menu updates to show their display name and initials avatar.
1. **Given** the person is signed in, **When** they open the user menu and select "Sign out", **Then** they are signed out and the menu reverts to the sign-in state.
1. **Given** the person is signed in, **When** data is created or modified, **Then** the person's Entra ID object ID is used as the `UserId` across all data records.

---

### User Story 6 - CosmosDB User Identity on Data Records (Priority: P1)

All CosmosDB containers that store user-associated data include a `UserId` field (or equivalent field like `OwnerId`) that identifies which user owns or created the record. In single-user mode, this is always `_anonymous`. In multi-user mode, this is the authenticated user's Entra ID object ID.

**Why this priority**: Without user identity on data records, multi-user data isolation is impossible. The data model must support user identity from the outset.

**Independent Test**: Can be tested by creating entities through the API in both auth modes and verifying the correct user ID is persisted to CosmosDB.

**Acceptance Scenarios**:

1. **Given** the API is in single-user mode, **When** a World is created, **Then** the World's `OwnerId` is `_anonymous`.
1. **Given** the API is in multi-user mode and a person is authenticated, **When** a World is created, **Then** the World's `OwnerId` is the person's Entra ID object ID.
1. **Given** the API is in single-user mode, **When** a WorldEntity is created, **Then** the entity's `OwnerId`, `CreatedBy`, and `ModifiedBy` fields are `_anonymous`.
1. **Given** the API is in multi-user mode and a person is authenticated, **When** a WorldEntity is modified, **Then** the entity's `ModifiedBy` field is updated to the person's Entra ID object ID.

---

### User Story 7 - Aspire AppHost Auth Configuration (Priority: P2)

The Aspire AppHost orchestration passes Entra ID configuration to the API and frontend services when configured. It supports running without Entra ID configuration for local development in single-user mode.

**Why this priority**: The AppHost must correctly propagate auth configuration to services for end-to-end multi-user support. Not needed for single-user mode to function.

**Independent Test**: Can be tested by running the Aspire AppHost with and without Entra ID parameters and verifying both services receive the correct configuration.

**Acceptance Scenarios**:

1. **Given** the AppHost is started without Entra ID configuration, **When** services start, **Then** both the API and frontend run in anonymous single-user mode.
1. **Given** the AppHost is configured with Entra ID client ID and tenant ID, **When** services start, **Then** the API receives `AzureAd:ClientId` and the frontend receives MSAL configuration values.

---

### Edge Cases

- What happens when an Entra ID token expires mid-session? The frontend should prompt for re-authentication or silently acquire a new token via MSAL's built-in token refresh.
- What happens when the Entra ID service is unreachable during multi-user mode? The application should display a clear error state, not silently fall back to anonymous mode.
- What happens if `AzureAd:ClientId` is configured on the backend but the frontend is missing MSAL configuration? The frontend operates in anonymous mode, but API calls fail with 401 — the application should surface a helpful error message.
- What happens when a user signs out in multi-user mode? Their session data is cleared, but their previously created data remains intact with their user ID.

## Requirements

### Functional Requirements

- **FR-001**: The system MUST support two authentication modes: single-user (anonymous) and multi-user (Entra ID), determined by configuration.
- **FR-002**: In single-user mode, the system MUST use `_anonymous` as the user identifier for all data operations.
- **FR-003**: In multi-user mode, the system MUST authenticate users via Microsoft Entra ID and use the user's Entra ID object ID as the user identifier.
- **FR-004**: The backend API MUST detect authentication mode by checking for the presence of `AzureAd:ClientId` in configuration. If present and non-empty, multi-user mode is active. Otherwise, single-user mode is active.
- **FR-005**: The frontend MUST detect authentication mode by checking whether MSAL client configuration (specifically the client ID) is injected at build time. If present and non-empty, multi-user mode is active. Otherwise, single-user mode is active.
- **FR-006**: The frontend MUST render a user menu dropdown in the top-right area of the header toolbar, to the right of existing header controls (theme toggle, notification bell).
- **FR-007**: In single-user mode, the user menu MUST display an anonymous user icon, the label "Anonymous", and a dropdown containing identity information ("Anonymous Mode"), a settings link, and a disabled sign-in option.
- **FR-008**: In multi-user mode with a signed-in user, the user menu MUST display the user's initials avatar, their display name, and a dropdown containing the user's name and email, a settings link, and a sign-out option.
- **FR-009**: In multi-user mode without a signed-in user, the user menu MUST display a sign-in button/option with a dropdown containing the sign-in action and a settings link.
- **FR-010**: The backend API MUST inject a synthetic `_anonymous` claims principal (with object ID `_anonymous`) when running in single-user mode, so all controller logic can consistently read user identity from claims.
- **FR-011**: The backend API MUST provide a `ClaimsPrincipalExtensions.GetUserIdOrAnonymous()` method (or equivalent) that returns the user's Entra ID object ID or `_anonymous`.
- **FR-012**: All CosmosDB containers storing user-associated data MUST include a user identity field (`OwnerId`, `CreatedBy`, `ModifiedBy`) populated from the authenticated or anonymous user identity.
- **FR-017**: In multi-user mode, API queries MUST filter results by the authenticated user's identity (e.g., list only worlds where `OwnerId` matches the caller). Cross-user access (RBAC-based sharing) is explicitly out of scope for this feature.
- **FR-013**: The frontend MUST conditionally wrap the application with an MSAL provider only when multi-user mode is configured, and MUST initialize the MSAL instance before rendering.
- **FR-014**: The Aspire AppHost MUST support passing Entra ID configuration (client ID, tenant ID) to the API and frontend services.
- **FR-015**: The user menu MUST be fully keyboard accessible (Tab to focus, Enter/Space to open, arrow keys to navigate, Escape to close).
- **FR-016**: The user menu trigger and dropdown MUST have appropriate ARIA attributes (role, aria-label, aria-expanded) for screen reader users.
- **FR-018**: The application MUST include a `/settings` route with a settings page containing at minimum a dark/light mode toggle. The "Settings" link in the user menu MUST navigate to this route.
- **FR-019**: Health check endpoints (e.g., `/health`, `/alive`) MUST remain publicly accessible without authentication regardless of the active authentication mode.
- **FR-020**: In multi-user mode, data-bearing pages MUST be wrapped in an auth guard that displays a "Sign in to continue" prompt when the user is unauthenticated. The layout, header, and user menu MUST remain visible. In single-user mode, the auth guard MUST be bypassed entirely.

### Key Entities

- **User Identity**: Represents the authenticated or anonymous user. In single-user mode, always `_anonymous`. In multi-user mode, the Entra ID object ID. Not stored as a separate entity — embedded as fields on existing entities (World, WorldEntity, Asset).
- **Auth Configuration**: The set of configuration values (Entra ID client ID, tenant ID, audience, scopes) that determine whether multi-user mode is active. Sourced from app settings (backend) and Vite build-time injection (frontend).

## Success Criteria

### Measurable Outcomes

- **SC-001**: A person using the application in single-user mode can see and interact with the user menu within 2 seconds of the page loading.
- **SC-002**: All data created in single-user mode has `_anonymous` as the user identifier — verified by inspecting CosmosDB records.
- **SC-003**: The user menu is fully operable using only a keyboard, with visible focus indicators at all times.
- **SC-004**: A person can complete the sign-in flow in multi-user mode (when configured) in under 30 seconds.
- **SC-005**: The application correctly determines auth mode and renders the appropriate UI on first load without any flicker or mode switching.
- **SC-006**: All existing tests continue to pass with the anonymous user identity injected by default.
- **SC-007**: The user menu displays correctly on all supported viewport sizes (mobile through desktop).

## Clarifications

### Session 2026-04-05

- Q: In multi-user mode, should each person only see their own data (full isolation) or share a common data pool? → A: Full isolation by OwnerId. Future versions will add RBAC for cross-user collaboration (e.g., contributing to another user's worlds).
- Q: What should the "Settings" link in the user menu navigate to? → A: Navigate to a `/settings` route. No settings page exists yet; create a basic one with a dark/light mode toggle.
- Q: Should health check endpoints remain public in multi-user mode? → A: Yes. Health check endpoints (e.g., `/health`, `/alive`) are always publicly accessible without authentication, even in multi-user mode.
- Q: Should the entire app be gated behind sign-in in multi-user mode, or only data pages? → A: Auth guard on data pages only. The layout/header remains visible; data-bearing pages show a "Sign in to continue" prompt when unauthenticated. In single-user mode the auth guard is bypassed entirely.
- Q: What API audience URI and token scope should be used for Entra ID? → A: Audience `api://libris-maleficarum-api`, scope `access_as_user`.

## Assumptions

- The Entra ID app registration and API permissions are provisioned separately (out of scope for this feature) — the infrastructure for this may be added later.
- The `OwnerId`, `CreatedBy`, and `ModifiedBy` fields already exist in the data model for World and WorldEntity (confirmed in `DATA_MODEL.md`) and will be used as the user identity fields.
- The Vite build-time injection of MSAL configuration follows the same pattern as prompt-babbler, using `define` in `vite.config.ts` to expose Aspire-provided environment variables.
- The MSAL library (`@azure/msal-browser` and `@azure/msal-react`) will be added as frontend dependencies when implementing this feature.
- The `Microsoft.Identity.Web` package will be added as a backend dependency for JWT validation.
- The application settings pattern (`AzureAd:ClientId`, `AzureAd:TenantId`, `AzureAd:Audience`) follows the standard Microsoft Identity Web configuration. The API audience is `api://libris-maleficarum-api` and the token scope is `access_as_user`.
- Cross-user collaboration (RBAC-based sharing of worlds/entities) is out of scope for this feature and will be addressed in a future feature.
