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

/**
 * Base URL for API requests
 *
 * In development mode (with Vite), uses relative URLs that are proxied by Vite dev server.
 * The Vite proxy (configured in vite.config.ts) forwards /api/* requests to the Aspire-managed API service.
 *
 * In production, uses VITE_API_BASE_URL environment variable.
 *
 * @see vite.config.ts for proxy configuration
 */
const baseURL =
  import.meta.env.MODE === 'development'
    ? '' // Use relative URLs in development (proxied by Vite)
    : import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000';

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
    // Log retry attempts for debugging
    console.warn(
      `Retrying request (attempt ${retryCount}/${3}):`,
      {
        url: requestConfig.url,
        method: requestConfig.method,
        status: error.response?.status,
        message: error.message,
      }
    );
  },
});

// Add response interceptor for logging errors
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    // Log API errors for debugging
    if (error.response) {
      console.error('API Error:', {
        url: error.config?.url,
        method: error.config?.method,
        status: error.response.status,
        data: error.response.data,
      });
    } else if (error.request) {
      console.error('Network Error:', {
        url: error.config?.url,
        message: error.message,
      });
    }
    return Promise.reject(error);
  }
);
