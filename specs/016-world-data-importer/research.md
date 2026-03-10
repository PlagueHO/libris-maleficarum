# Research: World Data Importer

**Feature Branch**: `016-world-data-importer`
**Date**: 2026-03-10

## Research Topics

### 1. CLI Tool Naming and Project Naming

**Decision**: CLI executable name: `libris` (short, memorable, aligns with product identity). Assembly/project name: `LibrisMaleficarum.Cli`.

**Rationale**:

- Follows the pattern of well-known CLIs (`dotnet`, `az`, `git`, `kubectl`) — short, lowercase, single-word.
- `libris` is the natural abbreviation of `Libris Maleficarum` and avoids the awkwardness of `libris-maleficarum-cli` or `lm-cli`.
- Project name `LibrisMaleficarum.Cli` aligns perfectly with existing naming: `LibrisMaleficarum.Api`, `LibrisMaleficarum.Domain`, `LibrisMaleficarum.Infrastructure`, `LibrisMaleficarum.SearchIndexWorker`.
- The `<AssemblyName>libris</AssemblyName>` in the csproj produces `libris.exe` on Windows and `libris` on Linux/macOS.

**Alternatives considered**:

- `libris-cli` — Redundant suffix; CLIs don't need "cli" in their name (you don't type `dotnet-cli`).
- `lm` — Too cryptic, not discoverable. Potential collision with other tools.
- `libris-maleficarum` — Too verbose for a CLI tool name.

### 2. System.CommandLine Architecture Pattern

**Decision**: Use `System.CommandLine` (latest stable) with the command-subcommand-options pattern. Root command = `libris`, area command = `world`, action subcommands = `import`, `validate` (and future: `export`).

**Rationale**:

- Follows Microsoft's official design guidance: area commands group related actions, leaf commands execute actions.
- Matches the `dotnet tool install` / `az group create` pattern users expect.
- Enables future extensibility (`libris world export`, `libris entity search`, etc.) without breaking existing CLI contracts.

**Command structure**:

```text
libris
├── world
│   ├── import   --source <path> --api-url <url> [--token <token>] [--validate-only] [--verbose] [--max-concurrency <n>] [--log-file <path>]
│   └── validate --source <path> [--verbose]
└── --version
```

**Alternatives considered**:

- Flat command structure (`libris import-world`) — Doesn't scale when adding export, entity commands.
- Using Spectre.Console.Cli — Less standard, no official Microsoft backing, smaller ecosystem.

### 3. Reusable Import Library Placement

**Decision**: Create `LibrisMaleficarum.Import` as a class library project under `src/Components/Import/`. A new `Components/` folder groups small, reusable functional libraries that are shared across multiple consumers (CLI tools, services, workers). Both the CLI tool and future frontend import service consume this library.

**Rationale**:

- Spec FR-026 explicitly requires the import logic to be structured as a reusable library separate from the console tool.
- SC-006 requires the library to have no console-specific dependencies.
- The `Components/` folder provides a clear grouping for shared functional libraries that don't fit neatly into the existing `Api/Domain/Infrastructure` layering. Other future shared components will also live here.
- Placing it under `src/Components/Import/` mirrors the nesting pattern used by `src/Worker/SearchIndexWorker/` and `src/Tools/Cli/` — a category folder containing specific project folders.
- The library references `LibrisMaleficarum.Domain` for entity types and models.
- The CLI project (`src/Tools/Cli/`) references the import library and handles only CLI concerns (argument parsing, console output, exit codes).

**Alternatives considered**:

- `src/Import/` (flat at src root) — Doesn't communicate the shared/reusable nature; puts it at the same level as Api/Domain which implies it's a primary layer.
- `src/Libraries/Import/` — Reasonable, but "Libraries" is vague. "Components" better conveys discrete functional units.
- `src/Shared/Import/` — Too generic; "Shared" doesn't convey what's being shared or why.
- Put import logic in Domain project — Violates single responsibility; Domain should only contain entities and interfaces.
- Put import logic in Infrastructure — Import is not a persistence concern; it's a distinct capability.

### 4. CLI Project Placement

**Decision**: Place CLI project at `src/Tools/Cli/LibrisMaleficarum.Cli.csproj`. The `Tools/` folder groups utility/tool projects separate from the main service projects.

**Rationale**:

- The CLI is not a service (like Api or Worker) — it's a tool. A `Tools/` folder clearly communicates this distinction.
- Follows precedent in large .NET solutions where tools get their own top-level grouping.
- Keeps the `src/` root clean: Api, Domain, Infrastructure, Orchestration, Worker, Import, Tools.
- Future tools (e.g., migration tool, admin CLI) can also live under `Tools/`.

**Alternatives considered**:

- `src/Cli/` — Flat, but doesn't communicate "tool" nature. Puts it at same level as Api/Domain.
- `src/Tools/LibrisMaleficarum.Cli/` without intermediate Cli folder — Acceptable, but `Tools/Cli/` mirrors the `Worker/SearchIndexWorker/` pattern of category/specific nesting.

### 5. Import File Format Specification

**Decision**: Use a folder-based format with a mandatory `world.json` at root and entity JSON files using camelCase properties with `localId`/`parentLocalId` for relationship mapping. Parent-child relationships are defined **solely** by `parentLocalId` — any entity type can be a child of any other entity type.

**Rationale**:

- Aligns with spec FR-004 through FR-008.
- camelCase matches the API JSON serialization and frontend conventions.
- `localId` / `parentLocalId` enables human-readable relationship definitions without requiring the user to generate GUIDs.
- Folder structure is for human organization only (spec states no semantic meaning).

**Parent-Child Relationship Model**:

Per the [DATA_MODEL.md](../../docs/design/DATA_MODEL.md), the WorldEntity hierarchy is **unrestricted** — any entity can be a child of any other entity. The `ParentId` field is a simple GUID reference with no type constraints. The system provides `RecommendedChildTypes` per entity type as a UI hint, but the API does **not** enforce parent-child type rules.

This means the import format must support:

- A `Character` as a child of a `City` (e.g., a character who lives there).
- A `Quest` as a child of a `Campaign`.
- A `Faction` as a child of a `Country`.
- A `Building` as a child of a `Character` (e.g., a character's stronghold).
- A `Session` as a child of a `Campaign`.
- Deep nesting: `Continent` → `Country` → `City` → `Building` → `Room`.

The `parentLocalId` field handles all of these cases naturally — it references any other entity's `localId` regardless of type. If `parentLocalId` is `null` or omitted, the entity is a root-level entity within the world.

**Import file schema**:

```json
// world.json
{
  "name": "The Realm of Shadows",
  "description": "A dark fantasy world for D&D 5e campaigns"
}
```

```json
// entities/continents/shadowlands.json
{
  "localId": "shadowlands",
  "entityType": "Continent",
  "name": "The Shadowlands",
  "description": "A vast continent shrouded in perpetual twilight",
  "parentLocalId": null,
  "tags": ["dark", "continent", "main"],
  "properties": {
    "climate": "Temperate-Dark",
    "population": "Unknown"
  }
}
```

```json
// entities/cities/ironhold.json — Location as child of a country
{
  "localId": "ironhold",
  "entityType": "City",
  "name": "Ironhold",
  "description": "The fortified capital of the Iron Dominion",
  "parentLocalId": "iron-dominion",
  "tags": ["capital", "fortified"]
}
```

```json
// entities/characters/king-aldric.json — Character as child of a city
{
  "localId": "king-aldric",
  "entityType": "Character",
  "name": "King Aldric",
  "description": "The aging ruler of the Iron Dominion",
  "parentLocalId": "ironhold",
  "tags": ["royalty", "npc"],
  "properties": {
    "class": "Fighter",
    "level": 15
  }
}
```

```json
// entities/quests/defend-ironhold.json — Quest as child of a campaign
{
  "localId": "defend-ironhold",
  "entityType": "Quest",
  "name": "Defend Ironhold",
  "description": "Protect the capital from the Shadow Court's siege",
  "parentLocalId": "the-shadow-war",
  "tags": ["main-quest", "combat"]
}
```

**Key clarification**: The subfolder where an entity JSON file lives (e.g., `characters/`, `cities/`) has **no semantic meaning** for parent-child relationships. An entity file in `characters/` can have `parentLocalId` pointing to a city, campaign, or any other entity. The folder structure is purely for human organization. The import reader scans all `.json` files recursively (excluding `world.json`) regardless of folder depth.

### 6. Parallel Import Strategy

**Decision**: Import entities level-by-level (breadth-first by depth), with configurable concurrency per level using `SemaphoreSlim`. Default max concurrency: 10.

**Rationale**:

- Spec FR-029 requires parallel creation of entities at the same hierarchy depth.
- Spec FR-030 requires configurable max concurrency.
- Parents must exist before children (FR-010), so depth-first ordering per level is mandatory.
- `SemaphoreSlim` with `Task.WhenAll` is the idiomatic .NET pattern for bounded parallelism.
- Default of 10 balances throughput against API rate limits and Cosmos DB RU budgets.

**Alternatives considered**:

- Sequential import — Too slow for large worlds (50+ entities). Violates FR-029.
- Unbounded parallelism — Risks overwhelming API/Cosmos DB with 429 errors.
- Channel-based producer/consumer — Overkill for this batch workload.

### 7. Authentication Pattern

**Decision**: Accept a `--token` CLI option (highest priority), fall back to `LIBRIS_API_TOKEN` environment variable.

**Rationale**:

- Spec FR-033/FR-034 explicitly require this dual approach.
- Spec FR-035 prohibits hardcoding, logging, or persisting credentials.
- Environment variable name `LIBRIS_API_TOKEN` follows standard conventions (`AZURE_*`, `GITHUB_TOKEN`, etc.).
- CLI parameter allows one-off overrides; env var enables CI/CD scripting.

### 8. Error Handling and Exit Codes

**Decision**: Use standard exit codes: 0 = full success, 1 = partial failure (some entities failed), 2 = total failure (no entities imported), 3 = validation failure (validate-only mode).

**Rationale**:

- Spec FR-025 requires 0 for success and non-zero for errors.
- Distinguishing partial from total failure helps CI/CD scripts decide whether to proceed.
- Matches common CLI patterns (e.g., `dotnet test` uses different exit codes for different failure modes).

### 9. API Client SDK

**Decision**: Create a dedicated API client SDK project `LibrisMaleficarum.Api.Client` under `src/Client/Api/`. This SDK wraps `HttpClient` with strongly-typed methods for all existing API endpoints, built-in resilience (retry/circuit breaker via `Microsoft.Extensions.Http.Resilience`), and a clean `ILibrisApiClient` interface. The Import library consumes this SDK rather than implementing its own HTTP logic.

**Rationale**:

- The API surface is already well-defined (Create World, Create Entity, etc.) and will grow as the backend expands. A dedicated SDK ensures all consumers get the same typed, tested, resilient API access.
- Multiple consumers will need API access: the Import library, the CLI tool (for future commands), and potentially a frontend import service. Without a shared SDK, each consumer re-implements request/response serialization, error handling, and retry logic.
- The SDK lives under `src/Client/Api/` — `Client/` is a role-based category folder (like `Worker/`, `Tools/`, `Orchestration/`) that communicates the project's purpose: consuming a service. The `Api/` subfolder identifies which service.
- The naming `LibrisMaleficarum.Api.Client` pairs naturally with `LibrisMaleficarum.Api` (server ↔ client), following the standard .NET SDK pattern (e.g., `Azure.Storage.Blobs` client for the Blobs service).
- The SDK references `LibrisMaleficarum.Domain` for shared types (e.g., `EntityType` enum) but does **not** reference Api or Infrastructure — it talks to the API over HTTP only.
- Provides `ILibrisApiClient` interface for testability — consumers mock the interface in unit tests rather than setting up HTTP mocks.
- Includes DI registration via `AddLibrisApiClient(options => ...)` extension method for clean integration.
- Future clients (e.g., SignalR hub client) would go under `src/Client/SignalR/` naturally.

**SDK scope** (initial, for this feature):

- `CreateWorldAsync(CreateWorldRequest)` → `WorldResponse`
- `CreateEntityAsync(Guid worldId, CreateEntityRequest)` → `EntityResponse`
- Future endpoints added as the API grows (get, update, delete, search).

**Alternatives considered**:

- `src/Components/ApiClient/` with name `LibrisMaleficarum.ApiClient` — Mixes a protocol client role with the `Components/` category for business logic libraries. An API client is fundamentally different from Import (protocol adapter vs. domain feature).
- Raw `HttpClient` in Import library — Works short-term, but creates duplication as more consumers appear. No shared error handling or typed responses.
- NSwag/Kiota auto-generated client — Overkill for the current API surface. Adds build-time code generation complexity. Can be reconsidered when the API stabilizes and grows significantly.
- Put API client in Infrastructure project — Infrastructure is for persistence (Cosmos DB, EF Core), not HTTP clients. The SDK is a distinct cross-cutting concern.

### 10. Test Strategy

**Decision**: Unit tests for the Import library (parsing, validation, hierarchy resolution) + Integration tests for the CLI tool (end-to-end import with test API).

**Rationale**:

- Constitution mandates TDD (Principle III).
- Unit tests cover: JSON parsing, validation rules, hierarchy cycle detection, localId uniqueness, depth ordering.
- Integration tests cover: CLI argument parsing, API interaction, error reporting, exit codes.
- Test projects: `LibrisMaleficarum.Api.Client.Tests` (unit), `LibrisMaleficarum.Import.Tests` (unit), `LibrisMaleficarum.Cli.Tests` (unit), `LibrisMaleficarum.Cli.IntegrationTests` (integration).
- Uses MSTest (aligned with global.json `MSTest.Sdk` configuration) + FluentAssertions.

### 11. Sample World Dataset

**Decision**: Create a fantasy/TTRPG sample world under `samples/worlds/grimhollow/` with 25+ entities across 5+ entity types, minimum 3-level hierarchy depth. Also provide a zip version.

**Rationale**:

- Spec SC-005 requires at least 20 entities, 4 entity types, 3 levels of depth.
- Fantasy/D&D theme aligns with the application domain.
- A `samples/` folder at the repo root is the standard location for example data.
- Having both folder and zip forms enables testing both import paths.

### 12. `--validate` Subcommand vs. `--validate-only` Flag

**Decision**: Provide both: a `world validate` subcommand for standalone validation AND a `--validate-only` flag on `world import` for convenience.

**Rationale**:

- `libris world validate --source ./my-world` is the clean, discoverable approach following the command-subcommand pattern.
- `libris world import --source ./my-world --validate-only` provides quick dry-run for users already typing an import command.
- Both call the same underlying validation logic in the Import library.
