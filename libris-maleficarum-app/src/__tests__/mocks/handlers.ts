/**
 * Mock Service Worker (MSW) Handlers
 *
 * HTTP request handlers for testing World and WorldEntity API endpoints.
 * Used by Vitest integration tests to mock backend responses.
 *
 * @see https://mswjs.io/docs/basics/request-handler
 */

import { http, HttpResponse } from 'msw';
import type {
  WorldEntity,
  WorldEntityListResponse,
  WorldEntityResponse,
  CreateWorldEntityRequest,
  UpdateWorldEntityRequest,
  MoveWorldEntityRequest,
} from '@/services/types/worldEntity.types';
import { WorldEntityType } from '@/services/types/worldEntity.types';
import type {
  World,
  WorldListResponse,
  WorldResponse,
  CreateWorldRequest,
  UpdateWorldRequest,
} from '@/services/types/world.types';

/**
 * In-memory mock database for World items
 */
const mockWorlds: Map<string, World> = new Map();

/**
 * In-memory mock database for WorldEntity items
 */
const mockEntities: Map<string, WorldEntity> = new Map();

/**
 * Seed initial mock data
 */
function seedMockData(): void {
  // Seed World data
  mockWorlds.set('test-world-123', {
    id: 'test-world-123',
    name: 'Forgotten Realms',
    description: 'A high fantasy world',
    ownerId: 'test-user@example.com',
    createdAt: '2026-01-13T11:00:00Z',
    updatedAt: '2026-01-13T11:00:00Z',
    isDeleted: false,
  });

  const worldId = 'test-world-123';

  // Root entities (Continent)
  const continentId = 'continent-faerun';
  mockEntities.set(continentId, {
    id: continentId,
    worldId,
    parentId: null,
    entityType: WorldEntityType.Continent,
    name: 'Faerûn',
    description: 'A continent in the world of Toril',
    tags: ['forgotten-realms', 'primary-setting'],
    path: [],
    depth: 0,
    hasChildren: true,
    ownerId: 'test-user@example.com',
    createdAt: '2026-01-13T12:00:00Z',
    updatedAt: '2026-01-13T12:00:00Z',
    isDeleted: false,
  });

  // Country under Continent
  const countryId = 'country-cormyr';
  mockEntities.set(countryId, {
    id: countryId,
    worldId,
    parentId: continentId,
    entityType: WorldEntityType.Country,
    name: 'Cormyr',
    description: 'A kingdom in the Heartlands',
    tags: ['kingdom', 'purple-dragon'],
    path: [continentId],
    depth: 1,
    hasChildren: true,
    ownerId: 'test-user@example.com',
    createdAt: '2026-01-13T12:01:00Z',
    updatedAt: '2026-01-13T12:01:00Z',
    isDeleted: false,
  });

  // City under Country
  const cityId = 'city-suzail';
  mockEntities.set(cityId, {
    id: cityId,
    worldId,
    parentId: countryId,
    entityType: WorldEntityType.City,
    name: 'Suzail',
    description: 'Capital of Cormyr',
    tags: ['capital', 'port-city'],
    path: [continentId, countryId],
    depth: 2,
    hasChildren: false,
    ownerId: 'test-user@example.com',
    createdAt: '2026-01-13T12:02:00Z',
    updatedAt: '2026-01-13T12:02:00Z',
    isDeleted: false,
  });

  // Character entity
  const characterId = 'character-elminster';
  mockEntities.set(characterId, {
    id: characterId,
    worldId,
    parentId: cityId,
    entityType: WorldEntityType.Character,
    name: 'Elminster Aumar',
    description: 'Legendary wizard of the Realms',
    tags: ['wizard', 'chosen-of-mystra', 'npc'],
    path: [continentId, countryId, cityId],
    depth: 3,
    hasChildren: false,
    ownerId: 'test-user@example.com',
    createdAt: '2026-01-13T12:03:00Z',
    updatedAt: '2026-01-13T12:03:00Z',
    isDeleted: false,
  });

  // Seed second world for world-switching tests
  // Note: This world is NOT added to mockWorlds Map, so it won't appear in GET /api/worlds
  // It's only accessible via GET /api/worlds/world-2 and entity endpoints
  const world2Id = 'world-2';

  // Root entity for world-2
  const world2ContinentId = 'world-2-continent';
  mockEntities.set(world2ContinentId, {
    id: world2ContinentId,
    worldId: world2Id,
    parentId: null,
    entityType: WorldEntityType.Continent,
    name: 'World 2 Continent',
    description: 'A continent in World 2',
    tags: ['test'],
    path: [],
    depth: 0,
    hasChildren: false,
    ownerId: 'test-user@example.com',
    createdAt: '2026-01-13T13:01:00Z',
    updatedAt: '2026-01-13T13:01:00Z',
    isDeleted: false,
  });
}

// Initialize mock data
seedMockData();

/**
 * MSW HTTP handlers for World API
 */
export const worldHandlers = [
  /**
   * GET /api/v1/worlds
   *
   * Fetch list of worlds for authenticated user
   */
  http.get('http://localhost:5000/api/v1/worlds', () => {
    const worlds = Array.from(mockWorlds.values()).filter(w => !w.isDeleted);
    const response: WorldListResponse = {
      data: worlds,
      meta: { requestId: 'mock-1', timestamp: new Date().toISOString() },
    };
    return HttpResponse.json(response);
  }),

  /**
   * GET /api/v1/worlds/world-2
   *
   * Special handler for world-2 (used in world-switching tests)
   * This world is not in the main list but can be accessed directly
   */
  http.get('http://localhost:5000/api/v1/worlds/world-2', () => {
    const world2: World = {
      id: 'world-2',
      name: 'World 2',
      description: 'Second test world for switching scenarios',
      ownerId: 'test-user@example.com',
      createdAt: '2026-01-13T13:00:00Z',
      updatedAt: '2026-01-13T13:00:00Z',
      isDeleted: false,
    };
    const response: WorldResponse = {
      data: world2,
      meta: { requestId: 'mock-world-2', timestamp: new Date().toISOString() },
    };
    return HttpResponse.json(response);
  }),

  /**
   * GET /api/v1/worlds/:id
   *
   * Fetch a single world by ID
   */
  http.get('http://localhost:5000/api/v1/worlds/:id', ({ params }) => {
    const { id } = params;
    const world = mockWorlds.get(id as string);

    if (!world || world.isDeleted) {
      return new HttpResponse(null, {
        status: 404,
        statusText: 'World not found',
      });
    }

    const response: WorldResponse = {
      data: world,
      meta: { requestId: 'mock-world-get', timestamp: new Date().toISOString() },
    };
    return HttpResponse.json(response);
  }),

  /**
   * POST /api/v1/worlds
   *
   * Create a new world
   */
  http.post('http://localhost:5000/api/v1/worlds', async ({ request }) => {
    const body = (await request.json()) as CreateWorldRequest;

    const newWorldId = `world-${Date.now()}`;
    const newWorld: World = {
      id: newWorldId,
      name: body.name,
      description: body.description,
      ownerId: 'test-user@example.com',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
    };

    mockWorlds.set(newWorldId, newWorld);

    const response: WorldResponse = {
      data: newWorld,
      meta: { requestId: 'mock-3', timestamp: new Date().toISOString() },
    };
    return HttpResponse.json(response, { status: 201 });
  }),

  /**
   * PUT /api/v1/worlds/:id
   *
   * Update an existing world
   */
  http.put('http://localhost:5000/api/v1/worlds/:id', async ({ params, request }) => {
    const { id } = params;
    const body = (await request.json()) as UpdateWorldRequest;
    const world = mockWorlds.get(id as string);

    if (!world || world.isDeleted) {
      return new HttpResponse(null, {
        status: 404,
        statusText: 'World not found',
      });
    }

    if (body.name !== undefined) world.name = body.name;
    if (body.description !== undefined) world.description = body.description;
    world.updatedAt = new Date().toISOString();

    const response: WorldResponse = {
      data: world,
      meta: { requestId: 'mock-4', timestamp: new Date().toISOString() },
    };
    return HttpResponse.json(response);
  }),

  /**
   * DELETE /api/v1/worlds/:id
   *
   * Soft delete a world
   */
  http.delete('http://localhost:5000/api/v1/worlds/:id', ({ params }) => {
    const { id } = params;
    const world = mockWorlds.get(id as string);

    if (!world) {
      return new HttpResponse(null, {
        status: 404,
        statusText: 'World not found',
      });
    }

    world.isDeleted = true;
    world.updatedAt = new Date().toISOString();

    return new HttpResponse(null, { status: 204 });
  }),
];

/**
 * MSW HTTP handlers for WorldEntity API
 */
export const worldEntityHandlers = [
  /**
   * GET /api/v1/worlds/world-error/entities - Test error handling
   */
  http.get('http://localhost:5000/api/v1/worlds/world-error/entities', () => {
    return new HttpResponse(null, { status: 500 });
  }),

  /**
   * GET /api/v1/worlds/world-network-error/entities - Test network error handling
   */
  http.get('http://localhost:5000/api/v1/worlds/world-network-error/entities', () => {
    return new HttpResponse(null, { status: 500 });
  }),

  /**
   * GET /api/v1/worlds/:worldId/entities
   *
   * Fetch list of entities with optional filters
   */
  http.get('http://localhost:5000/api/v1/worlds/:worldId/entities', ({ params, request }) => {
    const { worldId } = params;
    const url = new URL(request.url);

    const parentId = url.searchParams.get('parentId');
    const entityType = url.searchParams.get('entityType') as WorldEntityType | null;
    const page = parseInt(url.searchParams.get('page') ?? '1', 10);
    const pageSize = parseInt(url.searchParams.get('pageSize') ?? '50', 10);
    const includeDeleted = url.searchParams.get('includeDeleted') === 'true';

    console.log('[MSW GET /entities] Request:', {
      worldId,
      parentId,
      entityType,
      mockEntitiesCount: mockEntities.size,
      mockEntitiesWorldIds: Array.from(mockEntities.values())
        .map((e) => e.worldId)
        .filter((id) => id === worldId),
    });

    // Filter entities by worldId
    let filteredEntities = Array.from(mockEntities.values()).filter(
      (entity) => entity.worldId === worldId,
    );

    console.log('[MSW GET /entities] After worldId filter:', filteredEntities.length);

    // Filter by parentId (support null for root entities)
    // If parentId query param not provided, return only root entities
    if (parentId === null) {
      // No parentId query param - return root entities only
      filteredEntities = filteredEntities.filter((entity) => entity.parentId === null);
    } else if (parentId === 'null') {
      // Explicit 'null' string - return root entities
      filteredEntities = filteredEntities.filter((entity) => entity.parentId === null);
    } else {
      // Specific parentId - filter by it
      filteredEntities = filteredEntities.filter((entity) => entity.parentId === parentId);
    }

    console.log('[MSW GET /entities] After parentId filter:', {
      filteredCount: filteredEntities.length,
      parentIdParam: parentId,
    });

    // Filter by entityType
    if (entityType) {
      filteredEntities = filteredEntities.filter(
        (entity) => entity.entityType === entityType,
      );
    }

    // Filter out soft-deleted entities unless explicitly requested
    if (!includeDeleted) {
      filteredEntities = filteredEntities.filter((entity) => !entity.isDeleted);
    }

    // Pagination
    const totalCount = filteredEntities.length;
    const startIndex = (page - 1) * pageSize;
    const endIndex = startIndex + pageSize;
    const paginatedEntities = filteredEntities.slice(startIndex, endIndex);

    const response: WorldEntityListResponse = {
      items: paginatedEntities,
      totalCount,
      page,
      pageSize,
      hasMore: endIndex < totalCount,
    };

    console.log('[MSW GET /entities] Response:', {
      itemsCount: paginatedEntities.length,
      totalCount,
    });

    return HttpResponse.json(response);
  }),

  /**
   * GET /api/v1/worlds/:worldId/entities/:entityId
   *
   * Fetch a single entity by ID
   */
  http.get('http://localhost:5000/api/v1/worlds/:worldId/entities/:entityId', ({ params }) => {
    const { entityId } = params;

    const entity = mockEntities.get(entityId as string);

    if (!entity) {
      return new HttpResponse(null, {
        status: 404,
        statusText: 'Entity not found',
      });
    }

    const response: WorldEntityResponse = {
      entity,
    };

    return HttpResponse.json(response);
  }),

  /**
   * POST /api/v1/worlds/:worldId/entities
   *
   * Create a new entity
   */
  http.post('http://localhost:5000/api/v1/worlds/:worldId/entities', async ({ params, request }) => {
    const { worldId } = params;
    const body = (await request.json()) as CreateWorldEntityRequest;

    // Generate new entity
    const newEntityId = `entity-${Date.now()}`;
    const parentEntity = body.parentId ? mockEntities.get(body.parentId) : null;

    const newEntity: WorldEntity = {
      id: newEntityId,
      worldId: worldId as string,
      parentId: body.parentId ?? null,
      entityType: body.entityType,
      name: body.name,
      description: body.description ?? '',
      tags: body.tags ?? [],
      path: parentEntity ? [...parentEntity.path, parentEntity.id] : [],
      depth: parentEntity ? parentEntity.depth + 1 : 0,
      hasChildren: false,
      ownerId: 'test-user@example.com',
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
      isDeleted: false,
    };

    mockEntities.set(newEntityId, newEntity);

    console.log('[MSW POST /entities] Entity created:', {
      newEntityId,
      worldId,
      parentId: body.parentId,
      name: body.name,
      mockEntitiesCount: mockEntities.size,
    });

    // Update parent's hasChildren flag if needed
    if (parentEntity) {
      parentEntity.hasChildren = true;
    }

    const response: WorldEntityResponse = {
      entity: newEntity,
    };

    return HttpResponse.json(response, { status: 201 });
  }),

  /**
   * PUT /api/v1/worlds/:worldId/entities/:entityId
   *
   * Update an existing entity
   */
  http.put(
    'http://localhost:5000/api/v1/worlds/:worldId/entities/:entityId',
    async ({ params, request }) => {
      const { entityId } = params;
      const body = (await request.json()) as UpdateWorldEntityRequest;

      const entity = mockEntities.get(entityId as string);

      if (!entity) {
        return new HttpResponse(null, {
          status: 404,
          statusText: 'Entity not found',
        });
      }

      // Update entity fields
      if (body.name !== undefined) entity.name = body.name;
      if (body.description !== undefined) entity.description = body.description;
      if (body.tags !== undefined) entity.tags = body.tags;
      if (body.entityType !== undefined) entity.entityType = body.entityType;
      entity.updatedAt = new Date().toISOString();

      const response: WorldEntityResponse = {
        entity,
      };

      return HttpResponse.json(response);
    },
  ),

  /**
   * DELETE /api/v1/worlds/:worldId/entities/:entityId
   *
   * Soft delete an entity
   */
  http.delete('http://localhost:5000/api/v1/worlds/:worldId/entities/:entityId', ({ params }) => {
    const { entityId } = params;

    const entity = mockEntities.get(entityId as string);

    if (!entity) {
      return new HttpResponse(null, {
        status: 404,
        statusText: 'Entity not found',
      });
    }

    // Soft delete
    entity.isDeleted = true;
    entity.updatedAt = new Date().toISOString();

    return new HttpResponse(null, { status: 204 });
  }),

  /**
   * PATCH /api/v1/worlds/:worldId/entities/:entityId/move
   *
   * Move an entity to a new parent
   */
  http.patch(
    'http://localhost:5000/api/v1/worlds/:worldId/entities/:entityId/move',
    async ({ params, request }) => {
      const { entityId } = params;
      const body = (await request.json()) as MoveWorldEntityRequest;

      const entity = mockEntities.get(entityId as string);

      if (!entity) {
        return new HttpResponse(null, {
          status: 404,
          statusText: 'Entity not found',
        });
      }

      const newParent = body.newParentId ? mockEntities.get(body.newParentId) : null;

      // Update entity's parentId and path
      entity.parentId = body.newParentId ?? null;
      entity.path = newParent ? [...newParent.path, newParent.id] : [];
      entity.depth = newParent ? newParent.depth + 1 : 0;
      entity.updatedAt = new Date().toISOString();

      const response: WorldEntityResponse = {
        entity,
      };

      return HttpResponse.json(response);
    },
  ),
];

/**
 * All MSW handlers (World + WorldEntity)
 */
export const handlers = [...worldHandlers, ...worldEntityHandlers];
