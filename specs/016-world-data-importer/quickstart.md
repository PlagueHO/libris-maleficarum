# Quickstart: World Data Importer

**Feature Branch**: `016-world-data-importer`

## Prerequisites

- .NET 10 SDK
- Backend API running (via `dotnet run --project src/Orchestration/AppHost`)
- A world data folder or zip file

## Build the CLI

```bash
cd libris-maleficarum-service
dotnet build src/Tools/Cli/LibrisMaleficarum.Cli.csproj
```

## Validate Import Data

```bash
# Validate a folder
libris world validate --source ./samples/worlds/grimhollow

# Validate a zip file
libris world validate --source ./samples/worlds/grimhollow.zip
```

## Import a World

```bash
# Import from folder (with token via parameter)
libris world import \
  --source ./samples/worlds/grimhollow \
  --api-url https://localhost:5001 \
  --owner-id "user-123" \
  --token "your-auth-token"

# Import from folder (with token via environment variable)
export LIBRIS_API_TOKEN="your-auth-token"
libris world import \
  --source ./samples/worlds/grimhollow \
  --api-url https://localhost:5001 \
  --owner-id "user-123"

# Import from zip with verbose output
libris world import \
  --source ./samples/worlds/grimhollow.zip \
  --api-url https://localhost:5001 \
  --owner-id "user-123" \
  --verbose

# Dry-run: validate only without importing
libris world import \
  --source ./samples/worlds/grimhollow \
  --api-url https://localhost:5001 \
  --owner-id "user-123" \
  --validate-only

# Control concurrency and log to file
libris world import \
  --source ./samples/worlds/grimhollow \
  --api-url https://localhost:5001 \
  --owner-id "user-123" \
  --max-concurrency 5 \
  --log-file import-log.txt
```

## Create Import Data

### Folder structure

```text
my-world/
├── world.json                   # Required: world metadata
└── entities/                    # Entity JSON files (any subfolder layout)
    ├── continents/
    │   └── shadowlands.json
    ├── countries/
    │   └── dark-kingdom.json
    └── characters/
        └── hero-paladin.json
```

### world.json

```json
{
  "name": "My World",
  "description": "A test world for development"
}
```

### Entity JSON file

```json
{
  "localId": "shadowlands",
  "entityType": "Continent",
  "name": "The Shadowlands",
  "description": "A vast continent",
  "parentLocalId": null,
  "tags": ["continent", "main"],
  "properties": {
    "climate": "Temperate"
  }
}
```

### Child entity referencing a parent

```json
{
  "localId": "dark-kingdom",
  "entityType": "Country",
  "name": "Dark Kingdom",
  "description": "A kingdom within the Shadowlands",
  "parentLocalId": "shadowlands",
  "tags": ["country", "evil"]
}
```

## Using the Import Library Programmatically

```csharp
using LibrisMaleficarum.Import.Interfaces;
using LibrisMaleficarum.Import.Models;
using Microsoft.Extensions.DependencyInjection;

// Register import services
services.AddWorldImportServices();

// Resolve and use
var importService = serviceProvider.GetRequiredService<IWorldImportService>();

// Validate only
var validationResult = await importService.ValidateAsync("./my-world");
if (!validationResult.IsValid)
{
    foreach (var error in validationResult.Errors)
        Console.WriteLine($"[{error.FilePath}] {error.Code}: {error.Message}");
}

// Full import
var result = await importService.ImportAsync("./my-world", new ImportOptions
{
    ApiBaseUrl = "https://localhost:5001",
    OwnerId = "user-123",
    AuthToken = "your-token",
    MaxConcurrency = 10
});

Console.WriteLine($"Created {result.TotalEntitiesCreated} entities in {result.Duration}");
```

## Running Tests

```bash
# Unit tests for import library
dotnet test tests/unit/Import.Tests/LibrisMaleficarum.Import.Tests.csproj

# Unit tests for CLI
dotnet test tests/unit/Cli.Tests/LibrisMaleficarum.Cli.Tests.csproj

# All unit tests
dotnet test --solution LibrisMaleficarum.slnx --filter TestCategory=Unit
```
