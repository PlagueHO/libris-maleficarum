# Tasks: Fantasy D&D Theme Styling

**Input**: Design documents from `/specs/013-fantasy-dnd-theme/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Not explicitly requested as a standalone effort. Existing jest-axe a11y tests provide regression validation. New tests are written only for the ThemeToggle component and useTheme hook (TDD per Constitution Principle III). Run the full test suite as a verification task at phase boundaries.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Frontend**: `libris-maleficarum-app/src/` for source, `libris-maleficarum-app/public/` for static assets

---

## Phase 1: Setup

**Purpose**: Remove legacy CSS conflicts and register missing design tokens

- [X] T001 Remove the old `@theme { }` block (HSL values) from `libris-maleficarum-app/src/index.css`
- [X] T002 Add `--color-destructive-foreground: var(--destructive-foreground);` to the `@theme inline` block in `libris-maleficarum-app/src/index.css`
- [X] T003 Register heading font variable `--font-heading: 'Cinzel', Georgia, 'Times New Roman', serif;` in the `@theme inline` block in `libris-maleficarum-app/src/index.css`

---

## Phase 2: Foundational (Font Infrastructure)

**Purpose**: Add the Cinzel fantasy font - MUST complete before typography stories can be verified

- [X] T004 [P] Download Cinzel variable woff2 files (Latin + LatinExt) and place in `libris-maleficarum-app/public/fonts/`
- [X] T005 [P] Add `<link rel="preload">` for Cinzel woff2 files in `libris-maleficarum-app/index.html`
- [X] T006 Add `@font-face` declarations for Cinzel (Latin + LatinExt subsets) with `font-display: swap` in `libris-maleficarum-app/src/index.css`
- [X] T007 Add CSS rule `h1, h2, h3, h4, h5, h6 { font-family: 'Cinzel', Georgia, 'Times New Roman', serif; }` in `@layer base` in `libris-maleficarum-app/src/index.css`

**Checkpoint**: Font infrastructure ready - headings render in Cinzel with Georgia fallback, `font-heading` utility available

---

## Phase 3: User Story 1 - Consistent Fantasy Theme Across Pages (Priority: P1) :dart: MVP

**Goal**: Replace all colour tokens in `:root` and `.dark` with the fantasy D&D palette (gold primary, royal blue secondary, gold accent for interactive states). All ~22 Shadcn/UI components inherit the new theme automatically.

**Independent Test**: Navigate every screen (world sidebar, main panel, chat panel, toolbar) in both modes and confirm the fantasy colour palette is applied consistently. All hover/selection states render gold (not blue). Run `pnpm test` - all jest-axe tests pass.

### Implementation for User Story 1

- [X] T008 [US1] Replace all oklch values except chart tokens in the `:root` block with the light-mode fantasy palette from research.md Section 2 in `libris-maleficarum-app/src/index.css`
- [X] T009 [US1] Add `--destructive-foreground: oklch(0.985 0 0);` to the `:root` block in `libris-maleficarum-app/src/index.css`
- [X] T010 [US1] Replace all oklch values except chart tokens in the `.dark` block with the dark-mode fantasy palette from research.md Section 2 in `libris-maleficarum-app/src/index.css`
- [X] T011 [US1] Add `--destructive-foreground: oklch(0.985 0 0);` to the `.dark` block in `libris-maleficarum-app/src/index.css`
- [X] T012 [US1] Update `--chart-1` through `--chart-5` in the `:root` block with fantasy-harmonised light-mode chart colours in `libris-maleficarum-app/src/index.css`
- [X] T013 [US1] Update `--chart-1` through `--chart-5` in the `.dark` block with fantasy-harmonised dark-mode chart colours in `libris-maleficarum-app/src/index.css`
- [X] T014 [US1] Run `pnpm test` in `libris-maleficarum-app/` - all existing jest-axe a11y tests must pass with the new colour tokens

### Accent Token Update - Gold for Interactive States (FR-001 Clarification)

- [X] T040 [US1] Research optimal oklch gold-toned accent values for `--accent` and `--sidebar-accent` tokens — must be subtle warm gold (hue ~80-85), lighter/less saturated than `--primary` (`oklch(0.75 0.15 85)`), with >=4.5:1 contrast against `--accent-foreground` text
- [X] T041 [US1] Update research.md Section 2 tables to replace light blue accent values with new gold-toned accent values and document contrast verification results in `specs/013-fantasy-dnd-theme/research.md`
- [X] T042 [P] [US1] Update `--accent` from `oklch(0.7 0.1 240)` to gold-toned value and `--accent-foreground` from `oklch(0.18 0.02 255)` (verify contrast) in the `:root` block of `libris-maleficarum-app/src/index.css`
- [X] T043 [P] [US1] Update `--sidebar-accent` from `oklch(0.93 0.01 250)` to gold-toned value and `--sidebar-accent-foreground` from `oklch(0.18 0.02 255)` (verify contrast) in the `:root` block of `libris-maleficarum-app/src/index.css`
- [X] T044 [P] [US1] Update `--accent` from `oklch(0.6 0.1 240)` to dark gold-toned value and `--accent-foreground` from `oklch(0.17 0.03 255)` (verify contrast) in the `.dark` block of `libris-maleficarum-app/src/index.css`
- [X] T045 [P] [US1] Update `--sidebar-accent` from `oklch(0.27 0.02 255)` to dark gold-toned value and `--sidebar-accent-foreground` from `oklch(0.93 0.01 80)` (verify contrast) in the `.dark` block of `libris-maleficarum-app/src/index.css`
- [X] T046 [US1] Update `--chart-3` from `oklch(0.7 0.1 240)` (light) and `oklch(0.6 0.1 240)` (dark) to a distinct decorative blue or teal value that differentiates from the gold accent in `libris-maleficarum-app/src/index.css`
- [X] T047 [US1] Run `pnpm test` in `libris-maleficarum-app/` - all 600+ tests must pass with gold accent tokens
- [X] T048 [US1] Run `pnpm build` in `libris-maleficarum-app/` - TypeScript compilation and Vite build must succeed
- [ ] T049 [US1] Verify hover/selection states render gold (not blue) across all components using `bg-accent`/`hover:bg-accent`: buttons, context menus, select items, calendar, entity tree nodes, entity type selector, dialog close buttons in both modes

**Checkpoint**: User Story 1 complete - the full fantasy colour palette is active with gold accent for all interactive states in both modes, all a11y tests pass

---

## Phase 4: User Story 3 - Fantasy Header Typography (Priority: P2)

**Goal**: Extend the fantasy font to dialog titles, drawer titles, and card titles via the `font-heading` Tailwind utility class. Headings (h1-h6) already use Cinzel from Phase 2. Toolbar labels, navigation text, and body text stay in Inter.

**Independent Test**: Open a dialog, drawer, or card - the title renders in Cinzel. Inspect toolbar labels and navigation text - they use Inter. Block font load in DevTools - titles fall back to Georgia.

### Implementation for User Story 3

- [X] T015 [P] [US3] Add `font-heading` class to `DialogTitle` className in `libris-maleficarum-app/src/components/ui/dialog.tsx`
- [X] T016 [P] [US3] Add `font-heading` class to `DrawerTitle` className in `libris-maleficarum-app/src/components/ui/drawer.tsx`
- [X] T017 [P] [US3] Add `font-heading` class to `CardTitle` className in `libris-maleficarum-app/src/components/ui/card.tsx`
- [ ] T018 [US3] Verify headings, dialog titles, drawer titles, and card titles render in Cinzel; toolbar labels, navigation text, and body text render in Inter across all screens
- [ ] T019 [US3] Verify font fallback by blocking custom font load in browser DevTools - titles fall back to Georgia serif without layout shift

**Checkpoint**: User Story 3 complete - fantasy header typography confirmed on all title types, including fallback

---

## Phase 5: User Story 6 - Dark/Light Mode Toggle Switch (Priority: P1, TDD)

**Goal**: Add a visible toggle switch to the top toolbar (left of notification button) so users can switch between dark and light mode. The toggle persists the preference in localStorage and defaults to the OS preferred colour scheme on first visit.

**Independent Test**: Click the toggle and verify the theme switches immediately. Reload and confirm the preference persists. Tab to the toggle and verify keyboard accessibility. Use a screen reader and verify the toggle is announced correctly.

### Tests for User Story 6 (TDD - tests before implementation)

- [X] T020 [US6] Create test file `libris-maleficarum-app/src/hooks/useTheme.test.ts` with tests for: reads localStorage, falls back to OS preference via prefers-color-scheme, falls back to light, applies/removes `.dark` class on documentElement, persists preference on toggle

### Implementation for User Story 6

- [X] T021 [US6] Create `libris-maleficarum-app/src/hooks/useTheme.ts` - custom hook that reads from localStorage (key `theme`), applies `.dark` class to `document.documentElement`, watches `prefers-color-scheme` media query, persists preference on change
- [X] T022 [US6] Create test file `libris-maleficarum-app/src/components/shared/ThemeToggle/ThemeToggle.test.tsx` with tests for: renders Sun icon in dark mode, renders Moon icon in light mode, toggles on click, has accessible label, passes jest-axe, keyboard activatable via Enter/Space
- [X] T023 [US6] Create `libris-maleficarum-app/src/components/shared/ThemeToggle/ThemeToggle.tsx` - ghost icon button using Lucide Sun/Moon icons, calls useTheme hook, dynamic `aria-label` (e.g. "Switch to dark mode")
- [X] T024 [US6] Create barrel export `libris-maleficarum-app/src/components/shared/ThemeToggle/index.ts`
- [X] T025 [US6] Add `ThemeToggle` to `libris-maleficarum-app/src/components/TopToolbar/TopToolbar.tsx` inside the `ml-auto` div immediately before `NotificationBell`
- [X] T026 [US6] Add inline `<script>` to `libris-maleficarum-app/index.html` before the root div that reads localStorage synchronously and applies `.dark` class to `<html>` to prevent flash of wrong theme on page load
- [X] T027 [US6] Run `pnpm test` in `libris-maleficarum-app/` - all tests pass including new ThemeToggle and useTheme tests
- [ ] T028 [US6] Verify toggle visually: click toggle confirms theme switches immediately; reload page confirms preference persists; sun/moon icons match current mode
- [ ] T028a [US6] Verify toggle remains visible and usable on narrow viewports (sm: breakpoint and below)

**Checkpoint**: User Story 6 complete - toggle is visible, accessible, keyboard operable, and persists preference

---

## Phase 6: User Story 2 + User Story 4 - Mode Harmony & Interactive Accessibility (P1/P2 Validation)

**Goal**: Confirm both modes present a harmonious fantasy theme with proper contrast, and that all interactive elements have visible focus indicators meeting accessibility requirements. Gold accent tokens must render correctly for hover/selection in both modes.

**Independent Test**: Toggle between dark and light mode on every screen. Tab through all interactive elements. Verify focus ring visibility (gold ring) and hover state distinguishability in both modes.

**Note**: These are validation-only stories - the colour token work in Phase 3 (including accent update) and toggle in Phase 5 deliver both. No new source code tasks expected unless issues are found.

- [ ] T029 [US2] Toggle between dark and light mode on all major screens and verify no elements appear invisible, unreadable, or mismatched
- [ ] T030 [US2] Spot-check contrast ratios with browser DevTools: foreground-on-background, primary-foreground-on-primary, accent-foreground-on-accent, muted-foreground-on-muted in both modes - all must meet WCAG AA (4.5:1 normal, 3:1 large)
- [ ] T031 [US4] Tab through all interactive elements and verify gold focus ring is clearly visible with >=3:1 contrast against adjacent colours in both modes
- [ ] T032 [US4] Verify hover states on buttons are visually distinguishable from default state without relying on colour alone
- [ ] T032a [US4] Verify disabled controls are visually distinguishable from enabled controls and are not keyboard focusable

**Checkpoint**: User Stories 2 and 4 confirmed - both modes are harmonious, all interactive elements are accessible

---

## Phase 7: User Story 5 - Colour Not Used as Sole Information Indicator (Priority: P3 Validation)

**Goal**: Ensure the new theme does not regress existing non-colour indicators for error states, selection states, and status indicators.

**Independent Test**: View the app with a colour blindness simulator and confirm all states remain distinguishable via shape, text, or iconography.

- [ ] T033 [US5] Verify form error states display both colour change and text error message in `libris-maleficarum-app/src/` - confirm no regression from new palette
- [ ] T034 [US5] Verify selected sidebar items use background change plus a visual indicator (border or icon) - not colour alone - in `libris-maleficarum-app/src/components/WorldSidebar/`

**Checkpoint**: User Story 5 confirmed - no colour-only information indicators

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Hardcoded colour migration (completed), final validation, and cleanup

- [X] T035 Verify `tooltip.test.tsx` exists and covers tooltip rendering with theme tokens in `libris-maleficarum-app/src/components/ui/`
- [X] T036 Replace hardcoded `border-slate-200 bg-slate-950 text-slate-50` with `border-border bg-popover text-popover-foreground` in `libris-maleficarum-app/src/components/ui/tooltip.tsx`
- [X] T037 Review `libris-maleficarum-app/src/components/ui/sonner.tsx` for non-theme hardcoded colour usage and migrate if found
- [X] T038 Run full test suite `pnpm test` after tooltip/sonner migration - confirm all tests pass
- [ ] T039 Run quickstart.md validation - follow all steps in `specs/013-fantasy-dnd-theme/quickstart.md` and confirm expected results

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - start immediately ✅ COMPLETE
- **Foundational (Phase 2)**: Depends on T001 (legacy `@theme` removal) ✅ COMPLETE
- **User Story 1 (Phase 3)**: Depends on Phase 1 completion (clean `index.css`). Initial palette ✅ COMPLETE; accent update T040-T049 REMAINING
- **User Story 3 (Phase 4)**: Depends on Phase 2 completion (font files and `font-heading` in place). Implementation ✅ COMPLETE; verification T018-T019 REMAINING
- **User Story 6 (Phase 5)**: Depends on Phase 3 completion (colour tokens applied for both modes). Implementation ✅ COMPLETE; verification T028-T028a REMAINING
- **User Stories 2 & 4 (Phase 6)**: Depends on Phase 3 accent update (T040-T049) and Phase 5 toggle — validates gold hover/selection in both modes
- **User Story 5 (Phase 7)**: Depends on Phase 3 completion (new theme active including gold accent)
- **Polish (Phase 8)**: Tooltip/sonner migration ✅ COMPLETE; quickstart validation after all phases

### Within Each Phase

- T040 (research) must precede T041 (update research.md) must precede T042-T045 (token updates)
- T042, T043, T044, T045 are parallel (different token groups, same file but non-overlapping lines)
- T046 depends on accent colour decision (T040) to pick a distinct chart-3 value
- T047, T048 depend on T042-T046 (test/build after all token changes)
- T049 depends on T047 passing (visual verification after tests confirm no regressions)

### Parallel Opportunities

```text
Phase 1:  T001 --> T002 + T003 (parallel after @theme removal) ✅ COMPLETE
Phase 2:  T004 || T005 (different files) --> T006 --> T007 ✅ COMPLETE
Phase 3a: T008-T014 (sequential palette application) ✅ COMPLETE
Phase 3b: T040 --> T041 --> T042 || T043 || T044 || T045 (parallel accent updates) --> T046 --> T047 --> T048 --> T049
Phase 4:  T015 || T016 || T017 (different files) ✅ COMPLETE --> T018 --> T019
Phase 5:  T020-T027 ✅ COMPLETE --> T028 --> T028a
Phase 6:  T029 --> T030 --> T031 --> T032 --> T032a
Phase 7:  T033 --> T034
Phase 8:  T035-T038 ✅ COMPLETE --> T039
```

---

## Implementation Strategy

### MVP First (Phase 1 + Phase 2 + Phase 3)

1. ✅ Complete Phase 1: Remove legacy CSS, register missing tokens
2. ✅ Complete Phase 2: Font setup
3. ✅ Complete Phase 3a: Initial colour palette in both modes
4. **NEXT**: Complete Phase 3b: Accent token update (blue → gold) for interactive states
5. **VALIDATE**: Run `pnpm test`, `pnpm build`, visually verify gold hover/selection across all screens

### Incremental Delivery

1. ✅ Phases 1-3a → MVP: Fantasy colour palette + font (COMPLETE)
2. ✅ Phase 4 → Typography on titles: font-heading on dialog/drawer/card (implementation COMPLETE)
3. ✅ Phase 5 → Toggle: useTheme hook + ThemeToggle component + toolbar integration (implementation COMPLETE)
4. **NEXT** Phase 3b → Accent token update: `--accent` and `--sidebar-accent` blue → gold (T040-T049)
5. Phase 4-5 verification → Manual verification of typography and toggle (T018-T019, T028-T028a)
6. Phase 6 → Validate dark/light mode harmony + interactive accessibility (T029-T032a)
7. Phase 7 → Validate colour-independence of indicators (T033-T034)
8. ✅ Phase 8a → Hardcoded migration (COMPLETE)
9. Phase 8b → Quickstart validation (T039)

---

## Notes

- All colour values reference `specs/013-fantasy-dnd-theme/research.md` Section 2 (oklch format)
- ~22 Shadcn/UI components inherit theme changes automatically via CSS custom properties
- **24 of 39 original tasks COMPLETE** (T001-T017, T020-T027, T035-T038)
- **10 new tasks added** (T040-T049) for accent token update per FR-001 clarification (gold for all interactive states)
- **Total remaining**: 20 tasks — 10 implementation (T040-T049), 10 manual validation (T018-T019, T028-T034, T028a, T032a, T039)
- `bg-accent`/`hover:bg-accent` is used in ~20+ component locations (buttons, context menus, select items, calendar, entity tree, entity type selector, dialog close)
- `--accent` currently `oklch(0.7 0.1 240)` (light) / `oklch(0.6 0.1 240)` (dark) — hue 240 is BLUE, must become hue ~85 (GOLD)
- `--sidebar-accent` currently `oklch(0.93 0.01 250)` (light) / `oklch(0.27 0.02 255)` (dark) — must also become gold-toned
- New accent values must be subtler/less saturated than `--primary` to differentiate hover bg from active/focused states
- Constitution Principle III (TDD): Tests written before implementation for ThemeToggle and useTheme
- Commit after each phase for clean rollback points
