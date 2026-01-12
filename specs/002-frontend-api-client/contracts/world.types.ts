/**
 * World Entity API Contract
 * 
 * TypeScript type definitions for World entity endpoints.
 * These types define the contract between frontend and backend APIs.
 */

/**
 * World entity - root container for game/story content
 */
export interface World {
  /** Unique identifier (UUID v4) */
  id: string;

  /** Display name of the world */
  name: string;

  /** Rich text description of the world */
  description: string;

  /** User ID of the world owner */
  ownerId: string;

  /** ISO 8601 timestamp of creation */
  createdAt: string;

  /** ISO 8601 timestamp of last update */
  updatedAt: string;

  /** Soft delete flag */
  isDeleted: boolean;
}

/**
 * Response wrapper for list endpoints
 */
export interface WorldListResponse {
  /** Array of world entities */
  data: World[];

  /** Response metadata */
  meta: {
    /** Request correlation ID for tracing */
    requestId: string;

    /** ISO 8601 timestamp of response generation */
    timestamp: string;
  };
}

/**
 * Response wrapper for single-item endpoints
 */
export interface WorldResponse {
  /** Single world entity */
  data: World;

  /** Response metadata */
  meta: {
    /** Request correlation ID for tracing */
    requestId: string;

    /** ISO 8601 timestamp of response generation */
    timestamp: string;
  };
}

/**
 * Request body for POST /api/worlds
 */
export interface CreateWorldRequest {
  /** Display name (required, 1-200 characters) */
  name: string;

  /** Rich text description (required, 10-5000 characters) */
  description: string;

  // ownerId is derived from authenticated user context
}

/**
 * Request body for PUT /api/worlds/{id}
 * All fields optional for partial updates
 */
export interface UpdateWorldRequest {
  /** Display name (1-200 characters) */
  name?: string;

  /** Rich text description (10-5000 characters) */
  description?: string;
}

/**
 * Endpoints
 * 
 * GET    /api/worlds           -> WorldListResponse
 * GET    /api/worlds/{id}      -> WorldResponse
 * POST   /api/worlds           -> WorldResponse (201 Created)
 * PUT    /api/worlds/{id}      -> WorldResponse
 * DELETE /api/worlds/{id}      -> 204 No Content
 * 
 * All endpoints require authentication (Bearer token in Authorization header).
 * All endpoints may return ProblemDetails on error (4xx, 5xx).
 */

/**
 * RFC 7807 Problem Details for HTTP APIs
 * Returned by all endpoints on error
 */
export interface ProblemDetails {
  /** URI reference identifying the problem type */
  type?: string;

  /** Short, human-readable summary of the problem */
  title: string;

  /** HTTP status code */
  status: number;

  /** Human-readable explanation specific to this occurrence */
  detail?: string;

  /** URI reference identifying the specific occurrence */
  instance?: string;

  /** ASP.NET Core validation errors (400 Bad Request) */
  errors?: Record<string, string[]>;
}

/**
 * Error Examples
 * 
 * 400 Bad Request:
 * {
 *   "type": "https://api.librismaleficarum.com/errors/validation",
 *   "title": "Validation Failed",
 *   "status": 400,
 *   "errors": {
 *     "Name": ["The Name field is required."],
 *     "Description": ["Description must be between 10 and 5000 characters."]
 *   }
 * }
 * 
 * 404 Not Found:
 * {
 *   "type": "https://api.librismaleficarum.com/errors/not-found",
 *   "title": "Resource Not Found",
 *   "status": 404,
 *   "detail": "World with ID '123' not found",
 *   "instance": "/api/worlds/123"
 * }
 * 
 * 429 Too Many Requests:
 * {
 *   "type": "https://api.librismaleficarum.com/errors/rate-limit",
 *   "title": "Rate Limit Exceeded",
 *   "status": 429,
 *   "detail": "Too many requests. Please retry after 30 seconds."
 * }
 * Response includes `Retry-After: 30` header.
 * 
 * 500 Internal Server Error:
 * {
 *   "type": "https://api.librismaleficarum.com/errors/server-error",
 *   "title": "Internal Server Error",
 *   "status": 500,
 *   "detail": "An unexpected error occurred. Request ID: abc-123"
 * }
 */
