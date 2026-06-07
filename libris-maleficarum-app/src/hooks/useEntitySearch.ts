import { useMemo } from 'react';

import { useSearchEntitiesQuery } from '@/services/searchApi';
import type { SearchResultItem } from '@/services/types';
import { useAppSelector } from '@/store/store';
import { selectSelectedWorldId } from '@/store/worldSidebarSlice';

import { useDebouncedValue } from './useDebouncedValue';

export const MIN_QUERY_LENGTH = 2;

export interface UseEntitySearchResult {
  query: string;
  results: SearchResultItem[];
  isSearching: boolean;
  hasResults: boolean;
  isEmptyQuery: boolean;
  isError: boolean;
  refetch: () => void;
}

export function useEntitySearch(query: string, entityType?: string): UseEntitySearchResult {
  const selectedWorldId = useAppSelector(selectSelectedWorldId);
  const debouncedQuery = useDebouncedValue(query.trim(), 250);

  const shouldSearch = Boolean(selectedWorldId) && debouncedQuery.length >= MIN_QUERY_LENGTH;

  const { data, isFetching, isError, refetch } = useSearchEntitiesQuery(
    {
      worldId: selectedWorldId ?? '',
      q: debouncedQuery,
      entityType,
      limit: 8,
    },
    { skip: !shouldSearch },
  );

  return useMemo(
    () => ({
      query: debouncedQuery,
      results: data?.data ?? [],
      isSearching: isFetching,
      hasResults: (data?.data?.length ?? 0) > 0,
      isEmptyQuery: debouncedQuery.length < MIN_QUERY_LENGTH,
      isError,
      refetch,
    }),
    [data?.data, debouncedQuery, isError, isFetching, refetch],
  );
}
