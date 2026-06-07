/**
 * Search result types for the world-scoped entity search endpoint.
 */

export interface SearchResultItem {
  id: string;
  name: string;
  entityType: string;
  descriptionSnippet?: string;
  relevanceScore: number;
  worldId: string;
  parentId: string | null;
  path?: string[];
  depth?: number;
  tags: string[];
  ownerId: string;
  createdAt: string;
  updatedAt: string;
}

export interface SearchMeta {
  totalCount: number;
  offset: number;
  limit: number;
}

export interface SearchResponse {
  data: SearchResultItem[];
  meta: SearchMeta;
}

export interface SearchEntitiesArg {
  worldId: string;
  q?: string;
  mode?: 'hybrid' | 'text' | 'vector';
  entityType?: string;
  tags?: string;
  limit?: number;
  offset?: number;
}
