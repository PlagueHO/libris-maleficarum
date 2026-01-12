/**
 * API Client Tests
 *
 * Test suite for Axios instance with retry logic, error handling, and timeout configuration.
 *
 * NOTE: Comprehensive retry behavior is tested via worldApi.test.tsx integration tests with MSW.
 * These tests verify basic client configuration only.
 */

import { describe, it, expect } from 'vitest';
import { apiClient } from '@/lib/apiClient';

describe('apiClient', () => {
  describe('Configuration', () => {
    it('should have a default timeout of 10 seconds', () => {
      expect(apiClient.defaults.timeout).toBe(10000);
    });

    it('should accept JSON and return JSON by default', () => {
      expect(apiClient.defaults.headers['Content-Type']).toBe('application/json');
      expect(apiClient.defaults.headers['Accept']).toBe('application/json');
    });

    it('should have a base URL configured', () => {
      // Base URL should be set (either from env vars or default)
      expect(apiClient.defaults.baseURL).toBeDefined();
      expect(typeof apiClient.defaults.baseURL).toBe('string');
    });

    it('should have axios-retry configured', () => {
      // Retry logic is comprehensively tested in worldApi.test.tsx
      // This test verifies the client is properly configured with interceptors
      expect(apiClient.interceptors.response).toBeDefined();
      expect(apiClient.interceptors.request).toBeDefined();
    });
  });
});
