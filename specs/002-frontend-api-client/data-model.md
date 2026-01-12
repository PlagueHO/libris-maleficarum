# Data Model Documentation

## World Entity

### Core Entity Structure

```typescript
interface World {
  id: string;              // UUID (GUID format)
  name: string;            // Display name (e.g., "Middle Earth")
  description: string;     // Rich text description
  ownerId: string;         // User ID who owns this world
  createdAt: string;       // ISO 8601 timestamp
  updatedAt: string;       // ISO 8601 timestamp
  isDeleted: boolean;      // Soft delete flag
}
```

### Request/Response Wrappers

```typescript
// List response
interface WorldListResponse {
  data: World[];
  meta: {
    requestId: string;     // For request tracing
    timestamp: string;     // Response generation time
  };
}

// Single item response
interface WorldResponse {
  data: World;
  meta: {
    requestId: string;
    timestamp: string;
  };
}

// Create request (POST /api/worlds)
interface CreateWorldRequest {
  name: string;
  description: string;
  // ownerId derived from authenticated user
}

// Update request (PUT /api/worlds/{id})
interface UpdateWorldRequest {
  name?: string;           // Optional: partial updates supported
  description?: string;
}
```

## Error Structure (RFC 7807)

### ProblemDetails

```typescript
interface ProblemDetails {
  type?: string;           // URI reference to error type
  title: string;           // Human-readable summary
  status: number;          // HTTP status code
  detail?: string;         // Specific error explanation
  instance?: string;       // URI of specific occurrence
  errors?: Record<string, string[]>; // Validation errors (ASP.NET)
}
```

### Common Error Scenarios

**400 Bad Request - Validation Error**:

```json
{
  "type": "https://api.librismaleficarum.com/errors/validation",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred",
  "errors": {
    "Name": ["The Name field is required."],
    "Description": ["Description must be between 10 and 5000 characters."]
  }
}
```

**404 Not Found**:

```json
{
  "type": "https://api.librismaleficarum.com/errors/not-found",
  "title": "Resource Not Found",
  "status": 404,
  "detail": "World with ID '123' not found",
  "instance": "/api/worlds/123"
}
```

**429 Too Many Requests**:

```json
{
  "type": "https://api.librismaleficarum.com/errors/rate-limit",
  "title": "Rate Limit Exceeded",
  "status": 429,
  "detail": "Too many requests. Please retry after 30 seconds."
}
```

Response includes `Retry-After: 30` header (axios-retry respects this automatically).

**500 Internal Server Error**:

```json
{
  "type": "https://api.librismaleficarum.com/errors/server-error",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An unexpected error occurred. Request ID: abc-123"
}
```

## Entity Relationships (Future)

### WorldEntity Hierarchy

```typescript
interface WorldEntity {
  id: string;
  worldId: string;         // Parent world
  parentId: string | null; // Parent entity (null for root)
  type: WorldEntityType;   // Continent, Country, Region, etc.
  name: string;
  description: string;
  ownerId: string;
  createdAt: string;
  updatedAt: string;
  isDeleted: boolean;
}

enum WorldEntityType {
  Continent = 'Continent',
  Country = 'Country',
  Region = 'Region',
  City = 'City',
  Location = 'Location',
  Character = 'Character',
  Organization = 'Organization',
  Item = 'Item',
  Event = 'Event',
  Custom = 'Custom',
}
```

**Note**: WorldEntity endpoints not yet implemented (YAGNI principle). Add via `worldEntityApi.ts` when needed following the same pattern as `worldApi.ts`.

## Cache Tag Structure

### Tag Types

```typescript
tagTypes: ['World', 'Character', 'Location', 'Organization']
```

### Tag Patterns

**List Tag**:

```typescript
{ type: 'World', id: 'LIST' }
```

Invalidated by: `createWorld`, `deleteWorld`

**Item Tag**:

```typescript
{ type: 'World', id: worldId }
```

Invalidated by: `updateWorld(worldId)`, `deleteWorld(worldId)`

**Composite Tags** (Queries provide multiple tags):

```typescript
providesTags: (result) =>
  result
    ? [
        ...result.map(({ id }) => ({ type: 'World' as const, id })),
        { type: 'World', id: 'LIST' },
      ]
    : [{ type: 'World', id: 'LIST' }]
```

**Mutation Invalidation**:

```typescript
// createWorld invalidates LIST only
invalidatesTags: [{ type: 'World', id: 'LIST' }]

// updateWorld invalidates specific item + LIST
invalidatesTags: (result, error, { id }) => [
  { type: 'World', id },
  { type: 'World', id: 'LIST' },
]

// deleteWorld invalidates specific item + LIST
invalidatesTags: (result, error, id) => [
  { type: 'World', id },
  { type: 'World', id: 'LIST' },
]
```

## Type Safety Guarantees

1. **Compile-time errors**: Invalid property access causes TypeScript errors
1. **IntelliSense**: Full autocomplete for request/response structures
1. **Type guards**: `isProblemDetails()` validates error objects at runtime
1. **Generic preservation**: Types maintained through `transformResponse`

## Data Flow

```text
Component
  ↓ useGetWorldsQuery()
RTK Query
  ↓ axiosBaseQuery({ url: '/api/worlds' })
Axios Client
  ↓ GET http://localhost:5000/api/worlds (with retry logic)
Backend API
  ↓ WorldListResponse
Axios Client
  ↓ transformResponse → World[]
RTK Query (cached with tags)
  ↓ { data: World[], isLoading: false, isSuccess: true }
Component
```

Mutations follow same flow but trigger cache invalidation upon success.
