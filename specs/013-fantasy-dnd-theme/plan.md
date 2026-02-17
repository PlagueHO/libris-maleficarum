# Implementation Plan: Fantasy D&D Theme Styling

**Branch**: `013-fantasy-dnd-theme` | **Date**: 2026-02-17 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/013-fantasy-dnd-theme/spec.md`

## Summary

Update the Libris Maleficarum UI from a neutral grey/brown palette to a fantasy D&D theme using gold (primary), royal blue (secondary), and light blue (accent) colours. Add the Cinzel font for headings (h1–h6), dialog titles, and panel/section headers. Update all Shadcn/UI design tokens in `index.css` for both light and dark modes. Migrate hardcoded Tailwind colour classes in components to use theme tokens. Add a dark/light mode toggle switch to the top toolbar (left of the notification button) so users can switch modes. All changes must maintain WCAG 2.2 Level AA contrast compliance.

## Technical Context

**Language/Version**: TypeScript 5.x, CSS
**Primary Dependencies**: React 19, TailwindCSS v4, Shadcn/UI, Radix UI, Lucide React
**Storage**: N/A (CSS-only changes + localStorage for theme preference)
**Testing**: Vitest + Testing Library + jest-axe (existing test suite validates a11y)
**Target Platform**: Web (modern browsers with oklch support)
**Project Type**: Web (frontend only)
**Performance Goals**: Font loads with `font-display: swap` (zero FOIT), no layout shift from font loading
**Constraints**: All text ≥ 4.5:1 contrast (AA normal), all large text ≥ 3:1 (AA large), all interactive boundaries ≥ 3:1
**Scale/Scope**: ~8 files modified (index.css, index.html, dialog.tsx, drawer.tsx, card.tsx, tooltip.tsx, sonner.tsx, TopToolbar.tsx), ~22 UI components inherit tokens automatically, plus ~5 new files for the dark mode toggle component and theme hook

## Constitution Check

*GATE: PASS — no violations. Re-checked after Phase 1 design: still PASS.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Cloud-Native Architecture | N/A | No infrastructure changes |
| II. Clean Architecture | PASS | CSS-only changes + one React hook + one UI component; no architecture affected |
| III. TDD (NON-NEGOTIABLE) | PASS | Existing jest-axe tests validate a11y compliance; new ThemeToggle and useTheme tests written before implementation (TDD) |
| IV. Framework & Technology Standards | NOTE | Constitution references Fluent UI v9, but codebase already uses Shadcn/UI + Tailwind. No new deviation introduced by this feature. |
| V. Developer Experience | PASS | No dev tooling changes |
| VI. Security & Privacy | PASS | No secrets, auth, or network changes. localStorage stores only theme preference string. |
| VII. Semantic Versioning | PASS | MINOR version bump — visual enhancement, backward-compatible |

## Project Structure

### Documentation (this feature)

```text
specs/013-fantasy-dnd-theme/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: font selection, colour values, audit, toggle design
├── data-model.md        # Phase 1: design token taxonomy, state transitions
├── quickstart.md        # Phase 1: verification guide
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (files to modify)

```text
libris-maleficarum-app/
├── index.html                                    # Font preload link + inline theme script
├── src/
│   ├── index.css                                 # PRIMARY: all theme tokens + font rules
│   ├── hooks/
│   │   ├── useTheme.ts                           # NEW: theme state management hook
│   │   └── useTheme.test.ts                      # NEW: tests for theme hook
│   ├── components/
│   │   ├── TopToolbar/
│   │   │   └── TopToolbar.tsx                    # Add ThemeToggle to toolbar
│   │   ├── shared/
│   │   │   └── ThemeToggle/                      # NEW: dark/light mode toggle
│   │   │       ├── ThemeToggle.tsx               # Toggle button with sun/moon icon
│   │   │       ├── ThemeToggle.test.tsx          # Tests for toggle behaviour + a11y
│   │   │       └── index.ts                      # Barrel export
│   │   └── ui/
│   │       └── tooltip.tsx                       # Migrate hardcoded slate colours
│   └── __tests__/                                # Existing test files (run for regression)
```

**Structure Decision**: Frontend-only change. All theme tokens live in `index.css` and propagate to all Shadcn/UI components via CSS custom properties. Only `tooltip.tsx` had hardcoded non-theme colours (already migrated). New files: `useTheme.ts` hook for theme state, `ThemeToggle/` component for the toolbar toggle. No backend changes.

## Implementation Phases

### Phase A: Font Setup (FR-003, FR-004, FR-005, FR-012) — COMPLETE

**Goal**: Add Cinzel fantasy font for headings, dialog titles, and panel/section headers.

1. ~~Download Cinzel variable woff2 files (Latin + LatinExt) and place in `public/fonts/`~~
2. ~~Add `@font-face` declarations in `index.css` with `font-display: swap`~~
3. ~~Add CSS rule: `h1, h2, h3, h4, h5, h6 { font-family: 'Cinzel', Georgia, 'Times New Roman', serif; }`~~
4. ~~Register `--font-heading` in `@theme inline` for the `font-heading` Tailwind utility~~
5. ~~Add `<link rel="preload">` in `index.html`~~
6. Apply `font-heading` utility class to dialog titles (`DrawerTitle`, `DialogTitle`), card titles (`CardTitle`), and panel/section headers that are not semantic heading elements
7. Verify: headings use Cinzel, dialog titles and panel headers use Cinzel via `font-heading`, body text stays Inter

### Phase B: Light Mode Colour Tokens (FR-001, FR-002, FR-006, FR-007, FR-011) — COMPLETE

**Goal**: Update `:root` block with fantasy palette and remove legacy `@theme` block.

1. ~~Remove the old `@theme { }` block (dead HSL values)~~
2. ~~Replace all oklch values in `:root` with gold/royal-blue/light-blue palette~~
3. ~~Add `--destructive-foreground` token~~
4. ~~Run contrast checks on all token pairings~~

### Phase C: Dark Mode Colour Tokens (FR-001, FR-002, FR-006, FR-007, FR-011) — COMPLETE

**Goal**: Update `.dark` block with fantasy palette.

1. ~~Replace all oklch values in `.dark` with dark fantasy palette~~
2. ~~Add `--destructive-foreground` token~~
3. ~~Run contrast checks~~

### Phase D: Chart Colours (FR-010) — COMPLETE

**Goal**: Update chart tokens to harmonise with fantasy palette. Done.

### Phase E: Hardcoded Colour Migration (FR-006) — COMPLETE

**Goal**: Replace hardcoded Tailwind colours with theme tokens.

1. ~~tooltip.tsx: migrated to `border-border bg-popover text-popover-foreground`~~
2. ~~sonner.tsx: reviewed, no changes needed~~

### Phase F: Accessibility Validation (FR-007, FR-008, FR-009)

**Goal**: Confirm all contrast and accessibility requirements are met.

1. Run full test suite: `pnpm test` — all jest-axe tests must pass
2. Manually verify keyboard focus ring visibility (gold ring on both dark and light backgrounds)
3. Spot-check contrast ratios with browser DevTools on key pairings
4. Verify font fallback by temporarily blocking font load

### Phase G: Dark/Light Mode Toggle (FR-013, FR-014, FR-015, FR-016, FR-017, FR-018, FR-019)

**Goal**: Add a visible toggle switch to the top toolbar so users can switch between dark and light mode.

1. Create a `useTheme` hook (`src/hooks/useTheme.ts`):
   - Read initial preference from localStorage (key `theme`), falling back to `prefers-color-scheme`, falling back to light
   - Apply/remove `.dark` class on `document.documentElement`
   - Persist preference to localStorage on change
   - Watch `prefers-color-scheme` media query for initial OS default detection
2. Create a `ThemeToggle` component (`src/components/shared/ThemeToggle/ThemeToggle.tsx`):
   - Renders as a ghost icon button (Sun icon in dark mode to indicate "switch to light," Moon icon in light mode to indicate "switch to dark")
   - Uses `useTheme` hook to read current mode and toggle on click
   - Has `aria-label` that communicates the action (e.g., "Switch to dark mode")
   - Receives visible keyboard focus with gold focus ring
3. Add `ThemeToggle` to `TopToolbar.tsx`:
   - Position inside the `ml-auto` div, immediately before `NotificationBell`
4. Add inline `<script>` to `index.html`:
   - Reads localStorage synchronously before React mounts
   - Applies `.dark` class to `<html>` to prevent flash of wrong theme
5. Write tests (TDD — tests before implementation):
   - `useTheme.test.ts`: localStorage read/write, `.dark` class application, OS preference fallback
   - `ThemeToggle.test.tsx`: correct icon, toggle behaviour, accessible label, jest-axe, keyboard
6. Run full test suite to confirm no regressions

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| oklch values produce unexpected colours in some browsers | Low | Medium | Test in Chrome, Firefox, Safari; oklch has broad support since 2023 |
| Cinzel font not readable at small heading sizes | Low | Low | h5/h6 are rarely used; fallback serif (Georgia) is highly readable |
| Existing jest-axe tests fail with new contrast values | Medium | Low | Colour values chosen to exceed AA thresholds; fix any failures by adjusting tokens |
| Toggle flash-of-wrong-theme on page load | Medium | Medium | Inline `<script>` in `index.html` reads localStorage synchronously before React hydrates |
| localStorage unavailable (private browsing, disabled) | Low | Low | Graceful fallback: use OS `prefers-color-scheme`, default to light if that also fails |

## Complexity Tracking

No constitution violations to justify. This feature adds one React hook, one UI component, and updates CSS custom property values. Zero new architectural patterns or abstractions.
