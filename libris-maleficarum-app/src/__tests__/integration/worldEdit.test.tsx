import { describe, it, expect, beforeEach, afterAll, beforeAll } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../utils/test-utils';
import { server } from '../mocks/server';
import { http, HttpResponse } from 'msw';
import { WorldSidebar } from '../../components/WorldSidebar/WorldSidebar';
import { World } from '../../services/types/world.types';

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

    it('should edit an existing world name and description', async () => {
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

        const updatedWorld: World = {
            ...initialWorld,
            name: 'Updated World',
            description: 'Updated Description'
        };

        // Mock Database State
        let currentWorldState = initialWorld;

        // Mock API responses
        server.use(
            // 1. Get Worlds List - Dynamic return based on current state
            http.get(`${BASE_URL}/api/v1/worlds`, () => {
                return HttpResponse.json({
                    data: [currentWorldState],
                    meta: { requestId: '1', timestamp: '' }
                });
            }),

            // 2. Get Single World
            http.get(`${BASE_URL}/api/v1/worlds/world-1`, () => {
                return HttpResponse.json({
                    data: currentWorldState,
                    meta: { requestId: '2', timestamp: '' }
                });
            }),

            // 3. Update World
            http.put(`${BASE_URL}/api/v1/worlds/world-1`, async ({ request }) => {
                const body = await request.json() as { name: string; description: string };
                
                if (body.name !== 'Updated World' || body.description !== 'Updated Description') {
                     return new HttpResponse(null, { status: 400 });
                }

                // Update state
                currentWorldState = updatedWorld;

                return HttpResponse.json({
                    data: updatedWorld,
                    meta: { requestId: '3', timestamp: '' }
                });
            }),

             // 4. Hierarchy (called by Sidebar)
             http.get(`${BASE_URL}/api/v1/worlds/world-1/entities`, () => {
                return HttpResponse.json({ items: [], totalCount: 0 });
             })
        );

        const preloadedState = {
            worldSidebar: {
                selectedWorldId: 'world-1',
                // other state defaults
            }
        };

        renderWithProviders(<WorldSidebar />, { preloadedState });

        // 1. Verify initial state
        await screen.findByText('Original World'); // Wait for load
        
        // 2. Click Edit button (Settings icon)
        const editButton = screen.getByLabelText('Edit current world');
        await user.click(editButton);

        // 3. Verify Modal opens with pre-filled data
        const modal = await screen.findByRole('dialog', { name: 'Edit World' });
        expect(modal).toBeInTheDocument();
        
        const nameInput = screen.getByLabelText(/World Name/i);
        const descInput = screen.getByLabelText(/Description/i);

        expect(nameInput).toHaveValue('Original World');
        expect(descInput).toHaveValue('Original Description');

        // 4. Modify fields
        await user.clear(nameInput);
        await user.type(nameInput, 'Updated World');
        
        await user.clear(descInput);
        await user.type(descInput, 'Updated Description');

        // 5. Save
        const saveButton = screen.getByRole('button', { name: 'Save' });
        await user.click(saveButton);

        // 6. Verify Modal closes
        await waitFor(() => {
            expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
        });

        // 7. Verify Optimistic UI / Refetch update
        // The selector should now show "Updated World"
        await waitFor(() => {
            expect(screen.getByText('Updated World')).toBeInTheDocument();
        });
        
        // Ensure "Original World" is gone
        expect(screen.queryByText('Original World')).not.toBeInTheDocument();
    });
});
