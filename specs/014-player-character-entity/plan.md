# Implementation Plan: Player Character Entity Type

**Branch**: `014-player-character-entity` | **Date**: 2026-02-18 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/014-player-character-entity/spec.md`

## Summary

Add a `PlayerCharacter` entity type to the Entity Type Registry, enabling users to create D&D 5th edition player character entities with 18 roleplaying-focused custom properties. This is an additive, data-only change that extends both the .NET 10 backend (enum + schema config) and the React 19+ frontend (registry entry, icon mapping, parent entity updates, tests). No new API endpoints required—PlayerCharacter uses existing WorldEntity CRUD operations. The backend stores custom properties as opaque JSON in the `Attributes` field; the frontend registry defines the property schema that generates the form UI.

## Technical Context

**Language/Version**: TypeScript 5.x (React 19+ frontend) + C# 14 (.NET 10 backend)
**Primary Dependencies**: React 19, Redux Toolkit, lucide-react, Vitest, Testing Library, jest-axe (frontend) | EF Core Cosmos DB, FluentValidation, Aspire.NET (backend)
**Storage**: Azure Cosmos DB (partition key `/WorldId`, `Attributes` stored as JSON string)
**Testing**: Vitest + Testing Library + jest-axe (frontend) | dotnet test with MSTest + FluentAssertions (backend)
**Target Platform**: Web application (Azure Static Web App + Azure Container Apps)
**Project Type**: Web (frontend + backend)
**Performance Goals**: N/A — additive data-only change, no new endpoints or rendering paths
**Constraints**: Backend EntityType enum change must be additive (non-breaking); Cosmos DB item < 2MB; Backstory field max 2000 chars
**Scale/Scope**: 1 new enum value, 1 registry entry with 18 custom properties, 6 parent entity suggestedChildren updates, icon mapping in 2 files, test count updates

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Cloud-Native Architecture | PASS | No infra changes. Uses existing Cosmos DB partition strategy. |
| II | Clean Architecture & Separation | PASS | Backend: enum in Domain/ValueObjects, config in appsettings. Frontend: registry pattern, icon mapping separated from components. |
| III | Test-Driven Development (NON-NEGOTIABLE) | PASS | Backend unit tests for enum validation. Frontend: registry tests (count 29→30), accessibility tests with jest-axe, extensibility tests. TDD order in quickstart.md. |
| IV | Framework & Technology Standards | PASS | React 19+ with TypeScript, .NET 10, Vitest, Redux Toolkit. Shadcn/UI + TailwindCSS for styling. |
| V | Developer Experience & Inner Loop | PASS | No orchestration changes. Existing Aspire AppHost and Vite dev server work unchanged. |
| VI | Security & Privacy by Default | PASS | No secrets, no new endpoints, no auth changes. Attributes stored as JSON within existing security boundary. |
| VII | Semantic Versioning | PASS | Additive enum change = MINOR version bump. No breaking changes. Commit uses `+semver: minor`. |

**Gate Result**: ALL PASS — no violations, no justifications needed.

## Project Structure

### Documentation (this feature)

```text
specs/014-player-character-entity/
├── plan.md              # This file
├── research.md          # Phase 0 output — 6 research findings
├── data-model.md        # Phase 1 output — 18 custom properties documented
├── quickstart.md        # Phase 1 output — 8-step implementation order
├── contracts/
│   └── api-contracts.md # Phase 1 output — existing endpoint usage examples
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
libris-maleficarum-service/                    # Backend (.NET 10)
├── src/
│   ├── Domain/
│   │   └── ValueObjects/
│   │       └── EntityType.cs                  # ADD: PlayerCharacter enum value
│   ├── Api/
│   │   └── appsettings.json                   # ADD: PlayerCharacter schema version entry
│   └── Infrastructure/                         # No changes needed
└── tests/
    └── unit/
        ├── Api.Tests/                          # VERIFY: EntityType enum validation (existing)
        └── Infrastructure.Tests/               # VERIFY: No changes expected

libris-maleficarum-app/                        # Frontend (React 19 + TypeScript)
├── src/
│   ├── services/
│   │   └── config/
│   │       └── entityTypeRegistry.ts          # ADD: PlayerCharacter registry entry (18 properties)
│   ├── lib/
│   │   └── entityIcons.ts                     # ADD: PlayerCharacter to EntityType union + iconMap
│   ├── components/
│   │   └── shared/
│   │       └── EntityTypeSelector/
│   │           └── EntityTypeSelector.tsx      # ADD: UserCheck import + iconMap entry
│   └── __tests__/
│       ├── services/
│       │   └── entityTypeRegistry.test.ts     # UPDATE: count 29→30
│       └── config/
│           └── entityTypeRegistry.test.ts     # UPDATE: count 29→30, add to expectedRootTypes
└── tests/                                      # No changes to visual/e2e tests
```

**Structure Decision**: Web application — frontend (`libris-maleficarum-app/`) + backend (`libris-maleficarum-service/`). Both already exist. No new directories or projects required. All changes are modifications to existing files within established patterns.

## Complexity Tracking

> No violations detected. This is a purely additive, data-only change following established patterns.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| *(none)* | — | — |
