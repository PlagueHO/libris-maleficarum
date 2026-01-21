# Research: Tailwind CSS Migration Patterns

**Phase**: 0 (Research & Discovery)  
**Date**: 2026-01-21  
**Status**: Complete

## Overview

This document captures technical research findings for migrating from CSS Modules to Tailwind CSS v4 with Shadcn/UI components. All unknowns from the Technical Context have been resolved.

---

## 1. Tailwind CSS v4 Best Practices

### Configuration Approach

**Decision**: Use CSS-based configuration (Tailwind v4 default)

Tailwind v4 has shifted to CSS-based configuration instead of `tailwind.config.js`. Configuration is done via CSS custom properties in `src/index.css`:

```css
@import "tailwindcss";

@theme {
  --color-primary: oklch(0.5 0.2 250);
  --font-sans: "Inter", system-ui, sans-serif;
  --spacing-huge: 10rem;
}
```

**Rationale**: Aligns with Tailwind v4 native approach, better performance, simpler mental model.

### Common Utility Patterns

**Flexbox Layouts**:

```tsx
// Horizontal flex container with gap
<div className="flex items-center gap-4">

// Vertical flex with space between
<div className="flex flex-col justify-between h-full">

// Centered content
<div className="flex items-center justify-center min-h-screen">
```

**Grid Layouts**:

```tsx
// Two-column form
<div className="grid grid-cols-2 gap-4">

// Responsive grid (1 col mobile, 3 cols desktop)
<div className="grid grid-cols-1 md:grid-cols-3 gap-6">
```

**Spacing**:

```tsx
// Padding and margin use same scale
<div className="p-4 m-2">       // padding: 1rem, margin: 0.5rem
<div className="px-6 py-3">     // horizontal 1.5rem, vertical 0.75rem
<div className="space-y-4">     // gap between children (vertical)
```

**Colors**:

```tsx
// Shadcn/UI uses CSS variables
<div className="bg-background text-foreground">
<div className="bg-primary text-primary-foreground">
<div className="border border-border">
```

### Responsive Design

**Breakpoints** (Tailwind defaults):

- `sm`: 640px
- `md`: 768px
- `lg`: 1024px
- `xl`: 1280px
- `2xl`: 1536px

**Pattern** (mobile-first):

```tsx
// Base styles apply to mobile, then override for larger screens
<div className="text-sm md:text-base lg:text-lg">
<div className="grid-cols-1 md:grid-cols-2 lg:grid-cols-3">
```

### Animations & Transitions

**Built-in animations**:

```tsx
<div className="transition-all duration-300 hover:scale-105">
<div className="animate-pulse">
<div className="animate-spin">  // for loading spinners
```

**Custom animations** (define in @theme):

```css
@theme {
  --animate-slide-in: slide-in 0.3s ease-out;
}

@keyframes slide-in {
  from { transform: translateX(-100%); }
  to { transform: translateX(0); }
}
```

### State Patterns

**Hover/Focus/Active**:

```tsx
<button className="bg-primary hover:bg-primary/90 focus:ring-2 focus:ring-primary active:scale-95">

// Group hover (hover parent affects child)
<div className="group">
  <span className="opacity-0 group-hover:opacity-100">
</div>
```

**Disabled state**:

```tsx
<button className="disabled:opacity-50 disabled:cursor-not-allowed" disabled>
```

---

## 2. Class Variance Authority (CVA) Patterns

### Basic Usage

**Installation**: ✅ Already installed (`class-variance-authority@0.7.1`)

**Simple variant example**:

```tsx
import { cva, type VariantProps } from "class-variance-authority";

const buttonVariants = cva(
  "inline-flex items-center justify-center rounded-md font-medium transition-colors focus-visible:outline-none focus-visible:ring-2",
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground hover:bg-primary/90",
        destructive: "bg-destructive text-destructive-foreground hover:bg-destructive/90",
        outline: "border border-input bg-background hover:bg-accent",
        ghost: "hover:bg-accent hover:text-accent-foreground",
      },
      size: {
        default: "h-10 px-4 py-2",
        sm: "h-9 px-3",
        lg: "h-11 px-8",
        icon: "h-10 w-10",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
);

type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof buttonVariants>;

export function Button({ variant, size, className, ...props }: ButtonProps) {
  return (
    <button 
      className={cn(buttonVariants({ variant, size }), className)} 
      {...props} 
    />
  );
}
```

### Compound Variants

**When variant combinations need special styling**:

```tsx
const cardVariants = cva("rounded-lg border", {
  variants: {
    variant: {
      default: "bg-background",
      elevated: "shadow-md",
    },
    size: {
      sm: "p-4",
      lg: "p-6",
    },
  },
  compoundVariants: [
    {
      variant: "elevated",
      size: "lg",
      class: "shadow-lg",  // larger shadow for elevated large cards
    },
  ],
});
```

### TypeScript Integration

**Extract prop types from CVA**:

```tsx
type ButtonVariants = VariantProps<typeof buttonVariants>;

// Use in component props
interface CustomButtonProps extends ButtonVariants {
  label: string;
  onClick: () => void;
}
```

### Shadcn/UI CVA Best Practices

**Study source**: Shadcn/UI Button component (`src/components/ui/button.tsx`) already uses CVA pattern. This serves as the reference implementation for all other components.

**Key observations**:

- Base classes go in first argument (always applied)
- Variants go in second argument object
- Use `cn()` utility to merge variant classes with custom className prop
- Export VariantProps type for consuming components
- Provide defaultVariants for common use case

---

## 3. Screenshot Testing Setup

### Tool Selection

**Decision**: Playwright for visual regression testing

**Rationale**:

- Native screenshot diffing built-in
- Cross-browser support (Chromium, Firefox, WebKit)
- Headless and headed modes
- CI/CD friendly
- Better than Percy (paid), BackstopJS (unmaintained), or Chromatic (expensive)

### Installation

```bash
cd libris-maleficarum-app
pnpm add -D @playwright/test
npx playwright install --with-deps chromium
```

### Configuration

**File**: `playwright.config.ts`

```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/visual',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  use: {
    baseURL: 'http://localhost:4000',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: 'pnpm dev',
    url: 'http://localhost:4000',
    reuseExistingServer: !process.env.CI,
  },
});
```

### Baseline Capture Process

**Step 1**: Create visual test file `tests/visual/components.spec.ts`:

```typescript
import { test, expect } from '@playwright/test';

test.describe('Component Visual Regression', () => {
  test('WorldSidebar - empty state', async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('[data-testid="world-sidebar"]');
    await expect(page.locator('[data-testid="world-sidebar"]')).toHaveScreenshot('world-sidebar-empty.png');
  });

  test('WorldDetailForm - create world', async ({ page }) => {
    await page.goto('/worlds/new');
    await page.waitForSelector('[data-testid="world-detail-form"]');
    await expect(page.locator('[data-testid="world-detail-form"]')).toHaveScreenshot('world-detail-form.png');
  });

  // Add test for each major component/page
});
```

**Step 2**: Generate baseline screenshots (BEFORE migration):

```bash
pnpm playwright test --update-snapshots
```

This creates `tests/visual/__screenshots__/` directory with baseline PNGs.

**Step 3**: Run tests after migration:

```bash
pnpm playwright test
```

Playwright automatically compares new screenshots against baselines and fails if differences exceed threshold.

### CI/CD Integration

**GitHub Actions example**:

```yaml
- name: Run Playwright visual tests
  run: pnpm playwright test
  
- name: Upload diff images
  if: failure()
  uses: actions/upload-artifact@v3
  with:
    name: playwright-report
    path: playwright-report/
```

### Updating Baselines

**When intentional changes**:

```bash
# Review diffs in playwright-report/index.html
pnpm playwright test --update-snapshots
git add tests/visual/__screenshots__/
git commit -m "chore: update visual baselines for [reason]"
```

### Optimal Viewport Sizes

**Test 3 viewports**:

- Mobile: 375x667 (iPhone SE)
- Tablet: 768x1024 (iPad)
- Desktop: 1920x1080 (common desktop)

```typescript
test('Component - mobile', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 667 });
  // ... test
});
```

---

## 4. CSS Module to Tailwind Mapping

### Analysis of Existing CSS Modules

**Files analyzed**:

1. `EmptyState.module.css`
1. `WorldSidebar.module.css`
1. `WorldSelector.module.css`
1. `EntityTreeNode.module.css`
1. `EntityTree.module.css`
1. `WorldDetailForm.module.css`
1. `DeleteConfirmationModal.module.css`

**Common patterns found**:

- Flexbox layouts (flex, flex-direction, align-items, justify-content)
- Spacing (margin, padding)
- Typography (font-size, font-weight, line-height)
- Colors (background-color, color, border-color)
- Borders (border, border-radius)
- Sizing (width, height, min-height, max-height)
- Transitions (transition, hover states)

### Mapping Guide

#### Flexbox → Tailwind

| CSS Module | Tailwind |
|------------|----------|
| `display: flex;` | `flex` |
| `flex-direction: column;` | `flex-col` |
| `align-items: center;` | `items-center` |
| `justify-content: space-between;` | `justify-between` |
| `gap: 16px;` | `gap-4` |
| `flex: 1;` | `flex-1` |

#### Spacing → Tailwind Scale

| CSS Module | Tailwind | Actual Value |
|------------|----------|--------------|
| `padding: 4px;` | `p-1` | 0.25rem |
| `padding: 8px;` | `p-2` | 0.5rem |
| `padding: 16px;` | `p-4` | 1rem |
| `margin: 12px;` | `m-3` | 0.75rem |
| `padding: 8px 16px;` | `px-4 py-2` | 0.5rem 1rem |

#### Colors → Shadcn/UI Variables

| CSS Module | Tailwind |
|------------|----------|
| `background-color: white;` | `bg-background` |
| `color: black;` | `text-foreground` |
| `background-color: #0078d4;` | `bg-primary` |
| `color: #323130;` | `text-muted-foreground` |
| `border-color: #e1e1e1;` | `border-border` |

**Note**: Shadcn/UI uses CSS variables for theming. Always prefer semantic color names over arbitrary values.

#### Typography

| CSS Module | Tailwind |
|------------|----------|
| `font-size: 14px;` | `text-sm` |
| `font-size: 16px;` | `text-base` |
| `font-weight: 600;` | `font-semibold` |
| `line-height: 1.5;` | `leading-normal` |
| `text-align: center;` | `text-center` |

#### Borders & Shadows

| CSS Module | Tailwind |
|------------|----------|
| `border: 1px solid #e1e1e1;` | `border border-border` |
| `border-radius: 4px;` | `rounded` |
| `border-radius: 8px;` | `rounded-lg` |
| `box-shadow: 0 2px 4px rgba(0,0,0,0.1);` | `shadow-sm` |

#### Complex Selectors

**Pseudo-elements** (use arbitrary values if needed):

```tsx
// Before: .item::before { content: "→"; }
<span className="before:content-['→'] before:mr-2">

// Before: .item:hover { opacity: 0.8; }
<div className="hover:opacity-80">
```

**Child combinators** (use group pattern):

```tsx
// Before: .parent:hover .child { color: blue; }
<div className="group">
  <span className="group-hover:text-blue-500">
</div>
```

**@apply directive** (use sparingly for complex repeated patterns):

```css
@layer components {
  .btn-complex {
    @apply px-4 py-2 rounded-lg bg-primary text-white hover:bg-primary/90 focus:ring-2;
  }
}
```

---

## 5. Test Migration Strategy

### Removing CSS Module Imports

**Before**:

```typescript
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import styles from './Component.module.css';
import Component from './Component';

it('renders with correct style', () => {
  render(<Component />);
  expect(screen.getByRole('button')).toHaveClass(styles.button);
});
```

**After**:

```typescript
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import Component from './Component';

it('renders with correct role', () => {
  render(<Component />);
  expect(screen.getByRole('button')).toBeInTheDocument();
});
```

### Alternative Query Strategies

**Option 1: data-testid** (explicit test hooks):

```tsx
// Component
<button data-testid="submit-button" className="btn-primary">

// Test
expect(screen.getByTestId('submit-button')).toBeInTheDocument();
```

**Option 2: ARIA queries** (accessibility-first, preferred):

```tsx
// Component  
<button aria-label="Submit form" className="btn-primary">

// Test
expect(screen.getByLabelText('Submit form')).toBeInTheDocument();
```

**Option 3: Role + accessible name**:

```tsx
// Test
expect(screen.getByRole('button', { name: /submit/i })).toBeInTheDocument();
```

**Recommendation**: Prefer ARIA queries (aligns with accessibility testing). Use data-testid only when ARIA queries are insufficient.

### Maintaining Accessibility Coverage

**Ensure jest-axe tests remain**:

```typescript
import { axe, toHaveNoViolations } from 'jest-axe';

expect.extend(toHaveNoViolations);

it('has no accessibility violations', async () => {
  const { container } = render(<Component />);
  const results = await axe(container);
  expect(results).toHaveNoViolations();
});
```

**No changes needed** - Accessibility tests are agnostic to styling implementation.

### Test Utilities Update

**Check `vitest.setup.ts`** for any CSS Module-specific helpers. None found in current setup—standard Testing Library configuration only.

---

## Decision Summary

| Decision Point | Choice | Rationale |
|----------------|--------|-----------|
| **Tailwind Config** | CSS-based (@theme) | Tailwind v4 default, better performance |
| **Variant Management** | CVA | Type-safe, matches Shadcn/UI patterns |
| **Visual Testing** | Playwright | Built-in screenshot diffing, CI-friendly |
| **Test Queries** | ARIA-first | Accessibility compliance, resilient to styling changes |
| **Color System** | Shadcn/UI CSS variables | Themeable, semantic names |
| **Complex Patterns** | @apply sparingly | Escape hatch for truly complex repeated patterns |
| **Migration Order** | Leaf components first | Reduces dependencies, enables incremental validation |

---

## Next Steps

Proceed to **Phase 1: Design & Contracts**:

1. Create `data-model.md` with component inventory
1. Create `quickstart.md` with developer migration guide
1. Install Playwright and capture baseline screenshots
1. Re-run Constitution Check

**All NEEDS CLARIFICATION items resolved** ✅
