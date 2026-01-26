# Feature Specification: Schema-Driven Entity Properties (Phase 1)

**Feature Branch**: `009-schema-driven-properties`  
**Created**: 2026-01-26  
**Status**: Draft  
**Input**: User description: "Phase 1 of World Entity type support for schema-driven properties in code registry"

## Overview

This feature introduces schema-driven custom properties for WorldEntity types. Instead of maintaining separate hand-coded property components for each entity type (e.g., `GeographicRegionProperties.tsx`, `CulturalRegionProperties.tsx`), property definitions will be stored as schema metadata in the entity type registry. A dynamic property renderer will interpret these schemas to generate both edit and read-only views.

This is Phase 1 of a multi-phase initiative toward fully user-configurable entity types:

- **Phase 1** (this spec): Schema-driven properties in code registry
- Phase 2 (future): Registry becomes a service; schema fetched from backend
- Phase 3 (future): Admin UI for creating/editing entity type schemas
- Phase 4 (future): Full user-configurable entity types

## Clarifications

### Session 2026-01-26

- Q: When an entity has properties data but the schema definition was removed from the registry, what should the fallback renderer display? → A: Display all properties as key-value pairs using generic Object.entries() renderer (like current EntityDetailReadOnlyView)
- Q: When stored property data has a type mismatch with the schema (e.g., schema expects integer but data contains "123" as a string), how should the system handle it? → A: Attempt automatic type coercion on load; show validation error only if coercion fails (e.g., "123" → 123 works, "abc" → shows error)
- Q: When a user attempts to add a duplicate tag to a tagArray field, what should happen? → A: Prevent duplicate on input with visual feedback (highlight existing tag for 500ms using Tailwind animation, or show toast message)
- Q: How granular should validation rules be in the schema? → A: Support common rules: required (boolean), min/max (for numbers), pattern (regex for text fields)
- Q: Where should the PropertyFieldSchema TypeScript interface be defined? → A: In entityTypeRegistry.ts alongside EntityTypeConfig (co-located with usage)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Edit Entity with Schema-Driven Properties (Priority: P1)

A world builder opens an existing GeographicRegion entity for editing. The form displays the standard fields (Name, Description) plus custom property fields (Climate, Terrain, Population, Area) that are dynamically rendered based on the entity type's property schema. The user modifies the Climate field and saves. The changes persist correctly.

**Why this priority**: Core editing functionality must work correctly for the feature to have any value. This validates the complete edit flow with dynamic property rendering.

**Independent Test**: Can be tested by editing any entity type with custom properties and verifying the form renders correctly and saves data.

**Acceptance Scenarios**:

1. **Given** a GeographicRegion entity exists with Climate="Temperate", **When** the user opens the edit form, **Then** the Climate field displays "Temperate" and all other geographic property fields (Terrain, Population, Area) are visible with appropriate input controls.
1. **Given** the user is editing a GeographicRegion entity, **When** they change the Climate value and save, **Then** the updated value persists and is displayed when the entity is viewed again.
1. **Given** a CulturalRegion entity with Languages=["Common", "Elvish"], **When** the user opens the edit form, **Then** the Languages field displays as a tag input with both tags visible and editable.

---

### User Story 2 - View Entity with Schema-Driven Properties (Priority: P1)

A world builder selects a PoliticalRegion entity in the sidebar. The read-only detail view displays the entity name, type, description, and custom properties (GovernmentType, MemberStates, EstablishedDate) in a formatted, readable layout based on the property schema.

**Why this priority**: Read-only viewing is equally important as editing. Users spend significant time browsing entities without editing them.

**Independent Test**: Can be tested by viewing any entity type with custom properties and verifying all properties display correctly with appropriate formatting.

**Acceptance Scenarios**:

1. **Given** a PoliticalRegion entity with GovernmentType="Monarchy" and MemberStates=["Kingdom A", "Kingdom B"], **When** the user views the entity, **Then** GovernmentType displays as text and MemberStates displays as a list of badges/tags.
1. **Given** a GeographicRegion with Population=1500000, **When** the user views the entity, **Then** Population displays formatted with thousand separators (e.g., "1,500,000").
1. **Given** an entity type with no custom properties defined in its schema, **When** the user views the entity, **Then** no custom properties section appears.

---

### User Story 3 - Create Entity with Schema-Driven Properties (Priority: P2)

A world builder creates a new MilitaryRegion entity under a Country. After selecting the entity type, the form dynamically displays the custom property fields (CommandStructure, StrategicImportance, MilitaryAssets) as empty/default values. The user fills in the properties and creates the entity.

**Why this priority**: Create flow is essential but can reuse most edit logic. Slightly lower priority than edit/view which covers existing data.

**Independent Test**: Can be tested by creating a new entity of any type with custom properties and verifying fields appear and data saves.

**Acceptance Scenarios**:

1. **Given** the user is creating a new MilitaryRegion, **When** they select the MilitaryRegion entity type, **Then** the form displays empty fields for CommandStructure (textarea), StrategicImportance (textarea), and MilitaryAssets (tag input).
1. **Given** the user has filled in MilitaryAssets=["Fort Alpha", "Naval Base"], **When** they save the entity, **Then** the entity is created with the correct property values.

---

### User Story 4 - Consistent Validation Across Property Types (Priority: P2)

A world builder enters invalid data in a numeric property field (e.g., "abc" in the Population field). The form displays an inline validation error and prevents submission until the value is corrected.

**Why this priority**: Validation ensures data quality and provides a good user experience, but the core rendering must work first.

**Independent Test**: Can be tested by entering invalid values in each field type and verifying validation errors appear.

**Acceptance Scenarios**:

1. **Given** the user is editing a GeographicRegion, **When** they enter "not-a-number" in the Population field, **Then** an inline error message appears and the form cannot be submitted.
1. **Given** a textarea field with maxLength=200, **When** the user types beyond 200 characters, **Then** input is prevented or truncated, and a character counter shows the limit.
1. **Given** the user corrects an invalid field value, **When** they modify the input to be valid, **Then** the error message disappears immediately.

---

### User Story 5 - Entity Types Without Custom Properties (Priority: P3)

A world builder edits a Character entity (which has no custom property schema defined). The form displays only the standard fields (Name, Type, Description) without any custom properties section.

**Why this priority**: Backward compatibility for entity types without schemas; lower priority since most value is in schema-driven types.

**Independent Test**: Can be tested by editing entity types that don't have propertySchema defined.

**Acceptance Scenarios**:

1. **Given** a Character entity (no propertySchema), **When** the user opens the edit form, **Then** no custom properties section appears below the Description field.
1. **Given** a Character entity, **When** the user views it in read-only mode, **Then** no custom properties section appears.

---

### Edge Cases

- What happens when an entity has properties data but the schema field was removed? Display all properties as key-value pairs using the generic Object.entries() renderer (same as current EntityDetailReadOnlyView behavior).
- What happens when schema defines a field but the stored data is missing that property? Display the field as empty/default.
- What happens when stored property data has a type mismatch with schema (e.g., string instead of number)? Attempt automatic type coercion on load (e.g., "123" string → 123 number). Show validation error only if coercion fails (e.g., "abc" cannot coerce to number).
- What happens when a tag array field has duplicate entries? In edit mode, prevent duplicates on input with visual feedback (briefly highlight existing tag for 500ms using Tailwind animation, or show toast message). In read-only mode, display existing duplicates without error (data preserved as-is).
- What happens when the entity type changes during editing (currently blocked)? Continue to block entity type changes on existing entities.

## Requirements *(mandatory)*

### Functional Requirements

#### Property Schema Definition

- **FR-001**: Each entity type configuration MUST support an optional `propertySchema` field containing an ordered array of property field definitions.
- **FR-002**: Each property field definition MUST include: key (unique identifier), label (display text), and type (field type).
- **FR-003**: Supported field types MUST include: `text` (single-line input), `textarea` (multi-line input), `integer` (whole numbers), `decimal` (floating-point numbers), `tagArray` (list of string tags).
- **FR-004**: Property field definitions MUST support optional attributes: placeholder, description (help text), maxLength. An optional validation sub-object MAY define rules including: required (boolean), min/max (for numeric fields), and pattern (regex for text fields). Fields without validation rules use only type-based validation.
- **FR-005**: The property schema MUST be the single source of truth for rendering property fields in both edit and read-only modes.

#### Dynamic Property Rendering

- **FR-006**: The system MUST dynamically render property input fields based on the entity type's propertySchema.
- **FR-007**: The system MUST render property fields in the order defined in the schema.
- **FR-008**: Entity types without a propertySchema MUST NOT display any custom properties section.
- **FR-009**: Read-only mode MUST format property values appropriately (e.g., numbers with thousand separators, arrays as tag badges).
- **FR-010**: Edit mode MUST provide appropriate input controls for each field type.

#### Validation

- **FR-011**: The system MUST validate property values according to field type (e.g., integer fields reject non-numeric input).
- **FR-012**: The system MUST validate maxLength constraints and display character counters where applicable.
- **FR-013**: Validation errors MUST display inline below the relevant field.
- **FR-014**: The system MUST prevent form submission when validation errors exist.

#### Data Persistence

- **FR-015**: Property values MUST serialize to JSON for storage in the entity's properties field.
- **FR-016**: Property values MUST deserialize correctly when loading an entity for viewing or editing.
- **FR-017**: Empty/undefined property values MUST NOT be stored (avoid storing empty objects or null values as properties).

#### Migration

- **FR-018**: The existing four Regional property components MUST be replaced by the dynamic renderer.
- **FR-019**: Existing entity data with properties MUST continue to work without migration.

### Key Entities

- **PropertyFieldSchema**: Defines a single custom property field with key, label, type, and optional attributes (placeholder, description, maxLength). Optional PropertyFieldValidation sub-object supports: required (boolean), min/max (for numeric fields), pattern (regex for text fields). Defined in entityTypeRegistry.ts alongside EntityTypeConfig.
- **EntityTypeConfig**: Extended with optional `propertySchema` array containing PropertyFieldSchema definitions. Central metadata for each WorldEntity type.
- **WorldEntity.properties**: JSON string field storing the actual property values for an entity instance.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All four existing Regional entity types (GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion) render their custom properties using the dynamic schema-driven renderer.
- **SC-002**: Adding a new entity type with custom properties requires only registry configuration changes (no new component files).
- **SC-003**: 100% of existing entity property data displays correctly after the migration.
- **SC-004**: Users can complete property editing workflows with equivalent or better experience than the current implementation.
- **SC-005**: The four existing custom property component files (~500+ lines) are removed, reducing codebase duplication.
- **SC-006**: All property field types (text, textarea, integer, decimal, tagArray) pass accessibility testing with no violations.

## Assumptions

- The existing tagInput component (`TagInput.tsx`) can be reused as-is for tagArray field rendering.
- The existing numeric validation utilities (`numericValidation.ts`) provide sufficient validation for integer and decimal fields.
- Entity type changes during editing remain blocked (existing behavior).
- The propertySchema will remain in the frontend code registry for Phase 1; backend-driven schemas are out of scope.
- No new field types beyond the five listed (text, textarea, integer, decimal, tagArray) are required for Phase 1.

## Out of Scope

- Backend changes or API modifications
- User-configurable entity type schemas (Phase 3+)
- New field types beyond the five specified (e.g., date pickers, selects, relationships)
- Schema versioning or migration tooling
- Admin UI for schema management
