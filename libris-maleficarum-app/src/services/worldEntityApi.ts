/**
 * WorldEntity API Slice
 *
 * RTK Query endpoints for WorldEntity CRUD operations.
 * Uses code splitting via injectEndpoints for better bundle optimization.
 *
 * @see https://redux-toolkit.js.org/rtk-query/usage/code-splitting
 */

import { api } from './api';
import { logger } from '@/lib/logger';
import type {
  WorldEntity,
  WorldEntityListResponse,
  WorldEntityResponse,
  CreateWorldEntityRequest,
  UpdateWorldEntityRequest,
  MoveWorldEntityRequest,
  GetWorldEntitiesQueryParams,
  WorldEntityType,
} from './types/worldEntity.types';
import { ENTITY_SCHEMA_VERSIONS } from './types/worldEntity.types';

/**
 * WorldEntity API endpoints injected into base API slice
 */
export const worldEntityApi = api.injectEndpoints({
  endpoints: (builder) => ({
    /**
     * GET /api/v1/worlds/{worldId}/entities
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
      transformResponse: (response: WorldEntityListResponse) => ({
        ...response,
        data: response.data,
      }),
      providesTags: (result, _error, { worldId }) =>
        result
          ? [
              ...result.data.map(({ id }) => ({
                type: 'WorldEntity' as const,
                id,
              })),
              { type: 'WorldEntity', id: `LIST_${worldId}` },
            ]
          : [{ type: 'WorldEntity', id: `LIST_${worldId}` }],
    }),

    /**
     * GET /api/v1/worlds/{worldId}/entities (filtered by parent)
     */
    getEntitiesByParent: builder.query<
      WorldEntity[],
      { worldId: string; parentId: string | null }
    >({
      query: ({ worldId, parentId }) => {
        const params: Record<string, string | number> = {
          limit: 100,
        };

        if (parentId === null) {
          params.parentId = 'null';
        } else {
          params.parentId = parentId;
        }

        return {
          url: `/api/v1/worlds/${worldId}/entities`,
          method: 'GET',
          params,
        };
      },
      transformResponse: (response: WorldEntityListResponse) => response.data,
      providesTags: (result, _error, { worldId, parentId }) => {
        const parentTag = {
          type: 'WorldEntity' as const,
          id: `PARENT_${worldId}_${parentId ?? 'ROOT'}`,
        };

        logger.debug('API', 'Entities fetched by parent', {
          worldId,
          parentId,
          count: result?.length ?? 0,
        });

        return result
          ? [
              ...result.map(({ id }) => ({
                type: 'WorldEntity' as const,
                id,
              })),
              parentTag,
            ]
          : [parentTag];
      },
    }),

    /**
     * GET /api/v1/worlds/{worldId}/entities/{entityId}
     */
    getWorldEntityById: builder.query<
      WorldEntity,
      { worldId: string; entityId: string }
    >({
      query: ({ worldId, entityId }) => ({
        url: `/api/v1/worlds/${worldId}/entities/${entityId}`,
        method: 'GET',
      }),
      transformResponse: (response: WorldEntityResponse) => response.data,
      providesTags: (_result, _error, { entityId }) => [
        { type: 'WorldEntity', id: entityId },
      ],
    }),

    /**
     * POST /api/v1/worlds/{worldId}/entities
     */
    createWorldEntity: builder.mutation<
      WorldEntity,
      { worldId: string; data: CreateWorldEntityRequest }
    >({
      query: ({ worldId, data }) => ({
        url: `/api/v1/worlds/${worldId}/entities`,
        method: 'POST',
        data: {
          ...data,
          schemaVersion:
            data.schemaVersion ?? ENTITY_SCHEMA_VERSIONS[data.entityType],
        },
      }),
      transformResponse: (response: WorldEntityResponse) => response.data,
      invalidatesTags: (_result, _error, { worldId, data }) => {
        const parentTag = {
          type: 'WorldEntity' as const,
          id: `PARENT_${worldId}_${data.parentId ?? 'ROOT'}`,
        } as const;

        logger.debug('API', 'Entity created, invalidating cache', {
          worldId,
          parentId: data.parentId,
          entityType: data.entityType,
        });

        const tags: { type: 'WorldEntity'; id: string }[] = [
          { type: 'WorldEntity', id: `LIST_${worldId}` },
          parentTag,
        ];

        if (data.parentId) {
          tags.push({ type: 'WorldEntity', id: data.parentId });
        }

        return tags;
      },
    }),

    /**
     * PUT /api/v1/worlds/{worldId}/entities/{entityId}
     */
    updateWorldEntity: builder.mutation<
      WorldEntity,
      {
        worldId: string;
        entityId: string;
        data: UpdateWorldEntityRequest;
        currentEntityType: WorldEntityType;
      }
    >({
      query: ({ worldId, entityId, data, currentEntityType }) => ({
        url: `/api/v1/worlds/${worldId}/entities/${entityId}`,
        method: 'PUT',
        data: {
          ...data,
          schemaVersion:
            data.schemaVersion ??
            ENTITY_SCHEMA_VERSIONS[data.entityType ?? currentEntityType],
        },
      }),
      transformResponse: (response: WorldEntityResponse) => response.data,
      invalidatesTags: (result, _error, { worldId, entityId }) => [
        { type: 'WorldEntity', id: entityId },
        { type: 'WorldEntity', id: `LIST_${worldId}` },
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
      ],
    }),

    /**
     * PATCH /api/v1/worlds/{worldId}/entities/{entityId}/move
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
      transformResponse: (response: WorldEntityResponse) => response.data,
      invalidatesTags: (result, _error, { worldId, entityId, data }) => {
        const tags: { type: 'WorldEntity'; id: string }[] = [
          { type: 'WorldEntity', id: entityId },
          { type: 'WorldEntity', id: `LIST_${worldId}` },
          {
            type: 'WorldEntity',
            id: `PARENT_${worldId}_${data.newParentId ?? 'ROOT'}`,
          },
        ];

        if (data.newParentId) {
          tags.push({ type: 'WorldEntity', id: data.newParentId });
        }

        if (result?.parentId && result.parentId !== data.newParentId) {
          tags.push({
            type: 'WorldEntity',
            id: `PARENT_${worldId}_${result.parentId}`,
          });
          tags.push({ type: 'WorldEntity', id: result.parentId });
        }

        return tags;
      },
    }),
  }),
});

/**
 * Auto-generated hooks for WorldEntity API endpoints
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
