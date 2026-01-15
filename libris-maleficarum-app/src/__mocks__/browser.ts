/**
 * Mock Service Worker (MSW) Browser Setup
 *
 * Initializes MSW for development/demo mode in the browser.
 * This allows testing the frontend without a running backend service.
 *
 * Only loaded in development mode via conditional import in main.tsx
 */

import { setupWorker } from 'msw/browser';
import { handlers } from '@/__tests__/mocks/handlers';

/**
 * Setup MSW to intercept network requests in the browser
 */
export const worker = setupWorker(...handlers);
