<!-- markdownlint-disable-file -->
# Release Changes: Active Search Box for World Entities

**Related Plan**: active-search-box-world-entities-plan.instructions.md
**Implementation Date**: 2026-06-07

## Summary

Completed the frontend world-scoped search workstream for the active search box: wired the canonical search endpoint into the TopToolbar search surface, added a debounced query path with entity-type filtering and per-world localStorage history/pinned/recent behavior, and verified the search contract with focused tests and a production build.

## Changes

### Added

* libris-maleficarum-app/src/components/GlobalSearch/GlobalSearch.tsx - Implemented the floating world-scoped search surface, including entity-type filtering, live results, and empty-state / history surfaces.
* libris-maleficarum-app/src/hooks/useSearchHistory.ts - Added per-world localStorage-backed search history, recent entities, and pinned entities with caps and dedupe rules.
* libris-maleficarum-app/src/hooks/useSearchHistory.test.ts - Added tests for per-world history isolation, recent-item tracking, and capped dedupe behavior.
* libris-maleficarum-app/src/__tests__/services/searchApi.test.tsx - Added a focused contract test for the canonical world-scoped search endpoint.

### Modified

* libris-maleficarum-app/src/services/searchApi.ts - Wired the frontend to the canonical world-scoped search API path.
* libris-maleficarum-app/src/services/types/search.types.ts - Extended the search response shape to support path/depth metadata used by the UI.
* libris-maleficarum-app/src/hooks/useEntitySearch.ts - Added the debounced facade over the canonical search query.
* libris-maleficarum-app/src/__tests__/mocks/handlers.ts - Updated the MSW handler to return the path/depth payload used by the search UI.
* libris-maleficarum-service/tests/unit/Api.Tests/Controllers/WorldsControllerTests.cs - Aligned the canonical search contract tests to the real ownership gate and response projection path.

### Removed

* None

## Additional or Deviating Changes

* The search persistence path intentionally stays localStorage-first for v1 and defers backend-synced user settings to the follow-up work item noted in the planning log.
* The entity-type filter is now exposed in the search UI, but the broader backend-synced settings model remains out of scope for this implementation pass.

## Release Summary

The active-search implementation is now wired end to end in the frontend: the canonical world-scoped search route is consumed by the TopToolbar search surface, entity-type filtering is available in the search popover, and per-world localStorage persistence for history/recent/pinned data is in place. Validation included focused search-hook tests and a successful production build, with the backend ownership/contract tests aligned to the canonical route.
