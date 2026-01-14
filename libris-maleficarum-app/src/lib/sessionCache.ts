/**
 * sessionStorage cache utilities with TTL (Time-To-Live) support
 * 
 * Provides caching for World Sidebar hierarchy data, expanded state, and selected entities.
 * Cache is automatically cleared on tab/browser close (sessionStorage behavior).
 * 
 * @module sessionCache
 */

interface CacheEntry<T> {
  data: T;
  timestamp: number;
  ttl: number; // milliseconds
}

const DEFAULT_TTL = 300000; // 5 minutes in milliseconds

/**
 * Retrieve cached data if valid (not expired)
 * 
 * @param key - Cache key (e.g., 'sidebar_hierarchy_{worldId}')
 * @param defaultValue - Value to return if cache miss or expired
 * @returns Cached data or defaultValue
 */
export function get<T>(key: string, defaultValue: T): T {
  try {
    const cached = sessionStorage.getItem(key);
    if (!cached) {
      return defaultValue;
    }

    const entry: CacheEntry<T> = JSON.parse(cached);
    const now = Date.now();
    const isExpired = now - entry.timestamp > entry.ttl;

    if (isExpired) {
      sessionStorage.removeItem(key);
      return defaultValue;
    }

    return entry.data;
  } catch (error) {
    console.error(`Failed to retrieve cache for key "${key}":`, error);
    return defaultValue;
  }
}

/**
 * Store data in sessionStorage with TTL
 * 
 * @param key - Cache key
 * @param data - Data to cache
 * @param ttl - Time-to-live in milliseconds (default: 5 minutes)
 */
export function set<T>(key: string, data: T, ttl: number = DEFAULT_TTL): void {
  try {
    const entry: CacheEntry<T> = {
      data,
      timestamp: Date.now(),
      ttl,
    };

    sessionStorage.setItem(key, JSON.stringify(entry));
  } catch (error) {
    console.error(`Failed to cache data for key "${key}":`, error);
  }
}

/**
 * Invalidate (remove) cache entry
 * 
 * @param key - Cache key to invalidate
 */
export function invalidate(key: string): void {
  try {
    sessionStorage.removeItem(key);
  } catch (error) {
    console.error(`Failed to invalidate cache for key "${key}":`, error);
  }
}

/**
 * Invalidate all cache entries matching a pattern
 * 
 * @param pattern - RegExp pattern to match keys (e.g., /^sidebar_.*_worldId123$/)
 */
export function invalidatePattern(pattern: RegExp): void {
  try {
    const keys = Object.keys(sessionStorage);
    keys.forEach((key) => {
      if (pattern.test(key)) {
        sessionStorage.removeItem(key);
      }
    });
  } catch (error) {
    console.error(`Failed to invalidate cache pattern:`, error);
  }
}

/**
 * Clear all sessionStorage cache entries
 * Use with caution - removes all cached data for the app
 */
export function clearAll(): void {
  try {
    sessionStorage.clear();
  } catch (error) {
    console.error(`Failed to clear all cache:`, error);
  }
}
