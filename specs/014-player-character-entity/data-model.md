# Data Model: Player Character Entity Type

**Feature**: 014-player-character-entity
**Date**: 2026-02-18

## Entities

### PlayerCharacter

A D&D 5th edition player character entity representing a real player's avatar in the game world. Focuses on roleplaying identity and AI content generation context rather than mechanical statistics.

**Storage**: Persisted as a `WorldEntity` document in Cosmos DB `WorldEntities` container with `EntityType = "PlayerCharacter"`. Custom properties stored in the `Attributes` JSON string field.

**Partition Key**: `/WorldId` (inherited from WorldEntity)

#### Base Fields (from WorldEntity)

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Id | GUID | Yes | Auto-generated |
| WorldId | GUID | Yes | Partition key |
| ParentId | GUID? | No | Null for root-level PCs (canBeRoot: true) |
| EntityType | string | Yes | `"PlayerCharacter"` |
| Name | string | Yes | Character name (1-200 chars) |
| Description | string? | No | Brief summary (max 5000 chars) |
| Tags | string[] | No | Categorization tags (max 20) |
| SchemaVersion | int | Yes | `1` |

#### Custom Properties (in Attributes JSON)

| Key | Label | Type | MaxLength | Description |
|-----|-------|------|-----------|-------------|
| PlayerName | Player Name | text | 100 | Real-world player controlling this character |
| Race | Race/Species | text | 100 | e.g., Human, High Elf, Half-Orc |
| Class | Class | text | 100 | e.g., Fighter, Wizard, Rogue |
| Subclass | Subclass | text | 100 | e.g., Champion, School of Evocation |
| Level | Level | integer | — | Character level (e.g., 7) |
| Background | Background | text | 100 | D&D 5e background (e.g., Sage, Noble) |
| Alignment | Alignment | text | 50 | e.g., Chaotic Good, Lawful Neutral |
| PersonalityTraits | Personality Traits | textarea | 500 | Key personality characteristics |
| Ideals | Ideals | textarea | 500 | Guiding principles and beliefs |
| Bonds | Bonds | textarea | 500 | Connections to people, places, or events |
| Flaws | Flaws | textarea | 500 | Weaknesses and vulnerabilities |
| Backstory | Backstory | textarea | 2000 | Narrative history and origin |
| Appearance | Appearance | textarea | 500 | Physical description |
| Factions | Factions | tagArray | 50/tag | Multiple faction affiliations |
| NotableEquipment | Notable Equipment | tagArray | 50/tag | Signature gear and items |
| SignatureAbilities | Signature Abilities | tagArray | 50/tag | Key abilities and spells |
| Languages | Languages | tagArray | 50/tag | Known languages |
| Relationships | Relationships | tagArray | 50/tag | Connections to other characters |

#### Relationships

| Relationship | Target | Via | Notes |
|-------------|--------|-----|-------|
| Parent | Any entity with `PlayerCharacter` in suggestedChildren | `ParentId` | Campaign, Session, Faction, City, People, Folder |
| Children | Item, Quest | `suggestedChildren` | Carried gear, personal quests |
| Root | World (top-level) | `canBeRoot: true` | Can exist without parent |

#### State Transitions

None — PlayerCharacter follows the standard WorldEntity lifecycle (create → update → soft-delete → restore/purge).

#### Validation Rules

- All custom property fields are optional (entity only requires base `Name`)
- Level accepts any integer (no min/max — homebrew flexibility)
- Backstory limited to 2000 characters
- Tag arrays limited to 50 characters per tag (inherited from tagArray type)
- Text fields limited by their configured `maxLength`

## Registry Configuration

### Frontend (entityTypeRegistry.ts)

```typescript
{
  type: 'PlayerCharacter',
  label: 'Player Character',
  description: 'A player-controlled D&D 5e character with roleplaying attributes',
  category: 'Characters & Factions',
  icon: 'UserCheck',
  schemaVersion: 1,
  suggestedChildren: ['Item', 'Quest'],
  canBeRoot: true,
  propertySchema: [/* 18 fields as listed above */],
}
```

### Backend (EntityType.cs)

```csharp
/// <summary>
/// A player-controlled character (distinct from NPC Character type).
/// </summary>
PlayerCharacter,
```

### Backend (appsettings.json)

```json
"PlayerCharacter": { "MinVersion": 1, "MaxVersion": 1 }
```

## Parent Entity Updates

The following entity types must add `'PlayerCharacter'` to their `suggestedChildren`:

| Entity Type | Current suggestedChildren | Change |
|-------------|--------------------------|--------|
| Campaign | Session, Quest, Event, Character, Location, Faction | + PlayerCharacter |
| Session | Event, Location | + PlayerCharacter |
| Faction | Character, Location, Event, Quest | + PlayerCharacter |
| City | Building, Location, Character, Faction, Event | + PlayerCharacter |
| People | Character, Faction | + PlayerCharacter |
| Folder | Folder, Continent, Country, ..., Item | + PlayerCharacter |
