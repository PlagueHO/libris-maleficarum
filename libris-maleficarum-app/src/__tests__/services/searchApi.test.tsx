import { afterAll, afterEach, beforeAll, describe, expect, it } from 'vitest';
import { configureStore } from '@reduxjs/toolkit';
import { Provider } from 'react-redux';
import { renderHook, waitFor } from '@testing-library/react';
import type { ReactNode } from 'react';
import { setupServer } from 'msw/node';

import { api } from '@/services/api';
import { useSearchEntitiesQuery } from '@/services/searchApi';
import { handlers } from '@/__tests__/mocks/handlers';

const server = setupServer(...handlers);

beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

function createTestWrapper() {
  const store = configureStore({
    reducer: {
      [api.reducerPath]: api.reducer,
    },
    middleware: (getDefaultMiddleware) => getDefaultMiddleware().concat(api.middleware),
  });

  function TestWrapper({ children }: { children: ReactNode }) {
    return <Provider store={store}>{children}</Provider>;
  }

  return TestWrapper;
}

describe('Search API', () => {
  it('returns world-scoped search results for the canonical search endpoint', async () => {
    const { result } = renderHook(
      () => useSearchEntitiesQuery({ worldId: 'test-world-123', q: 'Cormyr', limit: 8 }),
      { wrapper: createTestWrapper() },
    );

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.data?.meta.totalCount).toBeGreaterThanOrEqual(1);
    expect(result.current.data?.data[0]?.name).toBe('Cormyr');
    expect(result.current.data?.data[0]?.path).toEqual(['continent-faerun']);
  });
});
