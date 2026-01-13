# Feature Specification: World Sidebar Navigation

**Feature Branch**: `003-world-sidebar`  
**Created**: 2026-01-13  
**Status**: Draft  
**Input**: Create the feature specification for the main sidebar of the frontend application. It will allow selecting the current world, creating or editing the world metadata and displaying the world entity hierarchy of the currently selected world. It should be an elegant and modern UI experience that is intuitive and require no explanation. Once the current World is selected or changed, the world entities in the root should be displayed and can be expanded if they have any child entities. The hierarchy will be cached in the client so to reduce calls to the backend APIs. If there are no worlds defined for the owner then it should clearly indicate a "create a world" button or link. This feature should also provide the world metadata form to allow the creation and editing of a world.

## Clarifications

### Session 2026-01-13

- Q: What browser storage mechanism should be used for caching hierarchy data and expanded state? → A: sessionStorage (cleared on tab close)
- Q: How should the world creation and editing forms be presented to the user? → A: Modal dialog (overlay with backdrop)
- Q: When a user clicks on an entity in the hierarchy, what should happen? → A: Select entity and display details in main panel
- Q: Should the sidebar support entity context menu actions (right-click or action menu)? → A: Yes, with basic actions (edit, delete, move)
- Q: How should the sidebar handle entity creation (adding new entities to the hierarchy)? → A: Via context menu on parent entity + inline "+" button next to each entity + button at world level for root entities
- Q: When creating a new entity, how should the user specify the entity type? → A: Type selector in creation modal with context-aware suggestions based on parent entity type

## User Scenarios & Testing *(mandatory)*

### User Story 1 - First-Time World Creation (Priority: P1)

A new user opens the application for the first time and needs to create their first TTRPG world to begin organizing their campaign content.

**Why this priority**: This is the critical entry point for all new users. Without the ability to create a world, users cannot use any other features of the application. It represents the foundational MVP functionality.

**Independent Test**: Can be fully tested by launching the app with a new user account, seeing the empty state prompt, clicking "Create World", filling in basic world details (name, description), and confirming the world appears in the sidebar ready for use.

**Acceptance Scenarios**:

1. **Given** a user has no existing worlds, **When** they open the application, **Then** the sidebar displays a prominent "Create Your First World" message with a clear call-to-action button
1. **Given** the user clicks "Create World", **When** the world creation form appears, **Then** they can enter a world name (required) and description (optional)
1. **Given** the user submits valid world details, **When** the world is created, **Then** it becomes the active world and appears in the sidebar selector with an empty entity hierarchy below it
1. **Given** the world creation succeeds, **When** the sidebar refreshes, **Then** the empty state is replaced with the world selector showing the newly created world

---

### User Story 2 - World Selection and Hierarchy Navigation (Priority: P1)

A user with multiple worlds needs to switch between them and navigate the entity hierarchy within each world to find specific locations, characters, or other content.

**Why this priority**: This is the core navigation pattern users will perform constantly during every session. It enables users to access and organize their world content, making it essential for basic usability.

**Independent Test**: Can be fully tested by creating 2-3 worlds with varying entity hierarchies, switching between worlds using the selector, and expanding/collapsing entity nodes to verify navigation and state persistence.

**Acceptance Scenarios**:

1. **Given** a user has multiple worlds, **When** they click the world selector dropdown, **Then** all their worlds are displayed in alphabetical order with the current world highlighted
1. **Given** the user selects a different world, **When** the selection changes, **Then** the sidebar loads and displays the root entities of the newly selected world
1. **Given** a world with root entities is displayed, **When** the user clicks an entity that has children, **Then** the entity expands to reveal its child entities with appropriate indentation
1. **Given** an expanded entity, **When** the user clicks it again, **Then** the entity collapses to hide its children
1. **Given** a user navigates the hierarchy, **When** they expand multiple levels, **Then** each level maintains visual hierarchy through progressive indentation and connecting lines
1. **Given** an entity has no children, **When** it appears in the hierarchy, **Then** no expand/collapse indicator is shown
1. **Given** a user switches to a different world, **When** they return to the previous world, **Then** the previously expanded/collapsed state is restored from cache

---

### User Story 3 - World Metadata Editing (Priority: P2)

A user needs to update details about their world such as changing its name, updating the description, or modifying other metadata as their campaign evolves.

**Why this priority**: While not required for initial setup, users frequently need to refine world details as campaigns develop. This is a common enough operation to warrant early implementation but can be deferred after basic creation and navigation.

**Independent Test**: Can be fully tested by selecting an existing world, opening its edit form through a context menu or settings icon, modifying world properties, saving changes, and verifying the updates are reflected in the sidebar.

**Acceptance Scenarios**:

1. **Given** a user has selected a world, **When** they access the world settings (via icon or context menu), **Then** a world editing form appears with current metadata pre-populated
1. **Given** the user modifies the world name or description, **When** they save changes, **Then** the updated name appears immediately in the world selector without requiring a page refresh
1. **Given** the user is editing world metadata, **When** they cancel the operation, **Then** no changes are persisted and the form closes
1. **Given** the user attempts to save an empty world name, **When** they submit the form, **Then** validation feedback prevents submission and prompts for a valid name

---

### User Story 4 - Efficient Hierarchy Loading with Caching (Priority: P2)

A user working with large world hierarchies (hundreds of entities) needs fast, responsive navigation without constant loading delays when expanding/collapsing nodes or switching between previously viewed sections.

**Why this priority**: Performance and responsiveness are critical for user satisfaction, especially for power users with complex worlds. Caching reduces API calls and improves perceived performance, but the feature is still usable without optimal caching.

**Independent Test**: Can be fully tested by creating a world with 50+ entities in a multi-level hierarchy, expanding several branches, switching to another world and back, and verifying that previously expanded nodes load instantly from cache rather than fetching from the API.

**Acceptance Scenarios**:

1. **Given** a user expands an entity to load its children, **When** the data is fetched, **Then** it is cached locally with a 5-minute TTL
1. **Given** cached hierarchy data exists, **When** the user expands a previously loaded entity, **Then** children appear instantly without API calls or loading indicators
1. **Given** cache data expires (5 minutes elapsed), **When** the user re-expands an entity, **Then** fresh data is fetched and the cache is updated
1. **Given** a user switches worlds, **When** they return to a previous world within the cache window, **Then** the entire hierarchy state (expanded nodes, loaded children) is restored from cache
1. **Given** hierarchy data changes (entity created, moved, or deleted), **When** the change completes, **Then** affected cache entries are invalidated and fresh data is loaded

---

### User Story 5 - Visual Hierarchy and Modern UI Polish (Priority: P3)

A user needs an intuitive, visually appealing sidebar that clearly communicates hierarchy relationships, entity types, and interactive elements without requiring explanation or training.

**Why this priority**: While important for user experience and professional appearance, visual polish can be refined iteratively after core functionality is working. Users can accomplish their tasks with basic styling.

**Independent Test**: Can be fully tested through visual inspection and usability testing: users unfamiliar with the app should be able to identify expandable entities, recognize entity types by icons, and understand parent-child relationships at a glance.

**Acceptance Scenarios**:

1. **Given** the sidebar displays entity hierarchy, **When** entities have children, **Then** clear expand/collapse icons (chevrons or arrows) appear beside entity names
1. **Given** entities are nested, **When** displayed in the hierarchy, **Then** visual indentation and connecting lines clearly show parent-child relationships
1. **Given** different entity types exist (Continents, Countries, Characters, etc.), **When** displayed in the hierarchy, **Then** distinctive icons represent each type for quick recognition
1. **Given** a user hovers over interactive elements, **When** the cursor moves over entities, selectors, or buttons, **Then** appropriate hover states and tooltips provide visual feedback
1. **Given** the sidebar contains many entities, **When** the list exceeds viewport height, **Then** smooth scrolling with sticky positioning for the world selector maintains context
1. **Given** the application supports light and dark themes, **When** users switch themes, **Then** the sidebar adapts with appropriate contrast and readability

---

### Edge Cases

- What happens when a world has zero root entities (newly created world)?
  - Display an empty state message: "No entities yet. Start building your world by adding locations, characters, or campaigns."
  
- What happens when a world entity hierarchy is extremely deep (10+ levels)?
  - Maintain visual hierarchy with indentation up to a maximum of 8 levels, then use subtle styling changes rather than excessive indentation for deeper levels.
  
- What happens when the API call to fetch world entities fails or times out?
  - Display an error message in the sidebar: "Unable to load world entities. Check your connection and try again." with a retry button.
  
- What happens when a user rapidly switches between worlds before previous load completes?
  - Cancel in-flight API requests for the previous world and prioritize loading the newly selected world to avoid race conditions.
  
- What happens when a user tries to create a world with a name that already exists?
  - World names do not need to be unique (user may have multiple campaigns with similar names); allow duplicate names but display creation date to help distinguish them in the selector.
  
- What happens when hierarchy cache becomes stale while a user is viewing an outdated tree?
  - Display a subtle notification banner: "Entities updated. Refresh to see latest changes." with an action to reload the hierarchy.
  
- What happens when a user expands an entity with 500+ children?
  - Implement virtual scrolling/lazy rendering for large child lists to maintain performance, loading children in batches as the user scrolls.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display all worlds owned by the authenticated user in a dropdown selector at the top of the sidebar
- **FR-002**: System MUST highlight the currently selected world in the world selector dropdown
- **FR-003**: System MUST display a prominent "Create Your First World" call-to-action when the user has no existing worlds
- **FR-004**: System MUST provide a world creation form in a modal dialog with fields for world name (required, max 100 characters) and description (optional, max 500 characters)
- **FR-005**: System MUST validate that world name is not empty before allowing form submission
- **FR-006**: System MUST provide a world editing form in a modal dialog accessible from the sidebar (via icon/menu) that allows modification of world metadata
- **FR-007**: System MUST immediately update the world selector display when world metadata is modified, without requiring page refresh
- **FR-008**: System MUST load and display root-level entities (entities with ParentId equal to WorldId) when a world is selected
- **FR-009**: System MUST display expand/collapse indicators (chevrons/arrows) for entities that have children
- **FR-010**: System MUST NOT display expand/collapse indicators for leaf entities (entities without children)
- **FR-011**: System MUST lazy-load child entities when a user expands a parent entity for the first time
- **FR-012**: System MUST visually indicate loading state (spinner/skeleton) while fetching entity children from the API
- **FR-013**: System MUST cache loaded hierarchy data (entity children) in sessionStorage with a 5-minute TTL (cleared on tab/browser close)
- **FR-014**: System MUST restore previously expanded/collapsed hierarchy state from sessionStorage when a user returns to a world within the cache window (same browser session only)
- **FR-015**: System MUST invalidate cache entries when entity data changes (create, update, delete, move operations)
- **FR-016**: System MUST visually distinguish entity types using distinctive icons (based on EntityType field)
- **FR-017**: System MUST display entity hierarchy with progressive indentation to show parent-child relationships
- **FR-018**: System MUST display connecting lines or visual guides between parent and child entities for clarity
- **FR-019**: System MUST handle world switching by canceling any in-flight API requests for the previous world and loading the newly selected world
- **FR-020**: System MUST display an empty state message when a world has no root entities
- **FR-021**: System MUST display error messages with retry options when API calls fail
- **FR-022**: System MUST sort worlds in the selector dropdown alphabetically by name
- **FR-023**: System MUST support smooth scrolling for long entity lists that exceed viewport height
- **FR-024**: System MUST maintain world selector visibility (sticky positioning) when scrolling through long entity hierarchies
- **FR-025**: System MUST provide hover states and visual feedback for all interactive elements (entities, buttons, selector)
- **FR-026**: System MUST support keyboard navigation for accessibility (tab through elements, arrow keys for hierarchy, enter to expand/collapse)
- **FR-027**: System MUST visually highlight the currently selected entity in the hierarchy
- **FR-028**: System MUST display selected entity details in the main panel (outside sidebar) when an entity is clicked
- **FR-029**: System MUST provide context menu (right-click or action button) for entities with actions: add child entity, edit entity, delete entity, and move entity to different parent
- **FR-030**: System MUST display inline "+" button next to entities when hovered to allow quick child entity creation
- **FR-031**: System MUST provide "Add Root Entity" button or action at the world level for creating top-level entities
- **FR-032**: System MUST present entity type selector in the creation modal with context-aware suggestions prioritized based on parent entity type (e.g., Country/Region suggested for Continent parent)
- **FR-033**: System MUST allow selection of any entity type regardless of parent, while providing intelligent suggestions for common patterns

### Key Entities

- **World**: Represents a TTRPG world/campaign container with properties: id (GUID), Name (string, max 100 chars), Description (string, max 500 chars), OwnerId (user identifier), CreatedDate, ModifiedDate, IsDeleted flag
  
- **WorldEntity**: Represents hierarchical entities within a world with properties: id (GUID), WorldId (parent world reference), ParentId (parent entity reference, equals WorldId for root entities), EntityType (string: "Continent", "Country", "Character", "Campaign", etc.), Name (string), Tags (array), OwnerId, Path (array of ancestor IDs), Depth (integer, hierarchy level), CreatedDate, ModifiedDate, IsDeleted flag, Properties (flexible JSON), SystemProperties (TTRPG system-specific JSON)

### Data Flow

1. **World Selection**: User selects world → API: GET `/api/v1/worlds/{worldId}` → Cache world metadata → Load root entities
1. **Root Entity Loading**: GET `/api/v1/worlds/{worldId}/entities` with filter ParentId=WorldId → Cache results (5-minute TTL) → Render hierarchy
1. **Entity Expansion**: User expands entity → Check cache → If cached, render immediately; If not cached, GET `/api/v1/worlds/{worldId}/entities/{parentId}/children` → Cache results → Render children
1. **World Creation**: User submits form → POST `/api/v1/worlds` with {Name, Description} → Receive world object → Add to world list → Set as active world → Display empty hierarchy
1. **World Editing**: User opens edit form → Pre-populate with current values → User saves → PUT `/api/v1/worlds/{worldId}` with updated metadata → Update local cache → Refresh world selector display

### Assumptions

- World names do not need to be globally unique (users can have multiple worlds with the same name)
- The maximum hierarchy depth supported is 10 levels (sufficient for typical TTRPG world structures)
- Cache TTL of 5 minutes balances performance with data freshness for typical usage patterns
- Cached data stored in sessionStorage (not persistent across browser sessions/tabs)
- Users will typically work with one world at a time during a session
- Entity icons will be provided by the design system or icon library (Fluent UI icons)
- The sidebar will occupy a fixed or collapsible column on the left side of the application layout
- Entity creation through sidebar UI is one of multiple creation methods (future: AI agents, bulk import, templates, etc.)
- Sidebar hierarchy reflects entities created through any method and automatically updates when entities are added/modified externally

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can create their first world and see it appear in the sidebar within 5 seconds of form submission
- **SC-002**: Expanding a previously loaded entity node displays children instantly (under 100ms) when served from cache
- **SC-003**: Switching between cached worlds restores the complete hierarchy state (expanded nodes, scroll position) within 200ms
- **SC-004**: Initial root entity load for a world with 50 entities completes within 2 seconds on a standard broadband connection
- **SC-005**: The sidebar remains responsive (smooth scrolling, instant hover feedback) with hierarchies containing up to 500 visible entities
- **SC-006**: 90% of new users successfully create their first world without requiring help documentation or tooltips
- **SC-007**: Users can navigate to any entity within a 3-level hierarchy in under 5 seconds (including time to expand nodes)
- **SC-008**: Cache hit rate exceeds 80% for entity expansion operations during typical navigation sessions
- **SC-009**: Error recovery (retry after failed API call) succeeds on first retry attempt in 95% of cases
- **SC-010**: Visual hierarchy relationships (parent-child connections) are correctly identified by 95% of users in usability testing without explanation
