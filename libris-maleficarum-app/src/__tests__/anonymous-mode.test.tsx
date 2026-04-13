/// <reference types="vitest/globals" />
/// <reference types="@testing-library/jest-dom" />

import { describe, it, expect, beforeAll, afterEach, afterAll, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { setupServer } from 'msw/node';
import App from '@/App';
import { api } from '@/services/api';
import worldSidebarReducer from '@/store/worldSidebarSlice';
import notificationsReducer from '@/store/notificationsSlice';
import { handlers } from '@/__tests__/mocks/handlers';

expect.extend(toHaveNoViolations);

// Mock useAccessCode to bypass the loading state so the app renders immediately
vi.mock('@/hooks/useAccessCode', () => ({
  useAccessCode: () => ({
    accessCodeRequired: false,
    isVerified: true,
    isLoading: false,
    error: null,
    submitCode: vi.fn(),
  }),
}));

const server = setupServer(...handlers);

beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

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

function renderApp() {
  const store = createMockStore();
  return render(
    <Provider store={store}>
      <App />
    </Provider>
  );
}

describe('Anonymous Single-User Mode', () => {
  it('shows user menu trigger in header', () => {
    renderApp();
    expect(screen.getByRole('button', { name: /user menu/i })).toBeInTheDocument();
  });

  it('shows "Anonymous" when user menu is opened', async () => {
    const user = userEvent.setup();
    renderApp();

    await user.click(screen.getByRole('button', { name: /user menu/i }));

    expect(screen.getByText(/anonymous/i)).toBeInTheDocument();
  });

  it('shows settings menu item in user menu', async () => {
    const user = userEvent.setup();
    renderApp();

    await user.click(screen.getByRole('button', { name: /user menu/i }));

    expect(screen.getByRole('menuitem', { name: /settings/i })).toBeInTheDocument();
  });

  it('opens settings panel when Settings menu item is clicked', async () => {
    const user = userEvent.setup();
    renderApp();

    await user.click(screen.getByRole('button', { name: /user menu/i }));
    await user.click(screen.getByRole('menuitem', { name: /settings/i }));

    expect(screen.getByRole('heading', { name: /settings/i })).toBeInTheDocument();
  });

  it('has no accessibility violations in anonymous mode', async () => {
    const { container } = renderApp();
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
