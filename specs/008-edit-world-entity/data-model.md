# Data Model: World Entity Editing

**Feature**: 008-edit-world-entity  
**Date**: 2026-01-25  
**Purpose**: Define data structures and state shapes for entity editing feature

## Redux State Extensions

### WorldSidebarState (modifications to `worldSidebarSlice.ts`)

**No changes needed** - Existing state already supports entity editing:

```typescript
export interface WorldSidebarState {
  selectedWorldId: string | null;
  selectedEntityId: string | null;
  expandedNodeIds: string[];
  
  mainPanelMode: MainPanelMode; // Already includes 'editing_entity'
  editingEntityId: string | null; // Already tracks entity being edited
  hasUnsavedChanges: boolean;    // Already tracks dirty state
  
  // ... other existing fields ...
}

export type MainPanelMode = 
  | 'empty' 
  | 'viewing_entity' 
  | 'editing_world' 
 | 'creating_world' 
  | 'creating_entity' 
  | 'editing_entity'   // ✅ Already defined
  | 'moving_entity';
```

**Existing Actions to Reuse**:

- `openEntityFormEdit(entityId: string)` - Sets `mainPanelMode = 'editing_entity'`, `editingEntityId = entityId`
- `closeEntityForm()` - Resets edit state
- `setUnsavedChanges(hasChanges: boolean)` - Tracks form dirty state

**Optional Enhancement** (for unsaved changes dialog):

```typescript
// If dialog state needs to be in Redux (alternative: component-local state)
export interface WorldSidebarState {
  // ... existing fields ...
  
  // Optional: Pending navigation context
  pendingNavigation?: {
    action: 'edit_entity' | 'select_entity' | 'close_form';
    targetEntityId?: string;
  } | null;
}
```

**Decision**: Start with **component-local state** for dialog. Promote to Redux only if shared across components.

---

## Component-Local State

### WorldEntityForm (form state)

```typescript
interface WorldEntityFormState {
  // Form field values
  name: string;
  description: string;
  entityType: WorldEntityType | '';
  customProperties: Record<string, unknown> | null;
  
  // Validation
  errors: {
    name?: string;
    type?: string;
    [fieldName: string]: string | undefined;
  };
  
  // Metadata
  isDirty: boolean; // Computed: current values !== original values
}
```

**Change Detection Logic**:

```typescript
const isDirty = useMemo(() => {
  if (!isEditing || !existingEntity) return false;
  
  return (
    name !== existingEntity.name ||
    description !== (existingEntity.description || '') ||
    JSON.stringify(customProperties) !== (existingEntity.properties || '{}')
  );
}, [name, description, customProperties, existingEntity, isEditing]);
```

---

### UnsavedChangesDialog (dialog state)

```typescript
interface UnsavedChangesDialogState {
  isOpen: boolean;
  isSaving: boolean;
  saveError: string | null;
}
```

**Dialog Trigger Conditions**:

```typescript
const shouldShowDialog = (
  hasUnsavedChanges: boolean,
  targetAction: 'edit_entity' | 'close_form'
): boolean => {
  return hasUnsavedChanges && targetAction !== 'save';
};
```

---

## Entity Data Model (existing, no changes)

### WorldEntity

```typescript
export interface WorldEntity {
  id: string;                    // GUID
  worldId: string;               // Partition key
  parentId: string | null;       // Parent entity ID (null for root)
  name: string;                  // Required, max 100 chars
  description: string | null;    // Optional, max 500 chars
  entityType: WorldEntityType;   // Read-only during edit
  tags: string[];                // Editable list
  properties: string | null;     // JSON-serialized custom properties
  schemaVersion: string;         // e.g., "1.0.0"
  hasChildren: boolean;          // Computed server-side
  createdAt: string;             // ISO 8601
  updatedAt: string;             // ISO 8601
}

export enum WorldEntityType {
  World = 'World',
  Continent = 'Continent',
  Country = 'Country',
  Region = 'Region',
  City = 'City',
  Character = 'Character',
  // ... other types
}
```

---

## API Request/Response Shapes (existing)

### Update Entity Request

```typescript
// RTK Query mutation: useUpdateWorldEntityMutation
interface UpdateWorldEntityRequest {
  worldId: string;
  entityId: string;
  data: {
    name: string;
    description: string | null;
    properties?: string; // JSON string
    schemaVersion: string;
  };
  currentEntityType: WorldEntityType; // For optimistic update
}
```

**API Endpoint**: `PATCH /api/worlds/{worldId}/entities/{entityId}`

**Response**: Updated `WorldEntity` object

**Error Responses**:

- 400 Bad Request: Validation errors (name empty, invalid schema)
- 404 Not Found: Entity or World doesn't exist
- 409 Conflict: Concurrent modification detected (future)
- 500 Server Error: Database error

---

## State Flow Diagram

```text
User clicks Edit icon/button
         ↓
   [openEntityFormEdit(entityId)] action dispatched
         ↓
   Redux updates:
   - mainPanelMode = 'editing_entity'
   - editingEntityId = entityId
         ↓
   MainPanel renders WorldEntityForm
         ↓
   Form loads entity via useGetWorldEntityByIdQuery
         ↓
   User modifies fields → isDirty = true
         ↓
   setUnsavedChanges(true) dispatched
         ↓
   User clicks Save
         ↓
   useUpdateWorldEntityMutation() called
         ↓
   [Success] → mainPanelMode = 'viewing_entity'
            → Display EntityDetailReadOnlyView
   [Error]   → Show error toast, stay in edit mode
```

---

## Validation Rules Schema

```typescript
interface ValidationRule {
  field: string;
  required: boolean;
  maxLength?: number;
  pattern?: RegExp;
  customValidator?: (value: any, entity?: WorldEntity) => string | null;
}

const entityValidationRules: Record<WorldEntityType, ValidationRule[]> = {
  [WorldEntityType.World]: [
    { field: 'name', required: true, maxLength: 100 },
    { field: 'description', required: false, maxLength: 500 },
  ],
  [WorldEntityType.Character]: [
    { field: 'name', required: true, maxLength: 100 },
    { field: 'description', required: false, maxLength: 500 },
    // Future: Add character-specific rules
  ],
  // ... other entity types
};
```

---

## Data Integrity Constraints

### Client-Side (Form Validation)

- Name: Required, 1-100 characters, non-empty after trim
- Description: Optional, 0-500 characters
- Entity Type: Read-only in edit mode (cannot change)
- Custom Properties: Must be valid JSON structure

### Server-Side (API)

- Unique entity ID (enforced by Cosmos DB)
- Parent-child relationships must be valid (parent exists)
- Schema version must match entity type
- Partition key consistency (worldId)

### Optimistic Updates

- Update hierarchy immediately after save (before API confirmation)
- Revalidate on error (RTK Query automatic refetch)
- Rollback on API error (show previous state + error toast)

---

## Performance Considerations

### State Updates

- Debounce form field onChange (300ms) to reduce re-renders
- Memoize isDirty calculation (useMemo)
- Avoid unnecessary Redux dispatches (check prev value before setUnsavedChanges)

### API Calls

- Cancel in-flight query if user navigates away (RTK Query abort controller)
- Cache entity data for 60 seconds (RTK Query keepUnusedDataFor)
- Invalidate cache on successful update (providesTags/invalidatesTags)

---

## Testing Data Fixtures

```typescript
// __tests__/fixtures/worldEntity.fixtures.ts

export const mockWorldEntity: WorldEntity = {
  id: 'entity-123',
  worldId: 'world-456',
  parentId: null,
  name: 'Test Entity',
  description: 'Test description',
  entityType: WorldEntityType.City,
  tags: ['test', 'fixture'],
  properties: null,
  schemaVersion: '1.0.0',
  hasChildren: false,
  createdAt: '2026-01-25T00:00:00Z',
  updatedAt: '2026-01-25T00:00:00Z',
};

export const mockEntityWithProperties: WorldEntity = {
  ...mockWorldEntity,
  id: 'entity-789',
  entityType: WorldEntityType.GeographicRegion,
  properties: JSON.stringify({
    climate: 'temperate',
    terrain: 'forest',
    population: 10000,
  }),
};
```

---

## Summary

**State Management**: Leverage existing Redux structure; no new state needed  
**Component State**: Form fields, validation errors, dirty tracking (local)  
**API**: Existing `useUpdateWorldEntityMutation` handles persistence  
**Validation**: Client-side schema-based rules + server-side constraints  
**Performance**: Memoization, debouncing, optimistic updates

**Next**: Generate component contracts and quickstart guide
