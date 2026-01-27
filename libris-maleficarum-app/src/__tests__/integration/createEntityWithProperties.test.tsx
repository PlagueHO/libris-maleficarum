/**
 * Integration test for creating entities with schema-driven custom properties
 *
 * Tests the complete flow of creating a new MilitaryRegion entity including:
 * - Selecting entity type that has propertySchema
 * - DynamicPropertiesForm rendering with empty fields
 * - Filling in custom properties
 * - Submitting new entity with properties
 *
 * @module __tests__/integration/createEntityWithProperties
 */

import { describe, it, expect, beforeAll, afterEach, afterAll } from 'vitest';
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
  WorldEntityResponse,
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

// Track created entities for verification
let createdEntity: WorldEntity | null = null;

// Use shared handlers from mocks directory
const server = setupServer(...handlers);

beforeAll(() => {
  server.listen();
});

afterEach(() => {
  server.resetHandlers();
  createdEntity = null; // Reset between tests
});

afterAll(() => {
  server.close();
});

describe('T039: Integration - Create MilitaryRegion Entity with Custom Properties', () => {
  it('should create a new MilitaryRegion entity with custom properties', async () => {
    // Mock Faerûn entity for fetching by ID (needed when opening create form)
    const faerunEntity: WorldEntity = {
      id: 'continent-faerun',
      worldId: 'test-world-123',
      parentId: null,
      entityType: 'GeographicRegion',
      name: 'Faerûn',
      description: 'The primary continent',
      tags: [],
      properties: {},
      path: [],
      depth: 0,
      hasChildren: true,
      ownerId: 'test-user@example.com',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
      schemaVersion: 1,
    };

    // Override handlers for this test
    server.use(
      // GET entity by ID (for parent entity lookup)
      http.get(`${baseUrl}/api/v1/worlds/:worldId/entities/:entityId`, ({ params }) => {
        const { entityId } = params;
        if (entityId === 'continent-faerun') {
          return HttpResponse.json({ data: faerunEntity });
        }
        return new HttpResponse(null, { status: 404 });
      }),
      // POST to capture created entity
      http.post(`${baseUrl}/api/v1/worlds/:worldId/entities`, async ({ request, params }) => {
        const { worldId } = params as { worldId: string };
        const body = (await request.json()) as CreateWorldEntityRequest;

        const newEntity: WorldEntity = {
          id: `entity-${Date.now()}`,
          worldId,
          parentId: body.parentId || null,
          entityType: body.entityType,
          name: body.name,
          description: body.description || '',
          tags: body.tags || [],
          properties: body.properties,
          path: body.parentId ? ['continent-faerun'] : [],
          depth: body.parentId ? 1 : 0,
          hasChildren: false,
          ownerId: 'test-user@example.com',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          isDeleted: false,
          schemaVersion: 1,
        };

        createdEntity = newEntity; // Store for verification

        const response: WorldEntityResponse = {
          data: newEntity,
        };

        return HttpResponse.json(response, { status: 201 });
      })
    );

    render(
      <Provider store={store}>
        <App />
      </Provider>
    );

    const user = userEvent.setup();

    // 1. Select world
    const worldTrigger = await screen.findByRole('combobox', { name: /select world/i });
    await user.click(worldTrigger);

    const worldOption = await screen.findByRole('option', { name: 'Forgotten Realms' });
    await user.click(worldOption);

    // Wait for hierarchy to load and continent to appear
    await waitFor(() => {
      expect(screen.getByText('Faerûn')).toBeInTheDocument();
    });

    // 2. Select the continent to enable child creation
    // Use getAllByText and take first to avoid multiple matches
    const continentNodes = screen.getAllByText('Faerûn');
    await user.click(continentNodes[0]);

    // 3. Click "Add child to Faerûn" button
    const addChildBtn = await screen.findByRole('button', { name: /add child to faerûn/i });
    await user.click(addChildBtn);
    // Wait for form to load
    await waitFor(
      () => {
        expect(screen.getByText(/create entity/i)).toBeInTheDocument();
      },
      { timeout: 3000 }
    );
    // 4. Fill in entity details
    const nameInput = await screen.findByPlaceholderText(/entity name/i);
    await user.type(nameInput, 'Northern Defense Zone');

    const typeSelect = screen.getByRole('combobox', { name: /type/i });
    await user.click(typeSelect);

    // Wait for options to render with extended timeout
    const militaryRegionOption = await waitFor(
      () => screen.getByRole('option', { name: /military region/i }),
      { timeout: 5000 }
    );
    await user.click(militaryRegionOption);

    const descInput = screen.getByPlaceholderText(/brief description/i);
    await user.type(descInput, 'Strategic military region in the north');

    // 5. Fill in custom properties
    // CommandStructure (text field)
    const commandStructureInput = await screen.findByLabelText(/command structure/i);
    await user.type(commandStructureInput, 'General Blackthorn, 5th Legion');

    // StrategicImportance (integer field)
    const strategicImportanceInput = await screen.findByLabelText(/strategic importance/i);
    await user.clear(strategicImportanceInput);
    await user.type(strategicImportanceInput, '85');

    // MilitaryAssets (tagArray field) - add tags with proper delays for DOM updates
    const militaryAssetsInput = await screen.findByLabelText(/military assets/i);
    await user.type(militaryAssetsInput, 'Fortress', { delay: 50 });
    await user.keyboard('{Enter}');
    
    await user.type(militaryAssetsInput, 'Cavalry', { delay: 50 });
    await user.keyboard('{Enter}');
    
    await user.type(militaryAssetsInput, 'Archers', { delay: 50 });
    await user.keyboard('{Enter}');
    
    // Wait for all tags to render (check that tags appear as text in the DOM)
    await waitFor(
      () => {
        expect(screen.getByText('Fortress')).toBeInTheDocument();
        expect(screen.getByText('Cavalry')).toBeInTheDocument();
        expect(screen.getByText('Archers')).toBeInTheDocument();
      },
      { timeout: 3000 }
    );

    // 6. Submit the form
    const submitBtn = await screen.findByRole('button', { name: /^create$/i });
    await user.click(submitBtn);

    // 7. Verify entity was created with correct properties
    await waitFor(() => {
      expect(createdEntity).not.toBeNull();
      expect(createdEntity?.name).toBe('Northern Defense Zone');
      expect(createdEntity?.properties).toBeDefined();

      // Parse and verify properties
      const props = JSON.parse(createdEntity?.properties || '{}');
      expect(props.CommandStructure).toBe('General Blackthorn, 5th Legion');
      expect(props.StrategicImportance).toBe('85'); // Textarea returns string
      expect(props.MilitaryAssets).toEqual(['Fortress', 'Cavalry', 'Archers']);
    });
  });

  it('should handle creating MilitaryRegion with minimal properties', async () => {
    // Mock Faerûn entity for fetching by ID
    const faerunEntity: WorldEntity = {
      id: 'continent-faerun',
      worldId: 'test-world-123',
      parentId: null,
      entityType: 'GeographicRegion',
      name: 'Faerûn',
      description: 'The primary continent',
      tags: [],
      properties: {},
      path: [],
      depth: 0,
      hasChildren: true,
      ownerId: 'test-user@example.com',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
      schemaVersion: 1,
    };

    // Override handlers for this test
    server.use(
      http.get(`${baseUrl}/api/v1/worlds/:worldId/entities/:entityId`, ({ params }) => {
        const { entityId } = params;
        if (entityId === 'continent-faerun') {
          return HttpResponse.json({ data: faerunEntity });
        }
        return new HttpResponse(null, { status: 404 });
      }),
      http.post(`${baseUrl}/api/v1/worlds/:worldId/entities`, async ({ request, params }) => {
        const { worldId } = params as { worldId: string };
        const body = (await request.json()) as CreateWorldEntityRequest;

        const newEntity: WorldEntity = {
          id: `entity-${Date.now()}`,
          worldId,
          parentId: body.parentId || null,
          entityType: body.entityType,
          name: body.name,
          description: body.description || '',
          tags: body.tags || [],
          properties: body.properties,
          path: body.parentId ? ['continent-faerun'] : [],
          depth: body.parentId ? 1 : 0,
          hasChildren: false,
          ownerId: 'test-user@example.com',
          createdAt: new Date().toISOString(),
          updatedAt: new Date().toISOString(),
          isDeleted: false,
          schemaVersion: 1,
        };

        createdEntity = newEntity; // Store for verification

        const response: WorldEntityResponse = {
          data: newEntity,
        };

        return HttpResponse.json(response, { status: 201 });
      })
    );

    render(
      <Provider store={store}>
        <App />
      </Provider>
    );

    const user = userEvent.setup();

    // 1. Select world
    const worldTrigger = await screen.findByRole('combobox', { name: /select world/i });
    await user.click(worldTrigger);

    const worldOption = await screen.findByRole('option', { name: 'Forgotten Realms' });
    await user.click(worldOption);

    await waitFor(() => {
      expect(screen.getAllByText('Faerûn').length).toBeGreaterThan(0);
    });

    // 2. Select the continent
    const continentNodes = screen.getAllByText('Faerûn');
    await user.click(continentNodes[0]);

    // 3. Click "Add Child Entity"
    const addChildBtn = await screen.findByRole('button', { name: /add child to faerûn/i });
    await user.click(addChildBtn);

    // Wait for form to load
    await waitFor(
      () => {
        expect(screen.getByText(/create entity/i)).toBeInTheDocument();
      },
      { timeout: 3000 }
    );

    // 4. Fill in only required fields (name and type)
    const nameInput = screen.getByPlaceholderText(/entity name/i);
    await user.type(nameInput, 'Minimal Military Region');

    const typeSelect = screen.getByRole('combobox', { name: /type/i });
    await user.click(typeSelect);

    // Wait for options to render with extended timeout
    const militaryRegionOption = await waitFor(
      () => screen.getByRole('option', { name: /military region/i }),
      { timeout: 5000 }
    );
    await user.click(militaryRegionOption);

    // Leave custom property fields empty

    // 5. Submit
    const submitBtn = await screen.findByRole('button', { name: /^create$/i });
    await user.click(submitBtn);

    // 6. Verify entity was created with empty/undefined properties
    await waitFor(() => {
      expect(createdEntity).not.toBeNull();
      expect(createdEntity?.name).toBe('Minimal Military Region');

      // properties should either be undefined or an empty JSON object
      if (createdEntity?.properties) {
        const props = JSON.parse(createdEntity.properties);
        // All property values should be undefined/empty
        expect(Object.keys(props).length).toBe(0);
      }
    });
  });
});

describe('T040: DynamicPropertiesForm - Empty Fields for New Entities', () => {
  it('should display all schema fields as empty when creating new entity', async () => {
    // Mock Faerûn entity for fetching by ID
    const faerunEntity: WorldEntity = {
      id: 'continent-faerun',
      worldId: 'test-world-123',
      parentId: null,
      entityType: 'GeographicRegion',
      name: 'Faerûn',
      description: 'The primary continent',
      tags: [],
      properties: {},
      path: [],
      depth: 0,
      hasChildren: true,
      ownerId: 'test-user@example.com',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
      schemaVersion: 1,
    };

    server.use(
      http.get(`${baseUrl}/api/v1/worlds/:worldId/entities/:entityId`, ({ params }) => {
        const { entityId } = params;
        if (entityId === 'continent-faerun') {
          return HttpResponse.json({ data: faerunEntity });
        }
        return new HttpResponse(null, { status: 404 });
      })
    );

    render(
      <Provider store={store}>
        <App />
      </Provider>
    );

    const user = userEvent.setup();

    // 1. Select world
    const worldTrigger = await screen.findByRole('combobox', { name: /select world/i });
    await user.click(worldTrigger);

    const worldOption = await screen.findByRole('option', { name: 'Forgotten Realms' });
    await user.click(worldOption);

    await waitFor(() => {
      expect(screen.getAllByText('Faerûn').length).toBeGreaterThan(0);
    });

    // 2. Select the continent
    const continentNodes = screen.getAllByText('Faerûn');
    await user.click(continentNodes[0]);

    // 3. Open create entity dialog
    const addChildBtn = await screen.findByRole('button', { name: /add child to faerûn/i });
    await user.click(addChildBtn);

    // Wait for form to load
    await waitFor(
      () => {
        expect(screen.getByText(/create entity/i)).toBeInTheDocument();
      },
      { timeout: 3000 }
    );

    // 4. Select MilitaryRegion type
    const typeSelect = screen.getByRole('combobox', { name: /type/i });
    await user.click(typeSelect);

    const militaryRegionOption = await screen.findByRole('option', { name: /military region/i });
    await user.click(militaryRegionOption);

    // 5. Verify all 3 schema fields are rendered and empty
    // CommandStructure (text field)
    const commandStructureInput = await screen.findByLabelText(/command structure/i);
    expect(commandStructureInput).toHaveValue('');

    // StrategicImportance (integer field) - should start empty or with placeholder
    const strategicImportanceInput = await screen.findByLabelText(/strategic importance/i);
    expect(strategicImportanceInput).toHaveValue('');

    // MilitaryAssets (tagArray field) - should have no tags
    const militaryAssetsInput = await screen.findByLabelText(/military assets/i);
    expect(militaryAssetsInput).toHaveValue('');

    // Verify no pre-populated values
    const badges = screen.queryAllByRole('button', { name: /remove/i });
    expect(badges.length).toBe(0); // No tags should be present
  });
});
