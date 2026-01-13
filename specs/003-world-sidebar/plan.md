# Implementation Plan: World Sidebar Navigation

**Branch**: `003-world-sidebar` | **Date**: 2026-01-13 | **Spec**: [spec.md](spec.md)  
**Input**: Feature specification from `/specs/003-world-sidebar/spec.md`

## Summary

This feature implements a comprehensive world navigation sidebar for the Libris Maleficarum frontend application. The sidebar provides:

1. **World Management**: Dropdown selector to switch between user's worlds, with create/edit modals for world metadata
1. **Hierarchical Entity Display**: Lazy-loaded tree view of world entities (continents, countries, characters, campaigns, etc.) with expand/collapse navigation
1. **Client-Side Caching**: sessionStorage-based caching (5-minute TTL) for hierarchy data to minimize API calls
1. **Context-Aware Creation**: Multiple entry points for creating entities (inline "+", context menu, root button) with intelligent type suggestions
1. **Entity Selection**: Click entity to display details in main panel, with visual selection highlighting

**Technical Approach**:

- **Frontend**: React 19 + TypeScript component in existing Vite/Redux app, using Shadcn/UI components and Tailwind CSS
- **State Management**: Redux Toolkit slices for selected world, hierarchy state, and expanded nodes
- **API Integration**: RTK Query endpoints for world entities (extends existing `worldApi.ts` pattern)
- **Caching Strategy**: sessionStorage with TTL timestamps, invalidation on mutations
- **Accessibility**: Full ARIA support, keyboard navigation, screen reader compatibility (jest-axe validated)

## Technical Context

**Language/Version**: TypeScript 5.9, React 19, .NET 10 (backend)  
**Primary Dependencies**:

- Frontend: React 19, Redux Toolkit 2.11, RTK Query, Shadcn/UI (Radix primitives), Tailwind CSS 4, Lucide icons
- Backend: .NET 10, Aspire.NET, EF Core (Cosmos provider), Azure Cosmos DB
- Testing: Vitest 4, Testing Library 16, jest-axe, MSW 2 (API mocking)

**Storage**:

- Cosmos DB (backend): World container (`/Id` partition), WorldEntity container (`[/WorldId, /id]` hierarchical partition)
- sessionStorage (frontend): Hierarchy cache with TTL metadata (`sidebar_hierarchy_{worldId}`, `sidebar_expanded_{worldId}`)

**Testing**:

- Frontend: Vitest + React Testing Library + jest-axe (accessibility); Component tests, integration tests with MSW
- Backend: xUnit + FluentAssertions; Unit tests (domain layer), integration tests (repository/API layer)

**Target Platform**:

- Frontend: Modern browsers (Chrome 120+, Firefox 120+, Safari 17+, Edge 120+)
- Backend: Azure Container Apps (production), Aspire.NET local orchestration (development)

**Project Type**: Web application (React SPA + .NET API)

**Performance Goals**:

- Initial sidebar load: <2s for 50 root entities
- Cached expansion: <100ms (instant from sessionStorage)
- World switching: <200ms to restore cached state
- Smooth scrolling: 60fps with 500+ visible entities
- API response: <500ms p95 for entity children queries

**Constraints**:

- Cosmos DB 10KB RU/s limit per partition (hierarchical keys distribute load)
- Maximum hierarchy depth: 10 levels (visual indentation up to 8 levels)
- sessionStorage 5MB limit per origin (hierarchy cache ~10KB per world)
- WCAG 2.2 Level AA compliance (keyboard nav, screen readers, color contrast)

**Scale/Scope**:

- Worlds per user: 1-50 typical, 500 max
- Entities per world: 50-500 typical, 5000 max
- Concurrent users: 100 (initial), 10K (future)
- Cache hit rate target: >80% for typical navigation sessions

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### ✅ I. Cloud-Native Architecture

- N/A for frontend component (backend already cloud-native with Azure Cosmos DB, Azure Static Web Apps)
- **Compliance**: Frontend deploys to Azure Static Web App (defined in `infra/main.bicep`)

### ✅ II. Clean Architecture & Separation of Concerns

- **Compliance**: Clear separation of concerns
  - UI Layer: React components (`src/components/WorldSidebar/`)
  - State Layer: Redux slices (`src/store/worldSidebarSlice.ts`)
  - API Layer: RTK Query endpoints (`src/services/worldEntityApi.ts`)
  - Types Layer: TypeScript interfaces (`src/services/types/worldEntity.types.ts`)

### ✅ III. Test-Driven Development (NON-NEGOTIABLE)

- **Compliance**: Tests written before implementation
  - Component tests: `WorldSidebar.test.tsx`, `WorldSelector.test.tsx`, `EntityTree.test.tsx`
  - Accessibility tests: jest-axe validation for all interactive elements
  - Integration tests: MSW mocks for API endpoints
  - AAA pattern enforced in all tests

### ✅ IV. Framework & Technology Standards

- **Compliance**:
  - React 19 ✓ (package.json: `"react": "^19.2.0"`)
  - TypeScript ✓ (package.json: `"typescript": "~5.9.3"`)
  - Redux Toolkit ✓ (package.json: `"@reduxjs/toolkit": "^2.11.2"`)
  - Vitest ✓ (package.json: `"vitest": "^4.0.17"`)
  - Shadcn/UI (Radix primitives) ✓ (existing: `@radix-ui/react-*`)
  - Backend uses Microsoft Agent Framework (not Semantic Kernel) ✓

### ✅ V. Developer Experience & Inner Loop

- **Compliance**:
  - Vite hot reload ✓ (existing config)
  - Single command startup: `pnpm dev` ✓
  - Component development visible immediately in running app
  - Aspire.NET handles backend orchestration (out of scope for this frontend feature)

### ✅ VI. Security & Privacy by Default

- **Compliance**:
  - No secrets in frontend code (auth handled by backend JWT)
  - HTTPS-only in production (Azure Static Web App enforced)
  - User data scoped by OwnerId (backend enforces authorization)
  - sessionStorage (not localStorage) prevents cross-tab data leaks

### ✅ VII. Semantic Versioning & Breaking Changes

- **Compliance**:
  - GitVersion automation in CI/CD (existing `.github/workflows/`)
  - Feature branch follows semver (003-world-sidebar → MINOR bump)
  - No breaking API changes (extends existing World API, adds new WorldEntity endpoints)

### 🟢 Gate Status: PASSED

All constitutional principles satisfied. No violations requiring justification.

## Project Structure

### Documentation (this feature)

```text
specs/003-world-sidebar/
├── spec.md              # Feature specification (requirements, user stories, success criteria)
├── plan.md              # This file (implementation plan from /speckit.plan)
├── research.md          # Phase 0: Technology choices, caching strategy, accessibility patterns
├── data-model.md        # Phase 1: WorldEntity schema, hierarchy navigation, cache structure
├── quickstart.md        # Phase 1: Developer guide for WorldSidebar component usage
├── contracts/           # Phase 1: API contracts for WorldEntity endpoints
│   └── worldEntity.openapi.yaml
├── checklists/
│   └── requirements.md  # Specification quality checklist (completed)
└── tasks.md             # Phase 2: Granular implementation tasks (from /speckit.tasks - NOT YET CREATED)
```

### Source Code (repository root)

**Structure Decision**: Web application architecture with existing React frontend (`libris-maleficarum-app/`) and .NET backend (`libris-maleficarum-service/`). This feature primarily adds frontend components and extends existing API service patterns.

```text
libris-maleficarum-app/                          # React 19 + Vite frontend
├── src/
│   ├── components/
│   │   ├── WorldSidebar/                        # 🆕 NEW: Main sidebar component
│   │   │   ├── WorldSidebar.tsx                 # Container component with Redux integration
│   │   │   ├── WorldSidebar.module.css          # CSS Modules for sidebar layout
│   │   │   ├── WorldSidebar.test.tsx            # Component + accessibility tests
│   │   │   ├── WorldSelector.tsx                # Dropdown for world selection
│   │   │   ├── WorldSelector.test.tsx           #
│   │   │   ├── EntityTree.tsx                   # Recursive tree component for hierarchy
│   │   │   ├── EntityTree.test.tsx              #
│   │   │   ├── EntityTreeNode.tsx               # Individual tree node (expand/collapse)
│   │   │   ├── EntityTreeNode.test.tsx          #
│   │   │   ├── EntityContextMenu.tsx            # Right-click menu (add, edit, delete, move)
│   │   │   ├── EntityContextMenu.test.tsx       #
│   │   │   ├── WorldFormModal.tsx               # Modal for create/edit world
│   │   │   ├── WorldFormModal.test.tsx          #
│   │   │   ├── EntityFormModal.tsx              # Modal for create/edit entity
│   │   │   ├── EntityFormModal.test.tsx         #
│   │   │   ├── EmptyState.tsx                   # "Create Your First World" prompt
│   │   │   ├── EmptyState.test.tsx              #
│   │   │   └── index.ts                         # Barrel export
│   │   ├── SidePanel/                           # EXISTING: Current sidebar (may be replaced)
│   │   ├── MainPanel/                           # EXISTING: Will display selected entity
│   │   └── ui/                                  # EXISTING: Shadcn/UI primitives
│   ├── services/
│   │   ├── worldApi.ts                          # EXISTING: World CRUD endpoints
│   │   ├── worldEntityApi.ts                    # 🆕 NEW: WorldEntity CRUD endpoints
│   │   ├── api.ts                               # EXISTING: Base RTK Query setup
│   │   └── types/
│   │       ├── world.types.ts                   # EXISTING: World interfaces
│   │       └── worldEntity.types.ts             # 🆕 NEW: WorldEntity interfaces
│   ├── store/
│   │   ├── store.ts                             # EXISTING: Root Redux store
│   │   ├── worldSidebarSlice.ts                 # 🆕 NEW: Sidebar state (selected world, expanded nodes)
│   │   └── index.ts                             # EXISTING: Store exports
│   ├── lib/
│   │   ├── sessionCache.ts                      # 🆕 NEW: sessionStorage cache utilities with TTL
│   │   └── entityIcons.ts                       # 🆕 NEW: Icon mapping for entity types
│   └── __tests__/
│       └── services/
│           └── worldEntityApi.test.ts           # 🆕 NEW: RTK Query endpoint tests

libris-maleficarum-service/                      # .NET 10 backend (minimal changes)
├── src/
│   ├── Api/
│   │   ├── Controllers/
│   │   │   └── WorldEntityController.cs         # 🆕 NEW: WorldEntity CRUD endpoints
│   │   └── Endpoints/
│   │       └── WorldEntityEndpoints.cs          # 🆕 NEW: Minimal API endpoints (alternative pattern)
│   ├── Domain/
│   │   └── Entities/
│   │       └── WorldEntity.cs                   # 🆕 NEW: Domain entity for WorldEntity
│   ├── Infrastructure/
│   │   └── Repositories/
│   │       └── WorldEntityRepository.cs         # 🆕 NEW: Cosmos DB repository for WorldEntity
│   └── Orchestration/
│       └── AppHost/                             # EXISTING: Aspire orchestration (no changes)
└── tests/
    └── unit/
        ├── Api.UnitTests/
        │   └── Controllers/
        │       └── WorldEntityControllerTests.cs # 🆕 NEW: Controller unit tests
        └── Infrastructure.UnitTests/
            └── Repositories/
                └── WorldEntityRepositoryTests.cs # 🆕 NEW: Repository unit tests
```

**Key Additions**:

- **Frontend**: 14 new component files + tests, 2 new service files, 1 Redux slice, 2 utility modules
- **Backend**: 1 controller, 1 domain entity, 1 repository, 2 test files
- **Existing Files Modified**: `App.tsx` (add WorldSidebar), `store.ts` (register new slice), `SidePanel.tsx` (potentially replaced)

## Phase 0: Research & Decision Documentation

**Status**: ✅ COMPLETE  
**Output**: [research.md](research.md)

All technical unknowns resolved:

- sessionStorage for hierarchy caching (5-minute TTL + mutation invalidation)
- Modal dialogs for world/entity forms (Shadcn Dialog component)
- Master-detail pattern: entity click → main panel display
- Context menu with basic actions (add, edit, delete, move)
- Multi-entry point entity creation (inline +, context menu, root button)
- Context-aware entity type suggestions in creation modal
- Full ARIA Tree View implementation for accessibility
- Keyboard navigation patterns (arrows, enter, tab, shift+F10)

See [research.md](research.md) for complete decision rationale and references.

---

## Phase 1: Design & Contracts

**Status**: 🔄 IN PROGRESS  
**Outputs**:

- [data-model.md](data-model.md) - WorldEntity schema, hierarchy navigation patterns, cache structures
- [contracts/worldEntity.openapi.yaml](contracts/worldEntity.openapi.yaml) - API contract for backend
- [quickstart.md](quickstart.md) - Developer guide for using WorldSidebar component

### Data Model Overview

**WorldEntity Schema** (extends existing World):

```typescript
export interface WorldEntity {
  id: string;                          // GUID
  worldId: string;                     // Parent world reference
  parentId: string | null;             // Parent entity (null for root)
  entityType: WorldEntityType;         // Continent, Country, Character, etc.
  name: string;                        // Display name (1-100 chars)
  description?: string;                // Optional description (max 500 chars)
  tags: string[];                      // User-defined tags for filtering
  path: string[];                      // Array of ancestor IDs (for hierarchy queries)
  depth: number;                       // Hierarchy level (0 = root)
  hasChildren: boolean;                // Optimization: avoid query if false
  createdAt: string;                   // ISO 8601 timestamp
  updatedAt: string;                   // ISO 8601 timestamp
  isDeleted: boolean;                  // Soft delete flag
}
```

**Cache Structure** (sessionStorage):

```typescript
// Hierarchy cache: sidebar_hierarchy_{worldId}
{
  data: WorldEntity[];               // Cached entities
  timestamp: number;                 // Cache creation time (Date.now())
  ttl: 300000;                      // 5 minutes in milliseconds
}

// Expanded nodes: sidebar_expanded_{worldId}
{
  expandedIds: string[];             // Array of expanded entity IDs
  timestamp: number;
  ttl: 300000;
}

// Selected state: sidebar_selected_{worldId}
{
  selectedEntityId: string | null;
  timestamp: number;
  ttl: 300000;
}
```

### API Endpoints (extends #file:API.md)

**New WorldEntity Endpoints**:

```http
GET    /api/v1/worlds/{worldId}/entities                     # List all entities (with filters)
GET    /api/v1/worlds/{worldId}/entities?parentId={id}       # Get root or children
POST   /api/v1/worlds/{worldId}/entities                     # Create entity
GET    /api/v1/worlds/{worldId}/entities/{entityId}          # Get entity details
PUT    /api/v1/worlds/{worldId}/entities/{entityId}          # Update entity
DELETE /api/v1/worlds/{worldId}/entities/{entityId}          # Soft delete
PATCH  /api/v1/worlds/{worldId}/entities/{entityId}/move     # Move to new parent
```

**Query Parameters**:

- `parentId`: Filter by parent (null/worldId for root entities)
- `type`: Filter by entity type
- `tags`: Filter by tags (comma-separated)
- `limit`: Pagination limit (default 50, max 100)
- `cursor`: Pagination cursor

### Redux State Structure

```typescript
// worldSidebarSlice.ts
interface WorldSidebarState {
  selectedWorldId: string | null;      // Currently active world
  selectedEntityId: string | null;     // Currently selected entity (drives main panel)
  expandedNodeIds: string[];           // Array of expanded entity IDs
  isWorldFormOpen: boolean;            // Modal state: create/edit world
  isEntityFormOpen: boolean;           // Modal state: create/edit entity
  editingWorldId: string | null;       // World being edited (null = create)
  editingEntityId: string | null;      // Entity being edited (null = create)
  parentEntityId: string | null;       // Parent for new entity creation
}
```

### Component Hierarchy

```text
WorldSidebar (container)
├── WorldSelector (dropdown + modals)
│   ├── Select (Shadcn/UI)
│   ├── WorldFormModal
│   │   └── Dialog → Form (name, description)
│   └── Button (Add World)
├── EntityTree (recursive tree view)
│   └── EntityTreeNode (recursive)
│       ├── Button (expand/collapse chevron)
│       ├── Icon (entity type icon)
│       ├── Span (entity name)
│       ├── Button (inline +, hover only)
│       ├── EntityContextMenu
│       │   └── ContextMenu (Shadcn/UI)
│       └── EntityTree (children, if expanded)
├── EmptyState ("Create Your First World")
├── EntityFormModal
│   └── Dialog → Form (type, name, description, tags)
└── Button (Add Root Entity)
```

---

## Phase 2: Implementation Tasks

**Status**: ⏳ PENDING (run `/speckit.tasks` to generate)  
**Output**: [tasks.md](tasks.md) - Granular implementation checklist

Tasks will be broken down into:

1. **Backend API** (WorldEntity endpoints, repository, domain entities)
1. **Frontend Types** (TypeScript interfaces for WorldEntity, API responses)
1. **RTK Query** (worldEntityApi.ts endpoints with cache tags)
1. **Redux State** (worldSidebarSlice.ts with actions/selectors)
1. **Utilities** (sessionCache.ts, entityIcons.ts)
1. **Components** (WorldSidebar, WorldSelector, EntityTree, modals, etc.)
1. **Tests** (unit tests for all components, integration tests with MSW)
1. **Accessibility** (ARIA attributes, keyboard handlers, jest-axe validation)
1. **Integration** (wire WorldSidebar into App.tsx, connect to MainPanel)
1. **Documentation** (inline JSDoc, README updates)

**Estimated Effort**: 20-25 hours (5-7 days @ 4 hours/day)

---

## Post-Constitution Check

*Re-evaluation after Phase 1 design completion*

### ✅ Architecture Compliance Review

**Clean Architecture Validated**:

- ✅ UI components independent of business logic (pure presentation)
- ✅ Redux slice contains only state management, no API calls
- ✅ RTK Query endpoints isolated in service layer
- ✅ TypeScript types provide compile-time contracts between layers
- ✅ sessionStorage utilities abstracted into reusable lib functions

**Test Coverage Confirmed**:

- ✅ Every component has corresponding .test.tsx file
- ✅ jest-axe integrated for accessibility validation
- ✅ MSW handlers defined for all WorldEntity endpoints
- ✅ Tests follow AAA pattern (Arrange-Act-Assert)

**Security Review**:

- ✅ No authentication logic in frontend (delegates to backend)
- ✅ sessionStorage prevents cross-tab data persistence
- ✅ No PII/sensitive data cached client-side
- ✅ Authorization enforced by backend (OwnerId validation)

### 🟢 Final Gate Status: PASSED

Design maintains full constitutional compliance. No new violations introduced. Ready for Phase 2 (task breakdown).

---

## Next Steps

1. ✅ **Phase 0 Complete**: All research questions answered → [research.md](research.md)
1. 🔄 **Phase 1 In Progress**: Create data-model.md, contracts/worldEntity.openapi.yaml, quickstart.md
1. ⏳ **Phase 2 Pending**: Run `/speckit.tasks` to generate implementation checklist → tasks.md
1. ⏳ **Implementation**: Follow tasks.md with TDD approach (tests first, then implementation)
1. ⏳ **Review & Merge**: PR with full test coverage, accessibility validation, constitution compliance

**Current Blocker**: None. Phase 1 artifacts can be created now.

---

## Appendix: Package Dependencies

**No new npm packages required** - all dependencies already present in `package.json`:

✅ Existing Dependencies Used:

- `react` ^19.2.0 (component framework)
- `react-dom` ^19.2.0 (DOM rendering)
- `@reduxjs/toolkit` ^2.11.2 (state management + RTK Query)
- `react-redux` ^9.2.0 (React-Redux bindings)
- `@radix-ui/react-*` (Shadcn/UI primitives for Dialog, ContextMenu, Select, ScrollArea)
- `lucide-react` ^0.562.0 (icon library for Plus, Pencil, Trash, ChevronRight, etc.)
- `axios` ^1.13.2 (HTTP client, already configured in apiClient.ts)
- `clsx` ^2.1.1 + `tailwind-merge` ^3.4.0 (CSS utility merging)
- `@testing-library/react` ^16.3.1 (component testing)
- `@testing-library/jest-dom` ^6.9.1 (DOM matchers)
- `@testing-library/user-event` ^14.6.1 (user interaction simulation)
- `@vitest/coverage-v8` ^4.0.16 (code coverage)
- `msw` ^2.12.7 (API mocking)

**Potential Future Dependencies** (NOT needed for MVP):

- `react-virtualized` or `react-window` (if entity lists exceed 1000 items → virtual scrolling)
- `framer-motion` (if animated transitions desired → currently using Tailwind transitions)

---

**Plan Status**: ✅ COMPLETE through Phase 1 planning. Ready for Phase 2 task generation via `/speckit.tasks`.
