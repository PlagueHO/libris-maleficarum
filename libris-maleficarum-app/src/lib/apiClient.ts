/**
 * API Client with Retry Logic
 *
 * Axios instance configured with exponential backoff retry logic for transient failures.
 * Respects Retry-After headers for 429 Too Many Requests responses.
 *
 * @see https://github.com/softonic/axios-retry
 */

import axios, { type AxiosInstance } from 'axios';
import axiosRetry from 'axios-retry';
import { logger } from './logger';

/**
 * Base URL for API requests
 *
 * Priority:
 * 1. VITE_API_BASE_URL env var (set explicitly for MSW or real backend)
 * 2. Default to empty string (uses Vite proxy → Aspire backend)
 * 3. Fallback to localhost:5000 (production default)
 *
 * @see vite.config.ts for proxy configuration
 */

// Debug: Log all environment variables to help troubleshoot Aspire integration
logger.debug('API', 'API Client Configuration', {
  mode: import.meta.env.MODE,
  hasAspireBackend: import.meta.env.VITE_HAS_ASPIRE_BACKEND,
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL,
});

const baseURL =
  import.meta.env.VITE_API_BASE_URL ||
  (import.meta.env.MODE === 'development' ? '' : 'http://localhost:5000');

logger.debug('API', `Final baseURL: ${baseURL || '(empty - using Vite proxy)'}`);

/**
 * Axios instance with retry configuration
 *
 * Features:
 * - 10-second timeout
 * - JSON request/response format
 * - Automatic retry on transient errors (5xx, 429, timeouts)
 * - Exponential backoff with Retry-After header support
 */
export const apiClient: AxiosInstance = axios.create({
  baseURL,
  timeout: 10000, // 10 seconds
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  },
});

/**
 * Configure axios-retry for automatic retry with exponential backoff
 */
axiosRetry(apiClient, {
  retries: 3, // Maximum 3 retry attempts
  retryDelay: (retryCount, error) => {
    // Respect Retry-After header for 429 Too Many Requests
    const retryAfter = error.response?.headers?.['retry-after'];
    if (retryAfter && error.response?.status === 429) {
      const delaySeconds = parseInt(retryAfter, 10);
      if (!isNaN(delaySeconds)) {
        return delaySeconds * 1000; // Convert to milliseconds
      }
    }

    // Exponential backoff: 1s, 2s, 4s
    return axiosRetry.exponentialDelay(retryCount);
  },
  retryCondition: (error) => {
    // Retry on network errors and timeouts
    if (axiosRetry.isNetworkOrIdempotentRequestError(error)) {
      return true;
    }

    // Retry on 429 Too Many Requests (rate limiting)
    if (error.response?.status === 429) {
      return true;
    }

    // Retry on 5xx server errors (transient failures)
    if (error.response?.status && error.response.status >= 500) {
      return true;
    }

    // Do NOT retry on 4xx client errors (except 429)
    return false;
  },
  onRetry: (retryCount, error, requestConfig) => {
    logger.warn('API', `Retrying request (attempt ${retryCount}/3)`, {
      url: requestConfig.url,
      method: requestConfig.method?.toUpperCase(),
      status: error.response?.status,
    });
  },
});

// Add request interceptor for logging outgoing API calls
apiClient.interceptors.request.use(
  (config) => {
    // Log all outgoing API requests
    logger.apiRequest(
      config.method?.toUpperCase() || 'UNKNOWN',
      config.url || 'unknown',
      {
        params: config.params,
        hasData: !!config.data,
      }
    );
    return config;
  },
  (error) => {
    logger.error('API', 'Request failed before sending', { error });
    return Promise.reject(error);
  }
);

// Add response interceptor for logging responses and errors
apiClient.interceptors.response.use(
  (response) => {
    // Log successful API responses
    logger.apiResponse(
      response.config.url || 'unknown',
      response.status,
      {
        method: response.config.method?.toUpperCase(),
        hasData: !!response.data,
      }
    );
    return response;
  },
  (error) => {
    // Log API errors for debugging
    if (error.response) {
      logger.apiError(error.config?.url || 'unknown', error, {
        method: error.config?.method?.toUpperCase(),
        status: error.response.status,
        data: error.response.data,
      });
    } else if (error.request) {
      logger.error('API', 'Network error', {
        url: error.config?.url,
        method: error.config?.method?.toUpperCase(),
        message: error.message,
      });
    }
    return Promise.reject(error);
  }
);
