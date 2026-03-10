# Tasks: World Data Importer

**Input**: Design documents from `/specs/016-world-data-importer/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/import-api-contract.md, quickstart.md

**Tests**: Included — constitution mandates TDD (Principle III), and plan.md specifies MSTest + FluentAssertions + NSubstitute for all three new projects.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

- **Service code**: `libris-maleficarum-service/src/`
- **Tests**: `libris-maleficarum-service/tests/`
- **Sample data**: `samples/worlds/grimhollow/`

---

## Phase 1: Setup (Project Initialization)

**Purpose**: Create the three new projects, test projects, update solution file, and establish project structure.

- [ ] T001 Create API client SDK project file at `libris-maleficarum-service/src/Client/Api/LibrisMaleficarum.Api.Client.csproj` with net10.0 TFM, Microsoft.Extensions.Http.Resilience and Microsoft.Extensions.DependencyInjection.Abstractions package references, and Domain project reference (per plan.md Project File Specifications)
- [ ] T002 Create import library project file at `libris-maleficarum-service/src/Components/Import/LibrisMaleficarum.Import.csproj` with net10.0 TFM, Microsoft.Extensions.DependencyInjection.Abstractions package reference, and Api.Client + Domain project references (per plan.md)
- [ ] T003 Create CLI tool project file at `libris-maleficarum-service/src/Tools/Cli/LibrisMaleficarum.Cli.csproj` with net10.0 TFM, OutputType Exe, AssemblyName `libris`, System.CommandLine package reference, and Import project reference (per plan.md)
- [ ] T004 [P] Create API client test project file at `libris-maleficarum-service/tests/unit/Api.Client.Tests/LibrisMaleficarum.Api.Client.Tests.csproj` with MSTest SDK, FluentAssertions, NSubstitute, and Api.Client project reference
- [ ] T005 [P] Create import library test project file at `libris-maleficarum-service/tests/unit/Import.Tests/LibrisMaleficarum.Import.Tests.csproj` with MSTest SDK, FluentAssertions, NSubstitute, and Import project reference
- [ ] T006 [P] Create CLI test project file at `libris-maleficarum-service/tests/unit/Cli.Tests/LibrisMaleficarum.Cli.Tests.csproj` with MSTest SDK, FluentAssertions, NSubstitute, and Cli project reference
- [ ] T007 Update solution file at `libris-maleficarum-service/LibrisMaleficarum.slnx` to add all six new projects with appropriate solution folder structure (`/src/Client/Api/`, `/src/Components/Import/`, `/src/Tools/Cli/`, `/tests/unit/Api.Client.Tests/`, `/tests/unit/Import.Tests/`, `/tests/unit/Cli.Tests/`)
- [ ] T008 Verify solution builds successfully by running `dotnet build LibrisMaleficarum.slnx` from `libris-maleficarum-service/`

**Checkpoint**: All projects compile, solution structure matches plan.md.

---

## Phase 2: Foundational (API Client SDK — Blocking Prerequisite)

**Purpose**: Implement the `LibrisMaleficarum.Api.Client` SDK that the Import library depends on. This MUST be complete before any user story work begins.

**⚠️ CRITICAL**: The Import library consumes `ILibrisApiClient`; all user stories are blocked until the SDK is functional and tested.

### API Client SDK Models

- [ ] T009 [P] Create `ApiResponse<T>` generic wrapper model at `libris-maleficarum-service/src/Client/Api/Models/ApiResponse.cs` with `Data` property matching the API response envelope `{ "data": ... }`
- [ ] T010 [P] Create `ApiError` model at `libris-maleficarum-service/src/Client/Api/Models/ApiError.cs` with `Message`, `Code`, and `Details` properties for error responses
- [ ] T011 [P] Create `CreateWorldRequest` model at `libris-maleficarum-service/src/Client/Api/Models/CreateWorldRequest.cs` with `Name`, `Description`, `OwnerId` properties per contracts/import-api-contract.md
- [ ] T012 [P] Create `WorldResponse` model at `libris-maleficarum-service/src/Client/Api/Models/WorldResponse.cs` with `Id`, `OwnerId`, `Name`, `Description`, `CreatedDate`, `ModifiedDate` properties per contracts/import-api-contract.md
- [ ] T013 [P] Create `CreateEntityRequest` model at `libris-maleficarum-service/src/Client/Api/Models/CreateEntityRequest.cs` with `Name`, `Description`, `EntityType`, `ParentId`, `Tags`, `Attributes`, `OwnerId`, `SchemaVersion` properties per contracts/import-api-contract.md
- [ ] T014 [P] Create `EntityResponse` model at `libris-maleficarum-service/src/Client/Api/Models/EntityResponse.cs` with `Id`, `WorldId`, `ParentId`, `EntityType`, `Name`, `Description`, `Tags`, `Path`, `Depth`, `HasChildren`, `OwnerId`, `Attributes`, `CreatedDate`, `ModifiedDate`, `IsDeleted`, `SchemaVersion` properties per contracts/import-api-contract.md

### API Client SDK Exceptions

- [ ] T015 [P] Create `LibrisApiException` at `libris-maleficarum-service/src/Client/Api/Exceptions/LibrisApiException.cs` with `StatusCode`, `ErrorMessage`, and `ApiError` properties for non-success API responses
- [ ] T016 [P] Create `LibrisApiAuthenticationException` at `libris-maleficarum-service/src/Client/Api/Exceptions/LibrisApiAuthenticationException.cs` inheriting from `LibrisApiException` for 401/403 responses

### API Client SDK Interface and Implementation

- [ ] T017 Create `ILibrisApiClient` interface at `libris-maleficarum-service/src/Client/Api/ILibrisApiClient.cs` with `CreateWorldAsync(CreateWorldRequest, CancellationToken)` and `CreateEntityAsync(Guid worldId, CreateEntityRequest, CancellationToken)` methods returning typed responses (per data-model.md)
- [ ] T018 Create `LibrisApiClient` implementation at `libris-maleficarum-service/src/Client/Api/LibrisApiClient.cs` implementing `ILibrisApiClient` using `HttpClient` for POST requests to `/api/v1/worlds` and `/api/v1/worlds/{worldId}/entities`, with `System.Text.Json` deserialization, proper error handling (throw `LibrisApiException`/`LibrisApiAuthenticationException`), and `CancellationToken` propagation
- [ ] T019 Create `ServiceCollectionExtensions` at `libris-maleficarum-service/src/Client/Api/Extensions/ServiceCollectionExtensions.cs` with `AddLibrisApiClient(Action<LibrisApiClientOptions>)` extension method registering `ILibrisApiClient`/`LibrisApiClient` with `IHttpClientFactory`, configuring base URL, auth token header, and `Microsoft.Extensions.Http.Resilience` standard resilience handler

### API Client SDK Tests

- [ ] T020 [P] Create `LibrisApiClientTests` at `libris-maleficarum-service/tests/unit/Api.Client.Tests/LibrisApiClientTests.cs` with unit tests for: successful `CreateWorldAsync` (mock HttpMessageHandler returning 201), successful `CreateEntityAsync` (201), 400 response throws `LibrisApiException`, 401 response throws `LibrisApiAuthenticationException`, `CancellationToken` cancellation, JSON deserialization of response envelope
- [ ] T021 [P] Create `ServiceCollectionExtensionsTests` at `libris-maleficarum-service/tests/unit/Api.Client.Tests/ServiceCollectionExtensionsTests.cs` with tests verifying `AddLibrisApiClient` registers `ILibrisApiClient` in DI container with correct configuration
- [ ] T022 Verify API client SDK builds and all tests pass by running `dotnet test tests/unit/Api.Client.Tests/`

**Checkpoint**: API client SDK fully functional and tested. Import library can now consume `ILibrisApiClient`.

---

## Phase 3: User Story 1 — Import World from Folder (Priority: P1) 🎯 MVP

**Goal**: A developer can run `libris world import --source ./folder --api-url <url> --owner-id <id>` to import a complete world and all entities from a local folder of JSON files into the backend API.

**Independent Test**: Run the CLI against a sample folder containing `world.json` and entity JSON files, verify all entities created via API with correct parent-child relationships.

### Import Library Models (US1)

- [ ] T023 [P] [US1] Create `WorldImportDefinition` model at `libris-maleficarum-service/src/Components/Import/Models/WorldImportDefinition.cs` with `Name` (required) and `Description` (optional) properties per data-model.md
- [ ] T024 [P] [US1] Create `EntityImportDefinition` model at `libris-maleficarum-service/src/Components/Import/Models/EntityImportDefinition.cs` with `LocalId`, `EntityType`, `Name`, `Description`, `ParentLocalId`, `Tags`, `Properties`, and `SourceFilePath` (JsonIgnore) properties per data-model.md
- [ ] T025 [P] [US1] Create `ImportSourceContent` model and `ImportSourceType` enum at `libris-maleficarum-service/src/Components/Import/Models/ImportSourceContent.cs` with `World`, `Entities`, `ParseErrors`, `SourcePath`, `SourceType` properties per data-model.md
- [ ] T026 [P] [US1] Create `ResolvedEntity` model at `libris-maleficarum-service/src/Components/Import/Models/ResolvedEntity.cs` with `Definition`, `AssignedId`, `ResolvedParentId`, `Path`, `Depth`, `Children` properties per data-model.md
- [ ] T027 [P] [US1] Create `ImportManifest` model at `libris-maleficarum-service/src/Components/Import/Models/ImportManifest.cs` with `World`, `Entities`, `EntitiesByDepth`, `MaxDepth`, `TotalEntityCount`, `CountsByType` properties per data-model.md
- [ ] T028 [P] [US1] Create `ImportValidationResult` model at `libris-maleficarum-service/src/Components/Import/Models/ImportValidationResult.cs` with `IsValid` (computed), `Errors`, `Warnings`, `Manifest` properties per data-model.md
- [ ] T029 [P] [US1] Create `ImportValidationError` model at `libris-maleficarum-service/src/Components/Import/Models/ImportValidationError.cs` with `FilePath`, `Code`, `Message`, `LineNumber` properties per data-model.md
- [ ] T030 [P] [US1] Create `ImportValidationWarning` model at `libris-maleficarum-service/src/Components/Import/Models/ImportValidationWarning.cs` with `FilePath`, `Code`, `Message` properties per data-model.md
- [ ] T031 [P] [US1] Create `ImportResult` model at `libris-maleficarum-service/src/Components/Import/Models/ImportResult.cs` with `Success`, `WorldId`, `TotalEntitiesCreated`, `TotalEntitiesFailed`, `TotalEntitiesSkipped`, `CreatedByType`, `Errors`, `Duration` properties per data-model.md
- [ ] T032 [P] [US1] Create `EntityImportError` model at `libris-maleficarum-service/src/Components/Import/Models/EntityImportError.cs` with `LocalId`, `EntityName`, `ErrorMessage`, `FilePath`, `SkippedDescendantLocalIds` properties per data-model.md
- [ ] T033 [P] [US1] Create `ImportOptions` model at `libris-maleficarum-service/src/Components/Import/Models/ImportOptions.cs` with `ApiBaseUrl`, `OwnerId`, `AuthToken`, `MaxConcurrency` (default 10), `ValidateOnly`, `Verbose` properties per data-model.md
- [ ] T034 [P] [US1] Create `ImportProgress` model and `ImportPhase` enum at `libris-maleficarum-service/src/Components/Import/Models/ImportProgress.cs` with `TotalEntities`, `CompletedEntities`, `CurrentDepth`, `CurrentEntityName`, `Phase` properties per data-model.md
- [ ] T035 [P] [US1] Create `CreateEntityImportRequest` model at `libris-maleficarum-service/src/Components/Import/Models/CreateEntityImportRequest.cs` with `Name`, `Description`, `EntityType`, `ParentId`, `Tags`, `Attributes`, `OwnerId` properties per data-model.md
- [ ] T036 [P] [US1] Create `ImportValidationErrorCodes` constants class at `libris-maleficarum-service/src/Components/Import/Validation/ImportValidationErrorCodes.cs` with all error code constants from data-model.md validation error codes table

### Import Library Interfaces (US1)

- [ ] T037 [P] [US1] Create `IImportSourceReader` interface at `libris-maleficarum-service/src/Components/Import/Interfaces/IImportSourceReader.cs` with `ReadAsync(string sourcePath, CancellationToken)` returning `Task<ImportSourceContent>` per data-model.md
- [ ] T038 [P] [US1] Create `IImportValidator` interface at `libris-maleficarum-service/src/Components/Import/Interfaces/IImportValidator.cs` with `Validate(ImportSourceContent)` returning `ImportValidationResult` per data-model.md
- [ ] T039 [P] [US1] Create `IWorldImportService` interface at `libris-maleficarum-service/src/Components/Import/Interfaces/IWorldImportService.cs` with `ImportAsync(sourcePath, ImportOptions, IProgress<ImportProgress>?, CancellationToken)` and `ValidateAsync(sourcePath, CancellationToken)` methods per data-model.md

### Import Library Tests — Source Reader (US1)

- [ ] T040 [P] [US1] Create `ImportSourceReaderTests` at `libris-maleficarum-service/tests/unit/Import.Tests/Services/ImportSourceReaderTests.cs` with tests for: reads valid folder with world.json and entity files (FR-001), auto-detects folder source type (FR-003), ignores non-JSON files (FR-008), returns parse error when world.json missing (FR-004), returns parse error for malformed JSON entity files (FR-014), reads entity files from nested subfolders (FR-005), sets SourceFilePath on parsed entities for error reporting (FR-024)

### Import Library Implementation — Source Reader (US1)

- [ ] T041 [US1] Implement `ImportSourceReader` at `libris-maleficarum-service/src/Components/Import/Services/ImportSourceReader.cs` implementing `IImportSourceReader`: detect source type by path (folder vs. zip), for folder mode recursively enumerate `*.json` files, parse `world.json` at root as `WorldImportDefinition`, parse other JSON files as `EntityImportDefinition` with `System.Text.Json`, skip non-JSON files (FR-008), collect parse errors with file paths (FR-024), return `ImportSourceContent`

### Import Library Tests — Validator (US1)

- [ ] T042 [P] [US1] Create `ImportValidatorTests` at `libris-maleficarum-service/tests/unit/Import.Tests/Services/ImportValidatorTests.cs` with tests for: valid content produces valid manifest, missing world returns WORLD_MISSING error, entity missing localId returns ENTITY_MISSING_LOCAL_ID (FR-036), duplicate localId returns ENTITY_DUPLICATE_LOCAL_ID (FR-038), entity missing name returns ENTITY_MISSING_NAME, entity missing entityType returns ENTITY_MISSING_TYPE, invalid entityType returns ENTITY_INVALID_TYPE (FR-015), dangling parentLocalId returns ENTITY_DANGLING_PARENT (FR-016), cycle in parent references returns ENTITY_CYCLE_DETECTED (FR-016), name exceeding 200 chars returns ENTITY_NAME_TOO_LONG, description exceeding 5000 chars returns ENTITY_DESC_TOO_LONG, tags exceeding 20 items returns ENTITY_TOO_MANY_TAGS, tag exceeding 50 chars returns ENTITY_TAG_TOO_LONG, properties exceeding 100KB returns ENTITY_PROPS_TOO_LARGE, valid content builds correct EntitiesByDepth grouping, valid content assigns correct depth and path for multi-level hierarchy

### Import Library Implementation — Validator (US1)

- [ ] T043 [US1] Implement `ImportValidator` at `libris-maleficarum-service/src/Components/Import/Services/ImportValidator.cs` implementing `IImportValidator`: validate world presence and fields, validate each entity (localId uniqueness, required fields, entityType against Domain EntityType enum, field length limits, tag/property limits), validate parentLocalId references and detect cycles via depth-first traversal (FR-016), build `ImportManifest` with resolved hierarchy (assign GUIDs, compute depth/path, group by depth), return `ImportValidationResult` with errors, warnings, and manifest

### Import Library Tests — EntityImportDefinition (US1)

- [ ] T044 [P] [US1] Create `EntityImportDefinitionTests` at `libris-maleficarum-service/tests/unit/Import.Tests/Models/EntityImportDefinitionTests.cs` with tests for: deserialization from JSON with all fields, deserialization with optional fields null/missing, SourceFilePath is not serialized (JsonIgnore), camelCase JSON property names

### Import Library Tests — World Import Service (US1)

- [ ] T045 [P] [US1] Create `WorldImportServiceTests` at `libris-maleficarum-service/tests/unit/Import.Tests/Services/WorldImportServiceTests.cs` with tests for: successful import calls CreateWorldAsync then CreateEntityAsync in hierarchy order (FR-009/FR-010), assigns new GUIDs (FR-011), sets ownerId on all API calls (FR-012), computes correct path/depth/parentId (FR-013), parallel creation per depth level (FR-029), respects MaxConcurrency option (FR-030), failed entity skips descendants with error reporting (FR-027), reports progress through IProgress<ImportProgress>, returns ImportResult with correct counts and duration (FR-023), validate-only mode calls ValidateAsync without API calls (FR-017), API connection failure produces clear error (US1-AC4), cancellation token stops import

### Import Library Implementation — World Import Service (US1)

- [ ] T046 [US1] Implement `WorldImportService` at `libris-maleficarum-service/src/Components/Import/Services/WorldImportService.cs` implementing `IWorldImportService`: orchestrate read → validate → execute flow, for `ImportAsync`: call reader, call validator, if invalid return early, call `ILibrisApiClient.CreateWorldAsync` (FR-009), iterate EntitiesByDepth from depth 0 to MaxDepth, at each depth level create entities in parallel using `SemaphoreSlim` with `MaxConcurrency` (FR-029/FR-030), map `ResolvedEntity` to `CreateEntityRequest` with assigned IDs/path/depth/parentId (FR-013), on entity failure record error and collect descendant localIds to skip (FR-027), report progress via `IProgress<ImportProgress>`, return `ImportResult` with counts, errors, duration (FR-023). For `ValidateAsync`: call reader then validator, return `ImportValidationResult` (FR-017).

### Import Library DI Registration (US1)

- [ ] T047 [US1] Create `ServiceCollectionExtensions` at `libris-maleficarum-service/src/Components/Import/Extensions/ServiceCollectionExtensions.cs` with `AddWorldImportServices()` extension method registering `IImportSourceReader`/`ImportSourceReader`, `IImportValidator`/`ImportValidator`, `IWorldImportService`/`WorldImportService` as scoped services

### CLI Implementation (US1)

- [ ] T048 [US1] Create `Program.cs` entry point at `libris-maleficarum-service/src/Tools/Cli/Program.cs` with DI container setup (`AddLibrisApiClient`, `AddWorldImportServices`), root `RootCommand` for `libris`, and `WorldCommand` registration using `System.CommandLine`
- [ ] T049 [US1] Create `WorldCommand` at `libris-maleficarum-service/src/Tools/Cli/Commands/WorldCommand.cs` as the `world` area command grouping `WorldImportCommand` and `WorldValidateCommand` subcommands
- [ ] T050 [US1] Create `WorldImportCommand` at `libris-maleficarum-service/src/Tools/Cli/Commands/WorldImportCommand.cs` with `--source` (required), `--api-url` (required), `--owner-id` (required), `--token` (optional, falls back to LIBRIS_API_TOKEN env var per FR-033/FR-034), `--validate-only` flag (FR-021), `--verbose` flag (FR-022), `--max-concurrency` (default 10, FR-031), `--log-file` (optional, FR-028) options. Handler: resolve auth token (FR-033/FR-034, never log per FR-035), build `ImportOptions`, call `IWorldImportService.ImportAsync`, format output via `ConsoleReporter`, set exit code per contract (0/1/2)
- [ ] T051 [US1] Create `WorldValidateCommand` at `libris-maleficarum-service/src/Tools/Cli/Commands/WorldValidateCommand.cs` with `--source` (required) and `--verbose` (optional) options. Handler: call `IWorldImportService.ValidateAsync`, format output via `ConsoleReporter`, set exit code per contract (0/3)
- [ ] T052 [US1] Create `ConsoleReporter` at `libris-maleficarum-service/src/Tools/Cli/Output/ConsoleReporter.cs` with methods to format import success/failure output and validation output matching exact format from contracts/import-api-contract.md (entity counts by type, errors with file paths and skipped descendants, duration)

### CLI Tests (US1)

- [ ] T053 [P] [US1] Create `WorldImportCommandTests` at `libris-maleficarum-service/tests/unit/Cli.Tests/Commands/WorldImportCommandTests.cs` with tests for: required options validated (--source, --api-url, --owner-id), --token takes precedence over env var (FR-033), falls back to LIBRIS_API_TOKEN env var (FR-034), --validate-only invokes ValidateAsync not ImportAsync, --max-concurrency passed to ImportOptions, exit code 0 on success, exit code 1 on partial failure, exit code 2 on total failure, --log-file creates log file (FR-028)
- [ ] T054 [P] [US1] Create `WorldValidateCommandTests` at `libris-maleficarum-service/tests/unit/Cli.Tests/Commands/WorldValidateCommandTests.cs` with tests for: required --source option validated, exit code 0 on valid data, exit code 3 on validation errors, verbose output includes entity summary
- [ ] T055 [US1] Verify full build and all unit tests pass by running `dotnet test --solution LibrisMaleficarum.slnx --filter TestCategory=Unit` from `libris-maleficarum-service/`

**Checkpoint**: User Story 1 complete — folder-based import works end-to-end via CLI. All unit tests pass.

---

## Phase 4: User Story 2 — Import World from Zip Archive (Priority: P2)

**Goal**: A developer can import a world from a `.zip` file containing the same JSON folder structure, enabling portable sharing of world data.

**Independent Test**: Create a zip from the sample world folder, run `libris world import --source ./world.zip`, verify identical results to folder import.

### Zip Import Tests (US2)

- [ ] T056 [P] [US2] Add zip-specific tests to `ImportSourceReaderTests` at `libris-maleficarum-service/tests/unit/Import.Tests/Services/ImportSourceReaderTests.cs`: reads valid zip with world.json and entity files (FR-002), auto-detects zip source type by .zip extension (FR-003), ignores non-JSON entries in zip (FR-008), returns parse error when zip contains no world.json, handles corrupted zip returning ZIP_INVALID error, rejects zip entry with path traversal (zip-slip) returning ZIP_SLIP_DETECTED error (FR-032), reads entity files from nested folders within zip

### Zip Import Implementation (US2)

- [ ] T057 [US2] Extend `ImportSourceReader` at `libris-maleficarum-service/src/Components/Import/Services/ImportSourceReader.cs` to handle zip archive sources: detect `.zip` extension, open with `System.IO.Compression.ZipArchive`, validate each entry path against zip-slip by comparing `Path.GetFullPath()` of entry against extraction base directory (FR-032), read `world.json` from zip root, enumerate and parse entity JSON entries, skip non-JSON entries, collect parse errors, return `ImportSourceContent` with `SourceType.ZipArchive`
- [ ] T058 [US2] Verify zip import tests pass by running `dotnet test tests/unit/Import.Tests/ --filter "ClassName~ImportSourceReaderTests"` from `libris-maleficarum-service/`

**Checkpoint**: User Story 2 complete — zip import works identically to folder import. Zip-slip protection validated.

---

## Phase 5: User Story 3 — Validate Import Data Without Importing (Priority: P3)

**Goal**: A developer can run `libris world validate --source ./folder` to check import data for errors without making any API calls.

**Independent Test**: Run validate against a folder with both valid and invalid files, verify all issues reported and no API calls made.

> **Note**: The core validate-only infrastructure (IWorldImportService.ValidateAsync, WorldValidateCommand, ConsoleReporter validation output) was built in Phase 3 (US1). This phase adds targeted validation scenario tests and ensures the validate-only path is fully exercised.

### Validation Scenario Tests (US3)

- [ ] T059 [P] [US3] Create validation integration scenario tests at `libris-maleficarum-service/tests/unit/Import.Tests/Services/WorldImportServiceValidateTests.cs` with tests for: ValidateAsync on valid folder returns IsValid with complete manifest summary (US3-AC2), ValidateAsync on folder with mixed valid/invalid files reports all errors (US3-AC1), ValidateAsync checks JSON structural validity (US3-AC1), ValidateAsync checks required fields (US3-AC1), ValidateAsync checks valid entity types (US3-AC1), ValidateAsync checks localId uniqueness (US3-AC1), ValidateAsync checks parentLocalId references (US3-AC1), ValidateAsync checks cycle-free hierarchy (US3-AC1), ValidateAsync never calls ILibrisApiClient methods (US3-AC1), ValidateAsync on zip source works identically to folder
- [ ] T060 [US3] Verify validation tests pass and no regressions by running `dotnet test tests/unit/Import.Tests/` from `libris-maleficarum-service/`

**Checkpoint**: User Story 3 complete — validate-only mode thoroughly tested across all validation rules.

---

## Phase 6: User Story 4 — Sample World Data for Testing (Priority: P3)

**Goal**: The project includes a ready-made sample world dataset (Grimhollow) that developers can use for testing, demos, and verifying the import tool.

**Independent Test**: Run `libris world import --source ./samples/worlds/grimhollow` against a running backend and verify all entities created successfully.

### Sample Data Creation (US4)

- [ ] T061 [P] [US4] Create sample `world.json` at `samples/worlds/grimhollow/world.json` with name "Grimhollow" and description for a dark fantasy TTRPG world
- [ ] T062 [P] [US4] Create sample continent entity at `samples/worlds/grimhollow/entities/continents/grimhollow-continent.json` with localId `grimhollow-continent`, entityType `Continent`, no parentLocalId (root entity)
- [ ] T063 [P] [US4] Create sample country entities at `samples/worlds/grimhollow/entities/countries/iron-dominion.json` and `samples/worlds/grimhollow/entities/countries/sylvan-reach.json` with parentLocalId `grimhollow-continent`
- [ ] T064 [P] [US4] Create sample region entities at `samples/worlds/grimhollow/entities/regions/blackmoor-marshes.json`, `samples/worlds/grimhollow/entities/regions/crystalpeak-mountains.json`, and `samples/worlds/grimhollow/entities/regions/whispering-woods.json` with appropriate country parentLocalIds
- [ ] T065 [P] [US4] Create sample city entities at `samples/worlds/grimhollow/entities/cities/ironhold.json`, `samples/worlds/grimhollow/entities/cities/silverdale.json`, `samples/worlds/grimhollow/entities/cities/thornwall.json`, and `samples/worlds/grimhollow/entities/cities/marshaven.json` with appropriate region parentLocalIds
- [ ] T066 [P] [US4] Create sample building entities at `samples/worlds/grimhollow/entities/buildings/ironhold-castle.json`, `samples/worlds/grimhollow/entities/buildings/silverdale-temple.json`, and `samples/worlds/grimhollow/entities/buildings/thornwall-tavern.json` with appropriate city parentLocalIds
- [ ] T067 [P] [US4] Create sample character entities at `samples/worlds/grimhollow/entities/characters/king-aldric.json`, `samples/worlds/grimhollow/entities/characters/queen-elara.json`, `samples/worlds/grimhollow/entities/characters/captain-thorne.json`, `samples/worlds/grimhollow/entities/characters/elder-whisper.json`, `samples/worlds/grimhollow/entities/characters/merchant-gilda.json`, and `samples/worlds/grimhollow/entities/characters/shadow-assassin.json` with appropriate parentLocalIds referencing cities
- [ ] T068 [P] [US4] Create sample faction entities at `samples/worlds/grimhollow/entities/factions/iron-guard.json`, `samples/worlds/grimhollow/entities/factions/shadow-court.json`, and `samples/worlds/grimhollow/entities/factions/sylvan-council.json` with appropriate parentLocalIds
- [ ] T069 [P] [US4] Create sample campaign entity at `samples/worlds/grimhollow/entities/campaigns/the-shadow-war.json` with localId `the-shadow-war`, entityType `Campaign`, no parentLocalId
- [ ] T070 [P] [US4] Create sample quest entities at `samples/worlds/grimhollow/entities/quests/find-the-lost-artifact.json`, `samples/worlds/grimhollow/entities/quests/defend-ironhold.json`, and `samples/worlds/grimhollow/entities/quests/investigate-the-marshes.json` with parentLocalId `the-shadow-war`
- [ ] T071 [US4] Validate sample data by running `dotnet run --project libris-maleficarum-service/src/Tools/Cli/ -- world validate --source samples/worlds/grimhollow` and verify exit code 0 with summary showing 25+ entities across 8+ types with hierarchy depth 4 (SC-005)

**Checkpoint**: User Story 4 complete — sample Grimhollow world data passes validation with 25+ entities, 8+ types, 4 levels of hierarchy.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, code quality, and final validation.

- [ ] T072 [P] Update `libris-maleficarum-service/README.md` to document the CLI tool (`libris world import` / `libris world validate`), add build instructions, and reference `samples/worlds/grimhollow/`
- [ ] T073 [P] Create `samples/worlds/grimhollow/README.md` documenting the sample world: entity hierarchy, entity types included, usage with the import tool
- [ ] T074 [P] Create zip archive `samples/worlds/grimhollow.zip` from the `grimhollow/` folder for testing US2 zip import path
- [ ] T075 Run full solution build and all unit tests: `dotnet build LibrisMaleficarum.slnx` and `dotnet test --solution LibrisMaleficarum.slnx --filter TestCategory=Unit` from `libris-maleficarum-service/`
- [ ] T076 Run quickstart.md validation: execute each CLI command from quickstart.md (build, validate, programmatic usage example) and verify they work as documented
- [ ] T077 Verify SC-001: time a full import of the Grimhollow sample (25+ entities) against a running backend, confirm under 60 seconds

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2 — core import functionality
- **User Story 2 (Phase 4)**: Depends on Phase 3 T041 (ImportSourceReader exists to extend)
- **User Story 3 (Phase 5)**: Depends on Phase 3 (validation infrastructure built in US1)
- **User Story 4 (Phase 6)**: Depends on Phase 3 (CLI tool must exist to validate sample data)
- **Polish (Phase 7)**: Depends on all user story phases

### User Story Dependencies

- **US1 (P1)**: Depends on Foundational only — core MVP
- **US2 (P2)**: Depends on US1 (extends `ImportSourceReader`)
- **US3 (P3)**: Depends on US1 (tests the validate-only path built in US1)
- **US4 (P3)**: Depends on US1 (uses CLI tool to validate sample data); can start file creation [P] during any phase

### Within Each User Story

- Tests MUST be written and FAIL before implementation
- Models before services
- Services before CLI commands
- Interfaces before implementations
- Core implementation before integration

### Parallel Opportunities

**Phase 1**: T004, T005, T006 can run in parallel (test projects)
**Phase 2**: T009–T016 can all run in parallel (models + exceptions); T020, T021 can run in parallel (test files)
**Phase 3**: T023–T036 can all run in parallel (all import models); T037–T039 in parallel (interfaces); T040, T042, T044, T045 in parallel (test files); T053, T054 in parallel (CLI tests)
**Phase 4**: T056 in parallel with other US2 work
**Phase 5**: T059 standalone
**Phase 6**: T061–T070 can all run in parallel (all sample data files)
**Phase 7**: T072, T073, T074 can run in parallel

---

## Parallel Example: User Story 1

```text
# Step 1 — Launch all models in parallel:
T023: Create WorldImportDefinition
T024: Create EntityImportDefinition
T025: Create ImportSourceContent + ImportSourceType
T026: Create ResolvedEntity
T027: Create ImportManifest
T028: Create ImportValidationResult
T029: Create ImportValidationError
T030: Create ImportValidationWarning
T031: Create ImportResult
T032: Create EntityImportError
T033: Create ImportOptions
T034: Create ImportProgress + ImportPhase
T035: Create CreateEntityImportRequest
T036: Create ImportValidationErrorCodes

# Step 2 — Launch all interfaces in parallel:
T037: Create IImportSourceReader
T038: Create IImportValidator
T039: Create IWorldImportService

# Step 3 — Launch tests in parallel (write first, watch fail):
T040: ImportSourceReaderTests
T042: ImportValidatorTests
T044: EntityImportDefinitionTests
T045: WorldImportServiceTests

# Step 4 — Implement services sequentially (make tests pass):
T041: ImportSourceReader → makes T040 pass
T043: ImportValidator → makes T042 pass
T046: WorldImportService → makes T045 pass

# Step 5 — DI registration:
T047: ServiceCollectionExtensions

# Step 6 — CLI (sequential, depends on services):
T048: Program.cs
T049: WorldCommand
T050: WorldImportCommand
T051: WorldValidateCommand
T052: ConsoleReporter

# Step 7 — CLI tests in parallel:
T053: WorldImportCommandTests
T054: WorldValidateCommandTests

# Step 8 — Full verification:
T055: Build + test pass
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup — project scaffolding
2. Complete Phase 2: Foundational — API client SDK
3. Complete Phase 3: User Story 1 — folder import + CLI
4. **STOP and VALIDATE**: Run `libris world validate --source samples/worlds/grimhollow` (after creating minimal sample data)
5. Deploy/demo if ready — folder import is the core value

### Incremental Delivery

1. Setup + Foundational → SDK ready
2. Add User Story 1 → Folder import works → **MVP!**
3. Add User Story 2 → Zip import works → Portable sharing enabled
4. Add User Story 3 → Validate-only thoroughly tested → Quality-of-life improvement
5. Add User Story 4 → Sample data bundled → Ready for demos and test automation
6. Polish → Docs, zip archive, final validation
7. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational (Phase 2) is done:
   - Developer A: US1 models + interfaces + services
   - Developer A (continued): US1 CLI commands
3. After US1 core services exist:
   - Developer A: US2 (extend source reader for zip)
   - Developer B: US3 (validation scenario tests)
   - Developer C: US4 (sample data creation — can start earlier in parallel)
4. All converge on Phase 7: Polish

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks in the same phase
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Tests MUST be written and fail before implementation (TDD per constitution)
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Auth tokens must never be logged, persisted, or hardcoded (FR-035)
- All zip entry paths must be validated against zip-slip attacks (FR-032)
