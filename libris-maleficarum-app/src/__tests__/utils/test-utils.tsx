import React, { type PropsWithChildren } from 'react';
import { render } from '@testing-library/react';
import type { RenderOptions } from '@testing-library/react';
import { configureStore } from '@reduxjs/toolkit';
import type { Store } from '@reduxjs/toolkit';
import { Provider } from 'react-redux';
import worldSidebarReducer from '../../store/worldSidebarSlice';
import type { RootState } from '../../store/store';
import { api } from '../../services/api';

// Mock reducer for sidePanel to match RootState
const mockSidePanelReducer = (state = { isExpanded: true }) => state;

// Allow passing a partial state for preloading
interface ExtendedRenderOptions extends Omit<RenderOptions, 'queries'> {
  preloadedState?: Partial<RootState>;
  store?: Store;
}

export function renderWithProviders(
  ui: React.ReactElement,
  {
    preloadedState = {},
    // Automatically create a store instance if no store was passed in
     
    store = configureStore({
      reducer: {
        sidePanel: mockSidePanelReducer,
        worldSidebar: worldSidebarReducer,
        [api.reducerPath]: api.reducer,
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
      } as any,
       
      middleware: (getDefaultMiddleware) =>
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        getDefaultMiddleware().concat(api.middleware) as any,
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      preloadedState: preloadedState as any,
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
    }) as any,
    ...renderOptions
  }: ExtendedRenderOptions = {}
) {
  function Wrapper({ children }: PropsWithChildren) {
    return <Provider store={store}>{children}</Provider>;
  }

  // Return an object with the store and all of RTL's query functions
  return { store, ...render(ui, { wrapper: Wrapper, ...renderOptions }) };
}
