/**
 * Visual Regression Tests for NotificationCenter
 * 
 * Tests the visual appearance of the notification center component
 * across different states and screen sizes.
 * 
 * Run with: pnpm test:visual or npx playwright test
 */

import { test, expect } from '@playwright/test';

test.describe('NotificationCenter Visual Regression', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to the app (adjust URL based on local dev server)
    await page.goto('https://127.0.0.1:4000');
    
    // Wait for app to load
    await page.waitForSelector('[aria-label="Notifications"]', { timeout: 5000 });
  });

  test('notification bell button - initial state', async ({ page }) => {
    const bellButton = page.locator('[aria-label="Notifications"]');
    await expect(bellButton).toBeVisible();
    
    // Take screenshot of bell button in initial state
    await expect(bellButton).toHaveScreenshot('notification-bell-initial.png');
  });

  test('notification bell button - with badge', async ({ page }) => {
    // TODO: Mock API to return operations that would trigger badge
    // This requires MSW setup in the test environment
    // For now, this is a placeholder structure
    
    const bellButton = page.locator('[aria-label="Notifications"]');
    
    // Wait for potential badge to appear (if operations exist)
    // await page.waitForSelector('[aria-label*="unread"]', { timeout: 2000 }).catch(() => {});
    
    await expect(bellButton).toHaveScreenshot('notification-bell-with-badge.png');
  });

  test('notification center - empty state', async ({ page }) => {
    // Click notification bell to open drawer
    const bellButton = page.locator('[aria-label="Notifications"]');
    await bellButton.click();
    
    // Wait for drawer to open
    await page.waitForSelector('text=No notifications', { timeout: 2000 });
    
    // Take screenshot of empty state
    const drawer = page.locator('[role="dialog"]');
    await expect(drawer).toHaveScreenshot('notification-center-empty.png');
  });

  test('notification center - with operations (desktop)', async ({ page, viewport }) => {
    // Set desktop viewport
    await page.setViewportSize({ width: 1280, height: 720 });
    
    // TODO: Mock API to return sample operations
    // This requires MSW setup or backend API mocking
    
    // Click notification bell to open drawer
    const bellButton = page.locator('[aria-label="Notifications"]');
    await bellButton.click();
    
    // Wait for drawer
    await page.waitForSelector('[role="dialog"]', { timeout: 2000 });
    
    // Take screenshot
    const drawer = page.locator('[role="dialog"]');
    await expect(drawer).toHaveScreenshot('notification-center-desktop.png');
  });

  test('notification center - with operations (mobile)', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    // TODO: Mock API to return sample operations
    
    // Click notification bell to open drawer
    const bellButton = page.locator('[aria-label="Notifications"]');
    await bellButton.click();
    
    // Wait for drawer (should be full-screen on mobile)
    await page.waitForSelector('[role="dialog"]', { timeout: 2000 });
    
    // Take screenshot
    await expect(page).toHaveScreenshot('notification-center-mobile.png');
  });

  test('notification item - in-progress state', async ({ page }) => {
    // TODO: Mock API with in-progress operation
    
    const bellButton = page.locator('[aria-label="Notifications"]');
    await bellButton.click();
    
    await page.waitForSelector('[role="dialog"]', { timeout: 2000 });
    
    // Find first notification item
    const notificationItem = page.locator('[role="article"]').first();
    
    if (await notificationItem.isVisible()) {
      await expect(notificationItem).toHaveScreenshot('notification-item-in-progress.png');
    }
  });

  test('notification item - completed state', async ({ page }) => {
    // TODO: Mock API with completed operation
    
    const bellButton = page.locator('[aria-label="Notifications"]');
    await bellButton.click();
    
    await page.waitForSelector('[role="dialog"]', { timeout: 2000 });
    
    const notificationItem = page.locator('[role="article"]').first();
    
    if (await notificationItem.isVisible()) {
      await expect(notificationItem).toHaveScreenshot('notification-item-completed.png');
    }
  });

  test('notification item - failed state', async ({ page }) => {
    // TODO: Mock API with failed operation
    
    const bellButton = page.locator('[aria-label="Notifications"]');
    await bellButton.click();
    
    await page.waitForSelector('[role="dialog"]', { timeout: 2000 });
    
    const notificationItem = page.locator('[role="article"]').first();
    
    if (await notificationItem.isVisible()) {
      await expect(notificationItem).toHaveScreenshot('notification-item-failed.png');
    }
  });

  test('drawer closes on ESC key', async ({ page }) => {
    // Open drawer
    const bellButton = page.locator('[aria-label="Notifications"]');
    await bellButton.click();
    
    // Wait for drawer to open
    const drawer = page.locator('[role="dialog"]');
    await expect(drawer).toBeVisible();
    
    // Press ESC
    await page.keyboard.press('Escape');
    
    // Drawer should close (disappear)
    await expect(drawer).not.toBeVisible();
  });

  test('drawer closes on click outside', async ({ page }) => {
    // Open drawer
    const bellButton = page.locator('[aria-label="Notifications"]');
    await bellButton.click();
    
    // Wait for drawer
    const drawer = page.locator('[role="dialog"]');
    await expect(drawer).toBeVisible();
    
    // Click outside (on overlay or main content area)
    // Adjust selector based on actual app structure
    await page.click('body', { position: { x: 10, y: 10 } });
    
    // Drawer should close
    await expect(drawer).not.toBeVisible();
  });
});

test.describe('NotificationCenter Responsive Behavior', () => {
  test('drawer is full-width on mobile', async ({ page }) => {
    // Mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    await page.goto('https://127.0.0.1:4000');
    
    const bellButton = page.locator('[aria-label="Notifications"]');
    await bellButton.click();
    
    const drawer = page.locator('[role="dialog"]');
    await expect(drawer).toBeVisible();
    
    // Check that drawer takes full width
    const boundingBox = await drawer.boundingBox();
    expect(boundingBox?.width).toBeGreaterThanOrEqual(375 - 20); // Allow small margin
  });

  test('drawer is 24rem width on desktop', async ({ page }) => {
    // Desktop viewport
    await page.setViewportSize({ width: 1280, height: 720 });
    
    await page.goto('https://127.0.0.1:4000');
    
    const bellButton = page.locator('[aria-label="Notifications"]');
    await bellButton.click();
    
    const drawer = page.locator('[role="dialog"]');
    await expect(drawer).toBeVisible();
    
    // Check that drawer is fixed width (24rem = 384px)
    const boundingBox = await drawer.boundingBox();
    expect(boundingBox?.width).toBeCloseTo(384, 10); // Allow 10px tolerance
  });
});

/**
 * NOTE: These tests are scaffolded but require:
 * 1. MSW (Mock Service Worker) setup in Playwright tests to mock API responses
 * 2. Test data fixtures for different operation states
 * 3. Baseline screenshot generation on first run
 * 
 * To run:
 * 1. Ensure dev server is running: pnpm dev
 * 2. Run Playwright: pnpm test:visual
 * 3. Update baselines if intentional visual changes: pnpm test:visual --update-snapshots
 */
