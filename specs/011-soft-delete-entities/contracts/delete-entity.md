# API Contract: Soft Delete World Entity

**Feature**: 011-soft-delete-entities  
**Date**: 2026-01-31 (Updated for all-async approach)

## OpenAPI Specification

```yaml
openapi: 3.0.3
info:
  title: Libris Maleficarum - Soft Delete API
  version: 1.0.0
  description: API contract for soft deleting world entities with async processing and status polling

paths:
  /api/v1/worlds/{worldId}/entities/{entityId}:
    delete:
      operationId: deleteWorldEntity
      summary: Initiate soft delete of a world entity
      description: |
        Initiates an asynchronous delete operation for the specified entity. 
        If cascade=true (default), all descendant entities are also deleted.
        
        Returns immediately with 202 Accepted and a Location header pointing to 
        the status endpoint. The frontend should poll the status endpoint to 
        track progress.
        
        This operation is idempotent - deleting an already-deleted entity creates
        an operation that completes immediately with deleted=0.
      tags:
        - WorldEntities
      parameters:
        - name: worldId
          in: path
          required: true
          description: The unique identifier of the world
          schema:
            type: string
            format: uuid
        - name: entityId
          in: path
          required: true
          description: The unique identifier of the entity to delete
          schema:
            type: string
            format: uuid
        - name: cascade
          in: query
          required: false
          description: |
            If true (default), recursively soft-delete all descendant entities.
            If false, only delete the specified entity (fails if it has children).
          schema:
            type: boolean
            default: true
      responses:
        '202':
          description: Delete operation accepted and queued for processing
          headers:
            Location:
              description: URL to the delete operation status endpoint
              schema:
                type: string
                example: /api/v1/worlds/550e8400.../delete-operations/abc123...
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/DeleteOperationResponse'
              example:
                data:
                  id: "abc12345-6789-0abc-def0-123456789abc"
                  worldId: "550e8400-e29b-41d4-a716-446655440000"
                  rootEntityId: "123e4567-e89b-12d3-a456-426614174000"
                  rootEntityName: "Continent Arcanis"
                  status: "pending"
                  totalEntities: 0
                  deletedCount: 0
                  failedCount: 0
                  cascade: true
                  createdAt: "2026-01-31T12:00:00.000Z"
        '400':
          description: Bad request - entity has children but cascade=false
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ErrorResponse'
        '403':
          description: Forbidden - user does not own this world
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ErrorResponse'
        '404':
          description: World or entity not found
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ErrorResponse'
        '429':
          description: Too many concurrent delete operations (limit 5 per user per world)
          headers:
            Retry-After:
              description: Suggested seconds to wait before retrying
              schema:
                type: integer
                example: 30
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ErrorResponse'

  /api/v1/worlds/{worldId}/delete-operations/{operationId}:
    get:
      operationId: getDeleteOperation
      summary: Get delete operation status
      description: |
        Returns the current status and progress of a delete operation.
        Poll this endpoint to track the progress of an async delete.
        
        Operations are retained for 24 hours after completion, then auto-purged.
      tags:
        - DeleteOperations
      parameters:
        - name: worldId
          in: path
          required: true
          schema:
            type: string
            format: uuid
        - name: operationId
          in: path
          required: true
          schema:
            type: string
            format: uuid
      responses:
        '200':
          description: Delete operation status
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/DeleteOperationResponse'
              examples:
                pending:
                  summary: Operation pending
                  value:
                    data:
                      id: "abc12345-6789-0abc-def0-123456789abc"
                      status: "pending"
                      totalEntities: 0
                      deletedCount: 0
                      createdAt: "2026-01-31T12:00:00.000Z"
                in_progress:
                  summary: Operation in progress
                  value:
                    data:
                      id: "abc12345-6789-0abc-def0-123456789abc"
                      status: "in_progress"
                      totalEntities: 150
                      deletedCount: 75
                      failedCount: 0
                      startedAt: "2026-01-31T12:00:00.100Z"
                completed:
                  summary: Operation completed
                  value:
                    data:
                      id: "abc12345-6789-0abc-def0-123456789abc"
                      status: "completed"
                      totalEntities: 150
                      deletedCount: 150
                      failedCount: 0
                      completedAt: "2026-01-31T12:00:05.000Z"
        '404':
          description: Operation not found or expired
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/ErrorResponse'

  /api/v1/worlds/{worldId}/delete-operations:
    get:
      operationId: listDeleteOperations
      summary: List recent delete operations
      description: |
        Returns a list of recent delete operations for the world (last 24 hours).
        Useful for showing a "pending operations" indicator in the UI.
      tags:
        - DeleteOperations
      parameters:
        - name: worldId
          in: path
          required: true
          schema:
            type: string
            format: uuid
        - name: limit
          in: query
          required: false
          schema:
            type: integer
            default: 20
            maximum: 100
      responses:
        '200':
          description: List of recent delete operations
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/DeleteOperationListResponse'

components:
  schemas:
    DeleteOperationResponse:
      type: object
      properties:
        data:
          $ref: '#/components/schemas/DeleteOperation'
    
    DeleteOperationListResponse:
      type: object
      properties:
        data:
          type: array
          items:
            $ref: '#/components/schemas/DeleteOperation'
        meta:
          type: object
          properties:
            count:
              type: integer
    
    DeleteOperation:
      type: object
      required:
        - id
        - worldId
        - rootEntityId
        - status
        - createdAt
      properties:
        id:
          type: string
          format: uuid
          description: Unique identifier for this operation
        worldId:
          type: string
          format: uuid
        rootEntityId:
          type: string
          format: uuid
          description: The entity being deleted
        rootEntityName:
          type: string
          description: Display name of the entity being deleted
        status:
          type: string
          enum: [pending, in_progress, completed, partial, failed]
          description: Current status of the operation
        totalEntities:
          type: integer
          description: Total entities to delete (0 if not yet calculated)
        deletedCount:
          type: integer
          description: Number of entities successfully deleted
        failedCount:
          type: integer
          description: Number of entities that failed to delete
        failedEntityIds:
          type: array
          items:
            type: string
            format: uuid
          description: IDs of entities that failed (for debugging)
        errorDetails:
          type: string
          description: Error message if operation failed
        cascade:
          type: boolean
          description: Whether cascade delete was requested
        createdBy:
          type: string
          description: User ID who initiated the delete
        createdAt:
          type: string
          format: date-time
        startedAt:
          type: string
          format: date-time
          nullable: true
        completedAt:
          type: string
          format: date-time
          nullable: true
    
    ErrorResponse:
      type: object
      properties:
        error:
          $ref: '#/components/schemas/ErrorDetail'
    
    ErrorDetail:
      type: object
      required:
        - code
        - message
      properties:
        code:
          type: string
          enum:
            - ENTITY_NOT_FOUND
            - WORLD_NOT_FOUND
            - ENTITY_HAS_CHILDREN
            - OPERATION_NOT_FOUND
            - FORBIDDEN
            - VALIDATION_ERROR
            - RATE_LIMIT_EXCEEDED
        message:
          type: string
```

## Request/Response Examples

### Initiate Delete (Single Entity, No Children)

```http
DELETE /api/v1/worlds/550e8400-e29b-41d4-a716-446655440000/entities/123e4567-e89b-12d3-a456-426614174000
Authorization: Bearer {token}
```

**Response**: `202 Accepted`
```http
Location: /api/v1/worlds/550e8400-e29b-41d4-a716-446655440000/delete-operations/abc12345-6789-0abc-def0-123456789abc
Content-Type: application/json

{
  "data": {
    "id": "abc12345-6789-0abc-def0-123456789abc",
    "worldId": "550e8400-e29b-41d4-a716-446655440000",
    "rootEntityId": "123e4567-e89b-12d3-a456-426614174000",
    "rootEntityName": "Town Guard",
    "status": "pending",
    "totalEntities": 0,
    "deletedCount": 0,
    "failedCount": 0,
    "cascade": true,
    "createdBy": "user-123",
    "createdAt": "2026-01-31T12:00:00.000Z"
  }
}
```

### Poll Status (In Progress)

```http
GET /api/v1/worlds/550e8400-e29b-41d4-a716-446655440000/delete-operations/abc12345-6789-0abc-def0-123456789abc
Authorization: Bearer {token}
```

**Response**: `200 OK`
```json
{
  "data": {
    "id": "abc12345-6789-0abc-def0-123456789abc",
    "worldId": "550e8400-e29b-41d4-a716-446655440000",
    "rootEntityId": "123e4567-e89b-12d3-a456-426614174000",
    "rootEntityName": "Continent Arcanis",
    "status": "in_progress",
    "totalEntities": 150,
    "deletedCount": 75,
    "failedCount": 0,
    "failedEntityIds": [],
    "cascade": true,
    "createdBy": "user-123",
    "createdAt": "2026-01-31T12:00:00.000Z",
    "startedAt": "2026-01-31T12:00:00.100Z"
  }
}
```

### Poll Status (Completed)

```http
GET /api/v1/worlds/550e8400-e29b-41d4-a716-446655440000/delete-operations/abc12345-6789-0abc-def0-123456789abc
Authorization: Bearer {token}
```

**Response**: `200 OK`
```json
{
  "data": {
    "id": "abc12345-6789-0abc-def0-123456789abc",
    "worldId": "550e8400-e29b-41d4-a716-446655440000",
    "rootEntityId": "123e4567-e89b-12d3-a456-426614174000",
    "rootEntityName": "Continent Arcanis",
    "status": "completed",
    "totalEntities": 150,
    "deletedCount": 150,
    "failedCount": 0,
    "failedEntityIds": [],
    "cascade": true,
    "createdBy": "user-123",
    "createdAt": "2026-01-31T12:00:00.000Z",
    "startedAt": "2026-01-31T12:00:00.100Z",
    "completedAt": "2026-01-31T12:00:05.000Z"
  }
}
```

### Delete Already-Deleted Entity (Idempotent)

```http
DELETE /api/v1/worlds/550e8400-e29b-41d4-a716-446655440000/entities/123e4567-e89b-12d3-a456-426614174000
Authorization: Bearer {token}
```

**Response**: `202 Accepted`

After polling, the operation shows:
```json
{
  "data": {
    "id": "def45678-...",
    "status": "completed",
    "totalEntities": 0,
    "deletedCount": 0,
    "failedCount": 0
  }
}
```

### List Recent Operations

```http
GET /api/v1/worlds/550e8400-e29b-41d4-a716-446655440000/delete-operations?limit=10
Authorization: Bearer {token}
```

**Response**: `200 OK`
```json
{
  "data": [
    {
      "id": "abc12345-...",
      "rootEntityName": "Continent Arcanis",
      "status": "completed",
      "deletedCount": 150,
      "completedAt": "2026-01-31T12:00:05.000Z"
    },
    {
      "id": "def45678-...",
      "rootEntityName": "Character Elara",
      "status": "in_progress",
      "deletedCount": 5,
      "totalEntities": 12
    }
  ],
  "meta": {
    "count": 2
  }
}
```

### Rate Limit Exceeded (429)

```http
DELETE /api/v1/worlds/550e8400-e29b-41d4-a716-446655440000/entities/123e4567-e89b-12d3-a456-426614174000
Authorization: Bearer {token}
```

**Response**: `429 Too Many Requests`
```http
Retry-After: 30
Content-Type: application/json

{
  "error": {
    "code": "RATE_LIMIT_EXCEEDED",
    "message": "You have reached the limit of 5 concurrent delete operations for this world. Please wait for existing operations to complete."
  }
}
```

## Frontend Polling Pattern

```typescript
async function deleteEntityWithPolling(worldId: string, entityId: string): Promise<DeleteResult> {
  // 1. Initiate delete
  const response = await fetch(`/api/v1/worlds/${worldId}/entities/${entityId}`, {
    method: 'DELETE',
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  if (response.status !== 202) {
    throw new Error('Delete initiation failed');
  }
  
  const { data: operation } = await response.json();
  const statusUrl = response.headers.get('Location')!;
  
  // 2. Poll for completion
  while (true) {
    await delay(500); // Poll every 500ms
    
    const statusResponse = await fetch(statusUrl, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    
    const { data: status } = await statusResponse.json();
    
    // Update UI with progress
    onProgress?.(status.deletedCount, status.totalEntities);
    
    if (status.status === 'completed' || status.status === 'partial' || status.status === 'failed') {
      return status;
    }
  }
}
```

## Behavior Notes

1. **All Async**: Every delete returns 202 Accepted immediately (no sync path)
2. **Idempotency**: Deleting an already-deleted entity creates an operation that completes with `deleted: 0`
3. **Authorization**: User must own the world
4. **TTL**: Operations auto-purge 24 hours after completion
5. **Consistency**: During processing, descendant entities may briefly appear until marked deleted
6. **Progress Updates**: Status updated in near real-time (within 2 seconds of actual progress)
7. **Rate Limiting**: Users are limited to 5 concurrent delete operations per world; 429 returned if exceeded
