# Quickstart: Fantasy D&D Theme Styling

**Feature**: 013-fantasy-dnd-theme

## What This Feature Does

Updates the Libris Maleficarum UI from a neutral grey/brown theme to a fantasy D&D theme using gold (primary), royal blue (secondary), and light blue (accent) colours. Adds the Cinzel font for headings (h1–h6) only. Supports both light and dark modes with WCAG 2.2 AA compliant contrast.

## Files Changed

| File | Change |
|------|--------|
| `libris-maleficarum-app/index.html` | Add Cinzel font preload link |
| `libris-maleficarum-app/src/index.css` | Update all colour tokens in `@theme`, `:root`, `.dark` blocks; add `@font-face` for Cinzel; add heading font rule |
| `libris-maleficarum-app/src/components/ui/tooltip.tsx` | Replace hardcoded slate colours with theme tokens |
| `libris-maleficarum-app/src/components/ui/sonner.tsx` | Review only — modify only if hardcoded colours found |

## How to Verify

1. Run `pnpm dev` in `libris-maleficarum-app/`
2. Open https://127.0.0.1:4000
3. Verify dark mode: deep navy backgrounds, gold buttons/links, royal blue sidebar accents
4. Toggle to light mode: warm off-white backgrounds, gold buttons, royal blue accents
5. Inspect any h1–h6 element: should use Cinzel font
6. Tab through interactive elements: focus rings should be gold and clearly visible
7. Run `pnpm test` — all existing tests (including jest-axe a11y checks) should pass

## Key Design Decisions

- **Gold as primary**: Most distinctive colour, makes CTAs stand out against blue backgrounds.
- **Cinzel for headings only**: Clean modern fantasy feel without reducing body text readability.
- **Self-hosted font**: Avoids external CDN dependency; `font-display: swap` for zero-FOIT.
- **oklch colour format**: Matches existing codebase convention, perceptually uniform.
- **Subtle parchment tint**: Light mode uses `oklch(0.97 0.008 80)` — warm cream, not tan.
