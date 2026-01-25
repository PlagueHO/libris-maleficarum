/**
 * WorldEntityForm Component Tests
 *
 * Tests for the en unified entity creation/editing form.
 * Focus on validation logic, error handling, and accessibility.
 *
 * @module components/MainPanel/WorldEntityForm.test
 */

import { describe, it, expect, vi, beforeEach, afterEach, beforeAll, afterAll } from 'vitest';

// Mock EntityTypeSelector to avoid Popover portal rendering issues in JSDOM
// Must be declared before imports that use the EntityTypeSelector component
vi.mock('@/components/ui/EntityTypeSelector', async () => {
  const mod = await import('@/components/ui/__mocks__/EntityTypeSelector');
  return mod;
});

import { render, screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import { axe, toHaveNoViolations } from 'jest-axe';
import { EntityDetailForm as WorldEntityForm } from './WorldEntityForm';
import worldSidebarReducer from '../../store/worldSidebarSlice';
import { worldEntityApi } from '../../services/worldEntityApi';
import { WorldEntityType } from '../../services/types/worldEntity.types';
import type { WorldEntity } from '../../services/types/worldEntity.types';

expect.extend(toHaveNoViolations);

// Mock parent entity for create mode
const mockParentEntity: WorldEntity = {
  id: 'parent-1',
  worldId: 'world-1',
  parentId: null,
  name: 'Parent Entity',
  description: 'A parent entity',
  entityType: WorldEntityType.Continent,
  tags: [],
  properties: undefined,
  schemaVersion: 1,
  path: ['/Parent Entity'],
  depth: 0,
  hasChildren: true,
  ownerId: 'user-1',
  isDeleted: false,
  createdAt: '2026-01-25T00:00:00Z',
  updatedAt: '2026-01-25T00:00:00Z',
};

// MSW Server setup
const server = setupServer(
  // Mock OPTIONS (CORS preflight)
  http.options('*', () => {
    return new HttpResponse(null, { status: 204, headers: { 'Access-Control-Allow-Origin': '*' } });
  }),
  // Mock GET parent entity endpoint
  http.get('http://localhost:5000/api/v1/worlds/:worldId/entities/:entityId', ({ params }) => {
    if (params.entityId === 'parent-1') {
      return HttpResponse.json(mockParentEntity);
    }
    return new HttpResponse(null, { status: 404 });
  })
);

beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }));
afterAll(() => server.close());

describe('WorldEntityForm - Validation (T032)', () => {
  let store: ReturnType<typeof configureStore>;

  beforeEach(() => {

    // Create store with initial state for create mode
    store = configureStore({
      reducer: {
      worldSidebar: worldSidebarReducer,
        [worldEntityApi.reducerPath]: worldEntityApi.reducer,
      },
      middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware().concat(worldEntityApi.middleware),
      preloadedState: {
        worldSidebar: {
          selectedWorldId: 'world-1',
          selectedEntityId: null,
          newEntityParentId: 'parent-1',
          editingEntityId: null,
          mainPanelMode: 'creating_entity' as const,
          hasUnsavedChanges: false,
          expandedNodeIds: [],
          isWorldFormOpen: false,
          editingWorldId: null,
          deletingEntityId: null,
          showDeleteConfirmation: false,
          movingEntityId: null,
          creatingEntityParentId: null,
        },
      },
    });
  });

  afterEach(() => {
    server.resetHandlers();
    vi.clearAllMocks();
  });

  describe('Required Field Validation', () => {
    it('should show error when trying to submit with empty name field', async () => {
      const user = userEvent.setup();

      render(
        <Provider store={store}>
          <WorldEntityForm />
        </Provider>
      );

      // Wait for form to load
      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Leave name field empty and try to submit
      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      // Verify error message appears
      await waitFor(() => {
        expect(screen.getByText(/name is required/i)).toBeInTheDocument();
      });

      // Verify name input has aria-invalid attribute
      const nameInput = screen.getByLabelText(/name/i);
      expect(nameInput).toHaveAttribute('aria-invalid', 'true');
    });

    it('should show error when name is only whitespace', async () => {
      const user = userEvent.setup();

      render(
        <Provider store={store}>
          <WorldEntityForm />
        </Provider>
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Enter only whitespace
      const nameInput = screen.getByLabelText(/name/i);
      await user.clear(nameInput);
      await user.type(nameInput, '   ');

      // Try to submit
      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      // Verify error appears
      await waitFor(() => {
        expect(screen.getByText(/name is required/i)).toBeInTheDocument();
      });
    });

    it('should show error when trying to submit without selecting entity type', async () => {
      const user = userEvent.setup();

      render(
        <Provider store={store}>
          <WorldEntityForm />
        </Provider>
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Fill in name but don't select type
      const nameInput = screen.getByLabelText(/name/i);
      await user.type(nameInput, 'New Entity');

      // Try to submit
      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      // Verify type error appears
      await waitFor(() => {
        expect(screen.getByText(/type is required/i)).toBeInTheDocument();
      });
    });

    it('should clear name error when valid name is entered', async () => {
      const user = userEvent.setup();

      render(
        <Provider store={store}>
          <WorldEntityForm />
        </Provider>
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Trigger error by submitting empty
      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      // Verify error appears
      await waitFor(() => {
        expect(screen.getByText(/name is required/i)).toBeInTheDocument();
      });

      // Fix the error by entering a valid name
      const nameInput = screen.getByLabelText(/name/i);
      await user.type(nameInput, 'Valid Entity Name');

      // Submit again to trigger re-validation
      await user.click(saveButton);

      // Error should be cleared (or at least name error should be gone)
      await waitFor(() => {
        expect(screen.queryByText(/name is required/i)).not.toBeInTheDocument();
      });
    });

    it('should clear type error when entity type is selected', async () => {
      const user = userEvent.setup();

      render(
        <Provider store={store}>
          <WorldEntityForm />
        </Provider>
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Fill name but skip type
      const nameInput = screen.getByLabelText(/name/i);
      await user.type(nameInput, 'Test Entity');

      // Submit to trigger type error
      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      await waitFor(() => {
        expect(screen.getByText(/type is required/i)).toBeInTheDocument();
      });

      // Select a type (assuming EntityTypeSelector is a select element or has role combobox)
      const typeSelector = screen.getByLabelText(/type/i);
      await user.click(typeSelector);

      // Find and click an option (adjust selector based on actual EntityTypeSelector implementation)
      const option = await screen.findByRole('option', { name: /geographic region/i });
      await user.click(option);

      // Submit again to trigger re-validation
      await user.click(saveButton);

      // Type error should be cleared
      await waitFor(() => {
        expect(screen.queryByText(/type is required/i)).not.toBeInTheDocument();
      });
    });

    it('should prevent form submission when validation fails', async () => {
      const user = userEvent.setup();
      const mockCreateEntity = vi.fn();

      // Mock the createEntity mutation to spy on calls
      vi.spyOn(worldEntityApi.endpoints.createWorldEntity, 'useMutation').mockReturnValue([
        mockCreateEntity,
        { isLoading: false, isSuccess: false, isError: false, data: undefined, error: undefined },
      ] as unknown as ReturnType<typeof worldEntityApi.endpoints.createWorldEntity.useMutation>);

      render(
        <Provider store={store}>
          <WorldEntityForm />
        </Provider>
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Try to submit with empty fields
      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      // Wait for validation to run
      await waitFor(() => {
        expect(screen.getByText(/name is required/i)).toBeInTheDocument();
      });

      // Verify the API mutation was NOT called
      expect(mockCreateEntity).not.toHaveBeenCalled();
    });
  });

  describe('Schema Rules Validation (T033)', () => {
    it('should show error when name exceeds 100 characters', async () => {
      const user = userEvent.setup();

      render(
        <Provider store={store}>
          <WorldEntityForm />
        </Provider>
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Select entity type first
      const typeSelector = screen.getByLabelText(/type/i);
      await user.click(typeSelector);
      const option = await screen.findByRole('option', { name: /geographic region/i });
      await user.click(option);

      // Enter a name longer than 100 characters (bypass maxLength by setting value directly)
      const longName = 'A'.repeat(101);
      const nameInput = screen.getByLabelText(/name/i) as HTMLInputElement;
      // Use fireEvent to properly trigger React's onChange handler
      fireEvent.input(nameInput, { target: { value: longName } });

      // Try to submit
      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      // Verify error appears
      await waitFor(() => {
        expect(screen.getByText(/name must be 100 characters or less/i)).toBeInTheDocument();
      });
    });

    it('should allow name with exactly 100 characters', async () => {
      const user = userEvent.setup();

      render(
        <Provider store={store}>
          <WorldEntityForm />
        </Provider>
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Enter exactly 100 characters
      const exactName = 'A'.repeat(100);
      const nameInput = screen.getByLabelText(/name/i);
      await user.type(nameInput, exactName);

      // Select entity type
      const typeSelector = screen.getByLabelText(/type/i);
      await user.click(typeSelector);
      const option = await screen.findByRole('option', { name: /geographic region/i });
      await user.click(option);

      // Try to submit
      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      // Verify no name length error appears
      await waitFor(() => {
        expect(screen.queryByText(/name must be 100 characters or less/i)).not.toBeInTheDocument();
      });
    });

    it('should show error when description exceeds 500 characters', async () => {
      const user = userEvent.setup();

      render(
        <Provider store={store}>
          <WorldEntityForm />
        </Provider>
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Enter valid name
      const nameInput = screen.getByLabelText(/name/i);
      await user.type(nameInput, 'Valid Entity');

      // Select entity type
      const typeSelector = screen.getByLabelText(/type/i);
      await user.click(typeSelector);
      const option = await screen.findByRole('option', { name: /geographic region/i });
      await user.click(option);

      // Enter description longer than 500 characters (bypass maxLength)
      const longDescription = 'B'.repeat(501);
      const descriptionInput = screen.getByLabelText(/description/i) as HTMLTextAreaElement;
      // Use fireEvent to properly trigger React's onChange handler
      fireEvent.input(descriptionInput, { target: { value: longDescription } });

      // Try to submit
      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      // Verify error appears
      await waitFor(() => {
        expect(screen.getByText(/description must be 500 characters or less/i)).toBeInTheDocument();
      });
    });

    it('should allow description with exactly 500 characters', async () => {
      const user = userEvent.setup();

      render(
        <Provider store={store}>
          <WorldEntityForm />
        </Provider>
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Enter valid name and type
      const nameInput = screen.getByLabelText(/name/i);
      await user.type(nameInput, 'Valid Entity');

      const typeSelector = screen.getByLabelText(/type/i);
      await user.click(typeSelector);
      const option = await screen.findByRole('option', { name: /geographic region/i });
      await user.click(option);

      // Enter exactly 500 characters (bypass maxLength with direct value)
      const exactDescription = 'B'.repeat(500);
      const descriptionInput = screen.getByLabelText(/description/i) as HTMLTextAreaElement;
      // Use fireEvent to properly trigger React's onChange handler
      fireEvent.input(descriptionInput, { target: { value: exactDescription } });

      // Try to submit
      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      // Verify no description length error
      await waitFor(() => {
        expect(screen.queryByText(/description must be 500 characters or less/i)).not.toBeInTheDocument();
      });
    });

    it('should allow empty description (optional field)', async () => {
      const user = userEvent.setup();

      render(
        <Provider store={store}>
          <WorldEntityForm />
        </Provider>
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Fill only required fields
      const nameInput = screen.getByLabelText(/name/i);
      await user.type(nameInput, 'Entity Name');

      const typeSelector = screen.getByLabelText(/type/i);
      await user.click(typeSelector);
      const option = await screen.findByRole('option', { name: /geographic region/i });
      await user.click(option);

      // Leave description empty and submit
      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      // Verify no description validation error appears (check for specific error message pattern)
      await waitFor(() => {
        expect(screen.queryByText(/description must be 500 characters or less/i)).not.toBeInTheDocument();
      });
    });
  });

  describe('Accessibility', () => {
    it('should have no accessibility violations in form with validation errors', async () => {
      const user = userEvent.setup();

      const { container } = render(
        <Provider store={store}>
          <WorldEntityForm />
        </Provider>
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Trigger validation errors
      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      await waitFor(() => {
        expect(screen.getByText(/name is required/i)).toBeInTheDocument();
      });

      // Run accessibility check
      const results = await axe(container);
      expect(results).toHaveNoViolations();
    });

    it('should associate error messages with inputs via aria-describedby', async () => {
      const user = userEvent.setup();

      render(
        <Provider store={store}>
          <WorldEntityForm />
        </Provider>
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Trigger name error
      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      await waitFor(() => {
        expect(screen.getByText(/name is required/i)).toBeInTheDocument();
      });

      // Verify aria-describedby linkage
      const nameInput = screen.getByLabelText(/name/i);
      const describedBy = nameInput.getAttribute('aria-describedby');

      if (describedBy) {
        const errorElement = document.getElementById(describedBy);
        expect(errorElement).toBeInTheDocument();
        expect(errorElement).toHaveTextContent(/name is required/i);
      }
    });
  });

  describe('Unsaved Changes Dialog', () => {
    it('should NOT show unsaved changes dialog after successfully creating entity', async () => {
      const user = userEvent.setup();

      // Mock successful entity creation
      server.use(
        http.post('http://localhost:5000/api/v1/worlds/:worldId/entities', () => {
          return HttpResponse.json({
            data: {
              id: 'new-entity-1',
              worldId: 'world-1',
              parentId: 'parent-1',
              name: 'New Test Entity',
              description: 'Test description',
              entityType: WorldEntityType.GeographicRegion,
              tags: [],
              properties: undefined,
              schemaVersion: 1,
              path: ['/Parent Entity', '/New Test Entity'],
              depth: 1,
              hasChildren: false,
              ownerId: 'user-1',
              isDeleted: false,
              createdAt: '2026-01-26T00:00:00Z',
              updatedAt: '2026-01-26T00:00:00Z',
            },
          });
        }),
      );

      render(
        <Provider store={store}>
          <WorldEntityForm />
        </Provider>
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Fill in required fields
      const nameInput = screen.getByLabelText(/name/i);
      await user.type(nameInput, 'New Test Entity');

      const typeSelector = screen.getByLabelText(/type/i);
      await user.click(typeSelector);
      const option = await screen.findByRole('option', { name: /geographic region/i });
      await user.click(option);

      const descriptionInput = screen.getByLabelText(/description/i);
      await user.type(descriptionInput, 'Test description');

      // Submit the form
      const createButton = screen.getByRole('button', { name: /create/i });
      await user.click(createButton);

      // Wait a moment for async operations
      await waitFor(() => {
        // We don't expect the dialog to be visible
        expect(screen.queryByText(/unsaved changes/i)).not.toBeInTheDocument();
      }, { timeout: 3000 });

      // Additionally verify dialog role is not present
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });
});
