# Implementation Plan: Fantasy D&D Theme Styling

**Branch**: `013-fantasy-dnd-theme` | **Date**: 2026-02-17 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/013-fantasy-dnd-theme/spec.md`

## Summary

Update the Libris Maleficarum UI from a neutral grey/brown palette to a fantasy D&D theme using gold (primary), royal blue (secondary), and light blue (accent) colours. Add the Cinzel font for headings (h1–h6) only. Update all Shadcn/UI design tokens in `index.css` for both light and dark modes. Migrate hardcoded Tailwind colour classes in components to use theme tokens. All changes must maintain WCAG 2.2 Level AA contrast compliance.

## Technical Context

**Language/Version**: TypeScript 5.x, CSS  
**Primary Dependencies**: React 19, TailwindCSS v4, Shadcn/UI, Radix UI  
**Storage**: N/A (CSS-only changes, no persistent data)  
**Testing**: Vitest + Testing Library + jest-axe (existing test suite validates a11y)  
**Target Platform**: Web (modern browsers with oklch support)  
**Project Type**: Web (frontend only)  
**Performance Goals**: Font loads with `font-display: swap` (zero FOIT), no layout shift from font loading  
**Constraints**: All text ≥ 4.5:1 contrast (AA normal), all large text ≥ 3:1 (AA large), all interactive boundaries ≥ 3:1  
**Scale/Scope**: ~4 files changed, ~22 UI components inherit tokens automatically

## Constitution Check

*GATE: PASS — no violations. Re-checked after Phase 1 design: still PASS.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Cloud-Native Architecture | N/A | No infrastructure changes |
| II. Clean Architecture | PASS | CSS-only changes, no architecture affected |
| III. TDD (NON-NEGOTIABLE) | PASS | Existing jest-axe tests validate a11y compliance; run full test suite after changes |
| IV. Framework & Technology Standards | NOTE | Constitution references Fluent UI v9, but codebase already uses Shadcn/UI + Tailwind. No new deviation introduced by this feature. |
| V. Developer Experience | PASS | No dev tooling changes |
| VI. Security & Privacy | PASS | No secrets, auth, or network changes |
| VII. Semantic Versioning | PASS | MINOR version bump — visual enhancement, backward-compatible |

## Project Structure

### Documentation (this feature)

```text
specs/013-fantasy-dnd-theme/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0: font selection, colour values, audit
├── data-model.md        # Phase 1: design token taxonomy
├── quickstart.md        # Phase 1: verification guide
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (files to modify)

```text
libris-maleficarum-app/
├── index.html                                    # Add font preload link
├── src/
│   ├── index.css                                 # PRIMARY: all theme tokens + font rules
│   └── components/
│       └── ui/
│           └── tooltip.tsx                        # Migrate hardcoded slate colours
```

**Structure Decision**: Frontend-only change. All theme tokens live in `index.css` and propagate to all Shadcn/UI components via CSS custom properties. Only `tooltip.tsx` has hardcoded non-theme colours that need migration. `EntityContextMenu.tsx` and `NotificationItem.tsx` use semantic colour names (red for destructive, blue/green for status) alongside icons — acceptable per FR-009.

## Implementation Phases

### Phase A: Font Setup (FR-003, FR-004, FR-005, FR-012)

**Goal**: Add Cinzel fantasy font for headings.

1. Download Cinzel Regular (400) and Bold (700) woff2 files from Google Fonts
2. Place in `libris-maleficarum-app/public/fonts/Cinzel-Regular.woff2` and `Cinzel-Bold.woff2`
3. Add `@font-face` declarations in `index.css` with `font-display: swap`
4. Add CSS rule: `h1, h2, h3, h4, h5, h6 { font-family: 'Cinzel', Georgia, 'Times New Roman', serif; }`
5. Register the heading font as a Tailwind theme variable in `@theme inline`:
   ```css
   @theme inline {
     --font-heading: 'Cinzel', Georgia, 'Times New Roman', serif;
   }
   ```
   This enables the `font-heading` Tailwind utility class for components that need the fantasy font.
6. Add `<link rel="preload">` in `index.html` for the font files
7. Verify: headings use Cinzel, body text stays Inter; `font-heading` utility class works

### Phase B: Light Mode Colour Tokens (FR-001, FR-002, FR-006, FR-007, FR-011)

**Goal**: Update `:root` block with fantasy palette and remove legacy `@theme` block.

1. **Remove the old `@theme { }` block** (lines ~6–40 of `index.css`) — it contains dead HSL values from a previous theme attempt that conflict with the oklch `:root`/`.dark` values. Only `@theme inline` should remain, as this is the correct Tailwind v4 + Shadcn/UI pattern.
2. Replace all oklch values in `:root` block with gold/royal-blue/light-blue palette (see `research.md` for exact values)
3. Add the missing `--destructive-foreground` token to `:root` (Shadcn/UI standard; currently absent from the codebase)
4. Verify: light mode shows warm off-white backgrounds, gold primary, royal blue secondary, light blue accent
5. Run contrast checks on all token pairings

### Phase C: Dark Mode Colour Tokens (FR-001, FR-002, FR-006, FR-007, FR-011)

**Goal**: Update `.dark` block with fantasy palette.

1. Replace all oklch values in `.dark` block with dark fantasy palette (see `research.md`)
2. Add the missing `--destructive-foreground` token to `.dark` (Shadcn/UI standard; currently absent from the codebase)
3. Verify: dark mode shows deep navy backgrounds, bright gold primary, medium royal blue secondary
4. Run contrast checks on all token pairings

### Phase D: Chart Colours (FR-010)

**Goal**: Update chart tokens to harmonise with fantasy palette.

1. Update `--chart-1` through `--chart-5` in both `:root` and `.dark` blocks
2. Verify charts use gold, royal blue, light blue, ruby red, and forest green
3. Confirm all 5 colours are visually distinguishable from each other

### Phase E: Hardcoded Colour Migration (FR-006)

**Goal**: Replace hardcoded Tailwind colours with theme tokens.

1. **tooltip.tsx**: Replace `border-slate-200 bg-slate-950 text-slate-50` → `border-border bg-popover text-popover-foreground`; replace `dark:border-slate-800 dark:bg-slate-50 dark:text-slate-900` → remove (theme tokens handle dark mode automatically)
2. Review `sonner.tsx` for any non-theme colour usage

### Phase F: Accessibility Validation (FR-007, FR-008, FR-009)

**Goal**: Confirm all contrast and accessibility requirements are met.

1. Run full test suite: `pnpm test` — all jest-axe tests must pass
2. Manually verify keyboard focus ring visibility (gold ring on both dark and light backgrounds)
3. Spot-check contrast ratios with browser DevTools on key pairings:
   - Foreground on background (both modes)
   - Primary-foreground on primary (both modes)
   - Muted-foreground on muted (both modes)
4. Verify font fallback by temporarily blocking font load — headings should show Georgia

## Complexity Tracking

No constitution violations to justify. This feature adds zero new abstractions, patterns, or dependencies — it updates existing CSS custom property values and adds one font.

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| oklch values produce unexpected colours in some browsers | Low | Medium | Test in Chrome, Firefox, Safari; oklch has broad support since 2023 |
| Cinzel font not readable at small heading sizes | Low | Low | h5/h6 are rarely used; fallback serif (Georgia) is highly readable |
| Existing jest-axe tests fail with new contrast values | Medium | Low | Colour values are specifically chosen to exceed AA thresholds; fix any failures by adjusting token values |
| Legacy `@theme` block conflicts with new `:root` values | Medium | Medium | **Remove the old `@theme` block entirely** — per Shadcn/UI Tailwind v4 best practices, only `@theme inline` with `var()` references should be used alongside `:root`/`.dark` oklch values. The old block contains dead HSL values from a previous theme attempt. |
