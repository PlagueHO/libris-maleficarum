# Implementation Plan: Async Entity Operations with Notification Center

**Branch**: `012-async-entity-operations` | **Date**: February 3, 2026 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/012-async-entity-operations/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Implement asynchronous WorldEntity delete operations with cascading entity removal, complemented by a notification center UI component similar to Azure Portal. The notification center features a bell icon in the top-right toolbar that displays a badge count for unread notifications and opens a sidebar panel showing active and completed async operations with real-time status updates via periodic polling.

**Technical Approach**: Frontend-only implementation using React 19+ with TypeScript, Redux Toolkit for state management, Shadcn/ui components for UI, and periodic polling (2-3 second intervals) for status updates with abstraction layer to enable future migration to push-based mechanisms (SSE/WebSocket/SignalR).

## Technical Context

**Language/Version**: TypeScript 5.x / React 19+ / Node.js 20.x  
**Primary Dependencies**: Redux Toolkit (RTK Query), Shadcn/ui + Radix UI primitives, TailwindCSS v4, Vitest + Testing Library + jest-axe  
**Storage**: Session-only (in-memory Redux state), no persistence across browser sessions  
**Testing**: Vitest with React Testing Library, jest-axe for accessibility, MSW for API mocking  
**Target Platform**: Modern browsers (Chrome/Edge/Firefox/Safari latest 2 versions), WCAG 2.2 Level AA compliance  
**Project Type**: Web application (React SPA frontend in `libris-maleficarum-app/`)  
**Performance Goals**: Real-time UI updates within 2 seconds of server state changes, support for 10+ concurrent async operations per user without degradation  
**Constraints**: Session-only notification retention (24-hour auto-cleanup), periodic polling with 2-3 second interval, partial commit semantics for cascading deletes (no rollback)  
**Scale/Scope**: Single-user TTRPG campaign management, expected load: 1-5 concurrent async operations typical, 10+ operations edge case

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### I. Cloud-Native Architecture - ✅ PASS

- Frontend-only feature; no infrastructure changes required
- No new Azure resources needed; uses existing API endpoints
- Aligns with cloud deployment model (static React app on Azure Static Web Apps)

### II. Clean Architecture & Separation of Concerns - ✅ PASS

- Clear separation: UI components (NotificationCenter, NotificationBell) → State (Redux slice) → Services (API polling abstraction)
- Dependencies flow inward: Components depend on state selectors; state depends on service layer
- Service abstraction layer enables future migration to push-based mechanisms without impacting UI

### III. Test-Driven Development (NON-NEGOTIABLE) - ✅ PASS

- All components require accessibility tests with jest-axe (FR-019, FR-020, SC-008)
- Unit tests for Redux slice (async operation state management, polling logic)
- Integration tests for notification center workflows (P1-P4 user stories)
- Component tests using React Testing Library with MSW for API mocking

### IV. Framework & Technology Standards - ✅ PASS

- React 19+ with TypeScript ✓
- Shadcn/ui components (Dialog, ScrollArea, Badge, Button) ✓
- Redux Toolkit with RTK Query for API operations ✓
- TailwindCSS for styling ✓
- Vitest + Testing Library + jest-axe for testing ✓

### V. Developer Experience & Inner Loop - ✅ PASS

- Vite hot reload preserved (standard component development workflow)
- No build pipeline changes required
- Runnable with existing `pnpm dev` command
- Component library (Shadcn/ui) already integrated

### VI. Security & Privacy by Default - ✅ PASS

- No secrets or credentials involved (frontend polling logic only)
- Session-only storage prevents data leakage across sessions
- No PII in notifications (entity names and operation types only)
- Backend API endpoints must implement proper authentication (out of scope for this feature)

### VII. Semantic Versioning & Breaking Changes - ✅ PASS

- No breaking changes to existing APIs or components
- New feature is additive (new UI components + Redux slice)
- Backward compatible with existing WorldEntity delete behavior
- Version bump: MINOR (new feature, backward compatible)

**Gate Status**: ✅ ALL GATES PASS - Proceed to Phase 0 Research

## Project Structure

### Documentation (this feature)

```text
specs/012-async-entity-operations/
├── spec.md              # Feature specification
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (below)
├── data-model.md        # Phase 1 output (below)
├── quickstart.md        # Phase 1 output (below)
├── contracts/           # Phase 1 output (below)
│   └── async-operations-api.yaml  # OpenAPI spec for backend endpoints
├── checklists/
│   └── requirements.md  # Requirements validation checklist
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
libris-maleficarum-app/
├── src/
│   ├── components/
│   │   ├── NotificationCenter/          # NEW: Notification center components
│   │   │   ├── NotificationCenter.tsx      # Sidebar panel component
│   │   │   ├── NotificationCenter.test.tsx # Accessibility + unit tests
│   │   │   ├── NotificationBell.tsx        # Bell icon button with badge
│   │   │   ├── NotificationBell.test.tsx   # Accessibility + unit tests
│   │   │   ├── NotificationItem.tsx        # Individual notification item
│   │   │   ├── NotificationItem.test.tsx   # Accessibility + unit tests
│   │   │   └── index.ts                    # Barrel export
│   │   ├── TopToolbar/
│   │   │   └── TopToolbar.tsx              # MODIFIED: Add NotificationBell
│   │   ├── MainPanel/
│   │   │   ├── DeleteConfirmationModal.tsx # MODIFIED: Initiate async delete
│   │   │   └── WorldEntityForm.tsx         # MODIFIED: Handle entity being deleted
│   │   └── ui/
│   │       └── badge.tsx                   # EXISTING: Shadcn/ui Badge component
│   │
│   ├── services/
│   │   ├── asyncOperationsApi.ts        # NEW: RTK Query endpoints for async ops (includes polling abstraction)
│   │   └── worldEntityApi.ts            # MODIFIED: Add async delete mutation
│   │
│   ├── store/
│   │   ├── store.ts                     # MODIFIED: Add notifications reducer
│   │   └── notificationsSlice.ts        # NEW: Redux slice for async operations
│   │
│   ├── lib/
│   │   └── asyncOperationHelpers.ts     # NEW: Utility functions (formatters, status mappers)
│   │
│   └── __tests__/
│       ├── integration/
│       │   ├── asyncDeleteWorkflow.test.tsx       # NEW: P1-P4 user story tests
│       │   └── notificationCenterFlow.test.tsx    # NEW: Notification center integration tests
│       └── services/
│           ├── asyncOperationsApi.test.ts         # NEW: API layer tests
│           └── asyncOperationsService.test.ts     # NEW: Polling service tests
│
└── tests/
    └── visual/
        └── notificationCenter.spec.ts   # NEW: Playwright visual tests

```

**Structure Decision**: Web application with React frontend. New components follow existing patterns:

- Components in PascalCase folders under `src/components/` (shared: NotificationCenter/)
- Shadcn/ui components in kebab-case under `src/components/ui/` (using existing badge.tsx)
- Services use RTK Query pattern (injectEndpoints into base `api` slice)
- Redux state management via new `notificationsSlice.ts`
- Co-located tests with `.test.tsx`/`.test.ts` suffix
- Integration tests in `src/__tests__/integration/`
- Visual regression tests in `tests/visual/`

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations detected. All constitutional principles are satisfied by this feature implementation.
