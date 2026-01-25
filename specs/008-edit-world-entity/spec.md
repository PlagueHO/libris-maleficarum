# Feature Specification: World Entity Editing

**Feature Branch**: `008-edit-world-entity`  
**Created**: January 25, 2026  
**Status**: Draft  
**Input**: User description: "Add feature to allow editing of World Entity, by either clicking an Edit button in the most common place for an edit button when viewing a World Entity form or by clicking an Edit (pen) icon against the World Entity in the WorldSidebar world entity Hierarchy."

## Clarifications

### Session 2026-01-25

- Q: When a user clicks the edit icon next to an entity in the hierarchy tree, where should the edit interface appear? → A: Edit interface displays in MainPanel, replacing existing content. If MainPanel has unsaved changes, show a dialog with Yes/No/Cancel options before replacing.
- Q: When editing an existing world entity, should users be allowed to change the entity type? → A: No, entity type is read-only during edit (can only be set during creation). Note: Type changes planned for future version.
- Q: When the user clicks "Save" after editing an entity, what should happen to the MainPanel view after a successful save? → A: Return to read-only detail view of the saved entity.
- Q: When a user is viewing a world entity in read-only detail view, where should the "Edit" button be positioned? → A: Top-right corner of the form/detail view.
- Q: What validation rules should be enforced when editing world entities beyond basic "required field" checks? → A: Schema-based validation using entity type registry for type-specific rules.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Quick Edit from Hierarchy (Priority: P1)

A user browsing the world entity hierarchy in the WorldSidebar spots an entity that needs correction (e.g., typo in a character name, wrong location). They click the edit icon next to the entity in the hierarchy tree and immediately begin editing the entity's properties without navigating to a separate detail view.

**Why this priority**: This is the most direct and efficient path for users who spend most of their time in the hierarchy view. It minimizes navigation and context switching, making quick corrections faster. This represents the primary workflow for power users managing large campaign worlds.

**Independent Test**: Can be fully tested by displaying the hierarchy, clicking an edit icon on any entity, verifying the edit interface appears, and confirming that changes can be saved - delivers immediate value for rapid entity updates.

**Acceptance Scenarios**:

1. **Given** a user is viewing the WorldSidebar with the entity hierarchy visible, **When** they hover over or select a world entity item, **Then** an edit icon (pen symbol) appears next to the entity name
1. **Given** a user sees an edit icon next to a world entity, **When** they click the edit icon, **Then** the edit form for that entity appears in the MainPanel
1. **Given** the MainPanel currently displays content with unsaved changes, **When** the user clicks an edit icon in the hierarchy, **Then** a dialog appears asking to save changes with Yes/No/Cancel options
1. **Given** the unsaved changes dialog is shown, **When** the user selects Yes, **Then** the current changes are saved and the edit form for the selected entity appears in the MainPanel
1. **Given** the unsaved changes dialog is shown, **When** the user selects No, **Then** the current changes are discarded and the edit form for the selected entity appears in the MainPanel
1. **Given** the unsaved changes dialog is shown, **When** the user selects Cancel, **Then** the dialog closes and the MainPanel content remains unchanged
1. **Given** a user has made changes to an entity via the hierarchy edit, **When** they save the changes, **Then** the entity is updated, the hierarchy reflects the new values immediately, and the MainPanel displays the read-only detail view of the saved entity
1. **Given** a user has opened edit mode from the hierarchy, **When** they cancel or close without saving, **Then** the entity remains unchanged and edit mode is exited

---

### User Story 2 - Edit from Detail View (Priority: P2)

A user has navigated to view the full details of a world entity (e.g., viewing all attributes of a city or character). While reviewing the comprehensive information, they identify fields that need updating. They click an "Edit" button prominently displayed in the detail view and modify the entity without leaving the current context.

**Why this priority**: This complements the hierarchy-based editing by serving users who are already in a review or inspection workflow. It's essential for thorough updates requiring viewing all entity properties at once, but slightly less critical than quick hierarchy edits since it involves a user who has already navigated to the detail view.

**Independent Test**: Can be fully tested by navigating to any entity's detail view, clicking the edit button, modifying fields, and saving changes - delivers value for comprehensive entity updates during review workflows.

**Acceptance Scenarios**:

1. **Given** a user is viewing the detail form/page of a world entity, **When** the view loads, **Then** an "Edit" button is displayed in the top-right corner of the form
1. **Given** a user sees the Edit button on the detail view, **When** they click it, **Then** the form fields become editable (except entity type which remains read-only) and save/cancel actions become available
1. **Given** a user is in edit mode, **When** they view the entity type field, **Then** it displays the current type but is disabled/read-only
1. **Given** a user has modified entity fields in the detail view, **When** they save the changes, **Then** the entity is updated and the MainPanel transitions to read-only detail view showing the updated values
1. **Given** a user is in edit mode in the detail view, **When** they cancel the edit, **Then** all changes are discarded and the form returns to read-only mode showing original values

---

### User Story 3 - Edit with Validation Feedback (Priority: P3)

A user attempts to edit a world entity but provides invalid data (e.g., empty required field, invalid format). The system validates the input and provides clear, helpful error messages indicating which fields need correction and why, preventing the save operation until the data is valid.

**Why this priority**: Data integrity is important, but basic editing functionality must exist first. This story ensures quality through validation but is secondary to enabling the core edit workflows.

**Independent Test**: Can be fully tested by entering invalid data in edit mode (from either entry point), attempting to save, verifying error messages appear, correcting the data, and successfully saving - delivers value by protecting data quality.

**Acceptance Scenarios**:

1. **Given** a user is editing a world entity, **When** they attempt to save with required fields empty, **Then** clear error messages appear next to the invalid fields and the save is prevented
1. **Given** a user is editing a world entity, **When** they enter data that violates entity type schema rules, **Then** the system displays schema-specific validation messages and prevents saving until corrected
1. **Given** a user sees validation errors, **When** they correct all invalid fields according to the schema requirements, **Then** the error messages clear and the save action becomes enabled
1. **Given** a user is editing an entity, **When** validation fails, **Then** the user remains in edit mode with their partial changes preserved for correction

---

### Edge Cases

- What happens when a user attempts to edit an entity that another user is currently editing? (Assumption: Single-user application based on TTRPG campaign management context - concurrent editing not required)
- What happens when the user navigates away or refreshes the browser while in edit mode with unsaved changes? (Assumption: Warn user of unsaved changes before navigation)
- How does the system handle editing when network connectivity is lost? (Assumption: Show error message on save failure, preserve changes locally if possible)
- What happens when editing a deleted entity or an entity that no longer exists? (Assumption: Not applicable for single-user scenario, but could handle with "Entity not found" error if API returns 404)
- How are very long entity names or property values handled in the edit interface? (Assumption: Use standard responsive form design with scrolling or multi-line inputs)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST display an edit icon (pen symbol) next to each world entity item in the WorldSidebar hierarchy when the user hovers over or selects the item
- **FR-002**: System MUST provide an "Edit" button in the top-right corner of the detail view when displaying any world entity in read-only mode
- **FR-003**: System MUST display the edit form in the MainPanel when users initiate edit mode by clicking either the hierarchy edit icon or the detail view Edit button
- **FR-004**: System MUST detect unsaved changes in the MainPanel before replacing its content with an edit form
- **FR-005**: System MUST display a dialog with Yes/No/Cancel options when the user attempts to open an edit form while the MainPanel has unsaved changes
- **FR-006**: System MUST save current changes and load the edit form when the user selects Yes in the unsaved changes dialog
- **FR-007**: System MUST discard current changes and load the edit form when the user selects No in the unsaved changes dialog
- **FR-008**: System MUST keep the current MainPanel content unchanged when the user selects Cancel in the unsaved changes dialog
- **FR-009**: System MUST load all current entity property values into editable form fields when edit mode is activated
- **FR-010**: System MUST display the entity type as read-only (disabled) during edit mode, preventing type changes
- **FR-011**: System MUST provide save and cancel actions when in edit mode
- **FR-012**: System MUST validate all edited fields according to entity type schema requirements from the entity type registry before allowing save
- **FR-013**: System MUST display clear, field-specific error messages derived from schema validation rules when validation fails
- **FR-014**: System MUST prevent saving when validation errors exist
- **FR-015**: System MUST update the entity with new values when save is successful
- **FR-016**: System MUST transition the MainPanel to read-only detail view of the saved entity after successful save
- **FR-017**: System MUST refresh the hierarchy to reflect updated entity values after successful save
- **FR-018**: System MUST discard all changes when the user cancels edit mode
- **FR-019**: System MUST warn users about unsaved changes if they attempt to navigate away while in edit mode
- **FR-020**: System MUST handle all editable entity types (World, Continent, Country, Region, City, Character, etc.) using the entity type registry
- **FR-021**: System MUST maintain accessibility standards (keyboard navigation, ARIA labels, screen reader support) for all edit controls and dialogs
- **FR-022**: System MUST provide visual feedback indicating which mode the user is in (view vs. edit)

### Key Entities

- **World Entity**: Represents any entity in the campaign world hierarchy (World, Continent, Country, Region, City, Character, etc.). Contains properties like name, description, entity type, parent relationships, and metadata. Subject to schema versioning and type-specific validation rules.
- **Edit Session**: Represents the state of an active edit operation, tracking which entity is being edited, current field values, validation state, and whether changes have been made (for unsaved changes detection).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can initiate editing of any world entity from the hierarchy in under 2 seconds (from hover/select to edit mode active)
- **SC-002**: Users can initiate editing from the detail view in under 1 second (single click to edit mode)
- **SC-003**: 95% of entity edits complete successfully on first save attempt
- **SC-004**: Validation errors are displayed within 500ms of save attempt
- **SC-005**: Updated entity values appear in the hierarchy within 1 second of successful save
- **SC-006**: Zero data loss when users cancel edits or encounter save errors
- **SC-007**: Edit interface meets WCAG 2.2 Level AA accessibility standards

## Assumptions

- **Assumption 1**: This is a single-user application (one user per campaign world), so concurrent edit conflict resolution is not required
- **Assumption 2**: Entity schema validation rules are already defined and available via the entity type registry (from feature 007)
- **Assumption 3**: Standard web form patterns apply - unsaved changes warnings, undo/redo not required in MVP
- **Assumption 4**: Edit operations require immediate save (no draft/auto-save functionality in MVP)
- **Assumption 5**: The WorldSidebar hierarchy component already exists and can be extended with edit icons
- **Assumption 6**: Detail view component already exists and can be extended with an Edit button and edit mode toggle
