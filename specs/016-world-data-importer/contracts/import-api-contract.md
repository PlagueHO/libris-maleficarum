# Import API Client Contract

**Feature Branch**: `016-world-data-importer`
**Date**: 2026-03-10

## Overview

The import tool interacts with the existing backend REST API via the shared `LibrisMaleficarum.Api.Client` SDK. This document specifies the exact API calls the SDK makes on behalf of the import library. No new API endpoints are required — the import tool uses the existing World and WorldEntity CRUD endpoints.

## API Endpoints Used

### Create World

```http
POST /api/v1/worlds
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "The Realm of Shadows",
  "description": "A dark fantasy world for D&D 5e campaigns"
}
```

**Response** (201 Created):

```json
{
  "data": {
    "id": "a1b2c3d4-...",
    "ownerId": "user-123",
    "name": "The Realm of Shadows",
    "description": "A dark fantasy world for D&D 5e campaigns",
    "createdDate": "2026-03-10T12:00:00Z",
    "modifiedDate": "2026-03-10T12:00:00Z"
  }
}
```

**Error Responses**:

- 400 Bad Request — Validation errors in request body
- 401 Unauthorized — Missing or invalid token

### Create World Entity

```http
POST /api/v1/worlds/{worldId}/entities
Authorization: Bearer {token}
Content-Type: application/json

{
  "name": "The Shadowlands",
  "description": "A vast continent shrouded in perpetual twilight",
  "entityType": "Continent",
  "parentId": null,
  "tags": ["dark", "continent", "main"],
  "attributes": {
    "climate": "Temperate-Dark",
    "population": "Unknown"
  },
  "schemaVersion": 1
}
```

**Response** (201 Created):

```json
{
  "data": {
    "id": "b2c3d4e5-...",
    "worldId": "a1b2c3d4-...",
    "parentId": null,
    "entityType": "Continent",
    "name": "The Shadowlands",
    "description": "A vast continent shrouded in perpetual twilight",
    "tags": ["dark", "continent", "main"],
    "path": [],
    "depth": 0,
    "hasChildren": false,
    "ownerId": "user-123",
    "attributes": {
      "climate": "Temperate-Dark",
      "population": "Unknown"
    },
    "createdDate": "2026-03-10T12:00:01Z",
    "modifiedDate": "2026-03-10T12:00:01Z",
    "isDeleted": false,
    "schemaVersion": 1
  }
}
```

**Error Responses**:

- 400 Bad Request — Validation errors
- 401 Unauthorized — Missing or invalid token
- 403 Forbidden — User doesn't own the world
- 404 Not Found — World doesn't exist

## CLI Command Contract

### `libris world import`

Imports a world from a folder or zip archive into the backend API.

```text
libris world import --source <path> --api-url <url> --owner-id <id> [options]

Arguments:
  (none)

Options:
  --source <path>           Required. Path to import folder or zip file.
  --api-url <url>           Required. Backend API base URL (e.g., https://localhost:5001).
  --owner-id <id>           Required. Owner identity for the created world and entities.
  --token <token>           Authentication token. Falls back to LIBRIS_API_TOKEN env var.
  --validate-only           Validate import data without making API calls.
  --verbose                 Enable detailed output during import.
  --max-concurrency <n>     Max parallel API calls per hierarchy level. Default: 10.
  --log-file <path>         Write detailed import log to file.
  -h, --help                Show help information.
```

**Exit codes**:

| Code | Meaning |
|------|---------|
| 0 | Success — all entities imported |
| 1 | Partial failure — some entities failed |
| 2 | Total failure — no entities imported |
| 3 | Validation failure — errors found in validate-only mode |

**Output format** (success):

```text
Import completed successfully.

  World:     The Realm of Shadows (a1b2c3d4-...)
  Duration:  12.3s

  Entities created: 27
    Continent:  2
    Country:    4
    Region:     6
    City:       8
    Character:  5
    Campaign:   2
```

**Output format** (partial failure):

```text
Import completed with errors.

  World:     The Realm of Shadows (a1b2c3d4-...)
  Duration:  15.7s

  Entities created: 23
  Entities failed:  2
  Entities skipped: 4 (descendants of failed entities)

  Errors:
    [shadowlands/dark-citadel.json] dark-citadel: API returned 400 Bad Request - Invalid entity type
      Skipped descendants: dark-citadel-throne, dark-citadel-dungeon, ...

    [shadowlands/lost-village.json] lost-village: Connection timeout after 30s
      Skipped descendants: lost-village-elder
```

### `libris world validate`

Validates import data without making API calls.

```text
libris world validate --source <path> [options]

Options:
  --source <path>           Required. Path to import folder or zip file.
  --verbose                 Show detailed validation output.
  -h, --help                Show help information.
```

**Exit codes**:

| Code | Meaning |
|------|---------|
| 0 | Validation passed — all data is valid |
| 3 | Validation failed — errors found |

**Output format** (valid):

```text
Validation passed.

  World:     The Realm of Shadows
  Entities:  27 across 6 types
    Continent:  2
    Country:    4
    Region:     6
    City:       8
    Character:  5
    Campaign:   2
  Max depth: 3
```

**Output format** (invalid):

```text
Validation failed with 3 errors.

  Errors:
    [entities/bad-entity.json] ENTITY_MISSING_NAME: Entity file missing required 'name' field
    [entities/duplicate.json] ENTITY_DUPLICATE_LOCAL_ID: Duplicate localId 'shadowlands' (also in entities/continents/shadowlands.json)
    [entities/orphan.json] ENTITY_DANGLING_PARENT: parentLocalId 'nonexistent' does not match any localId
```

## Import File Format Contract

### world.json (required, at root)

```json
{
  "name": "string (required, 1-100 chars)",
  "description": "string (optional, max 2000 chars)"
}
```

### Entity JSON files (any subfolder)

```json
{
  "localId": "string (required, unique within import)",
  "entityType": "string (required, valid EntityType enum value)",
  "name": "string (required, 1-200 chars)",
  "description": "string (optional, max 5000 chars)",
  "parentLocalId": "string (optional, references another localId, null = root-level entity)",
  "tags": ["string (optional, max 20 items, each max 50 chars)"],
  "properties": { "key": "value (optional, max 100KB serialized)" }
}
```

### Valid EntityType Values

Matches the `LibrisMaleficarum.Domain.Entities.EntityType` enum:

- Geographic: `Continent`, `Country`, `Region`, `City`, `Building`, `Room`, `Location`, `GeographicRegion`, `PoliticalRegion`, `MilitaryRegion`
- Character: `Character`, `PlayerCharacter`, `Faction`
- Content: `Event`, `Quest`, `Item`, `Campaign`, `Session`
- Container: `Folder`, `Locations`, `People`, `Events`, `History`, `Lore`, `Bestiary`, `Items`, `Adventures`, `Geographies`

### Folder Structure Example

```text
my-world/
├── world.json                          # World metadata (required)
├── entities/                           # Entity files (any organization)
│   ├── continents/
│   │   ├── shadowlands.json
│   │   └── brightlands.json
│   ├── countries/
│   │   ├── dark-kingdom.json
│   │   └── light-republic.json
│   ├── characters/
│   │   ├── hero-paladin.json
│   │   └── villain-necromancer.json
│   └── campaigns/
│       └── shadow-war.json
└── README.md                           # Ignored (non-JSON)
```
