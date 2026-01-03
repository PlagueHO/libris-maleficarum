# Feature Specification: Backend REST API with Cosmos DB Integration

**Feature Branch**: `001-backend-rest-api`  
**Created**: 2026-01-03  
**Status**: Draft  
**Input**: User description: "The backend REST API in .NET 10 using ASP.NET Web API as per the specification in the BACKEND.md and API.md. It should be created in the existing projects: LibrisMaleficarum.Api.csproj, LibrisMaleficarum.Domain.csproj and LibrisMaleficarum.Infrastructure.csproj. It will use Aspire AppHost in LibrisMaleficarum.AppHost.csproj to provide the Cosmos DB service (for local development). This feature should include creation of the repository for communicating with the backend Cosmos DB."

## User Scenarios & Testing

### User Story 1 - World Management API (Priority: P1)

As a frontend developer, I need to create, read, update, and delete worlds through a RESTful API so that users can manage their world-building projects.

**Why this priority**: This is the foundational functionality that enables all other features. Without the ability to manage worlds, no other entity operations can function. This represents the minimum viable API for the application.

**Independent Test**: Can be fully tested by making HTTP requests to `/api/v1/worlds` endpoints and verifying CRUD operations work correctly, returning appropriate HTTP status codes and JSON responses. Delivers immediate value by allowing world creation and retrieval.

**Acceptance Scenarios**:

1. **Given** no existing worlds for a user, **When** the user sends POST request to `/api/v1/worlds` with valid world data, **Then** the system creates a new world and returns 201 Created with the world details
1. **Given** a user has created worlds, **When** the user sends GET request to `/api/v1/worlds`, **Then** the system returns 200 OK with a list of all worlds owned by that user
1. **Given** a world exists, **When** the user sends GET request to `/api/v1/worlds/{worldId}`, **Then** the system returns 200 OK with the complete world details
1. **Given** a world exists, **When** the user sends PUT request to `/api/v1/worlds/{worldId}` with updated data, **Then** the system updates the world and returns 200 OK
1. **Given** a world exists, **When** the user sends DELETE request to `/api/v1/worlds/{worldId}`, **Then** the system soft-deletes the world and returns 204 No Content
1. **Given** a world does not exist, **When** the user sends GET request to `/api/v1/worlds/{invalidId}`, **Then** the system returns 404 Not Found with error details

---

### User Story 2 - Entity Management API (Priority: P2)

As a frontend developer, I need to create and manage entities (locations, characters, campaigns) within worlds through the API so that users can build detailed world content.

**Why this priority**: Once worlds can be managed, users need to add content to them. This enables the core world-building functionality but depends on World Management being operational first.

**Independent Test**: Can be tested by first creating a world via P1 API, then making HTTP requests to `/api/v1/worlds/{worldId}/entities` endpoints to verify entity CRUD operations, hierarchical relationships, and filtering work correctly.

**Acceptance Scenarios**:

1. **Given** a world exists, **When** the user sends POST request to `/api/v1/worlds/{worldId}/entities` with valid entity data, **Then** the system creates the entity and returns 201 Created
1. **Given** entities exist in a world, **When** the user sends GET request to `/api/v1/worlds/{worldId}/entities`, **Then** the system returns 200 OK with all entities in that world
1. **Given** a parent entity exists, **When** the user sends GET request to `/api/v1/worlds/{worldId}/entities/{parentId}/children`, **Then** the system returns 200 OK with all child entities
1. **Given** an entity exists, **When** the user sends PUT request to `/api/v1/worlds/{worldId}/entities/{entityId}` with updated data, **Then** the system updates the entity and returns 200 OK
1. **Given** an entity exists, **When** the user sends DELETE request to `/api/v1/worlds/{worldId}/entities/{entityId}`, **Then** the system soft-deletes the entity and returns 204 No Content
1. **Given** entities with different types exist, **When** the user sends GET request to `/api/v1/worlds/{worldId}/entities?type=Character`, **Then** the system returns 200 OK with only Character entities

---

### User Story 3 - Asset Management API (Priority: P3)

As a frontend developer, I need to upload and manage assets (images, audio, documents) associated with entities through the API so that users can enrich their world content with media files.

**Why this priority**: Asset management enhances the world-building experience but is not critical for the core functionality. Users can create and manage textual content without assets.

**Independent Test**: Can be tested by creating a world and entity via P1/P2 APIs, then uploading files to `/api/v1/worlds/{worldId}/entities/{entityId}/assets` and verifying upload, retrieval, and deletion work correctly with proper file handling.

**Acceptance Scenarios**:

1. **Given** an entity exists, **When** the user sends POST request to `/api/v1/worlds/{worldId}/entities/{entityId}/assets` with a file upload, **Then** the system uploads the asset and returns 201 Created with asset metadata
1. **Given** assets are attached to an entity, **When** the user sends GET request to `/api/v1/worlds/{worldId}/entities/{entityId}/assets`, **Then** the system returns 200 OK with a list of all assets
1. **Given** an asset exists, **When** the user sends GET request to `/api/v1/worlds/{worldId}/assets/{assetId}/download`, **Then** the system returns the asset file with appropriate content type
1. **Given** an asset exists, **When** the user sends DELETE request to `/api/v1/worlds/{worldId}/assets/{assetId}`, **Then** the system deletes the asset and returns 204 No Content
1. **Given** a file is too large, **When** the user attempts to upload it, **Then** the system returns 400 Bad Request with size limit error

---

### User Story 4 - Search and Filter API (Priority: P3)

As a frontend developer, I need to search and filter entities within a world through the API so that users can quickly find specific content in large worlds.

**Why this priority**: Search improves usability for complex worlds but is not essential for basic world management. Users can browse entities manually in small worlds.

**Independent Test**: Can be tested by creating a world with multiple entities via P1/P2 APIs, then using `/api/v1/worlds/{worldId}/search` with various query parameters to verify search and filtering return correct results.

**Acceptance Scenarios**:

1. **Given** entities with different names exist, **When** the user sends GET request to `/api/v1/worlds/{worldId}/search?q={searchTerm}`, **Then** the system returns 200 OK with entities matching the search term
1. **Given** entities with different tags exist, **When** the user sends GET request to `/api/v1/worlds/{worldId}/entities?tags={tag1,tag2}`, **Then** the system returns 200 OK with entities having those tags
1. **Given** many entities exist, **When** the user sends GET request with pagination parameters, **Then** the system returns paginated results with cursor for next page

---

### User Story 5 - Local Development with Aspire (Priority: P1)

As a developer, I need to run the entire backend stack (API + Cosmos DB) locally with a single command using Aspire so that I can develop and test without cloud dependencies.

**Why this priority**: Essential for developer productivity and inner-loop development. Without this, developers would need to configure and manage multiple services manually, significantly slowing development.

**Independent Test**: Can be tested by running the Aspire AppHost project and verifying that (1) Cosmos DB Emulator starts automatically, (2) API service registers and starts, (3) Aspire Dashboard shows all services healthy, and (4) API can successfully connect to Cosmos DB.

**Acceptance Scenarios**:

1. **Given** the developer has cloned the repository, **When** they run `dotnet run --project src/Orchestration/AppHost`, **Then** Aspire starts all services and opens the dashboard
1. **Given** Aspire is running, **When** the developer navigates to the dashboard, **Then** they see API service and Cosmos DB service both healthy
1. **Given** Aspire is running, **When** the developer makes an API request to create a world, **Then** the data is persisted in the local Cosmos DB Emulator
1. **Given** Aspire is stopped, **When** the developer restarts it, **Then** previously created data persists across restarts

---

### Edge Cases

- What happens when a user tries to access a world owned by another user? (Expected: 403 Forbidden with authorization error)
- What happens when a user tries to create an entity with an invalid `EntityType`? (Expected: 400 Bad Request with field-level validation error specifying valid EntityType values)
- What happens when a user tries to delete a parent entity that has children? (Expected: 400 Bad Request with error listing child count; optional `cascade=true` query parameter enables cascading soft-delete of all descendants)
- What happens when Cosmos DB is unavailable? (Expected: 503 Service Unavailable with retry-after header)
- What happens when a user uploads an asset with a malicious file extension? (Expected: 400 Bad Request with validation error listing allowed types)
- What happens when a user uploads an asset exceeding the size limit? (Expected: 400 Bad Request with error message indicating max size)
- What happens when pagination cursor is invalid or expired? (Expected: 400 Bad Request with error message)
- What happens when concurrent updates occur to the same entity? (Expected: Optimistic concurrency with ETag validation, 409 Conflict on version mismatch)

## Requirements

### Functional Requirements

- **FR-001**: System MUST expose RESTful API endpoints for World CRUD operations at `/api/v1/worlds`
- **FR-002**: System MUST expose RESTful API endpoints for Entity CRUD operations at `/api/v1/worlds/{worldId}/entities`
- **FR-003**: System MUST expose RESTful API endpoints for Asset management at `/api/v1/worlds/{worldId}/assets`
- **FR-004**: System MUST validate all API requests and return 400 Bad Request with detailed error messages for invalid input
- **FR-004a**: System MUST return field-level validation errors including field name and specific error message for each validation failure
- **FR-005**: System MUST return consistent JSON response format for all endpoints (data + meta structure)
- **FR-006**: System MUST return appropriate HTTP status codes (200, 201, 204, 400, 401, 403, 404, 409, 500)
- **FR-007**: System MUST implement repository pattern with interfaces in Domain layer and implementations in Infrastructure layer
- **FR-008**: System MUST use Entity Framework Core with Cosmos DB provider for data access
- **FR-009**: System MUST implement stubbed `IUserContextService` for user authentication (production auth deferred)
- **FR-010**: System MUST enforce row-level security ensuring users can only access worlds they own
- **FR-011**: System MUST support hierarchical partition keys for Cosmos DB (`/WorldId` for entities)
- **FR-012**: System MUST implement soft delete for worlds and entities (move to DeletedWorldEntity container)
- **FR-013**: System MUST support cursor-based pagination for list endpoints
- **FR-013a**: System MUST return default 50 items per page when no limit specified, with maximum 200 items per page
- **FR-014**: System MUST support filtering by entity type and tags via query parameters
- **FR-014a**: System MUST implement search via abstracted interface to allow future replacement with Azure AI Search
- **FR-014b**: System MUST perform case-insensitive partial matching (contains) on Name, Description, and Tags fields for initial search implementation
- **FR-015**: System MUST support sorting by name, createdDate, modifiedDate via query parameters
- **FR-016**: System MUST configure Aspire AppHost to orchestrate API service and Cosmos DB Emulator for local development
- **FR-017**: System MUST configure service defaults (telemetry, health checks, resilience) via `AddServiceDefaults()` extension
- **FR-018**: System MUST provide Aspire Dashboard access for observability (logs, traces, metrics)
- **FR-019**: System MUST support asset upload with file validation (type, size)
- **FR-019a**: System MUST enforce runtime-configurable asset size limits (default: 25MB per file)
- **FR-019b**: System MUST enforce runtime-configurable allowed file types (default: images [jpg/jpeg/png/gif/webp], audio [mp3/wav/ogg], video [mp4/webm], documents [pdf/txt/md])
- **FR-020**: System MUST generate and return SAS tokens for secure asset download from Azure Blob Storage
- **FR-021**: System MUST prevent deletion of parent entities with children by default, returning 400 Bad Request with child count
- **FR-022**: System MUST support optional `cascade=true` query parameter on DELETE requests to enable cascading soft-delete of parent and all descendants
- **FR-023**: System MUST return ETag header in all GET responses for worlds and entities
- **FR-024**: System MUST validate If-Match header when provided on PUT/PATCH requests, returning 409 Conflict if ETag mismatch detected
- **FR-025**: System MUST allow PUT/PATCH requests without If-Match header (last-write-wins behavior when not provided)

### Key Entities

- **World**: Represents a user's world-building project with properties: Id (GUID), OwnerId (GUID), Name, Description, CreatedDate, ModifiedDate, IsDeleted
- **WorldEntity**: Represents any entity within a world (locations, characters, campaigns, etc.) with properties: Id (GUID), WorldId (GUID), ParentId (nullable GUID), EntityType (enum), Name, Description, Tags, Attributes (flexible JSON), CreatedDate, ModifiedDate, IsDeleted
- **Asset**: Represents uploaded media files with properties: Id (GUID), WorldId (GUID), EntityId (GUID), FileName, ContentType, Size, BlobUrl, CreatedDate
- **DeletedWorldEntity**: Soft-deleted entities for potential recovery with properties: Original entity properties + DeletedDate, DeletedBy

### Repository Interfaces (Domain Layer)

- **IWorldRepository**: Methods for CRUD operations on worlds (GetByIdAsync, GetAllByOwnerAsync, CreateAsync, UpdateAsync, DeleteAsync)
- **IWorldEntityRepository**: Methods for CRUD operations on entities (GetByIdAsync, GetAllByWorldAsync, GetChildrenAsync, CreateAsync, UpdateAsync, DeleteAsync, SearchAsync)
- **ISearchService**: Abstracted search interface to enable future replacement with Azure AI Search (SearchEntitiesAsync with query parameter)
- **IAssetRepository**: Methods for asset management (GetByIdAsync, GetAllByEntityAsync, CreateAsync, UpdateAsync, DeleteAsync)

### Infrastructure Configuration

- **DbContext**: Configure Entity Framework Core with Cosmos DB provider, entity configurations, partition keys
- **Repository Implementations**: Concrete implementations of repository interfaces using EF Core
- **Aspire AppHost**: Configure `builder.AddProject<Api>()` for API service, `builder.AddCosmosDbEmulator()` for local Cosmos DB, service discovery and connection string injection
- **ServiceDefaults**: Configure telemetry (OpenTelemetry), health checks, resilience patterns (Polly) via extension method

## Success Criteria

### Measurable Outcomes

- **SC-001**: Developers can start the entire backend stack (API + Cosmos DB) with a single command and see all services healthy in Aspire Dashboard within 30 seconds
- **SC-002**: API endpoints respond to CRUD requests for worlds within 200ms for 95th percentile
- **SC-003**: API successfully creates and retrieves 100 worlds in under 5 seconds during load testing
- **SC-004**: All API endpoints return consistent JSON response format with proper error handling (100% coverage)
- **SC-005**: Repository pattern successfully abstracts Cosmos DB access, allowing unit tests to use in-memory providers
- **SC-006**: API enforces authorization rules, blocking unauthorized access attempts with 403 Forbidden (0% unauthorized access allowed)
- **SC-007**: Entity hierarchy queries (parent-child relationships) return correct results for nested structures up to 5 levels deep
- **SC-008**: Pagination works correctly for lists containing over 1000 entities, returning consistent cursors
- **SC-009**: Asset uploads complete successfully for files up to 25MB within 10 seconds
- **SC-010**: Search and filter operations return accurate results within 500ms for datasets containing 10,000+ entities

## Clarifications

### Session 2026-01-03

- Q: What are the specific validation rules for asset uploads (file size and type limits)? → A: Runtime-configurable with defaults of 25MB max file size; allowed types: images (jpg/jpeg/png/gif/webp), audio (mp3/wav/ogg), video (mp4/webm), documents (pdf/txt/md)
- Q: What should be the default number of items returned per page when no limit is specified? → A: Default 50 items per page, maximum 200 items per page
- Q: What fields should be searched and what type of matching should be used? → A: Case-insensitive partial matching (contains) on Name, Description, and Tags fields; implemented via abstracted ISearchService interface for future Azure AI Search integration
- Q: How much detail should validation error responses include? → A: Field-level validation errors including field name and specific error message for each validation failure
- Q: Should ETag/concurrency checks be required for all updates or optional? → A: Optional - ETag returned in GET responses, If-Match header validated when provided on PUT/PATCH, last-write-wins when not provided
