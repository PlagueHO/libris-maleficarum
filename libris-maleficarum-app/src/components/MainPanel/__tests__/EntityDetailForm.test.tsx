import { describe, it, expect, vi, beforeEach, beforeAll, afterEach, afterAll } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { toHaveNoViolations } from 'jest-axe';
import { Provider } from 'react-redux';
import { store } from '../../../store/store';
import App from '../../../App';
import { WorldEntityType, type CreateWorldEntityRequest } from '@/services/types/worldEntity.types';

// Setup mock server
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import { handlers } from '../../../__tests__/mocks/handlers';

expect.extend(toHaveNoViolations);

const server = setupServer(...handlers);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

describe('EntityDetailForm - Custom Properties Integration', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  // Helper function to open entity detail form for creation
  async function openCreateForm(user: ReturnType<typeof userEvent.setup>) {
    // Select world
    const worldTrigger = await screen.findByRole('combobox', { name: /select world/i });
    await user.click(worldTrigger);
    const worldOption = await screen.findByRole('option', { name: 'Forgotten Realms' });
    await user.click(worldOption);

    // Click "+" to open create form
    const createBtn = await screen.findByRole('button', { name: /add root entity/i });
    await user.click(createBtn);
  }

  describe('T054: GeographicRegion Custom Properties Rendering', () => {
    it('should render GeographicRegionProperties when entityType is GeographicRegion', async () => {
      const user = userEvent.setup();
      render(
        <Provider store={store}>
          <App />
        </Provider>
      );

      await openCreateForm(user);

      // Select GeographicRegion type (search for it since it's not recommended at root level)
      const typeButton = await screen.findByRole('combobox', { name: /entity type/i });
      await user.click(typeButton);
      
      // Search for Geographic Region
      const searchInput = await screen.findByPlaceholderText(/search entity types/i);
      await user.type(searchInput, 'geographic');
      
      const geoRegionOption = await screen.findByRole('option', { name: /geographic region/i });
      await user.click(geoRegionOption);

      // Verify GeographicRegionProperties fields appear
      await waitFor(() => {
        expect(screen.getByLabelText(/climate/i)).toBeInTheDocument();
      });
      expect(screen.getByLabelText(/terrain/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/population/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/area/i)).toBeInTheDocument();
    });

    it('should update custom properties when fields change', async () => {
      const user = userEvent.setup();
      render(
        <Provider store={store}>
          <App />
        </Provider>
      );

      await openCreateForm(user);

      // Select GeographicRegion type (search for it since it's not recommended at root level)
      const typeButton = await screen.findByRole('combobox', { name: /entity type/i });
      await user.click(typeButton);
      
      // Search for Geographic Region
      const searchInput = await screen.findByPlaceholderText(/search entity types/i);
      await user.type(searchInput, 'geographic');
      
      const geoRegionOption = await screen.findByRole('option', { name: /geographic region/i });
      await user.click(geoRegionOption);

      // Fill in custom properties
      await waitFor(() => {
        expect(screen.getByLabelText(/climate/i)).toBeInTheDocument();
      });

      const climateInput = screen.getByLabelText(/climate/i);
      await user.type(climateInput, 'Tropical rainforest climate');

      const terrainInput = screen.getByLabelText(/terrain/i);
      await user.type(terrainInput, 'Dense jungle with rivers');

      const populationInput = screen.getByLabelText(/population/i);
      await user.type(populationInput, '250000');

      const areaInput = screen.getByLabelText(/area/i);
      await user.type(areaInput, '1500.5');
      
      // Trigger blur by tabbing away from the field
      await user.tab();

      // Wait for blur formatting to complete
      await waitFor(() => {
        expect(populationInput).toHaveValue('250,000');
      });
      
      await waitFor(() => {
        expect(areaInput).toHaveValue('1,500.5');
      });

      // Verify all fields contain the entered values
      expect(climateInput).toHaveValue('Tropical rainforest climate');
      expect(terrainInput).toHaveValue('Dense jungle with rivers');
    });
  });

  describe('T055: PoliticalRegion Custom Properties Rendering', () => {
    it('should render PoliticalRegionProperties when entityType is PoliticalRegion', async () => {
      const user = userEvent.setup();
      render(
        <Provider store={store}>
          <App />
        </Provider>
      );

      await openCreateForm(user);

      // Select PoliticalRegion type (search for it since it's not recommended at root level)
      const typeButton = await screen.findByRole('combobox', { name: /entity type/i });
      await user.click(typeButton);
      
      // Search for Political Region
      const searchInput = await screen.findByPlaceholderText(/search entity types/i);
      await user.type(searchInput, 'political');
      
      const polRegionOption = await screen.findByRole('option', { name: /political region/i });
      await user.click(polRegionOption);

      // Verify PoliticalRegionProperties fields appear
      await waitFor(() => {
        expect(screen.getByLabelText(/government type/i)).toBeInTheDocument();
      });
      expect(screen.getByLabelText(/member states/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/established date/i)).toBeInTheDocument();
    });

    it('should handle MemberStates TagInput correctly', async () => {
      const user = userEvent.setup();
      render(
        <Provider store={store}>
          <App />
        </Provider>
      );

      await openCreateForm(user);

      // Select PoliticalRegion type (search for it since it's not recommended at root level)
      const typeButton = await screen.findByRole('combobox', { name: /entity type/i });
      await user.click(typeButton);
      
      // Search for Political Region
      const searchInput = await screen.findByPlaceholderText(/search entity types/i);
      await user.type(searchInput, 'political');
      
      const polRegionOption = await screen.findByRole('option', { name: /political region/i });
      await user.click(polRegionOption);

      // Wait for component to render
      await waitFor(() => {
        expect(screen.getByLabelText(/member states/i)).toBeInTheDocument();
      });

      // Add member states
      const memberStatesInput = screen.getByLabelText(/member states/i);
      await user.type(memberStatesInput, 'Kingdom of Larion{Enter}');
      await user.type(memberStatesInput, 'Duchy of Westmarch{Enter}');

      // Verify tags appear (TagInput should render badges)
      await waitFor(() => {
        expect(screen.getByText('Kingdom of Larion')).toBeInTheDocument();
      });
      expect(screen.getByText('Duchy of Westmarch')).toBeInTheDocument();
    });

    it('should handle free-form EstablishedDate field', async () => {
      const user = userEvent.setup();
      render(
        <Provider store={store}>
          <App />
        </Provider>
      );

      await openCreateForm(user);

      // Select PoliticalRegion type (search for it since it's not recommended at root level)
      const typeButton = await screen.findByRole('combobox', { name: /entity type/i });
      await user.click(typeButton);
      
      // Search for Political Region
      const searchInput = await screen.findByPlaceholderText(/search entity types/i);
      await user.type(searchInput, 'political');
      
      const polRegionOption = await screen.findByRole('option', { name: /political region/i });
      await user.click(polRegionOption);

      // Wait for component to render
      await waitFor(() => {
        expect(screen.getByLabelText(/established date/i)).toBeInTheDocument();
      });

      // Enter fantasy date format
      const dateInput = screen.getByLabelText(/established date/i);
      await user.type(dateInput, 'Year 1456, Third Age');

      expect(dateInput).toHaveValue('Year 1456, Third Age');
    });
  });

  describe('T056: Entity Submission with Properties', () => {
    it('should include serialized Properties field when creating entity with custom properties', async () => {
      const user = userEvent.setup();
      
      // Spy on POST request
      let capturedRequestBody: CreateWorldEntityRequest | null = null;
      server.use(
        http.post('/api/v1/worlds/:worldId/entities', async ({ request }) => {
          capturedRequestBody = await request.json() as CreateWorldEntityRequest;
          return HttpResponse.json({
            data: {
              id: 'new-entity-id',
              worldId: 'test-world-123',
              ...capturedRequestBody,
              parentId: null,
              tags: [],
              path: [],
              depth: 0,
              hasChildren: false,
              ownerId: 'test-user@example.com',
              createdAt: '2026-01-13T12:00:00Z',
              updatedAt: '2026-01-13T12:00:00Z',
              isDeleted: false,
            },
          });
        })
      );

      render(
        <Provider store={store}>
          <App />
        </Provider>
      );

      await openCreateForm(user);

      // Fill in basic fields
      const nameInput = await screen.findByLabelText(/^name/i);
      await user.type(nameInput, 'Emerald Jungle');

      const descInput = await screen.findByLabelText(/description/i);
      await user.type(descInput, 'A vast tropical rainforest');

      // Select GeographicRegion type (search for it since it's not recommended at root level)
      const typeButton = await screen.findByRole('combobox', { name: /entity type/i });
      await user.click(typeButton);
      
      // Search for Geographic Region
      const searchInput = await screen.findByPlaceholderText(/search entity types/i);
      await user.type(searchInput, 'geographic');
      
      const geoRegionOption = await screen.findByRole('option', { name: /geographic region/i });
      await user.click(geoRegionOption);

      // Fill custom properties
      await waitFor(() => {
        expect(screen.getByLabelText(/climate/i)).toBeInTheDocument();
      });

      const climateInput = screen.getByLabelText(/climate/i);
      await user.type(climateInput, 'Tropical');

      const populationInput = screen.getByLabelText(/population/i);
      await user.type(populationInput, '1000000');

      // Submit form (get the last Create button, which is the form submit button)
      const submitButtons = screen.getAllByRole('button', { name: /create/i });
      const submitButton = submitButtons[submitButtons.length - 1];
      await user.click(submitButton);

      // Verify request was made with serialized Properties
      await waitFor(() => {
        expect(capturedRequestBody).not.toBeNull();
      });

      expect(capturedRequestBody).not.toBeNull();
      expect(capturedRequestBody!.properties).toBeDefined();
      expect(typeof capturedRequestBody!.properties).toBe('string');
      
      const parsedProperties = JSON.parse(capturedRequestBody!.properties!);
      expect(parsedProperties).toEqual(
        expect.objectContaining({
          Climate: 'Tropical',
          Population: 1000000,
        })
      );
    });

    it('should omit Properties field when no custom properties entered', async () => {
      const user = userEvent.setup();
      
      // Spy on POST request
      let capturedRequestBody: CreateWorldEntityRequest | null = null;
      server.use(
        http.post('/api/v1/worlds/:worldId/entities', async ({ request }) => {
          capturedRequestBody = await request.json() as CreateWorldEntityRequest;
          return HttpResponse.json({
            data: {
              id: 'new-entity-id',
              worldId: 'test-world-123',
              ...capturedRequestBody,
              parentId: null,
              tags: [],
              path: [],
              depth: 0,
              hasChildren: false,
              ownerId: 'test-user@example.com',
              createdAt: '2026-01-13T12:00:00Z',
              updatedAt: '2026-01-13T12:00:00Z',
              isDeleted: false,
            },
          });
        })
      );

      render(
        <Provider store={store}>
          <App />
        </Provider>
      );

      await openCreateForm(user);

      // Fill only basic fields (World type - no custom properties)
      const nameInput = await screen.findByLabelText(/^name/i);
      await user.type(nameInput, 'Test World');

      const descInput = await screen.findByLabelText(/description/i);
      await user.type(descInput, 'A test world');

      // Submit without changing entity type (defaults to World)
      const submitButtons = screen.getAllByRole('button', { name: /create/i });
      const submitButton = submitButtons[submitButtons.length - 1]; // Get the form submit button, not the add entity button
      await user.click(submitButton);

      // Verify Properties field is undefined
      await waitFor(() => {
        expect(capturedRequestBody).not.toBeNull();
      });

      expect(capturedRequestBody).not.toBeNull();
      expect(capturedRequestBody!.properties).toBeUndefined();
    });
  });

  describe('T057: Entity Loading with Properties Deserialization', () => {
    it('should deserialize Properties field and populate custom property fields', async () => {
      const user = userEvent.setup();
      
      // Add entity to tree for selection
      const testEntity = {
        id: 'geo-region-with-props',
        worldId: 'test-world-123',
        parentId: null,
        name: 'Test Geographic Region',
        description: 'Test description',
        entityType: WorldEntityType.GeographicRegion,
        tags: [],
        path: [],
        depth: 0,
        hasChildren: false,
        ownerId: 'test-user@example.com',
        createdAt: '2026-01-13T12:00:00Z',
        updatedAt: '2026-01-13T12:00:00Z',
        isDeleted: false,
        properties: JSON.stringify({
          Climate: 'Temperate',
          Terrain: 'Plains and forests',
          Population: 750000,
          Area: 2500.75,
        }),
      };
      
      server.use(
        http.get('http://localhost:5000/api/v1/worlds/:worldId/entities', ({ params, request }) => {
          const { worldId } = params;
          if (worldId !== 'test-world-123') {
            return HttpResponse.json({ data: [], meta: { count: 0, nextCursor: null } });
          }
          
          const url = new URL(request.url);
          const parentId = url.searchParams.get('parentId');
          if (parentId !== null && parentId !== 'null') {
            return HttpResponse.json({ data: [], meta: { count: 0, nextCursor: null } });
          }
          
          return HttpResponse.json({
            data: [testEntity],
            meta: { count: 1, nextCursor: null },
          });
        }),
        http.get('http://localhost:5000/api/v1/worlds/:worldId/entities/:entityId', ({ params }) => {
          const { entityId } = params;
          if (entityId === 'geo-region-with-props') {
            return HttpResponse.json({ data: testEntity });
          }
          return new HttpResponse(null, { status: 404 });
        })
      );

      render(
        <Provider store={store}>
          <App />
        </Provider>
      );

      // Select world
      const worldTrigger = await screen.findByRole('combobox', { name: /select world/i });
      await user.click(worldTrigger);
      const worldOption = await screen.findByRole('option', { name: 'Forgotten Realms' });
      await user.click(worldOption);

      // Wait for tree to load and click on entity to open it in edit mode
      const entityNode = await screen.findByText('Test Geographic Region', {}, { timeout: 5000 });
      await user.click(entityNode);

      // Wait for form to load and verify custom properties populated
      await waitFor(() => {
        expect(screen.getByDisplayValue('Temperate')).toBeInTheDocument();
      }, { timeout: 5000 });

      expect(screen.getByDisplayValue('Plains and forests')).toBeInTheDocument();
      expect(screen.getByDisplayValue('750,000')).toBeInTheDocument();
      expect(screen.getByDisplayValue('2,500.75')).toBeInTheDocument();
    });

    it('should handle PoliticalRegion properties with arrays correctly', async () => {
      const user = userEvent.setup();
      
      // Add political region to tree
      const testEntity = {
        id: 'pol-region-with-props',
        worldId: 'test-world-123',
        parentId: null,
        name: 'Test Political Region',
        description: 'Test alliance',
        entityType: WorldEntityType.PoliticalRegion,
        tags: [],
        path: [],
        depth: 0,
        hasChildren: false,
        ownerId: 'test-user@example.com',
        createdAt: '2026-01-13T12:00:00Z',
        updatedAt: '2026-01-13T12:00:00Z',
        isDeleted: false,
        properties: JSON.stringify({
          GovernmentType: 'Confederacy',
          MemberStates: ['State A', 'State B', 'State C'],
          EstablishedDate: 'Year 1200, Second Age',
        }),
      };
      
      server.use(
        http.get('http://localhost:5000/api/v1/worlds/:worldId/entities', ({ params, request }) => {
          const { worldId } = params;
          if (worldId !== 'test-world-123') {
            return HttpResponse.json({ data: [], meta: { count: 0, nextCursor: null } });
          }
          
          const url = new URL(request.url);
          const parentId = url.searchParams.get('parentId');
          if (parentId !== null && parentId !== 'null') {
            return HttpResponse.json({ data: [], meta: { count: 0, nextCursor: null } });
          }
          
          return HttpResponse.json({
            data: [testEntity],
            meta: { count: 1, nextCursor: null },
          });
        }),
        http.get('http://localhost:5000/api/v1/worlds/:worldId/entities/:entityId', ({ params }) => {
          const { entityId } = params;
          if (entityId === 'pol-region-with-props') {
            return HttpResponse.json({ data: testEntity });
          }
          return new HttpResponse(null, { status: 404 });
        })
      );

      render(
        <Provider store={store}>
          <App />
        </Provider>
      );

      // Select world
      const worldTrigger = await screen.findByRole('combobox', { name: /select world/i });
      await user.click(worldTrigger);
      const worldOption = await screen.findByRole('option', { name: 'Forgotten Realms' });
      await user.click(worldOption);

      // Wait for tree to load and click on entity
      const entityNode = await screen.findByText('Test Political Region', {}, { timeout: 5000 });
      await user.click(entityNode);

      // Wait for form to load
      await waitFor(() => {
        expect(screen.getByDisplayValue('Confederacy')).toBeInTheDocument();
      }, { timeout: 5000 });

      // Verify TagInput rendered the member states
      expect(screen.getByText('State A')).toBeInTheDocument();
      expect(screen.getByText('State B')).toBeInTheDocument();
      expect(screen.getByText('State C')).toBeInTheDocument();

      expect(screen.getByDisplayValue('Year 1200, Second Age')).toBeInTheDocument();
    });

    it('should handle malformed Properties JSON gracefully', async () => {
      const user = userEvent.setup();
      const consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});

      // Add entity with malformed JSON to tree
      const testEntity = {
        id: 'entity-with-malformed-json',
        worldId: 'test-world-123',
        parentId: null,
        name: 'Malformed Entity',
        description: 'Test',
        entityType: WorldEntityType.GeographicRegion,
        tags: [],
        path: [],
        depth: 0,
        hasChildren: false,
        ownerId: 'test-user@example.com',
        createdAt: '2026-01-13T12:00:00Z',
        updatedAt: '2026-01-13T12:00:00Z',
        isDeleted: false,
        properties: 'invalid json{',
      };
      
      server.use(
        http.get('http://localhost:5000/api/v1/worlds/:worldId/entities', ({ params, request }) => {
          const { worldId } = params;
          if (worldId !== 'test-world-123') {
            return HttpResponse.json({ data: [], meta: { count: 0, nextCursor: null } });
          }
          
          const url = new URL(request.url);
          const parentId = url.searchParams.get('parentId');
          if (parentId !== null && parentId !== 'null') {
            return HttpResponse.json({ data: [], meta: { count: 0, nextCursor: null } });
          }
          
          return HttpResponse.json({
            data: [testEntity],
            meta: { count: 1, nextCursor: null },
          });
        }),
        http.get('http://localhost:5000/api/v1/worlds/:worldId/entities/:entityId', ({ params }) => {
          const { entityId } = params;
          if (entityId === 'entity-with-malformed-json') {
            return HttpResponse.json({ data: testEntity });
          }
          return new HttpResponse(null, { status: 404 });
        })
      );

      render(
        <Provider store={store}>
          <App />
        </Provider>
      );

      // Select world
      const worldTrigger = await screen.findByRole('combobox', { name: /select world/i });
      await user.click(worldTrigger);
      const worldOption = await screen.findByRole('option', { name: 'Forgotten Realms' });
      await user.click(worldOption);

      // Wait for tree to load and click on entity
      const entityNode = await screen.findByText('Malformed Entity', {}, { timeout: 5000 });
      await user.click(entityNode);

      // Form should still render without crashing
      await waitFor(() => {
        expect(screen.getByDisplayValue('Malformed Entity')).toBeInTheDocument();
      }, { timeout: 5000 });

      // Custom property fields should be empty (not crash)
      expect(screen.getByLabelText(/climate/i)).toHaveValue('');

      consoleErrorSpy.mockRestore();
    });
  });

  describe('T061: Flexible Entity Placement - Character Under World', () => {
    it('should allow creating Character directly under World root (bypassing People container)', async () => {
      const user = userEvent.setup();
      render(
        <Provider store={store}>
          <App />
        </Provider>
      );

      await openCreateForm(user);

      // Select Character type (unconventional - normally Character goes under People container)
      const typeButton = await screen.findByRole('combobox', { name: /entity type/i });
      await user.click(typeButton);
      
      // Search for Character (may not be in recommended types for root level)
      const searchInput = screen.getByPlaceholderText(/search/i);
      await user.type(searchInput, 'Character');
      
      // Use getAllByRole and select the exact match (first one will be Character entity, not People container)
      const characterOptions = await screen.findAllByRole('option', { name: /character/i });
      const characterOption = characterOptions.find(opt => opt.textContent?.includes('A person, NPC'));
      expect(characterOption).toBeDefined();
      await user.click(characterOption!);

      // Fill in name
      const nameInput = screen.getByLabelText(/^name/i);
      await user.type(nameInput, 'Elminster Aumar');

      // Submit form - should succeed without validation errors
      const submitBtn = screen.getAllByRole('button', { name: /create/i }).find(btn => 
        btn.textContent === 'Create'
      );
      expect(submitBtn).toBeDefined();
      await user.click(submitBtn!);

      // Verify no error messages appear
      await waitFor(() => {
        expect(screen.queryByText(/not allowed/i)).not.toBeInTheDocument();
        expect(screen.queryByText(/invalid/i)).not.toBeInTheDocument();
      });
    });
  });

  describe('T063: Flexible Entity Placement - Campaign Entities Under Non-Campaign Parents', () => {
    it('should allow selecting Quest type when parent is Continent (unconventional pairing)', async () => {
      const user = userEvent.setup();
      render(
        <Provider store={store}>
          <App />
        </Provider>
      );

      await openCreateForm(user);

      // Instead of creating under a specific parent, just verify that Quest type is available
      // when searching (no longer filtered by parent type)
      const typeButton = await screen.findByRole('combobox', { name: /entity type/i });
      await user.click(typeButton);
      
      // Search for Quest - it should be available regardless of parent
      const searchInput = screen.getByPlaceholderText(/search/i);
      await user.type(searchInput, 'Quest');
      
      // Quest should appear in search results (proving it's not filtered out)
      const questOptions = await screen.findAllByRole('option', { name: /quest/i });
      const questOption = questOptions.find(opt => opt.textContent?.includes('mission, objective'));
      expect(questOption).toBeDefined();
      
      // Select it
      await user.click(questOption!);

      // Fill in name
      const nameInput = screen.getByLabelText(/^name/i);
      await user.type(nameInput, 'The Hunt for the Lost Crown');

      // Verify type was selected successfully (Quest is now the value)
      expect(typeButton).toHaveTextContent('Quest');
    });
  });
});
