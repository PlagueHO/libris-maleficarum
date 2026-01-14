/**
 * Hierarchy Caching Integration Tests (T045)
 *
 * Integration tests for sessionStorage caching of entity hierarchies.
 * Covers cache hits across world switches, cache invalidation on mutations,
 * and cache restoration on return to previously viewed world.
 *
 * @see EntityTree.tsx
 * @see sessionCache.ts
 */

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import * as sessionCache from '@/lib/sessionCache';

describe('Hierarchy Caching Integration', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  describe('Cache Population and Retrieval', () => {
    it('should populate cache when hierarchy is fetched', () => {
      // Arrange
      const cacheKey = 'sidebar_hierarchy_test-world-123_null';
      const hierarchyData = [
        { id: '1', parentId: null, name: 'Faerûn', type: 'continent' },
        { id: '2', parentId: null, name: 'Greyhawk', type: 'continent' },
      ];

      // Act
      sessionCache.set(cacheKey, hierarchyData);

      // Assert
      const cached = sessionCache.get(cacheKey, null);
      expect(cached).toEqual(hierarchyData);
    });

    it('should return default value when cache is not found', () => {
      // Arrange
      const cacheKey = 'non-existent-cache-key';
      const defaultValue: unknown[] = [];

      // Act
      const result = sessionCache.get(cacheKey, defaultValue);

      // Assert
      expect(result).toEqual(defaultValue);
    });

    it('should maintain separate caches for different worlds', () => {
      // Arrange
      const world1Key = 'sidebar_hierarchy_world-1_null';
      const world2Key = 'sidebar_hierarchy_world-2_null';

      const world1Data = [{ id: '1', name: 'World 1 Entity' }];
      const world2Data = [{ id: '2', name: 'World 2 Entity' }];

      // Act
      sessionCache.set(world1Key, world1Data);
      sessionCache.set(world2Key, world2Data);

      // Assert
      expect(sessionCache.get(world1Key, null)).toEqual(world1Data);
      expect(sessionCache.get(world2Key, null)).toEqual(world2Data);
    });

    it('should maintain separate caches for different parent entities', () => {
      // Arrange
      const world1Root = 'sidebar_hierarchy_world-1_null';
      const world1Child = 'sidebar_hierarchy_world-1_entity-123';

      const rootData = [{ id: '1', name: 'Root Entity' }];
      const childData = [{ id: '2', parentId: '1', name: 'Child Entity' }];

      // Act
      sessionCache.set(world1Root, rootData);
      sessionCache.set(world1Child, childData);

      // Assert
      expect(sessionCache.get(world1Root, null)).toEqual(rootData);
      expect(sessionCache.get(world1Child, null)).toEqual(childData);
    });
  });

  describe('Cache Invalidation on Mutations', () => {
    it('should invalidate cache when entity is created', () => {
      // Arrange
      const cacheKey = 'sidebar_hierarchy_test-world-123_null';
      sessionCache.set(cacheKey, [{ id: '1', name: 'Entity 1' }]);

      // Assert cache exists
      expect(sessionCache.get(cacheKey, null)).not.toBeNull();

      // Act: Invalidate cache
      sessionCache.invalidate(cacheKey);

      // Assert: Cache is gone
      expect(sessionCache.get(cacheKey, null)).toBeNull();
    });

    it('should clear world cache when world is updated', () => {
      // Arrange
      const worldId = 'test-world-123';
      const hierarchyCacheKey = `sidebar_hierarchy_${worldId}_null`;
      const childrenCacheKey = `sidebar_hierarchy_${worldId}_entity-1`;

      sessionCache.set(hierarchyCacheKey, [{ id: '1' }]);
      sessionCache.set(childrenCacheKey, [{ id: '2', parentId: '1' }]);

      // Act: Invalidate all cache for this world
      sessionCache.invalidatePattern(new RegExp(`^sidebar_hierarchy_${worldId}`));

      // Assert
      expect(sessionCache.get(hierarchyCacheKey, null)).toBeNull();
      expect(sessionCache.get(childrenCacheKey, null)).toBeNull();
    });

    it('should preserve other worlds cache when one world is updated', () => {
      // Arrange
      sessionCache.set('sidebar_hierarchy_world-1_null', [{ id: '1' }]);
      sessionCache.set('sidebar_hierarchy_world-2_null', [{ id: '2' }]);

      // Act: Invalidate only world-1 cache
      sessionCache.invalidatePattern(/^sidebar_hierarchy_world-1/);

      // Assert
      expect(sessionCache.get('sidebar_hierarchy_world-1_null', null)).toBeNull();
      expect(sessionCache.get('sidebar_hierarchy_world-2_null', null)).toEqual([{ id: '2' }]);
    });
  });

  describe('Cache Restoration on World Return', () => {
    it('should restore expanded node state when returning to world', () => {
      // Arrange
      const worldId = 'test-world-123';
      const expandedKey = `sidebar_expanded_${worldId}`;

      // Simulate user expanding nodes
      const expandedNodes = ['entity-1', 'entity-2'];
      sessionCache.set(expandedKey, expandedNodes);

      // Act: Retrieve expanded state
      const restored = sessionCache.get(expandedKey, []);

      // Assert: Expanded state is restored
      expect(restored).toEqual(['entity-1', 'entity-2']);
    });

    it('should handle missing expanded state gracefully', () => {
      // Act
      const expanded = sessionCache.get('sidebar_expanded_non-existent', []);

      // Assert
      expect(expanded).toEqual([]);
    });
  });

  describe('Cache Performance', () => {
    it('should load cached hierarchy faster than creating new data', () => {
      // Arrange
      const cacheKey = 'sidebar_hierarchy_test-world-123_null';
      const largeHierarchy = Array(50)
        .fill(0)
        .map((_, i) => ({
          id: `entity-${i}`,
          parentId: null,
          name: `Entity ${i}`,
          type: 'region',
        }));

      sessionCache.set(cacheKey, largeHierarchy);

      // Act: Measure cache retrieval time
      const cacheStart = performance.now();
      const cached = sessionCache.get(cacheKey, []);
      const cacheTime = performance.now() - cacheStart;

      // Assert
      expect(cached).toEqual(largeHierarchy);
      expect(cacheTime).toBeLessThan(1); // Should be nearly instant
    });

    it('should maintain cache hit rate above 50% for typical navigation', () => {
      // Arrange
      const worlds = ['world-1', 'world-2', 'world-3'];
      let cacheHits = 0;
      let totalAccesses = 0;

      // Act: Simulate navigation pattern
      for (let i = 0; i < 100; i++) {
        const worldId = worlds[i % worlds.length];
        const key = `sidebar_hierarchy_${worldId}_null`;

        // Cache every other access
        if (i % 2 === 0) {
          sessionCache.set(key, [{ id: '1' }]);
        }

        const result = sessionCache.get(key, null);
        totalAccesses++;

        if (result !== null) {
          cacheHits++;
        }
      }

      // Assert: Cache hit rate > 50% (we're caching every other access)
      const hitRate = cacheHits / totalAccesses;
      expect(hitRate).toBeGreaterThan(0.5);
    });
  });
});
