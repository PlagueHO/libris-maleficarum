import { test, expect } from '@playwright/test';

/**
 * Visual Regression Tests for Tailwind CSS Migration
 * 
 * These tests capture screenshots of components before and after migration
 * to ensure zero visual regression during the CSS Module → Tailwind transition.
 * 
 * CRITICAL: Run `pnpm playwright test --update-snapshots` BEFORE starting migration
 * to capture baseline screenshots with CSS Modules.
 */

test.describe('Component Visual Regression - Desktop', () => {
  test('Application root layout', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    // Wait for any animations to complete
    await page.waitForTimeout(500);
    
    await expect(page).toHaveScreenshot('app-root-desktop.png', {
      fullPage: true,
      animations: 'disabled',
    });
  });

  test('WorldSidebar - empty state', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    const sidebar = page.locator('[data-testid="world-sidebar"]').first();
    if (await sidebar.isVisible()) {
      await expect(sidebar).toHaveScreenshot('world-sidebar-empty-desktop.png');
    } else {
      console.log('WorldSidebar not found - may need data-testid attribute');
    }
  });

  test('WorldDetailForm - create mode', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    const form = page.locator('[data-testid="world-detail-form"]').first();
    if (await form.isVisible()) {
      await expect(form).toHaveScreenshot('world-detail-form-desktop.png');
    } else {
      console.log('WorldDetailForm not found - may need data-testid attribute');
    }
  });

  test('TopToolbar', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    const toolbar = page.locator('[data-testid="top-toolbar"]').first();
    if (await toolbar.isVisible()) {
      await expect(toolbar).toHaveScreenshot('top-toolbar-desktop.png');
    } else {
      console.log('TopToolbar not found - may need data-testid attribute');
    }
  });

  test('ChatPanel', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    const chatPanel = page.locator('[data-testid="chat-panel"]').first();
    if (await chatPanel.isVisible()) {
      await expect(chatPanel).toHaveScreenshot('chat-panel-desktop.png');
    } else {
      console.log('ChatPanel not found - may need data-testid attribute');
    }
  });
});

test.describe('Component Visual Regression - Mobile', () => {
  test.use({ viewport: { width: 375, height: 667 } });

  test('Application root layout - mobile', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    await expect(page).toHaveScreenshot('app-root-mobile.png', {
      fullPage: true,
      animations: 'disabled',
    });
  });

  test('WorldSidebar - mobile', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    
    const sidebar = page.locator('[data-testid="world-sidebar"]').first();
    if (await sidebar.isVisible()) {
      await expect(sidebar).toHaveScreenshot('world-sidebar-mobile.png');
    }
  });
});

test.describe('Component Visual Regression - Tablet', () => {
  test.use({ viewport: { width: 768, height: 1024 } });

  test('Application root layout - tablet', async ({ page }) => {
    await page.goto('/');
    await page.waitForLoadState('networkidle');
    await page.waitForTimeout(500);
    
    await expect(page).toHaveScreenshot('app-root-tablet.png', {
      fullPage: true,
      animations: 'disabled',
    });
  });
});
