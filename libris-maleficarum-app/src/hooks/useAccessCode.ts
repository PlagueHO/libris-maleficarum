/**
 * useAccessCode Hook
 *
 * Manages access code verification flow for the application.
 * Checks if an access code is required, validates cached codes,
 * and provides submission functionality.
 */

import { useState, useEffect, useCallback } from 'react';
import { setAccessCode } from '@/lib/apiClient';
import { getAccessStatus } from '@/services/configService';
import { apiClient } from '@/lib/apiClient';
import { logger } from '@/lib/logger';

const SESSION_STORAGE_KEY = 'libris-access-code';

export interface UseAccessCodeResult {
  accessCodeRequired: boolean;
  isVerified: boolean;
  isLoading: boolean;
  error: string | null;
  submitCode: (code: string) => Promise<void>;
}

export function useAccessCode(): UseAccessCodeResult {
  const [accessCodeRequired, setAccessCodeRequired] = useState(false);
  const [isVerified, setIsVerified] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    async function checkAccessStatus() {
      try {
        const status = await getAccessStatus();

        if (cancelled) return;

        if (!status.accessCodeRequired) {
          setAccessCodeRequired(false);
          setIsVerified(true);
          setIsLoading(false);
          return;
        }

        setAccessCodeRequired(true);

        // Check for cached access code in sessionStorage
        const cachedCode = sessionStorage.getItem(SESSION_STORAGE_KEY);
        if (cachedCode) {
          setAccessCode(cachedCode);
          try {
            await apiClient.get('/api/v1/worlds');
            if (cancelled) return;
            setIsVerified(true);
            setIsLoading(false);
            return;
          } catch {
            // Cached code is invalid — clear it
            if (cancelled) return;
            sessionStorage.removeItem(SESSION_STORAGE_KEY);
            setAccessCode(null);
          }
        }

        setIsLoading(false);
      } catch {
        if (cancelled) return;
        logger.warn('AUTH', 'Failed to check access status, assuming not required');
        // If we can't reach the server, let the user through
        setIsVerified(true);
        setIsLoading(false);
      }
    }

    checkAccessStatus();

    return () => {
      cancelled = true;
    };
  }, []);

  const submitCode = useCallback(async (code: string) => {
    setError(null);
    setIsLoading(true);

    setAccessCode(code);

    try {
      await apiClient.get('/api/v1/worlds');
      sessionStorage.setItem(SESSION_STORAGE_KEY, code);
      setIsVerified(true);
    } catch (err: unknown) {
      setAccessCode(null);
      const axiosErr = err as { response?: { status?: number } };
      if (axiosErr?.response?.status === 401) {
        setError('Invalid access code. Please try again.');
      } else {
        setError('Unable to connect to the server.');
      }
    } finally {
      setIsLoading(false);
    }
  }, []);

  return {
    accessCodeRequired,
    isVerified,
    isLoading,
    error,
    submitCode,
  };
}
