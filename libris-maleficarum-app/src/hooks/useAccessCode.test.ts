/// <reference types="vitest/globals" />

import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { useAccessCode } from './useAccessCode';

// Mock dependencies
vi.mock('@/services/configService', () => ({
  getAccessStatus: vi.fn(),
}));

vi.mock('@/lib/apiClient', () => ({
  setAccessCode: vi.fn(),
  getAccessCode: vi.fn(() => null),
  apiClient: {
    get: vi.fn(),
  },
}));

vi.mock('@/lib/logger', () => ({
  logger: {
    warn: vi.fn(),
    debug: vi.fn(),
    error: vi.fn(),
    info: vi.fn(),
  },
}));

import { getAccessStatus } from '@/services/configService';
import { setAccessCode, apiClient } from '@/lib/apiClient';

const mockGetAccessStatus = vi.mocked(getAccessStatus);
const mockSetAccessCode = vi.mocked(setAccessCode);
const mockApiClientGet = vi.mocked(apiClient.get);

describe('useAccessCode', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    sessionStorage.clear();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('should set isVerified to true immediately when access code is not required', async () => {
    mockGetAccessStatus.mockResolvedValue({ accessCodeRequired: false });

    const { result } = renderHook(() => useAccessCode());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.accessCodeRequired).toBe(false);
    expect(result.current.isVerified).toBe(true);
    expect(result.current.error).toBeNull();
  });

  it('should check cached code when access code is required', async () => {
    mockGetAccessStatus.mockResolvedValue({ accessCodeRequired: true });
    sessionStorage.setItem('libris-access-code', 'cached-code');
    mockApiClientGet.mockResolvedValue({ data: [] });

    const { result } = renderHook(() => useAccessCode());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.isVerified).toBe(true);
    expect(mockSetAccessCode).toHaveBeenCalledWith('cached-code');
  });

  it('should clear invalid cached code and show dialog', async () => {
    mockGetAccessStatus.mockResolvedValue({ accessCodeRequired: true });
    sessionStorage.setItem('libris-access-code', 'invalid-code');
    mockApiClientGet.mockRejectedValue({ response: { status: 401 } });

    const { result } = renderHook(() => useAccessCode());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    expect(result.current.isVerified).toBe(false);
    expect(result.current.accessCodeRequired).toBe(true);
    expect(sessionStorage.getItem('libris-access-code')).toBeNull();
  });

  it('should verify and store code on successful submitCode', async () => {
    mockGetAccessStatus.mockResolvedValue({ accessCodeRequired: true });
    mockApiClientGet.mockResolvedValue({ data: [] });

    const { result } = renderHook(() => useAccessCode());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    await act(async () => {
      await result.current.submitCode('valid-code');
    });

    expect(result.current.isVerified).toBe(true);
    expect(result.current.error).toBeNull();
    expect(sessionStorage.getItem('libris-access-code')).toBe('valid-code');
  });

  it('should set error on invalid submitCode', async () => {
    mockGetAccessStatus.mockResolvedValue({ accessCodeRequired: true });
    // Submit call returns 401
    mockApiClientGet.mockRejectedValue({ response: { status: 401 } });

    const { result } = renderHook(() => useAccessCode());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    await act(async () => {
      await result.current.submitCode('wrong-code');
    });

    expect(result.current.isVerified).toBe(false);
    expect(result.current.error).toBe('Invalid access code. Please try again.');
    expect(mockSetAccessCode).toHaveBeenCalledWith('wrong-code');
    expect(mockSetAccessCode).toHaveBeenCalledWith(null);
  });

  it('should set server error on non-401 submitCode failure', async () => {
    mockGetAccessStatus.mockResolvedValue({ accessCodeRequired: true });
    mockApiClientGet.mockRejectedValue(new Error('Network Error'));

    const { result } = renderHook(() => useAccessCode());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    await act(async () => {
      await result.current.submitCode('some-code');
    });

    expect(result.current.isVerified).toBe(false);
    expect(result.current.error).toBe('Unable to connect to the server.');
  });

  it('should handle server unreachable during status check', async () => {
    mockGetAccessStatus.mockRejectedValue(new Error('Network Error'));

    const { result } = renderHook(() => useAccessCode());

    await waitFor(() => {
      expect(result.current.isLoading).toBe(false);
    });

    // Should let user through when server is unreachable
    expect(result.current.isVerified).toBe(true);
  });
});
