# Quickstart Guide: Tailwind CSS Migration

**Phase**: 1 (Design & Contracts)  
**Date**: 2026-01-21  
**Audience**: Developers implementing the migration

## Overview

This guide provides step-by-step instructions for migrating components from CSS Modules to Tailwind CSS with Shadcn/UI. Follow these patterns for consistent, maintainable results.

---

## Prerequisites

### 1. Verify Tailwind CSS v4 Setup

✅ **Already configured** - Tailwind CSS v4.1.18 is installed with @tailwindcss/vite plugin.

Check `src/index.css` contains:

```css
@import "tailwindcss";
```

### 2. Verify Shadcn/UI Configuration

✅ **Already configured** - `components.json` exists with proper aliases.

Available components in `src/components/ui/`:

- Badge, Button, Card
- Context Menu, Dialog, Drawer
- Form Actions, Form Layout
- Input, Popover, Scroll Area
- Select, Separator, Textarea, Tooltip

### 3. Install Playwright for Screenshot Testing

```bash
cd libris-maleficarum-app
pnpm add -D @playwright/test
npx playwright install --with-deps chromium
```

Create `playwright.config.ts`:

```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './tests/visual',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  use: {
    baseURL: 'http://localhost:4000',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
  ],
  webServer: {
    command: 'pnpm dev',
    url: 'http://localhost:4000',
    reuseExistingServer: !process.env.CI,
  },
});
```

### 4. Capture Baseline Screenshots

**CRITICAL**: Do this BEFORE any migration to establish visual baseline.

Create `tests/visual/components.spec.ts`:

```typescript
import { test, expect } from '@playwright/test';

test.describe('Component Visual Regression', () => {
  test('WorldSidebar - empty state', async ({ page }) => {
    await page.goto('/');
    await page.waitForSelector('[data-testid="world-sidebar"]');
    const sidebar = page.locator('[data-testid="world-sidebar"]');
    await expect(sidebar).toHaveScreenshot('world-sidebar.png');
  });

  test('WorldDetailForm', async ({ page }) => {
    await page.goto('/worlds/new');
    await page.waitForSelector('[data-testid="world-detail-form"]');
    const form = page.locator('[data-testid="world-detail-form"]');
    await expect(form).toHaveScreenshot('world-detail-form.png');
  });

  // Add more component tests
});
```

Generate baselines:

```bash
pnpm playwright test --update-snapshots
```

---

## Migration Pattern

### Step-by-Step Process

#### 1. Add data-testid Attributes

**Before migration**, add `data-testid` to component root for screenshot testing:

```tsx
// Before
export function WorldSidebar() {
  return (
    <aside className={styles.sidebar}>

// After (add data-testid FIRST)
export function WorldSidebar() {
  return (
    <aside data-testid="world-sidebar" className={styles.sidebar}>
```

Commit this change separately before migrating styles.

#### 2. Remove CSS Module Import

```tsx
// Before
import styles from './Component.module.css';

// After
// (remove import)
```

#### 3. Replace className with Tailwind Utilities

**Simple example**:

```tsx
// Before (CSS Modules)
<div className={styles.container}>
  <h1 className={styles.title}>Hello</h1>
  <p className={styles.description}>World</p>
</div>

// After (Tailwind)
<div className="flex flex-col gap-4 p-6 bg-background rounded-lg border border-border">
  <h1 className="text-2xl font-semibold">Hello</h1>
  <p className="text-sm text-muted-foreground">World</p>
</div>
```

**With conditional classes** (use `cn()` utility):

```tsx
import { cn } from "@/lib/utils";

// Before
<button className={`${styles.button} ${isActive ? styles.active : ''}`}>

// After
<button className={cn(
  "px-4 py-2 rounded-md font-medium transition-colors",
  "hover:bg-accent hover:text-accent-foreground",
  isActive && "bg-accent text-accent-foreground"
)}>
```

#### 4. Use Shadcn/UI Components

Replace custom styled elements with Shadcn/UI primitives:

```tsx
// Before (custom button)
<button className={styles.primaryButton} onClick={handleClick}>
  Save
</button>

// After (Shadcn/UI Button)
import { Button } from "@/components/ui/button";

<Button onClick={handleClick}>
  Save
</Button>

// With variants
<Button variant="destructive" size="sm">Delete</Button>
<Button variant="outline">Cancel</Button>
```

#### 5. Update Component Tests

Remove CSS Module assertions, use ARIA queries:

```tsx
// Before
import styles from './Component.module.css';

it('renders button', () => {
  render(<Component />);
  expect(screen.getByRole('button')).toHaveClass(styles.button);
});

// After
it('renders button', () => {
  render(<Component />);
  expect(screen.getByRole('button')).toBeInTheDocument();
  // Focus on behavior, not styles
});
```

#### 6. Run Tests

```bash
# Unit/component tests
pnpm test

# Accessibility tests (should still pass)
pnpm test -- --grep "accessibility"

# Visual regression tests
pnpm playwright test
```

If screenshot tests fail, review diffs in `playwright-report/index.html`.

#### 7. Delete CSS Module File

Only after all tests pass:

```bash
git rm src/components/Component/Component.module.css
```

---

## Common Patterns

### Layout Patterns

#### Flex Container (Vertical)

```tsx
<div className="flex flex-col gap-4">
  <div>Item 1</div>
  <div>Item 2</div>
</div>
```

#### Flex Container (Horizontal, Space Between)

```tsx
<div className="flex items-center justify-between">
  <span>Left</span>
  <button>Right</button>
</div>
```

#### Grid Layout (2 columns, responsive)

```tsx
<div className="grid grid-cols-1 md:grid-cols-2 gap-4">
  <div>Column 1</div>
  <div>Column 2</div>
</div>
```

#### Centered Content

```tsx
<div className="flex items-center justify-center min-h-screen">
  <div>Centered</div>
</div>
```

### Form Patterns

#### Form Field Group

```tsx
<div className="space-y-2">
  <label htmlFor="email" className="text-sm font-medium">
    Email
  </label>
  <Input 
    id="email" 
    type="email"
    placeholder="you@example.com"
    aria-invalid={errors.email ? 'true' : 'false'}
  />
  {errors.email && (
    <p className="text-sm text-destructive">{errors.email}</p>
  )}
</div>
```

#### Form Actions (Cancel/Submit)

```tsx
<div className="flex justify-end gap-2 pt-4 border-t border-border">
  <Button type="button" variant="outline" onClick={onCancel}>
    Cancel
  </Button>
  <Button type="submit">
    Save
  </Button>
</div>
```

### Interactive State Patterns

#### Hover & Focus

```tsx
<button className={cn(
  "px-4 py-2 rounded-md transition-colors",
  "hover:bg-accent hover:text-accent-foreground",
  "focus:outline-none focus:ring-2 focus:ring-primary"
)}>
  Click me
</button>
```

#### Selected/Active State

```tsx
<div className={cn(
  "p-2 rounded-md cursor-pointer",
  "hover:bg-accent",
  isSelected && "bg-accent text-accent-foreground"
)}>
  {label}
</div>
```

#### Disabled State

```tsx
<Button disabled className="disabled:opacity-50 disabled:cursor-not-allowed">
  Disabled
</Button>
```

### Modal/Dialog Pattern

```tsx
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from "@/components/ui/dialog";

<Dialog open={isOpen} onOpenChange={setIsOpen}>
  <DialogContent className="sm:max-w-md">
    <DialogHeader>
      <DialogTitle>Confirm Action</DialogTitle>
    </DialogHeader>
    
    <p className="text-sm text-muted-foreground">
      Are you sure?
    </p>
    
    <DialogFooter>
      <Button variant="outline" onClick={onCancel}>Cancel</Button>
      <Button variant="destructive" onClick={onConfirm}>Delete</Button>
    </DialogFooter>
  </DialogContent>
</Dialog>
```

---

## Using CVA for Component Variants

When creating reusable components with variants, use Class Variance Authority:

```tsx
import { cva, type VariantProps } from "class-variance-authority";
import { cn } from "@/lib/utils";

const cardVariants = cva(
  // Base classes (always applied)
  "rounded-lg border p-4",
  {
    variants: {
      variant: {
        default: "bg-background border-border",
        elevated: "bg-background border-border shadow-md",
        filled: "bg-muted border-transparent",
      },
      size: {
        sm: "p-3 text-sm",
        md: "p-4",
        lg: "p-6 text-lg",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "md",
    },
  }
);

interface CardProps extends React.HTMLAttributes<HTMLDivElement>,
  VariantProps<typeof cardVariants> {
  // Custom props
}

export function Card({ variant, size, className, children, ...props }: CardProps) {
  return (
    <div 
      className={cn(cardVariants({ variant, size }), className)} 
      {...props}
    >
      {children}
    </div>
  );
}

// Usage
<Card variant="elevated" size="lg">Content</Card>
```

**Benefits**:

- Type-safe variant props (autocomplete in IDE)
- Consistent styling across app
- Easy to extend with new variants

---

## Handling Complex Scenarios

### Dynamic Styles (Indentation, Calculated Values)

When styles must be calculated at runtime, use inline styles:

```tsx
// Tree node with dynamic indentation based on level
<div 
  className="flex items-center gap-2 py-1.5 rounded-md hover:bg-accent"
  style={{ paddingLeft: `${level * 1.5}rem` }}
>
  {content}
</div>
```

**Rationale**: Tailwind doesn't support arbitrary runtime values. Inline styles are acceptable for truly dynamic values.

### Complex Animations

For animations beyond Tailwind's built-in utilities, define in CSS:

```css
/* src/index.css */
@layer components {
  @keyframes slide-in {
    from { transform: translateX(-100%); }
    to { transform: translateX(0); }
  }
  
  .animate-slide-in {
    animation: slide-in 0.3s ease-out;
  }
}
```

Then use the class:

```tsx
<div className="animate-slide-in">
  Sliding content
</div>
```

### Repeated Complex Patterns

If a complex pattern is repeated across many components, use `@apply`:

```css
/* src/index.css */
@layer components {
  .card-complex {
    @apply rounded-lg border border-border bg-background p-6 shadow-sm;
    @apply hover:shadow-md transition-shadow duration-200;
  }
}
```

**Warning**: Use `@apply` sparingly. Prefer composition with CVA variants for maintainability.

---

## Troubleshooting

### Issue: Classes Not Applied

**Symptom**: Tailwind classes don't appear in browser DevTools.

**Cause**: Tailwind purging removed "unused" classes.

**Solution**: Ensure file is included in Tailwind content config (already configured for `src/**/*.{ts,tsx}`).

### Issue: Conflicting Classes

**Symptom**: Some styles override others unexpectedly.

**Cause**: Class order matters in Tailwind (last class wins).

**Solution**: Use `cn()` utility with proper ordering:

```tsx
// Wrong (size classes conflict)
className="p-4 p-6"  // p-6 wins

// Right (use cn with conditional)
className={cn("p-4", isLarge && "p-6")}
```

### Issue: Screenshot Tests Fail

**Symptom**: Playwright reports visual differences.

**Solutions**:

1. Review diff images in `playwright-report/`
1. If changes are intentional: `pnpm playwright test --update-snapshots`
1. If changes are bugs: fix Tailwind classes to match original appearance

### Issue: Responsive Breakpoints Not Working

**Symptom**: Mobile/desktop styles not applying correctly.

**Cause**: Incorrect breakpoint syntax or viewport size.

**Solution**: Remember mobile-first approach:

```tsx
// Base = mobile, then override for larger screens
className="text-sm md:text-base lg:text-lg"

// Not the other way around
```

---

## Checklist for Each Component

- [ ] Add `data-testid` to component root (for screenshot tests)
- [ ] Remove CSS Module import
- [ ] Replace `className={styles.x}` with Tailwind utilities
- [ ] Use Shadcn/UI components where applicable
- [ ] Apply CVA for component variants (if needed)
- [ ] Update component tests (remove CSS Module assertions)
- [ ] Run unit tests: `pnpm test`
- [ ] Run accessibility tests: `pnpm test -- --grep "accessibility"`
- [ ] Run visual tests: `pnpm playwright test`
- [ ] Review screenshot diffs (if any)
- [ ] Delete CSS Module file: `git rm *.module.css`
- [ ] Commit changes with descriptive message

---

## Example: Complete Migration

**Before** (`EmptyState.tsx`):

```tsx
import styles from './EmptyState.module.css';

export function EmptyState() {
  return (
    <div className={styles.container}>
      <div className={styles.icon}>📭</div>
      <p className={styles.message}>No worlds found</p>
      <button className={styles.createButton} onClick={onCreate}>
        Create World
      </button>
    </div>
  );
}
```

**After** (`EmptyState.tsx`):

```tsx
import { Button } from "@/components/ui/button";

export function EmptyState() {
  return (
    <div 
      data-testid="empty-state"
      className="flex flex-col items-center justify-center gap-4 p-8 text-center"
    >
      <div className="text-4xl">📭</div>
      <p className="text-sm text-muted-foreground">No worlds found</p>
      <Button onClick={onCreate}>
        Create World
      </Button>
    </div>
  );
}
```

**Test Update**:

```tsx
// Before
import styles from './EmptyState.module.css';

it('renders empty state', () => {
  render(<EmptyState />);
  expect(screen.getByText('No worlds found')).toHaveClass(styles.message);
});

// After
it('renders empty state', () => {
  render(<EmptyState />);
  expect(screen.getByText('No worlds found')).toBeInTheDocument();
  expect(screen.getByRole('button', { name: /create world/i })).toBeInTheDocument();
});
```

---

## Resources

- **Tailwind CSS Docs**: <https://tailwindcss.com/docs>
- **Shadcn/UI Docs**: <https://ui.shadcn.com/docs>
- **CVA GitHub**: <https://github.com/joe-bell/cva>
- **Playwright Docs**: <https://playwright.dev/docs/test-snapshots>

---

## Next Steps

After completing this guide:

1. Review `data-model.md` for component inventory and migration order
1. Start with P1 components (EmptyState, DeleteConfirmationModal, WorldSelector)
1. Run `/speckit.tasks` command to generate detailed implementation tasks

**Ready to migrate!** 🚀
