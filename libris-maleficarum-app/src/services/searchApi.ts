import { api } from './api';
import type { SearchEntitiesArg, SearchResponse } from './types';

export const searchApi = api.injectEndpoints({
  endpoints: (builder) => ({
    searchEntities: builder.query<SearchResponse, SearchEntitiesArg>({
      query: ({ worldId, q = '', mode, entityType, tags, limit = 8, offset = 0 }) => ({
        url: `/api/v1/worlds/${worldId}/search`,
        method: 'GET',
        params: {
          q,
          mode,
          entityType,
          tags,
          limit,
          offset,
        },
      }),
      keepUnusedDataFor: 120,
      providesTags: (_result, _error, { worldId }) => [
        { type: 'WorldEntity', id: `SEARCH_${worldId}` },
      ],
    }),
  }),
});

export const { useSearchEntitiesQuery } = searchApi;
