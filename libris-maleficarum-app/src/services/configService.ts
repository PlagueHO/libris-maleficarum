/**
 * Config Service
 *
 * API functions for application configuration endpoints.
 */

import { apiClient } from '@/lib/apiClient';
import type { AccessControlStatus } from '@/services/types';

/**
 * Fetch access control status from the backend.
 *
 * @returns Whether an access code is required.
 */
export async function getAccessStatus(): Promise<AccessControlStatus> {
  const response = await apiClient.get<AccessControlStatus>('/api/config/access-status');
  return response.data;
}
