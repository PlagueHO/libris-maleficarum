# Feature Specification: Container Entity Type Support

**Feature Branch**: `004-entity-type-support`  
**Created**: 2026-01-19  
**Status**: Draft  
**Input**: User description: "Support for WorldEntity Container Entity Types and Organisational Entity Types in the WorldSidebar Entity Heirarchy and the EntityDetailForm. Ensure aligns to existing codebase & feature definition for Entity Heirarchy any Entity Details."

**Note**: After analysis, "Organizational" EntityTypes (GeographicRegion, PoliticalRegion, etc.) have no functional difference from Standard EntityTypes - both use the same schema and Properties field for custom attributes. This specification treats them as Standard EntityTypes with domain-specific properties rather than a separate category.

## Clarifications

### Session 2026-01-19

- Q: How should Container entities display in the hierarchy compared to standard entities? → A: Container entities use folder icons but maintain the same expand/collapse, indentation, and connecting line patterns as standard entities to ensure consistent UX and minimize development scope
- Q: Can custom properties (Climate, Population, Area, etc.) be edited after entity creation in this feature iteration? → A: Custom properties cannot be edited in this feature iteration; edit mode will be implemented in a future iteration, minimizing scope while delivering core creation functionality
- Q: How should text list properties (Member States, Languages, Military Assets) be entered and displayed? → A: Text lists use a reusable React tag input component (chips) with add/remove functionality, usable across all entity forms
- Q: How should EntityTypeSelector determine which types to recommend when the parent is a Container? → A: Containers recommend types based on their semantic domain (Locations→geographic types, People→character types, Events→quest types, etc.) using a predefined mapping in ENTITY_TYPE_SUGGESTIONS
- Q: What are the maximum bounds and precision requirements for numeric custom properties (Population, Area)? → A: Use JavaScript Number.MAX_SAFE_INTEGER (9,007,199,254,740,991) as maximum bound with no decimal precision for whole number fields like Population; Area allows decimals for precision

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Organize World Content with Container Types (Priority: P1)

A user creating a new TTRPG world needs to organize their content into logical top-level categories (Locations, People, Events, Lore, Items, Adventures) before populating each category with specific entities. This provides a clear mental model and reduces clutter in the root hierarchy.

**Why this priority**: Container types are the foundational organizational pattern for all world content. Without them, users face a flat root hierarchy that becomes unwieldy with dozens of mixed entity types. This is essential for basic world-building usability.

**Independent Test**: Can be fully tested by creating a new world, adding container entities (Locations, People, Events) as root-level entities, then creating specific entities (Continent, Character, Quest) as children of appropriate containers, and verifying the hierarchy displays correctly with clear visual grouping.

**Acceptance Scenarios**:

1. **Given** a user is creating a root-level entity, **When** they open the entity type selector, **Then** container types (Locations, People, Events, Lore, Items, Adventures, Geographies, History, Bestiary) appear in the recommended section at the top of the list
1. **Given** a user creates a "Locations" container, **When** they add a child entity to it, **Then** the entity type selector recommends geographic types (Continent, GeographicRegion, PoliticalRegion, Country, Region, City, Dungeon, Building, Map) first based on the semantic domain mapping, while still allowing any entity type to be selected
1. **Given** a user creates a "People" container, **When** they add a child entity, **Then** the selector recommends Character, Organization, Faction, Family, Race, Culture types first
1. **Given** a user expands a container entity in the sidebar, **When** it contains children, **Then** the container visually groups its children with clear indentation and connecting lines
1. **Given** container entities exist in the hierarchy, **When** displayed in the sidebar, **Then** containers use distinctive icons to differentiate them from standard entity types

---

### User Story 2 - Create Entities with Custom Properties (Priority: P1)

A user building a detailed geographic or political structure needs to create entities (GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion) that convey semantic meaning and contain domain-specific properties like Climate, Population, or Area, enabling rich, structured world-building beyond simple name and description.

**Why this priority**: Entity types with custom properties enable rich, multi-layered world-building with queryable data. This is a core feature for users creating complex campaigns with real-world inspired geographies and political structures.

**Independent Test**: Can be fully tested by creating a Continent entity, adding a GeographicRegion child (e.g., "Western Europe") with properties (Climate: Temperate, Population: 195000000), then adding Country entities as children, and verifying the entity appears correctly with its properties displayed in the EntityDetailForm.

**Acceptance Scenarios**:

1. **Given** a user creates a geographic entity (Continent, Country), **When** adding a child entity, **Then** regional types (GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion) appear in the recommended types list
1. **Given** a user selects GeographicRegion as the entity type, **When** the EntityDetailForm displays, **Then** additional fields for Climate, Terrain, Population, and Area are shown below the standard Name and Description fields
1. **Given** a user creates a PoliticalRegion entity, **When** filling out the form, **Then** fields for Government Type, Member States, and Established Date are available
1. **Given** a user creates a CulturalRegion, **When** filling the form, **Then** fields for Languages, Religions, and Cultural Traits are shown
1. **Given** entities with custom properties are created, **When** displayed in the sidebar, **Then** they use appropriate icons to convey their semantic type
1. **Given** these entities exist, **When** querying the hierarchy, **Then** users can filter by entity type (e.g., "Show all GeographicRegions in Europe")

---

### User Story 3 - Flexible Entity Placement Without Restrictions (Priority: P1)

A user building a creative or non-traditional world structure needs the freedom to organize entities in unconventional ways (e.g., placing a Character directly under World, or nesting a City under a Campaign for campaign-specific locations) without system-imposed restrictions, while still benefiting from smart type suggestions.

**Why this priority**: User creativity should never be blocked by rigid hierarchy rules. While recommendations help guide new users toward common patterns, experienced users must retain complete flexibility to model their unique world structures.

**Independent Test**: Can be fully tested by attempting to create unusual but valid entity relationships (Monster → Continent for a world-turtle, Locations → Character for a bag of holding city, Character → World for a god-level entity) and verifying all combinations are allowed without validation errors.

**Acceptance Scenarios**:

1. **Given** a user is creating an entity, **When** selecting an entity type, **Then** the selector shows recommended types first but allows scrolling or searching to select ANY entity type regardless of parent
1. **Given** a user creates an entity with an unrecommended parent-child combination, **When** they submit the form, **Then** the entity is created successfully without validation warnings or errors
1. **Given** a user creates a Character directly under World (bypassing containers), **When** the hierarchy loads, **Then** the Character appears as a root entity alongside any containers
1. **Given** a user nests regional types (GeographicRegion within Continent within another GeographicRegion), **When** displayed, **Then** the hierarchy renders correctly with appropriate depth indicators
1. **Given** a user places campaign-specific entities (Session, Quest) under non-Campaign parents, **When** created, **Then** the system accepts the structure without restrictions

---

### User Story 4 - Context-Aware Type Recommendations (Priority: P2)

A user adding entities to the hierarchy benefits from intelligent type suggestions based on the parent entity type, streamlining common workflows (e.g., suggesting Country when adding children to a Continent) while maintaining the ability to override suggestions for creative structures.

**Why this priority**: Smart suggestions reduce cognitive load and speed up common tasks for users building conventional hierarchies, but this is an enhancement to usability rather than core functionality.

**Independent Test**: Can be fully tested by creating entities with various parent types (Continent, City, Campaign, Character) and verifying that the entity type selector orders recommendations appropriately while still allowing access to all types.

**Acceptance Scenarios**:

1. **Given** a user is creating a child of a Continent, **When** the entity type selector opens, **Then** GeographicRegion, PoliticalRegion, Country, Region appear in the recommended section before other types
1. **Given** a user is creating a child of a City, **When** the selector opens, **Then** Building, Location, Character, Faction, Event types are recommended first
1. **Given** a user is creating a child of a Campaign, **When** the selector opens, **Then** Session, Quest, Event, Character, Location, Faction are recommended
1. **Given** a user wants to use a non-recommended type, **When** they scroll or search in the selector, **Then** all entity types remain accessible below the recommended section
1. **Given** a user creates a child under a root-level World (no parent), **When** the selector opens, **Then** container types and high-level entities (Continent, Campaign) are recommended first

---

### Edge Cases

- What happens when a user creates a deeply nested hierarchy (GeographicRegion → GeographicRegion → GeographicRegion, 5+ levels deep)?
  - **Behavior**: System supports arbitrary nesting depth up to the maximum of 10 levels defined in the data model. Visual hierarchy maintained through indentation and connecting lines. No artificial nesting restrictions.

- What happens when a user creates a container entity (e.g., "Locations") but adds non-geographic children to it (e.g., Character, Item)?
  - **Behavior**: System allows any entity type as a child of any parent, including containers. Recommendations guide common patterns but never enforce restrictions.

- What happens when entity properties (Climate, Population, Area) are left blank during creation?
  - **Behavior**: All custom properties are optional. Entity can be created with only Name (required) and Type (required). Properties can be added later via edit mode.

- What happens when a user searches for an entity type in the selector and the search term matches both container and standard types?
  - **Behavior**: Search results display all matching types grouped by category (Containers first, then Standard types), maintaining visual hierarchy.

- What happens when a container entity has no children?
  - **Behavior**: Container displays in sidebar like any entity without children (no expand/collapse indicator). Empty state message in EntityDetailForm suggests adding children when viewing the container.

- What happens when entity properties (e.g., Population) need to support very large numbers (billions) or scientific notation?
  - **Behavior**: Population and Area fields accept numeric input with appropriate formatting (e.g., 195,000,000 or 1.95e8). Field validation allows large numbers without overflow.

- What happens when a user copies entities between worlds that have different organizational structures?
  - **Behavior**: [NEEDS CLARIFICATION: Cross-world entity operations are out of scope for this feature but should be considered in future design]

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST support all Container EntityTypes defined in the data model: `Locations`, `People`, `Events`, `History`, `Lore`, `Bestiary`, `Items`, `Adventures`, `Geographies`, `Other`
- **FR-002**: System MUST support Standard EntityTypes with custom properties including: `GeographicRegion`, `PoliticalRegion`, `CulturalRegion`, `MilitaryRegion`
- **FR-003**: EntityTypeSelector component MUST include all Container and Standard types (including those with custom properties) in the dropdown options
- **FR-004**: EntityTypeSelector MUST categorize entity types into sections: Container and Standard types with clear visual separation
- **FR-005**: EntityTypeSelector MUST prioritize Container types in recommendations when creating root-level entities (no parent)
- **FR-006**: EntityTypeSelector MUST recommend relevant regional types based on parent entity type (e.g., GeographicRegion, PoliticalRegion recommended for Continent parent)
- **FR-007**: EntityTypeSelector MUST allow selection of ANY entity type regardless of parent type; recommendations are suggestions only, never restrictions
- **FR-008**: EntityDetailForm MUST display additional property fields for entity types with custom properties based on the selected type:
  - `GeographicRegion`: Climate (string), Terrain (string), Population (number), Area (number in km²)
  - `PoliticalRegion`: Government Type (string), Member States (tag input), Established Date (date)
  - `CulturalRegion`: Languages (tag input), Religions (tag input), Cultural Traits (text)
  - `MilitaryRegion`: Command Structure (string), Strategic Importance (string), Military Assets (tag input)
- **FR-008a**: System MUST provide a reusable TagInput React component (chip-based input with add/remove functionality) for all text list properties across entity forms
- **FR-009**: EntityDetailForm MUST make all custom properties optional; only Name and EntityType are required fields
- **FR-010**: EntityDetailForm MUST support numeric input formatting for large numbers in Population (integer, max: Number.MAX_SAFE_INTEGER) and Area (decimal allowed, max: Number.MAX_SAFE_INTEGER) fields with comma separators for readability (e.g., 195,000,000)
- **FR-011**: System MUST persist custom entity properties in the `Properties` field of the WorldEntity document as flexible JSON
- **FR-012**: WorldSidebar MUST use distinctive folder icons for Container entity types to differentiate them from standard entities, while maintaining the same expand/collapse, indentation, and connecting line patterns
- **FR-013**: WorldSidebar MUST use appropriate icons for entity types based on their semantic meaning (e.g., folder icons for Containers, map/globe icons for regional types, etc.)
- **FR-014**: WorldSidebar MUST display all entities with the same hierarchy navigation patterns (expand/collapse, indentation, connecting lines)
- **FR-015**: System MUST update the `ENTITY_TYPE_SUGGESTIONS` mapping in `worldEntity.types.ts` to include recommendations for Container and regional types based on parent context, including semantic domain mappings for Containers:
  - `Locations` → Continent, GeographicRegion, PoliticalRegion, Country, Region, City, Dungeon, Building, Map
  - `People` → Character, Organization, Faction, Family, Race, Culture
  - `Events` → Quest, Encounter, Battle, Festival, HistoricalEvent, CurrentEvent
  - `Lore` → Religion, Deity, MagicSystem, Artifact, Story, Myth, Legend
  - `Items` → Equipment, Weapon, Armor, Item
  - `Adventures` → Campaign, Session, Scene, Quest
  - `Geographies` → Mountain, River, Lake, Forest, Desert, Ocean, Island, ClimateZone
  - `History` → Timeline, Era, Chronicle, HistoricalEvent
  - `Bestiary` → Creature, Monster, Animal
- **FR-016**: System MUST update the `ENTITY_TYPE_META` mapping to include metadata (label, description, category) for all new Container and regional entity types
- **FR-017**: EntityTypeSelector MUST group entity types by category in the dropdown: Geography, Characters & Factions, Events & Quests, Items, Campaigns, Containers, Other
- **FR-018**: EntityTypeSelector search functionality MUST search across entity type labels, descriptions, and categories to find matching types
- **FR-019**: System MUST allow nesting of regional types (e.g., GeographicRegion within GeographicRegion) without depth restrictions up to the global maximum of 10 levels
- **FR-020**: System MUST allow Container types to contain any entity type, including other Containers or regional types
- **FR-021**: EntityDetailForm MUST validate that numeric custom properties are within valid ranges:
  - Population: non-negative integer, max Number.MAX_SAFE_INTEGER (9,007,199,254,740,991)
  - Area: non-negative number with decimal precision allowed, max Number.MAX_SAFE_INTEGER
- **FR-022**: EntityDetailForm MUST display custom entity properties in read-only mode when viewing an existing entity; editing custom properties is out of scope for this feature iteration
- **FR-023**: System MUST serialize custom entity properties to JSON and store in the `Properties` field of the WorldEntity Cosmos DB document
- **FR-024**: System MUST deserialize custom entity properties from the `Properties` JSON field when loading an entity for display or editing

### Key Entities *(include if feature involves data)*

- **Container EntityTypes**: Top-level organizational folders (Locations, People, Events, Lore, Items, Adventures, Geographies, History, Bestiary, Other) used to categorize world content by domain
  - **Purpose**: Provide high-level organization directly under World entity; reduce root-level clutter
  - **Visual Distinction**: Unique icons (folder or category symbols) to differentiate from standard entities
  - **Recommended Children**: Each container recommends relevant child types (e.g., Locations → Continent, Country, City, Dungeon, GeographicRegion)

- **Standard EntityTypes with Custom Properties**: Entity types that use the standard BaseWorldEntity schema but define additional custom properties stored in the `Properties` JSON field
  - **Examples**: GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion (as well as existing types like Continent, Country, City, Character, Quest, etc.)
  - **Custom Properties**: Domain-specific fields stored in `Properties` JSON field (Climate, Population, Area, Government Type, etc.)
  - **Visual Distinction**: Type-specific icons based on semantic meaning (map/globe for regional types, character icon for Character, etc.)
  - **Nestable**: Can contain other entities or be nested within Containers or other entity types
  - **Note**: Regional types (GeographicRegion, PoliticalRegion, CulturalRegion, MilitaryRegion) were previously categorized as "Organizational" types in the data model, but functionally they behave identically to other Standard types - just with different custom properties defined in their PropertySchema

### Data Flow

1. **EntityTypeSelector Loading**:
   - Component reads `WorldEntityType` enum from `worldEntity.types.ts` to populate dropdown options
   - Component reads `ENTITY_TYPE_META` to display labels, descriptions, and category groupings
   - Component reads `ENTITY_TYPE_SUGGESTIONS` to determine recommended types based on parent

2. **Creating Entity with Container Type or Custom Properties**:
   - User selects Container or regional type (GeographicRegion, etc.) from EntityTypeSelector
   - EntityDetailForm detects type and conditionally renders custom property fields if applicable (using TagInput component for text list properties)
   - User fills Name (required), Type (required), and optional custom properties
   - TagInput component serializes chip values as JSON string arrays (e.g., ["English", "French", "Spanish"])
   - On submit: POST `/api/v1/worlds/{worldId}/entities` with `entityType` set to Container or regional type
   - Backend stores entity with `Properties` JSON field containing custom properties (including string arrays for tag inputs)

3. **Displaying Entity with Custom Properties**:
   - GET `/api/v1/worlds/{worldId}/entities/{entityId}` returns WorldEntity document
   - Frontend deserializes `Properties` JSON field to extract custom properties (Climate, Population, etc.)
   - EntityDetailForm renders custom properties in read-only layout alongside standard fields (Name, Type, Description)

4. **Sidebar Hierarchy Display**:
   - WorldSidebar queries entities and renders hierarchy with type-specific icons
   - Icon mapping: Container types → folder/category icons, regional types → map/globe icons, Standard types → existing entity icons
   - Hierarchy navigation (expand/collapse) works identically for all entity types

### Assumptions

- Container types and regional types with custom properties are stored as standard WorldEntity documents; no separate Cosmos DB container or schema needed
- Custom properties are stored in the flexible `Properties` JSON field of WorldEntity; no schema migrations required
- Icon assets for Container and regional types are available in the Fluent UI icon library or can be sourced from existing project assets
- Custom property schemas (field definitions, validation rules) for regional types are simple enough to be defined inline in the EntityDetailForm component; dedicated PropertySchema documents (as described in DATA_MODEL.md) are out of scope for this feature
- A reusable TagInput React component will be created to handle text list properties across all entity types, storing values as JSON string arrays in the Properties field
- Users will primarily use Container types for root-level organization, though system allows any entity type at root
- Editing custom properties is out of scope for this feature iteration and will be implemented in a future update; users can only set custom properties during entity creation
- Cross-world entity operations (copy, move between worlds) are out of scope for this feature
- Advanced querying/filtering by custom properties (e.g., "Find all GeographicRegions with Population > 10M") is out of scope; future AI Search integration will enable these queries

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create Container entities (Locations, People, Events) as root-level entities and see them display in the WorldSidebar with distinctive icons within 3 seconds of form submission
- **SC-002**: Users can create entities with custom properties (GeographicRegion, PoliticalRegion) and verify properties are persisted correctly when viewing the entity detail form
- **SC-003**: Entity type selector recommends appropriate Container types when creating root-level entities, with Container types appearing in the top 3 recommended slots 90% of the time in usability testing
- **SC-004**: Entity type selector recommends appropriate regional types (GeographicRegion, PoliticalRegion) when creating children of geographic entities (Continent, Country), appearing in the top 5 recommended slots
- **SC-005**: Users can create any entity type as a child of any parent type without encountering validation errors or restrictions, verified through 20+ test cases covering unusual parent-child combinations
- **SC-006**: Entity custom properties (Climate, Population, Area, Government Type) display correctly in EntityDetailForm read-only view with appropriate formatting (e.g., Population: 195,000,000 formatted with commas)
- **SC-007**: WorldSidebar hierarchy displays Container and regional entities with correct indentation, connecting lines, and type-specific icons, verified through visual inspection across 10+ hierarchy patterns
- **SC-008**: 85% of users in usability testing correctly identify Container vs regional entity types vs other Standard types based on icon and visual distinction without requiring explanation
- **SC-009**: Entity type selector search functionality returns relevant results within 100ms for all search terms, including matches in labels, descriptions, and categories
- **SC-010**: Creating an entity with custom properties (5 fields populated) completes within 5 seconds on standard broadband connection with properties correctly stored in Cosmos DB `Properties` JSON field
