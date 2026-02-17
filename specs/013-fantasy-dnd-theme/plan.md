# Implementation Plan: Fantasy D&D Theme Styling

**Branch**: `013-fantasy-dnd-theme` | **Date**: 2026-02-17 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/013-fantasy-dnd-theme/spec.md`

## Summary

Update the frontend application UI to a fantasy D&D-themed visual design. The colour palette uses gold (primary: buttons, links, focus rings, highlights, hover, selection), royal blue (secondary: sidebar, cards, borders), and light blue (decorative accent only — not interactive states). A Cinzel fantasy font applies to headings (h1–h6), dialog titles, and panel headers. A dark/light mode toggle with localStorage persistence is added to the top toolbar. All changes are CSS-token-level via Shadcn/UI custom properties — no component replacements needed except the new ThemeToggle component.

**Spec Update (latest clarification)**: Gold is now used for ALL interactive highlights, hover states, and selection backgrounds. The `--accent` and `--sidebar-accent` CSS tokens must be changed from light blue (hue 240) to a subtle gold tone (hue ~85) so that `bg-accent` and `hover:bg-accent` render gold across the entire UI. Light blue is retained only for decorative, non-interactive use.

## Technical Context

**Language/Version**: TypeScript 5.x, React 19, ES2022 target
**Primary Dependencies**: Vite 7.x, TailwindCSS v4, Shadcn/UI (Radix primitives), Lucide React icons, Redux Toolkit
**Storage**: localStorage (theme preference key `theme`), no backend changes
**Testing**: Vitest + Testing Library + jest-axe (accessibility), 600+ existing tests
**Target Platform**: Modern browsers (desktop + mobile), HTTPS
**Project Type**: Frontend web application (existing React SPA)
**Performance Goals**: No layout shift from font loading (font-display: swap); theme toggle < 16ms (single class toggle)
**Constraints**: WCAG 2.2 Level AA contrast (4.5:1 normal, 3:1 large text, 3:1 interactive boundaries)
**Scale/Scope**: ~44 test files, ~22 Shadcn/UI components inherit theme tokens, ~8 files modified/created for this feature

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Cloud-Native Architecture | N/A | Frontend-only CSS/component change — no infra impact |
| II. Clean Architecture | PASS | UI styling isolated to CSS tokens + 1 new hook + 1 new component. No business logic changes. |
| III. TDD (NON-NEGOTIABLE) | PASS | useTheme.test.ts (8 tests) written before useTheme.ts; ThemeToggle.test.tsx (7 tests) written before ThemeToggle.tsx. All 600 tests pass. |
| IV. Framework & Technology Standards | PASS | React 19+ ✓, TypeScript ✓, Vitest ✓, Redux Toolkit ✓. Note: Constitution says "Fluent UI v9" but copilot-instructions.md and AGENTS.md override to Shadcn/UI — Shadcn/UI is the actual standard. |
| V. Developer Experience | PASS | Vite hot-reload works with CSS token changes; no new build tooling |
| VI. Security & Privacy | PASS | No secrets, no API changes, localStorage only (non-sensitive preference) |
| VII. Semantic Versioning | PASS | Visual-only change — MINOR bump (backward-compatible feature) |

**Frontend Standards sub-check:**

- Functional components with hooks ✓ (useTheme is a custom hook)
- ARIA roles and labels ✓ (ThemeToggle has dynamic aria-label)
- Each component has corresponding test file ✓
- State management: Theme uses local React state (not Redux) — appropriate since theme is a DOM-level concern, not application state

## Project Structure

### Documentation (this feature)

```text
specs/013-fantasy-dnd-theme/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output (COMPLETE)
├── data-model.md        # Phase 1 output (N/A — CSS-only feature)
├── quickstart.md        # Phase 1 output (COMPLETE)
├── checklists/
│   └── requirements.md  # Requirements checklist (COMPLETE)
└── tasks.md             # Phase 2 output (existing, needs update for accent change)
```

### Source Code (frontend only)

```text
libris-maleficarum-app/
├── index.html                                    # Theme flash prevention script, font preload (COMPLETE)
├── public/fonts/
│   ├── Cinzel-Latin.woff2                        # Self-hosted fantasy font (COMPLETE)
│   └── Cinzel-LatinExt.woff2                     # Extended charset (COMPLETE)
├── src/
│   ├── index.css                                 # Design tokens: :root + .dark oklch values (NEEDS UPDATE: accent tokens)
│   ├── hooks/
│   │   ├── useTheme.ts                           # Theme hook (COMPLETE)
│   │   └── useTheme.test.ts                      # 8 tests (COMPLETE)
│   └── components/
│       ├── shared/ThemeToggle/
│       │   ├── ThemeToggle.tsx                    # Toggle component (COMPLETE)
│       │   ├── ThemeToggle.test.tsx               # 7 tests (COMPLETE)
│       │   └── index.ts                          # Barrel export (COMPLETE)
│       ├── TopToolbar/TopToolbar.tsx              # ThemeToggle integrated (COMPLETE)
│       └── ui/
│           ├── dialog.tsx                         # font-heading on DialogTitle (COMPLETE)
│           ├── drawer.tsx                         # font-heading on DrawerTitle (COMPLETE)
│           ├── card.tsx                           # font-heading on CardTitle (COMPLETE)
│           ├── tooltip.tsx                        # Migrated to theme tokens (COMPLETE)
│           └── sonner.tsx                         # Migrated to theme tokens (COMPLETE)
└── vitest.setup.ts                               # matchMedia mock for tests (COMPLETE)
```

**Structure Decision**: Frontend-only change within existing `libris-maleficarum-app/` structure. No new directories needed beyond what already exists.

## Implementation Phases

### Phase A: Setup & Font Infrastructure (COMPLETE)

All setup work from prior sessions:

- Removed legacy `@theme` block (HSL values) from index.css
- Registered `--font-heading` and `--color-destructive-foreground` in `@theme inline`
- Downloaded Cinzel woff2 files to `public/fonts/`
- Added `<link rel="preload">` for font files in index.html
- Added `@font-face` declarations with `font-display: swap`
- Added heading font rule in `@layer base` for h1–h6

### Phase B: Colour Palette (COMPLETE — NEEDS PARTIAL UPDATE)

Prior sessions applied the full oklch fantasy palette to `:root` and `.dark`:

- Gold primary, royal blue secondary, light blue accent, chart colours, sidebar tokens
- All existing jest-axe tests pass with current values

**UPDATE NEEDED**: The `--accent` and `--sidebar-accent` tokens currently use light blue (hue 240). Per the latest spec clarification (FR-001), these must change to gold-toned values so that `bg-accent` / `hover:bg-accent` renders gold for interactive highlights, hover, and selection everywhere. The `--accent-foreground` and `--sidebar-accent-foreground` must also be verified for contrast against the new gold background.

### Phase C: Font Heading Propagation (COMPLETE)

- Added `font-heading` class to DialogTitle, DrawerTitle, CardTitle
- Headings (h1–h6) already use Cinzel via `@layer base` rule

### Phase D: Dark/Light Mode Toggle (COMPLETE)

- useTheme hook: reads localStorage, OS preference fallback, applies `.dark` class
- ThemeToggle component: Sun/Moon icons (target-mode convention), ghost button, accessible label
- Barrel export + TopToolbar integration (before NotificationBell)
- Inline `<script>` in index.html for flash prevention
- Full TDD: 8 hook tests + 7 component tests

### Phase E: Hardcoded Colour Migration (COMPLETE)

- tooltip.tsx: Migrated `border-slate-200 bg-slate-950 text-slate-50` → theme tokens
- sonner.tsx: Verified using theme tokens (no hardcoded colours found)

### Phase F: Accent Token Update — Gold for Interactive States (COMPLETE)

**What**: Change `--accent` and `--sidebar-accent` from light blue (hue 240) to a subtle gold tone (hue ~85) so all interactive highlights, hover states, and selection backgrounds use gold.

**Why**: FR-001 clarification — gold serves as the primary colour for "buttons, links, focus rings, highlights, hover states, and selection backgrounds." The `bg-accent` class is used across ~20+ component locations for hover/selection/focus states (buttons, context menus, select items, calendar, entity tree nodes, entity type selector, dialog close buttons).

**Affected tokens** (8 token changes across 2 CSS blocks):

| Token | Current (light blue) | New (gold-toned) |
|-------|---------------------|-------------------|
| `:root` `--accent` | `oklch(0.7 0.1 240)` | `oklch(0.92 0.04 85)` — subtle warm gold wash |
| `:root` `--accent-foreground` | `oklch(0.18 0.02 255)` | `oklch(0.18 0.02 255)` — unchanged, ~11.6:1 contrast |
| `:root` `--sidebar-accent` | `oklch(0.93 0.01 250)` | `oklch(0.92 0.04 85)` — matches accent |
| `:root` `--sidebar-accent-foreground` | `oklch(0.18 0.02 255)` | `oklch(0.18 0.02 255)` — unchanged |
| `.dark` `--accent` | `oklch(0.6 0.1 240)` | `oklch(0.30 0.04 85)` — dark warm gold-amber |
| `.dark` `--accent-foreground` | `oklch(0.17 0.03 255)` | `oklch(0.93 0.01 80)` — warm cream, ~7.7:1 contrast |
| `.dark` `--sidebar-accent` | `oklch(0.27 0.02 255)` | `oklch(0.30 0.04 85)` — matches accent |
| `.dark` `--sidebar-accent-foreground` | `oklch(0.93 0.01 80)` | `oklch(0.93 0.01 80)` — unchanged |

**Design constraints for new accent values**:

1. Must be visually gold (hue ~80–85) not blue
2. Must be subtler/lighter than `--primary` (`oklch(0.75 0.15 85)`) to differentiate hover from active/focused
3. Must maintain ≥ 4.5:1 contrast with `--accent-foreground` text
4. Light mode: should be a very subtle warm gold tint that works as a hover/selection background
5. Dark mode: should be a darker warm gold tone that works against dark backgrounds
6. Must not clash with `--muted` (currently `oklch(0.93 0.01 250)` light / `oklch(0.27 0.02 255)` dark)

**Approach**:

1. Research optimal oklch gold-toned accent values meeting all constraints
2. Update research.md with new values and contrast verification
3. Update `:root` and `.dark` blocks in `index.css`
4. Update `--chart-3` if it was referencing old accent blue (currently `oklch(0.7 0.1 240)`)
5. Run full test suite — all 600+ tests must pass
6. Visual verification of hover/selection states across all components

### Phase G: Validation (REMAINING — MANUAL)

Visual and accessibility verification tasks:

- Toggle both modes on all major screens — verify gold highlights everywhere
- Spot-check contrast ratios (foreground-on-background, primary-on-primary, accent-on-accent)
- Tab through interactive elements — verify gold focus ring visibility (≥ 3:1)
- Verify hover states use gold (not blue) in: sidebar entity tree, buttons, context menus, select items, calendar
- Verify disabled controls are distinguishable and not keyboard focusable
- Verify narrow viewport toggle visibility
- Verify font fallback by blocking custom font load in DevTools
- Run quickstart.md walkthrough

## Complexity Tracking

No constitution violations to justify. The feature is entirely CSS-token-level with one small React hook and component.
