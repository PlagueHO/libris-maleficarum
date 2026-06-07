<!-- markdownlint-disable-file -->
# Research: WorldEntity Data Model & Entity-Type Registry (Search Results Panel)

## Research Topics / Questions

1. Full WorldEntity data model: every field, Properties vs SystemProperties split, partition keys, GUID IDs, soft delete, parent/child hierarchy, Type, Name, Summary/Description, tags, timestamps, recently-edited/opened fields.
2. Full list of World Entity types from `registries/entity-types.json` + schema — identifiers, display names, icons, metadata.
3. Entity-type registry system (`docs/design/registry_architecture.md`, `docs/design/schema_version_matrix.md`).
4. Representative WorldEntity sample JSON from `samples/worlds/`.
5. Which fields to display in a search result row (name, type, snippet/summary, breadcrumb/path, tags, last modified) — exact field names.
6. How hierarchy position is represented (ParentId, path, ancestors) so the frontend can expand the tree to a found entity.
7. Which fields are/should be indexed for search vs. returned in results vs. needed for navigation.

Status: **Complete**

---

## Authoritative Source

`docs/design/data_model.md` is the source of truth for persistence naming and field shapes. The persisted Cosmos DB JSON contract uses **camelCase** field names (data_model.md lines 18-37). C# model properties may be PascalCase, but persisted document fields and SQL predicates must be camelCase (`c.worldId`, `c.parentId`, `c.isDeleted`).

> [!IMPORTANT]
> Several code samples inside data_model.md (Asset/WorldEntity JSON examples around lines 600-720) use PascalCase field names (e.g. `WorldId`, `ParentId`, `IsDeleted`). These contradict the camelCase Naming Standard stated at the top of the same doc and the actual frontend TypeScript interface. **Treat camelCase as correct** (matches Naming Standard table data_model.md lines 22-29 and `worldEntity.types.ts`). The PascalCase JSON examples are stale.

---

## 1. WorldEntity Data Model (camelCase persistence contract)

### Containers (data_model.md lines 56-62)

| Container | Purpose | Partition Key |
| --------- | ------- | ------------- |
| `World` | World/campaign metadata (top-level) | `/id` |
| `WorldEntity` | World entity hierarchy (all entities in a world) | `/worldId` |
| `Asset` | Asset metadata (images/audio/etc.) | `[/worldId, /entityId]` (hierarchical) |

Note: `World` is a SEPARATE document/container from `WorldEntity`. The "World" is NOT itself a `WorldEntity` document and is NOT defined in `registries/entity-types.json`. Root entities in a world have `parentId == worldId` (see hierarchy section).

### BaseWorldEntity fields (data_model.md lines 207-264, C# `BaseWorldEntity` record)

Persisted camelCase field — C# property name — type — notes:

- `id` — Id — string (GUID) — **REQUIRED**. Unique entity identifier. Point-read key (with `worldId`).
- `worldId` — WorldId — string (GUID) — **REQUIRED**. Root world / tenant ID. **Partition key.**
- `parentId` — ParentId — string (GUID) | null — **REQUIRED** (nullable). Parent entity ID; null for root World (in TS it is `string | null`, set to `worldId` for root entities per samples/queries).
- `entityType` — EntityType — string — **REQUIRED**. Discriminator (e.g. `"Continent"`, `"Character"`). Matches a `type` in the Entity Type Registry.
- `schemaId` — SchemaId — string? — Optional. References property/system schema (e.g. `"dnd5e-character"`, `"fantasy-location"`, `"geographic-region"`). Drives `systemProperties` validation.
- `path` — Path — `List<Guid>?` / `string[]` — Ancestor entity IDs from root to parent (for hierarchy ops). **Key for breadcrumb/tree-expand.** (See discrepancy note below — data_model says GUID ancestor IDs; some JSON samples store names.)
- `depth` — Depth — int — **REQUIRED**. Hierarchy depth (0 = root).
- `name` — Name — string — **REQUIRED**. Display name (indexed). Primary search-result title.
- `description` — Description — string? — Optional rich text description. **This is the closest field to a "summary/snippet"** — there is NO separate `summary`/`title` field.
- `tags` — Tags — `List<string>?` / `string[]` — User-defined tags for search/filtering.
- `ownerId` — OwnerId — string — **REQUIRED**. Owner user ID.
- `createdBy` — CreatedBy — string? — Optional. User who created.
- `modifiedBy` — ModifiedBy — string? — Optional. User who last modified.
- `createdAt` — CreatedAt — DateTime (ISO 8601) — **REQUIRED**. Creation timestamp.
- `updatedAt` — UpdatedAt — DateTime (ISO 8601) — **REQUIRED**. **Last-modified timestamp → use for "last modified" / sort.**
- `schemaVersion` — SchemaVersion — int — **REQUIRED**. 1-based property-schema version. Lazy migration on save.
- `isDeleted` — IsDeleted — bool — **REQUIRED**. Soft-delete flag (default false). All search/list queries filter `isDeleted = false`.
- `deletedDate` — DeletedDate — DateTime? — When deleted.
- `deletedBy` — DeletedBy — string? — Who deleted.
- `ttl` — Ttl — int? — Cosmos reserved TTL seconds. Set to 7776000 (90 days) on soft delete for auto-purge.
- `properties` — Properties — object? — **Flexible bag**: baseline entity-type fields, cross-game-system (e.g. `population`, `climate`, `level`). Governed by Entity Type Registry `propertySchema`.
- `systemProperties` — SystemProperties — object? — **Flexible bag**: game-system/ruleset-specific fields (e.g. D&D 5e `armorClass`). Governed by Game System Registry (planned).

### Frontend WorldEntity interface (libris-maleficarum-app/src/services/types/worldEntity.types.ts lines 50-104)

The frontend TypeScript interface confirms camelCase and adds one field NOT in data_model.md:

- `id`, `worldId`, `parentId` (`string | null`), `entityType`, `name`, `description?`, `tags: string[]`, `path: string[]`, `depth: number`, `ownerId`, `createdAt`, `updatedAt`, `isDeleted`, `schemaId?`, `properties?`, `systemProperties?`, `schemaVersion`.
- **`hasChildren: boolean`** (worldEntity.types.ts line 78) — "Optimization flag: skip children query if false." **This is the field the search panel should use to decide whether a found entity is expandable in the tree.** It is present in the frontend contract but absent from the data_model.md BaseWorldEntity schema (gap to flag).
- Frontend doc comments note `description` max 500 chars (worldEntity.types.ts line 67) vs data_model "rich text"; and `path` is `string[]` of ancestor IDs (worldEntity.types.ts line 75).

> [!NOTE]
> There is NO "recently opened/viewed/accessed" field anywhere in the data model, the TS types, or samples (grep for `recently|lastOpened|lastViewed|lastAccessed|openedAt|viewedAt` found only unrelated UI code). "Recently edited" must be derived from `updatedAt` (sort DESC). "Recently opened" would require new client-side or server-side tracking — does not exist today.

### Properties vs SystemProperties contract (data_model.md lines 285-296)

- `properties` = baseline fields expected for an entity type regardless of game system/ruleset (e.g. `population` on `City`/`Continent`). Governed by `registries/entity-types.json` → `propertySchema`.
- `systemProperties` = ruleset-specific fields (e.g. D&D 5e character mechanics). Governed by the (planned) Game System Registry.
- Same `entityType` can carry different `systemProperties` per world while `properties` stays stable.
- `schemaId` selects which system schema applies to `systemProperties`. When null/absent, only the Entity Type Registry `propertySchema` applies and `systemProperties` is unvalidated free-form JSON (registry_architecture.md lines 316-320).

---

## 2. Full Entity Type List (registries/entity-types.json)

31 entity types. Format: `type` (identifier, PascalCase) — `label` (display) — `icon` (Lucide icon name) — `category` — flags. Line numbers are in `registries/entity-types.json`.

### Category: Geography

- `Continent` — "Continent" — icon `Globe` — canBeRoot — (line 3)
- `Country` — "Country" — icon `Map` — (line 61)
- `Region` — "Region" — icon `MapPin` — (line 138)
- `City` — "City" — icon `Building` — (line 199)
- `Building` — "Building" — icon `Building` — (line 268)
- `Room` — "Room" — icon `Home` — (line 335)
- `Location` — "Location" — icon `MapPin` — (line 394)
- `GeographicRegion` — "Geographic Region" — icon `Globe` — (line 1277)
- `PoliticalRegion` — "Political Region" — icon `Shield` — (line 1322)
- `CulturalRegion` — "Cultural Region" — icon `Users` — (line 1359)
- `MilitaryRegion` — "Military Region" — icon `Shield` — (line 1396)

### Category: Characters & Factions

- `Character` — "Character" — icon `User` — (line 462)
- `PlayerCharacter` — "Player Character" — icon `UserCheck` — canBeRoot — (line 549)
- `Faction` — "Faction" — icon `Users` — (line 695)

### Category: Events & Quests

- `Event` — "Event" — icon `Calendar` — (line 778)
- `Quest` — "Quest" — icon `Scroll` — (line 844)

### Category: Items

- `Item` — "Item" — icon `Package` — (line 924)

### Category: Campaigns

- `Campaign` — "Campaign" — icon `Scroll` — canBeRoot — (line 991)
- `Session` — "Session" — icon `Calendar` — (line 1061)

### Category: Containers (organizational; all canBeRoot)

- `Folder` — "Folder" — icon `Folder` — (line 1133)
- `Locations` — "Locations" — icon `FolderOpen` — (line 1157)
- `People` — "People" — icon `Users` — (line 1176)
- `Events` — "Events" — icon `CalendarDays` — (line 1190)
- `History` — "History" — icon `BookOpen` — (line 1203)
- `Lore` — "Lore" — icon `BookMarked` — (line 1215)
- `Bestiary` — "Bestiary" — icon `Bug` — (line 1227)
- `Items` — "Items" — icon `Box` — (line 1239)
- `Adventures` — "Adventures" — icon `Compass` — (line 1251)
- `Geographies` — "Geographies" — icon `Mountain` — (line 1265)

### Category: Other

- `Other` — "Other" — icon `HelpCircle` — canBeRoot — (line 1431)

### Icon system

- `icon` values are **Lucide React icon component names** (registry_architecture.md line 87: "Lucide icon component name"). The search panel should map the entity's `entityType` → registry `icon` string → Lucide icon component. There are NO image/asset icon references in the registry; icons are font/component icons.
- A separate per-entity image asset can exist via the `Asset` container (e.g. character portrait, location map) but is NOT referenced by a field on the WorldEntity in the camelCase contract. (Stale PascalCase JSON examples in data_model.md show `MapAssetId`/`IconAssetId`, and the lazy-load query example selects `IconAssetId` (data_model.md line ~470), but these fields are NOT in the BaseWorldEntity schema or the TS interface — treat as aspirational/not-implemented.)

### EntityTypeConfig structure (registry_architecture.md lines 70-93)

```typescript
interface EntityTypeConfig {
  readonly type: string;          // PascalCase identifier → WorldEntityType union member
  readonly label: string;         // Human-readable display name
  readonly description: string;   // UI hint text
  readonly category: EntityTypeCategory;
  readonly icon: string;          // Lucide icon component name
  readonly schemaVersion: number; // Current propertySchema version
  readonly suggestedChildren: readonly string[]; // PascalCase entityType values (UI create selector)
  readonly canBeRoot?: boolean;
  readonly propertySchema?: readonly PropertyFieldSchema[];
}

interface PropertyFieldSchema {
  readonly key: string;    // camelCase — Cosmos property-bag key
  readonly label: string;
  readonly type: PropertyFieldType; // text|textarea|integer|decimal|tagArray|date|datetime|time
  readonly placeholder?: string;
  readonly description?: string;
  readonly maxLength?: number;
  readonly validation?: PropertyFieldValidation;
}
```

---

## 3. Entity-Type Registry System

- Canonical data: `registries/entity-types.json`
- Canonical schema: `schemas/registries/entity-type-registry.schema.json`
- Generator: `libris-maleficarum-app/scripts/generate-registry.mjs` (run via `pnpm gen:registry`)
- Generated runtime: `libris-maleficarum-app/src/services/config/entityTypeRegistry.generated.ts`
- Typed facade: `libris-maleficarum-app/src/services/config/entityTypeRegistry.ts` (exports `ENTITY_TYPE_REGISTRY`)
- Backend (planned): `libris-maleficarum-service/src/Domain/EntityTypes/EntityTypeRegistry.cs`

Derived constants generated in `worldEntity.types.ts` (registry_architecture.md lines 134-141): `WorldEntityType` (union + const map), `ENTITY_SCHEMA_VERSIONS` (`type → schemaVersion`), `ENTITY_TYPE_META` (`type → full config`), `ENTITY_TYPE_SUGGESTIONS` (`type → suggestedChildren`). **`ENTITY_TYPE_META[entityType].icon` and `.label` are the fastest path for the search panel to resolve display icon + label.**

Game System Registry (separate, mostly planned) governs `systemProperties` by `{systemId}-{entityType}` `schemaId` (registry_architecture.md lines 252-320).

`schemaVersion` is per-entity-type and is validated on CREATE/UPDATE (schema_version_matrix.md): frontend always sends explicit version; backend validates `MIN ≤ v ≤ MAX`, rejects downgrades and future versions; lazy migration upgrades on save. Not directly relevant to search display, but `schemaVersion` is a required field on every entity.

---

## 4. Representative Sample WorldEntity JSON

> [!IMPORTANT]
> `samples/worlds/grimhollow/` uses the **import/export format**, which is NOT the persisted Cosmos shape. Import docs use `localId` / `parentLocalId` (human-friendly slugs resolved to GUIDs on import) and OMIT server-assigned fields (`id`, `worldId`, `depth`, `path`, `ownerId`, timestamps, `isDeleted`, `schemaVersion`). Also note sample `properties` values are strings (e.g. `"level": "18"`) rather than typed numbers — import normalization/typing happens server-side.

`samples/worlds/grimhollow/world.json` (world metadata only):

```json
{
  "name": "Grimhollow",
  "description": "A dark fantasy world shrouded in perpetual twilight..."
}
```

`samples/worlds/grimhollow/entities/continents/grimhollow-continent.json` (import shape):

```json
{
  "localId": "grimhollow-continent",
  "entityType": "Continent",
  "name": "The Grimhollow Continent",
  "description": "A vast landmass scarred by the Sundering War...",
  "parentLocalId": null,
  "tags": ["dark-fantasy", "twilight-realm", "war-torn", "ancient-evil"],
  "properties": {
    "climate": "Perpetual twilight with ashen skies...",
    "population": "Approximately 2.3 million souls, declining",
    "knownThreats": "The Hollow — a creeping void...",
    "dominantReligion": "The Old Covenant of the Five Silent Gods"
  }
}
```

`samples/worlds/grimhollow/entities/characters/king-aldric.json` (import shape, note `parentLocalId` → "ironhold" city):

```json
{
  "localId": "king-aldric",
  "entityType": "Character",
  "name": "King Aldric the Ironheart",
  "description": "The aging but indomitable ruler of the Iron Dominion...",
  "parentLocalId": "ironhold",
  "tags": ["royalty", "warrior-king", "paranoid", "iron-dominion"],
  "properties": { "class": "Fighter (Champion)", "level": "18", "alignment": "Lawful Neutral", "race": "Human" }
}
```

### Reconstructed PERSISTED (Cosmos, camelCase) shape — what search returns

Based on the BaseWorldEntity contract + frontend interface, a persisted `WorldEntity` looks like:

```json
{
  "id": "c9d8e7f6-5a4b-3c2d-1e0f-9a8b7c6d5e4f",
  "worldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "parentId": "a1b2c3d4-e5f6-7a8b-9c0d-1e2f3a4b5c6d",
  "entityType": "Character",
  "schemaId": "dnd5e-character",
  "name": "King Aldric the Ironheart",
  "description": "The aging but indomitable ruler of the Iron Dominion...",
  "tags": ["royalty", "warrior-king", "paranoid", "iron-dominion"],
  "path": ["<continentId>", "<countryId>", "<regionId>", "<cityId>"],
  "depth": 5,
  "hasChildren": false,
  "ownerId": "user-abc",
  "createdBy": "user-abc",
  "modifiedBy": "user-abc",
  "createdAt": "2024-06-01T10:00:00Z",
  "updatedAt": "2024-06-15T14:30:00Z",
  "schemaVersion": 2,
  "isDeleted": false,
  "properties": { "class": "Fighter (Champion)", "level": 18, "alignment": "Lawful Neutral", "race": "Human" },
  "systemProperties": { "armorClass": 18, "hitPoints": { "current": 145, "max": 145 } }
}
```

Grimhollow hierarchy (README.md) for tree-expand testing:

```text
Grimhollow (World)
└── The Grimhollow Continent (Continent)
    └── The Iron Dominion (Country)
        └── Blackmoor Marshes (Region)
            └── Ironhold (City)
                ├── Ironhold Castle (Building)
                ├── King Aldric the Ironheart (Character)
                └── The Shadow Assassin (Character)
```

---

## 5. Fields to Display in a Search Result Row

Exact persisted (camelCase) field names:

| Result-row element | Field | Notes |
| ------------------ | ----- | ----- |
| Icon | `entityType` → `ENTITY_TYPE_META[entityType].icon` (Lucide name) | No per-entity icon field; resolve via registry. |
| Type badge/label | `entityType` → `ENTITY_TYPE_META[entityType].label` | e.g. `"Player Character"`. |
| Title | `name` | Primary heading; 1-100 chars. |
| Snippet / summary | `description` | No dedicated summary field; truncate `description`. Optionally surface matched `properties`/`tags`. |
| Breadcrumb / path | `path` (ancestor IDs) + each ancestor's `name` | `path` holds ancestor IDs only; ancestor names must be resolved (batch fetch or denormalize). `depth` gives nesting level. |
| Tags | `tags` | string array → badge list. |
| Last modified | `updatedAt` | ISO 8601; also use for "recently edited" sort (DESC). |
| Created | `createdAt` | Optional secondary timestamp. |
| Expandable indicator | `hasChildren` | Frontend-only flag (see gap). |

---

## 6. Hierarchy / Parent Representation (for tree-expand-to-entity)

- `parentId` (camelCase) — direct parent entity ID. Root entities: `parentId == worldId` (data_model.md line 191: "null for root World"; but lazy-load query and frontend treat root as `parentId == worldId`; TS allows `string | null`). **Discrepancy to confirm — see gaps.**
- `path` — ordered array of ancestor entity IDs from root → parent. **This is the primary mechanism to expand the tree to reveal a found entity:** iterate `path` IDs, expanding/loading each node in order, then select the entity by `id`. data_model.md declares `List<Guid>` (IDs); the import/JSON samples in data_model.md show name strings in `Path` (e.g. `["Eldoria","Arcanis"]`) — **the authoritative contract is ancestor IDs** (matches `worldEntity.types.ts` "Array of ancestor IDs from root to parent").
- `depth` — integer nesting level (0 = root). Use for indentation and to validate `path.length`.
- Lazy-load children query (data_model.md lines 367-388): `WHERE worldId == @worldId AND parentId == @parentId AND !isDeleded ORDER BY name`. Children endpoint: `GET worlds/{worldId}/entities/{parentId}/children`. Cost ~2-5 RUs per node expansion.
- `hasChildren` (frontend flag) lets the panel show/hide the expand affordance without an extra children query.

Tree-expand-to-found-entity algorithm:

1. From the search result, read `worldId`, `id`, and `path` (ancestor IDs root→parent).
2. For each ancestor ID in `path` (in order), ensure that node is loaded/expanded (call children endpoint on its parent if not cached).
3. After the parent (`parentId`) is expanded, scroll to and select the entity by `id`.

---

## 7. Indexing: Search vs. Results vs. Navigation

- **External index**: All WorldEntities are indexed by **Azure AI Search** for semantic ranking, hybrid search, and cross-world discovery (data_model.md lines 184, "External Indexing"). Cosmos also supports in-partition name/tag search (`Search entities` op, 5-14 RUs, data_model.md line ~100).
- **Searchable content (should be indexed)**: `name` (explicitly "indexed"), `tags`, `description`, and likely `properties`/`systemProperties` values for full-text/semantic search. The Cosmos "Search entities" operation is documented as "Name/tag search with isDeleted filter."
- **Always filter**: `isDeleted = false` on every list/search/children query.
- **Returned in search-result rows**: `id`, `worldId`, `parentId`, `entityType`, `name`, `description`, `tags`, `updatedAt`, `path`, `depth`, `hasChildren`. (Lazy-load projection example `EntityNode` returns `id`, `name`, `entityType` — minimal; the search panel needs more, listed above.)
- **Needed for navigation / opening**: `id` + `worldId` (point-read key, 1 RU), `parentId` and `path` (tree expand), `entityType` (icon/label + routing). Point read: `WHERE worldId == @worldId AND id == @entityId` (data_model.md "Get entity by ID", 1 RU).

---

## Key Discoveries (Evidence Summary)

1. Persistence is **camelCase**; PascalCase JSON examples in data_model.md are stale. Authoritative camelCase confirmed by Naming Standard table (data_model.md lines 22-29) + `worldEntity.types.ts`.
2. **No `summary`/`title` field** — use `description` for snippet, `name` for title.
3. **No "recently opened/viewed" field** — derive "recently edited" from `updatedAt`. "Recently opened" needs net-new tracking.
4. **`hasChildren`** exists only in the frontend TS interface (worldEntity.types.ts line 78), not in data_model.md BaseWorldEntity — use it for expandability but flag the contract gap.
5. **`path`** = ancestor entity IDs (authoritative) — primary tree-expand mechanism; `depth` = nesting level.
6. **Icons are Lucide component names** from `registries/entity-types.json` `icon`; resolve via `ENTITY_TYPE_META[entityType].icon`. No per-entity icon field in the contract.
7. 31 entity types across 7 categories (Geography, Characters & Factions, Events & Quests, Items, Campaigns, Containers, Other). `World` is a separate document, not in the registry.
8. `properties` (entity-type baseline) vs `systemProperties` (game-system ruleset) split governed by two registries; `schemaId` selects the system schema.

---

## Gaps / Clarifying Questions

1. **`hasChildren` contract gap**: present in frontend `WorldEntity` (worldEntity.types.ts line 78) but absent from data_model.md BaseWorldEntity. Is it persisted, computed server-side per query, or derived client-side? Affects whether the search API returns it.
2. **Root `parentId` semantics**: data_model.md says `parentId` is null for root World, but lazy-load query/frontend treat root entities as `parentId == worldId`, and TS type is `string | null`. Confirm canonical root representation for correct tree-expand at depth 0.
3. **`path` content**: declared as ancestor GUIDs but a data_model.md JSON example stores ancestor names. Confirm IDs (assumed) so breadcrumb resolution fetches names by ID.
4. **Search result field set**: Is the search API (Azure AI Search) projection defined anywhere? The only documented projection is the minimal `EntityNode` (`id`, `name`, `entityType`). Need confirmation that `description`, `tags`, `updatedAt`, `path`, `depth`, `hasChildren` are returned in search hits, or whether a follow-up point-read is required per result.
5. **Stale asset-icon fields** (`IconAssetId`, `MapAssetId`) appear in data_model.md examples/queries but not in the contract. Confirm there is no entity-level icon/portrait reference field (relevant if search rows should show entity images instead of Lucide icons).
6. **Backend not implemented**: `libris-maleficarum-service` WorldEntity API/repository exists per docs but the search endpoint shape should be verified against actual code before finalizing the panel's data contract.

---

## Recommended Next Research (not done this session)

- [ ] Inspect backend `libris-maleficarum-service/src/` (Domain `WorldEntity.cs`, Infrastructure `WorldEntityRepository`/`WorldEntityConfiguration.cs`, Api controllers) to confirm persisted field names, `hasChildren`, and root `parentId` handling.
- [ ] Locate the Azure AI Search index definition / search endpoint and document the exact search-hit projection.
- [ ] Read `docs/design/api.md` for the search/children/get-entity REST contracts the panel will call.
- [ ] Inspect `entityTypeRegistry.generated.ts` and `ENTITY_TYPE_META` usage to confirm icon-resolution helper available to the search panel.
- [ ] Verify import pipeline (`libris world import`) to confirm how `localId`/`parentLocalId` map to persisted `id`/`parentId`/`path`/`depth`.
