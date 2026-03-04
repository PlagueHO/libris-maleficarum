# API Contracts: Player Character Entity Type

**Feature**: 014-player-character-entity
**Date**: 2026-02-18

## Overview

No new API endpoints are required. The PlayerCharacter entity type uses the existing WorldEntity CRUD endpoints. The only backend changes are:

1. Adding `PlayerCharacter` to the `EntityType` enum (enables validation)
1. Adding schema version config to `appsettings.json`

## Existing Endpoints Used

All operations go through the existing WorldEntity REST API:

### Create Player Character

```http
POST /api/v1/worlds/{worldId}/entities
Content-Type: application/json

{
  "name": "Elara Nightwhisper",
  "description": "A cunning half-elf rogue with a mysterious past",
  "entityType": "PlayerCharacter",
  "parentId": "campaign-guid-here",
  "tags": ["player", "party-member"],
  "schemaVersion": 1,
  "attributes": {
    "PlayerName": "Jane",
    "Race": "Half-Elf",
    "Class": "Rogue",
    "Subclass": "Arcane Trickster",
    "Level": 7,
    "Background": "Criminal",
    "Alignment": "Chaotic Good",
    "PersonalityTraits": "I always have a plan for when things go wrong.",
    "Ideals": "Freedom. Chains are meant to be broken.",
    "Bonds": "I owe everything to my mentor who taught me the art of theft.",
    "Flaws": "I can't resist a pretty face.",
    "Backstory": "Born on the streets of Waterdeep, Elara learned early that the only way to survive was to be clever...",
    "Appearance": "Slender build, silver hair, mismatched eyes (one green, one blue)",
    "Factions": ["Harpers", "Zhentarim"],
    "NotableEquipment": ["Cloak of Elvenkind", "Rapier +1"],
    "SignatureAbilities": ["Sneak Attack", "Mage Hand Legerdemain"],
    "Languages": ["Common", "Elvish", "Thieves' Cant"],
    "Relationships": ["Rival of Strahd", "Mentor: Elminster"]
  }
}
```

### Response

```http
HTTP/1.1 201 Created
Location: /api/v1/worlds/{worldId}/entities/{entityId}

{
  "data": {
    "id": "new-entity-guid",
    "worldId": "world-guid",
    "parentId": "campaign-guid",
    "entityType": "PlayerCharacter",
    "name": "Elara Nightwhisper",
    "description": "A cunning half-elf rogue with a mysterious past",
    "tags": ["player", "party-member"],
    "schemaVersion": 1,
    "attributes": { ... },
    "createdAt": "2026-02-18T12:00:00Z",
    "modifiedAt": "2026-02-18T12:00:00Z"
  }
}
```

### Update Player Character

```http
PUT /api/v1/worlds/{worldId}/entities/{entityId}
Content-Type: application/json

{
  "name": "Elara Nightwhisper",
  "description": "Updated description",
  "entityType": "PlayerCharacter",
  "parentId": "campaign-guid-here",
  "tags": ["player", "party-member"],
  "schemaVersion": 1,
  "attributes": {
    "Level": 8,
    ...
  }
}
```

### Read Player Character

```http
GET /api/v1/worlds/{worldId}/entities/{entityId}
```

### List Player Characters Under a Campaign

```http
GET /api/v1/worlds/{worldId}/entities?parentId={campaignId}&entityType=PlayerCharacter
```

### Delete Player Character

```http
DELETE /api/v1/worlds/{worldId}/entities/{entityId}
```

## Schema Version Contract

| Field | Value |
|-------|-------|
| Entity Type | `PlayerCharacter` |
| Schema Version | `1` |
| Min Supported (backend) | `1` |
| Max Supported (backend) | `1` |

## Validation Rules (Backend)

No additional validation beyond standard WorldEntity rules:

- `Name`: 1-200 characters, required
- `Description`: max 5000 characters, optional
- `EntityType`: must be valid enum value (`PlayerCharacter`)
- `Tags`: max 20, each 1-50 characters
- `Attributes`: max 100KB serialized JSON
- `SchemaVersion`: must be within [MinVersion, MaxVersion] range

## Frontend Registry Contract

The frontend `ENTITY_TYPE_REGISTRY` entry defines the property schema that generates the form UI. This is a frontend-only concern — the backend stores `Attributes` as opaque JSON.
