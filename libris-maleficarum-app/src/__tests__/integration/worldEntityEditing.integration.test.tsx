/**
 * World Entity Editing Integration Tests
 *
 * End-to-end integration tests for the complete edit workflow.
 *
 * @see specs/008-edit-world-entity/spec.md
 */

import { describe, it, expect, vi, beforeAll, afterEach, afterAll } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import { axe, toHaveNoViolations } from 'jest-axe';
import { EntityTreeNode } from '@/components/WorldSidebar/EntityTreeNode';
import { MainPanel } from '@/components/MainPanel/MainPanel';
import { api } from '@/services/api';
import worldSidebarReducer, { type WorldSidebarState } from '@/store/worldSidebarSlice';
import type { WorldEntity } from '@/services/types/worldEntity.types';
import { WorldEntityType } from '@/services/types/worldEntity.types';

expect.extend(toHaveNoViolations);

const BASE_URL = 'http://localhost:5000';

// Mock entity data
const mockEntity: WorldEntity = {
  id: 'entity-1',
  worldId: 'world-1',
  parentId: null,
  entityType: WorldEntityType.Continent,
  name: 'Faerûn',
  description: 'The main continent',
  tags: ['primary'],
  properties: undefined,
  path: ['/Faerûn'],
  depth: 0,
  hasChildren: true,
  ownerId: 'user-1',
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  isDeleted: false,
  schemaVersion: 1,
};

// Mutable state for MSW handlers
let currentMockEntity = { ...mockEntity };

// Default handlers for common requests
const defaultHandlers = [
  http.options('*', () => {
    return new HttpResponse(null, {
      status: 204,
      headers: {
        'Access-Control-Allow-Origin': '*',
        'Access-Control-Allow-Methods': 'GET, POST, PATCH, DELETE, OPTIONS',
        'Access-Control-Allow-Headers': 'Content-Type',
      },
    });
  }),
  // Default GET handler - Exact URL matching
  http.get(`${BASE_URL}/api/v1/worlds/:worldId/entities/:entityId`, ({ params }) => {
    const { entityId } = params;
    if (entityId === 'entity-1') return HttpResponse.json({ data: currentMockEntity });
    if (entityId === 'parent-1') return HttpResponse.json({ data: { ...mockEntity, id: 'parent-1' } });
    return new HttpResponse(null, { status: 404 });
  }),
  // Handler for creating entities
  http.post(`${BASE_URL}/api/v1/worlds/:worldId/entities`, () => {
      // Create returns WorldEntityResponse { data: WorldEntity }
      return HttpResponse.json({ data: mockEntity });
  }),
  // Handler for updating entities
  http.put(`${BASE_URL}/api/v1/worlds/:worldId/entities/:entityId`, async ({ request }) => {
    const body = (await request.json()) as Partial<WorldEntity>;
    // Update the mock state
    currentMockEntity = { ...currentMockEntity, ...body, updatedAt: new Date().toISOString() };
    return HttpResponse.json({ data: currentMockEntity });
  }),
];

const server = setupServer(...defaultHandlers);

// Helper to create store with partial state overrides
const createMockStore = (preloadedSidebarState?: Partial<WorldSidebarState>) => {
  return configureStore({
    reducer: {
      worldSidebar: worldSidebarReducer,
      [api.reducerPath]: api.reducer,
    },
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware().concat(api.middleware),
    preloadedState: {
        worldSidebar: {
            selectedWorldId: 'world-1', // Default world
            selectedEntityId: null,
            expandedNodeIds: [],
            mainPanelMode: 'empty' as const,
            isWorldFormOpen: false,
            editingWorldId: null,
            editingEntityId: null,
            newEntityParentId: null,
            hasUnsavedChanges: false,
            deletingEntityId: null,
            deletingEntityName: null,
            showDeleteConfirmation: false,
            movingEntityId: null,
            creatingEntityParentId: null,
            ...preloadedSidebarState // Override with specific test requirements
        }
    },
  });
};

describe('World Entity Editing Integration (T013)', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'warn' }));
  afterEach(() => {
    server.resetHandlers();
    vi.clearAllMocks();
    // Reset mock data
    currentMockEntity = { ...mockEntity };
  });
  afterAll(() => server.close());

  describe('User Story 1: Quick Edit from Hierarchy (T013)', () => {
    it('should allow editing entity by clicking edit icon in hierarchy', async () => {
      const user = userEvent.setup();
      
      const store = createMockStore({
          selectedWorldId: 'world-1',
      });

      const { rerender } = render(
        <Provider store={store}>
          <EntityTreeNode entity={mockEntity} level={0} />
        </Provider>,
      );

      const editButton = screen.getByRole('button', { name: /edit faerûn/i });
      await user.click(editButton);

      const state = store.getState();
      expect(state.worldSidebar.editingEntityId).toBe('entity-1');
      expect(state.worldSidebar.mainPanelMode).toBe('editing_entity');

      rerender(
        <Provider store={store}>
          <MainPanel />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByRole('heading', { name: /edit entry/i })).toBeInTheDocument();
      });
    });

    it('should save changes and refresh hierarchy after edit', async () => {
      const user = userEvent.setup();
      const store = createMockStore({
          selectedWorldId: 'world-1',
          selectedEntityId: 'entity-1',
          editingEntityId: 'entity-1',
          mainPanelMode: 'editing_entity',
      });
      
      render(
        <Provider store={store}>
          <MainPanel />
        </Provider>,
      );

      // Verify form loaded with data (retry mechanism makes this robust)
      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toHaveValue('Faerûn');
      }, { timeout: 2000 });

      const nameInput = screen.getByLabelText(/name/i);
      await user.clear(nameInput);
      await user.type(nameInput, 'Faerûn (Updated)');

      const descriptionInput = screen.getByLabelText(/description/i);
      await user.clear(descriptionInput);
      await user.type(descriptionInput, 'The main continent - now updated!');

      const saveButton = screen.getByRole('button', { name: /save changes/i });
      await user.click(saveButton);

      await waitFor(() => {
        expect(screen.queryByRole('heading', { name: /edit entry/i })).not.toBeInTheDocument();
      });
    });
  });

  describe('User Story 2: Edit from Detail View (T023)', () => {
    it('should allow editing entity by clicking Edit button in detail view', async () => {
      const user = userEvent.setup();
      
      const store = createMockStore({
          selectedWorldId: 'world-1',
          selectedEntityId: 'entity-1',
          mainPanelMode: 'viewing_entity'
      });

      render(
        <Provider store={store}>
          <MainPanel />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      }, { timeout: 2000 });

      const editButton = screen.getByRole('button', { name: /edit/i });
      await user.click(editButton);

      const state = store.getState();
      expect(state.worldSidebar.editingEntityId).toBe('entity-1');
      expect(state.worldSidebar.mainPanelMode).toBe('editing_entity');
      
      await waitFor(() => {
          expect(screen.getByRole('heading', { name: /edit entry/i })).toBeInTheDocument();
      });
    });

    it('should transition back to read-only view after successful edit save', async () => {
      const user = userEvent.setup();
      
      const store = createMockStore({
          selectedWorldId: 'world-1',
          selectedEntityId: 'entity-1',
          editingEntityId: 'entity-1',
          mainPanelMode: 'editing_entity'
      });

      render(
        <Provider store={store}>
          <MainPanel />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      const nameInput = screen.getByLabelText(/name/i);
      await user.clear(nameInput);
      await user.type(nameInput, 'Faerûn (Updated)');

      const saveButton = screen.getByRole('button', { name: /save changes/i });
      await user.click(saveButton);

      // Verify we are back to view mode and data is updated
      await waitFor(() => {
        expect(screen.getByText('Faerûn (Updated)')).toBeInTheDocument();
        // Ensure input is gone (meaning we are not in edit mode)
        expect(screen.queryByLabelText(/name/i)).not.toBeInTheDocument();
      });
    });

    it('should preserve detail view after canceling edit', async () => {
      const user = userEvent.setup();
      
      const store = createMockStore({
          selectedWorldId: 'world-1',
          selectedEntityId: 'entity-1',
          editingEntityId: 'entity-1',
          mainPanelMode: 'editing_entity'
      });

      render(
        <Provider store={store}>
          <MainPanel />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      const cancelButton = screen.getByRole('button', { name: /cancel/i });
      await user.click(cancelButton);

      await waitFor(() => {
        // Should return to viewing the ORIGINAL entity (mockEntity name is Faerûn)
        expect(screen.queryByRole('heading', { name: /edit entry/i })).not.toBeInTheDocument();
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      });
    });
  });

  describe('User Story 3: Validation Error Flow (T034)', () => {
    it('should display validation errors and prevent save with invalid data', async () => {
      const user = userEvent.setup();
      
      const store = createMockStore({
          selectedWorldId: 'world-1',
          newEntityParentId: 'parent-1',
          mainPanelMode: 'creating_entity',
      });

      const createEntitySpy = vi.fn();
      server.use(
        http.post(`${BASE_URL}/api/v1/worlds/:worldId/entities`, async () => {
          createEntitySpy();
          return HttpResponse.json({ data: mockEntity });
        }),
      );

      render(
        <Provider store={store}>
          <MainPanel />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      await waitFor(() => {
        expect(screen.getByText(/every entry must bear a name/i)).toBeInTheDocument();
        const input = screen.getByLabelText(/name/i);
        expect(input).toHaveAttribute('aria-invalid', 'true');
      });

      expect(createEntitySpy).not.toHaveBeenCalled();
    });

    it('should allow save after fixing validation errors', async () => {
      const user = userEvent.setup();
      
      const store = createMockStore({
          selectedWorldId: 'world-1',
          newEntityParentId: 'parent-1',
          mainPanelMode: 'creating_entity',
      });

      // No override needed, default handlers logic should work if URL matches

      render(
        <Provider store={store}>
          <MainPanel />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      // Trigger error
      const saveButton = screen.getByRole('button', { name: /create/i });
      await user.click(saveButton);

      await waitFor(() => {
        expect(screen.getByText(/every entry must bear a name/i)).toBeInTheDocument();
      });

      // Fix name
      const nameInput = screen.getByLabelText(/name/i);
      await user.type(nameInput, 'New Valid Entity');

      // Click select button
      const typeTrigger = screen.getByRole('combobox', { name: /type/i }); 
      await user.click(typeTrigger);

      // Find option in portal
      // Radix UI renders options in a portal at the document root
      // We need to look for role="option" globally or inside the listbox
      const option = await screen.findByRole('option', { name: /Geographic Region/i });
      await user.click(option);

      await user.click(saveButton);

      await waitFor(() => {
        expect(screen.queryByText(/every entry must bear a name/i)).not.toBeInTheDocument();
      });
    });

    it('should enforce maximum length on name input', async () => {
      const user = userEvent.setup();
      let updateCalled = false;

      const mockUpdatedEntity: WorldEntity = {
        ...currentMockEntity,
        name: 'A'.repeat(100),
        updatedAt: new Date().toISOString(),
      };

      const store = createMockStore({
          selectedWorldId: 'world-1',
          selectedEntityId: 'entity-1',
          editingEntityId: 'entity-1',
          mainPanelMode: 'editing_entity' as const,
      });
      
      server.use(
        http.put(`${BASE_URL}/api/v1/worlds/:worldId/entities/:entityId`, async () => {
          updateCalled = true;
          return HttpResponse.json({ data: mockUpdatedEntity });
        }),
      );

      render(
        <Provider store={store}>
          <MainPanel />
        </Provider>,
      );

      await waitFor(() => {
        expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
      });

      const nameInput = screen.getByLabelText(/name/i) as HTMLInputElement;
      await user.clear(nameInput);
      
      const longName = 'A'.repeat(101);
      await user.type(nameInput, longName);

      expect(nameInput.value).toBe('A'.repeat(100));
      expect(nameInput.value.length).toBe(100);
      
      const saveButton = screen.getByRole('button', { name: /save/i });
      await user.click(saveButton);

      await waitFor(() => {
        expect(updateCalled).toBe(true);
      });
    });

    it('should maintain accessibility when validation errors are present', async () => {
        const user = userEvent.setup();
        const store = createMockStore({
            selectedWorldId: 'world-1',
            newEntityParentId: 'parent-1',
            mainPanelMode: 'creating_entity',
        });
  
        const { container } = render(
          <Provider store={store}>
            <MainPanel />
          </Provider>,
        );
  
        await waitFor(() => {
          expect(screen.getByLabelText(/name/i)).toBeInTheDocument();
        });
  
        const saveButton = screen.getByRole('button', { name: /create/i });
        await user.click(saveButton);
  
        await waitFor(() => {
          expect(screen.getByText(/every entry must bear a name/i)).toBeInTheDocument();
        });
  
        expect(await axe(container)).toHaveNoViolations();
    });
  });
});
