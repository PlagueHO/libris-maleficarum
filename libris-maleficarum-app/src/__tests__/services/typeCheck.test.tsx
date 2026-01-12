/**
 * TypeScript Type Safety Tests
 *
 * These tests verify that our API client provides full type safety
 * at compile time. Many of these tests will cause compilation errors
 * if type safety is not properly implemented.
 */

import { describe, it, expect } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { configureStore } from '@reduxjs/toolkit';
import { Provider } from 'react-redux';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import type { ReactNode } from 'react';

import { api } from '@/services/api';
import { useGetWorldsQuery, useGetWorldByIdQuery, useCreateWorldMutation } from '@/services/worldApi';
import type { World, WorldListResponse, CreateWorldRequest } from '@/services/types';

// Mock server for runtime tests
const mockWorlds: World[] = [
  {
    id: '550e8400-e29b-41d4-a716-446655440000',
    name: 'Middle Earth',
    description: 'A fantasy world',
    ownerId: 'user-123',
    createdAt: '2025-01-01T00:00:00Z',
    updatedAt: '2025-01-01T00:00:00Z',
    isDeleted: false,
  },
];

const server = setupServer(
  http.get('http://localhost:5000/api/worlds', () => {
    const response: WorldListResponse = {
      data: mockWorlds,
      meta: { requestId: 'test-1', timestamp: new Date().toISOString() },
    };
    return HttpResponse.json(response);
  }),
  http.get('http://localhost:5000/api/worlds/:id', ({ params }) => {
    const world = mockWorlds.find((w) => w.id === params.id);
    if (!world) {
      return HttpResponse.json(
        { type: 'not-found', title: 'Not Found', status: 404 },
        { status: 404 }
      );
    }
    return HttpResponse.json({
      data: world,
      meta: { requestId: 'test-2', timestamp: new Date().toISOString() },
    });
  }),
  http.post('http://localhost:5000/api/worlds', () => {
    return HttpResponse.json(
      {
        data: { ...mockWorlds[0], id: 'new-id' },
        meta: { requestId: 'test-3', timestamp: new Date().toISOString() },
      },
      { status: 201 }
    );
  })
);

function createTestWrapper() {
  const store = configureStore({
    reducer: { [api.reducerPath]: api.reducer },
    middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(api.middleware),
  });

  function TestWrapper({ children }: { children: ReactNode }) {
    return <Provider store={store}>{children}</Provider>;
  }

  return TestWrapper;
}

describe('TypeScript Type Safety', () => {
  beforeAll(() => server.listen());
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  describe('Query Hook Type Safety', () => {
    it('should provide correct types for query results', async () => {
      const { result } = renderHook(() => useGetWorldsQuery(), {
        wrapper: createTestWrapper(),
      });

      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      if (result.current.data) {
        // TypeScript should know these are World[]
        const worlds = result.current.data;
        expect(Array.isArray(worlds)).toBe(true);

        const firstWorld = worlds[0];
        // All these properties should be accessible with type checking
        expect(typeof firstWorld.id).toBe('string');
        expect(typeof firstWorld.name).toBe('string');
        expect(typeof firstWorld.description).toBe('string');
        expect(typeof firstWorld.ownerId).toBe('string');
        expect(typeof firstWorld.createdAt).toBe('string');
        expect(typeof firstWorld.updatedAt).toBe('string');
        expect(typeof firstWorld.isDeleted).toBe('boolean');

        // TypeScript compilation check: The following line would cause a type error
        // if uncommented because 'invalidProperty' doesn't exist on World type
        // @ts-expect-error - Testing that invalid property access is caught
        const _invalid = firstWorld.invalidProperty;
        expect(_invalid).toBeUndefined();
      }
    });

    it('should provide correct types for query with arguments', async () => {
      const worldId = '550e8400-e29b-41d4-a716-446655440000';
      const { result } = renderHook(() => useGetWorldByIdQuery(worldId), {
        wrapper: createTestWrapper(),
      });

      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
        expect(result.current.data).toBeDefined();
      });

      if (result.current.data) {
        // TypeScript should know this is a single World, not World[]
        const world = result.current.data;

        // Verify we can't treat it as an array
        // @ts-expect-error - data is World, not World[], so .map shouldn't exist
        const _invalid = world.map;
        expect(_invalid).toBeUndefined();

        // But we can access World properties
        expect(typeof world.id).toBe('string');
        expect(world.id).toBe(worldId);
      }
    });
  });

  describe('Mutation Hook Type Safety', () => {
    it('should provide correct types for mutation arguments', async () => {
      const { result } = renderHook(() => useCreateWorldMutation(), {
        wrapper: createTestWrapper(),
      });

      // TypeScript should enforce CreateWorldRequest shape
      const validRequest: CreateWorldRequest = {
        name: 'Test World',
        description: 'A test world',
      };

      const [createWorld] = result.current;
      const promise = createWorld(validRequest);

      await waitFor(() => expect(promise).resolves.toBeDefined());

      // TypeScript compilation check: The following would cause an error
      // because CreateWorldRequest doesn't have an 'id' property
      // @ts-expect-error - Testing that invalid mutation args are caught
      const _invalidPromise = createWorld({ id: 'invalid', name: 'Test' });
      expect(_invalidPromise).toBeDefined();
    });

    it('should provide correct types for mutation results', async () => {
      const { result } = renderHook(() => useCreateWorldMutation(), {
        wrapper: createTestWrapper(),
      });

      const [createWorld] = result.current;
      const response = await createWorld({
        name: 'Test World',
        description: 'Test',
      });

      if ('data' in response && response.data) {
        // Response should be typed as World
        const world = response.data;
        expect(typeof world.id).toBe('string');
        expect(typeof world.name).toBe('string');

        // TypeScript should catch invalid property access
        // @ts-expect-error - Testing that invalid property access is caught
        const _invalid = world.invalidField;
        expect(_invalid).toBeUndefined();
      }
    });
  });

  describe('Error Type Safety', () => {
    it('should provide typed error objects', async () => {
      server.use(
        http.get('http://localhost:5000/api/worlds/:id', () => {
          return HttpResponse.json(
            {
              type: 'https://api.librismaleficarum.com/errors/not-found',
              title: 'Resource Not Found',
              status: 404,
              detail: 'World not found',
            },
            { status: 404 }
          );
        })
      );

      const { result } = renderHook(() => useGetWorldByIdQuery('non-existent'), {
        wrapper: createTestWrapper(),
      });

      await waitFor(() => expect(result.current.isError).toBe(true));

      if (result.current.error) {
        // Error should have a data property with ProblemDetails
        const error = result.current.error as { data: { status: number; title: string } };
        expect(error.data.status).toBe(404);
        expect(error.data.title).toBe('Resource Not Found');
      }
    });
  });

  describe('Hook State Type Safety', () => {
    it('should provide correct types for all hook state properties', async () => {
      const { result } = renderHook(() => useGetWorldsQuery(), {
        wrapper: createTestWrapper(),
      });

      // All these properties should be accessible with correct types
      // Boolean flags - check initial state
      expect(typeof result.current.isLoading).toBe('boolean');
      expect(typeof result.current.isSuccess).toBe('boolean');
      expect(typeof result.current.isError).toBe('boolean');
      expect(typeof result.current.isFetching).toBe('boolean');
      expect(typeof result.current.isUninitialized).toBe('boolean');

      // Data and error can be undefined initially
      if (!result.current.isSuccess) {
        expect(result.current.data).toBeUndefined();
      }

      if (!result.current.isError) {
        expect(result.current.error).toBeUndefined();
      }

      // Wait for successful state with data
      await waitFor(() => {
        expect(result.current.isSuccess).toBe(true);
        expect(result.current.data).toBeDefined();
      });

      // After success, data should be defined and be an array
      expect(Array.isArray(result.current.data)).toBe(true);
    });

    it('should provide correct mutation state types', () => {
      const { result } = renderHook(() => useCreateWorldMutation(), {
        wrapper: createTestWrapper(),
      });

      const [_mutate, state] = result.current;

      // Mutation state properties
      expect(typeof state.isLoading).toBe('boolean');
      expect(typeof state.isSuccess).toBe('boolean');
      expect(typeof state.isError).toBe('boolean');
      expect(typeof state.isUninitialized).toBe('boolean');

      // Data should be undefined initially
      expect(state.data).toBeUndefined();
      expect(state.error).toBeUndefined();
    });
  });

  describe('Generic Type Preservation', () => {
    it('should preserve types through transformResponse', async () => {
      const { result } = renderHook(() => useGetWorldsQuery(), {
        wrapper: createTestWrapper(),
      });

      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      // The response goes through transformResponse in worldApi.ts
      // which extracts WorldListResponse.data -> World[]
      // TypeScript should still know the final type is World[]
      if (result.current.data) {
        const worlds = result.current.data;

        // This should work without type assertion
        worlds.forEach((world) => {
          expect(typeof world.id).toBe('string');
          expect(typeof world.name).toBe('string');
          // No need for 'as World' cast - TypeScript knows the type
        });
      }
    });
  });
});
