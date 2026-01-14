/**
 * Base RTK Query API Slice
 *
 * Central API configuration using RTK Query with custom axiosBaseQuery.
 * Provides automatic caching, loading state management, and error handling.
 *
 * @see https://redux-toolkit.js.org/rtk-query/overview
 * @see https://redux-toolkit.js.org/rtk-query/usage/customizing-queries
 */

import { createApi } from '@reduxjs/toolkit/query/react';
import type { BaseQueryFn } from '@reduxjs/toolkit/query';
import type { AxiosError, AxiosRequestConfig } from 'axios';
import { apiClient } from '@/lib/apiClient';
import type { ProblemDetails } from '@/services/types';

/**
 * Custom base query using Axios instead of fetch
 *
 * RTK Query's fetchBaseQuery doesn't support retry logic,
 * so we use a custom baseQuery that wraps our configured Axios instance.
 *
 * Supports request cancellation via AbortController when components unmount
 * or queries are superseded by new requests.
 */
const axiosBaseQuery = (): BaseQueryFn<
  {
    url: string;
    method?: AxiosRequestConfig['method'];
    data?: AxiosRequestConfig['data'];
    params?: AxiosRequestConfig['params'];
    headers?: AxiosRequestConfig['headers'];
  },
  unknown,
  { status?: number; data: ProblemDetails }
> => {
  return async ({ url, method = 'GET', data, params, headers }, { signal }) => {
    try {
      const result = await apiClient({
        url,
        method,
        data,
        params,
        headers,
        signal, // Pass AbortSignal for request cancellation
      });
      return { data: result.data };
    } catch (axiosError) {
      const err = axiosError as AxiosError<ProblemDetails>;

      // Check if request was cancelled
      if (err.code === 'ERR_CANCELED') {
        return {
          error: {
            status: 0,
            data: {
              title: 'Request Cancelled',
              status: 0,
              detail: 'The request was cancelled',
            },
          },
        };
      }

      return {
        error: {
          status: err.response?.status,
          data: err.response?.data || {
            title: 'Network Error',
            status: 0,
            detail: err.message,
          },
        },
      };
    }
  };
};

/**
 * Base API slice configuration
 *
 * Features:
 * - Custom axiosBaseQuery with retry logic
 * - Tag-based cache invalidation
 * - 60-second cache duration (RTK Query default)
 * - Automatic refetching on mount if data is stale (>30s)
 */
export const api = createApi({
  reducerPath: 'api',
  baseQuery: axiosBaseQuery(),
  tagTypes: ['World', 'WorldEntity', 'Character', 'Location', 'Organization'], // Tag types for cache invalidation
  keepUnusedDataFor: 60, // Cache data for 60 seconds after last component unmounts
  refetchOnMountOrArgChange: 30, // Refetch if data is older than 30 seconds
  endpoints: () => ({}), // Endpoints defined via injectEndpoints in feature slices
});
