# API Design

This document describes the RESTful API design for the Libris Maleficarum backend. The API follows REST conventions with resource-based URLs and standard HTTP methods.

> [!NOTE]
> The API implementation is **not yet fully defined**. This document outlines the high-level API structure and will be expanded with detailed specifications (request/response schemas, validation rules, error codes) during backend implementation.

## API Principles

- **RESTful Design**: Resource-based URLs with standard HTTP methods (GET, POST, PUT, DELETE)
- **JSON Format**: All requests and responses use JSON
- **Versioning**: API version included in URL path (`/api/v1/...`) for future compatibility
- **Authentication**: JWT bearer tokens (to be implemented with Entra ID CIAM)
- **Authorization**: Row-level security based on `OwnerId` and world access permissions
- **Error Handling**: Consistent error response format with appropriate HTTP status codes
- **Pagination**: Cursor-based pagination for list endpoints
- **Rate Limiting**: Per-user rate limiting to prevent abuse

## API Structure

### Base URL

```text
https://api.librismaleficarum.com/api/v1
```

### Container Mapping

The API structure maps to the 4-container Cosmos DB architecture:

| API Resource | Cosmos DB Container | Description |
| ------------ | ------------------- | ----------- |
| `/worlds` | World | Top-level world metadata and ownership |
| `/worlds/{worldId}/entities` | WorldEntity | Entities within worlds (hierarchical) |
| `/worlds/{worldId}/entities/{entityId}/assets` | Asset | Asset metadata attached to entities |
| `/worlds/{worldId}/deleted` (future) | DeletedWorldEntity | Soft-deleted entities (recovery) |

**Key Architectural Points:**

- **World** container stores only world metadata (name, description, owner)
- **WorldEntity** container stores all entities within worlds with hierarchical relationships
- **Asset** container stores asset metadata separate from entities to prevent document bloat
- World must exist before creating entities (domain rule enforced by API)

### Resource Hierarchy

The API follows the hierarchical structure of the data model:

```text
/worlds                                    # Top-level worlds owned by user
  /{worldId}                               # Specific world
    /entities                              # All entities in world
      /{entityId}                          # Specific entity
        /children                          # Children of entity
        /assets                            # Assets attached to entity
    /assets                                # All assets in world
      /{assetId}                           # Specific asset
```

## Endpoint Categories

### World Management

Operations for creating, reading, updating, and deleting worlds.

```text
GET    /api/v1/worlds                      # List user's worlds
POST   /api/v1/worlds                      # Create new world
GET    /api/v1/worlds/{worldId}            # Get world details
PUT    /api/v1/worlds/{worldId}            # Update world
DELETE /api/v1/worlds/{worldId}            # Soft delete world
```

### Entity Management

Operations for managing entities within a world (locations, characters, campaigns, etc.).

```text
GET    /api/v1/worlds/{worldId}/entities                    # List all entities in world
POST   /api/v1/worlds/{worldId}/entities                    # Create entity
GET    /api/v1/worlds/{worldId}/entities/{entityId}         # Get entity details
PUT    /api/v1/worlds/{worldId}/entities/{entityId}         # Update entity
DELETE /api/v1/worlds/{worldId}/entities/{entityId}         # Soft delete entity
PATCH  /api/v1/worlds/{worldId}/entities/{entityId}         # Partial update entity

GET    /api/v1/worlds/{worldId}/entities/{parentId}/children  # Get children of entity
POST   /api/v1/worlds/{worldId}/entities/{entityId}/move      # Move entity to new parent
```

### Asset Management

Operations for uploading, managing, and retrieving assets (images, audio, documents).

```text
GET    /api/v1/worlds/{worldId}/entities/{entityId}/assets   # List entity assets
POST   /api/v1/worlds/{worldId}/entities/{entityId}/assets   # Upload asset
GET    /api/v1/worlds/{worldId}/assets/{assetId}             # Get asset details
PUT    /api/v1/worlds/{worldId}/assets/{assetId}             # Update asset metadata
DELETE /api/v1/worlds/{worldId}/assets/{assetId}             # Delete asset

GET    /api/v1/worlds/{worldId}/assets/{assetId}/download    # Download asset with SAS token
```

### Search and Discovery

Operations for searching and filtering entities within a world.

```text
GET    /api/v1/worlds/{worldId}/search                       # Search entities by name/tags
GET    /api/v1/worlds/{worldId}/entities?type={entityType}   # Filter by entity type
GET    /api/v1/worlds/{worldId}/entities?tags={tag1,tag2}    # Filter by tags
```

### AG-UI Agent Endpoint

CopilotKit integration endpoint for AI agent interactions.

```text
POST   /api/v1/copilotkit                  # AG-UI protocol endpoint (SSE/WebSocket)
```

## Authentication & Authorization

### Authentication

- **Method**: JWT Bearer tokens (future: Entra ID CIAM)
- **Header**: `Authorization: Bearer {token}`
- **Stubbed**: Initial implementation uses stubbed `IUserContextService`

### Authorization Rules

- **World Access**: Users can only access worlds they own (`OwnerId` match in World container)
- **Entity Access**: Users can only access entities within worlds they own (validated via World ownership)
- **Asset Access**: Users can only access assets within worlds they own (validated via World ownership)
- **Public Sharing**: Not supported in initial release
- **Domain Rules**:
  - World must exist before creating entities (enforced by repository layer)
  - Entities must have valid WorldId referencing existing World
  - Deleting a World cascades to all entities and assets (soft delete)
  - Restoring a World restores all soft-deleted entities and assets

## Request/Response Formats

### Success Response Format

```json
{
  "data": { /* resource data */ },
  "meta": {
    "requestId": "uuid",
    "timestamp": "2025-01-15T10:30:00Z"
  }
}
```

### Error Response Format

```json
{
  "error": {
    "code": "ENTITY_NOT_FOUND",
    "message": "Entity with ID '123' not found",
    "details": [ /* optional validation errors */ ]
  },
  "meta": {
    "requestId": "uuid",
    "timestamp": "2025-01-15T10:30:00Z"
  }
}
```

### HTTP Status Codes

- **200 OK**: Successful GET, PUT, PATCH
- **201 Created**: Successful POST with resource creation
- **204 No Content**: Successful DELETE
- **400 Bad Request**: Invalid request format or validation errors
- **401 Unauthorized**: Missing or invalid authentication token
- **403 Forbidden**: User lacks permission to access resource
- **404 Not Found**: Resource does not exist
- **409 Conflict**: Resource conflict (e.g., duplicate name)
- **422 Unprocessable Entity**: Validation failed
- **429 Too Many Requests**: Rate limit exceeded
- **500 Internal Server Error**: Unexpected server error

## Pagination

List endpoints support cursor-based pagination:

```text
GET /api/v1/worlds/{worldId}/entities?limit=50&cursor={nextCursor}
```

Response includes pagination metadata:

```json
{
  "data": [ /* array of entities */ ],
  "pagination": {
    "limit": 50,
    "hasMore": true,
    "nextCursor": "encoded-cursor-value"
  }
}
```

## Filtering and Sorting

List endpoints support filtering and sorting via query parameters:

```text
GET /api/v1/worlds/{worldId}/entities?type=Character&sort=name&order=asc
```

Supported parameters:

- `type`: Filter by entity type
- `tags`: Filter by tags (comma-separated)
- `sort`: Sort field (name, createdDate, modifiedDate)
- `order`: Sort order (asc, desc)

## Rate Limiting

- **Per-User Limits**: 100 requests per minute (standard tier)
- **Rate Limit Headers**:
  - `X-RateLimit-Limit`: Total requests allowed per window
  - `X-RateLimit-Remaining`: Requests remaining in current window
  - `X-RateLimit-Reset`: Unix timestamp when limit resets

## Versioning Strategy

- **URL Path Versioning**: `/api/v1/...`, `/api/v2/...`
- **Backward Compatibility**: v1 maintained for minimum 12 months after v2 release
- **Deprecation Notices**: Header `X-API-Deprecated: true` on deprecated versions

## OpenAPI/Swagger Documentation

- **Live Documentation**: Available at `/swagger` endpoint (development/staging only)
- **OpenAPI Spec**: Available at `/swagger/v1/swagger.json`
- **Interactive Testing**: Swagger UI for API exploration and testing

## Future Enhancements

- **GraphQL API**: Alternative query interface for complex entity graphs
- **WebSocket Support**: Real-time updates via SignalR
- **Batch Operations**: Bulk create/update/delete endpoints
- **Webhooks**: Event notifications for entity changes
- **API Keys**: Alternative authentication for programmatic access
