import { defineConfig, devices } from '@playwright/test';

/**
 * Visual regression testing configuration for Tailwind CSS migration
 * See https://playwright.dev/docs/test-configuration
 */
export default defineConfig({
  testDir: './tests/visual',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',
  
  use: {
    baseURL: 'https://127.0.0.1:4000',
    screenshot: 'only-on-failure',
    trace: 'on-first-retry',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: {
    command: 'pnpm dev',
    url: 'https://127.0.0.1:4000',
    reuseExistingServer: !process.env.CI,
    ignoreHTTPSErrors: true,
  },
});
