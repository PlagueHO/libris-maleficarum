/**
 * Custom render utilities for testing with all necessary providers.
 * 
 * This module provides a custom render function that wraps components with:
 * - Redux Provider (with configured store)
 * - WorldProvider (for world context)
 * 
 * @example
 * ```tsx
 * import { renderWithProviders, screen } from '@/__tests__/test-utils';
 * 
 * test('component renders', () => {
 *   renderWithProviders(<MyComponent />, {
 *     worldId: 'test-world-123',
 *     worldName: 'Test World'
 *   });
 *   expect(screen.getByText('Hello')).toBeInTheDocument();
 * });
 * ```
 */

import { type ReactElement, type ReactNode } from 'react';
import { render, type RenderOptions } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { WorldProvider } from '@/contexts';
import { api } from '@/services/api';
import notificationsReducer from '@/store/notificationsSlice';
import worldSidebarReducer from '@/store/worldSidebarSlice';

// Define RootState based on our reducers
export interface RootState {
  [api.reducerPath]: ReturnType<typeof api.reducer>;
  notifications: ReturnType<typeof notificationsReducer>;
  worldSidebar: ReturnType<typeof worldSidebarReducer>;
}

export interface CustomRenderOptions extends Omit<RenderOptions, 'wrapper'> {
  /**
   * World ID to provide in WorldContext
   * @default 'test-world-id'
   */
  worldId?: string;
  
  /**
   * World name to provide in WorldContext
   * @default 'Test World'
   */
  worldName?: string;
  
  /**
   * Preloaded state for Redux store
   */
  preloadedState?: Partial<RootState>;
}

/**
 * Custom render that wraps with all necessary providers.
 * 
 * Provides:
 * - Redux store with API, notifications, worldSidebar, and sidePanel slices
 * - WorldProvider with configurable worldId and worldName
 * 
 * @param ui - Component to render
 * @param options - Render options including worldId, worldName, and preloadedState
 * @returns Render result with store instance
 */
export function renderWithProviders(
  ui: ReactElement,
  {
    worldId = 'test-world-id',
    worldName = 'Test World',
    preloadedState,
    ...renderOptions
  }: CustomRenderOptions = {}
) {
  const store = configureStore({
    reducer: {
      // @ts-expect-error - Reducer type inference issues with RTK Query
      [api.reducerPath]: api.reducer,
      // @ts-expect-error - Reducer type inference issues
      notifications: notificationsReducer,
      // @ts-expect-error - Reducer type inference issues
      worldSidebar: worldSidebarReducer,
    },
    preloadedState,
    // @ts-expect-error - Middleware type inference issue with RTK Query
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware().concat(api.middleware),
  });

  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <Provider store={store}>
        <WorldProvider initialWorldId={worldId} initialWorldName={worldName}>
          {children}
        </WorldProvider>
      </Provider>
    );
  }

  return { store, ...render(ui, { wrapper: Wrapper, ...renderOptions }) };
}

// Re-export everything from testing-library for convenience
// eslint-disable-next-line react-refresh/only-export-components
export * from '@testing-library/react';
export { renderWithProviders as render };
