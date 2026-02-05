/**
 * Delete Operations API
 *
 * RTK Query endpoints for managing and tracking asynchronous WorldEntity delete operations.
 * Provides polling support for real-time status updates in the notification center.
 *
 * Aligns with backend endpoints:
 * - DELETE /api/v1/worlds/{worldId}/entities/{entityId} → Initiate delete
 * - GET /api/v1/worlds/{worldId}/delete-operations → List operations
 * - GET /api/v1/worlds/{worldId}/delete-operations/{operationId} → Get single operation
 * - POST /api/v1/worlds/{worldId}/delete-operations/{operationId}/retry → Retry failed operation
 *
 * @module deleteOperationsApi
 */

import { api } from './api';
import type { DeleteOperationDto, DeleteOperationStatus } from './types/asyncOperations';

/**
 * Response wrapper from backend API
 */
interface ApiResponse<T> {
  data: T;
  meta?: Record<string, unknown>;
}

/**
 * Query parameters for getDeleteOperations endpoint
 */
export interface GetDeleteOperationsParams {
  worldId: string;
  status?: DeleteOperationStatus[];
  limit?: number;
}

/**
 * Parameters for initiating delete
 */
export interface InitiateDeleteParams {
  worldId: string;
  entityId: string;
  cascade?: boolean;
}

/**
 * Parameters for retry operation
 */
export interface RetryDeleteParams {
  worldId: string;
  operationId: string;
}

/**
 * Parameters for cancel operation
 */
export interface CancelDeleteParams {
  worldId: string;
  operationId: string;
}

/**
 * Delete Operations API endpoints
 *
 * Injected into the base API slice using RTK Query's injectEndpoints pattern.
 * Supports polling for real-time updates via pollingInterval option.
 */
export const deleteOperationsApi = api.injectEndpoints({
  endpoints: (builder) => ({
    /**
     * Fetch list of delete operations for a world
     *
     * Supports filtering by status and limiting results.
     * Used with polling (pollingInterval: 3000) for real-time updates.
     *
     * Backend endpoint: GET /api/v1/worlds/{worldId}/delete-operations
     */
    getDeleteOperations: builder.query<
      DeleteOperationDto[],
      GetDeleteOperationsParams
    >({
      query: ({ worldId, status, limit }) => ({
        url: `/api/v1/worlds/${worldId}/delete-operations`,
        method: 'GET',
        params: status ? { status: status.join(','), limit } : { limit },
      }),
      transformResponse: (response: ApiResponse<DeleteOperationDto[]>) =>
        response.data,
      providesTags: (result) =>
        result
          ? [
              ...result.map(({ id }) => ({
                type: 'DeleteOperation' as const,
                id,
              })),
              { type: 'DeleteOperation', id: 'LIST' },
            ]
          : [{ type: 'DeleteOperation', id: 'LIST' }],
    }),

    /**
     * Fetch single delete operation by ID
     *
     * Used for detailed status view or targeted polling.
     *
     * Backend endpoint: GET /api/v1/worlds/{worldId}/delete-operations/{operationId}
     */
    getDeleteOperation: builder.query<
      DeleteOperationDto,
      { worldId: string; operationId: string }
    >({
      query: ({ worldId, operationId }) => ({
        url: `/api/v1/worlds/${worldId}/delete-operations/${operationId}`,
        method: 'GET',
      }),
      transformResponse: (response: ApiResponse<DeleteOperationDto>) =>
        response.data,
      providesTags: (_result, _error, { operationId }) => [
        { type: 'DeleteOperation', id: operationId },
      ],
    }),

    /**
     * Retry a failed or partial delete operation
     *
     * Only works for operations in 'failed' or 'partial' status.
     * Returns 400 Bad Request if operation is not in retryable state.
     *
     * Backend endpoint: POST /api/v1/worlds/{worldId}/delete-operations/{operationId}/retry
     */
    retryDeleteOperation: builder.mutation<DeleteOperationDto, RetryDeleteParams>({
      query: ({ worldId, operationId }) => ({
        url: `/api/v1/worlds/${worldId}/delete-operations/${operationId}/retry`,
        method: 'POST',
      }),
      transformResponse: (response: ApiResponse<DeleteOperationDto>) =>
        response.data,
      invalidatesTags: (_result, _error, { operationId }) => [
        { type: 'DeleteOperation', id: operationId },
        { type: 'DeleteOperation', id: 'LIST' },
      ],
    }),

    /**
     * Cancel an in-progress delete operation
     *
     * Only works for operations in 'in_progress' status.
     * Returns 400 Bad Request if operation is not cancellable.
     *
     * Backend endpoint: POST /api/v1/worlds/{worldId}/delete-operations/{operationId}/cancel
     */
    cancelDeleteOperation: builder.mutation<DeleteOperationDto, CancelDeleteParams>({
      query: ({ worldId, operationId }) => ({
        url: `/api/v1/worlds/${worldId}/delete-operations/${operationId}/cancel`,
        method: 'POST',
      }),
      transformResponse: (response: ApiResponse<DeleteOperationDto>) =>
        response.data,
      invalidatesTags: (_result, _error, { operationId }) => [
        { type: 'DeleteOperation', id: operationId },
        { type: 'DeleteOperation', id: 'LIST' },
      ],
    }),

    /**
     * Initiate async delete of WorldEntity with cascading
     *
     * Returns 202 Accepted with operation details for tracking.
     * Frontend should optimistically update the UI hierarchy while backend
     * processes delete asynchronously.
     *
     * Backend endpoint: DELETE /api/v1/worlds/{worldId}/entities/{entityId}
     */
    initiateEntityDelete: builder.mutation<DeleteOperationDto, InitiateDeleteParams>({
      query: ({ worldId, entityId, cascade = true }) => ({
        url: `/api/v1/worlds/${worldId}/entities/${entityId}`,
        method: 'DELETE',
        params: { cascade },
      }),
      transformResponse: (response: ApiResponse<DeleteOperationDto>) =>
        response.data,
      invalidatesTags: [
        { type: 'DeleteOperation', id: 'LIST' },
        { type: 'WorldEntity', id: 'LIST' },
      ],
    }),
  }),
});

/**
 * Auto-generated hooks for delete operations endpoints
 *
 * Use these hooks in React components for automatic cache management
 * and loading state handling.
 *
 * Example:
 * ```typescript
 * const { data, isLoading } = useGetDeleteOperationsQuery(
 *   { worldId: 'abc-123' },
 *   {
 *     pollingInterval: 3000, // Poll every 3 seconds
 *     skipPollingIfUnfocused: true, // Pause when tab inactive
 *   }
 * );
 * ```
 */
export const {
  useGetDeleteOperationsQuery,
  useGetDeleteOperationQuery,
  useRetryDeleteOperationMutation,
  useCancelDeleteOperationMutation,
  useInitiateEntityDeleteMutation,
} = deleteOperationsApi;
