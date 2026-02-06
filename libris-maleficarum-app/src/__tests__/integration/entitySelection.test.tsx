import { describe, it, expect, beforeEach, afterAll, beforeAll } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '../utils/testUtils';
import { server } from '../mocks/server';
import { http, HttpResponse } from 'msw';
import { MainPanel } from '../../components/MainPanel/MainPanel';
import { WorldSidebar } from '../../components/WorldSidebar/WorldSidebar';
import { WorldEntityType } from '../../services/types/worldEntity.types';

const BASE_URL = 'http://localhost:5000';

describe('Entity Selection Integration', () => {
    beforeAll(() => {
        server.listen({ onUnhandledRequest: 'error' });
    });

    beforeEach(() => {
        server.resetHandlers();
        sessionStorage.clear();
    });

    afterAll(() => {
        server.close();
    });

    it('should display entity details in MainPanel when entity is selected in Sidebar', async () => {
        // Setup initial state with a selected world
        const preloadedState = {
            worldSidebar: {
                selectedWorldId: 'test-world-123',
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
            },
        };

        // Mock API responses
        server.use(
            // 1. Get Root Entities for Sidebar
            http.get(`${BASE_URL}/api/v1/worlds/test-world-123/entities`, ({ request }) => {
                const url = new URL(request.url);
                const parentId = url.searchParams.get('parentId');
                
                // Robust check for root level (null/undefined/empty)
                if (!parentId || parentId === 'null' || parentId === 'undefined' || parentId === '') {
                    return HttpResponse.json({
                        data: [
                            {
                                id: 'continent-faerun',
                                name: 'Faerûn',
                                description: 'A continent in the world of Toril',
                                entityType: WorldEntityType.Continent,
                                worldId: 'test-world-123',
                                parentId: null,
                                hasChildren: true,
                                depth: 0,
                                path: [],
                                tags: ['forgotten-realms', 'primary-setting'],
                                ownerId: 'test-user@example.com',
                                createdAt: '2026-01-13T12:00:00Z', updatedAt: '2026-01-13T12:00:00Z', isDeleted: false,
                                schemaVersion: 1
                            }
                        ],
                        meta: {
                            count: 1,
                            nextCursor: null,
                        },
                    });
                }
                // Return empty for children requests in this simple test
                return HttpResponse.json({ 
                    data: [],
                    meta: {
                        count: 0,
                        nextCursor: null,
                    },
                });
            }),

            // 2. Get Entity Details for MainPanel
            http.get(`${BASE_URL}/api/v1/worlds/test-world-123/entities/continent-faerun`, () => {
                return HttpResponse.json({
                    data: {
                        id: 'continent-faerun',
                        name: 'Faerûn',
                        description: 'A continent in the world of Toril',
                        entityType: WorldEntityType.Continent,
                        worldId: 'test-world-123',
                        parentId: null,
                        hasChildren: true,
                        depth: 0,
                        path: [],
                        tags: ['forgotten-realms', 'primary-setting'],
                        ownerId: 'test-user@example.com',
                        createdAt: '2026-01-13T12:00:00Z', updatedAt: '2026-01-13T12:00:00Z', isDeleted: false,
                        schemaVersion: 1
                    },
                });
            })
        );
        
        // Render App-like structure (Sidebar + MainPanel)
        const TestApp = () => (
            <>
                <WorldSidebar optimisticallyDeletedIds={new Set()} />
                <MainPanel />
            </>
        );

        const { store } = renderWithProviders(<TestApp />, { preloadedState });

        // 1. Verify initial Welcome message
        expect(screen.getByText('Welcome, Chronicler')).toBeInTheDocument();

        // 2. Find entity in sidebar
        const entityNode = await screen.findByText('Faerûn');
        
        // 3. Click entity
        fireEvent.click(entityNode);

        // 4. Verify Redux state update
        expect(store.getState().worldSidebar.selectedEntityId).toBe('continent-faerun');

        // 5. Verify MainPanel updates (Welcome message gone, Entity Details shown)
        await waitFor(() => {
            const hasWelcome = screen.queryByText('Welcome, Chronicler');
            expect(hasWelcome).not.toBeInTheDocument();
        });
        
        // Use findByRole to wait for loading to finish and content to appear
        expect(await screen.findByRole('heading', { name: 'Faerûn' })).toBeInTheDocument();
        expect(screen.getByText('Continent')).toBeInTheDocument(); // Type badge
        expect(screen.getByText('A continent in the world of Toril')).toBeInTheDocument(); // Description
    });

    it('should show loading state in MainPanel when selecting a new entity', async () => {
        const preloadedState = {
            worldSidebar: {
                selectedWorldId: 'test-world-123',
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
            },
        };

        server.use(
            // Add handler for worlds list
            http.get(`${BASE_URL}/api/v1/worlds`, () => {
                return HttpResponse.json({
                    data: [{
                        id: 'test-world-123',
                        name: 'Test World',
                        description: 'A test world',
                        ownerId: 'u1',
                        createdAt: '2026-01-13T12:00:00Z',
                        updatedAt: '2026-01-13T12:00:00Z',
                        isDeleted: false
                    }],
                    meta: { requestId: '1', timestamp: '' }
                });
            }),
            // Sidebar Entities
             http.get(`${BASE_URL}/api/v1/worlds/test-world-123/entities`, ({ request }) => {
                const url = new URL(request.url);
                const parentId = url.searchParams.get('parentId');
                if (!parentId || parentId === 'null' || parentId === 'undefined') {
                    return HttpResponse.json({
                        data: [
                            {
                                id: 'e1', name: 'E1', description: 'Test location', entityType: WorldEntityType.Location,
                                worldId: 'test-world-123', parentId: null, hasChildren: false,
                                depth: 0, path: [], tags: [], ownerId: 'u1',
                                createdAt: '2026-01-13T12:00:00Z', updatedAt: '2026-01-13T12:00:00Z', isDeleted: false,
                                schemaVersion: 1
                            }
                        ],
                        meta: {
                            count: 1,
                            nextCursor: null,
                        },
                    });
                }
                return HttpResponse.json({ 
                    data: [],
                    meta: {
                        count: 0,
                        nextCursor: null,
                    },
                });
            }),
            // Entity Details with delay
            http.get(`${BASE_URL}/api/v1/worlds/test-world-123/entities/e1`, async () => {
                await new Promise(resolve => setTimeout(resolve, 100));
                return HttpResponse.json({
                    data: {
                        id: 'e1', name: 'E1', description: 'Test location', entityType: WorldEntityType.Location,
                        worldId: 'test-world-123', parentId: null, hasChildren: false,
                        depth: 0, path: [], tags: [], ownerId: 'u1',
                        createdAt: '2026-01-13T12:00:00Z', updatedAt: '2026-01-13T12:00:00Z', isDeleted: false,
                        schemaVersion: 1
                    },
                });
            })
        );

        const TestApp = () => (
            <>
                <WorldSidebar optimisticallyDeletedIds={new Set()} />
                <MainPanel />
            </>
        );

        renderWithProviders(<TestApp />, { preloadedState });

        // Select entity
        const entityNode = await screen.findByText('E1');
        fireEvent.click(entityNode);

        // Should see loading skeleton/spinner
        expect(screen.getByRole('status', { name: /consulting the tome/i })).toBeInTheDocument();

        // Should eventually see content
        await screen.findByRole('heading', { name: 'E1' });
    });
});
