import { describe, it, expect, beforeEach, afterAll, beforeAll } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { renderWithProviders } from '../utils/test-utils';
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
                selectedWorldId: 'world-1',
                selectedEntityId: null,
                expandedNodeIds: [],
                isWorldFormOpen: false,
                editingWorldId: null,
                isEntityFormOpen: false,
                editingEntityId: null,
                newEntityParentId: null,
            },
        };

        // Mock API responses
        server.use(
            // 1. Get Root Entities for Sidebar
            http.get(`${BASE_URL}/api/v1/worlds/world-1/entities`, ({ request }) => {
                const url = new URL(request.url);
                const parentId = url.searchParams.get('parentId');
                
                // Robust check for root level (null/undefined/empty)
                if (!parentId || parentId === 'null' || parentId === 'undefined' || parentId === '') {
                    return HttpResponse.json({
                        items: [
                            {
                                id: 'continent-1',
                                name: 'Test Continent',
                                entityType: WorldEntityType.Continent,
                                worldId: 'world-1',
                                parentId: null,
                                hasChildren: false,
                                depth: 0,
                                path: ['root'],
                                tags: [],
                                ownerId: 'u1',
                                createdAt: '2024-01-01', updatedAt: '2024-01-01', isDeleted: false
                            }
                        ],
                        totalCount: 1,
                        page: 1,
                        pageSize: 100,
                        hasMore: false
                    });
                }
                // Return empty for children requests in this simple test
                return HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 100, hasMore: false });
            }),

            // 2. Get Entity Details for MainPanel
            http.get(`${BASE_URL}/api/v1/worlds/world-1/entities/continent-1`, () => {
                return HttpResponse.json({
                    entity: {
                        id: 'continent-1',
                        name: 'Test Continent',
                        description: 'A vast land of testing.',
                        entityType: WorldEntityType.Continent,
                        worldId: 'world-1',
                        parentId: null,
                        hasChildren: false,
                        depth: 0,
                        path: ['root'],
                        tags: ['fantasy', 'test'],
                        ownerId: 'u1',
                        createdAt: '2024-01-01', updatedAt: '2024-01-01', isDeleted: false
                    }
                });
            })
        );
        
        // Render App-like structure (Sidebar + MainPanel)
        const TestApp = () => (
            <>
                <WorldSidebar />
                <MainPanel />
            </>
        );

        const { store } = renderWithProviders(<TestApp />, { preloadedState });

        // 1. Verify initial Welcome message
        expect(screen.getByText('Welcome to Libris Maleficarum')).toBeInTheDocument();

        // 2. Find entity in sidebar
        const entityNode = await screen.findByText('Test Continent');
        
        // 3. Click entity
        fireEvent.click(entityNode);

        // 4. Verify Redux state update
        expect(store.getState().worldSidebar.selectedEntityId).toBe('continent-1');

        // 5. Verify MainPanel updates (Welcome message gone, Entity Details shown)
        await waitFor(() => {
            const hasWelcome = screen.queryByText('Welcome to Libris Maleficarum');
            expect(hasWelcome).not.toBeInTheDocument();
        });
        
        // Use findByRole to wait for loading to finish and content to appear
        expect(await screen.findByRole('heading', { name: 'Test Continent' })).toBeInTheDocument();
        expect(screen.getByText('Continent')).toBeInTheDocument(); // Type badge
        expect(screen.getByText('A vast land of testing.')).toBeInTheDocument(); // Description
    });

    it('should show loading state in MainPanel when selecting a new entity', async () => {
        const preloadedState = {
            worldSidebar: {
                selectedWorldId: 'world-1',
                selectedEntityId: null,
                expandedNodeIds: [],
                isWorldFormOpen: false,
                editingWorldId: null,
                isEntityFormOpen: false,
                editingEntityId: null,
                newEntityParentId: null,
            },
        };

        server.use(
            // Sidebar Entities
             http.get(`${BASE_URL}/api/v1/worlds/world-1/entities`, ({ request }) => {
                const url = new URL(request.url);
                const parentId = url.searchParams.get('parentId');
                if (!parentId || parentId === 'null' || parentId === 'undefined') {
                    return HttpResponse.json({
                        items: [
                            {
                                id: 'e1', name: 'E1', entityType: WorldEntityType.Location,
                                worldId: 'world-1', parentId: null, hasChildren: false,
                                depth: 0, path: [], tags: [], ownerId: 'u1',
                                createdAt: '', updatedAt: '', isDeleted: false
                            }
                        ],
                        totalCount: 1,
                        page: 1,
                        pageSize: 100,
                        hasMore: false
                    });
                }
                return HttpResponse.json({ items: [], totalCount: 0, page: 1, pageSize: 100, hasMore: false });
            }),
            // Entity Details with delay
            http.get(`${BASE_URL}/api/v1/worlds/world-1/entities/e1`, async () => {
                await new Promise(resolve => setTimeout(resolve, 100));
                return HttpResponse.json({
                    entity: {
                        id: 'e1', name: 'E1', entityType: WorldEntityType.Location,
                        worldId: 'world-1', parentId: null, hasChildren: false,
                        depth: 0, path: [], tags: [], ownerId: 'u1',
                        createdAt: '', updatedAt: '', isDeleted: false
                    }
                });
            })
        );

        const TestApp = () => (
            <>
                <WorldSidebar />
                <MainPanel />
            </>
        );

        renderWithProviders(<TestApp />, { preloadedState });

        // Select entity
        const entityNode = await screen.findByText('E1');
        fireEvent.click(entityNode);

        // Should see loading skeleton/spinner
        expect(screen.getByRole('status', { name: /loading/i })).toBeInTheDocument();

        // Should eventually see content
        await screen.findByRole('heading', { name: 'E1' });
    });
});
