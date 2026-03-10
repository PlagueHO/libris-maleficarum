# Feature Specification: World Data Importer

**Feature Branch**: `016-world-data-importer`  
**Created**: 2026-03-10  
**Status**: Draft  
**Input**: User description: "Create a data importer console tool that can be used to import a world into the backend API. This will enable preload a world (create the world and load all world entities) into the backend API to enable exploratory testing, demo and test automation. The import format will be a folder containing a hierarchy of json documents. It should alternately be a zip file containing the same hierarchy of json documents. In future the same import (and export) functionality will be also used in the frontend app to provide import/export of the world for backup etc."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Import World from Folder (Priority: P1)

As a developer or tester, I want to import a complete world (world metadata plus all world entities in their hierarchy) from a local folder of JSON files so that I can quickly seed the backend with test data for exploratory testing, demos, or automated tests.

**Why this priority**: This is the core value proposition of the tool. Without folder-based import, there is no way to bulk-load world data into the backend, making exploratory testing and demos manual and time-consuming.

**Independent Test**: Can be fully tested by pointing the tool at a sample folder containing a world JSON file and nested entity JSON files, running the import, and verifying all entities appear in the backend via the API.

**Acceptance Scenarios**:

1. **Given** a folder containing a `world.json` file and entity JSON files (in any subfolder structure), **When** the user runs the import tool specifying that folder path and the backend API URL, **Then** the world is created via the API and all entities are created with the correct parent-child relationships based on their `localId`/`parentLocalId` references.
1. **Given** a valid import folder, **When** the import completes successfully, **Then** the tool outputs a summary showing the number of entities created, organized by entity type, along with the total import duration.
1. **Given** one or more entity JSON files contain invalid data (missing required fields, malformed JSON), **When** the import runs, **Then** the tool reports each validation error with the file path and reason, skips the invalid entity and all its descendants, continues importing remaining valid entities, and exits with a non-zero status code indicating partial failure.
1. **Given** the backend API is unreachable, **When** the user runs the import, **Then** the tool reports a clear connection error and exits with a non-zero status code.

---

### User Story 2 - Import World from Zip Archive (Priority: P2)

As a developer or tester, I want to import a world from a zip file (containing the same folder hierarchy of JSON documents) so that I can distribute and share world data as a single portable file.

**Why this priority**: Zip support enables portable sharing of world data across teams and environments. It builds on the folder import foundation (P1) by adding an additional input format.

**Independent Test**: Can be tested by creating a zip file from a valid import folder, running the import with the zip path, and verifying the same results as folder import.

**Acceptance Scenarios**:

1. **Given** a zip file containing a `world.json` at the root and entity JSON files in any subfolder structure, **When** the user runs the import tool specifying the zip file path, **Then** the tool extracts the contents and imports the world and all entities identically to folder-based import.
1. **Given** a corrupted or invalid zip file, **When** the user runs the import, **Then** the tool reports a clear error about the invalid archive and exits with a non-zero status code.

---

### User Story 3 - Validate Import Data Without Importing (Priority: P3)

As a developer or content creator, I want to validate my import data (folder or zip) without actually sending it to the backend so that I can fix errors before attempting the real import.

**Why this priority**: Dry-run validation saves time and reduces errors during import, especially for large world datasets. It is a quality-of-life improvement that builds on the core import functionality.

**Independent Test**: Can be tested by running the tool in validation-only mode against a folder with both valid and invalid files, and verifying it reports all issues without making any API calls.

**Acceptance Scenarios**:

1. **Given** an import source (folder or zip), **When** the user runs the tool in validate-only mode, **Then** the tool checks all JSON files for structural validity, required fields, valid entity types, valid `localId` uniqueness, valid `parentLocalId` references, and cycle-free hierarchy, and reports all issues without contacting the backend API.
1. **Given** a valid import source, **When** the user runs validate-only mode, **Then** the tool reports success with a summary of entities that would be imported.

---

### User Story 4 - Sample World Data for Testing (Priority: P3)

As a developer, I want the project to include a sample world dataset so that I have a ready-made import source for testing, demos, and verifying the import tool works correctly.

**Why this priority**: A bundled sample world provides immediate value for demos and test automation without requiring users to manually create test data.

**Independent Test**: Can be tested by running the import tool against the bundled sample data folder and verifying a complete world is created in the backend.

**Acceptance Scenarios**:

1. **Given** the project repository, **When** a developer looks for sample data, **Then** a sample world folder exists containing a world definition with at least 3 levels of entity hierarchy covering multiple entity types (locations, characters, campaigns).
1. **Given** the sample world dataset, **When** imported via the tool, **Then** all entities are created successfully with correct parent-child relationships and properties.

---

### Edge Cases

- What happens when an import folder is empty or contains no `world.json` file?
- What happens when an entity's `parentLocalId` references a `localId` that does not exist in the import?
- What happens when duplicate `localId` values are found across import files?
- What happens when the import folder contains non-JSON files (they should be ignored)?
- How does the tool handle very large worlds (thousands of entities) — does it provide progress feedback?
- What happens when a world with the same ID already exists in the backend?
- What happens when the zip file contains nested zip files or symlinks?
- How does the tool handle entity JSON files with extra unrecognized fields?
- What happens when `parentLocalId` references form a cycle (A → B → A)?

## Requirements *(mandatory)*

### Functional Requirements

#### Import Source

- **FR-001**: The tool MUST accept a folder path as an import source containing JSON files (in any subfolder organization).
- **FR-002**: The tool MUST accept a zip file path as an import source containing the same collection of JSON files.
- **FR-003**: The tool MUST detect the import source type (folder vs. zip) automatically based on the provided path.
- **FR-032**: When extracting a zip archive, the tool MUST reject any zip entry whose resolved path escapes the extraction directory (zip-slip protection) and report the offending entry as an error.

#### Import Format

- **FR-004**: The import source MUST contain a `world.json` file at the root level defining the world metadata (name, description).
- **FR-005**: Entity JSON files MAY be organized in any subfolder structure for human readability; folder structure has no semantic meaning for parent-child relationships.
- **FR-006**: Each entity JSON file MUST include at minimum: `localId` (a human-readable unique identifier within the import), `entityType`, `name`, and optionally `parentLocalId`, `description`, `tags`, and `properties`.
- **FR-007**: Parent-child relationships MUST be defined explicitly via `parentLocalId` fields in each entity JSON file. An entity with no `parentLocalId` (or null) is a root-level entity whose parent is the world itself.
- **FR-008**: The tool MUST ignore non-JSON files in the import source (e.g., README files, images).
- **FR-036**: Each `localId` MUST be unique across all entity files within a single import source.
- **FR-037**: The `localId` values are used only for establishing relationships during import; the tool assigns new GUIDs as the actual entity IDs in the backend.

#### Import Execution

- **FR-009**: The tool MUST create the world via the backend REST API before creating any entities.
- **FR-010**: The tool MUST create entities in hierarchy order (parents before children) to ensure valid parent references.
- **FR-011**: The tool MUST assign new GUIDs for the world and all entities during import (import data does not dictate backend IDs).
- **FR-012**: The tool MUST NOT set `ownerId` explicitly — the backend API derives ownership from the authenticated user token. The `--token` parameter (or `LIBRIS_API_TOKEN` env var) determines the owner.
- **FR-013**: The tool MUST populate `path`, `depth`, and `parentId` fields correctly based on the resolved hierarchy from `localId`/`parentLocalId` references.
- **FR-029**: The tool MUST create entities at the same hierarchy depth in parallel to improve import performance.
- **FR-030**: The tool MUST support a configurable max concurrency limit for parallel API calls, with a sensible default.

#### Validation

- **FR-014**: The tool MUST validate all JSON files for structural correctness before beginning the import.
- **FR-015**: The tool MUST validate that all entities have a recognized `entityType` matching the known entity types.
- **FR-016**: The tool MUST validate that all `parentLocalId` references resolve to an existing `localId` within the import, and that no cycles exist in the hierarchy.
- **FR-017**: The tool MUST support a validate-only mode (dry run) that checks data without making API calls.
- **FR-038**: The tool MUST validate that all `localId` values are unique across the entire import source.

#### Command-Line Interface

- **FR-018**: The tool MUST accept the import source path as a required argument.
- **FR-019**: The tool MUST accept the backend API base URL as a required argument.
- **FR-021**: The tool MUST support a `--validate-only` flag for dry-run validation.
- **FR-022**: The tool MUST support a `--verbose` flag for detailed output during import.
- **FR-031**: The tool MUST support a `--max-concurrency` parameter to control the number of parallel API calls (default: 10).
- **FR-033**: The tool MUST accept an authentication token via a CLI parameter (e.g., `--token`), which takes precedence over an environment variable.
- **FR-034**: The tool MUST fall back to reading an authentication token from a well-known environment variable if no CLI parameter is provided.
- **FR-035**: The tool MUST NOT hardcode, log, or persist authentication credentials.
- **FR-039**: The tool MUST set `schemaVersion` to `1` for all entities created during import. The import file format does not include a schema version field; versioning is managed by the backend.

#### Output and Reporting

- **FR-023**: The tool MUST output a summary upon completion showing: total entities created, entities by type, errors encountered, and total duration.
- **FR-024**: The tool MUST report individual file errors with file path, line number (if applicable), and a description of the issue.
- **FR-025**: The tool MUST exit with status code 0 on full success, and a non-zero status code if any errors occurred.
- **FR-027**: When an entity fails to import, the tool MUST skip that entity and all its descendant entities, since children cannot be created without a valid parent.
- **FR-028**: The tool MUST support an optional `--log-file` parameter to write a detailed import log to a file, capturing every entity creation attempt, success/failure status, and error details.

#### Reusability

- **FR-026**: The import format parsing and validation logic MUST be structured as a reusable library separate from the console tool, to enable future reuse in the frontend application for import/export functionality.

### Key Entities

- **Import Source**: The folder or zip archive provided by the user, containing the world definition and entity hierarchy as JSON files.
- **World Definition** (`world.json`): The root-level file defining world metadata (name, description). Maps to the World container in the backend.
- **Entity Definition** (entity JSON files): Individual JSON files representing world entities (locations, characters, campaigns, etc.). Maps to the WorldEntity container in the backend. Each file carries a `localId` (unique within the import), an optional `parentLocalId` (referencing another entity's `localId`), its `entityType`, `name`, `description`, `tags`, and optional `properties`.
- **Import Manifest** (generated internally): A computed representation of the full entity tree derived from `localId`/`parentLocalId` references, used to determine creation order (depth-first), validate hierarchy consistency (no cycles, no dangling references), and map local IDs to generated GUIDs.

## Clarifications

### Session 2026-03-10

- Q: When import is partially complete and a subsequent API call fails, what should the tool do? → A: Continue and report — skip the failed entity and all its descendant entities (since children cannot be created without a valid parent), continue importing remaining valid entities, and report all failures at the end.
- Q: Should API requests be sent sequentially or in parallel? → A: Parallel per hierarchy level — create all entities at the same depth concurrently (since all parents already exist), with a configurable max concurrency limit.
- Q: How should the tool handle zip entry paths to prevent zip-slip attacks? → A: Strict validation — reject any zip entry whose resolved path escapes the extraction directory.
- Q: How should the tool receive authentication credentials for the backend API? → A: Both — accept a CLI parameter (takes precedence) or fall back to an environment variable.
- Q: How should the import format define entity relationships given the flat-with-ParentId data model? → A: Flat files with explicit local references — each entity JSON has a `localId` and `parentLocalId` field; folder structure is for human organization only and has no semantic meaning for hierarchy.

## Assumptions

- The backend REST API (as documented in the API design) is available and supports the standard World and WorldEntity CRUD endpoints at the time this tool is used.
- Authentication to the backend API will be provided via a CLI parameter (e.g., `--token`) that takes precedence, or via an environment variable as a fallback. The tool MUST NOT hardcode or persist credentials.
- The import tool is intended primarily for developer/tester use and does not need a graphical interface. A well-structured CLI with clear output is sufficient.
- Entity JSON files use camelCase property naming to align with the frontend TypeScript conventions and the JSON serialization used by the backend API.
- The sample world dataset will be a fantasy/TTRPG themed world to fit the application domain.
- For this initial implementation, assets (images, audio, etc.) are out of scope for import. Only world metadata and entity data are imported.
- The import creates a new world each time; updating or merging with an existing world is out of scope for this feature.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A developer can import a sample world with 50+ entities from a folder in under 60 seconds.
- **SC-002**: A developer can import the same sample world from a zip file with identical results to folder import.
- **SC-003**: 100% of validation errors in import data are detected and reported before any API calls are made when using validate-only mode.
- **SC-004**: The import tool can be run as part of an automated CI/CD pipeline or test setup script without manual intervention (non-interactive execution).
- **SC-005**: The sample world dataset included in the project contains at least 20 entities across at least 4 different entity types with a minimum hierarchy depth of 3 levels.
- **SC-006**: The reusable import library can be consumed independently of the console tool (no console-specific dependencies in the library).
