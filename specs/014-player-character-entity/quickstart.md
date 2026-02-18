# Quickstart: Player Character Entity Type

**Feature**: 014-player-character-entity
**Date**: 2026-02-18

## Prerequisites

- Node.js 20.x, pnpm
- .NET 10 SDK
- Repository cloned with branch `014-player-character-entity`

## Implementation Order

### Step 1: Backend — Add EntityType enum value

**File**: `libris-maleficarum-service/src/Domain/ValueObjects/EntityType.cs`

Add `PlayerCharacter` between `Character` and `Faction` in the "Character & faction types" group:

```csharp
/// <summary>
/// A player-controlled character (distinct from NPC Character type).
/// </summary>
PlayerCharacter,
```

### Step 2: Backend — Add schema version config

**File**: `libris-maleficarum-service/src/Api/appsettings.json`

Add to `EntitySchemaVersions.EntityTypes`:

```json
"PlayerCharacter": { "MinVersion": 1, "MaxVersion": 1 },
```

### Step 3: Backend — Run tests

```bash
cd libris-maleficarum-service
dotnet test --filter TestCategory=Unit --configuration Release --no-restore
```

### Step 4: Frontend — Add registry entry

**File**: `libris-maleficarum-app/src/services/config/entityTypeRegistry.ts`

Add `PlayerCharacter` config object in the "Characters & Factions" section, after `Character` and before `Faction`.

### Step 5: Frontend — Update parent suggestedChildren

**File**: `libris-maleficarum-app/src/services/config/entityTypeRegistry.ts`

Add `'PlayerCharacter'` to `suggestedChildren` arrays for: Campaign, Session, Faction, City, People, Folder.

### Step 6: Frontend — Add icon mapping

**Files**:
- `libris-maleficarum-app/src/lib/entityIcons.ts` — add `PlayerCharacter` to `EntityType` union and `entityIconMap`
- `libris-maleficarum-app/src/components/shared/EntityTypeSelector/EntityTypeSelector.tsx` — import `UserCheck` and add to `iconMap`

### Step 7: Frontend — Update tests

**Files**:
- `src/__tests__/services/entityTypeRegistry.test.ts` — update count from 29 to 30
- `src/__tests__/config/entityTypeRegistry.test.ts` — update count from 29 to 30, add `'PlayerCharacter'` to expected root types

### Step 8: Frontend — Run tests

```bash
cd libris-maleficarum-app
pnpm test
```

## Verification

1. All backend unit tests pass
2. All frontend tests pass (including a11y)
3. Dev server shows "Player Character" in entity type selector under "Characters & Factions"
4. Player Character has UserCheck icon, distinct from NPC Character's User icon
5. Player Character appears as suggested child under Campaign, Session, Faction, City, People, Folder
6. All 18 property fields render in the entity form
7. Entity saves and round-trips all field values correctly
