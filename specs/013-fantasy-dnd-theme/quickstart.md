# Quickstart: Fantasy D&D Theme Styling

**Feature**: 013-fantasy-dnd-theme

## What This Feature Does

Updates the Libris Maleficarum UI from a neutral grey/brown theme to a fantasy D&D theme using gold (primary), royal blue (secondary), and light blue (accent) colours. Adds the Cinzel font for headings (h1–h6), dialog titles, and panel/section headers. Supports both light and dark modes with WCAG 2.2 AA compliant contrast.

## Files Changed

| File | Change |
|------|--------|
| `libris-maleficarum-app/index.html` | Add Cinzel font preload link |
| `libris-maleficarum-app/src/index.css` | Update all colour tokens in `@theme`, `:root`, `.dark` blocks; add `@font-face` for Cinzel; add heading font rule |
| `libris-maleficarum-app/src/components/ui/dialog.tsx` | Add `font-heading` class to `DialogTitle` |
| `libris-maleficarum-app/src/components/ui/drawer.tsx` | Add `font-heading` class to `DrawerTitle` |
| `libris-maleficarum-app/src/components/ui/card.tsx` | Add `font-heading` class to `CardTitle` |
| `libris-maleficarum-app/src/components/ui/tooltip.tsx` | Replace hardcoded slate colours with theme tokens |
| `libris-maleficarum-app/src/components/ui/sonner.tsx` | Review only — modify only if hardcoded colours found |
| `libris-maleficarum-app/src/hooks/useTheme.ts` | NEW: Theme state management hook (localStorage + `.dark` class) |
| `libris-maleficarum-app/src/components/shared/ThemeToggle/ThemeToggle.tsx` | NEW: Dark/light mode toggle button component |
| `libris-maleficarum-app/src/components/shared/ThemeToggle/ThemeToggle.test.tsx` | NEW: Tests for toggle behaviour and accessibility |
| `libris-maleficarum-app/src/components/shared/ThemeToggle/index.ts` | NEW: Barrel export |
| `libris-maleficarum-app/src/components/TopToolbar/TopToolbar.tsx` | Add ThemeToggle before NotificationBell |

## How to Verify

1. Run `pnpm dev` in `libris-maleficarum-app/`
1. Open <https://127.0.0.1:4000>
1. Verify dark mode: deep navy backgrounds, gold buttons/links, royal blue sidebar accents
1. Toggle to light mode using the sun/moon toggle in the toolbar (left of notification bell): warm off-white backgrounds, gold buttons, royal blue accents
1. Verify the toggle shows a moon icon in light mode (click to switch to dark) and a sun icon in dark mode (click to switch to light)
1. Reload the page and confirm the selected mode persists
1. Inspect any h1–h6 element, dialog title, or panel header: should use Cinzel font
1. Tab through interactive elements (including the toggle): focus rings should be gold and clearly visible
1. Run `pnpm test` — all existing tests (including jest-axe a11y checks) should pass

## Key Design Decisions

- **Gold as primary**: Most distinctive colour, makes CTAs stand out against blue backgrounds.
- **Cinzel for headings + titles**: Applying the fantasy font to semantic headings plus dialog/panel titles covers all visually prominent text without overusing the decorative font on smaller UI labels.
- **Self-hosted font**: Avoids external CDN dependency; `font-display: swap` for zero-FOIT.
- **oklch colour format**: Matches existing codebase convention, perceptually uniform.
- **Subtle parchment tint**: Light mode uses `oklch(0.97 0.008 80)` — warm cream, not tan.
- **Toggle with target-mode icon**: Sun icon in dark mode (switch to light), Moon icon in light mode (switch to dark) — shows what clicking will do.
- **localStorage persistence**: User's mode choice persists across reloads; defaults to OS preference on first visit.
