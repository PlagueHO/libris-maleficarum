# Feature Specification: Add Player Character (PC) Entity Type

**Feature Branch**: `014-player-character-entity`
**Created**: 2026-02-18
**Status**: Draft
**Input**: User description: "Add a Player Character (PC) to the Entity Registry that can be used to represent a D&D 5th edition player character. It should be able to collect the core elements of D&D 5th edition characters that would be required to assist in using AI in this application to generate new content. The form should be able to connect the key elements of a player character contributing to role playing, but not requiring things like ability scores. In future I will add the ability to import this from D&D Beyond using the D&D beyond API."

## Clarifications

### Session 2026-02-18

- Q: Should Personality Traits, Ideals, Bonds, and Flaws be four separate textarea fields or a single combined field? → A: Four separate textarea fields (matches D&D 5e character sheet layout and enables AI to reference individual personality dimensions)
- Q: Should the Faction field be a single text field (one primary faction) or a tagArray (multiple factions)? → A: TagArray for multiple faction affiliations (characters often belong to multiple factions simultaneously; enables AI to see all organizational ties)
- Q: What should the maximum character length for the Backstory textarea field be? → A: 2000 characters (sufficient for rich backstory providing strong AI context without allowing unbounded input)
- Q: Should PlayerCharacter be a root-level entity (canBeRoot: true) or only exist as a child of another entity? → A: Yes, canBeRoot: true — PCs can exist at the top level of a world

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create a Player Character Entity (Priority: P1)

As a **dungeon master or player**, when I am building my campaign world, I want to add a Player Character entity that captures the core roleplaying identity of a D&D 5e character so that the AI can reference the character's personality, backstory, and capabilities when generating new content such as NPC dialogue, quest hooks, or narrative summaries.

**Why this priority**: Without the ability to create a PC entity, no AI-assisted content generation can be personalized to the party. This is the foundational capability that everything else depends on.

**Independent Test**: Can be fully tested by creating a new "Player Character" entity from the entity type selector, filling in the form fields, saving, and confirming all entered data persists correctly.

**Acceptance Scenarios**:

1. **Given** the user is on the world sidebar, **When** they create a new child entity and select "Player Character" from the entity type selector, **Then** a new entity form appears with all PC-specific fields
1. **Given** the user has opened a new Player Character form, **When** they fill in core identity fields (character name, race/species, class, subclass, level, background), **Then** all values are accepted and displayed correctly
1. **Given** the user has filled in the PC form, **When** they save the entity, **Then** all entered data is persisted and can be retrieved when the entity is reopened

---

### User Story 2 - Capture Roleplaying Personality and Backstory (Priority: P1)

As a **dungeon master**, when I am preparing for a session, I want each Player Character to have rich personality and backstory information so that the AI can generate content tailored to specific characters (e.g., personal quest hooks, NPC reactions, in-character dialogue suggestions).

**Why this priority**: Personality and backstory are the primary inputs the AI needs to generate personalized content. Without these, AI output would be generic rather than character-specific.

**Independent Test**: Can be fully tested by creating a PC entity, entering personality traits, ideals, bonds, flaws, and backstory, saving, and verifying the data round-trips correctly.

**Acceptance Scenarios**:

1. **Given** a Player Character entity form is open, **When** the user enters personality traits, ideals, bonds, and flaws, **Then** each field accepts free-form text and preserves the entered values
1. **Given** a Player Character entity has a populated backstory field, **When** the AI generates content referencing that character, **Then** it has access to the backstory text for context

---

### User Story 3 - Record Class, Race, and Mechanical Identity (Priority: P2)

As a **dungeon master or player**, when I define my character, I want to specify the mechanical identity (class, subclass, race/species, level, background) so that the AI understands what the character can do and tailors encounter design, loot, and narrative elements accordingly.

**Why this priority**: Mechanical identity shapes what content the AI generates (e.g., magic items for a wizard vs. martial weapons for a fighter). It is essential for quality content generation but secondary to personality and backstory which drive narrative.

**Independent Test**: Can be fully tested by creating a PC with class "Wizard", subclass "School of Evocation", race "High Elf", level 7, and background "Sage", saving, and confirming all values persist.

**Acceptance Scenarios**:

1. **Given** a Player Character form, **When** the user enters a class, subclass, race/species, level, and background, **Then** all values are stored and displayed correctly
1. **Given** a PC with level set to 7, **When** the entity is reopened, **Then** the level displays as 7

---

### User Story 4 - Track Party Connections and Relationships (Priority: P2)

As a **dungeon master**, when planning sessions, I want to see each Player Character's faction affiliations and relationships so that the AI can weave inter-character dynamics and faction politics into generated content.

**Why this priority**: Relationships and faction ties create the connective tissue the AI needs to generate plot hooks involving multiple characters, but individual characters can still be useful without this information.

**Independent Test**: Can be fully tested by creating a PC, adding faction and relationship tags, saving, and confirming tags persist and display correctly.

**Acceptance Scenarios**:

1. **Given** a Player Character form, **When** the user adds faction affiliations (e.g., "Harpers", "Zhentarim"), **Then** each faction appears as a tag that can be added or removed
1. **Given** a Player Character form, **When** the user adds relationships (e.g., "Rival of Strahd", "Mentor: Elminster"), **Then** each relationship appears as a tag

---

### User Story 5 - Record Equipment and Signature Abilities (Priority: P3)

As a **dungeon master**, when generating encounters or loot, I want the AI to know what notable equipment and abilities each PC has so that it can create relevant treasure, challenges, and narrative moments.

**Why this priority**: Equipment and abilities enhance content generation quality but are not essential for basic AI-assisted narrative. Characters are most defined by who they are, not what they carry.

**Independent Test**: Can be fully tested by creating a PC, adding notable equipment and abilities as tags, saving, and confirming they persist.

**Acceptance Scenarios**:

1. **Given** a Player Character form, **When** the user adds notable equipment items (e.g., "Staff of the Magi", "Bag of Holding"), **Then** each item appears as a tag
1. **Given** a Player Character form, **When** the user adds signature abilities (e.g., "Fireball", "Second Wind", "Sneak Attack"), **Then** each ability appears as a tag

---

### User Story 6 - Player Character Appears in Entity Hierarchy (Priority: P1)

As a **dungeon master**, when organizing my world, I want Player Characters to appear in the entity sidebar tree under appropriate parent entities (e.g., under a Campaign, Faction, or City) so that they are integrated into the world structure.

**Why this priority**: If PCs do not appear in the hierarchy, they cannot be discovered or organized within the world. This is essential for usability.

**Independent Test**: Can be fully tested by creating a Player Character as a child of a Campaign entity, then verifying it appears in the sidebar tree with the correct icon and label.

**Acceptance Scenarios**:

1. **Given** a Campaign entity exists, **When** the user adds a Player Character as a child, **Then** the PC appears in the sidebar tree under the Campaign with the "UserCheck" icon and "Player Character" label
1. **Given** the entity type selector is displayed, **When** the user browses entity types, **Then** "Player Character" appears under a "Characters & Factions" category, distinct from the existing "Character" (NPC) type

---

### Edge Cases

- What happens when a user enters a level of 0 or a negative number? The system accepts any integer; validation is informational only since fantasy homebrew rules may allow unusual values.
- What happens when a user leaves all optional fields blank? The entity saves successfully with only the name (inherited from the base entity) populated. All PC-specific fields are optional.
- What happens when a user enters extremely long backstory text? The backstory field enforces a maximum of 2000 characters to prevent performance issues.
- How does the system distinguish between the existing "Character" (NPC/creature) type and the new "Player Character" type? They are separate entity types with different type identifiers (`Character` vs `PlayerCharacter`), different icons, and different property schemas.
- What happens when the "Player Character" type is suggested as a child but the parent type has not been updated? The parent entity type's `suggestedChildren` array must be updated to include `PlayerCharacter` for it to appear as a suggested child option.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST add a `PlayerCharacter` entity type to the Entity Type Registry with category "Characters & Factions"
- **FR-002**: The Player Character entity MUST include the following text fields for core identity: Race/Species, Class, Subclass, Background
- **FR-003**: The Player Character entity MUST include a Level field as an integer type
- **FR-004**: The Player Character entity MUST include four separate textarea fields for roleplaying personality: Personality Traits, Ideals, Bonds, and Flaws (each as an independent field matching the D&D 5e character sheet layout)
- **FR-005**: The Player Character entity MUST include a Backstory field as a textarea for narrative history with a maximum length of 2000 characters
- **FR-006**: The Player Character entity MUST include an Alignment field as a text field
- **FR-007**: The Player Character entity MUST include a Factions field as a tagArray to support multiple simultaneous faction affiliations (e.g., "Harpers", "Zhentarim")
- **FR-008**: The Player Character entity MUST include tag array fields for: Notable Equipment, Signature Abilities, Languages, Relationships
- **FR-009**: The Player Character entity MUST use a distinct icon ("UserCheck") to differentiate it from the existing Character (NPC) type which uses "User"
- **FR-010**: The Player Character entity MUST have `suggestedChildren` including at least "Item" and "Quest" (items carried and personal quests)
- **FR-011**: The Player Character entity MUST NOT require ability scores (Strength, Dexterity, etc.) as fields, since these will be imported from D&D Beyond in the future
- **FR-012**: The Player Character entity MUST have a `schemaVersion` of 1
- **FR-013**: The Player Character entity MUST include a Player Name field (text) to record which real-world player controls this character
- **FR-014**: Campaign, Session, Faction, and City entity types MUST be updated to include `PlayerCharacter` in their `suggestedChildren` arrays
- **FR-015**: The "People" container entity type MUST be updated to include `PlayerCharacter` in its `suggestedChildren` array
- **FR-016**: The "Folder" container entity type MUST be updated to include `PlayerCharacter` in its `suggestedChildren` array
- **FR-017**: All existing entity type tests MUST continue to pass after the addition
- **FR-018**: The Player Character entity MUST include an Appearance field as a textarea for physical description
- **FR-019**: The Player Character entity MUST have `canBeRoot: true`, allowing it to be created at the top level of a world without requiring a parent entity

### Key Entities

- **PlayerCharacter**: A D&D 5th edition player character representing a real player's avatar in the game world. Contains roleplaying-focused attributes (personality, backstory, class, race) rather than mechanical statistics (ability scores, hit points, armor class). Key attributes include: identity (race/species, class, subclass, level, background, alignment), personality (traits, ideals, bonds, flaws — each as a separate field), narrative (backstory, appearance), social connections (factions as tagArray for multiple affiliations, relationships, languages), and equipment/abilities (notable equipment, signature abilities). Relates to Items (carried gear), Quests (personal objectives), Campaigns (membership), and Factions (affiliations).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create a Player Character entity and populate all fields in under 5 minutes
- **SC-002**: A Player Character entity with all fields populated provides sufficient context for the AI to generate a character-specific quest hook (backstory, personality, class/abilities, and relationships are all available as context)
- **SC-003**: 100% of users can visually distinguish between a Player Character and an NPC Character in the entity sidebar tree (different icon and label)
- **SC-004**: Player Character appears as a suggested child type when creating entities under Campaign, Session, Faction, City, People, and Folder parent types
- **SC-005**: All existing entity type registry tests pass after expected count and root-type updates (zero unexpected regressions)
- **SC-006**: The Player Character entity type is selectable from the entity type selector under "Characters & Factions" category

## Dependencies & Assumptions

### Dependencies

- Existing Entity Type Registry infrastructure (entityTypeRegistry.ts and derived constants in worldEntity.types.ts)
- Existing entity form rendering system that reads `propertySchema` from the registry
- Existing sidebar tree rendering that uses entity type icons and labels

### Assumptions

- The D&D 5e character attributes chosen prioritize roleplaying and AI content generation over mechanical completeness; ability scores, hit points, spell slots, and other combat statistics are intentionally omitted
- A future D&D Beyond API integration will be able to populate or supplement these fields, but the schema is designed to be useful without that integration
- The "UserCheck" Lucide icon is available in the project's icon set (consistent with existing icon usage patterns)
- All property fields are optional; a valid Player Character entity only requires the base entity name
- The existing "Character" type remains unchanged and continues to serve as the NPC/creature entity type

## Out of Scope

- **Ability scores and combat statistics**: Intentionally excluded; will be added via D&D Beyond import in a future feature
- **D&D Beyond API integration**: Planned for a future feature; this spec only defines the data structure
- **Spell list management**: Too granular for the registry property schema; future enhancement
- **Character sheet PDF generation**: Out of scope for entity type definition
- **Multi-class support as separate fields**: A single Class and Subclass field is sufficient; multiclass details can be captured in the text field (e.g., "Fighter 5 / Wizard 3")
- **Character level-up tracking or progression**: The level field is a snapshot, not a progression tracker
