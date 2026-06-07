import { act, renderHook } from '@testing-library/react';
import { beforeEach, describe, expect, it } from 'vitest';

import { useSearchHistory } from './useSearchHistory';

describe('useSearchHistory', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('keeps recent search queries deduplicated and capped per world', () => {
    const { result } = renderHook(() => useSearchHistory('world-a'));

    act(() => {
      for (let index = 0; index < 10; index += 1) {
        result.current.addSearchQuery(`Query ${index % 3}`);
      }
    });

    expect(result.current.history).toHaveLength(3);
    expect(result.current.history[0]?.query).toBe('Query 0');
    expect(localStorage.getItem('lm.search.history.world-a')).toContain('Query 0');
  });

  it('tracks recent entities and pins up to five items per world', () => {
    const { result } = renderHook(() => useSearchHistory('world-a'));

    const entity = {
      id: 'entity-1',
      name: 'Cormyr',
      entityType: 'Country',
      descriptionSnippet: 'Kingdom',
      relevanceScore: 1,
      worldId: 'world-a',
      parentId: null,
      path: ['continent-faerun'],
      depth: 1,
      tags: ['kingdom'],
      ownerId: 'owner',
      createdAt: '2026-01-13T12:00:00Z',
      updatedAt: '2026-01-13T12:00:00Z',
    };

    act(() => {
      result.current.addRecentEntity(entity);
    });

    expect(result.current.recentEntities).toHaveLength(1);
    expect(result.current.recentEntities[0]?.name).toBe('Cormyr');

    act(() => {
      result.current.togglePinnedEntity(entity);
    });

    expect(result.current.pinnedEntities).toHaveLength(1);
    expect(result.current.canPin).toBe(true);
  });

  it('keeps storage isolated by world ID', () => {
    const worldA = renderHook(() => useSearchHistory('world-a'));
    const worldB = renderHook(() => useSearchHistory('world-b'));

    act(() => {
      worldA.result.current.addSearchQuery('Faerûn');
      worldB.result.current.addSearchQuery('Neverwinter');
    });

    expect(worldA.result.current.history).toHaveLength(1);
    expect(worldA.result.current.history[0]?.query).toBe('Faerûn');
    expect(worldB.result.current.history).toHaveLength(1);
    expect(worldB.result.current.history[0]?.query).toBe('Neverwinter');
  });
});
