# Implementation Plan: Container Entity Type Support

**Branch**: `004-entity-type-support` | **Date**: 2026-01-20 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-entity-type-support/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Add support for Container EntityTypes (Locations, People, Events, Lore, Items, Adventures, Geographies, History, Bestiary, Other) and Standard EntityTypes with custom properties (GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion) to the WorldSidebar hierarchy and EntityDetailForm. Container types provide top-level organization under World entities, while regional types support domain-specific properties (Climate, Population, Area, Government Type) stored in the flexible Properties JSON field. Implementation uses TypeScript enums for type definitions, conditional rendering in EntityDetailForm for custom property fields, and a reusable TagInput component for text list properties. All entity types use the same BaseWorldEntity schema with no schema migrations required.

## Technical Context

**Language/Version**: TypeScript 5.x (frontend), C# 14 / .NET 10 (backend - not in scope for this feature)
**Primary Dependencies**: React 19, Fluent UI v9, Redux Toolkit, Vitest, Testing Library, jest-axe
**Storage**: Azure Cosmos DB (existing WorldEntity container, no schema changes required)
**Testing**: Vitest + Testing Library + jest-axe for frontend; accessibility testing mandatory per TDD principle
**Target Platform**: Modern browsers (Chrome, Edge, Firefox, Safari) via Vite dev server and static build
**Project Type**: Web application (React frontend, .NET backend - frontend-only changes)
**Performance Goals**: EntityTypeSelector search <100ms response, entity creation <5s on standard broadband, hierarchy rendering <3s for typical trees
**Constraints**: No backend/API changes in this iteration, read-only custom properties, no schema migrations, JavaScript Number.MAX_SAFE_INTEGER for numeric fields
**Scale/Scope**: 10 new Container EntityTypes + 4 regional types with custom properties, ~5 new React components/utilities, TagInput reusable component

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Initial Check (Pre-Research)

| Principle | Compliance | Notes |
|-----------|------------|-------|
| **I. Cloud-Native Architecture** | ✅ PASS | Uses existing Azure Cosmos DB; no new infrastructure. Frontend-only changes compatible with Static Web App deployment. |
| **II. Clean Architecture** | ✅ PASS | Frontend maintains separation: UI components (EntityDetailForm, WorldSidebar), state (Redux), types (worldEntity.types.ts). No backend changes. |
| **III. Test-Driven Development** | ✅ PASS | All new components require .test.tsx files with jest-axe accessibility testing. TagInput, EntityTypeSelector, EntityDetailForm property rendering all testable. |
| **IV. Framework Standards** | ✅ PASS | Uses React 19 + TypeScript, Fluent UI v9 (TagInput likely uses Fluent components), Redux Toolkit for state, Vitest for testing. |
| **V. Developer Experience** | ✅ PASS | Changes run in existing Vite dev server with hot reload. No Aspire.NET changes (backend not modified). |
| **VI. Security & Privacy** | ✅ PASS | No secrets, no auth changes. Custom properties stored in Cosmos DB Properties field (existing security model applies). |
| **VII. Semantic Versioning** | ✅ PASS | Feature adds new types (MINOR bump). Breaking changes avoided: existing EntityTypes unaffected, Properties field already flexible. |

**Overall Assessment**: ✅ **COMPLIANT** - All constitutional principles satisfied. Frontend-only feature with no infrastructure, security, or architecture violations.

---

### Post-Design Check (After Phase 1)

| Principle | Compliance | Design Impact |
|-----------|------------|---------------|
| **I. Cloud-Native Architecture** | ✅ PASS | Design confirms zero infrastructure changes. Uses existing Cosmos DB WorldEntity container with Properties field. No new Azure resources. |
| **II. Clean Architecture** | ✅ PASS | Design maintains clean separation: TagInput (reusable UI), custom property components (isolated concerns), validation utilities (lib/validators), type definitions (services/types). No cross-cutting concerns. |
| **III. Test-Driven Development** | ✅ PASS | Design includes comprehensive test strategy: unit tests for validators, component tests for TagInput/property components, integration tests for entity creation flow, accessibility tests with jest-axe for all UI. AAA pattern followed. |
| **IV. Framework Standards** | ✅ PASS | Design uses Fluent UI v9 primitives (Tag, Input, Field, Combobox), React 19 functional components with hooks, TypeScript 5.x strict typing, Vitest + Testing Library. No deviations. |
| **V. Developer Experience** | ✅ PASS | Design provides clear component structure, reusable TagInput for future use, straightforward type system extension. Quickstart guide ensures smooth onboarding. |
| **VI. Security & Privacy** | ✅ PASS | Design stores custom properties as JSON in existing Properties field with existing RBAC/encryption. No hardcoded secrets, no new security surface. Validation is frontend-only (non-security). |
| **VII. Semantic Versioning** | ✅ PASS | Design avoids breaking changes: extends WorldEntityType enum (backward compatible), adds optional Properties field (already exists), new components don't affect existing code. MINOR version bump appropriate. |

**Post-Design Assessment**: ✅ **FULLY COMPLIANT** - Design maintains all constitutional principles with no violations or justifications required. Implementation plan aligns with project standards.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
libris-maleficarum-app/
├── src/
│   ├── components/
│   │   ├── MainPanel/
│   │   │   ├── EntityDetailForm.tsx          # Modified: Add custom property fields
│   │   │   ├── EntityDetailForm.module.css   # Modified: Styles for custom properties
│   │   │   └── EntityDetailForm.test.tsx     # Modified: Tests for custom properties
│   │   ├── SidePanel/
│   │   │   ├── WorldSidebar.tsx              # Modified: Container icons
│   │   │   ├── WorldSidebar.module.css       # Modified: Icon styles
│   │   │   └── WorldSidebar.test.tsx         # Modified: Icon display tests
│   │   ├── EntityTypeSelector/               # New or Modified: Type recommendations
│   │   │   ├── EntityTypeSelector.tsx
│   │   │   ├── EntityTypeSelector.module.css
│   │   │   └── EntityTypeSelector.test.tsx
│   │   └── shared/
│   │       ├── TagInput/                     # New: Reusable tag input component
│   │       │   ├── TagInput.tsx
│   │       │   ├── TagInput.module.css
│   │       │   └── TagInput.test.tsx
│   ├── services/
│   │   └── types/
│   │       ├── worldEntity.types.ts          # Modified: Add Container and regional types
│   │       └── worldEntity.types.test.ts     # Modified: Type metadata tests
│   ├── store/
│   │   └── store.ts                          # Possibly modified: If state changes needed
│   └── lib/
│       └── validators/
│           ├── numericValidation.ts          # New: Validators for Population/Area
│           └── numericValidation.test.ts     # New: Validation tests
└── tests/
    └── integration/
        └── entityCreation.test.tsx           # New: End-to-end entity creation flow

docs/design/
└── DATA_MODEL.md                             # Already updated: Container/Standard types

specs/004-entity-type-support/
├── plan.md                                   # This file
├── research.md                               # Phase 0 output (to be created)
├── data-model.md                             # Phase 1 output (to be created)
├── quickstart.md                             # Phase 1 output (to be created)
└── contracts/                                # Phase 1 output (to be created)
```

**Structure Decision**: Web application structure (frontend-only changes). React component modifications in `libris-maleficarum-app/src/components/`, type system updates in `services/types/`, new reusable TagInput in `components/shared/`. No backend changes required since feature uses existing API endpoints with Properties field for custom data.

## Complexity Tracking

> **No constitutional violations identified. This section intentionally left blank.**

## Cleanup Tasks

**Post-Feature Completion**:

- [ ] Remove "Active Feature" section from `.github/copilot-instructions.md` (lines 5-26) after feature is merged to main branch
- [ ] Verify branch `004-entity-type-support` is deleted after merge
- [ ] Archive or remove temporary feature context to keep copilot-instructions.md focused on active work
