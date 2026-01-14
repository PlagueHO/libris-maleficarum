/**
 * sessionCache Utility Tests (T044)
 *
 * Tests for sessionStorage cache utilities with TTL support.
 * Covers cache hits, cache misses, expiration, and pattern invalidation.
 *
 * @see sessionCache.ts
 */

import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import * as sessionCache from './sessionCache';

describe('sessionCache', () => {
  beforeEach(() => {
    // Clear sessionStorage before each test
    sessionStorage.clear();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  describe('get()', () => {
    it('should return cached data on cache hit', () => {
      // Arrange
      const key = 'test-key';
      const expectedData = { id: 1, name: 'Test' };
      sessionCache.set(key, expectedData);

      // Act
      const result = sessionCache.get(key, { id: 0, name: 'Default' });

      // Assert
      expect(result).toEqual(expectedData);
    });

    it('should return default value on cache miss', () => {
      // Arrange
      const key = 'non-existent-key';
      const defaultValue = { id: 0, name: 'Default' };

      // Act
      const result = sessionCache.get(key, defaultValue);

      // Assert
      expect(result).toEqual(defaultValue);
    });

    it('should return default value when cache is expired', () => {
      // Arrange
      const key = 'test-key';
      const data = { id: 1, name: 'Test' };
      const ttl = 100; // 100ms
      sessionCache.set(key, data, ttl);

      // Act - wait for cache to expire
      const delayMs = ttl + 50;
      return new Promise((resolve) => {
        setTimeout(() => {
          const defaultValue = { id: 0, name: 'Default' };
          const result = sessionCache.get(key, defaultValue);

          // Assert
          expect(result).toEqual(defaultValue);
          resolve(undefined);
        }, delayMs);
      });
    });

    it('should handle corrupted cache data gracefully', () => {
      // Arrange
      const key = 'corrupted-key';
      sessionStorage.setItem(key, 'invalid json {]');
      const defaultValue = { id: 0, name: 'Default' };

      // Act
      const result = sessionCache.get(key, defaultValue);

      // Assert
      expect(result).toEqual(defaultValue);
    });
  });

  describe('set()', () => {
    it('should store data in sessionStorage', () => {
      // Arrange
      const key = 'test-key';
      const data = { id: 1, name: 'Test' };

      // Act
      sessionCache.set(key, data);

      // Assert
      const stored = sessionStorage.getItem(key);
      expect(stored).toBeDefined();
      const parsed = JSON.parse(stored!);
      expect(parsed.data).toEqual(data);
    });

    it('should use default TTL of 5 minutes', () => {
      // Arrange
      const key = 'test-key';
      const data = { id: 1 };

      // Act
      sessionCache.set(key, data);

      // Assert
      const stored = sessionStorage.getItem(key);
      const parsed = JSON.parse(stored!);
      expect(parsed.ttl).toBe(300000); // 5 minutes in ms
    });

    it('should accept custom TTL', () => {
      // Arrange
      const key = 'test-key';
      const data = { id: 1 };
      const customTtl = 60000; // 1 minute

      // Act
      sessionCache.set(key, data, customTtl);

      // Assert
      const stored = sessionStorage.getItem(key);
      const parsed = JSON.parse(stored!);
      expect(parsed.ttl).toBe(customTtl);
    });

    it('should include timestamp when storing', () => {
      // Arrange
      const key = 'test-key';
      const data = { id: 1 };
      const beforeTime = Date.now();

      // Act
      sessionCache.set(key, data);

      // Assert
      const afterTime = Date.now();
      const stored = sessionStorage.getItem(key);
      const parsed = JSON.parse(stored!);
      expect(parsed.timestamp).toBeGreaterThanOrEqual(beforeTime);
      expect(parsed.timestamp).toBeLessThanOrEqual(afterTime);
    });

    it('should handle complex nested data structures', () => {
      // Arrange
      const key = 'nested-key';
      const data = {
        level1: {
          level2: {
            level3: [1, 2, 3],
            value: 'deep value',
          },
        },
      };

      // Act
      sessionCache.set(key, data);
      const result = sessionCache.get(key, {});

      // Assert
      expect(result).toEqual(data);
    });
  });

  describe('invalidate()', () => {
    it('should remove cache entry', () => {
      // Arrange
      const key = 'test-key';
      const data = { id: 1 };
      sessionCache.set(key, data);

      // Act
      sessionCache.invalidate(key);

      // Assert
      const result = sessionCache.get(key, { id: 0 });
      expect(result).toEqual({ id: 0 });
    });

    it('should handle invalidating non-existent keys gracefully', () => {
      // Act & Assert (should not throw)
      expect(() => sessionCache.invalidate('non-existent-key')).not.toThrow();
    });
  });

  describe('invalidatePattern()', () => {
    it('should remove all cache entries matching pattern', () => {
      // Arrange
      sessionCache.set('sidebar_hierarchy_world-1', { entities: [] });
      sessionCache.set('sidebar_hierarchy_world-2', { entities: [] });
      sessionCache.set('other_cache_key', { data: 'other' });

      // Act
      sessionCache.invalidatePattern(/^sidebar_hierarchy_world-\d+$/);

      // Assert
      const result1 = sessionCache.get('sidebar_hierarchy_world-1', null);
      const result2 = sessionCache.get('sidebar_hierarchy_world-2', null);
      const result3 = sessionCache.get('other_cache_key', { data: 'default' });

      expect(result1).toBeNull();
      expect(result2).toBeNull();
      expect(result3).toEqual({ data: 'other' });
    });

    it('should not match partial patterns', () => {
      // Arrange
      sessionCache.set('sidebar_expanded_world-1', true);
      sessionCache.set('sidebar_hierarchy_world-1', { entities: [] });

      // Act
      sessionCache.invalidatePattern(/^sidebar_hierarchy/);

      // Assert
      const expanded = sessionCache.get('sidebar_expanded_world-1', false);
      const hierarchy = sessionCache.get('sidebar_hierarchy_world-1', null);

      expect(expanded).toBe(true);
      expect(hierarchy).toBeNull();
    });

    it('should handle empty patterns gracefully', () => {
      // Arrange
      sessionCache.set('key1', { data: 1 });

      // Act & Assert (should not throw or clear everything)
      expect(() => sessionCache.invalidatePattern(/^$/)).not.toThrow();
      const result = sessionCache.get('key1', null);
      expect(result).toEqual({ data: 1 });
    });
  });

  describe('clearAll()', () => {
    it('should clear all sessionStorage entries', () => {
      // Arrange
      sessionCache.set('key1', { data: 1 });
      sessionCache.set('key2', { data: 2 });
      sessionCache.set('key3', { data: 3 });

      // Act
      sessionCache.clearAll();

      // Assert
      expect(sessionStorage.length).toBe(0);
    });

    it('should work when sessionStorage is already empty', () => {
      // Act & Assert (should not throw)
      expect(() => sessionCache.clearAll()).not.toThrow();
    });
  });

  describe('Cache Hit Rates', () => {
    it('should achieve high cache hit rate for repeated accesses', () => {
      // Arrange
      const key = 'repeated-key';
      const data = { entities: Array(50).fill({ id: 0 }) };
      sessionCache.set(key, data);

      // Act - 100 repeated accesses
      let hitCount = 0;
      for (let i = 0; i < 100; i++) {
        const result = sessionCache.get(key, null);
        if (result !== null) {
          hitCount++;
        }
      }

      // Assert - 100% hit rate
      expect(hitCount).toBe(100);
    });
  });

  describe('Performance Characteristics', () => {
    it('should retrieve cached data in <1ms', () => {
      // Arrange
      const key = 'perf-test-key';
      const largeData = { entities: Array(100).fill({ id: 0, name: 'Entity' }) };
      sessionCache.set(key, largeData);

      // Act
      const startTime = performance.now();
      sessionCache.get(key, {});
      const endTime = performance.now();

      // Assert
      expect(endTime - startTime).toBeLessThan(1);
    });

    it('should store large data structures efficiently', () => {
      // Arrange
      const key = 'large-data-key';
      const largeData = Array(1000)
        .fill(0)
        .map((_, i) => ({
          id: i,
          name: `Entity ${i}`,
          children: Array(5)
            .fill(0)
            .map((_, j) => ({ id: j, parent: i })),
        }));

      // Act
      const startTime = performance.now();
      sessionCache.set(key, largeData);
      const result = sessionCache.get(key, []);
      const endTime = performance.now();

      // Assert
      expect(result).toEqual(largeData);
      expect(endTime - startTime).toBeLessThan(10); // Should be very fast
    });
  });
});
