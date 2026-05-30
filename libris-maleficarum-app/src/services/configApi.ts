/**
 * Config API Slice
 *
 * RTK Query endpoints for application configuration state.
 */

import { api } from './api';
import type { AccessControlStatus } from './types';

export const configApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getAccessStatus: builder.query<AccessControlStatus, void>({
      query: () => ({
        url: '/api/config/access-status',
        method: 'GET',
      }),
    }),
  }),
});

export const { useGetAccessStatusQuery, useLazyGetAccessStatusQuery } = configApi;
