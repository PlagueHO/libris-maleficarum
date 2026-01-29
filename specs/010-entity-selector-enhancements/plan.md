# Implementation Plan: Entity Type Selector Enhancements

**Branch**: `010-entity-selector-enhancements` | **Date**: 2026-01-29 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/010-entity-selector-enhancements/spec.md`

## Summary

Enhance the EntityTypeSelector component to improve usability through visual icons (16×16px), compact spacing (8px padding), refined filter UI, and simplified grouping. Component will display entity types with icons to the left, reduce vertical height to show 8-10 items simultaneously, update filter placeholder to "Filter...", and reorganize types into "Recommended" (with star icon)" and "Other" (alphabetically sorted) sections separated by a thin horizontal line. When no recommendations exist, display types in a flat list without sections. Technical approach uses existing Shadcn/UI Popover + Radix primitives with Lucide React icons, TailwindCSS spacing utilities (py-2 for 8px padding), and enhanced ARIA attributes for accessibility compliance (WCAG 2.2 AA).

## Technical Context

**Language/Version**: TypeScript 5.9.3, React 19.2.0  
**Primary Dependencies**: Shadcn/UI components, Radix UI primitives, Lucide React 0.563.0, TailwindCSS 4.1.18, Redux Toolkit 2.11.2  
**Storage**: N/A (component renders client-side state from entity type registry)  
**Testing**: Vitest 4.0.18, Testing Library (React 16.3.2), jest-axe 10.0.0
**Target Platform**: Modern browsers (Chrome, Firefox, Safari, Edge latest 2 versions), desktop viewport 1920x1080+
**Project Type**: Web application (frontend focus)  
**Performance Goals**: Filter input response <100ms, icon loading <50ms, dropdown render <200ms  
**Constraints**: WCAG 2.2 Level AA compliance, 3:1 icon contrast ratio, 8-10 visible items without scrolling, keyboard navigability preserved  
**Scale/Scope**: ~20-30 total entity types, single selector component used 5-10 times across UI

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Cloud-Native Architecture** | ✅ PASS | Component is frontend-only, deployed with Static Web App. No cloud infrastructure changes required. |
| **II. Clean Architecture** | ✅ PASS | Component maintains separation: UI (EntityTypeSelector.tsx) → types (worldEntity.types.ts) → config (entityTypeRegistry.ts). No cross-layer violations. |
| **III. Test-Driven Development** | ✅ PASS | Will update EntityTypeSelector.test.tsx with accessibility tests (jest-axe) for new icon/spacing changes. Tests written before implementation per TDD mandate. |
| **IV. Framework & Technology Standards** | ✅ PASS | Uses React 19+, TypeScript, Shadcn/UI, Vitest. All dependencies already in project (Lucide React for icons, Radix UI for primitives). No new framework introductions. |
| **V. Developer Experience** | ✅ PASS | No backend changes required. Frontend hot reload via Vite preserved. Single component edit scope. |
| **VI. Security & Privacy by Default** | ✅ PASS | No security-sensitive changes. Icons loaded from bundled assets (Lucide React). No external resources or API calls added. |
| **VII. Semantic Versioning** | ✅ PASS | Component refactor is backward-compatible enhancement. Existing `EntityTypeSelectorProps` interface unchanged. PATCH version bump appropriate (bugfix-level UX improvement). |

**Overall Assessment**: ✅ **ALL GATES PASSED** - No constitutional violations. Feature aligns with frontend standards (functional components, hooks, Shadcn/UI, ARIA attributes). Accessibility testing required per Principle III.

## Project Structure

### Documentation (this feature)

```text
specs/010-entity-selector-enhancements/
├── spec.md              # Feature specification (complete)
├── plan.md              # This file (implementation plan)
├── research.md          # Phase 0: Icon research and spacing decisions
├── data-model.md        # Phase 1: Entity type metadata enhancements
├── quickstart.md        # Phase 1: Local testing guide
├── contracts/           # Phase 1: Component API contracts (TypeScript interfaces)
├── checklists/          # Validation checklists
│   └── requirements.md  # Specification quality checklist (complete)
└── tasks.md             # Phase 2: Task breakdown (created by /speckit.tasks)
```

### Source Code (repository root)

```text
libris-maleficarum-app/                    # Frontend React application
├── src/
│   ├── components/
│   │   ├── shared/
│   │   │   └── EntityTypeSelector/        # Target component for enhancement
│   │   │       ├── EntityTypeSelector.tsx  # Main component file (MODIFY)
│   │   │       ├── EntityTypeSelector.test.tsx  # Test file (MODIFY)
│   │   │       └── index.ts               # Barrel export
│   │   └── ui/                            # Shadcn/UI primitives (READ-ONLY)
│   │       ├── popover.tsx                # Reused for dropdown
│   │       ├── input.tsx                  # Reused for filter
│   │       ├── button.tsx                 # Reused for trigger
│   │       └── separator.tsx              # Reused for section divider
│   ├── services/
│   │   ├── types/
│   │   │   └── worldEntity.types.ts       # Entity type definitions (READ - icon usage)
│   │   └── config/
│   │       └── entityTypeRegistry.ts      # Entity type registry (READ - icon mappings)
│   └── lib/
│       └── utils.ts                       # cn() utility for className merging
└── tests/                                 # Test outputs (Vitest)
```

**Structure Decision**: Web application (frontend-only). All changes scoped to single component: `EntityTypeSelector.tsx` and its test file. No backend or API changes required. Entity type metadata (icons) already exists in registry. Component consumes data via existing props interface—no breaking changes to API.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |

N/A - No constitutional violations identified. All gates passed.

---

## Phase 0: Research & Discovery

✅ **COMPLETE** - See [research.md](research.md)

**Key Findings**:

- Icon mappings already exist in entity type registry (Lucide React icon names)
- Spacing calculations confirm 8px padding achieves 9 visible items (~50px/item in 450px dropdown)
- Accessibility validated: icons as aria-hidden, keyboard nav preserved, WCAG 2.2 AA compliant
- Separator implementation uses Shadcn/UI Separator component

---

## Phase 1: Design & Contracts

✅ **COMPLETE** - See documentation files below

**Deliverables**:

1. [data-model.md](data-model.md) - Entity type metadata schema (no DB changes needed)
1. [contracts/EntityTypeSelectorAPI.md](contracts/EntityTypeSelectorAPI.md) - Component props interface (unchanged, backward compatible)
1. [quickstart.md](quickstart.md) - Local testing and validation guide

**Key Decisions**:

- No API contract changes (props interface preserved)
- No data model changes (using existing registry)
- Component enhancements are purely presentational

---

## Phase 2: Implementation Tasks

**Status**: Ready for task breakdown via `/speckit.tasks`  

**High-Level Categories** (to be detailed in tasks.md):

1. **P1: Icon Integration**  
   - Add dynamic icon rendering from entity type metadata
   - Size icons to 16×16px (w-4 h-4)
   - Ensure aria-hidden="true" on all icons

1. **P2: Compact Spacing**  
   - Update padding: py-2.5 → py-2 (8px)
   - Verify 8-10 items visible without scrolling
   - Maintain text readability

1. **P3: Filter UI Update**  
   - Change placeholder: "Search entity types..." → "Filter..."
   - Preserve existing filter logic

1. **P4: Simplified Grouping**  
   - Remove category-based sections
   - Implement "Recommended" section with star icon
   - Add Separator component
   - Implement "Other" section (alphabetical)
   - Handle no-recommendations edge case

1. **Testing & Accessibility**  
   - Update EntityTypeSelector.test.tsx
   - Add jest-axe checks for new layout
   - Verify keyboard navigation
   - Confirm empty state message format

---

## Post-Phase 1 Constitution Re-Check

**Status**: ✅ **ALL GATES STILL PASSING**

| Principle | Re-Check Result |
|-----------|-----------------|
| I. Cloud-Native | ✅ No infrastructure changes |
| II. Clean Architecture | ✅ Separation maintained (UI → types → config) |
| III. Test-Driven Development | ✅ Tests included in implementation plan |
| IV. Framework Standards | ✅ React 19, TypeScript, Shadcn/UI, Vitest |
| V. Developer Experience | ✅ Single component scope, Vite hot reload |
| VI. Security & Privacy | ✅ No external resources, bundled icons only |
| VII. Semantic Versioning | ✅ Backward compatible (PATCH bump) |

**Conclusion**: Feature ready for implementation. No violations introduced during planning phase.

---

## Next Command

```bash
/speckit.tasks
```

**Purpose**: Generate detailed task breakdown in `tasks.md` with acceptance criteria, test cases, and implementation steps organized by priority (P1-P4).

---

## Implementation Plan Summary

**Feature**: Entity Type Selector Enhancements  
**Scope**: Single React component (`EntityTypeSelector.tsx`)  
**Changes**: Icons, compact spacing, filter UI, simplified grouping  
**Impact**: Backward compatible, no API changes, no DB schema changes  
**Dependencies**: Existing (Lucide React, Shadcn/UI, Radix UI)  
**Testing**: Unit tests, accessibility tests (jest-axe), manual validation  
**Documentation**: ✅ Complete (research, data model, contracts, quickstart)  
**Constitution**: ✅ Compliant (all gates passed)  

**Status**: **READY FOR `/speckit.tasks`**
