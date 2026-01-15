/**
 * WorldDetailForm Component Tests
 *
 * Tests for the main panel form used to create/edit worlds.
 * Tests both create and edit modes, validation, and accessibility.
 * This is a main panel form (not a modal) that works alongside ChatWindow.
 *
 * @see WorldDetailForm.tsx
 */

import { describe, it, expect, beforeAll, afterEach, afterAll } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { setupServer } from 'msw/node';
import { WorldDetailForm } from './WorldDetailForm';
import { api } from '@/services/api';
import worldSidebarReducer from '@/store/worldSidebarSlice';
import { handlers } from '@/__tests__/mocks/handlers';
import type { World } from '@/services/types/world.types';

expect.extend(toHaveNoViolations);

// Setup MSW server
const server = setupServer(...handlers);

beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

// Mock store setup
const createMockStore = (initialState?: Record<string, unknown>) => {
  return configureStore({
    reducer: {
      worldSidebar: worldSidebarReducer,
      [api.reducerPath]: api.reducer,
    },
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware().concat(api.middleware),
    preloadedState: initialState,
  });
};

describe('WorldDetailForm', () => {
  describe('Create Mode - Rendering', () => {
    it('should render form with "Create World" heading in create mode', () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <WorldDetailForm mode="create" />
        </Provider>,
      );

      // Assert
      expect(screen.getByRole('heading', { name: /create world/i })).toBeInTheDocument();
      expect(screen.getByRole('textbox', { name: /world name/i })).toHaveValue('');
      expect(screen.getByRole('textbox', { name: /description/i })).toHaveValue('');
    });

    it('should render save button labeled "Create" in create mode', () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <WorldDetailForm mode="create" />
        </Provider>,
      );

      // Assert
      const saveButton = screen.getByRole('button', { name: /create/i });
      expect(saveButton).toBeInTheDocument();
      expect(saveButton).not.toBeDisabled();
    });

    it('should have cancel button in create mode', () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <WorldDetailForm mode="create" />
        </Provider>,
      );

      // Assert
      expect(screen.getByRole('button', { name: /cancel/i })).toBeInTheDocument();
    });
  });

  describe('Edit Mode - Rendering', () => {
    it('should render form with "Edit World" heading in edit mode', () => {
      // Arrange
      const mockWorld: World = {
        id: 'world-1',
        name: 'Test World',
        description: 'Test description',
        createdDate: new Date().toISOString(),
      };

      const store = createMockStore({
        worldSidebar: {
          selectedWorldId: 'world-1',
          selectedEntityId: null,
          expandedNodeIds: [],
          isWorldFormOpen: true,
          editingWorldId: 'world-1',
          isEntityFormOpen: false,
          editingEntityId: null,
          newEntityParentId: null,
        },
      });

      // Act
      render(
        <Provider store={store}>
          <WorldDetailForm mode="edit" world={mockWorld} />
        </Provider>,
      );

      // Assert
      expect(screen.getByRole('heading', { name: /edit world/i })).toBeInTheDocument();
    });

    it('should pre-populate form fields with world data in edit mode', () => {
      // Arrange
      const mockWorld: World = {
        id: 'world-1',
        name: 'My Epic World',
        description: 'A tale of adventure and mystery',
        createdDate: new Date().toISOString(),
      };

      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <WorldDetailForm mode="edit" world={mockWorld} />
        </Provider>,
      );

      // Assert
      expect(screen.getByRole('textbox', { name: /world name/i })).toHaveValue(
        'My Epic World',
      );
      expect(screen.getByRole('textbox', { name: /description/i })).toHaveValue(
        'A tale of adventure and mystery',
      );
    });

    it('should render save button labeled "Save" in edit mode', () => {
      // Arrange
      const mockWorld: World = {
        id: 'world-1',
        name: 'Test World',
        description: 'Test',
        createdDate: new Date().toISOString(),
      };

      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <WorldDetailForm mode="edit" world={mockWorld} />
        </Provider>,
      );

      // Assert
      const saveButton = screen.getByRole('button', { name: /save/i });
      expect(saveButton).toBeInTheDocument();
      expect(saveButton).not.toBeDisabled();
    });
  });

  describe('Form Validation', () => {
    it('should show error message when name field is empty', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();

      render(
        <Provider store={store}>
          <WorldDetailForm mode="create" />
        </Provider>,
      );

      // Act
      const submitButton = screen.getByRole('button', { name: /create/i });
      await user.click(submitButton);

      // Assert
      await waitFor(() => {
        expect(screen.getByText(/world name is required/i)).toBeInTheDocument();
      });
    });

    it('should enforce max 100 character limit on world name', async () => {
      // Arrange
      const user = userEvent.setup();
      const store = createMockStore();
      const longName = 'A'.repeat(101);

      render(
        <Provider store={store}>
          <WorldDetailForm mode="create" />
        </Provider>,
      );

      // Act
      const nameInput = screen.getByRole('textbox', { name: /world name/i });
      await user.clear(nameInput);
      await user.type(nameInput, longName);

      // Assert
      expect(nameInput).toHaveValue(longName.substring(0, 100));
    });

    it('should enforce max 500 character limit on description', async () => {
      // Arrange
      const store = createMockStore();

      render(
        <Provider store={store}>
          <WorldDetailForm mode="create" />
        </Provider>,
      );

      // Act - Just verify maxlength attribute is set
      const descriptionInput = screen.getByRole('textbox', { name: /description/i });

      // Assert
      expect(descriptionInput).toHaveAttribute('maxlength', '500');
    });

    it('should not disable submit button when name is empty (validation on submit)', async () => {
      // Arrange
      const store = createMockStore();

      render(
        <Provider store={store}>
          <WorldDetailForm mode="create" />
        </Provider>,
      );

      // Assert - Button should be enabled (validation happens on submit, not on input change)
      const submitButton = screen.getByRole('button', { name: /create/i });
      expect(submitButton).not.toBeDisabled();
    });
  });

  describe('Accessibility', () => {
    it('should have proper ARIA labels for form fields', () => {
      // Arrange
      const store = createMockStore();

      // Act
      render(
        <Provider store={store}>
          <WorldDetailForm mode="create" />
        </Provider>,
      );

      // Assert
      expect(screen.getByRole('textbox', { name: /world name/i })).toBeInTheDocument();
      expect(screen.getByRole('textbox', { name: /description/i })).toBeInTheDocument();
    });

    it('should have no accessibility violations in create mode', async () => {
      // Arrange
      const store = createMockStore();

      // Act
      const { container } = render(
        <Provider store={store}>
          <WorldDetailForm mode="create" />
        </Provider>,
      );

      // Assert
      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should have no accessibility violations in edit mode', async () => {
      // Arrange
      const mockWorld: World = {
        id: 'world-1',
        name: 'Test World',
        description: 'Test description',
        createdDate: new Date().toISOString(),
      };

      const store = createMockStore();

      // Act
      const { container } = render(
        <Provider store={store}>
          <WorldDetailForm mode="edit" world={mockWorld} />
        </Provider>,
      );

      // Assert
      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });
  });
});
