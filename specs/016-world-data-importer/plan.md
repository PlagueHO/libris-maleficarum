# Implementation Plan: World Data Importer

**Branch**: `016-world-data-importer` | **Date**: 2026-03-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/016-world-data-importer/spec.md`

## Summary

Build a CLI tool (`libris`), a reusable import library (`LibrisMaleficarum.Import`), and an API client SDK (`LibrisMaleficarum.Api.Client`) that together import world data (world metadata + hierarchical entities) from a folder or zip archive of JSON files into the backend REST API. The CLI uses `System.CommandLine` with the `libris world import` / `libris world validate` command pattern. The import library is decoupled from the CLI for future reuse in a frontend import service, and delegates all HTTP communication to the shared API client SDK. Entities are imported in breadth-first hierarchy order with configurable parallel API calls per depth level.

## Technical Context

**Language/Version**: C# 14 / .NET 10 (aligned with `global.json` SDK 10.0.100)
**Primary Dependencies**: `System.CommandLine` (CLI parsing), `Microsoft.Extensions.Http.Resilience` (HTTP retry/resilience), `System.Text.Json` (JSON parsing), `System.IO.Compression` (zip handling)
**Storage**: N/A (reads local files, writes to backend API via HTTP)
**Testing**: MSTest SDK 4.1.0 (aligned with `global.json`), FluentAssertions, NSubstitute for mocking
**Target Platform**: Cross-platform .NET 10 console application (Windows, Linux, macOS)
**Project Type**: CLI tool + class library + API client SDK (three new projects)
**Performance Goals**: Import 50+ entity world in under 60 seconds (SC-001)
**Constraints**: Max 10 concurrent API calls by default (configurable). Must not hardcode/log/persist credentials.
**Scale/Scope**: Typical import: 20-200 entities. Max entity hierarchy depth: 10 levels.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| # | Principle | Status | Notes |
|---|-----------|--------|-------|
| I | Cloud-Native Architecture | PASS | CLI tool is a development utility; import targets cloud-deployed API. No new infrastructure resources required. |
| II | Clean Architecture & Separation of Concerns | PASS | Import library (`LibrisMaleficarum.Import`) separates from CLI concerns (`LibrisMaleficarum.Cli`). API client SDK (`LibrisMaleficarum.Api.Client`) provides shared typed HTTP access. Import lives under `src/Components/`, API client under `src/Client/`. Dependencies flow inward. |
| III | Test-Driven Development (NON-NEGOTIABLE) | PASS | Unit tests for Import library (parsing, validation, hierarchy). Unit tests for CLI (argument parsing, output formatting). Integration tests planned but deferred to task phase. MSTest + FluentAssertions. |
| IV | Framework & Technology Standards | PASS | .NET 10, System.CommandLine, System.Text.Json — all aligned with project standards. |
| V | Developer Experience & Inner Loop | PASS | CLI integrates with Aspire-orchestrated local environment. Can run against local API started via AppHost. |
| VI | Security & Privacy by Default | PASS | Auth token via CLI parameter or env var only (FR-033/034). Never hardcoded/logged/persisted (FR-035). Zip-slip protection (FR-032). |
| VII | Semantic Versioning & Breaking Changes | PASS | New feature, no breaking changes. CLI command structure designed for forward compatibility. |

**Post-Phase 1 Re-check**: All gates remain PASS. The import library has no console dependencies (SC-006 satisfied by design). The API client SDK is shared infrastructure with no console or import-specific dependencies. The data model uses existing Domain types (EntityType enum) without modification.

## Project Structure

### Documentation (this feature)

```text
specs/016-world-data-importer/
├── plan.md              # This file
├── research.md          # Phase 0 output — research decisions
├── data-model.md        # Phase 1 output — import models and interfaces
├── quickstart.md        # Phase 1 output — usage guide
├── contracts/           # Phase 1 output — API and CLI contracts
│   └── import-api-contract.md
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (within `libris-maleficarum-service/`)

```text
src/
├── Api/                                         # Existing — backend REST API
│   └── LibrisMaleficarum.Api.csproj
├── Domain/                                      # Existing — domain entities, EntityType enum
│   └── LibrisMaleficarum.Domain.csproj
├── Client/                                      # NEW — client SDK projects
│   └── Api/                                     # NEW — typed API client SDK
│       ├── LibrisMaleficarum.Api.Client.csproj
│       ├── ILibrisApiClient.cs                  # SDK interface
│       ├── LibrisApiClient.cs                   # HttpClient implementation
│       ├── Models/
│       │   ├── CreateWorldRequest.cs
│       │   ├── CreateEntityRequest.cs
│       │   ├── WorldResponse.cs
│       │   ├── EntityResponse.cs
│       │   ├── ApiResponse.cs
│       │   └── ApiError.cs
│       ├── Exceptions/
│       │   ├── LibrisApiException.cs
│       │   └── LibrisApiAuthenticationException.cs
│       └── Extensions/
│           └── ServiceCollectionExtensions.cs   # AddLibrisApiClient()
├── Components/                                  # NEW — shared reusable functional libraries
│   └── Import/                                  # NEW — reusable import library
│       ├── LibrisMaleficarum.Import.csproj
│       ├── Interfaces/
│       │   ├── IImportSourceReader.cs
│       │   ├── IImportValidator.cs
│       │   └── IWorldImportService.cs
│       ├── Models/
│   │   ├── WorldImportDefinition.cs
│   │   ├── EntityImportDefinition.cs
│   │   ├── ImportManifest.cs
│   │   ├── ResolvedEntity.cs
│   │   ├── ImportValidationResult.cs
│       │   ├── ImportValidationError.cs
│       │   ├── ImportValidationWarning.cs
│       │   ├── ImportResult.cs
│       │   ├── EntityImportError.cs
│       │   ├── ImportOptions.cs
│       │   ├── ImportSourceContent.cs
│       │   └── ImportProgress.cs
│       ├── Services/
│       │   ├── WorldImportService.cs          # Orchestrates read → validate → execute
│       │   ├── ImportSourceReader.cs          # Reads folder/zip into ImportSourceContent
│       │   └── ImportValidator.cs             # Validates content, builds ImportManifest
│       ├── Validation/
│       │   └── ImportValidationErrorCodes.cs  # Error code constants
│       └── Extensions/
│           └── ServiceCollectionExtensions.cs # DI registration: AddWorldImportServices()
├── Infrastructure/                              # Existing
│   └── LibrisMaleficarum.Infrastructure.csproj
├── Orchestration/                               # Existing
│   ├── AppHost/
│   │   └── LibrisMaleficarum.AppHost.csproj
│   └── ServiceDefaults/
│       └── LibrisMaleficarum.ServiceDefaults.csproj
├── Tools/                                       # NEW — tool projects
│   └── Cli/                                     # NEW — CLI tool
│       ├── LibrisMaleficarum.Cli.csproj
│       ├── Program.cs                           # Entry point, command registration
│       ├── Commands/
│       │   ├── WorldCommand.cs                  # "world" area command
│       │   ├── WorldImportCommand.cs            # "world import" action command
│       │   └── WorldValidateCommand.cs          # "world validate" action command
│       └── Output/
│           └── ConsoleReporter.cs               # Formats import results for console
└── Worker/                                      # Existing
    └── SearchIndexWorker/
        └── LibrisMaleficarum.SearchIndexWorker.csproj

tests/
├── unit/
│   ├── Api.Client.Tests/                        # NEW — API client SDK unit tests
│   │   ├── LibrisMaleficarum.Api.Client.Tests.csproj
│   │   └── LibrisApiClientTests.cs
│   ├── Import.Tests/                            # NEW — import library unit tests
│   │   ├── LibrisMaleficarum.Import.Tests.csproj
│   │   ├── Services/
│   │   │   ├── ImportSourceReaderTests.cs
│   │   │   ├── ImportValidatorTests.cs
│   │   │   └── WorldImportServiceTests.cs
│   │   └── Models/
│   │       └── EntityImportDefinitionTests.cs
│   ├── Cli.Tests/                               # NEW — CLI unit tests
│   │   ├── LibrisMaleficarum.Cli.Tests.csproj
│   │   └── Commands/
│   │       ├── WorldImportCommandTests.cs
│   │       └── WorldValidateCommandTests.cs
│   ├── Api.Tests/                               # Existing
│   ├── Domain.Tests/                            # Existing
│   ├── Infrastructure.Tests/                    # Existing
│   └── Worker.Tests/                            # Existing
└── integration/                                 # Existing
    ├── Api.IntegrationTests/                    # Existing
    ├── Infrastructure.IntegrationTests/         # Existing
    └── Orchestration.IntegrationTests/          # Existing

samples/                                         # NEW — at repo root
└── worlds/
    └── grimhollow/                              # Sample fantasy world
        ├── world.json
        └── entities/
            ├── continents/
            │   └── grimhollow-continent.json
            ├── countries/
            │   ├── iron-dominion.json
            │   └── sylvan-reach.json
            ├── regions/
            │   ├── blackmoor-marshes.json
            │   ├── crystalpeak-mountains.json
            │   └── whispering-woods.json
            ├── cities/
            │   ├── ironhold.json
            │   ├── silverdale.json
            │   ├── thornwall.json
            │   └── marshaven.json
            ├── buildings/
            │   ├── ironhold-castle.json
            │   ├── silverdale-temple.json
            │   └── thornwall-tavern.json
            ├── characters/
            │   ├── king-aldric.json
            │   ├── queen-elara.json
            │   ├── captain-thorne.json
            │   ├── elder-whisper.json
            │   ├── merchant-gilda.json
            │   └── shadow-assassin.json
            ├── factions/
            │   ├── iron-guard.json
            │   ├── shadow-court.json
            │   └── sylvan-council.json
            ├── campaigns/
            │   └── the-shadow-war.json
            └── quests/
                ├── find-the-lost-artifact.json
                ├── defend-ironhold.json
                └── investigate-the-marshes.json
```

**Structure Decision**: Three new source projects under `libris-maleficarum-service/src/`:

1. `src/Client/Api/` — API client SDK (`LibrisMaleficarum.Api.Client`). References `Domain` only. Provides strongly-typed, resilient HTTP access to all API endpoints. Shared by Import library and future consumers. `Client/` is a role-based category (like `Worker/`, `Tools/`) and `Api/` identifies the target service.
2. `src/Components/Import/` — Reusable import library (`LibrisMaleficarum.Import`). References `Api.Client` and `Domain`. Contains all parsing, validation, and hierarchy resolution logic. No console dependencies, no direct HTTP logic.
3. `src/Tools/Cli/` — CLI tool (`LibrisMaleficarum.Cli`). References `Import` only. Contains command definitions, argument parsing, and console output formatting. Produces `libris` executable via `<AssemblyName>libris</AssemblyName>`.

This follows the existing project organization pattern:

- Primary service projects at `src/{Name}/` (Api, Domain, Infrastructure)
- Client SDK projects at `src/Client/{Service}/` (Api)
- Shared reusable libraries at `src/Components/{Name}/` (Import)
- Tool/utility projects at `src/Tools/{Name}/` (Cli)
- Worker projects at `src/Worker/{Name}/` (SearchIndexWorker)
- Orchestration projects at `src/Orchestration/{Name}/` (AppHost, ServiceDefaults)

Sample data at repo root `samples/worlds/grimhollow/` — separate from service code, accessible to any consumer.

## Dependency Graph

```text
LibrisMaleficarum.Cli
  └── LibrisMaleficarum.Import
        ├── LibrisMaleficarum.Api.Client
        │     └── LibrisMaleficarum.Domain
        │           (EntityType enum, validation constants)
        └── LibrisMaleficarum.Domain
```

The Import library does NOT depend on Infrastructure, Api, or Orchestration. It consumes the shared `LibrisMaleficarum.Api.Client` SDK (via `ILibrisApiClient` interface) for all HTTP communication, keeping it decoupled from the backend internals. The Api.Client SDK does NOT depend on Infrastructure, Api, or Orchestration either — it talks to the API over HTTP only.

## Key Design Decisions

### 1. CLI Executable Name: `libris`

- Short, memorable, aligns with product identity.
- Set via `<AssemblyName>libris</AssemblyName>` in csproj.
- Follows convention of well-known CLIs (`dotnet`, `az`, `git`, `kubectl`).

### 2. Command Structure: `libris world import`

- Area command `world` groups world-related actions.
- Action commands `import` and `validate` are leaf commands with options.
- Follows Microsoft's System.CommandLine design guidance.
- Extensible for future commands (`libris world export`, `libris entity search`).

### 3. Role-Based Project Organization

- Spec FR-026 requires reusable library separate from CLI.
- SC-006 requires no console dependencies in library.
- Projects organized by role: `Client/` for service clients, `Components/` for shared business logic, `Tools/` for utilities.
- Enables future frontend import service to use same parsing/validation/API client logic.
- Clean dependency: Import → Api.Client → Domain.

### 4. Breadth-First Parallel Import

- Entities imported level-by-level (depth 0 first, then depth 1, etc.).
- Within each level, entities created in parallel with `SemaphoreSlim` throttling.
- Default max concurrency: 10, configurable via `--max-concurrency`.
- If an entity fails, all its descendants are skipped (FR-027).

### 5. Authentication: Token Parameter + Environment Variable

- `--token` CLI parameter takes precedence.
- Falls back to `LIBRIS_API_TOKEN` environment variable.
- Never logged, persisted, or hardcoded (FR-035).

### 6. Zip-Slip Protection

- All zip entry paths validated to ensure they don't escape extraction directory (FR-032).
- Uses `Path.GetFullPath()` comparison against target directory.

## Solution File Updates

The following entries will be added to `LibrisMaleficarum.slnx`:

```xml
<Folder Name="/src/Client/" />
<Folder Name="/src/Client/Api/">
  <Project Path="src/Client/Api/LibrisMaleficarum.Api.Client.csproj" />
</Folder>
<Folder Name="/src/Components/" />
<Folder Name="/src/Components/Import/">
  <Project Path="src/Components/Import/LibrisMaleficarum.Import.csproj" />
</Folder>
<Folder Name="/src/Tools/" />
<Folder Name="/src/Tools/Cli/">
  <Project Path="src/Tools/Cli/LibrisMaleficarum.Cli.csproj" />
</Folder>
<Folder Name="/tests/unit/Api.Client.Tests/">
  <Project Path="tests/unit/Api.Client.Tests/LibrisMaleficarum.Api.Client.Tests.csproj" />
</Folder>
<Folder Name="/tests/unit/Import.Tests/">
  <Project Path="tests/unit/Import.Tests/LibrisMaleficarum.Import.Tests.csproj" />
</Folder>
<Folder Name="/tests/unit/Cli.Tests/">
  <Project Path="tests/unit/Cli.Tests/LibrisMaleficarum.Cli.Tests.csproj" />
</Folder>
```

## Project File Specifications

### LibrisMaleficarum.Api.Client.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="10.1.0" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Domain\LibrisMaleficarum.Domain.csproj" />
  </ItemGroup>
</Project>
```

### LibrisMaleficarum.Import.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Client\Api\LibrisMaleficarum.Api.Client.csproj" />
    <ProjectReference Include="..\..\Domain\LibrisMaleficarum.Domain.csproj" />
  </ItemGroup>
</Project>
```

### LibrisMaleficarum.Cli.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <AssemblyName>libris</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="System.CommandLine" Version="2.0.0-beta5.25302.1" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Components\Import\LibrisMaleficarum.Import.csproj" />
  </ItemGroup>
</Project>
```

## Complexity Tracking

> No constitution violations to justify. All gates pass.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
