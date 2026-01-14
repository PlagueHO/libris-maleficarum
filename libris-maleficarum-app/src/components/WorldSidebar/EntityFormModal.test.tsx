import { describe, it, expect, vi, beforeEach, afterAll, beforeAll } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { EntityFormModal } from './EntityFormModal';
import { renderWithProviders } from '../../__tests__/utils/test-utils';
import { server } from '../../__tests__/mocks/server';
import { http, HttpResponse } from 'msw';
import { WorldEntityType } from '../../services/types/worldEntity.types';

// Extend expect with jest-axe
expect.extend(toHaveNoViolations);

// Mock clipboard API
Object.assign(navigator, {
  clipboard: {
    writeText: vi.fn(),
  },
});

// Mock Pointer Events for Radix UI
class MockPointerEvent extends Event {
  button: number;
  ctrlKey: boolean;
  pointerType: string;

  constructor(type: string, props: PointerEventInit) {
    super(type, props);
    this.button = props.button || 0;
    this.ctrlKey = props.ctrlKey || false;
    this.pointerType = props.pointerType || 'mouse';
  }
}
// eslint-disable-next-line @typescript-eslint/no-explicit-any
window.PointerEvent = MockPointerEvent as any;
window.HTMLElement.prototype.scrollIntoView = vi.fn();
window.HTMLElement.prototype.releasePointerCapture = vi.fn();
window.HTMLElement.prototype.hasPointerCapture = vi.fn();

const BASE_URL = 'http://localhost:5000';

describe('EntityFormModal', () => {
  beforeAll(() => {
    server.listen({ onUnhandledRequest: 'error' });
  });

  beforeEach(() => {
    server.resetHandlers();
    // Spy on console error but don't silence it completely so we can see what happens
    vi.spyOn(console, 'error');
  });

  afterAll(() => {
    server.close();
    vi.restoreAllMocks();
  });

  describe('Create Mode', () => {
    it('should call createEntity mutation on valid submit', async () => {
        const preloadedState = {
            worldSidebar: {
              isEntityFormOpen: true,
              editingEntityId: null,
              newEntityParentId: 'parent-1',
              selectedWorldId: 'world-1',
              expandedNodeIds: [],
              selectedEntityId: null,
              isWorldFormOpen: false,
              editingWorldId: null,
            },
        };
        const user = userEvent.setup();
        
        server.use(
            http.get(`${BASE_URL}/api/v1/worlds/world-1/entities/parent-1`, () => {
                return HttpResponse.json({
                    entity: {
                        id: 'parent-1',
                        name: 'Parent Entity',
                        entityType: WorldEntityType.Region, // Region allows City
                        worldId: 'world-1',
                        parentId: 'root',
                        hasChildren: true,
                        //... minimal fields
                        path: ['root'],
                        depth: 0,
                        ownerId: 'u1',
                        createdAt: '', updatedAt: '', isDeleted: false,
                        tags: []
                    }
                });
            }),
            http.post(`${BASE_URL}/api/v1/worlds/world-1/entities`, async () => {
                return HttpResponse.json({
                    entity: {
                        id: 'new-entity',
                        name: 'New Entity',
                        parentId: 'parent-1',
                        entityType: WorldEntityType.City,
                        description: 'Description',
                        tags: [],
                        worldId: 'world-1',
                        path: ['root', 'parent-1'],
                        depth: 1,
                        hasChildren: false,
                        ownerId: 'user-1',
                        createdAt: new Date().toISOString(),
                        updatedAt: new Date().toISOString(),
                        isDeleted: false,
                    }
                });
            })
        );

        const { store } = renderWithProviders(<EntityFormModal />, { preloadedState });

        // Wait for form to appear (loader to disappear)
        const nameInput = await screen.findByRole('textbox', { name: /name/i });
        await user.type(nameInput, 'New Entity');
        
        await user.type(screen.getByRole('textbox', { name: /description/i }), 'Description');
        
        // Use fireEvent for Select interaction to avoid user-event flakiness with Radix primitives in tests
        const typeSelect = screen.getByRole('combobox', { name: /type/i });
        fireEvent.click(typeSelect);
        const option = await screen.findByRole('option', { name: WorldEntityType.City });
        fireEvent.click(option);

        const submitBtn = screen.getByRole('button', { name: /create/i });
        fireEvent.click(submitBtn);

        await waitFor(() => {
            expect(store.getState().worldSidebar.isEntityFormOpen).toBe(false);
        });
    });
  });

  describe('Edit Mode', () => {
    it('should call updateEntity mutation on valid submit', async () => {
        const preloadedState = {
            worldSidebar: {
              isEntityFormOpen: true,
              editingEntityId: 'entity-1',
              newEntityParentId: null,
              selectedWorldId: 'world-1',
              expandedNodeIds: [],
              selectedEntityId: null,
              isWorldFormOpen: false,
              editingWorldId: null,
            },
        };

        const user = userEvent.setup();

        server.use(
            http.get(`${BASE_URL}/api/v1/worlds/world-1/entities/entity-1`, () => {
                return HttpResponse.json({
                    entity: {
                        id: 'entity-1',
                        name: 'Old Name',
                        entityType: WorldEntityType.City,
                        worldId: 'world-1',
                        tags: [],
                        parentId: 'parent-1', 
                        path: ['root', 'parent-1'],
                        depth: 1,
                        hasChildren: false,
                        ownerId: 'user-1',
                        createdAt: new Date().toISOString(),
                        updatedAt: new Date().toISOString(),
                        isDeleted: false,
                        description: 'Old Description'
                    }
                });
            }),
            http.put(`${BASE_URL}/api/v1/worlds/world-1/entities/entity-1`, async () => {
                const response = HttpResponse.json({
                    entity: { 
                        id: 'entity-1', 
                        name: 'Updated Name', 
                        entityType: WorldEntityType.City,
                        worldId: 'world-1',
                        tags: [],
                        parentId: 'parent-1', 
                        path: ['root', 'parent-1'],
                        depth: 1,
                        hasChildren: false,
                        ownerId: 'user-1',
                        createdAt: new Date().toISOString(),
                        updatedAt: new Date().toISOString(), 
                        isDeleted: false,
                        description: 'Old Description'
                    }
                });
                return response;
            })
        );

        const { store } = renderWithProviders(<EntityFormModal />, { preloadedState });

        await waitFor(() => {
            expect(screen.getByRole('textbox', { name: /name/i })).toHaveValue('Old Name');
        });

        // Verify entity type is pre-populated (text should be present in the trigger)
        // Note: usage of 'City' depends on how SelectValue renders it. Usually it renders the text content of the selected item.
        expect(screen.getByText('City')).toBeVisible();

        const nameInput = screen.getByRole('textbox', { name: /name/i });
        await user.clear(nameInput);
        await user.type(nameInput, 'Updated Name');

        // Manually select type in Edit Mode as well to ensure state is set effectively
        const typeSelect = screen.getByRole('combobox', { name: /type/i });
        fireEvent.click(typeSelect);
        const option = await screen.findByRole('option', { name: WorldEntityType.City });
        fireEvent.click(option);

        const submitBtn = screen.getByRole('button', { name: /save/i });
        fireEvent.click(submitBtn);

        await waitFor(() => {
            expect(store.getState().worldSidebar.isEntityFormOpen).toBe(false);
        });
    });
  });

  describe('Accessibility', () => {
    it('should have no accessibility violations', async () => {
        const preloadedState = {
            worldSidebar: {
              isEntityFormOpen: true,
              editingEntityId: null,
              newEntityParentId: 'root',
              selectedWorldId: 'world-1',
              expandedNodeIds: [],
              selectedEntityId: null,
              isWorldFormOpen: false,
              editingWorldId: null,
            },
        };
        const { container } = renderWithProviders(<EntityFormModal />, { preloadedState });

        await screen.findByRole('dialog');
        const results = await axe(container);
        expect(results).toHaveNoViolations();
    });
  });

  describe('Context Awareness', () => {
    it('should filter entity types based on parent entity type (Continent -> Country/Region)', async () => {
        const preloadedState = {
            worldSidebar: {
              isEntityFormOpen: true,
              editingEntityId: null,
              newEntityParentId: 'continent-1',
              selectedWorldId: 'world-1',
              expandedNodeIds: [],
              selectedEntityId: null,
              isWorldFormOpen: false,
              editingWorldId: null,
            },
        };
        
        server.use(
            http.get(`${BASE_URL}/api/v1/worlds/world-1/entities/continent-1`, () => {
                return HttpResponse.json({
                    entity: {
                        id: 'continent-1',
                        name: 'My Continent',
                        entityType: WorldEntityType.Continent,
                        worldId: 'world-1',
                        tags: [],
                        parentId: 'root', 
                        path: ['root'],
                        depth: 0,
                        hasChildren: true,
                        ownerId: 'user-1',
                        createdAt: new Date().toISOString(),
                        updatedAt: new Date().toISOString(), 
                        isDeleted: false,
                    }
                });
            })
        );
        
        renderWithProviders(<EntityFormModal />, { preloadedState });
        
        // Open Select
        const typeSelect = await screen.findByRole('combobox', { name: /type/i });
        fireEvent.click(typeSelect);
        
        // Check Country is present (Continent -> Country is valid)
        expect(screen.getByRole('option', { name: WorldEntityType.Country })).toBeInTheDocument();
        
        // Check City is NOT present (Continent -> City is invalid)
        expect(screen.queryByRole('option', { name: WorldEntityType.City })).not.toBeInTheDocument();
    });

    it('should show root types when no parent is selected', async () => {
        const preloadedState = {
            worldSidebar: {
              isEntityFormOpen: true,
              editingEntityId: null,
              newEntityParentId: null, // Root
              selectedWorldId: 'world-1',
              expandedNodeIds: [],
              selectedEntityId: null,
              isWorldFormOpen: false,
              editingWorldId: null,
            },
        };
        
        renderWithProviders(<EntityFormModal />, { preloadedState });
        
        const typeSelect = await screen.findByRole('combobox', { name: /type/i });
        fireEvent.click(typeSelect);
        
        // Root types: Continent, Campaign
        expect(screen.getByRole('option', { name: WorldEntityType.Continent })).toBeInTheDocument();
        expect(screen.getByRole('option', { name: WorldEntityType.Campaign })).toBeInTheDocument();
        
        // Non-root types should not be present
        expect(screen.queryByRole('option', { name: WorldEntityType.City })).not.toBeInTheDocument();
    });
  });
});
