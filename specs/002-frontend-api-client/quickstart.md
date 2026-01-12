# Quickstart Guide: Adding New API Endpoints

This guide walks you through adding a new REST API endpoint to the Libris Maleficarum frontend using RTK Query.

**Estimated Time**: < 10 minutes per endpoint

## Example: Adding Character Endpoints

### Step 1. Define TypeScript Types (2 min)

Create types in `src/services/types/character.types.ts`:

```typescript
export interface Character {
  id: string;
  worldId: string;
  name: string;
  description: string;
  biography?: string;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
}

export interface CharacterListResponse {
  data: Character[];
  meta: {
    requestId: string;
    timestamp: string;
  };
}

export interface CharacterResponse {
  data: Character;
  meta: {
    requestId: string;
    timestamp: string;
  };
}

export interface CreateCharacterRequest {
  worldId: string;
  name: string;
  description: string;
  biography?: string;
}

export interface UpdateCharacterRequest {
  name?: string;
  description?: string;
  biography?: string;
}
```

Add to `src/services/types/index.ts`:

```typescript
export type { Character, CharacterListResponse, CharacterResponse, CreateCharacterRequest, UpdateCharacterRequest } from './character.types';
```

### Step 2. Create API Slice (5 min)

Create `src/services/characterApi.ts`:

```typescript
import { api } from './api';
import type {
  Character,
  CharacterListResponse,
  CharacterResponse,
  CreateCharacterRequest,
  UpdateCharacterRequest,
} from './types';

export const characterApi = api.injectEndpoints({
  endpoints: (builder) => ({
    // GET /api/characters?worldId={worldId}
    getCharacters: builder.query<Character[], string>({
      query: (worldId) => ({
        url: '/api/characters',
        method: 'GET',
        params: { worldId },
      }),
      transformResponse: (response: CharacterListResponse) => response.data,
      providesTags: (result) =>
        result
          ? [
              ...result.map(({ id }) => ({ type: 'Character' as const, id })),
              { type: 'Character', id: 'LIST' },
            ]
          : [{ type: 'Character', id: 'LIST' }],
    }),

    // GET /api/characters/{id}
    getCharacterById: builder.query<Character, string>({
      query: (id) => ({
        url: `/api/characters/${id}`,
        method: 'GET',
      }),
      transformResponse: (response: CharacterResponse) => response.data,
      providesTags: (result, error, id) => [{ type: 'Character', id }],
    }),

    // POST /api/characters
    createCharacter: builder.mutation<Character, CreateCharacterRequest>({
      query: (body) => ({
        url: '/api/characters',
        method: 'POST',
        data: body,
      }),
      transformResponse: (response: CharacterResponse) => response.data,
      invalidatesTags: [{ type: 'Character', id: 'LIST' }],
    }),

    // PUT /api/characters/{id}
    updateCharacter: builder.mutation<
      Character,
      { id: string; data: UpdateCharacterRequest }
    >({
      query: ({ id, data }) => ({
        url: `/api/characters/${id}`,
        method: 'PUT',
        data,
      }),
      transformResponse: (response: CharacterResponse) => response.data,
      invalidatesTags: (result, error, { id }) => [
        { type: 'Character', id },
        { type: 'Character', id: 'LIST' },
      ],
    }),

    // DELETE /api/characters/{id}
    deleteCharacter: builder.mutation<void, string>({
      query: (id) => ({
        url: `/api/characters/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: (result, error, id) => [
        { type: 'Character', id },
        { type: 'Character', id: 'LIST' },
      ],
    }),
  }),
});

export const {
  useGetCharactersQuery,
  useGetCharacterByIdQuery,
  useCreateCharacterMutation,
  useUpdateCharacterMutation,
  useDeleteCharacterMutation,
} = characterApi;
```

**Note**: 'Character' tag type already exists in `src/services/api.ts` tagTypes array.

### Step 3. Use in Component (1 min)

```typescript
import { useGetCharactersQuery, useCreateCharacterMutation } from '@/services/characterApi';

function CharacterList({ worldId }: { worldId: string }) {
  const { data: characters, isLoading, isError, error } = useGetCharactersQuery(worldId);
  const [createCharacter, { isLoading: isCreating }] = useCreateCharacterMutation();

  const handleCreate = async () => {
    await createCharacter({
      worldId,
      name: 'Aragorn',
      description: 'Ranger of the North',
    });
    // Cache automatically refetches - no manual state update needed!
  };

  if (isLoading) return <div>Loading...</div>;
  if (isError) return <div>Error: {error.data.title}</div>;

  return (
    <div>
      {characters?.map((char) => (
        <div key={char.id}>{char.name}</div>
      ))}
      <button onClick={handleCreate} disabled={isCreating}>
        Create Character
      </button>
    </div>
  );
}
```

### Step 4. Write Tests (Optional, 3 min)

Create `src/__tests__/services/characterApi.test.tsx`:

```typescript
import { renderHook, waitFor } from '@testing-library/react';
import { setupServer } from 'msw/node';
import { http, HttpResponse } from 'msw';
import { useGetCharactersQuery } from '@/services/characterApi';
import { createTestWrapper } from './testUtils'; // Shared test wrapper

const server = setupServer(
  http.get('http://localhost:5000/api/characters', () => {
    return HttpResponse.json({
      data: [{ id: '1', worldId: 'world-1', name: 'Frodo', /* ... */ }],
      meta: { requestId: 'test-1', timestamp: new Date().toISOString() },
    });
  })
);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

it('should fetch characters for a world', async () => {
  const { result } = renderHook(() => useGetCharactersQuery('world-1'), {
    wrapper: createTestWrapper(),
  });

  await waitFor(() => expect(result.current.isSuccess).toBe(true));
  expect(result.current.data).toHaveLength(1);
  expect(result.current.data?.[0].name).toBe('Frodo');
});
```

## Checklist

- [ ] Create types in `src/services/types/{entity}.types.ts`
- [ ] Export types from `src/services/types/index.ts`
- [ ] Create API slice in `src/services/{entity}Api.ts`
- [ ] Define query endpoints with `providesTags`
- [ ] Define mutation endpoints with `invalidatesTags`
- [ ] Export auto-generated hooks
- [ ] Use hooks in component with destructured state
- [ ] (Optional) Write tests with MSW

## Common Patterns

### Query with Filters

```typescript
getCharacters: builder.query<Character[], { worldId: string; search?: string }>({
  query: ({ worldId, search }) => ({
    url: '/api/characters',
    params: { worldId, search },
  }),
  // ...
}),
```

### Optimistic Updates

```typescript
updateCharacter: builder.mutation({
  // ...
  async onQueryStarted({ id, data }, { dispatch, queryFulfilled }) {
    const patchResult = dispatch(
      characterApi.util.updateQueryData('getCharacterById', id, (draft) => {
        Object.assign(draft, data);
      })
    );
    try {
      await queryFulfilled;
    } catch {
      patchResult.undo();
    }
  },
}),
```

### Polling

```typescript
const { data } = useGetCharactersQuery(worldId, {
  pollingInterval: 30000, // Refetch every 30 seconds
});
```

### Manual Refetch

```typescript
const { data, refetch } = useGetCharactersQuery(worldId);

<button onClick={() => refetch()}>Refresh</button>
```

### Conditional Fetch

```typescript
const { data } = useGetCharactersQuery(worldId, {
  skip: !worldId, // Don't fetch until worldId is available
});
```

## Troubleshooting

### Cache not invalidating after mutation

- Verify `invalidatesTags` matches `providesTags` type
- Check tag IDs are consistent (`id` vs `characterId`)
- Ensure mutation succeeded (check network tab)

### TypeScript errors on hook usage

- Verify types exported from `types/index.ts`
- Check generic types on `builder.query<ResponseType, ArgType>`
- Run `pnpm type-check` for full error details

### MSW not intercepting requests

- Use full URL: `http://localhost:5000/api/...` not `/api/...`
- Verify server.listen() called in beforeAll
- Check MSW handler method matches (GET vs POST)

### Retries not working for endpoint

- Axios retry only applies to network errors, 5xx, and 429
- 4xx errors fail fast (by design)
- Check console for retry logs

## Additional Resources

- [RTK Query Tutorials](https://redux-toolkit.js.org/rtk-query/overview)
- [Existing worldApi.ts](../../libris-maleficarum-app/src/services/worldApi.ts) - Full example
- [Type definitions](../../libris-maleficarum-app/src/services/types/) - All entity types
- [Research doc](./research.md) - Patterns and decisions
