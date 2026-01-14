/**
 * WorldEntity API Slice
 *
 * RTK Query endpoints for WorldEntity CRUD operations.
 * Uses code splitting via injectEndpoints for better bundle optimization.
 *
 * @see https://redux-toolkit.js.org/rtk-query/usage/code-splitting
 */

import { api } from './api';
import type {
  WorldEntity,
  WorldEntityListResponse,
  WorldEntityResponse,
  CreateWorldEntityRequest,
  UpdateWorldEntityRequest,
  MoveWorldEntityRequest,
  GetWorldEntitiesQueryParams,
} from './types/worldEntity.types';

/**
 * WorldEntity API endpoints injected into base API slice
 */
export const worldEntityApi = api.injectEndpoints({
  endpoints: (builder) => ({
    /**
     * GET /api/v1/worlds/{worldId}/entities
     *
     * Fetch list of entities for a world, optionally filtered by parent, type, or tags.
     * Response is cached and tagged with 'WorldEntity' for automatic invalidation.
     *
     * @param params - Query parameters including worldId, parentId, entityType, tags, pagination
     */
    getWorldEntities: builder.query<
      WorldEntityListResponse,
      GetWorldEntitiesQueryParams
    >({
      query: ({ worldId, ...params }) => ({
        url: `/api/v1/worlds/${worldId}/entities`,
        method: 'GET',
        params,
      }),
      providesTags: (result, _error, { worldId }) =>
        result
          ? [
              ...result.items.map(({ id }) => ({
                type: 'WorldEntity' as const,
                id,
              })),
              { type: 'WorldEntity', id: `LIST_${worldId}` },
            ]
          : [{ type: 'WorldEntity', id: `LIST_${worldId}` }],
    }),

    /**
     * GET /api/v1/worlds/{worldId}/entities (filtered by parent)
     *
     * Convenience endpoint for fetching entities by parent ID.
     * Commonly used for lazy-loading tree children.
     *
     * @param worldId - Parent world ID
     * @param parentId - Parent entity ID (null for root entities)
     */
    getEntitiesByParent: builder.query<
      WorldEntity[],
      { worldId: string; parentId: string | null }
    >({
      query: ({ worldId, parentId }) => ({
        url: `/api/v1/worlds/${worldId}/entities`,
        method: 'GET',
        params: {
          parentId,
          pageSize: 100, // Assume most parents have <100 children
        },
      }),
      transformResponse: (response: WorldEntityListResponse) => response.items,
      providesTags: (result, _error, { worldId, parentId }) =>
        result
          ? [
              ...result.map(({ id }) => ({
                type: 'WorldEntity' as const,
                id,
              })),
              {
                type: 'WorldEntity',
                id: `PARENT_${worldId}_${parentId ?? 'ROOT'}`,
              },
            ]
          : [
              {
                type: 'WorldEntity',
                id: `PARENT_${worldId}_${parentId ?? 'ROOT'}`,
              },
            ],
    }),

    /**
     * GET /api/v1/worlds/{worldId}/entities/{entityId}
     *
     * Fetch a single entity by ID.
     * Response is cached and tagged with specific entity ID for targeted invalidation.
     *
     * @param worldId - Parent world ID
     * @param entityId - Entity ID
     */
    getWorldEntityById: builder.query<
      WorldEntity,
      { worldId: string; entityId: string }
    >({
      query: ({ worldId, entityId }) => ({
        url: `/api/v1/worlds/${worldId}/entities/${entityId}`,
        method: 'GET',
      }),
      transformResponse: (response: WorldEntityResponse) => response.entity,
      providesTags: (_result, _error, { entityId }) => [
        { type: 'WorldEntity', id: entityId },
      ],
    }),

    /**
     * POST /api/v1/worlds/{worldId}/entities
     *
     * Create a new entity.
     * Automatically invalidates the parent's children cache on success.
     *
     * @param worldId - Parent world ID
     * @param data - Entity creation request
     */
    createWorldEntity: builder.mutation<
      WorldEntity,
      { worldId: string; data: CreateWorldEntityRequest }
    >({
      query: ({ worldId, data }) => ({
        url: `/api/v1/worlds/${worldId}/entities`,
        method: 'POST',
        data,
      }),
      transformResponse: (response: WorldEntityResponse) => response.entity,
      invalidatesTags: (_result, _error, { worldId, data }) => [
        { type: 'WorldEntity', id: `LIST_${worldId}` },
        {
          type: 'WorldEntity',
          id: `PARENT_${worldId}_${data.parentId ?? 'ROOT'}`,
        },
      ],
    }),

    /**
     * PUT /api/v1/worlds/{worldId}/entities/{entityId}
     *
     * Update an existing entity.
     * Invalidates both the specific entity cache and its parent's children cache.
     *
     * @param worldId - Parent world ID
     * @param entityId - Entity ID
     * @param data - Entity update request
     */
    updateWorldEntity: builder.mutation<
      WorldEntity,
      { worldId: string; entityId: string; data: UpdateWorldEntityRequest }
    >({
      query: ({ worldId, entityId, data }) => ({
        url: `/api/v1/worlds/${worldId}/entities/${entityId}`,
        method: 'PUT',
        data,
      }),
      transformResponse: (response: WorldEntityResponse) => response.entity,
      invalidatesTags: (result, _error, { worldId, entityId }) => [
        { type: 'WorldEntity', id: entityId },
        { type: 'WorldEntity', id: `LIST_${worldId}` },
        // Also invalidate parent's children cache if the entity's parentId is known
        ...(result?.parentId
          ? [
              {
                type: 'WorldEntity' as const,
                id: `PARENT_${worldId}_${result.parentId}`,
              },
            ]
          : []),
      ],
    }),

    /**
     * DELETE /api/v1/worlds/{worldId}/entities/{entityId}
     *
     * Soft delete an entity.
     * Invalidates both the specific entity cache and its parent's children cache.
     *
     * @param worldId - Parent world ID
     * @param entityId - Entity ID
     */
    deleteWorldEntity: builder.mutation<
      void,
      { worldId: string; entityId: string }
    >({
      query: ({ worldId, entityId }) => ({
        url: `/api/v1/worlds/${worldId}/entities/${entityId}`,
        method: 'DELETE',
      }),
      invalidatesTags: (_result, _error, { worldId, entityId }) => [
        { type: 'WorldEntity', id: entityId },
        { type: 'WorldEntity', id: `LIST_${worldId}` },
        // Note: We can't know the parentId without fetching the entity first,
        // so we invalidate the world list broadly. Consider optimistic updates
        // if this becomes a performance concern.
      ],
    }),

    /**
     * PATCH /api/v1/worlds/{worldId}/entities/{entityId}/move
     *
     * Move an entity to a new parent (reparent operation).
     * Invalidates the old parent's and new parent's children caches.
     *
     * @param worldId - Parent world ID
     * @param entityId - Entity ID
     * @param data - Move request with newParentId
     */
    moveWorldEntity: builder.mutation<
      WorldEntity,
      { worldId: string; entityId: string; data: MoveWorldEntityRequest }
    >({
      query: ({ worldId, entityId, data }) => ({
        url: `/api/v1/worlds/${worldId}/entities/${entityId}/move`,
        method: 'PATCH',
        data,
      }),
      transformResponse: (response: WorldEntityResponse) => response.entity,
      invalidatesTags: (result, _error, { worldId, entityId, data }) => [
        { type: 'WorldEntity', id: entityId },
        { type: 'WorldEntity', id: `LIST_${worldId}` },
        // Invalidate new parent's children cache
        {
          type: 'WorldEntity',
          id: `PARENT_${worldId}_${data.newParentId ?? 'ROOT'}`,
        },
        // Invalidate old parent's children cache (if we know it from the result)
        ...(result?.parentId && result.parentId !== data.newParentId
          ? [
              {
                type: 'WorldEntity' as const,
                id: `PARENT_${worldId}_${result.parentId}`,
              },
            ]
          : []),
      ],
    }),
  }),
});

/**
 * Auto-generated hooks for WorldEntity API endpoints
 *
 * Usage in components:
 *
 * @example
 * // Fetch all entities for a world with filters
 * const { data, isLoading, error } = useGetWorldEntitiesQuery({
 *   worldId: 'abc123',
 *   parentId: null, // Root entities
 *   entityType: WorldEntityType.Continent,
 *   page: 1,
 *   pageSize: 50,
 * });
 *
 * @example
 * // Fetch children of a specific entity (lazy loading)
 * const { data: children } = useGetEntitiesByParentQuery({
 *   worldId: 'abc123',
 *   parentId: 'xyz789',
 * });
 *
 * @example
 * // Create a new entity
 * const [createEntity, { isLoading }] = useCreateWorldEntityMutation();
 * await createEntity({
 *   worldId: 'abc123',
 *   data: {
 *     worldId: 'abc123',
 *     parentId: 'xyz789',
 *     entityType: WorldEntityType.Country,
 *     name: 'Cormyr',
 *     description: 'A kingdom in Faerûn',
 *     tags: ['forgotten-realms'],
 *   },
 * }).unwrap();
 *
 * @example
 * // Update an entity
 * const [updateEntity] = useUpdateWorldEntityMutation();
 * await updateEntity({
 *   worldId: 'abc123',
 *   entityId: 'xyz789',
 *   data: { name: 'Updated Name' },
 * }).unwrap();
 *
 * @example
 * // Delete an entity
 * const [deleteEntity] = useDeleteWorldEntityMutation();
 * await deleteEntity({ worldId: 'abc123', entityId: 'xyz789' }).unwrap();
 *
 * @example
 * // Move an entity to a new parent
 * const [moveEntity] = useMoveWorldEntityMutation();
 * await moveEntity({
 *   worldId: 'abc123',
 *   entityId: 'xyz789',
 *   data: { newParentId: 'newparent123' },
 * }).unwrap();
 */
export const {
  useGetWorldEntitiesQuery,
  useGetEntitiesByParentQuery,
  useGetWorldEntityByIdQuery,
  useCreateWorldEntityMutation,
  useUpdateWorldEntityMutation,
  useDeleteWorldEntityMutation,
  useMoveWorldEntityMutation,
} = worldEntityApi;
