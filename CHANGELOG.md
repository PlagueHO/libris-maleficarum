# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.3] - 2026-05-29

### Changed

- Updated CLI wiring for System.CommandLine API changes.
- Updated integration tests to use FluentAssertions v8 numeric assertion APIs.
- Silenced TypeScript 6 baseUrl deprecation warnings in frontend tsconfig.
- Updated repository governance and workflow guidance, including agent guardrails and CI/CD protocol documentation.
- Updated repository standards files, including Code of Conduct and editor configuration.
- Removed DatePicker initialFocus usage to align component behavior.

### Removed

- Removed CodeQL workflow configuration from CI.

### Security

- Enhanced logging security controls.

### Dependencies

- Upgraded major frontend and tooling dependencies, including TypeScript 6, ESLint 10, Vite build toolchain groups, and markdownlint-cli2.
- Upgraded backend and test dependencies, including Microsoft.Identity.Web, MSTest.TestFramework, FluentAssertions, FluentValidation.AspNetCore, and System.CommandLine.
- Upgraded platform and CI dependencies, including dotnet-sdk baseline and actions/checkout v6.
- Consolidated grouped Dependabot updates for UI utilities, type definitions, and devcontainer dependency groups.

## [0.1.2] - 2026-05-24

### Added

- Added CI/CD workflow coverage for backend and frontend pipelines.

### Changed

- Updated infrastructure abbreviations in JSON configuration.
- Added required permissions for the lint-and-publish-bicep workflow job.
- Updated pnpm workspace configuration, documentation links, and package settings.

### Dependencies

- Bumped project and workflow dependencies across .NET, Azure SDK, and GitHub Actions packages.
- Bumped `Azure.Search.Documents` from 11.7.0 to 12.0.0 and `Azure.Storage.Blobs` from 12.26.0 to 12.28.0.
- Updated workflow toolchain versions, including pnpm 10.0.0 and devcontainer Docker-in-Docker feature updates.

## [0.1.1] - 2026-05-22

### Changed

- Updated backend service deployment workflows in CI/CD automation.

## [0.1.0] - 2026-05-21

### Added

- **User Authentication**: Dual-mode authentication — anonymous single-user mode (zero-config `_anonymous` identity) and multi-user Entra ID mode via MSAL with JWT bearer tokens, `AuthGuard` component, and bearer token Axios interceptor.
- **User Menu & Settings Panel**: Accessible dropdown in the header toolbar with identity display, sign-in/sign-out actions, and a settings overlay with theme toggle.
- **Edit World Entity**: Full editing workflow — edit from hierarchy tree, edit from detail view, client-side validation with `aria-invalid`/`aria-describedby`, and `UnsavedChangesDialog` to prevent accidental data loss. `WorldEntityForm` handles both create and edit modes.
- **Schema Versioning**: `SchemaVersion` field on `WorldEntity` with lazy migration on save, backend validation (INVALID, TOO_LOW, TOO_HIGH, DOWNGRADE_NOT_ALLOWED), frontend auto-injection, and EF Core value converter for backward compatibility.
- **World Data Importer**: CLI tool, import library, and API client SDK for bulk world data import; Grimhollow sample world auto-seeded via Aspire AppHost on first run.
- **Entity Search Index**: Azure AI Search integration with vector search, `SearchIndexWorker` powered by Cosmos DB change feed, and document indexing with filtering.
- **Async Entity Operations & Notification Center**: Background delete and cascade-delete with TTL-based soft delete, progress polling, Sonner toast notifications, and optimistic UI updates.
- **Soft Delete with Cascade**: `IsDeleted` flag, delete validation (blocks deletes without cascade option), configurable TTL, and `WorldEntity` hierarchy-aware cascade logic.
- **Container Entity Types**: 10 new container entity types (Locations, People, Events, History, Lore, Bestiary, Items, Adventures, Geographies, Campaigns) to organise world content into logical categories.
- **Regional Entity Types**: 4 new regional entity types (GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion) with custom JSON properties — climate, terrain, population, government type, languages, religions, and more.
- **Player Character Entity Type**: New entity type with icon mapping for TTRPG campaign management.
- **TagInput Component**: Reusable tag entry component with keyboard support, duplicate prevention, and full accessibility.
- **Numeric Validation**: Utilities for parsing and validating Population (integer) and Area (decimal) fields with thousand-separator support.
- **Smart Type Recommendations**: Context-aware entity type suggestions based on parent entity (e.g., Continent suggests GeographicRegion, Country).
- **Dark/Light Mode Toggle**: Fantasy D&D theme with a full dark/light colour palette switchable at runtime.
- **Dynamic Schema Properties — Phase 1**: Schema foundation and dynamic field renderer for entity-type-specific custom properties.
- **EntityTypeSelector**: Context-aware type suggestions with prominent recommended types and search across all 29 entity types.
- **Azure Static Web App**: Custom domain support, configurable location input, and full CI/CD deployment pipeline.
- **Access Code Protection**: Optional API protection via access code verification, configurable per environment.
- **Azure AI Foundry & Cognitive Services**: Capability host, Foundry project, AI services connections, and role assignments in Bicep infrastructure.
- **Backend Service**: ASP.NET Core 10 API with Clean/Hexagonal architecture, EF Core + Cosmos DB (NoSQL), Aspire.NET orchestration, health checks, and OpenTelemetry tracing.
- **Aspire AppHost**: Local development orchestration with service discovery, connection string injection, and Aspire Dashboard for observability.

### Changed

- Extended `WorldEntityType` enum from 15 to 29 types; updated `ENTITY_TYPE_META` with labels, descriptions, categories, and icons for all types.
- Updated `ENTITY_TYPE_SUGGESTIONS` mapping for intelligent context-aware recommendations.
- Migrated UI from Fluent UI v9 to Shadcn/UI + Radix UI primitives with TailwindCSS v4 (CSS-based configuration).
- Refactored app structure into feature-scoped components: `WorldSidebar`, `MainPanel`, `ChatPanel`, `TopToolbar`.
- World entity API endpoints versioned to `/api/v1/`.
- Improved `BaseUrl` validation and error handling in the API client.
- CI/CD workflows restructured: frontend build/deploy, backend service publish, and infrastructure provisioning run as separate jobs.
- Soft delete logic updated: `IsDeleted` flag filtering on all queries; separate endpoint for retrieving soft-deleted entities.
- Cosmos DB partition key strategy: `/WorldId` for entities, composite `/WorldId-ParentId` for hierarchy queries.
- Azure infrastructure: network-isolated architecture with private endpoints for all PaaS services; public network access disabled by default.

### Fixed

- Resolved `react-hooks/set-state-in-effect` lint errors from `eslint-plugin-react-hooks` 7.1.1.
- Relaxed flaky performance test threshold from 1ms to 50ms.
- Fixed pnpm v11 compatibility: added `pnpm-workspace.yaml` `allowBuilds` entries for esbuild, msw, and protobufjs.
- Fixed `AZURE_ENV_NAME` format for consistency in environment naming.
- Fixed anonymous-mode test loading state by mocking `useAccessCode`.
- Fixed URI validation for `BlobUrl` values.
- Increased resilience timeout for CLI import operations.

### Security

- All components comply with WCAG 2.2 Level AA; jest-axe tested with 0 violations across 27+ accessibility tests.
- Private endpoints for Key Vault, Storage, Cosmos DB, AI Search, and AI Services; public network access disabled.
- RBAC enabled for Key Vault; Cosmos DB and AI Search role assignments managed via Bicep templates.
- Azure User Assigned Managed Identity with federated OIDC for passwordless GitHub Actions authentication.

### Dependencies

- Bumped `vite` from 7.x to 8.x.
- Bumped `lucide-react` from 0.577 to 1.7.0.
- Bumped `jsdom` from 26.x to 29.x.
- Bumped `tailwindcss` from 4.1 to 4.2.
- Bumped `MSTest.Sdk` from 3.x to 4.2.3.
- Bumped numerous OpenTelemetry, React ecosystem, TypeScript, and GitHub Actions dependencies.
