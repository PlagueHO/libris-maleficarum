/// <reference types="vitest/globals" />
/// <reference types="@testing-library/jest-dom" />

import { describe, it, expect, beforeAll, afterEach, afterAll } from 'vitest';
import { render, screen } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { setupServer } from 'msw/node';
import App from './App';
import { api } from '@/services/api';
import worldSidebarReducer from '@/store/worldSidebarSlice';
import notificationsReducer from '@/store/notificationsSlice';
import { handlers } from '@/__tests__/mocks/handlers';

// Setup MSW server
const server = setupServer(...handlers);

beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

// Create a mock store for testing
const createMockStore = () => {
  return configureStore({
    reducer: {
      sidePanel: (state = { isExpanded: true }) => state,
      worldSidebar: worldSidebarReducer,
      notifications: notificationsReducer,
      [api.reducerPath]: api.reducer,
    },
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware().concat(api.middleware),
  });
};

describe('App', () => {
  it('renders without crashing', () => {
    const store = createMockStore();
    render(
      <Provider store={store}>
        <App />
      </Provider>
    );
    expect(screen.getByRole('heading', { name: /Libris Maleficarum/i })).toBeInTheDocument();
  });
});
