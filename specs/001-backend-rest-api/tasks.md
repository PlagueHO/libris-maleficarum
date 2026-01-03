# Tasks: Backend REST API with Cosmos DB Integration

**Input**: Design documents from `specs/001-backend-rest-api/`  
**Branch**: `001-backend-rest-api`  
**Generated**: 2026-01-03

**Prerequisites Checked**:
- ✅ plan.md (Technical Context, Constitution Check, Project Structure)
- ✅ spec.md (5 user stories with priorities, 25 functional requirements)
- ✅ research.md (9 technology decisions with code examples)
- ✅ data-model.md (4 Cosmos DB containers, entity schemas)
- ✅ contracts/ (OpenAPI 3.0 specs for worlds, entities, assets)
- ✅ quickstart.md (Developer onboarding guide)

## Format: `- [ ] [ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: User story label (US1=World Management, US2=Entity Management, US3=Asset Management, US4=Search, US5=Aspire)
- All paths relative to `libris-maleficarum-service/` directory

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Initialize solution structure and configure base dependencies

- [ ] T001 Verify .NET 10 SDK installed and LibrisMaleficarum.slnx solution loads correctly
- [ ] T002 [P] Add NuGet package Microsoft.EntityFrameworkCore.Cosmos v10.0.0 to Infrastructure project
- [ ] T003 [P] Add NuGet package FluentValidation.AspNetCore v11.3.0 to Api project
- [ ] T004 [P] Add NuGet package Aspire.Hosting.Azure.CosmosDB v13.1.0 to AppHost project
- [ ] T005 [P] Add NuGet package xUnit v2.9.0 to all test projects (Api.Tests, Domain.Tests, Infrastructure.Tests)
- [ ] T006 [P] Add NuGet package FluentAssertions v6.12.0 to all test projects
- [ ] T007 [P] Add NuGet package Moq v4.20.0 to test projects (Domain.Tests, Infrastructure.Tests)
- [ ] T008 [P] Add NuGet package Microsoft.AspNetCore.Mvc.Testing v10.0.0 to Api.Tests project

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST complete before ANY user story implementation can begin

**⚠️ CRITICAL**: No user story work can begin until this phase is 100% complete

### Domain Foundation

- [ ] T009 [P] Create EntityType enum in src/Domain/ValueObjects/EntityType.cs with 15 values (Character, Location, Campaign, Session, Faction, Item, Quest, Event, Continent, Country, Region, City, Building, Room, Other)
- [ ] T010 [P] Create IUserContextService interface in src/Domain/Interfaces/Services/IUserContextService.cs with GetCurrentUserIdAsync() method
- [ ] T011 [P] Create ApiResponse generic wrapper class in src/Api/Models/Responses/ApiResponse.cs with Data and Meta properties
- [ ] T012 [P] Create ErrorResponse class in src/Api/Models/Responses/ErrorResponse.cs with Error and Meta properties
- [ ] T013 [P] Create ValidationError class in src/Api/Models/Responses/ValidationError.cs with Field and Message properties

### Infrastructure Foundation

- [ ] T014 Create stubbed UserContextService in src/Infrastructure/Services/UserContextService.cs that returns hardcoded GUID (00000000-0000-0000-0000-000000000001)
- [ ] T015 Create ApplicationDbContext in src/Infrastructure/Persistence/ApplicationDbContext.cs with DbSet properties for Worlds, WorldEntities, Assets (configure Cosmos DB provider in OnConfiguring)
- [ ] T016 Configure ExceptionHandlingMiddleware in src/Api/Middleware/ExceptionHandlingMiddleware.cs to catch exceptions and return ErrorResponse with appropriate HTTP status codes
- [ ] T017 Register services in src/Api/Program.cs: AddDbContext with Cosmos DB connection string, AddScoped for IUserContextService, AddControllers, AddFluentValidation, AddEndpointsApiExplorer, AddSwaggerGen with XML comments
- [ ] T018 Configure middleware pipeline in src/Api/Program.cs: UseExceptionHandling, UseSwagger, UseSwaggerUI, UseHttpsRedirection, UseAuthorization, MapControllers

### Aspire Foundation (US5)

- [ ] T019 [US5] Create AppHost.cs in src/Orchestration/AppHost/AppHost.cs with builder.AddProject for Api service
- [ ] T020 [US5] Add builder.AddAzureCosmosDB("cosmosdb").RunAsEmulator() to AppHost.cs for local Cosmos DB Emulator
- [ ] T021 [US5] Configure service discovery in AppHost.cs: WithReference to connect Api to cosmosdb resource
- [ ] T022 [US5] Create Extensions.cs in src/Orchestration/ServiceDefaults/Extensions.cs with AddServiceDefaults() method (configure OpenTelemetry, health checks, resilience with Polly)
- [ ] T023 [US5] Call AddServiceDefaults() in Api Program.cs before other service registrations

**Checkpoint**: Foundation ready - all user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - World Management API (Priority: P1) 🎯 MVP

**Goal**: Enable frontend to create, read, update, and delete worlds via RESTful API

**Independent Test**: Make HTTP requests to `/api/v1/worlds` endpoints and verify CRUD operations return correct status codes and JSON responses

### Domain Layer (US1)

- [ ] T024 [P] [US1] Create World entity in src/Domain/Entities/World.cs with properties: Id (Guid), OwnerId (Guid), Name (string), Description (string?), CreatedDate (DateTime), ModifiedDate (DateTime), private constructor with validation
- [ ] T025 [P] [US1] Add Validate() method to World entity enforcing Name length 1-100 chars, Description max 2000 chars
- [ ] T026 [P] [US1] Add Update() method to World entity that accepts name, description, updates ModifiedDate
- [ ] T027 [P] [US1] Add SoftDelete() method to World entity (sets IsDeleted flag, updates ModifiedDate)
- [ ] T028 [P] [US1] Create IWorldRepository interface in src/Domain/Interfaces/Repositories/IWorldRepository.cs with methods: GetByIdAsync, GetAllByOwnerAsync, CreateAsync, UpdateAsync, DeleteAsync (soft delete)
- [ ] T029 [P] [US1] Create WorldNotFoundException in src/Domain/Exceptions/WorldNotFoundException.cs inheriting from Exception
- [ ] T030 [P] [US1] Create UnauthorizedWorldAccessException in src/Domain/Exceptions/UnauthorizedWorldAccessException.cs inheriting from Exception

### Infrastructure Layer (US1)

- [ ] T031 [US1] Create WorldConfiguration in src/Infrastructure/Persistence/Configurations/WorldConfiguration.cs implementing IEntityTypeConfiguration with ToContainer("Worlds"), HasPartitionKey(w => w.Id), HasNoDiscriminator()
- [ ] T032 [US1] Apply WorldConfiguration in ApplicationDbContext OnModelCreating method
- [ ] T033 [US1] Create WorldRepository in src/Infrastructure/Repositories/WorldRepository.cs implementing IWorldRepository with EF Core Cosmos provider queries using partition key
- [ ] T034 [US1] Implement GetByIdAsync in WorldRepository with OwnerId authorization check (throw UnauthorizedWorldAccessException if mismatch)
- [ ] T035 [US1] Implement GetAllByOwnerAsync in WorldRepository with cursor-based pagination (default 50, max 200 items)
- [ ] T036 [US1] Implement CreateAsync in WorldRepository setting OwnerId from IUserContextService, CreatedDate, ModifiedDate
- [ ] T037 [US1] Implement UpdateAsync in WorldRepository with ETag validation (If-Match header support, 409 Conflict on mismatch)
- [ ] T038 [US1] Implement DeleteAsync in WorldRepository calling entity.SoftDelete() instead of hard delete

### API Layer (US1)

- [ ] T039 [P] [US1] Create CreateWorldRequest DTO in src/Api/Models/Requests/CreateWorldRequest.cs with Name (required, 1-100 chars), Description (optional, max 2000 chars)
- [ ] T040 [P] [US1] Create UpdateWorldRequest DTO in src/Api/Models/Requests/UpdateWorldRequest.cs with same validation as CreateWorldRequest
- [ ] T041 [P] [US1] Create WorldResponse DTO in src/Api/Models/Responses/WorldResponse.cs with Id, OwnerId, Name, Description, CreatedDate, ModifiedDate
- [ ] T042 [P] [US1] Create CreateWorldRequestValidator in src/Api/Validators/CreateWorldRequestValidator.cs using FluentValidation with RuleFor Name and Description
- [ ] T043 [P] [US1] Create UpdateWorldRequestValidator in src/Api/Validators/UpdateWorldRequestValidator.cs using FluentValidation
- [ ] T044 [US1] Create WorldsController in src/Api/Controllers/WorldsController.cs with constructor injection of IWorldRepository and IUserContextService
- [ ] T045 [US1] Implement POST /api/v1/worlds endpoint in WorldsController returning 201 Created with Location header and ETag header
- [ ] T046 [US1] Implement GET /api/v1/worlds endpoint in WorldsController with limit and cursor query parameters, returning PaginatedApiResponse
- [ ] T047 [US1] Implement GET /api/v1/worlds/{worldId} endpoint in WorldsController returning 200 OK with ETag header
- [ ] T048 [US1] Implement PUT /api/v1/worlds/{worldId} endpoint in WorldsController with If-Match header validation, returning 200 OK with new ETag
- [ ] T049 [US1] Implement DELETE /api/v1/worlds/{worldId} endpoint in WorldsController returning 204 No Content on successful soft-delete

### Tests (US1)

- [ ] T050 [P] [US1] Create WorldTests.cs in tests/Domain.Tests/Entities/WorldTests.cs with AAA pattern tests for Validate(), Update(), SoftDelete() methods
- [ ] T051 [P] [US1] Create WorldRepositoryTests.cs in tests/Infrastructure.Tests/Repositories/WorldRepositoryTests.cs with in-memory Cosmos provider, test CRUD operations
- [ ] T052 [P] [US1] Create WorldsControllerTests.cs in tests/Api.Tests/Controllers/WorldsControllerTests.cs with WebApplicationFactory, test all endpoints return correct status codes
- [ ] T053 [US1] Add integration test in WorldsControllerTests.cs: POST creates world, GET retrieves it, PUT updates it, DELETE soft-deletes it (full lifecycle)
- [ ] T054 [US1] Add authorization test in WorldsControllerTests.cs: GET world owned by different user returns 403 Forbidden
- [ ] T055 [US1] Add validation test in WorldsControllerTests.cs: POST with Name exceeding 100 chars returns 400 Bad Request with field-level error

**Checkpoint**: User Story 1 complete - World Management API fully functional and independently testable

---

## Phase 4: User Story 2 - Entity Management API (Priority: P2)

**Goal**: Enable frontend to create and manage entities (locations, characters, campaigns) within worlds

**Independent Test**: Create world via US1 API, then make HTTP requests to `/api/v1/worlds/{worldId}/entities` to verify entity CRUD operations and hierarchical relationships

### Domain Layer (US2)

- [ ] T056 [P] [US2] Create WorldEntity entity in src/Domain/Entities/WorldEntity.cs with properties: Id (Guid), WorldId (Guid), ParentId (Guid?), Name (string), Description (string?), EntityType (EntityType enum), Tags (List<string>), Attributes (string for JSON), CreatedDate, ModifiedDate, private constructor
- [ ] T057 [P] [US2] Add Validate() method to WorldEntity enforcing Name 1-200 chars, Description max 5000 chars, Tags max 20 items (each max 50 chars), Attributes max 100KB serialized
- [ ] T058 [P] [US2] Add Update() method to WorldEntity accepting name, description, entityType, parentId, tags, attributes
- [ ] T059 [P] [US2] Add SoftDelete() method to WorldEntity
- [ ] T060 [P] [US2] Create IWorldEntityRepository interface in src/Domain/Interfaces/Repositories/IWorldEntityRepository.cs with methods: GetByIdAsync, GetAllByWorldAsync, GetChildrenAsync, CreateAsync, UpdateAsync, DeleteAsync, SearchAsync
- [ ] T061 [P] [US2] Create ISearchService interface in src/Domain/Interfaces/Services/ISearchService.cs with SearchEntitiesAsync method accepting query parameter
- [ ] T062 [P] [US2] Create EntityNotFoundException in src/Domain/Exceptions/EntityNotFoundException.cs

### Infrastructure Layer (US2)

- [ ] T063 [US2] Create WorldEntityConfiguration in src/Infrastructure/Persistence/Configurations/WorldEntityConfiguration.cs with ToContainer("WorldEntities"), HasPartitionKey(e => e.WorldId)
- [ ] T064 [US2] Apply WorldEntityConfiguration in ApplicationDbContext OnModelCreating, add DbSet<WorldEntity> property
- [ ] T065 [US2] Create WorldEntityRepository in src/Infrastructure/Repositories/WorldEntityRepository.cs implementing IWorldEntityRepository
- [ ] T066 [US2] Implement GetByIdAsync in WorldEntityRepository with partition key (WorldId) and authorization check via world ownership
- [ ] T067 [US2] Implement GetAllByWorldAsync in WorldEntityRepository with filtering by EntityType and Tags (case-insensitive partial match), cursor pagination
- [ ] T068 [US2] Implement GetChildrenAsync in WorldEntityRepository querying by ParentId within WorldId partition
- [ ] T069 [US2] Implement CreateAsync in WorldEntityRepository validating WorldId exists and user owns world
- [ ] T070 [US2] Implement UpdateAsync in WorldEntityRepository with ETag validation, preventing circular references on ParentId changes
- [ ] T071 [US2] Implement DeleteAsync in WorldEntityRepository with default behavior: return 400 if entity has children (query GetChildrenAsync), unless cascade=true query parameter provided
- [ ] T072 [US2] Implement cascade delete logic in WorldEntityRepository: recursively soft-delete all descendants when cascade=true
- [ ] T073 [US2] Create SearchService in src/Infrastructure/Services/SearchService.cs implementing ISearchService with LINQ case-insensitive Contains queries on Name, Description, Tags
- [ ] T074 [US2] Register ISearchService and IWorldEntityRepository in Api Program.cs AddScoped

### API Layer (US2)

- [ ] T075 [P] [US2] Create CreateEntityRequest DTO in src/Api/Models/Requests/CreateEntityRequest.cs with Name, Description, EntityType, ParentId, Tags, Attributes
- [ ] T076 [P] [US2] Create UpdateEntityRequest DTO in src/Api/Models/Requests/UpdateEntityRequest.cs with same properties as CreateEntityRequest
- [ ] T077 [P] [US2] Create PatchEntityRequest DTO in src/Api/Models/Requests/PatchEntityRequest.cs with all properties optional (for partial updates)
- [ ] T078 [P] [US2] Create MoveEntityRequest DTO in src/Api/Models/Requests/MoveEntityRequest.cs with NewParentId property
- [ ] T079 [P] [US2] Create EntityResponse DTO in src/Api/Models/Responses/EntityResponse.cs with all WorldEntity properties
- [ ] T080 [P] [US2] Create CreateEntityRequestValidator in src/Api/Validators/CreateEntityRequestValidator.cs validating Name length, EntityType enum, Tags count/length, Attributes JSON size
- [ ] T081 [US2] Create EntitiesController in src/Api/Controllers/EntitiesController.cs with constructor injection of IWorldEntityRepository, ISearchService, IWorldRepository
- [ ] T082 [US2] Implement POST /api/v1/worlds/{worldId}/entities endpoint in EntitiesController returning 201 Created with Location and ETag headers
- [ ] T083 [US2] Implement GET /api/v1/worlds/{worldId}/entities endpoint in EntitiesController with type and tags query parameters for filtering, pagination
- [ ] T084 [US2] Implement GET /api/v1/worlds/{worldId}/entities/{entityId} endpoint returning 200 OK with ETag
- [ ] T085 [US2] Implement PUT /api/v1/worlds/{worldId}/entities/{entityId} endpoint with If-Match header validation
- [ ] T086 [US2] Implement PATCH /api/v1/worlds/{worldId}/entities/{entityId} endpoint for partial updates (merge Attributes, replace Tags if provided)
- [ ] T087 [US2] Implement DELETE /api/v1/worlds/{worldId}/entities/{entityId} endpoint with cascade query parameter support
- [ ] T088 [US2] Implement GET /api/v1/worlds/{worldId}/entities/{parentId}/children endpoint returning child entities
- [ ] T089 [US2] Implement POST /api/v1/worlds/{worldId}/entities/{entityId}/move endpoint accepting MoveEntityRequest

### Tests (US2)

- [ ] T090 [P] [US2] Create WorldEntityTests.cs in tests/Domain.Tests/Entities/WorldEntityTests.cs testing Validate(), Update(), SoftDelete(), Tags/Attributes limits
- [ ] T091 [P] [US2] Create WorldEntityRepositoryTests.cs in tests/Infrastructure.Tests/Repositories/WorldEntityRepositoryTests.cs testing CRUD, filtering, hierarchy, cascade delete
- [ ] T092 [P] [US2] Create SearchServiceTests.cs in tests/Infrastructure.Tests/Services/SearchServiceTests.cs testing case-insensitive partial matching on Name/Description/Tags
- [ ] T093 [P] [US2] Create EntitiesControllerTests.cs in tests/Api.Tests/Controllers/EntitiesControllerTests.cs testing all endpoints
- [ ] T094 [US2] Add integration test: Create world, create parent entity, create child entity, verify GET children returns correct results
- [ ] T095 [US2] Add integration test: Create entity with children, DELETE without cascade returns 400, DELETE with cascade=true succeeds
- [ ] T096 [US2] Add integration test: Filter entities by EntityType=Character returns only Character entities
- [ ] T097 [US2] Add integration test: Move entity to new parent, verify ParentId updated, no circular reference allowed

**Checkpoint**: User Stories 1 AND 2 complete - World and Entity Management APIs fully functional independently

---

## Phase 5: User Story 3 - Asset Management API (Priority: P3)

**Goal**: Enable frontend to upload and manage assets (images, audio, documents) attached to entities

**Independent Test**: Create world and entity via US1/US2 APIs, then upload file to `/api/v1/worlds/{worldId}/entities/{entityId}/assets` and verify upload, retrieval, download, deletion

### Domain Layer (US3)

- [ ] T098 [P] [US3] Create Asset entity in src/Domain/Entities/Asset.cs with properties: Id (Guid), WorldId (Guid), EntityId (Guid), FileName (string), ContentType (string), SizeBytes (long), BlobUrl (string), CreatedDate, private constructor
- [ ] T099 [P] [US3] Add Validate() method to Asset enforcing FileName max 255 chars, SizeBytes <= configurable limit (default 25MB), ContentType in allowed list
- [ ] T100 [P] [US3] Create IAssetRepository interface in src/Domain/Interfaces/Repositories/IAssetRepository.cs with methods: GetByIdAsync, GetAllByEntityAsync, CreateAsync, DeleteAsync
- [ ] T101 [P] [US3] Create IBlobStorageService interface in src/Domain/Interfaces/Services/IBlobStorageService.cs with methods: UploadAsync, GetSasUriAsync, DeleteAsync

### Infrastructure Layer (US3)

- [ ] T102 [US3] Create AssetConfiguration in src/Infrastructure/Persistence/Configurations/AssetConfiguration.cs with ToContainer("Assets"), HasPartitionKey(a => a.WorldId)
- [ ] T103 [US3] Apply AssetConfiguration in ApplicationDbContext, add DbSet<Asset> property
- [ ] T104 [US3] Add NuGet package Azure.Storage.Blobs v12.22.0 to Infrastructure project
- [ ] T105 [US3] Create BlobStorageService in src/Infrastructure/Services/BlobStorageService.cs implementing IBlobStorageService using Azure.Storage.Blobs SDK
- [ ] T106 [US3] Implement UploadAsync in BlobStorageService accepting Stream and metadata, uploading to Azure Blob Storage container "assets"
- [ ] T107 [US3] Implement GetSasUriAsync in BlobStorageService generating 15-minute read-only SAS token for blob download
- [ ] T108 [US3] Implement DeleteAsync in BlobStorageService removing blob from storage
- [ ] T109 [US3] Create AssetRepository in src/Infrastructure/Repositories/AssetRepository.cs implementing IAssetRepository
- [ ] T110 [US3] Implement GetByIdAsync in AssetRepository with authorization check via world ownership
- [ ] T111 [US3] Implement GetAllByEntityAsync in AssetRepository with pagination, querying by EntityId within WorldId partition
- [ ] T112 [US3] Implement CreateAsync in AssetRepository: validate file, call BlobStorageService.UploadAsync, store metadata in Cosmos DB
- [ ] T113 [US3] Implement DeleteAsync in AssetRepository: delete from Cosmos DB, call BlobStorageService.DeleteAsync
- [ ] T114 [US3] Register IBlobStorageService and IAssetRepository in Api Program.cs AddScoped

### API Layer (US3)

- [ ] T115 [P] [US3] Create AssetResponse DTO in src/Api/Models/Responses/AssetResponse.cs with Id, WorldId, EntityId, FileName, ContentType, SizeBytes, BlobUrl, CreatedDate
- [ ] T116 [P] [US3] Create AssetDownloadResponse DTO in src/Api/Models/Responses/AssetDownloadResponse.cs with DownloadUrl (SAS URL), ExpiresAt, FileName, ContentType, SizeBytes
- [ ] T117 [US3] Create AssetsController in src/Api/Controllers/AssetsController.cs with constructor injection of IAssetRepository, IBlobStorageService, IWorldEntityRepository
- [ ] T118 [US3] Implement GET /api/v1/worlds/{worldId}/entities/{entityId}/assets endpoint in AssetsController with pagination
- [ ] T119 [US3] Implement POST /api/v1/worlds/{worldId}/entities/{entityId}/assets endpoint accepting multipart/form-data file upload, validate type/size, return 201 Created with Location header
- [ ] T120 [US3] Implement GET /api/v1/worlds/{worldId}/assets/{assetId} endpoint returning asset metadata (not binary)
- [ ] T121 [US3] Implement GET /api/v1/worlds/{worldId}/assets/{assetId}/download endpoint generating SAS URL and returning AssetDownloadResponse
- [ ] T122 [US3] Implement DELETE /api/v1/worlds/{worldId}/assets/{assetId} endpoint returning 204 No Content
- [ ] T123 [US3] Add validation in POST endpoint: return 400 Bad Request with FILE_TOO_LARGE error code if file exceeds limit
- [ ] T124 [US3] Add validation in POST endpoint: return 400 Bad Request with UNSUPPORTED_FILE_TYPE error code if ContentType not in allowed list (jpg/jpeg/png/gif/webp for images, mp3/wav/ogg for audio, mp4/webm for video, pdf/txt/md for documents)

### Tests (US3)

- [ ] T125 [P] [US3] Create AssetTests.cs in tests/Domain.Tests/Entities/AssetTests.cs testing Validate() method
- [ ] T126 [P] [US3] Create AssetRepositoryTests.cs in tests/Infrastructure.Tests/Repositories/AssetRepositoryTests.cs with mocked IBlobStorageService
- [ ] T127 [P] [US3] Create BlobStorageServiceTests.cs in tests/Infrastructure.Tests/Services/BlobStorageServiceTests.cs (integration test with Azure Storage Emulator)
- [ ] T128 [P] [US3] Create AssetsControllerTests.cs in tests/Api.Tests/Controllers/AssetsControllerTests.cs testing upload/download/delete endpoints
- [ ] T129 [US3] Add integration test: Upload PNG file, verify metadata stored, download via SAS URL succeeds
- [ ] T130 [US3] Add integration test: Upload file exceeding size limit returns 400 with FILE_TOO_LARGE error
- [ ] T131 [US3] Add integration test: Upload unsupported file type returns 400 with UNSUPPORTED_FILE_TYPE error
- [ ] T132 [US3] Add integration test: Delete asset removes metadata from Cosmos DB and blob from storage

**Checkpoint**: User Stories 1, 2, AND 3 complete - Full CRUD for Worlds, Entities, and Assets

---

## Phase 6: User Story 4 - Search and Filter API (Priority: P3)

**Goal**: Enable frontend to search and filter entities within a world

**Independent Test**: Create world with multiple entities via US1/US2 APIs, use `/api/v1/worlds/{worldId}/search` with query parameters to verify search returns correct results

### Infrastructure Layer (US4)

- [ ] T133 [US4] Enhance SearchService in src/Infrastructure/Services/SearchService.cs to support combined queries (Name OR Description OR Tags matching search term)
- [ ] T134 [US4] Add sorting support to SearchService: sort by Name, CreatedDate, or ModifiedDate (ascending/descending)
- [ ] T135 [US4] Add pagination to SearchService matching existing cursor-based implementation (default 50, max 200)

### API Layer (US4)

- [ ] T136 [US4] Add GET /api/v1/worlds/{worldId}/search endpoint in EntitiesController with query parameters: q (search term), sortBy (name/createdDate/modifiedDate), sortOrder (asc/desc)
- [ ] T137 [US4] Implement search endpoint calling ISearchService.SearchEntitiesAsync and returning PaginatedApiResponse

### Tests (US4)

- [ ] T138 [P] [US4] Add search tests to SearchServiceTests.cs: verify case-insensitive matching, verify sorting, verify pagination
- [ ] T139 [P] [US4] Add search tests to EntitiesControllerTests.cs: verify query parameter handling, verify results match search term
- [ ] T140 [US4] Add integration test: Create 3 entities with different names, search for partial name match, verify only matching entities returned
- [ ] T141 [US4] Add integration test: Create entities with tags, search for tag, verify results
- [ ] T142 [US4] Add integration test: Search with sortBy=createdDate&sortOrder=desc, verify newest entities first

**Checkpoint**: User Stories 1-4 complete - Full search and filter functionality operational

---

## Phase 7: User Story 5 - Aspire Integration (Priority: P1) 🎯 MVP

**Goal**: Enable developers to run entire backend stack locally with single command

**Independent Test**: Run `dotnet run --project src/Orchestration/AppHost` and verify (1) Cosmos DB Emulator starts, (2) API service starts, (3) Aspire Dashboard shows all services healthy, (4) API can create/retrieve worlds

**NOTE**: Foundation tasks T019-T023 already completed Aspire setup. This phase adds final validation and documentation.

### Documentation (US5)

- [ ] T143 [US5] Verify quickstart.md instructions in specs/001-backend-rest-api/quickstart.md are accurate for Aspire startup
- [ ] T144 [US5] Update README.md in libris-maleficarum-service/ with Getting Started section referencing quickstart.md

### Tests (US5)

- [ ] T145 [US5] Create AppHostTests.cs in tests/Api.Tests/Aspire/AppHostTests.cs verifying Aspire application model defines cosmosdb and api resources
- [ ] T146 [US5] Add manual test scenario in quickstart.md: Run AppHost, navigate to Dashboard, verify services green, make API request, verify data persists

**Checkpoint**: All P1 user stories (US1, US5) complete - MVP functional with single-command developer experience

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Improvements affecting multiple user stories and final validation

### Documentation

- [ ] T147 [P] Update OpenAPI XML comments in all controllers (WorldsController, EntitiesController, AssetsController) for Swagger documentation
- [ ] T148 [P] Verify all OpenAPI specs in contracts/ match implemented controller endpoints
- [ ] T149 [P] Add architecture diagram to plan.md showing Api → Domain ← Infrastructure dependencies

### Code Quality

- [ ] T150 [P] Run `dotnet format LibrisMaleficarum.slnx` to ensure consistent code formatting
- [ ] T151 [P] Run `dotnet build LibrisMaleficarum.slnx` and verify no compiler warnings
- [ ] T152 Run code analysis and address any security or performance warnings

### Testing

- [ ] T153 [P] Run all tests with `dotnet test LibrisMaleficarum.slnx` and verify 100% pass rate
- [ ] T154 [P] Generate code coverage report and verify >80% coverage for Domain and Infrastructure layers
- [ ] T155 Add end-to-end integration test: Create world → Create entity → Upload asset → Search entities → Verify all data correct

### Performance

- [ ] T156 Add performance test: Create 100 worlds and measure p95 response time (<200ms per SC-002)
- [ ] T157 Add performance test: Create 1000 entities in single world and verify pagination performance

### Validation

- [ ] T158 Run through quickstart.md validation checklist: Clone repo, start Aspire, test API endpoints, verify all steps work
- [ ] T159 Verify all 25 functional requirements (FR-001 to FR-025) are implemented and tested
- [ ] T160 Verify all 10 success criteria (SC-001 to SC-010) are met and measurable

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - **BLOCKS all user stories**
- **User Stories (Phases 3-7)**: All depend on Foundational phase completion
  - US1 (World Management) - No dependencies on other stories
  - US2 (Entity Management) - No dependencies on other stories (can run parallel to US1)
  - US3 (Asset Management) - No dependencies on other stories (can run parallel to US1/US2)
  - US4 (Search) - Enhances US2 but independently testable
  - US5 (Aspire) - Foundation already in Phase 2, final validation independent
- **Polish (Phase 8)**: Depends on all desired user stories being complete

### User Story Completion Order (Recommended for MVP)

1. **Foundational (Phase 2)** - MUST complete first → ~8-10 hours
2. **US1 (Phase 3)** - World Management → MVP Core → ~12-16 hours
3. **US5 (Phase 7)** - Aspire Validation → Developer Experience → ~2-4 hours
4. **Deploy MVP** - Deliver P1 functionality for testing
5. **US2 (Phase 4)** - Entity Management → ~16-20 hours
6. **US3 (Phase 5)** - Asset Management → ~12-16 hours
7. **US4 (Phase 6)** - Search → ~6-8 hours
8. **Polish (Phase 8)** - Final validation → ~6-8 hours

### Parallel Opportunities

**Setup Phase (All in parallel)**:
- T002-T008: All NuGet package additions (different projects)

**Foundational Phase (Parallelizable groups)**:
- T009-T013: Domain/Api foundation classes (different files)
- T014, T022: Service implementations (different files)

**Within Each User Story (Parallelizable groups)**:

**US1 Parallel Tasks**:
- T024-T030: Domain classes (World entity, repository interface, exceptions)
- T039-T043: API DTOs and validators
- T050-T052: Test files for different layers

**US2 Parallel Tasks**:
- T056-T062: Domain classes (WorldEntity entity, repository interfaces, exceptions)
- T075-T080: API DTOs and validators
- T090-T093: Test files for different layers

**US3 Parallel Tasks**:
- T098-T101: Domain classes (Asset entity, repository/service interfaces)
- T115-T116: API DTOs
- T125-T128: Test files

**US4 Parallel Tasks**:
- T138-T139: Test files for service and controller

**Polish Phase (All in parallel)**:
- T147-T149: Documentation updates
- T150-T151: Code formatting and build verification
- T153-T154: Test execution and coverage

### Within User Story Task Sequencing

**US1 Example Sequential Flow**:
1. Domain entities first (T024-T027) → Tests can be written (T050)
2. Repository interface (T028) → Repository implementation (T033-T038) → Tests (T051)
3. DTOs (T039-T041) → Validators (T042-T043)
4. Controller (T044) → Endpoints (T045-T049) → Tests (T052-T055)

**Critical Path**: Foundational → US1 Domain → US1 Infrastructure → US1 API → US1 Tests

---

## Parallel Example: Foundational Phase

```bash
# After T001-T008 complete, these can all start simultaneously:

# Terminal 1: Domain foundation
Task T009: Create EntityType enum
Task T010: Create IUserContextService interface
Task T012: Create ErrorResponse class
Task T013: Create ValidationError class

# Terminal 2: Infrastructure foundation
Task T014: Create UserContextService implementation
Task T015: Create ApplicationDbContext

# Terminal 3: API foundation
Task T011: Create ApiResponse wrapper
Task T016: Create ExceptionHandlingMiddleware

# Terminal 4: Aspire foundation (US5)
Task T019: Create AppHost.cs
Task T020: Add Cosmos DB Emulator
Task T022: Create ServiceDefaults Extensions
```

---

## Parallel Example: User Story 1

```bash
# After Foundational complete, these can start in parallel:

# Terminal 1: Domain layer
Task T024: Create World entity
Task T025: Add Validate() method
Task T026: Add Update() method
Task T027: Add SoftDelete() method

# Terminal 2: Repository interface and exceptions
Task T028: Create IWorldRepository interface
Task T029: Create WorldNotFoundException
Task T030: Create UnauthorizedWorldAccessException

# Terminal 3: API DTOs
Task T039: Create CreateWorldRequest
Task T040: Create UpdateWorldRequest
Task T041: Create WorldResponse

# Terminal 4: Validators
Task T042: Create CreateWorldRequestValidator
Task T043: Create UpdateWorldRequestValidator

# Then sequential: T031-T038 (Infrastructure), T044-T049 (API endpoints), T050-T055 (Tests)
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 5 Only)

**Estimated Time**: 20-30 hours total

1. Complete Phase 1: Setup (2 hours)
2. Complete Phase 2: Foundational (8-10 hours) - **CRITICAL BLOCKER**
3. Complete Phase 3: User Story 1 - World Management (12-16 hours)
4. Complete Phase 7: User Story 5 - Aspire Validation (2-4 hours)
5. **STOP and VALIDATE**: Test independently, verify quickstart.md works
6. Deploy/demo MVP

**MVP Delivers**:
- ✅ World CRUD operations via REST API
- ✅ Single-command developer startup (`dotnet run --project AppHost`)
- ✅ Aspire Dashboard for observability
- ✅ Cosmos DB Emulator integration
- ✅ Complete test coverage for core functionality

### Incremental Delivery (All User Stories)

**Estimated Time**: 60-80 hours total

1. Setup + Foundational → Foundation ready (10-12 hours)
2. Add User Story 1 → Test independently → **Deploy MVP** (12-16 hours)
3. Add User Story 5 → Validate Aspire → **Demo developer experience** (2-4 hours)
4. Add User Story 2 → Test independently → Deploy (16-20 hours)
5. Add User Story 3 → Test independently → Deploy (12-16 hours)
6. Add User Story 4 → Test independently → Deploy (6-8 hours)
7. Polish → Final validation → **Production release** (6-8 hours)

Each increment adds value without breaking previous functionality.

### Parallel Team Strategy (3 Developers)

With 3 developers working simultaneously:

**Week 1**:
- **All team**: Complete Setup + Foundational together (2-3 days)
- **Developer A**: User Story 1 - World Management (3-4 days)
- **Developer B**: User Story 2 - Entity Management (starts after Foundation) (4-5 days)
- **Developer C**: User Story 5 - Aspire final validation (starts after Foundation) (1 day)

**Week 2**:
- **Developer A**: User Story 3 - Asset Management (3-4 days)
- **Developer B**: User Story 4 - Search (2 days) + Polish documentation (1 day)
- **Developer C**: Polish testing, performance validation, end-to-end integration (3-4 days)

**Total Elapsed Time**: ~10 working days (vs 12-16 days sequential)

---

## Notes

- **[P] tasks**: Different files, can run simultaneously without conflicts
- **[Story] labels**: Map tasks to user stories from spec.md for traceability
- **ETag support**: Optional concurrency control per FR-023, FR-024, FR-025 (last-write-wins if not provided)
- **Soft delete**: All delete operations use soft delete (IsDeleted flag) per FR-012
- **Pagination**: Cursor-based with default 50, max 200 items per FR-013, FR-013a
- **Authorization**: All repository operations check OwnerId matches current user from IUserContextService per FR-010
- **Validation**: FluentValidation for request DTOs, domain entity Validate() methods for business rules
- **Tests**: AAA pattern (Arrange, Act, Assert) with FluentAssertions per research.md
- **Aspire**: Foundation tasks (T019-T023) enable single-command startup, user story 5 validates experience

### Task Verification

- Verify tests **fail first** before implementing functionality (TDD red-green-refactor)
- Commit after each task or logical group of related tasks
- Stop at checkpoints to validate user story independently before proceeding
- Run `dotnet test` frequently to ensure no regressions

### Common Pitfalls to Avoid

- ❌ Skipping Foundational phase → All user stories will fail
- ❌ Working on multiple user stories before foundation complete → Blocked dependencies
- ❌ Not testing user stories independently → Integration issues missed
- ❌ Implementing without failing tests first → Not following TDD
- ❌ Hardcoding connection strings → Use Aspire service discovery
- ❌ Forgetting partition keys in Cosmos queries → Poor performance
- ❌ Not validating OwnerId in repositories → Security vulnerability

### Success Indicators

- ✅ All tests pass: `dotnet test LibrisMaleficarum.slnx`
- ✅ Single command starts everything: `dotnet run --project src/Orchestration/AppHost`
- ✅ Aspire Dashboard shows all services healthy
- ✅ Can create world, add entities, upload assets via API
- ✅ All 25 functional requirements implemented
- ✅ All 10 success criteria measurable and passing
- ✅ quickstart.md instructions validated end-to-end
