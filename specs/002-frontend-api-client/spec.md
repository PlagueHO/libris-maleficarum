# Feature Specification: Frontend API Client and Services

**Feature Branch**: `002-frontend-api-client`  
**Created**: January 11, 2026  
**Status**: Draft  
**Input**: User description: "Frontend API client and services to provide access to the backend REST APIs. This should be implemented as per the chat discussion we've just had. Authentication and Authorization of the client and services won't be included yet - these will be added later and be provided by Microsoft Authentication Library (MSAL). The feature should only provide the client and services to access the REST APIs (including exception handling, retry etc) as well as thorough unit tests."

## Clarifications

### Session 2026-01-11

- Q: When the API returns a 429 Too Many Requests error (rate limiting), how should the client handle it? → A: Retry with exponential backoff respecting `Retry-After` header if present (treat as transient)
- Q: Where will the backend API base URL be configured and how will it differ between environments (local dev, staging, production)? → A: Vite environment variables (e.g., `VITE_API_BASE_URL`) with `.env` files per environment. For local development with Aspire AppHost, environment variables are automatically injected when using `.WithReference(api)` in both service discovery format (`services__apiservice__https__0`) and endpoint format (`APISERVICE_HTTPS`/`APISERVICE_HTTP`). The Vite dev server proxy configuration reads these auto-injected variables. References: [Aspire Vite playground example](https://github.com/dotnet/aspire/blob/main/playground/AspireWithJavaScript/AspireJavaScript.Vite/vite.config.ts), [Aspire WithReference documentation](https://learn.microsoft.com/en-us/dotnet/api/aspire.hosting.resourcebuilderextensions.withreference)
- Q: What structured error format should the API client expect from backend error responses? → A: Problem Details (RFC 7807) format with standard fields: `type`, `title`, `status`, `detail`, `instance`, plus optional `errors` object for validation failures (ASP.NET Core 7+ default format)
- Q: For response caching, what default cache duration (stale time) should be used for GET requests? → A: 60 seconds (1 minute) - provides good balance between reducing redundant API calls and ensuring reasonably fresh data. This is RTK Query's default and can be overridden per endpoint for real-time data needs

## Scope Boundaries

### In Scope

- **Generic API Client Infrastructure**: RTK Query setup, Axios configuration, retry logic, error handling, type safety mechanisms, cache management - all designed to work with ANY backend REST endpoint
- **Core Configuration**: Environment variable integration (Vite env vars + Aspire auto-injection), timeout settings, default headers, RFC 7807 error parsing
- **Example Endpoint Definitions**: World and WorldEntity CRUD operations as primary validation of the infrastructure patterns
- **Complete Testing**: Unit tests for infrastructure components, retry logic, error handling, cache invalidation, and example endpoints
- **Developer Documentation**: Setup guides, usage patterns, testing examples, troubleshooting

### Out of Scope

- **Asset Management Endpoints**: `/worlds/{worldId}/assets/*`, `/worlds/{worldId}/entities/{entityId}/assets/*` - will be added when asset upload/management features are implemented
- **Search and Discovery Endpoints**: `/worlds/{worldId}/search`, entity filtering/sorting endpoints - will be added when search UI features are implemented  
- **AG-UI Agent Endpoint**: `/api/v1/copilotkit` - will be added when CopilotKit integration is implemented
- **Authentication Implementation**: MSAL integration, token management, auth headers - explicitly deferred to separate feature
- **UI Components Consuming APIs**: No React components, pages, or features that use the API client - those are separate feature implementations
- **Backend API Implementation**: This feature only provides the client; backend APIs are assumed to exist

**Rationale**: Following YAGNI principle, we implement the infrastructure with sufficient examples to validate the patterns work correctly. Additional endpoint slices follow identical patterns and can be added incrementally as consuming features require them, preventing premature implementation of unused code.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basic API Data Retrieval (Priority: P1)

Developers can fetch world entities from the backend REST API through a standardized client service, automatically handling loading states, errors, and response caching without manual state management.

**Why this priority**: Core foundation for all frontend-backend communication. Without this, no other API features can work. Delivers immediate value by enabling basic read operations.

**Independent Test**: Can be fully tested by making a GET request for worlds list through the client service and verifying automatic loading state management, successful data retrieval, and proper error handling when the API is unavailable.

**Acceptance Scenarios**:

1. **Given** the frontend application is initialized, **When** a component requests the list of worlds, **Then** the client automatically manages loading state, fetches data from the backend API, caches the response, and provides the worlds data to the component
2. **Given** the API is unavailable, **When** a component requests world data, **Then** the client detects the failure, provides a structured error object to the component, and does not crash the application
3. **Given** multiple components request the same world data simultaneously, **When** the requests are made within the cache window, **Then** only one HTTP request is sent to the backend and all components receive the cached response

---

### User Story 2 - Entity Creation and Mutation (Priority: P2)

Developers can create, update, and delete world entities through mutation services that automatically invalidate related caches and provide optimistic updates for better user experience.

**Why this priority**: Enables full CRUD operations. Builds on P1 read operations. Critical for user data management but less urgent than basic data retrieval.

**Independent Test**: Can be fully tested by creating a new world entity through the mutation service, verifying the cache invalidation triggers a refetch, and confirming the new entity appears in the worlds list without manual refresh.

**Acceptance Scenarios**:

1. **Given** a user has loaded the worlds list, **When** they create a new world through the mutation service, **Then** the service sends a POST request, automatically invalidates the worlds list cache, triggers a refetch, and the new world appears in the list
2. **Given** a user is viewing a world entity, **When** they update its properties through the mutation service, **Then** the service sends a PUT/PATCH request, invalidates the specific entity cache, and the updated data reflects immediately
3. **Given** a user deletes a world entity, **When** the deletion completes, **Then** the mutation service removes the entity from all relevant caches and the UI updates to reflect the deletion

---

### User Story 3 - Automatic Retry with Exponential Backoff (Priority: P3)

The API client automatically retries failed requests with exponential backoff for transient network errors, protecting against temporary connectivity issues without requiring manual retry logic.

**Why this priority**: Improves reliability and user experience for unstable connections. Nice-to-have enhancement that builds on P1/P2 but not critical for MVP.

**Independent Test**: Can be fully tested by simulating a network timeout, verifying the client automatically retries the request with increasing delays (e.g., 1s, 2s, 4s), and succeeds when the network recovers within the retry window.

**Acceptance Scenarios**:

1. **Given** the API returns a 503 Service Unavailable error, **When** the client processes the response, **Then** it automatically retries the request up to 3 times with exponential backoff (1 second, 2 seconds, 4 seconds)
2. **Given** a request times out due to slow network, **When** the timeout is detected, **Then** the client retries the request and provides feedback on retry attempts
3. **Given** the API returns a 401 Unauthorized error, **When** the client processes the response, **Then** it does NOT retry (non-transient error) and immediately returns the error to the caller

---

### User Story 4 - Type-Safe Request and Response Handling (Priority: P2)

Developers define TypeScript interfaces for API requests and responses, and the client service provides full type safety from API calls through to component consumption.

**Why this priority**: Essential for maintainability and developer productivity. Prevents runtime errors from API contract mismatches. High value for long-term project health.

**Independent Test**: Can be fully tested by defining a typed API endpoint, making a request, and verifying TypeScript compile-time errors when attempting to access non-existent properties on the response.

**Acceptance Scenarios**:

1. **Given** an API endpoint is defined with TypeScript types, **When** a developer uses the auto-generated hook, **Then** they receive typed data, loading, and error objects with full IntelliSense support
2. **Given** the API response schema changes, **When** the developer updates the TypeScript interface, **Then** all call sites using outdated properties show TypeScript errors at compile time
3. **Given** a mutation requires specific request parameters, **When** a developer calls the mutation, **Then** TypeScript enforces the correct parameter types and prevents invalid requests

---

### Edge Cases

- What happens when the API returns an unexpected 2xx response format (e.g., missing expected fields)?
- How does the system handle extremely slow API responses (30+ seconds)?
- What happens when the user loses network connectivity mid-request?
- What happens when concurrent mutations attempt to update the same entity?
- How does the cache behave when the user navigates away and returns after cache expiration?
- What happens when the API returns a large payload (10MB+) that may impact browser memory?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide a centralized API client configuration with base URL from environment variables (Vite env vars in `.env` files for deployed environments, auto-injected by Aspire AppHost for local development), timeout settings, and default headers
- **FR-002**: System MUST implement RTK Query API slices for all world entity CRUD operations (create, read, update, delete)
- **FR-003**: System MUST automatically manage request/response states including loading, success, and error conditions
- **FR-004**: System MUST implement response caching with default cache duration of 60 seconds (configurable per endpoint) to prevent redundant API calls
- **FR-005**: System MUST provide automatic cache invalidation when mutations modify related entities
- **FR-006**: System MUST implement retry logic with exponential backoff for transient errors (5xx, 429 rate limiting, timeout, network errors)
- **FR-007**: System MUST NOT retry non-transient errors (4xx client errors except 429)
- **FR-008**: System MUST respect `Retry-After` header when present in 429 responses to determine retry delay
- **FR-009**: System MUST provide TypeScript interfaces for all API request and response models
- **FR-010**: System MUST auto-generate React hooks for all defined API endpoints
- **FR-011**: System MUST handle HTTP error responses following RFC 7807 Problem Details format and convert them to typed error objects
- **FR-012**: System MUST support request cancellation when components unmount or requests become stale
- **FR-013**: System MUST provide a mechanism to manually trigger cache invalidation or refetching
- **FR-014**: System MUST integrate with the existing Redux store without conflicts
- **FR-015**: System MUST log API errors to the browser console for debugging purposes
- **FR-016**: System MUST serialize request bodies as JSON and expect JSON responses
- **FR-017**: System MUST handle network timeouts with a default timeout of 10 seconds
- **FR-018**: Authentication headers will be added in future implementation (placeholder for MSAL integration, excluded from this feature scope)

### Key Entities *(include if feature involves data)*

- **World**: Root entity representing a fictional world, including properties like ID, name, description, creation timestamp
- **WorldEntity**: Base entity type for hierarchical world elements (continents, countries, regions, cities, characters), with properties for ID, name, type, parent relationships
- **API Request Models**: TypeScript interfaces representing request payloads for create/update operations
- **API Response Models**: TypeScript interfaces representing response shapes from the backend, including success responses and error responses
- **Error Response (RFC 7807 Problem Details)**: Structured error object following RFC 7807 standard with properties: `type` (URI reference identifying the problem type), `title` (human-readable summary), `status` (HTTP status code), `detail` (specific explanation), `instance` (URI reference to specific occurrence), and optional `errors` object for validation failures (ASP.NET Core format)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Developers can implement a new API endpoint with full type safety in under 10 minutes by defining the endpoint in an RTK Query slice
- **SC-002**: The client reduces redundant API calls by at least 80% through automatic response caching when multiple components request the same data
- **SC-003**: Users experience zero application crashes from API errors, with all failures gracefully handled and presented through error states
- **SC-004**: The client successfully retries and recovers from at least 90% of transient network failures (5xx errors, timeouts) without user intervention
- **SC-005**: Components consume API data without manual loading/error state management code, reducing boilerplate by approximately 70% compared to manual fetch implementations
- **SC-006**: All API client code has minimum 90% unit test coverage including success paths, error handling, retry logic, and cache invalidation
- **SC-007**: TypeScript compilation catches 100% of API contract violations (accessing non-existent response fields) at build time before runtime
- **SC-008**: Developers can identify and debug API failures within 2 minutes using structured error objects and console logging

## Assumptions

- Backend REST APIs are already implemented and accessible at a known base URL
- Backend APIs return standard HTTP status codes (2xx success, 4xx client error, 5xx server error)
- Backend APIs accept and return JSON payloads with consistent structure
- The React application already uses Redux Toolkit for state management
- Authentication will be implemented separately using MSAL in a future feature
- Backend error responses follow RFC 7807 Problem Details format (standard in ASP.NET Core 7+)
- The development environment has access to the backend APIs (either local or deployed)
- Network connectivity is generally stable, with transient failures being the exception
- Browser environment supports modern fetch API and JavaScript ES2022 features
- Local development uses Microsoft Aspire (AppHost) which automatically injects service endpoint environment variables when using `.WithReference()` - these variables are consumed by Vite's proxy configuration (see [Aspire JavaScript hosting extensions](https://learn.microsoft.com/en-us/dotnet/api/aspire.hosting.javascripthostingextensions.addviteapp))
- Deployed environments (Azure Container Apps) use Vite environment variables configured through Azure Static Web App configuration or Container App environment settings
