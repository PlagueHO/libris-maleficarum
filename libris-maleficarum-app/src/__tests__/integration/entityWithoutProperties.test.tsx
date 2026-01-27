/**
 * Integration test for entities without custom property schemas (T053)
 *
 * Tests backward compatibility for entity types that don't have propertySchema defined.
 * Verifies that Character entities (no schema) work correctly in both edit and view modes
 * without displaying a custom properties section.
 *
 * @module __tests__/integration/entityWithoutProperties
 */

import { describe, it, expect, beforeAll, afterEach, afterAll, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { store } from '@/store/store';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import { handlers } from '../mocks/handlers';
import App from '@/App';
import type {
  WorldEntity,
  CreateWorldEntityRequest,
} from '@/services/types/worldEntity.types';

// Detect environment for baseUrl
let isNode = false;
try {
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const globalAny = globalThis as any;
  isNode =
    typeof globalAny.process !== 'undefined' &&
    !!globalAny.process.versions &&
    !!globalAny.process.versions.node;
} catch {
  // ignore
}
const baseUrl = isNode ? 'http://localhost:5000' : '';

const server = setupServer(...handlers);

beforeAll(() => {
  server.listen();
});

afterEach(() => {
  server.resetHandlers();
});

afterAll(() => {
  server.close();
});

describe('T053: Integration - Entity Without propertySchema (Character)', () => {
  it.skip('should create Character entity without displaying custom properties section', async () => {
    const user = userEvent.setup();

    // Mock Faerûn entity for fetching by ID (needed when opening create form)
    const faerunEntity: WorldEntity = {
      id: 'continent-faerun',
      worldId: 'test-world-123',
      parentId: null,
      entityType: 'GeographicRegion',
      name: 'Faerûn',
      description: 'The primary continent of the Forgotten Realms',
      tags: ['continent', 'fantasy'],
      properties: {
        Climate: 'Varied',
        Terrain: 'Diverse',
      },
      path: [],
      depth: 0,
      hasChildren: true,
      ownerId: 'test-user@example.com',
      isDeleted: false,
      schemaVersion: 'v1.0',
      createdAt: new Date().toISOString(),
      createdBy: 'test-user@example.com',
      modifiedAt: new Date().toISOString(),
      modifiedBy: 'test-user@example.com',
      version: 1,
    };

    // Mock GET entity by ID (for parent entity lookup)
    server.use(
      http.get(`${baseUrl}/api/v1/worlds/:worldId/entities/:entityId`, ({ params }) => {
        const { entityId } = params;
        if (entityId === 'continent-faerun') {
          return HttpResponse.json({ data: faerunEntity });
        }
        return new HttpResponse(null, { status: 404 });
      })
    );

    // Mock successful creation
    server.use(
      http.post(`${baseUrl}/api/v1/worlds/:worldId/entities`, async ({ request, params }) => {
        const { worldId } = params as { worldId: string };
        const body = (await request.json()) as CreateWorldEntityRequest;

        const newEntity: WorldEntity = {
          id: `character-${Date.now()}`,
          worldId,
          parentId: body.parentId || null,
          entityType: body.entityType,
          name: body.name,
          description: body.description || '',
          tags: body.tags || [],
          properties: null, // Character has no custom properties
          path: body.parentId ? ['continent-faerun'] : [],
          depth: body.parentId ? 1 : 0,
          hasChildren: false,
          ownerId: 'test-user@example.com',
          isDeleted: false,
          schemaVersion: 'v1.0',
          createdAt: new Date().toISOString(),
          createdBy: 'test-user@example.com',
          modifiedAt: new Date().toISOString(),
          modifiedBy: 'test-user@example.com',
          version: 1,
        };

        return HttpResponse.json(newEntity, { status: 201 });
      })
    );

    // Render app
    render(
      <Provider store={store}>
        <App />
      </Provider>
    );

   // Select world first
    const worldTrigger = await screen.findByRole('combobox', { name: /select world/i });
    await user.click(worldTrigger);

    const worldOption = await screen.findByRole('option', { name: 'Forgotten Realms' });
    await user.click(worldOption);

    // Wait for entities to load
    await waitFor(
      () => {
        expect(screen.getByText('Faerûn')).toBeInTheDocument();
      },
      { timeout: 3000 }
    );

    // Select world in sidebar (select the Faerûn entity as context)
    const faerûnNodes = screen.getAllByText('Faerûn');
    await user.click(faerûnNodes[0]);

    // Click "Add child to Faerûn" button
    const newEntityButton = screen.getByRole('button', { name: /add child to faerûn/i });
    await user.click(newEntityButton);

    // Fill in basic entity details (form opens in MainPanel, not as dialog)
    const nameInput = await screen.findByPlaceholderText(/entity name/i);
    await user.type(nameInput, 'Elminster Aumar');

    const descriptionInput = screen.getByPlaceholderText(/brief description/i);
    await user.type(descriptionInput, 'A legendary wizard and sage');

    // Select Character entity type
    const typeSelect = screen.getByRole('combobox', { name: /type/i });
    await user.click(typeSelect);
    
    // Type to search for Character type (faster than scrolling)
    await user.keyboard('Cha');
    
    // Wait for Character option to appear by text
    const characterOption = await screen.findByText('Character', { selector: '[role="option"]' });
    await user.click(characterOption);

    // CRITICAL TEST: Verify NO custom properties section appears
    await waitFor(() => {
      expect(screen.queryByText('Character Properties')).not.toBeInTheDocument();
      expect(screen.queryByText(/properties/i)).not.toBeInTheDocument();
    });

    // Submit the form (should work without custom properties)
    const createButton = screen.getByRole('button', { name: 'Create' });
    await user.click(createButton);

    // Verify entity was created successfully
    await waitFor(
      () => {
        expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
      },
      { timeout: 2000 }
    );

    // Success!
  }, 30000);

  it('should display Character entity in view mode without custom properties section', async () => {
    const user = userEvent.setup();

    // Mock Character entity in world hierarchy
    const characterEntity: WorldEntity = {
      id: 'character-elminster',
      worldId: 'world-faerun',
      parentId: 'continent-faerun',
      entityType: 'Character',
      name: 'Elminster Aumar',
      description: 'A legendary wizard and sage of the Forgotten Realms',
      tags: ['wizard', 'sage', 'harpers'],
      properties: null, // No custom properties
      path: ['continent-faerun'],
      depth: 1,
      hasChildren: false,
      ownerId: 'test-user@example.com',
      isDeleted: false,
      schemaVersion: 'v1.0',
      createdAt: new Date().toISOString(),
      createdBy: 'test-user@example.com',
      modifiedAt: new Date().toISOString(),
      modifiedBy: 'test-user@example.com',
      version: 1,
    };

    // Override GET entities to include Character
    server.use(
      http.get(`${baseUrl}/api/v1/worlds/:worldId/entities`, () => {
        return HttpResponse.json({
          data: [
          {
            id: 'continent-faerun',
            worldId: 'world-faerun',
            parentId: null,
            entityType: 'GeographicRegion',
            name: 'Faerûn',
            description: 'The primary continent of the Forgotten Realms',
            tags: ['continent', 'fantasy'],
            properties: {
              Climate: 'Varied - from arctic tundra to tropical jungles',
              Terrain: 'Diverse landscapes including mountains, forests, deserts, and plains',
              Population: 66000000,
              Area: 9000000,
            },
            path: [],
            depth: 0,
            hasChildren: true,
            ownerId: 'test-user@example.com',
            isDeleted: false,
            schemaVersion: 'v1.0',
            createdAt: new Date().toISOString(),
            createdBy: 'test-user@example.com',
            modifiedAt: new Date().toISOString(),
            modifiedBy: 'test-user@example.com',
            version: 1,
          },
          characterEntity,
        ],
          meta: {
            count: 2,
            nextCursor: null,
          },
        });
      })
    );

    // Render app
    render(
      <Provider store={store}>
        <App />
      </Provider>
    );

    // Wait for world to load
    await waitFor(
      () => {
        expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
      },
      { timeout: 3000 }
    );

    // Expand Faerûn to show children
    await waitFor(
      () => {
        const expandButton = screen.getByRole('button', { name: /expand faerûn/i });
        expect(expandButton).toBeInTheDocument();
        return expandButton;
      },
      { timeout: 5000 }
    );
    const expandButton = screen.getByRole('button', { name: /expand faerûn/i });
    await user.click(expandButton);

    // Wait for Character to appear in sidebar
    await waitFor(() => {
      expect(screen.getByText('Elminster Aumar')).toBeInTheDocument();
    });

    // Select the Character entity by clicking on its name
    const characterTreeItem = screen.getByText('Elminster Aumar');
    await user.click(characterTreeItem);

    // Wait for entity to load in main panel
    await waitFor(() => {
      expect(screen.getByText('A legendary wizard and sage of the Forgotten Realms')).toBeInTheDocument();
    });

    // CRITICAL TEST: Verify Character details appear but NO custom properties section
    await waitFor(() => {
      expect(screen.getByText('Elminster Aumar')).toBeInTheDocument();
      expect(screen.getByText('A legendary wizard and sage of the Forgotten Realms')).toBeInTheDocument();
      
      // Tags should appear
      expect(screen.getByText('wizard')).toBeInTheDocument();
      expect(screen.getByText('sage')).toBeInTheDocument();
      
      // NO custom properties section (fallback renders when properties is null)
      expect(screen.queryByText('Character Properties')).not.toBeInTheDocument();
    });

    // Success!
  }, 30000);
});
