/**
 * WorldEntity API Tests
 *
 * Test suite for WorldEntity endpoints - specifically schema versioning logic.
 * Uses MSW (Mock Service Worker) for realistic HTTP mocking.
 *
 * NOTE: Most WorldEntity API behavior is tested via EntityDetailForm.test.tsx.
 * These tests focus solely on schema version injection logic.
 */

import { describe, it, expect, beforeAll, afterAll, afterEach } from 'vitest';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import { configureStore } from '@reduxjs/toolkit';
import { Provider } from 'react-redux';
import { renderHook, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';

// Import the worldEntity API slice and types
import { api } from '@/services/api';
import {
  useCreateWorldEntityMutation,
  useUpdateWorldEntityMutation,
} from '@/services/worldEntityApi';
import type {
  WorldEntity,
  WorldEntityResponse,
  CreateWorldEntityRequest,
  UpdateWorldEntityRequest,
} from '@/services/types/worldEntity.types';
import { WorldEntityType } from '@/services/types/worldEntity.types';

// Test configuration
const API_BASE_URL = 'http://localhost:5000';

// Mock data
const mockWorldEntity: WorldEntity = {
  id: '123e4567-e89b-12d3-a456-426614174000',
  worldId: '550e8400-e29b-41d4-a716-446655440000',
  parentId: null,
  entityType: WorldEntityType.Continent,
  name: 'Test Continent',
  description: 'A test continent',
  tags: [],
  path: [],
  depth: 0,
  hasChildren: false,
  ownerId: 'user-123',
  schemaVersion: 1,
  createdAt: '2025-01-01T00:00:00Z',
  updatedAt: '2025-01-01T00:00:00Z',
  isDeleted: false,
};

// Track request bodies for verification
let lastCreateRequest: CreateWorldEntityRequest | null = null;
let lastUpdateRequest: UpdateWorldEntityRequest | null = null;

// MSW server setup
const server = setupServer(
  // POST /api/v1/worlds/:worldId/entities
  http.post(`${API_BASE_URL}/api/v1/worlds/:worldId/entities`, async ({ request }) => {
    lastCreateRequest = (await request.json()) as CreateWorldEntityRequest;
    const response: WorldEntityResponse = {
      data: {
        ...mockWorldEntity,
        name: lastCreateRequest.name,
        entityType: lastCreateRequest.entityType,
        schemaVersion: lastCreateRequest.schemaVersion ?? 1,
      },
    };
    return HttpResponse.json(response, { status: 201 });
  }),

  // PUT /api/v1/worlds/:worldId/entities/:id
  http.put(`${API_BASE_URL}/api/v1/worlds/:worldId/entities/:id`, async ({ request }) => {
    lastUpdateRequest = (await request.json()) as UpdateWorldEntityRequest;
    const response: WorldEntityResponse = {
      data: {
        ...mockWorldEntity,
        name: lastUpdateRequest.name ?? mockWorldEntity.name,
        schemaVersion: lastUpdateRequest.schemaVersion ?? mockWorldEntity.schemaVersion,
      },
    };
    return HttpResponse.json(response, { status: 200 });
  })
);

// Start/stop MSW server
beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => {
  server.resetHandlers();
  lastCreateRequest = null;
  lastUpdateRequest = null;
});
afterAll(() => server.close());

// Wrapper component for hooks (provides Redux store)
function createWrapper() {
  const store = configureStore({
    reducer: {
      [api.reducerPath]: api.reducer,
    },
    middleware: (getDefaultMiddleware) =>
      getDefaultMiddleware().concat(api.middleware),
  });

  return ({ children }: { children: ReactNode }) => (
    <Provider store={store}>{children}</Provider>
  );
}

describe('WorldEntity API - Schema Versioning', () => {
  describe('T055: createWorldEntity schema version injection', () => {
    it('should include schemaVersion from ENTITY_SCHEMA_VERSIONS when creating entity', async () => {
      const { result } = renderHook(() => useCreateWorldEntityMutation(), {
        wrapper: createWrapper(),
      });

      const [createEntity] = result.current;

      const requestData: CreateWorldEntityRequest = {
        parentId: null,
        entityType: WorldEntityType.Continent,
        name: 'Test Continent',
        description: 'A test continent',
      };

      // Execute the mutation
      await createEntity({
        worldId: '550e8400-e29b-41d4-a716-446655440000',
        data: requestData,
      }).unwrap();

      // Wait for the request to be captured
      await waitFor(() => {
        expect(lastCreateRequest).toBeDefined();
      });

      // Verify the request included schemaVersion: 1 (from getSchemaVersion)
      expect(lastCreateRequest).toBeDefined();
      expect(lastCreateRequest!.schemaVersion).toBe(1);
      expect(lastCreateRequest!.entityType).toBe(WorldEntityType.Continent);
    });

    it('should preserve explicit schemaVersion if provided in request', async () => {
      const { result } = renderHook(() => useCreateWorldEntityMutation(), {
        wrapper: createWrapper(),
      });

      const [createEntity] = result.current;

      const requestData: CreateWorldEntityRequest = {
        parentId: null,
        entityType: WorldEntityType.Character,
        name: 'Test Character',
        schemaVersion: 2, // Explicitly set (hypothetical future version)
      };

      await createEntity({
        worldId: '550e8400-e29b-41d4-a716-446655440000',
        data: requestData,
      }).unwrap();

      await waitFor(() => {
        expect(lastCreateRequest).toBeDefined();
      });

      // Verify explicit schemaVersion is preserved
      expect(lastCreateRequest!.schemaVersion).toBe(2);
    });
  });

  describe('T056: updateWorldEntity schema version handling', () => {
    it('should allow schemaVersion in update request (component responsibility)', async () => {
      const { result } = renderHook(() => useUpdateWorldEntityMutation(), {
        wrapper: createWrapper(),
      });

      const [updateEntity] = result.current;

      const requestData: UpdateWorldEntityRequest = {
        name: 'Updated Continent',
        schemaVersion: 1, // Provided by component (EntityDetailForm)
      };

      await updateEntity({
        worldId: '550e8400-e29b-41d4-a716-446655440000',
        entityId: '123e4567-e89b-12d3-a456-426614174000',
        data: requestData,
        currentEntityType: WorldEntityType.Continent,
      }).unwrap();

      await waitFor(() => {
        expect(lastUpdateRequest).toBeDefined();
      });

      // Verify schemaVersion is included in the request
      expect(lastUpdateRequest!.schemaVersion).toBe(1);
    });

    it('should include current schemaVersion when not provided (FR-007 compliance)', async () => {
      const { result } = renderHook(() => useUpdateWorldEntityMutation(), {
        wrapper: createWrapper(),
      });

      const [updateEntity] = result.current;

      const requestData: UpdateWorldEntityRequest = {
        name: 'Updated Continent',
        // schemaVersion omitted - API client will use current version from config
      };

      await updateEntity({
        worldId: '550e8400-e29b-41d4-a716-446655440000',
        entityId: '123e4567-e89b-12d3-a456-426614174000',
        data: requestData,
        currentEntityType: WorldEntityType.Continent,
      }).unwrap();

      await waitFor(() => {
        expect(lastUpdateRequest).toBeDefined();
      });

      // FR-007: Verify schemaVersion is always included using current version from config
      expect(lastUpdateRequest!.schemaVersion).toBe(1); // Current version for Continent from config
    });
  });
});
