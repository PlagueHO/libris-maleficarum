import { useCallback, useEffect, useMemo, useState } from 'react';

import type { SearchResultItem } from '@/services/types';

export const MAX_HISTORY_ITEMS = 8;
export const MAX_RECENT_ITEMS = 8;
export const MAX_PINNED_ITEMS = 5;

export interface SearchHistoryEntry {
  query: string;
  updatedAt: string;
}

export interface RecentEntityRecord {
  id: string;
  worldId: string;
  name: string;
  entityType: string;
  path?: string[];
  updatedAt: string;
}

export type PinnedEntityRecord = RecentEntityRecord;

function storageKey(prefix: string, worldId: string | null): string {
  return `lm.search.${prefix}.${worldId ?? 'global'}`;
}

function readStoredItems<T>(key: string): T[] {
  if (typeof localStorage === 'undefined') {
    return [];
  }

  try {
    const raw = localStorage.getItem(key);
    if (!raw) {
      return [];
    }

    const parsed = JSON.parse(raw) as T[];
    return Array.isArray(parsed) ? parsed : [];
  } catch {
    return [];
  }
}

function writeStoredItems<T>(key: string, value: T[]): void {
  if (typeof localStorage === 'undefined') {
    return;
  }

  try {
    localStorage.setItem(key, JSON.stringify(value));
  } catch {
    // Ignore localStorage quota / privacy restrictions in tests or restricted browsers.
  }
}

function truncate<T>(items: T[], limit: number): T[] {
  return items.slice(0, limit);
}

function dedupeSearchQueries(items: SearchHistoryEntry[], query: string): SearchHistoryEntry[] {
  const nextQuery = query.trim();
  if (!nextQuery) {
    return items;
  }

  return truncate(
    [
      { query: nextQuery, updatedAt: new Date().toISOString() },
      ...items.filter((item) => item.query.toLowerCase() !== nextQuery.toLowerCase()),
    ],
    MAX_HISTORY_ITEMS,
  );
}

function dedupeRecentEntities(items: RecentEntityRecord[], entity: SearchResultItem): RecentEntityRecord[] {
  return truncate(
    [
      {
        id: entity.id,
        worldId: entity.worldId,
        name: entity.name,
        entityType: entity.entityType,
        path: entity.path,
        updatedAt: entity.updatedAt ?? new Date().toISOString(),
      },
      ...items.filter((item) => item.id !== entity.id),
    ],
    MAX_RECENT_ITEMS,
  );
}

export function useSearchHistory(worldId: string | null) {
  const [history, setHistory] = useState<SearchHistoryEntry[]>(() =>
    readStoredItems<SearchHistoryEntry>(storageKey('history', worldId)),
  );
  const [recentEntities, setRecentEntities] = useState<RecentEntityRecord[]>(() =>
    readStoredItems<RecentEntityRecord>(storageKey('recent', worldId)),
  );
  const [pinnedEntities, setPinnedEntities] = useState<PinnedEntityRecord[]>(() =>
    readStoredItems<PinnedEntityRecord>(storageKey('pinned', worldId)),
  );

  /* eslint-disable react-hooks/set-state-in-effect */
  useEffect(() => {
    setHistory(readStoredItems<SearchHistoryEntry>(storageKey('history', worldId)));
    setRecentEntities(readStoredItems<RecentEntityRecord>(storageKey('recent', worldId)));
    setPinnedEntities(readStoredItems<PinnedEntityRecord>(storageKey('pinned', worldId)));
  }, [worldId]);
  /* eslint-enable react-hooks/set-state-in-effect */

  const addSearchQuery = useCallback((query: string) => {
    setHistory((currentHistory) => {
      const nextHistory = dedupeSearchQueries(currentHistory, query);
      writeStoredItems(storageKey('history', worldId), nextHistory);
      return nextHistory;
    });
  }, [worldId]);

  const addRecentEntity = useCallback((entity: SearchResultItem) => {
    setRecentEntities((currentRecent) => {
      const nextRecent = dedupeRecentEntities(currentRecent, entity);
      writeStoredItems(storageKey('recent', worldId), nextRecent);
      return nextRecent;
    });
  }, [worldId]);

  const togglePinnedEntity = useCallback((entity: SearchResultItem) => {
    let wasPinned = false;

    setPinnedEntities((currentPinned) => {
      const existing = currentPinned.some((item) => item.id === entity.id && item.worldId === entity.worldId);

      if (existing) {
        const nextPinned = currentPinned.filter((item) => item.id !== entity.id || item.worldId !== entity.worldId);
        writeStoredItems(storageKey('pinned', worldId), nextPinned);
        return nextPinned;
      }

      if (currentPinned.length >= MAX_PINNED_ITEMS) {
        return currentPinned;
      }

      wasPinned = true;
      const nextPinned = truncate(
        [
          {
            id: entity.id,
            worldId: entity.worldId,
            name: entity.name,
            entityType: entity.entityType,
            path: entity.path,
            updatedAt: entity.updatedAt ?? new Date().toISOString(),
          },
          ...currentPinned,
        ],
        MAX_PINNED_ITEMS,
      );

      writeStoredItems(storageKey('pinned', worldId), nextPinned);
      return nextPinned;
    });

    return wasPinned;
  }, [worldId]);

  return useMemo(
    () => ({
      history,
      recentEntities,
      pinnedEntities,
      canPin: pinnedEntities.length < MAX_PINNED_ITEMS,
      addSearchQuery,
      addRecentEntity,
      togglePinnedEntity,
    }),
    [addRecentEntity, addSearchQuery, history, pinnedEntities, recentEntities, togglePinnedEntity],
  );
}
