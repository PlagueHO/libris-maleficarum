# Implementation Plan: Frontend API Client and Services

**Branch**: `002-frontend-api-client` | **Date**: 2026-01-11 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-frontend-api-client/spec.md`

## Summary

Implement a type-safe, RTK Query-based API client for the React frontend to consume backend REST APIs. The client will provide automatic state management, response caching (60s default), retry logic with exponential backoff, and structured error handling using RFC 7807 Problem Details format. Local development will leverage Aspire AppHost auto-injected environment variables for service discovery. The implementation follows YAGNI principles—only the client infrastructure is built; consuming features will be added later.

## Technical Context

**Language/Version**: TypeScript 5.9.3, React 19.2.0  
**Primary Dependencies**: 
- RTK Query (@reduxjs/toolkit 2.11.2) - API client with caching
- Redux Toolkit - Already integrated for state management
- Vite 7.2.4 - Build tool with dev server proxy
- Vitest 4.0.16 - Unit testing framework
- @testing-library/react 16.3.1 - Component testing
- Axios (to be added) - HTTP client for retry/interceptor capabilities

**Storage**: N/A (API client only, no local persistence)

**Testing**: Vitest with React Testing Library, jest-axe for accessibility (already configured)

**Target Platform**: Modern browsers (ES2022+), Vite dev server for local development

**Project Type**: Web application frontend (React SPA)

**Performance Goals**: 
- < 100ms API client overhead
- 80% reduction in redundant API calls via caching
- < 10 seconds to add new typed endpoint

**Constraints**: 
- Must integrate with existing Redux store without conflicts
- Aspire AppHost environment variables for service discovery in local dev
- No authentication (deferred to MSAL integration)
- RFC 7807 Problem Details error format from backend

**Scale/Scope**: 
- Initial: 5-10 world entity CRUD endpoints
- Extensible: Designed for 50+ endpoints as features grow
- Cache: Default 60s TTL, ~100 cached responses in memory

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ III. Test-Driven Development (NON-NEGOTIABLE)
- **Status**: COMPLIANT
- **Evidence**: Vitest + React Testing Library already configured. Plan includes comprehensive unit tests for API client, retry logic, cache invalidation, and error handling before implementation.

### ✅ IV. Framework & Technology Standards
- **Status**: COMPLIANT
- **Evidence**: React 19.2.0 + TypeScript 5.9.3 + Redux Toolkit 2.11.2 already in use. RTK Query is the Redux team's official solution for API state management.

### ✅ V. Developer Experience & Inner Loop
- **Status**: COMPLIANT
- **Evidence**: Vite already provides hot reload. Aspire AppHost auto-injects service discovery environment variables (e.g., `APISERVICE_HTTPS`) that Vite proxy configuration consumes—no manual URL updates needed.

### ✅ VI. Security & Privacy by Default
- **Status**: COMPLIANT (with scope exclusion)
- **Evidence**: Auth/secrets explicitly excluded from this feature scope per spec (FR-018). MSAL integration planned for future work. API client prepared for auth header injection point.

### ✅ II. Clean Architecture & Separation of Concerns
- **Status**: COMPLIANT
- **Evidence**: Clear layering: RTK Query API slices (data layer) → Auto-generated hooks (service layer) → Components (UI layer). Type-safe interfaces between layers.

### ⚠️ Re-validation Required After Phase 1
- Verify RTK Query slice structure maintains clean boundaries
- Confirm TypeScript types provide compile-time contract enforcement
- Validate test coverage meets TDD requirements (tests written first)

## Project Structure

### Documentation (this feature)

```text
specs/002-frontend-api-client/
├── plan.md              # This file
├── research.md          # Phase 0: RTK Query patterns, Aspire integration, retry strategies
├── data-model.md        # Phase 1: TypeScript interfaces for World entities + RFC 7807 errors
├── quickstart.md        # Phase 1: Developer guide for adding new endpoints
├── contracts/           # Phase 1: API request/response type definitions
│   ├── world.types.ts   # World entity types
│   ├── error.types.ts   # RFC 7807 Problem Details types
│   └── common.types.ts  # Shared request/response types
├── checklists/
│   └── requirements.md  # Requirement validation checklist
└── tasks.md             # Phase 2: Task breakdown (created by /speckit.tasks)
```

### Source Code (libris-maleficarum-app/)

```text
libris-maleficarum-app/
├── src/
│   ├── services/
│   │   ├── api.ts                    # Base RTK Query API slice configuration
│   │   ├── worldApi.ts               # World entity endpoints (GET/POST/PUT/DELETE)
│   │   └── types/                    # API type definitions
│   │       ├── world.types.ts        # World entity request/response types
│   │       ├── problemDetails.types.ts # RFC 7807 error types
│   │       └── index.ts              # Barrel export
│   │
│   ├── lib/
│   │   ├── apiClient.ts              # Axios instance with interceptors + retry logic
│   │   └── utils.ts                  # Existing utilities
│   │
│   ├── store/
│   │   └── store.ts                  # UPDATE: Add RTK Query API reducer
│   │
│   └── __tests__/
│       └── services/
│           ├── api.test.ts           # Base API configuration tests
│           ├── worldApi.test.ts      # World endpoint tests
│           ├── apiClient.test.ts     # Axios retry logic tests
│           └── errorHandling.test.ts # RFC 7807 error parsing tests
│
├── vite.config.ts                    # UPDATE: Add Aspire proxy configuration
├── package.json                      # UPDATE: Add axios, axios-retry
└── .env.example                      # NEW: Document required env vars
```

**Structure Decision**: Follow existing React app conventions in `libris-maleficarum-app/`. New `services/` directory for API client logic keeps clear separation from UI components. TypeScript types co-located in `services/types/` for easy import. Tests mirror source structure in `__tests__/services/`.

## Complexity Tracking

> **Not applicable** - No constitutional violations. Feature follows all established principles.

---

## Phase 0: Research & Technology Decisions

### RTK Query Integration Patterns

**Decision**: Use RTK Query as the primary API client solution
- **Rationale**: Redux Toolkit already integrated; RTK Query is the official Redux team solution for server state management
- **References**: 
  - [RTK Query Overview](https://redux-toolkit.js.org/rtk-query/overview)
  - [RTK Query TypeScript Guide](https://redux-toolkit.js.org/rtk-query/usage/typescript)

**Key Patterns Researched**:
1. **Base API Setup**: Use `createApi` with `fetchBaseQuery` for automatic Redux integration
2. **Code Splitting**: Use `api.injectEndpoints()` to split API slices by feature domain (worldApi, characterApi, etc.)
3. **Tag-Based Cache Invalidation**: Define entity tags (`['World']`) for automatic refetching on mutations
4. **Auto-Generated Hooks**: Export `useGetWorldsQuery`, `useCreateWorldMutation` for zero-boilerplate component usage

### Aspire AppHost Environment Variable Integration

**Decision**: Use Vite's `proxy` configuration to consume Aspire-injected environment variables
- **Rationale**: Aspire `.WithReference(api)` automatically injects `APISERVICE_HTTPS` and `APISERVICE_HTTP` env vars during local dev
- **References**:
  - [Aspire Vite playground example](https://github.com/dotnet/aspire/blob/main/playground/AspireWithJavaScript/AspireJavaScript.Vite/vite.config.ts)
  - [Aspire WithReference docs](https://learn.microsoft.com/en-us/dotnet/api/aspire.hosting.resourcebuilderextensions.withreference)

**Implementation Pattern**:
```typescript
// vite.config.ts
export default defineConfig({
  server: {
    proxy: {
      '/api': {
        target: process.env.APISERVICE_HTTPS || process.env.APISERVICE_HTTP,
        changeOrigin: true,
        secure: false
      }
    }
  }
})
```

**Environment Handling**:
- **Local Dev**: Aspire auto-injects `APISERVICE_HTTPS` → Vite proxy uses it
- **Deployed (Azure)**: Use `VITE_API_BASE_URL` in `.env` files or Azure Static Web App config

### Retry Strategy with Exponential Backoff

**Decision**: Use Axios with `axios-retry` library for retry logic
- **Rationale**: RTK Query's `fetchBaseQuery` doesn't support retries; Axios provides proven retry middleware with exponential backoff
- **References**:
  - [axios-retry library](https://github.com/softonic/axios-retry)
  - [RTK Query custom baseQuery](https://redux-toolkit.js.org/rtk-query/usage/customizing-queries#implementing-a-custom-basequery)

**Retry Configuration**:
```typescript
axiosRetry(axiosInstance, {
  retries: 3,
  retryDelay: axiosRetry.exponentialDelay,
  retryCondition: (error) => {
    // Retry on 5xx, network errors, timeouts, 429
    return axiosRetry.isNetworkOrIdempotentRequestError(error) ||
           error.response?.status === 429 ||
           error.response?.status === 503;
  },
  onRetry: (retryCount, error, requestConfig) => {
    // Respect Retry-After header for 429 responses
    const retryAfter = error.response?.headers['retry-after'];
    if (retryAfter) {
      requestConfig.retryDelay = parseInt(retryAfter) * 1000;
    }
  }
});
```

### RFC 7807 Problem Details Error Handling

**Decision**: Define TypeScript interfaces matching ASP.NET Core's Problem Details format
- **Rationale**: ASP.NET Core 7+ uses RFC 7807 by default; type-safe error handling improves developer experience
- **References**:
  - [RFC 7807 Specification](https://datatracker.ietf.org/doc/html/rfc7807)
  - [ASP.NET Core Problem Details](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.problemdetails)

**TypeScript Interface**:
```typescript
interface ProblemDetails {
  type?: string;        // URI reference
  title: string;        // Human-readable summary
  status: number;       // HTTP status code
  detail?: string;      // Specific explanation
  instance?: string;    // URI to occurrence
  errors?: Record<string, string[]>; // Validation errors (ASP.NET format)
}
```

### Cache Strategy

**Decision**: 60-second default stale time with tag-based invalidation
- **Rationale**: Balances freshness with performance; world entities are relatively static creative content
- **Pattern**: Configure in base API, override per endpoint if needed

```typescript
createApi({
  // ...
  keepUnusedDataFor: 60, // Cache for 60 seconds
  refetchOnMountOrArgChange: 30, // Refetch if >30s since last fetch
})
```

### Testing Strategy

**Decision**: Test-first approach with Vitest + MSW (Mock Service Worker) for API mocking
- **Rationale**: TDD is non-negotiable per constitution; MSW provides realistic HTTP mocking
- **References**: [MSW with RTK Query](https://mswjs.io/docs/recipes/testing-react)

**Test Categories**:
1. **Unit Tests**: API slice logic, retry interceptors, error transformers
2. **Integration Tests**: Full request/response cycle with MSW mocking backend
3. **Accessibility Tests**: N/A for API client (headless), but error UI components must use jest-axe

---

## Phase 1: Design & Contracts

### Data Model

See [data-model.md](./data-model.md) for detailed entity schemas and relationships.

**Core Entities**:
- `World`: Root entity with id, name, description, createdAt, updatedAt
- `WorldEntity`: Hierarchical entity base (Continent, Country, Region, City, Character)
- `ProblemDetails`: RFC 7807 error response structure

**Type Organization**:
- Request DTOs: Create/Update payloads (e.g., `CreateWorldRequest`, `UpdateWorldRequest`)
- Response DTOs: API response shapes (e.g., `WorldResponse`, `WorldListResponse`)
- Error DTOs: `ProblemDetails`, `ValidationProblemDetails`

### API Contracts

See [contracts/](./contracts/) directory for OpenAPI-style TypeScript definitions.

**World API Endpoints**:
```typescript
// GET /api/worlds
useGetWorldsQuery(): { data: World[], isLoading, error }

// GET /api/worlds/{id}
useGetWorldByIdQuery(id: string): { data: World, isLoading, error }

// POST /api/worlds
useCreateWorldMutation(): [trigger, { data, isLoading, error }]

// PUT /api/worlds/{id}
useUpdateWorldMutation(): [trigger, { data, isLoading, error }]

// DELETE /api/worlds/{id}
useDeleteWorldMutation(): [trigger, { isSuccess, isLoading, error }]
```

**Cache Tags**:
- `World` tag: Invalidated on create/update/delete mutations
- Provides automatic refetching of world lists when data changes

### Quickstart Guide

See [quickstart.md](./quickstart.md) for developer onboarding.

**Adding a New Endpoint (5-minute workflow)**:
1. Define TypeScript types in `services/types/`
2. Add endpoint to appropriate API slice with `api.injectEndpoints()`
3. Export auto-generated hook
4. Write test with MSW handler
5. Use hook in component

**Example**:
```typescript
// 1. Define types
export interface Character { id: string; name: string; worldId: string; }

// 2. Inject endpoint
export const characterApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getCharacters: builder.query<Character[], string>({
      query: (worldId) => `/worlds/${worldId}/characters`,
      providesTags: ['Character'],
    }),
  }),
});

// 3. Export hook
export const { useGetCharactersQuery } = characterApi;

// 4. Use in component
const { data: characters, isLoading } = useGetCharactersQuery(worldId);
```

---

## Phase 2: Implementation Plan

### File Creation Order (TDD Approach)

#### 1. Type Definitions (No tests required - pure types)
- `src/services/types/problemDetails.types.ts` - RFC 7807 error types
- `src/services/types/world.types.ts` - World entity types
- `src/services/types/index.ts` - Barrel export

#### 2. Axios Client + Tests (Test-first)
- **Test**: `src/__tests__/services/apiClient.test.ts`
  - Test retry on 503 with exponential backoff
  - Test no retry on 400/401/403/404
  - Test Retry-After header respect for 429
  - Test timeout handling
- **Implementation**: `src/lib/apiClient.ts`
  - Axios instance with base config
  - axios-retry configuration
  - Request/response interceptors

#### 3. Base RTK Query API + Tests (Test-first)
- **Test**: `src/__tests__/services/api.test.ts`
  - Test base query configuration
  - Test error transformation to ProblemDetails
  - Test tag system setup
- **Implementation**: `src/services/api.ts`
  - `createApi` with custom `axiosBaseQuery`
  - Tag definitions (`['World', 'Character', ...]`)
  - Base configuration (60s cache)

#### 4. World API Slice + Tests (Test-first)
- **Test**: `src/__tests__/services/worldApi.test.ts`
  - Test GET /worlds with MSW mock
  - Test GET /worlds/{id} with MSW mock
  - Test POST /worlds with cache invalidation
  - Test PUT /worlds/{id} with cache invalidation
  - Test DELETE /worlds/{id} with cache invalidation
  - Test error handling (404, 500, network)
- **Implementation**: `src/services/worldApi.ts`
  - Inject world CRUD endpoints
  - Export typed hooks
  - Configure tags for cache invalidation

#### 5. Store Integration + Tests
- **Test**: `src/__tests__/store/store.test.ts`
  - Test RTK Query reducer integration
  - Test middleware configuration
- **Implementation**: `src/store/store.ts`
  - Add `api.reducer` to store
  - Add `api.middleware` to middleware chain
  - Preserve existing `sidePanel` slice

#### 6. Vite Configuration
- **No test** (infrastructure file)
- **Implementation**: `vite.config.ts`
  - Add proxy for `/api` routes
  - Use `process.env.APISERVICE_HTTPS || process.env.APISERVICE_HTTP`
  - Document in comments

#### 7. Documentation
- `specs/002-frontend-api-client/research.md` - Consolidate research findings
- `specs/002-frontend-api-client/data-model.md` - Entity schemas
- `specs/002-frontend-api-client/quickstart.md` - Developer guide
- `specs/002-frontend-api-client/contracts/` - Type definitions + examples
- `.env.example` - Document `VITE_API_BASE_URL` for deployed environments

### Package Dependencies to Add

```json
{
  "dependencies": {
    "axios": "^1.7.10",
    "axios-retry": "^4.5.1"
  },
  "devDependencies": {
    "msw": "^2.9.4"
  }
}
```

### Configuration Updates

#### vite.config.ts
```typescript
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    proxy: {
      '/api': {
        target: process.env.APISERVICE_HTTPS || process.env.APISERVICE_HTTP || 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
        rewrite: (path) => path, // Keep /api prefix
      }
    }
  }
})
```

#### .env.example (New file)
```env
# Local Development (auto-injected by Aspire AppHost)
# APISERVICE_HTTPS=https://localhost:7234
# APISERVICE_HTTP=http://localhost:5234

# Deployed Environments (Azure Static Web App / Container Apps)
VITE_API_BASE_URL=https://api.example.com
```

### Test Coverage Requirements

Per constitution (TDD non-negotiable), minimum **90% coverage** for:
- ✅ API client retry logic (all retry conditions)
- ✅ Error transformation (RFC 7807 parsing)
- ✅ Cache invalidation (tag system)
- ✅ CRUD operations (all endpoints)
- ✅ Loading/error states (RTK Query state machine)

**Coverage Tool**: Vitest built-in coverage with `v8` provider
**Command**: `pnpm test -- --coverage`

### Success Criteria Validation

After implementation, verify against spec success criteria:

| Criterion | Validation Method |
|-----------|------------------|
| SC-001: Add endpoint in <10 min | Time developer adding new endpoint following quickstart |
| SC-002: 80% cache hit rate | Instrument RTK Query cache hits in dev tools |
| SC-003: Zero crashes from API errors | Test all error scenarios with MSW |
| SC-004: 90% retry success rate | Test transient failure recovery |
| SC-005: 70% boilerplate reduction | Compare LOC with manual fetch implementation |
| SC-006: 90% test coverage | Run `pnpm test -- --coverage` and verify report |
| SC-007: 100% TypeScript safety | Compile with `tsc --noEmit` and ensure zero errors |
| SC-008: Debug in <2 min | Test error logging and structured error objects |

### Post-Implementation Checklist

- [ ] All tests passing with >90% coverage
- [ ] TypeScript compilation successful (zero errors)
- [ ] ESLint passing (zero warnings)
- [ ] Documentation complete (research, data-model, quickstart, contracts)
- [ ] `.env.example` created with documented variables
- [ ] Vite dev server working with Aspire proxy
- [ ] Example usage in quickstart.md tested and verified
- [ ] No accessibility regressions (API client is headless, but no new UI added)
- [ ] Constitution re-check passed (all principles compliant)

---

## Next Steps

1. **Run Planning Command**: This plan is complete. Next, execute `/speckit.tasks` to generate the detailed task breakdown in `tasks.md`.
2. **Begin TDD Cycle**: Start with Phase 2, step 1 (type definitions), then write tests before implementation for each subsequent component.
3. **Incremental Validation**: After each major component (apiClient, api, worldApi), verify tests pass and coverage meets requirements.
4. **Documentation as You Go**: Update research.md and quickstart.md with learnings during implementation.

**Estimated Effort**: 12-16 hours for a single developer following TDD approach with unfamiliar RTK Query patterns. Faster with prior RTK Query experience.
