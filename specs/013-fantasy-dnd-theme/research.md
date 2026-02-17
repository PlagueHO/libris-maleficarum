# Research: Fantasy D&D Theme Styling

**Feature**: 013-fantasy-dnd-theme
**Date**: 2026-02-17

## 1. Fantasy Font Selection

### Decision: Cinzel (Google Fonts)

**Rationale**: Cinzel is a modern serif typeface inspired by classical Roman inscriptions with a contemporary feel. It is widely used in fantasy/RPG contexts, has excellent readability at heading sizes, and is freely available on Google Fonts with an OFL licence. It pairs well with sans-serif body text like Inter.

**Alternatives considered**:

- **MedievalSharp**: Too ornamental — poor readability at smaller heading sizes, limited weights.
- **Pirata One**: Strong pirate/fantasy feel but only one weight, too narrow for general headings.
- **Uncial Antiqua**: Authentic medieval feel but too stylised for a modern application.
- **EB Garamond**: Elegant serif but not distinctly "fantasy" enough to evoke D&D.
- **Cinzel Decorative**: More ornamental variant of Cinzel — too heavy for UI headings; reserve for logo if needed.
- **Almendra**: Good fantasy feel but limited language support and fewer weights than Cinzel.

**Font weights needed**: 400 (Regular) and 700 (Bold) — sufficient for h1–h6 hierarchy.

**Loading strategy**: Self-host via `@font-face` in `index.css` with `font-display: swap` to avoid layout shifts. Download woff2 files and place in `public/fonts/`. This avoids external CDN dependency and ensures fastest load. Alternatively, use Google Fonts `<link>` in `index.html` with `display=swap` parameter.

## 2. Colour Palette — oklch Values

### Decision: oklch colour system (matching existing codebase convention)

The existing `index.css` uses oklch for all `:root` and `.dark` variables. The fantasy palette continues this convention.

**Rationale**: oklch is perceptually uniform — contrast ratios can be reasoned about more intuitively. Consistent with existing code pattern.

### Light Mode Palette (`:root`)

| Token | Colour Description | oklch Value | Contrast Notes |
|-------|-------------------|-------------|----------------|
| `--background` | Warm off-white (parchment tint) | `oklch(0.97 0.008 80)` | Base surface |
| `--foreground` | Very dark navy-charcoal | `oklch(0.18 0.02 255)` | ~15:1 on background |
| `--card` | Slightly warmer white | `oklch(0.98 0.006 80)` | Card surfaces |
| `--card-foreground` | Same as foreground | `oklch(0.18 0.02 255)` | ~15:1 on card |
| `--popover` | Same as card | `oklch(0.98 0.006 80)` | Popover bg |
| `--popover-foreground` | Same as foreground | `oklch(0.18 0.02 255)` | |
| `--primary` | Rich gold | `oklch(0.75 0.15 85)` | Buttons, links |
| `--primary-foreground` | Dark navy (text on gold bg) | `oklch(0.18 0.02 255)` | ~8:1 on primary |
| `--secondary` | Royal blue | `oklch(0.45 0.15 260)` | Sidebar, cards |
| `--secondary-foreground` | White/cream | `oklch(0.97 0.008 80)` | ~7:1 on secondary |
| `--muted` | Very light blue-grey | `oklch(0.93 0.01 250)` | Subtle bg |
| `--muted-foreground` | Medium blue-grey | `oklch(0.5 0.03 255)` | ~5:1 on muted |
| `--accent` | Light blue | `oklch(0.7 0.1 240)` | Highlights |
| `--accent-foreground` | Dark navy | `oklch(0.18 0.02 255)` | ~8:1 on accent |
| `--destructive` | Red (unchanged role) | `oklch(0.577 0.245 27.325)` | Error states |
| `--destructive-foreground` | White/cream (text on red bg) | `oklch(0.985 0 0)` | ~12:1 on destructive |
| `--border` | Light blue-grey | `oklch(0.88 0.015 250)` | Subtle borders |
| `--input` | Same as border | `oklch(0.88 0.015 250)` | Input borders |
| `--ring` | Gold (focus ring) | `oklch(0.75 0.15 85)` | Matches primary |
| `--sidebar` | Soft off-white | `oklch(0.96 0.01 80)` | Sidebar bg |
| `--sidebar-foreground` | Dark navy | `oklch(0.18 0.02 255)` | |
| `--sidebar-primary` | Gold | `oklch(0.75 0.15 85)` | Active items |
| `--sidebar-primary-foreground` | Dark navy | `oklch(0.18 0.02 255)` | |
| `--sidebar-accent` | Light blue-grey | `oklch(0.93 0.01 250)` | Hover bg |
| `--sidebar-accent-foreground` | Dark navy | `oklch(0.18 0.02 255)` | |
| `--sidebar-border` | Border | `oklch(0.88 0.015 250)` | |
| `--sidebar-ring` | Gold | `oklch(0.75 0.15 85)` | |

### Dark Mode Palette (`.dark`)

| Token | Colour Description | oklch Value | Contrast Notes |
|-------|-------------------|-------------|----------------|
| `--background` | Deep navy-blue | `oklch(0.17 0.03 255)` | Dark base |
| `--foreground` | Warm cream white | `oklch(0.93 0.01 80)` | ~12:1 on background |
| `--card` | Slightly lighter navy | `oklch(0.22 0.03 255)` | Card surfaces |
| `--card-foreground` | Warm cream | `oklch(0.93 0.01 80)` | ~9:1 on card |
| `--popover` | Same as card | `oklch(0.22 0.03 255)` | |
| `--popover-foreground` | Warm cream | `oklch(0.93 0.01 80)` | |
| `--primary` | Bright gold | `oklch(0.8 0.16 85)` | Buttons, links |
| `--primary-foreground` | Deep navy | `oklch(0.17 0.03 255)` | ~9:1 on primary |
| `--secondary` | Medium royal blue | `oklch(0.5 0.14 260)` | Sidebar accents |
| `--secondary-foreground` | Warm cream | `oklch(0.93 0.01 80)` | ~5:1 on secondary |
| `--muted` | Dark desaturated blue | `oklch(0.27 0.02 255)` | Subtle bg |
| `--muted-foreground` | Light blue-grey | `oklch(0.65 0.03 250)` | ~4.5:1 on muted |
| `--accent` | Medium light blue | `oklch(0.6 0.1 240)` | Highlights |
| `--accent-foreground` | Deep navy | `oklch(0.17 0.03 255)` | ~6:1 on accent |
| `--destructive` | Bright red | `oklch(0.704 0.191 22.216)` | Error states |
| `--destructive-foreground` | Warm cream | `oklch(0.985 0 0)` | ~12:1 on destructive |
| `--border` | Dark blue-grey translucent | `oklch(1 0 0 / 12%)` | Subtle borders |
| `--input` | Slightly brighter | `oklch(1 0 0 / 16%)` | Input borders |
| `--ring` | Gold (focus ring) | `oklch(0.8 0.16 85)` | Matches primary |
| `--sidebar` | Dark navy (same as card) | `oklch(0.22 0.03 255)` | |
| `--sidebar-foreground` | Warm cream | `oklch(0.93 0.01 80)` | |
| `--sidebar-primary` | Bright gold | `oklch(0.8 0.16 85)` | |
| `--sidebar-primary-foreground` | Warm cream | `oklch(0.93 0.01 80)` | |
| `--sidebar-accent` | Muted blue | `oklch(0.27 0.02 255)` | |
| `--sidebar-accent-foreground` | Warm cream | `oklch(0.93 0.01 80)` | |
| `--sidebar-border` | Translucent white | `oklch(1 0 0 / 12%)` | |
| `--sidebar-ring` | Gold | `oklch(0.8 0.16 85)` | |

### Chart Colours (Fantasy-harmonised)

| Token | Light Mode | Dark Mode | Description |
|-------|-----------|-----------|-------------|
| `--chart-1` | `oklch(0.75 0.15 85)` | `oklch(0.8 0.16 85)` | Gold |
| `--chart-2` | `oklch(0.45 0.15 260)` | `oklch(0.55 0.14 260)` | Royal blue |
| `--chart-3` | `oklch(0.7 0.1 240)` | `oklch(0.6 0.1 240)` | Light blue |
| `--chart-4` | `oklch(0.577 0.245 27)` | `oklch(0.65 0.2 27)` | Ruby red |
| `--chart-5` | `oklch(0.6 0.12 155)` | `oklch(0.65 0.12 155)` | Forest green |

## 3. Hardcoded Colour Audit

Components using hardcoded Tailwind colours that need migration:

1. **tooltip.tsx** — Uses `border-slate-200`, `bg-slate-950`, `text-slate-50`, `dark:border-slate-800`, `dark:bg-slate-50`, `dark:text-slate-900`. Should migrate to theme tokens (`bg-popover`, `text-popover-foreground`, `border-border`).
2. **EntityContextMenu.tsx** — Uses `text-red-600`, `focus:bg-red-50`, `dark:focus:bg-red-900/10`. These are for destructive actions (delete) and can stay as semantic red, or migrate to `text-destructive`.
3. **NotificationItem.tsx** — Uses `text-blue-500`, `text-green-600`, `text-red-600` for status icons. These use colour alongside icons (Loader2, CheckCircle, XCircle), so colour is not the sole info indicator. Can stay as is or migrate to theme tokens for consistency.

## 4. Dark Mode Mechanism

**Decision**: Existing `.dark` class mechanism is preserved. A toggle switch is added to the top toolbar.

**Current implementation**: `@custom-variant dark (&:is(.dark *))` in index.css with TailwindCSS v4 syntax. Dark mode is toggled by adding/removing the `.dark` class on the root element.

**Current gap**: There is no UI control to toggle dark mode. The `.dark` class CSS definitions exist, but no JavaScript manages the toggle. A toggle switch must be added.

### Toggle Component Placement

The toggle should be placed in the `TopToolbar` component, inside the `ml-auto flex items-center gap-2` container, immediately before the `NotificationBell` component.

### Theme Persistence Strategy

- **Storage**: `localStorage` with key `theme` and values `"dark"`, `"light"`, or `"system"`.
- **Initial load**: Read `localStorage` first. If no preference, check `prefers-color-scheme` media query. Fall back to light mode.
- **Flash prevention**: Apply `.dark` class early (either via inline `<script>` in `index.html` before React mounts, or synchronously in a React hook before first paint) to avoid a flash of the wrong theme.

### Icon Convention

- Lucide `Sun` icon when light mode is active (clicking switches to dark).
- Lucide `Moon` icon when dark mode is active (clicking switches to light).
- This follows the convention of showing the current state, which aligns with common icon toggle patterns.

### Accessibility Requirements

- The toggle button must use `aria-label` describing the action: "Switch to dark mode" (when light) or "Switch to light mode" (when dark).
- It must be in the tab order and activatable via Enter or Space.
- Focus indicator must be the gold ring consistent with the theme.
- The toggle must not use colour alone to indicate state — the sun/moon icons provide a non-colour indicator.

## 5. Existing Theme Structure

The `index.css` has a dual-layer approach:

1. **`@theme` block** (lines 6–40): HSL-based values from a previous theme attempt. **This must be REMOVED** — it is a dead legacy pattern that conflicts with the oklch `:root`/`.dark` values. Per Shadcn/UI Tailwind v4 best practices, only `@theme inline` with `var()` references should be used alongside `:root`/`.dark` oklch values.
2. **`@theme inline` block** (lines 48–82): Maps CSS custom properties to Tailwind utilities (`--color-*: var(--*)`). This is the correct Tailwind v4 pattern and must be preserved.
3. **`:root` block** (lines 84–119): oklch light mode values.
4. **`.dark` block** (lines 121–152): oklch dark mode values.

**Plan**: Remove the old `@theme` block entirely. Update `:root` and `.dark` blocks with new oklch values. The `@theme inline` block stays as-is since it just maps `var()` references. Additionally, register the heading font family in `@theme inline` as `--font-heading` to enable the `font-heading` Tailwind utility class.
