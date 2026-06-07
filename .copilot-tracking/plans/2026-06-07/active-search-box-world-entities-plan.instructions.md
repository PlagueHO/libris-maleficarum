---
applyTo: '.copilot-tracking/changes/2026-06-07/active-search-box-world-entities-changes.md'
---
<!-- markdownlint-disable-file -->
# Implementation Plan: Active Search Box for World Entities

## Overview

Add a world-scoped, Azure-Portal-style active search box to the frontend TopToolbar that queries the existing backend Search API over World Entities, renders a floating non-modal results panel (live results plus pinned/recent/history), and opens selected entities in the MainPanel with hierarchy expansion.

## Objectives

### User Requirements

* Add a ShadcnUI active search box at the top of the frontend app — Source: research "Task Implementation Requests" bullet 1.
* Search World Entities in `worldentity-index` via the backend Search API (semantic/keyword/filters) — Source: research bullets 2-3.
* Floating results panel (opens on focus) showing results, search history, recent entities, and up to 5 pinned entities — Source: research bullet 4.
* Result click opens the entity in the main panel with hierarchy expansion; hover reveals an edit action — Source: research bullet 5.
* Apply backend-overload protections (debounce/throttle/cancel/cache/min-length) — Source: research bullet 6.
* Evaluate persisting user settings (pinned/recent) localStorage vs backend vs hybrid — Source: research bullet 7.

### Derived Objectives

* Extend the backend search projection with `path` (and `depth`) to enable hierarchy reveal without a second round-trip — Derived from: research "Key Discoveries" `path`-projection blocker (both fields already indexed; trivial change).
* Close the ownership-check gap on the canonical `GET /api/v1/worlds/{worldId}/search` route and remove the duplicate `/entities/search` action — Derived from: research "Follow-Up Items — Resolved" canonical-endpoint auth gap.
* Ship per-world `localStorage` persistence for v1 (history/recent/pinned), deferring backend-synced settings — Derived from: research scope (out, v1) + hybrid persistence recommendation.

## Context Summary

### Project Files

* libris-maleficarum-app/src/components/TopToolbar/TopToolbar.tsx - Insertion point for the search box (before the `ml-auto` action cluster).
* libris-maleficarum-app/src/services/api.ts - RTK Query base slice (`injectEndpoints` target; axios cancellation + auth already solved).
* libris-maleficarum-app/src/store/worldSidebarSlice.ts - `selectSelectedWorldId`, `setSelectedWorld`, `setSelectedEntity`, `setExpandedNodes`, `openEntityFormEdit` drive open + hierarchy reveal.
* libris-maleficarum-app/src/lib/entityIcons.ts + src/services/config/entityTypeRegistry.ts - `getEntityIcon` / `ENTITY_TYPE_META` for result icons/labels.
* libris-maleficarum-app/src/hooks/useTheme.ts - localStorage persistence convention precedent.
* libris-maleficarum-service/src/Infrastructure/Services/AzureAISearchService.cs - Search projection (`Select` + `SearchResult` mapping) to extend with `path`.
* libris-maleficarum-service/src/Api/Controllers/WorldsController.cs - Canonical search action needing the ownership check.
* libris-maleficarum-service/src/Api/Controllers/WorldEntitiesController.cs - Duplicate `/entities/search` action to remove (ownership block to port).

### References

* .copilot-tracking/research/2026-06-07/active-search-box-world-entities-research.md - Full research: API contract, integration mechanisms, cmdk/Popover UX, persistence analysis, resolved follow-ups, runnable snippets.
* docs/design/api.md - Canonical search route documentation.
* docs/design/data_model.md - Authoritative persistence shapes (`path`/`depth`/`hasChildren`, camelCase).

### Standards References

* .github/instructions/accessibility.instructions.md - WCAG 2.2 AA combobox pattern, aria-live, contrast.
* .github/copilot-instructions.md - Shadcn/UI + Tailwind + RTK Query + jest-axe conventions.
* AGENTS.md - Shadcn (not Fluent), Tailwind only, data_model.md authoritative, System.Text.Json, test patterns.

## Implementation Checklist

### [x] Implementation Phase 1: Backend Search API Hardening

<!-- parallelizable: true -->

Note: Phase 1 is in scope. `path` and `depth` are already indexed and synced — this is a projection-only fix (4 change sites, no query cost).

* [x] Step 1.1: Add `path` (and `depth`) to the search result projection
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 18-42)
* [x] Step 1.2: Close the ownership gap on the canonical search route and remove the duplicate
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 43-65)
* [x] Step 1.3: Update backend tests for the new projection and ownership behavior
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 66-80)
* [x] Step 1.4: Validate phase changes
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 81-86)

### [x] Implementation Phase 2: Frontend Search Data Layer

<!-- parallelizable: true -->

* [x] Step 2.1: Define search types
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 93-107)
* [x] Step 2.2: Inject the search endpoint
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 108-122)
* [x] Step 2.3: Add the debounce hook
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 123-136)
* [x] Step 2.4: Add the search facade hook
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 137-151)
* [x] Step 2.5: Add the MSW search handler
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 152-165)
* [x] Step 2.6: Validate phase changes
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 166-171)

### [x] Implementation Phase 3: Per-World localStorage Persistence

<!-- parallelizable: true -->

* [x] Step 3.1: Add the search-history persistence hook
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 178-192)
* [x] Step 3.2: Test the persistence hook
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 193-206)
* [x] Step 3.3: Validate phase changes
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 207-211)

### [x] Implementation Phase 4: GlobalSearch UI Component

<!-- parallelizable: false -->

* [x] Step 4.1: Install the shadcn command (cmdk) component
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 218-232)
* [x] Step 4.2: Add XSS-safe highlight component
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 233-246)
* [x] Step 4.3: Add the result row
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 247-261)
* [x] Step 4.4: Add the empty-state panel
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 262-275)
* [x] Step 4.5: Add the GlobalSearch shell
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 276-290)
* [x] Step 4.6: Add the barrel export
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 291-301)
* [x] Step 4.7: Add the entity-type filter control
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 302-319)

### [x] Implementation Phase 5: TopToolbar Integration and Tests

<!-- parallelizable: false -->

* [x] Step 5.1: Insert GlobalSearch into TopToolbar
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 327-340)
* [x] Step 5.2: Wire open + expand-on-select dispatches
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 341-355)
* [x] Step 5.3: Component and accessibility tests
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 356-370)

### [x] Implementation Phase 6: Validation

<!-- parallelizable: false -->

* [x] Step 6.1: Run full project validation
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 375-384)
* [x] Step 6.2: Fix minor validation issues
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 385-388)
* [x] Step 6.3: Report blocking issues
  * Details: .copilot-tracking/details/2026-06-07/active-search-box-world-entities-details.md (Lines 389-393)

## Planning Log

See .copilot-tracking/plans/logs/2026-06-07/active-search-box-world-entities-log.md for discrepancy tracking, implementation paths considered, and suggested follow-on work.

## Dependencies

* Node.js 20.x + pnpm (frontend); existing RTK Query + axios stack.
* shadcn CLI (`pnpm dlx shadcn@latest add command`) → adds `cmdk`.
* .NET 10 SDK (only if Phase 1 backend changes are made).
* Live Aspire-proxied `/api` path in dev; MSW for tests.

## Success Criteria

* World-scoped active search box in TopToolbar consuming `GET /api/v1/worlds/{worldId}/search` with debounce/min-length/cancellation/caching — Traces to: user requirements 1, 2, 6.
* Floating non-modal panel showing live results when typing and pinned/recent/history when empty — Traces to: user requirement 4.
* Result select opens the entity in MainPanel (view) and expands hierarchy ancestors; hover reveals edit; pinning capped at 5 — Traces to: user requirements 4, 5.
* Per-world `localStorage` persistence for history/recent/pinned — Traces to: user requirement 7 (v1 scope).
* WCAG 2.2 AA combobox a11y verified via jest-axe — Traces to: accessibility.instructions.md.
* Backend `path` projection delivered (or fallback documented) and canonical search route enforces ownership — Traces to: derived objectives 1, 2.
