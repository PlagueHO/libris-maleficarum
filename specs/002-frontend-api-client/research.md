## API Client Research Documentation

### Overview

This document captures research findings, patterns, and decisions made during the implementation of the frontend API client infrastructure for Libris Maleficarum.

## RTK Query Patterns

### Why RTK Query Over Custom Fetch

**Decision**: Use RTK Query instead of manual fetch/axios calls in components

**Rationale**:

- **Automatic caching**: Eliminates redundant API calls when multiple components request the same data
- **Loading state management**: Built-in `isLoading`, `isFetching`, `isSuccess`, `isError` states
- **Cache invalidation**: Tag-based system automatically refetches stale data after mutations
- **DevTools integration**: Full visibility into cache state, queries, and mutations via Redux DevTools
- **Type safety**: Full TypeScript support with generics for request/response types
- **Code reduction**: ~70% less code compared to manual fetch implementation

### Code Splitting with `injectEndpoints`

**Pattern**: Define endpoints in feature-specific files using `api.injectEndpoints()`

```typescript
// ✅ Good: Feature-specific endpoints in separate files
export const worldApi = api.injectEndpoints({
  endpoints: (builder) => ({
    getWorlds: builder.query<World[], void>({ ... }),
  }),
});
```

**Benefits**:

- Bundle optimization: Endpoints only included when feature is used
- Better organization: Domain logic stays with domain models
- Easier testing: Can mock/test endpoints independently

### Custom Base Query with Axios

**Decision**: Use custom `axiosBaseQuery` instead of RTK Query's `fetchBaseQuery`

**Rationale**:

- **Retry support**: `fetchBaseQuery` doesn't support automatic retries; axios-retry provides exponential backoff
- **Interceptors**: Axios interceptors enable centralized logging and error transformation
- **Retry-After header**: axios-retry respects `Retry-After` header for 429 rate limit responses
- **Existing patterns**: Team familiarity with Axios configuration

**Implementation**:

```typescript
const axiosBaseQuery = (): BaseQueryFn<...> => {
  return async ({ url, method, data, params, headers }, { signal }) => {
    const result = await apiClient({ url, method, data, params, headers, signal });
    return { data: result.data };
  };
};
```

### Cache Tag Strategy

**Pattern**: Use composite tags for fine-grained invalidation

```typescript
providesTags: (result) =>
  result
    ? [
        ...result.map(({ id }) => ({ type: 'World' as const, id })),
        { type: 'World', id: 'LIST' },
      ]
    : [{ type: 'World', id: 'LIST' }],
```

**Benefits**:

- Specific world updates only invalidate that world's cache
- List updates invalidate both the list AND all individual items
- Prevents stale data after mutations

## Aspire Integration

### Service Discovery via Environment Variables

**Pattern**: Aspire AppHost injects `APISERVICE_HTTPS` and `APISERVICE_HTTP` environment variables

**Vite Proxy Configuration**:

```typescript
server: {
  proxy: {
    '/api': {
      target: process.env.APISERVICE_HTTPS || 
              process.env.APISERVICE_HTTP || 
              process.env.VITE_API_BASE_URL || 
              'http://localhost:5000',
      changeOrigin: true,
      secure: false,
    },
  },
}
```

**ApiClient BaseURL**:

```typescript
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 
           import.meta.env.APISERVICE_HTTPS || 
           import.meta.env.APISERVICE_HTTP || 
           'http://localhost:5000',
});
```

**Key Findings**:

1. **Development**: Aspire auto-injects service URLs when running via AppHost
1. **Production**: Use `VITE_API_BASE_URL` for deployed environments (Azure Container Apps, Static Web Apps)
1. **Precedence**: `VITE_API_BASE_URL` > `APISERVICE_HTTPS` > `APISERVICE_HTTP` > fallback
1. **Security**: Vite only exposes env vars prefixed with `VITE_` to client code; Aspire vars accessible during build

## Retry Strategies

### Exponential Backoff Configuration

**Implementation**: axios-retry with custom retry delay function

```typescript
axiosRetry(apiClient, {
  retries: 3,
  retryDelay: (retryCount, error) => {
    // Check for Retry-After header (429 rate limiting)
    const retryAfter = error.response?.headers['retry-after'];
    if (retryAfter) {
      return parseInt(retryAfter, 10) * 1000;
    }
    // Exponential backoff: 1s, 2s, 4s
    return Math.pow(2, retryCount - 1) * 1000;
  },
  retryCondition: (error) => {
    // Retry on 5xx, 429, network errors, timeouts
    // Do NOT retry on 4xx (except 429)
  },
});
```

### When to Retry vs Fail Fast

**Retry Scenarios**:

- `5xx` server errors (transient failures)
- `429` Too Many Requests (rate limiting)
- Network errors (`ECONNABORTED`, `ENOTFOUND`, etc.)
- Timeout errors

**Fail Fast Scenarios**:

- `400` Bad Request (client error)
- `401` Unauthorized (authentication required)
- `403` Forbidden (insufficient permissions)
- `404` Not Found (resource doesn't exist)
- `422` Validation Failed (invalid input)

**Rationale**: Client errors indicate problems with the request itself; retrying won't help.

### Retry Logging

**Pattern**: Log retry attempts via `onRetry` callback

```typescript
onRetry: (retryCount, error, requestConfig) => {
  console.warn(`Retrying request (attempt ${retryCount}/3):`, {
    url: requestConfig.url,
    method: requestConfig.method,
    status: error.response?.status,
    message: error.message,
  });
},
```

**Benefits**:

- Debugging transient failures in production
- Monitoring retry frequency (potential backend issues)
- Identifying rate limiting patterns

## Error Handling

### RFC 7807 Problem Details

**Pattern**: ASP.NET Core returns errors in RFC 7807 format

```typescript
interface ProblemDetails {
  type?: string;
  title: string;
  status: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>; // ASP.NET validation errors
}
```

**Benefits**:

- Standardized error structure across all API endpoints
- Machine-readable error types (e.g., `https://api.com/errors/validation`)
- Human-readable titles and details for UI display
- Validation errors include field-specific messages

### Error Transformation in axiosBaseQuery

**Pattern**: Transform AxiosError into RTK Query error format

```typescript
catch (axiosError) {
  const err = axiosError as AxiosError<ProblemDetails>;
  return {
    error: {
      status: err.response?.status,
      data: err.response?.data || {
        title: 'Network Error',
        status: 0,
        detail: err.message,
      },
    },
  };
}
```

**Benefits**:

- Components receive consistent error shape
- TypeScript knows error structure (typed as `ProblemDetails`)
- Network errors transformed into same format as API errors

## Request Cancellation

### AbortController Integration

**Pattern**: Pass RTK Query's abort signal to Axios

```typescript
const axiosBaseQuery = () => {
  return async ({ url, method, data, params, headers }, { signal }) => {
    const result = await apiClient({
      url, method, data, params, headers,
      signal, // RTK Query provides AbortSignal
    });
  };
};
```

**Benefits**:

- Automatic cancellation when component unmounts
- Prevents memory leaks from in-flight requests
- Avoids setting state on unmounted components
- Cancellation when query is superseded by new request

### Handling Cancelled Requests

```typescript
if (err.code === 'ERR_CANCELED') {
  return {
    error: {
      status: 0,
      data: { title: 'Request Cancelled', status: 0 },
    },
  };
}
```

## Testing Patterns

### MSW for HTTP Mocking

**Pattern**: Use Mock Service Worker (msw) for realistic HTTP mocking in tests

```typescript
const server = setupServer(
  http.get('http://localhost:5000/api/worlds', () => {
    return HttpResponse.json({ data: mockWorlds });
  })
);
```

**Benefits**:

- Mocks at HTTP level (not Axios level) - more realistic
- Works with any HTTP library (Axios, fetch, etc.)
- Can test retry logic, network errors, timeout scenarios
- No need to mock Axios instance directly

### Testing Cache Invalidation

**Pattern**: Track MSW handler call counts to verify refetches

```typescript
let getWorldsCallCount = 0;
server.use(
  http.get('/api/worlds', () => {
    getWorldsCallCount++;
    return HttpResponse.json({ data: mockWorlds });
  })
);

// After mutation
await createWorld({ name: 'Test' });
expect(getWorldsCallCount).toBe(2); // Initial fetch + refetch after create
```

## Performance Considerations

### Cache Duration Tuning

**Current Settings**:

- `keepUnusedDataFor: 60` - Cache data for 60 seconds after last component unmounts
- `refetchOnMountOrArgChange: 30` - Refetch if data is older than 30 seconds

**Rationale**:

- 60s cache prevents redundant API calls during typical user navigation
- 30s refetch ensures moderately fresh data when user returns to view
- Balance between data freshness and API load

### Transform Response for Optimal Data Structure

**Pattern**: Extract nested data in `transformResponse`

```typescript
transformResponse: (response: WorldListResponse) => response.data,
```

**Benefits**:

- Components receive `World[]` directly, not `{ data: World[], meta: {...} }`
- Cleaner component code (no `.data.data` access)
- Metadata (requestId, timestamp) not needed in component layer

## Lessons Learned

1. **MSW URL Matching**: MSW handlers must match full URL including `http://localhost:5000` when apiClient uses absolute URLs
1. **JSX in .ts vs .tsx**: Vitest/esbuild requires `.tsx` extension for files with JSX, even if TypeScript compiler accepts `.ts`
1. **Test Wrapper Pattern**: Create test wrapper function (not component) to avoid React Hook violations in tests
1. **Retry Logging**: Always log retry attempts in development; invaluable for debugging transient failures
1. **Type Safety**: Full generics on endpoints prevent runtime errors; invest time in proper typing upfront
1. **AbortController**: Always pass abort signal to prevent memory leaks; RTK Query handles this automatically
1. **Cache Tag Granularity**: Use both list tags (`id: 'LIST'`) and item tags (`id: worldId`) for optimal invalidation
1. **Error Handling**: Consistent error shape (RFC 7807) simplifies component error handling across entire app

## References

- [RTK Query Overview](https://redux-toolkit.js.org/rtk-query/overview)
- [RTK Query Code Splitting](https://redux-toolkit.js.org/rtk-query/usage/code-splitting)
- [Axios Retry Documentation](https://github.com/softonic/axios-retry)
- [RFC 7807 Problem Details](https://datatracker.ietf.org/doc/html/rfc7807)
- [MSW Documentation](https://mswjs.io/docs/)
- [Microsoft Aspire Service Discovery](https://learn.microsoft.com/en-us/dotnet/aspire/service-discovery/overview)
