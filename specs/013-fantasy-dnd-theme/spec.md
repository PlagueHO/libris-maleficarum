# Feature Specification: Fantasy D&D Theme Styling

**Feature Branch**: `013-fantasy-dnd-theme`
**Created**: 2026-02-17
**Status**: Draft
**Input**: User description: "Update the application UI styling to give it more of a fantasy D&D theme. Use royal blue, light blue, gold colours with light/dark mode support and accessibility. Use a modern fantasy font for headers only. Add a light mode/dark mode toggle switch to the left of the notification button."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consistent Fantasy Theme Across Pages (Priority: P1)

A person using the application sees a cohesive fantasy-inspired visual design throughout the interface. The colour palette uses royal blue, light blue, and gold tones that evoke a classic tabletop RPG aesthetic. All backgrounds, text, borders, and interactive elements use this palette consistently across every page and panel.

**Why this priority**: The colour palette is the single most impactful change — it transforms the entire visual identity of the application from a generic utility feel to a fantasy campaign tool.

**Independent Test**: Can be fully tested by navigating through all major screens (world sidebar, main panel, chat panel, toolbar) and verifying the fantasy colour palette is applied consistently. Delivers immediate visual value.

**Acceptance Scenarios**:

1. **Given** a user opens the application in dark mode, **When** they view any page, **Then** backgrounds use deep blue-toned dark colours, primary interactive elements use gold, and secondary/accent elements use royal blue and light blue tones.
1. **Given** a user opens the application in light mode, **When** they view any page, **Then** backgrounds use subtle warm off-white tones (slight cream tint evoking parchment), primary interactive elements use gold, and accent elements use royal blue and light blue.
1. **Given** a user navigates between the sidebar, main panel, and chat panel, **When** they compare the visual styling, **Then** the fantasy colour palette is applied uniformly with no sections appearing unstyled or using the previous theme.

---

### User Story 2 - Dark Mode and Light Mode Support (Priority: P1)

A person using the application can view it in either light mode or dark mode and both modes present a visually harmonious fantasy theme. Switching between modes preserves readability and the fantasy aesthetic without jarring colour mismatches.

**Why this priority**: Light/dark mode is a core usability feature — the theme must work in both modes to be functional for all users.

**Independent Test**: Can be tested by toggling between light and dark mode on every screen and confirming colours, contrast, and readability meet expectations in both modes.

**Acceptance Scenarios**:

1. **Given** the application is in dark mode, **When** a user reads body text, **Then** all text meets WCAG 2.2 Level AA contrast requirements (at least 4.5:1 for normal text and 3:1 for large text) against its background.
1. **Given** the application is in light mode, **When** a user reads body text, **Then** all text meets WCAG 2.2 Level AA contrast requirements against its background.
1. **Given** a user switches from dark mode to light mode, **When** the mode changes, **Then** all UI elements transition to the light-mode fantasy palette without any elements appearing invisible, unreadable, or mismatched.

---

### User Story 6 - Dark/Light Mode Toggle Switch (Priority: P1)

A person using the application can see a toggle switch in the top toolbar that lets them switch between dark mode and light mode. The toggle is positioned to the left of the notification button, making it easy to find. The toggle clearly indicates which mode is currently active, and the switch takes effect immediately without a page reload.

**Why this priority**: The application has dark and light mode colour palettes defined in CSS, but there is currently no way for a person to switch between them. Without a visible toggle, the dark mode theme is inaccessible. This is a core usability gap.

**Independent Test**: Can be tested by clicking the toggle and observing the theme switch. Verify the toggle is keyboard accessible, screen-reader-announced, and persists the preference across page reloads.

**Acceptance Scenarios**:

1. **Given** a person views the top toolbar, **When** they look to the left of the notification button, **Then** they see a toggle switch for dark/light mode with a clear visual indicator of the action it will perform (e.g., sun icon when dark mode is active indicating "switch to light," moon icon when light mode is active indicating "switch to dark").
1. **Given** a person clicks the toggle, **When** the mode changes, **Then** the entire UI immediately switches to the selected mode without a page reload.
1. **Given** a person switches to dark mode and reloads the page, **When** the page loads, **Then** the application opens in dark mode with the toggle reflecting the correct state.
1. **Given** a person using a screen reader encounters the toggle, **When** they focus on it, **Then** it is announced with a descriptive label (e.g., "Switch to dark mode" or "Switch to light mode") that communicates the action it will perform.
1. **Given** a person navigates the toolbar via keyboard, **When** they tab to the toggle, **Then** the toggle receives visible focus and can be activated with Enter or Space.
1. **Given** a person has their operating system set to a preferred colour scheme (e.g., prefers-dark-mode), **When** they visit the application for the first time, **Then** the application defaults to the OS-preferred mode, and the toggle reflects that choice.

---

### User Story 3 - Fantasy Header Typography (Priority: P2)

A person viewing the application notices that headings (h1–h6), dialog titles, and panel/section header text use a distinctive modern fantasy-style font that differs from the body text. This typographic distinction reinforces the D&D theme and covers all visually prominent text without overusing the decorative font on smaller UI elements. Toolbar labels, navigation text, and all other body text remain in a highly readable sans-serif font.

**Why this priority**: Header typography adds thematic character but is secondary to the colour palette — the application is still functional and themed without custom fonts.

**Independent Test**: Can be tested by inspecting heading elements (h1–h6), dialog titles, and panel headers — confirming they use the fantasy font — while toolbar labels, navigation text, sidebar item labels, and body text use the standard font.

**Acceptance Scenarios**:

1. **Given** a user views any page with headings, **When** they compare headings and body text, **Then** headings display in a modern fantasy-style font and body text displays in the standard sans-serif font.
1. **Given** a user opens a dialog, **When** they view the dialog title, **Then** the title text renders in the fantasy font.
1. **Given** a user views a panel or section with a header, **When** they look at the header text, **Then** it renders in the fantasy font while surrounding body text and toolbar labels remain in the standard sans-serif font.
1. **Given** a user views the application on a slow connection where the custom font has not yet loaded, **When** the page renders, **Then** headings and titles fall back gracefully to a readable system font without layout shift or broken styling.
1. **Given** a person using a screen reader navigates the page, **When** they encounter headings, **Then** the headings are announced with their correct heading level and text content regardless of the visual font applied.

---

### User Story 4 - Accessible Interactive Elements (Priority: P2)

A person using the application interacts with buttons, links, form inputs, and other controls that are styled with the fantasy theme. All interactive elements are clearly distinguishable from non-interactive content and meet accessibility contrast requirements for their borders, focus indicators, and states.

**Why this priority**: Interactive element styling is essential for usability — users must be able to identify and operate controls. This is tightly coupled with the colour palette but specifically addresses interactive states.

**Independent Test**: Can be tested by tabbing through all interactive elements and verifying focus visibility, hover states, and contrast in both modes.

**Acceptance Scenarios**:

1. **Given** a user navigates via keyboard, **When** an interactive element receives focus, **Then** the focus indicator is clearly visible with at least 3:1 contrast against adjacent colours.
1. **Given** a user hovers over a button, **When** the hover state is displayed, **Then** the button changes appearance in a way that is distinguishable from its default state and does not rely solely on colour to communicate the state change.
1. **Given** a user views a disabled control, **When** they compare it to enabled controls, **Then** the disabled state is visually distinguishable and the control is not keyboard focusable.

---

### User Story 5 - Colour Not Used as Sole Information Indicator (Priority: P3)

A person with colour vision deficiency uses the application. Visual indicators such as error states, selected items, and status indicators use shape, text, or iconography in addition to colour to communicate meaning.

**Why this priority**: Ensuring information is not conveyed by colour alone is a WCAG requirement and an accessibility fundamental, but the existing components already partially address this — this story ensures the new theme does not regress.

**Independent Test**: Can be tested by viewing the application with a colour blindness simulator and confirming all states and indicators remain distinguishable.

**Acceptance Scenarios**:

1. **Given** a form field is in an error state, **When** a user views the field, **Then** the error is communicated via both a colour change and a text error message (not colour alone).
1. **Given** an item is selected in the sidebar, **When** a user views the sidebar, **Then** the selected state is communicated via both a colour/background change and a visual indicator such as a border or icon — not colour alone.

---

### Edge Cases

- What happens when the custom fantasy font fails to load? Headings must fall back to a readable system font stack without layout breakage.
- What happens when a user has a high-contrast or forced-colours accessibility mode enabled in their operating system? The application must not break and should respect system accessibility overrides.
- What happens when Shadcn/UI components are used that rely on internal colour tokens? All Shadcn/UI design tokens (primary, secondary, accent, muted, destructive, etc.) must be updated to the fantasy palette so components inherit the theme automatically.
- What happens with chart/data visualisation colours? Chart colours must also be updated to harmonise with the fantasy palette while remaining distinguishable.
- What happens when a person disables local storage or clears browser data? The mode preference is lost, and the application must fall back to the operating system's preferred colour scheme, defaulting to light mode if no OS preference is detectable.
- What happens when the toggle is rendered on a very narrow viewport? The toggle must remain visible and usable — it should not be hidden or cut off.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The application MUST use a colour palette based on royal blue, light blue, and gold as the three primary theme colours. Gold serves as the primary colour (buttons, links, focus rings), royal blue as the secondary colour (sidebar, cards, borders), and light blue as the accent colour (highlights, hover states).
- **FR-002**: The application MUST provide both a light mode and a dark mode theme, each using the fantasy colour palette.
- **FR-003**: HTML heading elements (h1–h6), dialog titles, and panel/section header text MUST render in a modern fantasy-style web font. Toolbar labels, navigation text, sidebar item labels, and all other non-heading text MUST use the body font.
- **FR-004**: Body text, form labels, toolbar labels, navigation text, sidebar item labels, and all non-heading/non-title text MUST remain in the current Inter sans-serif font.
- **FR-005**: The fantasy font MUST be loaded with appropriate fallback fonts defined so that headings render acceptably before the custom font loads.
- **FR-006**: All Shadcn/UI design tokens (--primary, --secondary, --accent, --muted, --destructive, --border, --input, --ring, --card, --popover, --sidebar, and their foreground variants) MUST be updated to reflect the fantasy colour palette.
- **FR-007**: All text MUST meet WCAG 2.2 Level AA contrast requirements — at least 4.5:1 for normal text and 3:1 for large text (18.5px bold or 24px) against its background.
- **FR-008**: All interactive element boundaries and focus indicators MUST have at least 3:1 contrast against adjacent colours.
- **FR-009**: Colour MUST NOT be used as the sole means of conveying information anywhere in the application.
- **FR-010**: Chart colours (--chart-1 through --chart-5) MUST be updated to harmonise with the fantasy palette while remaining distinguishable from one another.
- **FR-011**: The theme MUST work correctly with the dark mode toggle mechanism (`.dark` class on the root element).
- **FR-012**: The font loading strategy MUST avoid visible layout shifts (cumulative layout shift kept to a minimum by using appropriate font-display and sizing fallbacks).
- **FR-013**: The application MUST display a dark/light mode toggle switch in the top toolbar, positioned immediately to the left of the notification button.
- **FR-014**: The toggle MUST immediately switch the entire UI between dark and light mode when activated, without requiring a page reload.
- **FR-015**: The toggle MUST persist the user's mode preference in the browser so that it is preserved across page reloads and sessions.
- **FR-016**: The toggle MUST visually indicate the action it will perform (e.g., sun icon in dark mode meaning "switch to light," moon icon in light mode meaning "switch to dark") so users can tell what activating it will do.
- **FR-017**: The toggle MUST be fully keyboard accessible — reachable via Tab and activatable via Enter or Space — with a visible focus indicator.
- **FR-018**: The toggle MUST have an accessible label that communicates the action it will perform (e.g., "Switch to dark mode" or "Switch to light mode") for people using screen readers.
- **FR-019**: When no persisted preference exists, the application MUST default to the operating system's preferred colour scheme (prefers-color-scheme media query), falling back to light mode if no preference is detectable.

### Key Entities

- **Theme Colour Palette**: The set of CSS custom properties (design tokens) that define all colours used throughout the application. Gold maps to the primary role (buttons, links, focus rings), royal blue to the secondary role (sidebar, cards, borders), and light blue to the accent role (highlights, hover states). Each has light and dark mode variations.
- **Typography Scale**: The heading font stack (fantasy font + fallbacks) applied to h1–h6, dialog titles, and panel/section headers; the body font stack applied to all other text.
- **Mode Preference**: The user's selected colour mode (dark or light). Persisted in the browser so the choice survives page reloads. Defaults to the operating system preference on first visit.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All pages display the fantasy D&D colour palette (royal blue, light blue, gold) consistently in both light and dark modes, as verified by visual review of every major screen.
- **SC-002**: 100% of text elements pass WCAG 2.2 Level AA contrast checks (4.5:1 normal, 3:1 large) in both light and dark modes.
- **SC-003**: All heading elements (h1–h6), dialog titles, and panel/section headers render in the fantasy font while toolbar labels, navigation text, and body text render in the standard sans-serif font.
- **SC-004**: All interactive element focus indicators have at least 3:1 contrast against adjacent colours in both modes.
- **SC-005**: No information in the application is conveyed by colour alone — all states use additional visual indicators.
- **SC-006**: The fantasy font loads with a graceful fallback — headings render immediately in the fallback font via `font-display: swap`, with the custom font swapping in when available (no Flash of Invisible Text).
- **SC-007**: All existing automated accessibility tests continue to pass with the new theme applied.
- **SC-008**: The theme transition between light and dark mode is seamless — no elements appear broken, unreadable, or unstyled during or after the switch.
- **SC-009**: The dark/light mode toggle is visible in the toolbar to the left of the notification button and can be activated via mouse click, keyboard (Enter/Space), and touch.
- **SC-010**: A person's mode preference persists — after switching to dark mode and reloading, the application opens in dark mode.
- **SC-011**: First-time visitors see the mode matching their operating system preference (dark or light), with the toggle reflecting that default.

## Clarifications

### Session 2026-02-17

- Q: Which colour maps to which design role (primary, secondary, accent)? → A: Gold = primary (buttons, links, focus rings), Royal blue = secondary (sidebar, cards, borders), Light blue = accent (highlights, hover states).
- Q: How pronounced should the parchment aesthetic be for light mode backgrounds? → A: Subtle warm off-white — backgrounds have a very slight warm/cream tint evoking parchment, but remain essentially flat modern surfaces.
- Q: Should dialog titles, toolbar labels, and sidebar headers also use the fantasy font, or only h1–h6? → A: Headings + dialog/panel titles — h1–h6 plus dialog titles and panel/section header text use the fantasy font; toolbar labels and navigation text use the body font.

## Assumptions

- The dark mode mechanism uses the `.dark` class approach. A new toggle switch will be added to the top toolbar to let users control this. The toggle will manage adding/removing the `.dark` class.
- The fantasy font will be a freely licensed web font (e.g., from Google Fonts) that can be self-hosted or loaded from a CDN.
- The existing Shadcn/UI component library will continue to be used; the theme change is accomplished by updating CSS custom properties, not by replacing components.
- The current Inter font (or system sans-serif) for body text is retained as-is; only headings, dialog titles, and panel/section headers change font.
- Chart colours are presentational and do not need to be individually branded, only harmonised with the palette.
- The royal blue, light blue, and gold colours will be interpreted as approximate hue families — exact values will be determined during implementation to meet contrast requirements.
- Forced-colours / high-contrast operating system accessibility modes (e.g., Windows High Contrast) are out of scope for this feature — the application must not break, but specific visual adaptation is not targeted.
