# Feature Specification: Entity Type Selector Enhancements

**Feature Branch**: `010-entity-selector-enhancements`  
**Created**: 2026-01-29  
**Status**: Draft  
**Input**: User description: "Improve the EntityTypeSelector by making the following refinements: 1. World Entity should be selectable by typing in the drop down 2. World Entities in the drop down should display the appropriate icon to the left. 3. Reduce the vertical height of each World Entity in the drop down to show more items at once 5. Below the recommended section, have a separator and display the remaining World Entity Types in alphabetical order under an Other heading. 6. At the top of the drop down box, the Filter box should say Filter... 7. Do include the description of the World Entity below the title."

## Clarifications

### Session 2026-01-29

- Q: When the filter yields no matching entity types, what message should be displayed to the user? → A: "No entity types match '[search term]'" (shows what was searched)
- Q: The spec requires icons to be displayed to the left of each entity type name. What size should these icons be to balance visibility with the compact layout requirement? → A: 16×16 pixels (1rem) - Standard small size, compact
- Q: The spec requires a visual separator between the "Recommended" and "Other" sections. What style should this separator use? → A: Thin horizontal line (1px solid border) - Minimal, clean
- Q: FR-002 requires reducing the vertical height of list items to show 8-10 items without scrolling. What specific padding/spacing should each list item use? → A: 8px top/bottom padding (0.5rem) - Compact, fits 8-10 items
- Q: The edge cases section asks: "What happens when there are no recommended entity types for a given parent?" How should the UI behave in this scenario? → A: Show "Other" section without separator or heading - Simpler when no recommendations

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Visual Entity Type Identification (Priority: P1)

Maria is creating a new character in her campaign world and needs to select the appropriate entity type. She wants to quickly distinguish between different entity types at a glance using visual cues.

**Why this priority**: Visual identification through icons is the most critical improvement for usability. Users can scan and identify entity types 3-5x faster with icons compared to text-only labels, reducing cognitive load and selection errors.

**Independent Test**: Can be fully tested by opening the entity type selector dropdown and visually verifying that each entity type displays its unique icon to the left of the name, delivering immediate visual recognition.

**Acceptance Scenarios**:

1. **Given** I am viewing the entity type selector dropdown, **When** I see the list of entity types, **Then** each entity type displays its unique icon to the left of its name
1. **Given** I am viewing both recommended and other entity types, **When** I scan the list, **Then** I can distinguish entity types by their icons without reading the text

---

### User Story 2 - Compact List for Better Visibility (Priority: P2)

David is working on a complex campaign with many entity types. He wants to see more entity type options at once without scrolling excessively through the dropdown.

**Why this priority**: Reducing the vertical height of list items enables users to see more options simultaneously, improving decision-making efficiency and reducing scrolling fatigue. This is particularly valuable for users with large entity hierarchies.

**Independent Test**: Can be fully tested by opening the dropdown and counting the number of visible entity types without scrolling, verifying that more items are visible compared to the previous design.

**Acceptance Scenarios**:

1. **Given** I open the entity type selector dropdown, **When** I view the list without scrolling, **Then** I see at least 8-10 entity types visible in the dropdown area
1. **Given** I am viewing the entity list, **When** I compare the item height, **Then** each item occupies minimal vertical space while maintaining readability

---

### User Story 3 - Keyboard-Driven Type Selection (Priority: P3)

Sarah prefers keyboard navigation for efficiency. She wants to type to quickly filter and select entity types without using the mouse.

**Why this priority**: Keyboard-driven selection significantly improves workflow efficiency for power users who prefer keyboard shortcuts. The filter input enables rapid narrowing of options through typing.

**Independent Test**: Can be fully tested by opening the dropdown, typing partial entity type names, and verifying that the list filters appropriately and allows selection via keyboard.

**Acceptance Scenarios**:

1. **Given** I open the entity type selector dropdown, **When** I type characters in the filter input, **Then** the list of entity types filters to show only matching items
1. **Given** I have filtered the entity types, **When** I use arrow keys and Enter, **Then** I can navigate and select an entity type using only the keyboard
1. **Given** I see the filter input box, **When** I view the placeholder text, **Then** it displays "Filter..." as the prompt

---

### User Story 4 - Simplified Entity Type Grouping (Priority: P4)

Alex is exploring available entity types and wants a simple, organized view that doesn't overwhelm with too many category sections.

**Why this priority**: Simplifying the grouping from multiple categories to just "Recommended" and "Other" reduces visual complexity and cognitive overhead, making the selector easier to scan and use, especially for new users.

**Independent Test**: Can be fully tested by opening the dropdown and verifying the presence of exactly two sections: a "Recommended" section followed by a separator and an "Other" section with alphabetically sorted items.

**Acceptance Scenarios**:

1. **Given** I open the entity type selector dropdown, **When** I view the list structure, **Then** I see a "Recommended" section at the top
1. **Given** I have scrolled past the recommended section, **When** I view the remaining types, **Then** I see a visual separator followed by an "Other" heading
1. **Given** I am viewing the "Other" section, **When** I scan the entity types, **Then** they are displayed in alphabetical order by name

---

### Edge Cases

- What happens when the filter input yields no matching entity types? → Display message: "No entity types match '[search term]'"
- How does the component handle very long entity type names or descriptions that might overflow? → Truncate with ellipsis while maintaining minimum readable length
- What happens when there are no recommended entity types for a given parent? → Display all types directly without "Recommended" section, separator, or "Other" heading (flat list)
- How does keyboard navigation behave when transitioning between "Recommended" and "Other" sections? → Arrow keys navigate seamlessly across the boundary as a single continuous list

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Each entity type in the dropdown MUST display a unique icon (16×16 pixels / 1rem) positioned to the left of the entity type name
- **FR-002**: The vertical height of each entity type list item MUST use 8px (0.5rem) top/bottom padding to allow 8-10 items to be visible without scrolling
- **FR-003**: The filter input box placeholder text MUST display "Filter..." instead of the current text
- **FR-004**: Users MUST be able to type in the filter input to narrow down the list of entity types by matching against name or description
- **FR-005**: The dropdown MUST display a "Recommended" section at the top containing context-appropriate entity types (when recommendations exist)
- **FR-006**: Below the "Recommended" section, there MUST be a visual separator displayed as a thin horizontal line (1px solid border) (only when recommendations exist)
- **FR-007**: Below the separator, there MUST be an "Other" heading (only when recommendations exist)
- **FR-008**: Under the "Other" heading (or at the top if no recommendations exist), all remaining entity types MUST be displayed in alphabetical order by name
- **FR-009**: Entity types MUST continue to display their description text below the title/name
- **FR-010**: The component MUST remain accessible via keyboard navigation (arrow keys, Enter, Escape, Tab)
- **FR-011**: The component MUST maintain WCAG 2.2 Level AA accessibility compliance with proper ARIA attributes
- **FR-012**: Icon colors MUST meet the 3:1 contrast ratio requirement for graphical objects
- **FR-013**: When filter yields no results, the message "No entity types match '[search term]'" MUST be displayed, where [search term] is replaced with the user's filter input

### Key Entities

- **Entity Type**: Represents a classification for world building entities (e.g., Continent, Character, Settlement). Each entity type has a name, description, icon, category, and relationship rules defining what child types are valid.
- **Entity Type Metadata**: Static configuration data that defines the visual representation (icon, label, description) and behavioral properties (category, allowed children) for each entity type in the system.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can identify entity types by their icons without reading labels in 90% of cases (measured through usability testing)
- **SC-002**: The dropdown displays at least 8 entity types simultaneously without scrolling on standard desktop viewport (1920x1080)
- **SC-003**: Users can filter to a specific entity type within 2 seconds using the keyboard filter input
- **SC-004**: The time to select an entity type decreases by at least 30% compared to the previous version (measured through user task completion timing)
- **SC-005**: All interactive elements maintain keyboard navigability with clear focus indicators
- **SC-006**: Component passes automated accessibility testing with zero violations at WCAG 2.2 Level AA
- **SC-007**: Filter input responds to user typing with results updating within 100 milliseconds

## Scope

### In Scope

- Adding unique icons for each entity type in the dropdown list
- Reducing vertical spacing/padding of list items for more compact display
- Changing filter placeholder text from "Search entity types..." to "Filter..."
- Reorganizing entity type grouping to "Recommended" and "Other" sections only
- Alphabetically sorting entity types within the "Other" section
- Adding visual separator between "Recommended" and "Other" sections
- Maintaining existing keyboard navigation functionality
- Maintaining existing accessibility features and ARIA attributes
- Keeping entity type descriptions displayed below titles

### Out of Scope

- Creating new entity types or modifying entity type definitions
- Changing the underlying entity type suggestion logic or recommendation algorithm
- Modifying how entity types are loaded or fetched from data sources
- Adding new filtering capabilities beyond existing name/description matching
- Changing the component API or props interface (unless minimally necessary)
- Responsive mobile layout changes (focus is on desktop experience)
- Adding multi-select or batch selection capabilities
- Implementing recent selections or favorites functionality

## Assumptions

- **ASM-001**: Entity type metadata already includes or can be enhanced to include icon identifiers without database schema changes
- **ASM-002**: Icon assets (SVG or icon font) are available for all existing entity types in the system
- **ASM-003**: The current keyboard navigation implementation can be preserved with minimal changes
- **ASM-004**: Users primarily interact with this component on desktop viewports (1920x1080 or larger)
- **ASM-005**: The existing filter logic (matching against name and description) remains sufficient for user needs
- **ASM-006**: Reducing item height will not negatively impact readability for users with visual impairments (assumes testing validates this)
- **ASM-007**: The Azure Portal resource group selector pattern is an appropriate UX reference for this use case

## Dependencies

- Entity type metadata service/type definitions must support icon properties
- Icon library (e.g., Lucide React, Heroicons) must contain appropriate icons for all entity types
- TailwindCSS spacing utilities support the required compact spacing adjustments
- Component testing infrastructure supports visual regression testing for icon display verification

## Open Questions

None at this time. All requirements are sufficiently specified for implementation.
