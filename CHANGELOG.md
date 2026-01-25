# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Edit World Entity**: Complete editing workflow with three user stories:
  - **US1 - Edit from Hierarchy**: Click edit icon in EntityTreeNode to quickly modify entity properties
  - **US2 - Edit from Detail View**: Edit button in EntityDetailReadOnlyView transitions to WorldEntityForm in edit mode  
  - **US3 - Client-Side Validation**: Real-time validation with inline error display, `aria-invalid`, and `aria-describedby` for assistive technologies
  - **Data Safety**: UnsavedChangesDialog prevents accidental data loss when navigating away with unsaved changes
  - **Unified Form Component**: WorldEntityForm handles both create and edit workflows with mode detection
  - **Performance**: React.memo optimization on EntityTreeNode prevents unnecessary re-renders
  - **Accessibility**: All components jest-axe tested with 0 violations, full keyboard navigation, WCAG 2.2 Level AA compliant
  - **Validation Module**: Reusable `worldEntityValidator.ts` module for client-side schema validation
  - **Technical**:44/52 tasks complete (85%), 526 tests passing, clean build & lint, integration tests verify end-to-end workflows
- **Schema Versioning**: Added `SchemaVersion` field to WorldEntity for lazy migration on save
  - Entity-level schema versioning with 1-based integers (default: 1)
  - Backend validation with 4 error codes (INVALID, TOO_LOW, TOO_HIGH, DOWNGRADE_NOT_ALLOWED)
  - Frontend auto-injection of current schema version during entity creation/update
  - EF Core value converter for backward compatibility (0 → 1 conversion for pre-versioning documents)
  - Lazy migration strategy: entities upgrade to current version when saved, enabling gradual world migration
  - Comprehensive test coverage: 20 backend unit tests + component integration tests
  - Documentation: DATA_MODEL.md includes schema evolution guidelines, API.md includes validation error examples
- **Container Entity Types**: Added 10 new container entity types (Locations, People, Events, History, Lore, Bestiary, Items, Adventures, Geographies, Campaigns) to organize world content into logical categories.
- **Regional Entity Types**: Added 4 new regional entity types (GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion) with custom properties for domain-specific data.
- **Custom Properties**: Regional entities support custom properties stored in JSON field:
  - GeographicRegion: Climate, Terrain, Population (integer), Area (decimal)
  - PoliticalRegion: GovernmentType, MemberStates (tags), EstablishedDate
  - CulturalRegion: Languages (tags), Religions (tags), CulturalTraits
  - MilitaryRegion: CommandStructure, StrategicImportance, MilitaryAssets (tags)
- **TagInput Component**: Reusable component for entering lists of text values with keyboard support, duplicate prevention, and accessibility.
- **Numeric Validation**: Utilities for parsing and validating Population (integer) and Area (decimal) fields with thousand separators.
- **Smart Type Recommendations**: Context-aware entity type suggestions based on parent entity (e.g., Continent suggests GeographicRegion, Country).
- **Flexible Entity Placement**: Users can create any entity type under any parent without system restrictions.
- **Icon Support**: Visual icons for all 29 entity types using Fluent UI icons.
- **Accessibility**: All new components tested with jest-axe, WCAG 2.2 Level AA compliant, full keyboard navigation.
- **Validation Utilities**: Shared validation logic for text fields and array fields in custom property components.

### Changed

- Extended `WorldEntityType` enum from 15 to 29 types.
- Updated `ENTITY_TYPE_META` with labels, descriptions, categories, and icons for all entity types.
- Updated `ENTITY_TYPE_SUGGESTIONS` mapping for intelligent type recommendations.
- EntityTypeSelector now displays recommended types prominently while maintaining access to all types.

### Technical

- 82 tasks completed across 7 phases (Setup, Foundational, 4 User Stories, Polish).
- 27 accessibility tests passing with zero violations.
- Test coverage >80% for all new code.
- Performance: EntityTypeSelector search <100ms with 29 entity types.
