import { describe, it, expect, beforeEach, afterAll, beforeAll } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../utils/test-utils';
import { server } from '../mocks/server';
import { http, HttpResponse } from 'msw';
import { WorldSidebar } from '../../components/WorldSidebar/WorldSidebar';
import { MainPanel } from '../../components/MainPanel/MainPanel';
import type { World } from '../../services/types/world.types';

const BASE_URL = 'http://localhost:5000';

describe('World Editing Integration', () => {
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

    it('should display edit form in MainPanel when edit button is clicked', async () => {
        const user = userEvent.setup();

        const initialWorld: World = {
            id: 'world-1',
            name: 'Original World',
            description: 'Original Description',
            ownerId: 'u1',
            createdAt: '2024-01-01',
            updatedAt: '2024-01-01',
            isDeleted: false
        };

        // Mock API responses
        server.use(
            http.get(`${BASE_URL}/api/v1/worlds`, () => {
                return HttpResponse.json({
                    data: [initialWorld],
                    meta: { requestId: '1', timestamp: '' }
                });
            }),

            http.get(`${BASE_URL}/api/v1/worlds/world-1`, () => {
                return HttpResponse.json({
                    data: initialWorld,
                    meta: { requestId: '2', timestamp: '' }
                });
            }),

            http.get(`${BASE_URL}/api/v1/worlds/world-1/entities`, () => {
                return HttpResponse.json({ 
                    data: [],
                    meta: { count: 0, nextCursor: null },
                });
            })
        );

        const preloadedState = {
            worldSidebar: {
                selectedWorldId: 'world-1',
                selectedEntityId: null,
                expandedNodeIds: [],
                mainPanelMode: 'empty' as const,
                isWorldFormOpen: false,
                editingWorldId: null,
                editingEntityId: null,
                newEntityParentId: null,
                hasUnsavedChanges: false,
                deletingEntityId: null,
                showDeleteConfirmation: false,
                movingEntityId: null,
                creatingEntityParentId: null,
            },
        };

        renderWithProviders(
          <>
            <WorldSidebar />
            <MainPanel />
          </>,
          { preloadedState }
        );

        // 1. Verify initial state
        await screen.findByText('Original World'); // Wait for load
        
        // 2. Click Edit button (Settings icon)
        const editButton = screen.getByLabelText('Edit current world');
        await user.click(editButton);

        // 3. Verify form appears in MainPanel with pre-filled data
        const heading = await screen.findByRole('heading', { name: 'Edit World' });
        expect(heading).toBeInTheDocument();
        
        const nameInput = screen.getByLabelText(/World Name/i) as HTMLInputElement;
        const descInput = screen.getByLabelText(/Description/i) as HTMLTextAreaElement;

        expect(nameInput.value).toBe('Original World');
        expect(descInput.value).toBe('Original Description');

        // 4. Verify form is interactive
        await user.clear(nameInput);
        await user.type(nameInput, 'Updated World');
        
        expect(nameInput.value).toBe('Updated World');
    });

    it('should close form and return to welcome state after saving edited world', async () => {
        const user = userEvent.setup();

        // Use the seeded world ID from mock data
        const initialWorld: World = {
            id: 'test-world-123',
            name: 'Forgotten Realms',
            description: 'A high fantasy world',
            ownerId: 'test-user@example.com',
            createdAt: '2026-01-13T11:00:00Z',
            updatedAt: '2026-01-13T11:00:00Z',
            isDeleted: false
        };

        // Mock API responses (in addition to default handlers)
        server.use(
            http.get(`http://localhost:5000/api/v1/worlds`, () => {
                return HttpResponse.json({
                    data: [initialWorld],
                    meta: { requestId: '1', timestamp: '' }
                });
            }),

            http.get(`http://localhost:5000/api/v1/worlds/test-world-123`, () => {
                return HttpResponse.json({
                    data: initialWorld,
                    meta: { requestId: '2', timestamp: '' }
                });
            }),

            http.get(`http://localhost:5000/api/v1/worlds/test-world-123/entities`, () => {
                return HttpResponse.json({ 
                    data: [],
                    meta: { count: 0, nextCursor: null },
                });
            })
        );

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
                showDeleteConfirmation: false,
                movingEntityId: null,
                creatingEntityParentId: null,
            },
        };

        renderWithProviders(
          <>
            <WorldSidebar />
            <MainPanel />
          </>,
          { preloadedState }
        );

        // 1. Verify initial welcome state
        await screen.findByText('Welcome to Libris Maleficarum');
        
        // 2. Click Edit button
        const editButton = await screen.findByLabelText('Edit current world');
        await user.click(editButton);

        // 3. Verify form appears
        const heading = await screen.findByRole('heading', { name: 'Edit World' });
        expect(heading).toBeInTheDocument();

        // 4. Modify form field
        const nameInput = screen.getByLabelText(/World Name/i) as HTMLInputElement;
        await user.clear(nameInput);
        await user.type(nameInput, 'Updated Forgotten Realms');
        
        expect(nameInput.value).toBe('Updated Forgotten Realms');

        // 5. Save
        const saveButton = screen.getByRole('button', { name: /Save World/i });
        await user.click(saveButton);

        // 6. Verify form closes - the Edit World heading should disappear
        // Wait for the heading to disappear from the DOM
        await screen.findByText(/Welcome to Libris Maleficarum/i);
        expect(screen.queryByRole('heading', { name: 'Edit World' })).not.toBeInTheDocument();
        
        // 7. Verify welcome state is displayed (with grimoire message)
        expect(screen.getByText(/Your personal grimoire/i)).toBeInTheDocument();
    });
});
