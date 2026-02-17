# Data Model: Fantasy D&D Theme Styling

**Feature**: 013-fantasy-dnd-theme
**Date**: 2026-02-17

## Overview

This feature has no persistent data entities. The "data model" consists of CSS design tokens (custom properties) and font declarations defined in `index.css`.

## Design Tokens

### Colour Token Taxonomy

All colour tokens live as CSS custom properties in `:root` (light) and `.dark` (dark) scopes.

| Token | Role | Used By |
|-------|------|---------|
| `--primary` / `--primary-foreground` | Gold — buttons, links, focus rings | Button default variant, link text, focus-visible rings |
| `--secondary` / `--secondary-foreground` | Royal blue — sidebar accents, cards | Button secondary variant, card headers |
| `--accent` / `--accent-foreground` | Light blue — highlights, hover states | Hover backgrounds, info highlights |
| `--muted` / `--muted-foreground` | Desaturated blue-grey — subtle backgrounds | Disabled states, placeholder text |
| `--destructive` / `--destructive-foreground` | Red — error states | Delete buttons, error messages |
| `--background` / `--foreground` | Page surface / body text | html/body, main content |
| `--card` / `--card-foreground` | Card surface / card text | Card component |
| `--popover` / `--popover-foreground` | Popover surface / text | Dialog, popover, dropdown |
| `--border` | Border colour | All border utilities |
| `--input` | Input border colour | Form inputs |
| `--ring` | Focus ring colour (gold) | Focus-visible outline |
| `--sidebar-*` | Sidebar-specific variants | WorldSidebar component |
| `--chart-1` through `--chart-5` | Data visualisation | Chart components |

### Font Token Taxonomy

| Token | Application | Value |
|-------|------------|-------|
| `font-family` (body) | All non-heading text | `'Inter', system-ui, -apple-system, sans-serif` |
| `font-family` (headings) | h1–h6 only | `'Cinzel', Georgia, 'Times New Roman', serif` |

## State Transitions

N/A — no stateful entities. Dark/light mode toggling is handled by the existing `.dark` class mechanism.

## Validation Rules

- All text/background pairings must achieve ≥ 4.5:1 contrast (WCAG AA normal text).
- All large text/background pairings must achieve ≥ 3:1 contrast (WCAG AA large text).
- All interactive element boundaries must achieve ≥ 3:1 contrast against adjacent colours.
- Font fallback chain must be defined so headings never render in the browser default serif.
