import { describe, test, beforeAll, afterEach, afterAll, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { store } from '../../store/store';
import App from '../../App';

// Setup mock server
import { setupServer } from 'msw/node';
import { handlers } from '../mocks/handlers';
import { http, HttpResponse } from 'msw';
import type { World } from '../../services/types/world.types';
import { WorldEntityType, type WorldEntity } from '../../services/types/worldEntity.types';

const BASE_URL = 'http://localhost:5000';

const server = setupServer(...handlers);

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

describe('Entity Creation Integration', () => {
  test('should create a new entity successfully', async () => {
    vi.setConfig({ testTimeout: 30000 });

    const testWorld: World = {
      id: 'world-789',
      name: 'Forgotten Realms',
      description: 'A high fantasy world',
      ownerId: 'test-user@example.com',
      createdAt: '2026-01-13T11:00:00Z',
      updatedAt: '2026-01-13T11:00:00Z',
      isDeleted: false
    };

    // Use a Map like the default handlers
    const createdEntities = new Map<string, WorldEntity>();

    // Add necessary MSW handlers
    server.use(
      http.get(`${BASE_URL}/api/v1/worlds`, () => {
        return HttpResponse.json({
          data: [testWorld],
          meta: { requestId: '1', timestamp: '' }
        });
      }),
      http.get(`${BASE_URL}/api/v1/worlds/world-789/entities`, ({ request }) => {
        const url = new URL(request.url);
        const parentId = url.searchParams.get('parentId');
        
        // Filter by parentId (null for root)
        let entities = Array.from(createdEntities.values()).filter(
          (entity: WorldEntity) => entity.worldId === 'world-789'
        );
        
        if (!parentId || parentId === 'null') {
          entities = entities.filter((e: WorldEntity) => e.parentId === null);
        } else {
          entities = entities.filter((e: WorldEntity) => e.parentId === parentId);
        }

        return HttpResponse.json({ 
          data: entities,
          meta: { count: entities.length, nextCursor: null },
        });
      }),
      http.post(`${BASE_URL}/api/v1/worlds/world-789/entities`, async ({ request }) => {
        const body = await request.json() as { name: string; description: string; entityType: WorldEntityType; parentId: string | null };
        const newEntityId = `new-entity-${Date.now()}`;
        const newEntity = {
          id: newEntityId,
          name: body.name,
          description: body.description,
          entityType: body.entityType,
          worldId: 'world-789',
          parentId: body.parentId,
          hasChildren: false,
          depth: 0,
          path: [],
          tags: [],
          ownerId: 'test-user@example.com',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          isDeleted: false,
          schemaVersion: 1
        };
        createdEntities.set(newEntityId, newEntity);
        return HttpResponse.json({
          data: newEntity
        }, { status: 201 });
      })
    );
    render(
      <Provider store={store}>
        <App />
      </Provider>
    );

    const user = userEvent.setup();

    // 1. Select a world to enable entity creation
    const worldTrigger = await screen.findByRole('combobox', { name: /select world/i });
    await user.click(worldTrigger);
    
    // Select the option explicitly using role 'option'
    const worldOption = await screen.findByRole('option', { name: 'Forgotten Realms' });
    await user.click(worldOption);

    // 2. Open the Create Entity modal
    // Find the "Add Root Entity" button (the one with text, in the empty state)
    const createButtons = await screen.findAllByRole('button', { name: /add root entity/i });
    const createBtn = createButtons.find(btn => btn.textContent?.includes('Add Root Entity'));
    if (!createBtn) throw new Error('Could not find Add Root Entity button');
    await user.click(createBtn);

    // 3. Fill out the form
    const nameInput = await screen.findByLabelText(/name/i);
    await user.type(nameInput, 'New Castle');

    const typeSelectTrigger = await screen.findByLabelText(/type/i); // "Type" label
    await user.click(typeSelectTrigger);
    
    // Select a valid type (Continent is valid for root)
    const continentOption = await screen.findByRole('option', { name: /continent/i });
    await user.click(continentOption);

    const descInput = await screen.findByLabelText(/description/i);
    await user.type(descInput, 'A newly created castle');

    // 4. Submit
    const createSubmitBtn = await screen.findByRole('button', { name: /^create$/i });
    await user.click(createSubmitBtn);

    // 5. Verify it appears in the tree (EntityTree updates automatically via tag invalidation)
    // The mock handler adds it to the map and returns it.
    // The tree should re-render with "New Castle"
    await waitFor(() => {
      expect(screen.getByText('New Castle')).toBeInTheDocument();
    });
  });
});
