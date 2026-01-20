# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

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
