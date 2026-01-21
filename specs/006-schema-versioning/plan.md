# Implementation Plan: Schema Versioning for WorldEntities

**Branch**: `006-schema-versioning` | **Date**: 2026-01-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/006-schema-versioning/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Add `SchemaVersion` integer field to all WorldEntity documents to enable schema evolution over time. The frontend will automatically upgrade entities to the latest schema version on save ("lazy migration"), while the backend validates version ranges (min/max per entity type) to prevent downgrades and use of unsupported versions. This foundational feature enables future schema migrations without requiring bulk updates.

**Key Technical Decisions**:
- Frontend stores current schema versions in centralized constants file (`ENTITY_SCHEMA_VERSIONS`)
- Backend maintains min/max supported versions per entity type in configuration
- Schemas can only be extended (fields added), never contracted (fields removed)
- Missing `SchemaVersion` treated as version `1` for backward compatibility
- Detailed error responses with codes (SCHEMA_VERSION_INVALID, SCHEMA_VERSION_TOO_HIGH, etc.)

## Technical Context

**Frontend**:
- **Language/Version**: TypeScript 5.x + React 19 + Vite 6
- **Primary Dependencies**: Redux Toolkit 2.x, Vitest 2.x, React Testing Library 16.x, jest-axe, TailwindCSS 4, Shadcn/ui
- **Testing**: Vitest with React Testing Library + jest-axe for accessibility
- **Target Platform**: Modern browsers (ES2022+)
- **State Management**: Redux Toolkit with typed RootState/AppDispatch

**Backend**:
- **Language/Version**: C# 14 + .NET 10
- **Primary Dependencies**: ASP.NET Core 10, EF Core 10 (Cosmos DB provider), Aspire.NET 10, MSTest, FluentAssertions
- **Storage**: Azure Cosmos DB (WorldEntity container with hierarchical partition key `[/WorldId, /id]`)
- **Testing**: MSTest with FluentAssertions, separate unit and integration test categories
- **Target Platform**: Linux containers (Azure Container Apps for production, Aspire AppHost for local dev)

**Project Type**: Web application (React SPA frontend + ASP.NET Core REST API backend)

**Performance Goals**: 
- API: <100ms p95 for point reads (WorldEntity by ID), <200ms for entity list queries
- Frontend: <16ms component render time, <100ms state update latency

**Constraints**: 
- Cosmos DB documents limited to 2MB (not impacted—SchemaVersion adds ~10 bytes)
- No breaking changes to existing API contracts (additive only)
- Backward compatibility: existing entities without `SchemaVersion` must work
- All changes must pass existing lint/test suites

**Scale/Scope**: 
- ~1500 existing WorldEntity documents across test/dev environments
- 2 entity type constants files (frontend + backend)
- 4 code projects affected (Domain, Infrastructure, Api backend + frontend app)
- ~15 test files to update (backend entity tests + frontend API client tests)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Cloud-Native Architecture** | ✅ PASS | No infrastructure changes; schema version stored in Cosmos DB documents as integer field |
| **II. Clean Architecture** | ✅ PASS | Domain entity updated (WorldEntity), API layer adds validation, no cross-layer violations |
| **III. Test-Driven Development** | ✅ PASS | All changes require tests before implementation (entity tests, API tests, frontend tests with jest-axe) |
| **IV. Framework & Technology Standards** | ✅ PASS | Uses existing stack (React 19, .NET 10, EF Core, Redux Toolkit); no new frameworks |
| **V. Developer Experience** | ✅ PASS | No changes to Aspire AppHost or local dev workflow |
| **VI. Security & Privacy** | ✅ PASS | No security implications; schema version is metadata, not sensitive data |
| **VII. Semantic Versioning** | ✅ PASS | No breaking changes to API contracts (additive field only); minor version bump |

**Overall**: ✅ **ALL GATES PASS** - No constitutional violations. Feature is fully compliant.

## Project Structure

### Documentation (this feature)

```text
specs/006-schema-versioning/
├── spec.md              # Feature specification (completed)
├── checklists/
│   └── requirements.md  # Specification quality checklist (completed)
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command - COMPLETED)
├── data-model.md        # Phase 1 output (/speckit.plan command - COMPLETED)
├── quickstart.md        # Phase 1 output (/speckit.plan command - COMPLETED)
├── contracts/           # Phase 1 output (/speckit.plan command - COMPLETED)
│   ├── world-entity-create-request.json
│   ├── world-entity-update-request.json
│   ├── world-entity-response.json
│   └── schema-version-error-response.json
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
libris-maleficarum-service/                        # Backend (.NET 10 API)
├── src/
│   ├── Domain/
│   │   ├── Entities/
│   │   │   └── WorldEntity.cs                    # UPDATE: Add SchemaVersion property
│   │   ├── Configuration/
│   │   │   └── EntitySchemaVersionConfig.cs      # NEW: Min/max schema version config
│   │   └── Exceptions/
│   │       └── SchemaVersionException.cs         # NEW: Schema version validation exceptions
│   ├── Infrastructure/
│   │   ├── Data/
│   │   │   └── Configurations/
│   │   │       └── WorldEntityConfiguration.cs   # UPDATE: Map SchemaVersion to Cosmos DB
│   │   └── appsettings.json                      # UPDATE: Add entity schema version config section
│   └── Api/
│       ├── Controllers/
│       │   └── WorldEntityController.cs          # UPDATE: Validate SchemaVersion in requests
│       ├── DTOs/
│       │   ├── CreateWorldEntityRequest.cs       # UPDATE: Add SchemaVersion property
│       │   ├── UpdateWorldEntityRequest.cs       # UPDATE: Add SchemaVersion property
│       │   └── WorldEntityResponse.cs            # UPDATE: Add SchemaVersion property
│       └── Validators/
│           └── SchemaVersionValidator.cs         # NEW: Schema version range validation logic
└── tests/
    ├── Domain.Tests/
    │   └── Entities/
    │       └── WorldEntityTests.cs               # UPDATE: Add SchemaVersion property tests
    ├── Infrastructure.Tests/
    │   └── Data/
    │       └── WorldEntityRepositoryTests.cs     # UPDATE: Verify SchemaVersion persistence
    └── Api.Tests/
        └── Controllers/
            └── WorldEntityControllerTests.cs     # UPDATE: Add schema version validation tests

libris-maleficarum-app/                            # Frontend (React 19 + TypeScript)
├── src/
│   ├── services/
│   │   ├── types/
│   │   │   ├── worldEntity.types.ts              # UPDATE: Add schemaVersion to WorldEntity interface
│   │   │   └── worldEntity.types.test.ts         # UPDATE: Add schemaVersion to type tests
│   │   ├── worldEntityApi.ts                     # UPDATE: Include schemaVersion in requests
│   │   └── constants/
│   │       └── entitySchemaVersions.ts           # NEW: ENTITY_SCHEMA_VERSIONS constant map
│   ├── components/
│   │   └── MainPanel/
│   │       ├── EntityDetailForm.tsx              # UPDATE: Use schema version from constants
│   │       └── __tests__/
│   │           └── EntityDetailForm.test.tsx     # UPDATE: Verify schemaVersion in requests
│   └── store/
│       └── worldSidebarSlice.ts                  # UPDATE: Preserve schemaVersion in Redux state
└── tests/
    ├── __tests__/
    │   └── services/
    │       └── worldEntityApi.test.ts            # UPDATE: Add schemaVersion to API client tests
    └── integration/
        ├── entityCreation.test.tsx               # UPDATE: Verify schemaVersion in create flow
        └── entityEdit.test.tsx                   # NEW: Verify schema version auto-upgrade on save

docs/
└── design/
    ├── DATA_MODEL.md                              # UPDATE: Document SchemaVersion field in BaseWorldEntity
    └── API.md                                     # UPDATE: Add SchemaVersion to example payloads
```

**Structure Decision**: Web application with existing React frontend (`libris-maleficarum-app/`) and .NET backend (`libris-maleficarum-service/`). Changes are additive to existing projects—no new projects or major restructuring required. Frontend uses centralized constants file for schema versions; backend uses appsettings.json configuration for min/max version ranges.

## Complexity Tracking

> **No complexity violations detected**

This feature introduces no architectural complexity beyond constitutional guidelines. All changes are additive to existing Clean Architecture layers (Domain, Infrastructure, Api, Frontend). No new projects, patterns, or frameworks required.
