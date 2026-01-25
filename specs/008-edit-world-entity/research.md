# Research Findings: World Entity Editing

**Feature**: 008-edit-world-entity  
**Date**: 2026-01-25  
**Purpose**: Resolve unknowns identified in implementation plan before design phase

## Research Topic 1: Component Reusability Analysis

**Question**: Can `EntityDetailForm` (to be renamed `WorldEntityForm`) cleanly support both create and edit modes with a single implementation?

**Decision**: ✅ Yes - Minimal changes needed. Component already supports create/edit modes via `editingEntityId` prop.

**Rationale**:
- Existing `EntityDetailForm` already has conditional logic: `const isEditing = !!editingEntityId`
- Already loads entity data via `useGetWorldEntityByIdQuery` when `editingEntityId` is present
- Already pre-populates form fields in useEffect when `existingEntity` loads
- Mutation hook selection already works: `useCreateWorldEntityMutation` vs `useUpdateWorldEntityMutation`

**Required Changes**:
1. Make entity type field read-only when `isEditing === true`
2. Add `disabled` prop to `EntityTypeSelector` component
3. No structural refactoring needed

**Alternatives Considered**:
- ❌ Separate CreateEntityForm and EditEntityForm components - Rejected because it duplicates ~200 lines of validation, state management, and form rendering logic
- ❌ Mode prop ('create' | 'edit') - Rejected because Redux state already tracks this via `editingEntityId` presence

**References**:
- Current implementation: `libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.tsx` (to be renamed WorldEntityForm.tsx)
- Pattern precedent: `WorldDetailForm.tsx` uses similar approach with `mode` prop

---

## Research Topic 2: Unsaved Changes Detection Pattern

**Question**: What's the best practice for detecting form changes in React + Redux Toolkit context?

**Decision**: ✅ Use shallow field comparison with `hasUnsavedChanges` flag in Redux + component-local dirty tracking

**Rationale**:
- Redux `hasUnsavedChanges` flag already exists and integrated with `beforeunload` warning
- Shallow comparison sufficient for form fields (name, description, entityType are primitives)
- Custom properties (JSON) compared via `JSON.stringify()` for deep equality
- Performance: Comparison runs only on field change, not every render

**Implementation Pattern**:
```typescript
useEffect(() => {
  const hasChanges = 
    name.trim() !== (existingEntity?.name || '') ||
    description.trim() !== (existingEntity?.description || '') ||
    entityType !== (isEditing ? existingEntity?.entityType : '');
  
  dispatch(setUnsavedChanges(hasChanges));
}, [name, description, entityType, existingEntity, isEditing, dispatch]);
```

**Alternatives Considered**:
- ❌ Deep equality check (lodash.isEqual) - Rejected as overkill for shallow form state; adds 4.4KB dependency
- ❌ Form library (React Hook Form, Formik) - Rejected to avoid introducing new dependencies for simple form
- ❌ Immer-based change tracking - Rejected as unnecessary complexity; Redux Toolkit already uses Immer internally

**References**:
- Current implementation: `EntityDetailForm.tsx` (to be renamed WorldEntityForm.tsx) lines 79-87
- React docs: https://react.dev/reference/react/useEffect

---

## Research Topic 3: Edit Icon UX Pattern

**Question**: Should edit icon be always visible, on hover only, or on selection?

**Decision**: ✅ Hover-triggered (opacity-0 group-hover:opacity-100) matching existing Plus icon pattern

**Rationale**:
- Consistency with existing UX: `EntityTreeNode` already uses hover-reveal for Plus icon (line 106-117)
- Reduces visual clutter in hierarchy (only selected/hovered nodes show actions)
- Accessible: Icon still keyboard-focusable via `tab` navigation (tabindex not -1 for edit icon)
- Touch-friendly: Icon appears on tap/selection for mobile users

**Implementation Pattern**:
```tsx
<Button
  variant="icon-ghost"
  size="icon-sm"
  onClick={handleEdit}
  aria-label={`Edit ${entity.name}`}
 tabIndex={0} // Keyboard focusable
  className="opacity-0 group-hover:opacity-100 focus-visible:opacity-100 transition-opacity"
>
  <Edit size={14} aria-hidden="true" />
</Button>
```

**Alternatives Considered**:
- ❌ Always visible - rejected because clutters hierarchy UI with multiple action icons per row
- ❌ Show on selection only - rejected because user must select first vs direct action
- ❌ Only in context menu - rejected because spec requires visible edit icon (not just right-click)

**References**:
- Current pattern: `EntityTreeNode.tsx` lines 106-117 (Plus icon)
- Lucide React icons: https://lucide.dev/icons/edit

---

## Research Topic 4: Dialog Confirmation Best Practices

**Question**: How to handle dialog confirmation with async save operations?

**Decision**: ✅ Shadcn/ui Dialog with Promise-based handlers + loading state management

**Rationale**:
- Shadcn/ui Dialog already used in codebase (`DeleteConfirmationModal.tsx`)
- Controlled `open` prop syncs with Redux state
- Async handlers return Promise → enables loading states and error handling
- Focus management and keyboard navigation (Escape, Tab trap) built-in via Radix UI

**Implementation Pattern**:
```tsx
export function UnsavedChangesDialog({ open, onSave, onDiscard, onCancel, isSaving }: Props) {
  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && onCancel()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Unsaved Changes</DialogTitle>
          <DialogDescription>
            You have unsaved changes. Would you like to save them before continuing?
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={onCancel} disabled={isSaving}>
            Cancel
          </Button>
          <Button variant="secondary" onClick={onDiscard} disabled={isSaving}>
            Don't Save
          </Button>
          <Button onClick={onSave} disabled={isSaving}>
            {isSaving ? <Loader2 className="animate-spin"/> : 'Save'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
```

**Error Handling**:
- If `onSave()` Promise rejects → show toast error, keep dialog open, re-enable buttons
- If `onSave()` Promise resolves → close dialog, proceed with pending navigation

**Accessibility**:
- Dialog traps focus (Radix UI DialogPrimitive)
- Escape key calls `onCancel()`
- Clear button labels: "Save", "Don't Save", "Cancel"
- Loading state announced via button text change

**Alternatives Considered**:
- ❌ Browser confirm() dialog - rejected because not styled, not accessible, blocking
- ❌ Custom modal from scratch - rejected to leverage existing Shadcn/ui patterns
- ❌ Inline warning banner - rejected because spec requires dialog with Yes/No/Cancel options

**References**:
- Shadcn/ui Dialog: https://ui.shadcn.com/docs/components/dialog
- Current usage: `DeleteConfirmationModal.tsx`

---

## Research Topic 5: Schema Validation Strategy

**Question**: How does entity type registry provide validation rules?

**Decision**: ✅ Use `ENTITY_SCHEMA_VERSIONS` mapping + schema version field for validation context; implement field-level validators

**Current State Analysis**:
- `ENTITY_SCHEMA_VERSIONS` constant defined in `worldEntity.types.ts` maps entity types → schema versions (e.g., `World: '1.0.0'`)
- Schema version stored per entity: `entity.schemaVersion`
- Entity type registry (feature 007) provides type definitions, not runtime validators
- Validation currently implemented inline in `EntityDetailForm` (to be renamed WorldEntityForm) (name required, type required)

**Implementation Approach**:
```typescript
// services/validators/worldEntityValidator.ts

import { WorldEntityType } from '../types/worldEntity.types';

interface ValidationRule {
  field: string;
  validate: (value: any) => string | null; // Returns error message or null
  required?: boolean;
}

export const getValidationRules = (entityType: WorldEntityType): ValidationRule[] => {
  const baseRules: ValidationRule[] = [
    {
      field: 'name',
      required: true,
      validate: (value: string) => {
        if (!value?.trim()) return 'Name is required';
        if (value.length > 100) return 'Name must be 100 characters or less';
        return null;
      }
    },
    {
      field: 'description',
      validate: (value: string) => {
        if (value && value.length > 500) return 'Description must be 500 characters or less';
        return null;
      }
    }
  ];

  // Type-specific rules can be added based on entityType
  // Example: Character entities might require additional fields
  
  return baseRules;
};
```

**Validation Execution**:
```typescript
const validate = () => {
  const rules = getValidationRules(entityType as WorldEntityType);
  const newErrors: Record<string, string> = {};
  
  rules.forEach(rule => {
    const error = rule.validate(formState[rule.field]);
    if (error) newErrors[rule.field] = error;
  });
  
  setErrors(newErrors);
  return Object.keys(newErrors).length === 0;
};
```

**Rationale**:
- Centralized validation logic (DRY principle)
- Type-safe via TypeScript
- Extensible for future type-specific rules
- Schema version tracking enables future migrations

**Alternatives Considered**:
- ❌ JSON Schema validation - rejected as over-engineered for current needs; adds dependency (ajv ~400KB)
- ❌ Yup/Zod schema library - rejected to avoid new dependency; current validation is simple
- ❌ Backend-only validation - rejected because UX requires immediate feedback before save attempt

**References**:
- Current validation: `EntityDetailForm.tsx` (to be renamed WorldEntityForm.tsx) lines 114-119
- Schema versions: `worldEntity.types.ts` lines 145-154

---

## Summary of Decisions

| Topic | Decision | Impact |
|-------|----------|--------|
| Component reusability | Reuse EntityDetailForm with minor changes | No architectural change needed |
| Change detection | Shallow comparison + Redux flag | Leverage existing patterns |
| Edit icon visibility | Hover-triggered (like Plus icon) | Consistent UX |
| Dialog confirmation | Shadcn/ui Dialog + Promise handlers | Reuse existing components |
| Validation | Field-level validators by type | Centralized, extensible |

**Next Phase**: Design detailed component contracts and data models (Phase 1)
