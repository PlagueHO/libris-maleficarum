/**
 * World API Slice
 *
 * RTK Query endpoints for World entity CRUD operations.
 * Uses code splitting via injectEndpoints for better bundle optimization.
 *
 * @see https://redux-toolkit.js.org/rtk-query/usage/code-splitting
 */

import { api } from './api';
import { logger } from '@/lib/logger';
import type {
  World,
  WorldListResponse,
  WorldResponse,
  CreateWorldRequest,
  UpdateWorldRequest,
} from './types';

/**
 * World API endpoints injected into base API slice
 */
export const worldApi = api.injectEndpoints({
  endpoints: (builder) => ({
    /**
     * GET /api/v1/worlds
     *
     * Fetch list of all worlds owned by the authenticated user.
     * Response is cached and tagged with 'World' for automatic invalidation.
     */
    getWorlds: builder.query<World[], void>({
      query: () => ({
        url: '/api/v1/worlds',
        method: 'GET',
      }),
      transformResponse: (response: WorldListResponse) => response.data,
      providesTags: (_result) =>
        _result
          ? [
              ..._result.map(({ id }) => ({ type: 'World' as const, id })),
              { type: 'World', id: 'LIST' },
            ]
          : [{ type: 'World', id: 'LIST' }],
    }),

    /**
     * GET /api/v1/worlds/{id}
     *
     * Fetch a single world by ID.
     * Response is cached and tagged with specific world ID for targeted invalidation.
     */
    getWorldById: builder.query<World, string>({
      query: (id) => ({
        url: `/api/v1/worlds/${id}`,
        method: 'GET',
      }),
      transformResponse: (response: WorldResponse) => response.data,
      providesTags: (_result, _error, id) => [{ type: 'World', id }],
    }),

    /**
     * POST /api/v1/worlds
     *
     * Create a new world.
     * Automatically invalidates the worlds list cache on success.
     */
    createWorld: builder.mutation<World, CreateWorldRequest>({
      query: (body) => ({
        url: '/api/v1/worlds',
        method: 'POST',
        data: body,
      }),
      transformResponse: (response: WorldResponse) => response.data,
      invalidatesTags: (result) => {
        if (result) {
          logger.debug('API', 'World created, invalidating cache', {
            worldId: result.id,
            worldName: result.name,
          });
        }
        return [{ type: 'World', id: 'LIST' }];
      },
    }),

    /**
     * PUT /api/v1/worlds/{id}
     *
     * Update an existing world.
     * Invalidates both the specific world cache and the worlds list.
     */
    updateWorld: builder.mutation<
      World,
      { id: string; data: UpdateWorldRequest }
    >({
      query: ({ id, data }) => ({
        url: `/api/v1/worlds/${id}`,
        method: 'PUT',
        data,
      }),
      transformResponse: (response: WorldResponse) => response.data,
      invalidatesTags: (result, _error, { id }) => {
        if (result) {
          logger.debug('API', 'World updated, invalidating cache', {
            worldId: result.id,
            worldName: result.name,
          });
        }
        return [
          { type: 'World', id },
          { type: 'World', id: 'LIST' },
        ];
      },
    }),

    /**
     * DELETE /api/v1/worlds/{id}
     *
     * Soft delete a world.
     * Invalidates both the specific world cache and the worlds list.
     */
    deleteWorld: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/v1/worlds/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, id) => [
        { type: 'World', id },
        { type: 'World', id: 'LIST' },
      ],
    }),
  }),
});

/**
 * Auto-generated hooks for World API endpoints
 *
 * Usage in components:
 * const { data: worlds, isLoading, error } = useGetWorldsQuery();
 * const { data: world } = useGetWorldByIdQuery(worldId);
 * const [createWorld, { isLoading }] = useCreateWorldMutation();
 * const [updateWorld] = useUpdateWorldMutation();
 * const [deleteWorld] = useDeleteWorldMutation();
 */
export const {
  useGetWorldsQuery,
  useGetWorldByIdQuery,
  useCreateWorldMutation,
  useUpdateWorldMutation,
  useDeleteWorldMutation,
} = worldApi;
