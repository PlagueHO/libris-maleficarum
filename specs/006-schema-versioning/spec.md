# Feature Specification: Schema Versioning for WorldEntities

**Feature Branch**: `006-schema-versioning`  
**Created**: January 21, 2026  
**Status**: Draft  
**Input**: User description: "Add support to the WorldEntities to version the schema of each entity type. For example, a World Entity of Person could start with a schema version of 1 but would be updated 2 when the schema is extended. Extending a schema means adding new fields to the schema. The reason for this is so that we can evolve the schema of WorldEntities over time but not have to rewrite/update all WorldEntity when we extend the schema. In general the schema version won't be often used, but it will be written/updated whenever the WorldEntity is saved. You must ensure that the DATA_MODEL.md and API.md are updated to align to this change. We haven't yet defined the complete schema of each entity type, but will do in a future feature. This feature is just to include the support to ensure the APIs support it, the front end supports it and all the world entity create/edit functions work."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Backend API Captures Schema Version (Priority: P1)

When a user creates or updates a WorldEntity through the API, the system automatically captures and persists the schema version associated with that entity type. This ensures that as schemas evolve over time, each entity document retains information about which version of the schema it was created with, enabling future schema migrations and backward compatibility.

**Why this priority**: This is the foundation for schema versioning. Without capturing the schema version at write time, no other versioning functionality can work. This is the minimal viable implementation that enables future schema evolution.

**Independent Test**: Can be fully tested by creating or updating a WorldEntity via the API and verifying that the response includes a `SchemaVersion` field with an appropriate integer value (e.g., `1`). The backend persists this value to Cosmos DB and returns it in all read operations.

**Acceptance Scenarios**:

1. **Given** a user creates a new WorldEntity (e.g., Character, Location, Campaign), **When** the API persists the entity to Cosmos DB, **Then** the entity document includes a `SchemaVersion` field set to the current schema version for that entity type (default: `1`)
1. **Given** a user updates an existing WorldEntity, **When** the API persists the changes with a client-provided `SchemaVersion`, **Then** the entity document is updated with the new `SchemaVersion` value if it passes validation (>= existing version and <= backend's max supported version for that entity type)
1. **Given** a user reads a WorldEntity from the API, **When** the API returns the entity, **Then** the response includes the `SchemaVersion` field
1. **Given** multiple entity types exist (Character, Location, Campaign, etc.), **When** entities are created, **Then** each entity type uses its own independent schema version (e.g., Character v1, Location v1)

---

### User Story 2 - Frontend Saves with Latest Schema Version (Priority: P2)

When a user edits and saves a WorldEntity in the frontend application, the interface automatically uses the latest schema version defined in the frontend configuration. This ensures that every save operation migrates the entity to the current schema version, enabling automatic schema evolution without requiring bulk migration operations.

**Why this priority**: This enables automatic schema migration on every save, ensuring entities gradually migrate to newer schema versions as users interact with them. Since schemas can only be extended (fields added, never removed), this forward-migration is safe and prevents schema fragmentation.

**Independent Test**: Can be fully tested by loading a WorldEntity with an older schema version (e.g., `1`), editing it in the frontend, and confirming the save request includes the current schema version (e.g., `2`) defined in the frontend configuration.

**Acceptance Scenarios**:

1. **Given** a user loads a WorldEntity in the frontend, **When** the API response is received, **Then** the Redux state stores the `SchemaVersion` field from the response
1. **Given** a user edits a WorldEntity with schema version `1`, **When** the save request is sent to the API, **Then** the request payload includes the `SchemaVersion` field with the current schema version (e.g., `2`) as defined in the frontend configuration
1. **Given** a user creates a new WorldEntity via the frontend, **When** the create request is sent, **Then** the request includes the current `SchemaVersion` value from the frontend configuration
1. **Given** a developer inspects entity data in browser DevTools, **When** viewing Redux state or API responses, **Then** the `SchemaVersion` field is visible and populated

---

### User Story 3 - Documentation Reflects Schema Versioning (Priority: P3)

When developers or architects review the DATA_MODEL.md and API.md design documents, they see clear documentation of the schema versioning approach, including field definitions, default values, evolution strategy, and backward compatibility guidelines.

**Why this priority**: Documentation ensures consistency and guides future development. While important for long-term maintainability, it doesn't block implementation or testing of the feature itself.

**Independent Test**: Can be fully tested by reviewing the updated DATA_MODEL.md and API.md files and verifying they include schema versioning field definitions, example documents, query patterns, and evolution guidelines.

**Acceptance Scenarios**:

1. **Given** a developer reads DATA_MODEL.md, **When** reviewing the BaseWorldEntity schema, **Then** they see the `SchemaVersion` field documented with type, default value, and purpose
1. **Given** an architect reviews API.md, **When** examining request/response examples, **Then** they see `SchemaVersion` included in example JSON payloads
1. **Given** a developer plans a schema migration, **When** reading documentation, **Then** they find guidance on how to handle schema version increments and backward compatibility

---

## Clarifications

### Session 2026-01-21

- Q: Schema Version Configuration Storage - The specification states that "the current schema version for each entity type is stored in the frontend configuration" but doesn't specify how this configuration is managed across entity types or where exactly it lives. → A: Centralized constants file with version map (e.g., `ENTITY_SCHEMA_VERSIONS = { Character: 2, Location: 1 }`). Note: In a future feature, we will implement a data-driven world entity schema file for each entity type that will contain the version number in it.
- Q: Backend Schema Version Update Behavior - User Story 1 states "the entity document retains its existing SchemaVersion value unless the schema has been explicitly updated" but it's unclear what "explicitly updated" means in the context of the backend accepting any client-provided schema version. → A: Backend always accepts and persists whatever SchemaVersion the client sends in the request payload, BUT with validation: (1) SchemaVersion must be >= existing entity's SchemaVersion (prevents downgrades), and (2) SchemaVersion must be <= backend's supported schema version for that entity type (prevents using future schemas backend doesn't support).
- Q: Default Schema Version for New Entities - FR-002 states the system defaults SchemaVersion to `1` "when no version is explicitly provided," but User Story 2 says the frontend always sends the current schema version from its configuration. When should the backend use the default value of `1`? → A: Only when the client completely omits the SchemaVersion field from the request (backward compatibility for API clients that don't send SchemaVersion).
- Q: Error Response Details for Schema Version Validation - The specification mentions 400 Bad Request errors for invalid schema versions, but doesn't specify what details should be included in error responses to help clients understand and fix schema version validation failures. → A: Include specific error codes (e.g., "SCHEMA_VERSION_TOO_HIGH", "SCHEMA_DOWNGRADE_NOT_ALLOWED") and current/max version values in response to aid debugging.
- Q: Schema Version Handling During Entity Creation - User Story 2 states the frontend sends the current schema version when creating new entities, but it's unclear whether the backend should validate this matches expected version or accept any valid version within supported range. → A: The backend should maintain both minimum and maximum schema versions per WorldEntity type (e.g., Character: min=1, max=2), and validate that incoming SchemaVersion falls within this range for both create and update operations.

### Edge Cases

- What happens when an old entity (created before schema versioning was implemented) is read? The system treats missing `SchemaVersion` as version `1` (original schema)
- How does the system handle invalid schema version values (e.g., negative numbers, non-integers)? The API validates that `SchemaVersion` is a positive integer and rejects invalid values with a 400 Bad Request error
- What happens when a client attempts to create or update an entity with a deprecated schema version (below minimum)? The API rejects the request with a 400 Bad Request error indicating the schema version is no longer supported by the backend
- What happens when a client attempts to downgrade an entity's schema version (e.g., from v2 to v1)? The API rejects the request with a 400 Bad Request error indicating schema downgrades are not allowed
- What happens when a client sends a schema version higher than the backend's maximum supported version for that entity type? The API rejects the request with a 400 Bad Request error indicating the schema version is not supported by the backend
- What happens when a user edits an entity with an older schema version? The frontend automatically upgrades it to the latest schema version on save, effectively migrating the entity forward. Since schemas can only be extended (no field removal), this migration is safe and preserves all existing data.
- How does the system handle concurrent updates to the same entity with different schema versions? Standard optimistic concurrency controls apply (using `_etag`); schema version is just another field that gets updated atomically. If two users save concurrently, the second save will fail with a conflict error and the user must reload and retry.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST add a `SchemaVersion` field to the BaseWorldEntity schema, stored as an integer
- **FR-002**: System MUST default `SchemaVersion` to `1` for all new WorldEntities when no version is explicitly provided
- **FR-003**: System MUST persist the `SchemaVersion` field to the WorldEntity Cosmos DB container documents when creating or updating entities
- **FR-004**: System MUST return the `SchemaVersion` field in all API responses that include WorldEntity data (GET, POST, PUT)
- **FR-005**: Backend MUST validate that `SchemaVersion` is a positive integer (≥1) when provided in API requests
- **FR-005a**: Backend MUST reject requests with a `SchemaVersion` less than the existing entity's current `SchemaVersion` (prevents downgrades) with a 400 Bad Request error
- **FR-005b**: Backend MUST maintain a configuration of minimum and maximum supported schema versions per entity type (e.g., Character: min=1, max=2, Location: min=1, max=1) and reject requests with `SchemaVersion` values outside these ranges with a 400 Bad Request error
- **FR-005c**: Backend MUST include specific error codes (e.g., "SCHEMA_VERSION_INVALID", "SCHEMA_VERSION_TOO_HIGH", "SCHEMA_VERSION_TOO_LOW", "SCHEMA_DOWNGRADE_NOT_ALLOWED") and contextual details (current version, min/max supported versions) in schema version validation error responses
- **FR-006**: Frontend MUST store the `SchemaVersion` field in Redux state when loading entities from the API
- **FR-007**: Frontend MUST include the `SchemaVersion` field in all entity create and update requests (POST, PUT, PATCH) using the current schema version defined in the frontend configuration (not the original value from the loaded entity)
- **FR-008**: System MUST treat entities with missing `SchemaVersion` fields as version `1` for backward compatibility with existing data
- **FR-009**: DATA_MODEL.md MUST be updated to document the `SchemaVersion` field in the BaseWorldEntity schema, including purpose, type, and default value
- **FR-010**: API.md MUST be updated to include `SchemaVersion` in example request/response payloads for entity CRUD operations

### Key Entities

- **WorldEntity (all subtypes)**: Each entity document (Character, Location, Campaign, etc.) includes a `SchemaVersion` field indicating which version of that entity type's schema was used when the entity was created or last migrated. This allows the system to apply appropriate property validation rules and UI generation based on the schema version.
- **PropertySchema (future)**: Schema template documents will include a `Version` field to indicate the current version of the schema. The WorldEntity's `SchemaVersion` references this version number. This entity is not modified in this feature but is documented for context.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All WorldEntity documents persisted to Cosmos DB include a `SchemaVersion` field with a positive integer value
- **SC-002**: API responses for WorldEntity CRUD operations include the `SchemaVersion` field in 100% of cases
- **SC-003**: Frontend Redux state preserves `SchemaVersion` data without loss during the create/read/update cycle
- **SC-004**: DATA_MODEL.md and API.md documentation accurately reflect the schema versioning implementation with updated schemas, examples, and guidelines
- **SC-005**: Existing WorldEntities without `SchemaVersion` fields can be read and updated without errors (backward compatibility maintained)

## Assumptions

- Schema versions are simple integers starting at `1` and incrementing sequentially (e.g., `1`, `2`, `3`)
- Each entity type has its own independent schema version sequence (Character v1 is independent from Location v1)
- **Schemas can only be extended, never contracted**: New fields can be added in later versions, but existing fields cannot be removed or renamed. This ensures forward migration (v1 → v2) is always safe and non-destructive.
- Schema version updates are infrequent and manually controlled (not automatically incremented on every entity save)
- The current schema version for each entity type is stored in a centralized frontend constants file as a version map (e.g., `ENTITY_SCHEMA_VERSIONS = { Character: 2, Location: 1, Campaign: 1 }`) and updated when new schema versions are deployed
- The backend maintains a configuration of minimum and maximum supported schema versions per entity type (e.g., `Character: { min: 1, max: 2 }`) to validate client requests and prevent use of deprecated or future schema versions
- Frontend and backend schema version configurations are kept in sync during deployment (both updated together)
- In a future feature, schema versions will be defined in data-driven schema files per entity type; the constants file approach is an interim solution
- The backend validates schema version ranges (within min/max, no downgrades) but does not validate that property structures match the declared schema version; property validation is deferred to future work
- For this feature, all entities default to schema version `1` since we haven't defined complete schemas yet
- Automatic schema migration on save is a "lazy migration" strategy: entities are only upgraded when users edit them, not in bulk
- The backend will not validate that property structures match the declared schema version; that validation is deferred to future work

## Out of Scope

- Automatic schema version detection based on entity property analysis
- Schema migration logic to transform entities from one version to another
- Version-specific property validation rules
- UI indication of which schema version an entity uses
- Bulk schema version update operations
- Schema version history or audit trail
- Cross-entity schema version dependencies or constraints
- Schema version rollback mechanisms
