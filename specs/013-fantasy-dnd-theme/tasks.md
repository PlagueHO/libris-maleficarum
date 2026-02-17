# Tasks: Fantasy D&D Theme Styling

**Input**: Design documents from `/specs/013-fantasy-dnd-theme/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, quickstart.md

**Tests**: Not explicitly requested. Existing jest-axe a11y tests act as regression validation — run the full test suite as a verification task, not as new test authoring.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Frontend**: `libris-maleficarum-app/src/` for source, `libris-maleficarum-app/public/` for static assets

---

## Phase 1: Setup

**Purpose**: Remove legacy CSS conflicts and prepare index.css for new theme tokens

- [ ] T001 Remove the old `@theme { }` block (lines ~6–40, HSL values) from `libris-maleficarum-app/src/index.css` — this legacy block conflicts with oklch `:root`/`.dark` values per Shadcn/UI Tailwind v4 best practices
- [ ] T002 Add `--color-destructive-foreground: var(--destructive-foreground);` to the `@theme inline` block in `libris-maleficarum-app/src/index.css` — missing Shadcn/UI standard token
- [ ] T003 Register heading font variable `--font-heading: 'Cinzel', Georgia, 'Times New Roman', serif;` in the `@theme inline` block in `libris-maleficarum-app/src/index.css`

---

## Phase 2: Foundational (Font Infrastructure)

**Purpose**: Add the Cinzel fantasy font — MUST complete before typography stories can be verified

**⚠️ CRITICAL**: Font files and declarations must be in place before Phase 4 (US3) can be validated

- [ ] T004 [P] Download Cinzel Regular (400) and Bold (700) woff2 files and place in `libris-maleficarum-app/public/fonts/Cinzel-Regular.woff2` and `libris-maleficarum-app/public/fonts/Cinzel-Bold.woff2`
- [ ] T005 [P] Add `<link rel="preload" href="/fonts/Cinzel-Regular.woff2" as="font" type="font/woff2" crossorigin>` and same for Bold in `libris-maleficarum-app/index.html`
- [ ] T006 Add `@font-face` declarations for Cinzel Regular and Bold with `font-display: swap` in `libris-maleficarum-app/src/index.css`
- [ ] T007 Add CSS rule `h1, h2, h3, h4, h5, h6 { font-family: 'Cinzel', Georgia, 'Times New Roman', serif; }` in `libris-maleficarum-app/src/index.css`

**Checkpoint**: Font infrastructure ready — headings render in Cinzel with Georgia fallback, body text stays Inter

---

## Phase 3: User Story 1 — Consistent Fantasy Theme Across Pages (Priority: P1) 🎯 MVP

**Goal**: Replace all colour tokens in `:root` and `.dark` with the fantasy D&D palette (gold primary, royal blue secondary, light blue accent). All ~22 Shadcn/UI components inherit the new theme automatically.

**Independent Test**: Navigate every screen (world sidebar, main panel, chat panel, toolbar) in both modes and confirm the fantasy colour palette is applied consistently. Run `pnpm test` — all jest-axe tests pass.

### Implementation for User Story 1

- [ ] T008 [US1] Replace all oklch values **except** `--chart-1` through `--chart-5` in the `:root` block with the light-mode fantasy palette from research.md Section 2 in `libris-maleficarum-app/src/index.css`
- [ ] T009 [US1] Add `--destructive-foreground: oklch(0.985 0 0);` to the `:root` block in `libris-maleficarum-app/src/index.css`
- [ ] T010 [US1] Replace all oklch values **except** `--chart-1` through `--chart-5` in the `.dark` block with the dark-mode fantasy palette from research.md Section 2 in `libris-maleficarum-app/src/index.css`
- [ ] T011 [US1] Add `--destructive-foreground: oklch(0.985 0 0);` to the `.dark` block in `libris-maleficarum-app/src/index.css`
- [ ] T012 [US1] Update `--chart-1` through `--chart-5` in the `:root` block with fantasy-harmonised light-mode chart colours from research.md Section 2 in `libris-maleficarum-app/src/index.css`
- [ ] T013 [US1] Update `--chart-1` through `--chart-5` in the `.dark` block with fantasy-harmonised dark-mode chart colours from research.md Section 2 in `libris-maleficarum-app/src/index.css`
- [ ] T014 [US1] Run `pnpm test` in `libris-maleficarum-app/` — all existing jest-axe a11y tests must pass with the new colour tokens

**Checkpoint**: User Story 1 complete — the full fantasy colour palette is active in both modes, all components display themed colours, all a11y tests pass

---

## Phase 4: User Story 3 — Fantasy Header Typography (Priority: P2)

**Goal**: Headings (h1–h6) render in Cinzel fantasy font. All other text stays in Inter sans-serif. Font loading is graceful with zero layout shift.

**Independent Test**: Inspect any heading element — it uses Cinzel. Inspect body text, toolbar labels, sidebar headers, dialog titles — they all use Inter. Block font load in DevTools — headings fall back to Georgia.

**Note**: Font infrastructure was set up in Phase 2 (T004–T007). This phase confirms it works end-to-end.

- [ ] T015 [US3] Verify headings render in Cinzel and body/toolbar/dialog text renders in Inter across all screens — no code change expected (if issues found, create ad-hoc fix tasks before proceeding)
- [ ] T016 [US3] Verify font fallback by blocking custom font load in browser DevTools — headings should display in Georgia serif without layout shift

**Checkpoint**: User Story 3 complete — fantasy header typography confirmed working, including fallback

---

## Phase 5: User Story 2 — Dark/Light Mode Support (Priority: P1) & User Story 4 — Accessible Interactive Elements (Priority: P2)

**Goal**: Confirm both modes present a harmonious fantasy theme with proper contrast, and that all interactive elements have visible focus indicators and meet accessibility requirements.

**Independent Test**: Toggle between dark and light mode on every screen. Tab through all interactive elements. Verify focus ring visibility (gold ring) and hover state distinguishability in both modes.

**Note**: User Stories 2 and 4 are coalesced here because they are validation-only — the colour token work in Phase 3 delivers both. No new source code tasks; this is verification.

- [ ] T017 [US2] Toggle between dark and light mode on all major screens and verify no elements appear invisible, unreadable, or mismatched
- [ ] T018 [US2] Spot-check contrast ratios with browser DevTools: foreground-on-background, primary-foreground-on-primary, muted-foreground-on-muted in both modes — all must meet WCAG AA (4.5:1 normal, 3:1 large)
- [ ] T019 [US4] Tab through all interactive elements and verify gold focus ring is clearly visible with ≥3:1 contrast against adjacent colours in both modes
- [ ] T020 [US4] Verify hover states on buttons are visually distinguishable from default state without relying on colour alone

**Checkpoint**: User Stories 2 and 4 confirmed — both modes are harmonious, all interactive elements are accessible

---

## Phase 6: User Story 5 — Colour Not Used as Sole Information Indicator (Priority: P3)

**Goal**: Ensure the new theme does not regress existing non-colour indicators for error states, selection states, and status indicators.

**Independent Test**: View the app with a colour blindness simulator and confirm all states remain distinguishable via shape, text, or iconography.

- [ ] T021 [US5] Verify form error states display both colour change and text error message in `libris-maleficarum-app/src/` — confirm no regression
- [ ] T022 [US5] Verify selected sidebar items use background change plus a visual indicator (border or icon) — not colour alone — in `libris-maleficarum-app/src/components/WorldSidebar/`

**Checkpoint**: User Story 5 confirmed — no colour-only information indicators

---

## Phase 7: Hardcoded Colour Migration & Polish

**Purpose**: Migrate remaining hardcoded Tailwind colour classes to theme tokens and final cleanup

- [ ] T023 [TDD] Verify `tooltip.test.tsx` exists and covers tooltip rendering with theme tokens — if missing, create a minimal test asserting tooltip uses `bg-popover text-popover-foreground border-border` classes
- [ ] T024 Replace `border-slate-200 bg-slate-950 text-slate-50` with `border-border bg-popover text-popover-foreground` and remove `dark:border-slate-800 dark:bg-slate-50 dark:text-slate-900` in `libris-maleficarum-app/src/components/ui/tooltip.tsx`
- [ ] T025 Review `libris-maleficarum-app/src/components/ui/sonner.tsx` for any non-theme hardcoded colour usage and migrate if found
- [ ] T026 Run full test suite `pnpm test` in `libris-maleficarum-app/` — confirm all tests pass after tooltip/sonner migration
- [ ] T027 Run quickstart.md validation — follow all steps in `specs/013-fantasy-dnd-theme/quickstart.md` and confirm expected results

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on T001 (legacy `@theme` removal) — font additions can start in parallel with T002/T003
- **User Story 1 (Phase 3)**: Depends on Phase 1 completion (clean `index.css`)
- **User Story 3 (Phase 4)**: Depends on Phase 2 completion (font files in place)
- **User Stories 2 & 4 (Phase 5)**: Depends on Phase 3 completion (colour tokens applied)
- **User Story 5 (Phase 6)**: Depends on Phase 3 completion (new theme active)
- **Polish (Phase 7)**: Can start after Phase 3 — independent of Phases 4–6

### Within Each Phase

- T008/T009 (`:root` light) and T010/T011 (`.dark` dark) can run sequentially within the same file
- T012/T013 (charts) depend on T008/T010 being applied first
- T004 and T005 are parallel (different files: font download vs index.html)
- T024 and T025 are parallel (different files: tooltip.tsx vs sonner.tsx)

### Parallel Opportunities

```text
Phase 1:  T001 → T002 + T003 (parallel after @theme removal)
Phase 2:  T004 ∥ T005 (different files) → T006 → T007
Phase 3:  T008 → T009 → T010 → T011 → T012 → T013 → T014
Phase 7:  T023 → T024 ∥ T025 (different files) → T026 → T027
```

---

## Implementation Strategy

### MVP First (Phase 1 + Phase 2 + Phase 3)

1. Complete Phase 1: Remove legacy CSS, register missing tokens
2. Complete Phase 2: Font setup
3. Complete Phase 3: Full colour palette in both modes
4. **STOP and VALIDATE**: Run `pnpm test`, visually verify all screens
5. This delivers the core fantasy theme — Users see gold/blue/light-blue palette + Cinzel headings

### Incremental Delivery

1. Phases 1–3 → MVP: Fantasy colour palette + font ✅
2. Phase 4 → Verify typography ✅
3. Phase 5 → Verify dark/light mode + interactive accessibility ✅
4. Phase 6 → Verify colour-independence of indicators ✅
5. Phase 7 → Polish: migrate hardcoded colours + final validation ✅

---

## Notes

- All colour values reference `specs/013-fantasy-dnd-theme/research.md` Section 2 (oklch format)
- Only ~4 files are modified: `index.css` (primary), `index.html`, `tooltip.tsx`, possibly `sonner.tsx`
- ~22 Shadcn/UI components inherit theme changes automatically via CSS custom properties
- Phases 4–6 are validation-only — no source code changes expected (unless issues found)
- Commit after each phase for clean rollback points
- **Phase mapping**: Tasks are organized by user story, not by plan phase. Plan phases A–F correspond loosely to: A → Phase 2, B+C+D → Phases 1+3, E → Phase 7, F → Phases 4–6
