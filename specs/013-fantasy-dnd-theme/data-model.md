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
| `font-family` (body) | All non-heading/non-title text | `'Inter', system-ui, -apple-system, sans-serif` |
| `font-family` (headings) | h1–h6, dialog titles, panel/section headers | `'Cinzel', Georgia, 'Times New Roman', serif` |

## State Transitions

### Mode Preference State

The user's dark/light mode preference is a client-side state with the following transitions:

| From | Trigger | To | Side Effect |
|------|---------|----|---------|
| No preference (first visit) | Page load | OS-preferred mode (or light) | `.dark` class applied if dark; preference not yet persisted |
| Light mode | User clicks toggle | Dark mode | `.dark` class added to root; preference saved to browser storage |
| Dark mode | User clicks toggle | Light mode | `.dark` class removed from root; preference saved to browser storage |
| Persisted preference | Page reload | Persisted mode | `.dark` class applied before first paint |
| Persisted preference | User clears browser data | No preference | Falls back to OS preference on next load |

## Validation Rules

- All text/background pairings must achieve ≥ 4.5:1 contrast (WCAG AA normal text).
- All large text/background pairings must achieve ≥ 3:1 contrast (WCAG AA large text).
- All interactive element boundaries must achieve ≥ 3:1 contrast against adjacent colours.
- Font fallback chain must be defined so headings never render in the browser default serif.
