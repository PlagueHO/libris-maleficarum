# Quick Start Guide: World Entity Editing Feature

Developer guide for implementing the world entity editing feature in Libris Maleficarum.

## Overview

This feature adds the ability to edit existing World entities through two entry points:

1. **Edit button** in the top-right corner of the entity detail view (MainPanel)
1. **Edit icon** (pen) displayed on hover next to entity name in the world hierarchy (WorldSidebar)

Both entry points open the same unified form component (`WorldEntityForm`) in edit mode.

## Prerequisites

- Node.js 20.x (enforced by `run` script)
- pnpm package manager
- Dependencies installed: `pnpm install` (in `libris-maleficarum-app/`)
- Dev server running: `pnpm dev` (starts on <https://127.0.0.1:4000>)

## Feature Architecture

### Key Components

1. **WorldEntityForm** (rename from `EntityDetailForm`)
   - Unified form for create/edit modes
   - Location: `src/components/MainPanel/WorldEntityForm.tsx`
   - Controlled by `isEditing` flag (derived from `editingEntityId`)
   - Disables entity type selector during edit

1. **EntityDetailReadOnlyView** (new component)
   - Read-only display with Edit button
   - Location: `src/components/MainPanel/EntityDetailReadOnlyView.tsx`
   - Replaces current entity detail display logic
   - Shows Edit button in top-right corner

1. **UnsavedChangesDialog** (new component)
   - Confirmation dialog for unsaved changes
   - Location: `src/components/MainPanel/UnsavedChangesDialog.tsx`
   - Provides Save/Don't Save/Cancel options
   - Integrates with WorldEntityForm navigation logic

1. **EntityTreeNode** (modify existing)
   - Add hover-triggered Edit icon
   - Location: `src/components/WorldSidebar/EntityTreeNode.tsx`
   - Dispatch `openEntityFormEdit(entityId)` on edit icon click

### State Management

Redux slice: `worldSidebarSlice.ts`

- **State shape** (already supports editing):

  ```typescript
  {
    mainPanelMode: 'viewing_entity' | 'creating_entity' | 'editing_entity',
    selectedEntityId: string | null,
    editingEntityId: string | null, // Set when editing
    creatingParentId: string | null,
    hasUnsavedChanges: boolean
  }
  ```

- **Actions** (all already exist):
  - `openEntityFormEdit(entityId)`: Sets `editingEntityId`, `mainPanelMode='editing_entity'`
  - `closeEntityForm()`: Clears editing/creating IDs, resets mode
  - `setUnsavedChanges(hasChanges)`: Tracks form dirty state

### Data Flow

1. **Edit initiated from hierarchy**:
   - User hovers entity in tree → Edit icon appears
   - User clicks Edit icon → `dispatch(openEntityFormEdit(entityId))`
   - WorldSidebar renders WorldEntityForm with `isEditing=true`, `editingEntityId={id}`

1. **Edit initiated from detail view**:
   - MainPanel shows EntityDetailReadOnlyView
   - User clicks Edit button → `dispatch(openEntityFormEdit(entityId))`
   - MainPanel re-renders WorldEntityForm with `isEditing=true`

1. **Save flow**:
   - User modifies fields → `dispatch(setUnsavedChanges(true))`
   - User clicks Save → `updateWorldEntityMutation.mutate()`
   - On success → toast notification, `dispatch(closeEntityForm())`, return to read-only view

1. **Navigation with unsaved changes**:
   - User clicks different entity/close → UnsavedChangesDialog opens
   - User chooses Save/Don't Save/Cancel
   - Save: async save → proceed navigation | Don't Save: discard → proceed | Cancel: abort navigation

## Component Contracts

See `contracts/` folder for detailed TypeScript interfaces:

- `WorldEntityForm.contract.ts`: Form component props, state, behavior, validation, accessibility
- `EntityDetailReadOnlyView.contract.ts`: Read-only view structure, styling, edit button behavior
- `UnsavedChangesDialog.contract.ts`: Dialog props, button handlers, error handling, keyboard interactions

## Implementation Workflow

### Phase 1: Rename and Refactor Form Component

1. **Rename EntityDetailForm to WorldEntityForm**:

   ```bash
   cd libris-maleficarum-app/src/components/MainPanel
   mv EntityDetailForm.tsx WorldEntityForm.tsx
   ```

1. **Update WorldEntityForm**:
   - Disable EntityTypeSelector when `isEditing=true`:

     ```typescript
     <EntityTypeSelector
       value={entityType}
       onChange={setEntityType}
       disabled={isEditing} // Add this line
     />
     ```

   - Update imports in `MainPanel.tsx`

1. **Create EntityDetailReadOnlyView**:
   - Copy current read-only display logic from MainPanel
   - Add Edit button in top-right corner:

     ```tsx
     <Button
       variant="ghost"
       size="icon"
       onClick={() => dispatch(openEntityFormEdit(entity.id))}
       aria-label={`Edit ${entity.name}`}
     >
       <Pencil className="h-4 w-4" />
     </Button>
     ```

1. **Update MainPanel logic**:

   ```typescript
   // MainPanel.tsx
   if (mainPanelMode === 'viewing_entity' && selectedEntity) {
     return <EntityDetailReadOnlyView entity={selectedEntity} />;
   }
   if (mainPanelMode === 'editing_entity' && editingEntityId) {
     return <WorldEntityForm isEditing editingEntityId={editingEntityId} />;
   }
   if (mainPanelMode === 'creating_entity' && creatingParentId) {
     return <WorldEntityForm isEditing={false} parentId={creatingParentId} />;
   }
   ```

### Phase 2: Add Edit Icon to Hierarchy

1. **Update EntityTreeNode**:
   - Add hover state for edit icon:

     ```tsx
     const [isHovered, setIsHovered] = useState(false);

     <div
       onMouseEnter={() => setIsHovered(true)}
       onMouseLeave={() => setIsHovered(false)}
     >
       {/* Entity name */}
       {isHovered && (
         <>
           <Button
             variant="ghost"
             size="icon"
             onClick={() => dispatch(openEntityFormEdit(entity.id))}
             aria-label={`Edit ${entity.name}`}
           >
             <Pencil className="h-3 w-3" />
           </Button>
           {/* Existing Plus icon */}
         </>
       )}
     </div>
     ```

### Phase 3: Implement Unsaved Changes Dialog

1. **Create UnsavedChangesDialog component**:
   - Use Shadcn/ui Dialog component
   - Three buttons: Cancel (outline), Don't Save (secondary), Save (primary)
   - Handle async save with loading state

1. **Integrate with WorldEntityForm**:

   ```typescript
   const [showUnsavedDialog, setShowUnsavedDialog] = useState(false);
   const [pendingAction, setPendingAction] = useState<() => void | null>(null);

   const handleNavigationAttempt = (action: () => void) => {
     if (hasUnsavedChanges) {
       setPendingAction(() => action);
       setShowUnsavedDialog(true);
     } else {
       action();
     }
   };

   const handleSave = async () => {
     await updateEntity();
     pendingAction?.();
     setShowUnsavedDialog(false);
   };
   ```

1. **Intercept navigation events**:
   - Close button click
   - Edit icon click for different entity
   - Entity selection in hierarchy

### Phase 4: Add Validation

1. **Schema-based validation** using Entity Type Registry (feature 007):

   ```typescript
   // Assume getEntityTypeSchema(entityType) returns schema
   const schema = getEntityTypeSchema(entityType);

   const validateField = (name: string, value: string): string | null => {
     const fieldSchema = schema.properties.find(p => p.name === name);
     if (!fieldSchema) return null;

     if (fieldSchema.required && !value.trim()) {
       return `${fieldSchema.label} is required`;
     }
     if (fieldSchema.maxLength && value.length > fieldSchema.maxLength) {
       return `${fieldSchema.label} must be ${fieldSchema.maxLength} characters or less`;
     }
     // Add more validation rules per schema
     return null;
   };
   ```

1. **Display validation errors**:
   - Show inline error message below field
   - Use `aria-describedby` to associate with input
   - Disable Save button when form has errors

## Testing Strategy (TDD)

### Test-Driven Development Workflow

1. **Write failing test** → 2. **Implement minimum code** → 3. **Refactor** → Repeat

### Component Test Files

Create these test files alongside components:

- `WorldEntityForm.test.tsx` (rename from `EntityDetailForm.test.tsx`)
- `EntityDetailReadOnlyView.test.tsx` (new)
- `UnsavedChangesDialog.test.tsx` (new)
- `EntityTreeNode.test.tsx` (add edit icon tests)

### Test Template

```typescript
import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { axe, toHaveNoViolations } from 'jest-axe';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import worldSidebarReducer from '@/store/worldSidebarSlice';

expect.extend(toHaveNoViolations);

describe('ComponentName', () => {
  const renderWithRedux = (ui: React.ReactElement) => {
    const store = configureStore({
      reducer: { worldSidebar: worldSidebarReducer },
    });
    return render(<Provider store={store}>{ui}</Provider>);
  };

  it('renders with no accessibility violations', async () => {
    const { container } = renderWithRedux(<Component />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('handles user interaction', async () => {
    renderWithRedux(<Component />);
    const button = screen.getByRole('button', { name: /edit/i });
    fireEvent.click(button);
    await waitFor(() => {
      expect(/* expected behavior */).toBe(true);
    });
  });
});
```

### Testing Checklist (Per Component)

#### WorldEntityForm

- [ ] Renders in create mode with empty fields
- [ ] Renders in edit mode with pre-filled values from entity
- [ ] Disables entity type selector in edit mode
- [ ] Enables entity type selector in create mode
- [ ] Tracks unsaved changes (hasUnsavedChanges=true after field edit)
- [ ] Validates required fields (name, entity type)
- [ ] Displays inline validation errors
- [ ] Disables Save button when form invalid
- [ ] Calls updateWorldEntity mutation on Save (edit mode)
- [ ] Calls createWorldEntity mutation on Save (create mode)
- [ ] Shows loading state during save
- [ ] Displays success toast after save
- [ ] Displays error toast on save failure
- [ ] Triggers UnsavedChangesDialog on close with unsaved changes
- [ ] No accessibility violations (jest-axe)
- [ ] Keyboard navigable (tab order: fields → Save → Cancel)
- [ ] Screen reader accessible (labels, error announcements)

#### EntityDetailReadOnlyView

- [ ] Renders entity name, type, description
- [ ] Renders custom properties from properties object
- [ ] Shows Edit button in top-right corner
- [ ] Edit button has correct aria-label: "Edit {entity name}"
- [ ] Edit button triggers openEntityFormEdit action
- [ ] Edit button uses Pencil icon
- [ ] Edit button meets minimum touch target (44x44px)
- [ ] No accessibility violations (jest-axe)
- [ ] Keyboard accessible (Edit button reachable via Tab)
- [ ] Screen reader announces Edit button correctly

#### UnsavedChangesDialog

- [ ] Renders when open=true
- [ ] Does not render when open=false
- [ ] Displays correct title: "Unsaved Changes"
- [ ] Displays correct description
- [ ] Renders three buttons: Cancel, Don't Save, Save
- [ ] Cancel button calls onCancel
- [ ] Don't Save button calls onDiscard
- [ ] Save button calls onSave
- [ ] Escape key calls onCancel
- [ ] Click outside dialog calls onCancel
- [ ] Disables all buttons when isSaving=true
- [ ] Shows loading spinner in Save button when isSaving
- [ ] Handles successful save (Promise resolves)
- [ ] Handles failed save (Promise rejects, stays open)
- [ ] Focus trapped within dialog
- [ ] Initial focus on Save button
- [ ] Focus returns to trigger on close
- [ ] No accessibility violations (jest-axe)
- [ ] Dialog has role=dialog, aria-modal=true
- [ ] Dialog has aria-labelledby and aria-describedby

#### EntityTreeNode (Edit Icon)

- [ ] Edit icon appears on hover
- [ ] Edit icon hidden when not hovering
- [ ] Edit icon triggers openEntityFormEdit action
- [ ] Edit icon has correct aria-label: "Edit {entity name}"
- [ ] Edit icon uses Pencil icon
- [ ] Edit icon positioned next to entity name
- [ ] Edit icon does not overlap Plus icon
- [ ] Keyboard accessible (Tab to entity, Enter to focus, Space to activate icon)
- [ ] No accessibility violations (jest-axe)

### Integration Tests

Create `src/__tests__/integration/entityEdit.test.tsx`:

- [ ] Full flow: Hierarchy edit icon → form edit → save → return to detail view
- [ ] Full flow: Detail view edit button → form edit → save → return to detail view
- [ ] Unsaved changes: Edit → modify → navigate away → dialog → Save → navigation proceeds
- [ ] Unsaved changes: Edit → modify → navigate away → dialog → Don't Save → navigation proceeds
- [ ] Unsaved changes: Edit → modify → navigate away → dialog → Cancel → stay in form
- [ ] Network error during save → toast error, stay in edit mode
- [ ] Validation error → inline errors, disable Save button
- [ ] Edit different entity while editing → unsaved dialog → Save → new entity loads in edit mode

## Running Tests

```bash
cd libris-maleficarum-app

# Run all tests (headless)
pnpm test

# Run specific test file
pnpm test src/components/MainPanel/WorldEntityForm.test.tsx

# Run tests matching pattern
pnpm test -- --grep "accessibility"

# Run test UI (browser-based, watch mode)
pnpm test:ui

# Run accessibility tests only
pnpm test:accessibility
```

## Performance Targets

- **Edit initiation from hierarchy**: < 2 seconds (click edit icon → form visible)
- **Edit initiation from detail view**: < 1 second (click Edit button → form visible)
- **Validation feedback**: < 500ms (field blur → error message appears)
- **Save operation**: < 3 seconds (click Save → success toast, return to detail view)

## Accessibility Compliance (WCAG 2.2 Level AA)

### Key Requirements

1. **Keyboard Navigation**:
   - All interactive elements (buttons, inputs) keyboard accessible
   - Logical tab order: form fields → Save → Cancel
   - Escape key closes unsaved changes dialog

1. **Screen Reader Support**:
   - All buttons have accessible labels (aria-label or visible text)
   - Form inputs associated with labels (htmlFor/id)
   - Validation errors announced (aria-describedby)
   - Loading states announced (aria-busy)

1. **Visual Accessibility**:
   - Text contrast 4.5:1 (body text), 3:1 (large text/controls)
   - Focus indicators clearly visible
   - Minimum touch targets 44x44px

1. **Error Handling**:
   - Validation errors visible and announced
   - Error messages describe how to fix
   - Failed save shows user-friendly error toast

### Testing Accessibility

All component tests MUST include:

```typescript
it('has no accessibility violations', async () => {
  const { container } = render(<Component />);
  const results = await axe(container);
  expect(results).toHaveNoViolations();
});
```

Run accessibility-specific tests:

```bash
pnpm test:accessibility
```

## Code Reusability Principles

1. **Reuse existing components**:
   - EntityDetailForm → WorldEntityForm (rename only, minimal changes)
   - Shadcn/ui Dialog for UnsavedChangesDialog
   - Existing Button, Input, Textarea components

1. **Avoid code smells**:
   - No code duplication between create/edit modes
   - Single source of truth for form validation
   - Unified navigation logic (intercept pattern for unsaved changes)

1. **Component composition**:
   - EntityDetailReadOnlyView delegates to WorldEntityForm (edit mode)
   - UnsavedChangesDialog decoupled from form logic (accepts callbacks)
   - EntityTreeNode triggers Redux actions, doesn't manage state

## Debugging Tips

### Redux DevTools

1. Open Redux DevTools in browser
1. Monitor state changes:
   - `mainPanelMode` transitions: `viewing_entity` → `editing_entity`
   - `editingEntityId` set when edit icon clicked
   - `hasUnsavedChanges` toggles as fields modified

### Network Tab

1. Filter for API calls: `worldentities` endpoint
1. Check `PUT /worldentities/{id}` on save
1. Inspect request payload matches entity schema
1. Verify 200 response on success

### React DevTools

1. Inspect WorldEntityForm component state
1. Check props: `isEditing`, `editingEntityId`, `parentId`
1. Verify field values update as user types
1. Check validation error state

## Common Issues and Solutions

### Edit button not appearing

- **Check**: EntityDetailReadOnlyView rendered in MainPanel?
- **Fix**: Ensure `mainPanelMode === 'viewing_entity'` and `selectedEntity` exists

### Edit icon not showing on hover

- **Check**: Hover state tracked in EntityTreeNode?
- **Fix**: Add `onMouseEnter/onMouseLeave` handlers, conditional render based on `isHovered`

### Form doesn't load entity data

- **Check**: `editingEntityId` passed correctly to WorldEntityForm?
- **Fix**: Ensure Redux state `editingEntityId` set via `openEntityFormEdit` action

### Unsaved changes dialog doesn't trigger

- **Check**: `hasUnsavedChanges` tracked in Redux?
- **Fix**: Call `dispatch(setUnsavedChanges(true))` on field change

### Save button disabled

- **Check**: Form validation state
- **Fix**: Ensure required fields (name, entity type) have values

### Save fails with 404

- **Check**: Entity still exists in database?
- **Fix**: Handle 404, show toast: "Entity no longer exists", close form

## Next Steps

After reviewing this quickstart guide, proceed to task breakdown:

```bash
/speckit.tasks
```

This will generate `tasks.md` with granular, testable implementation tasks organized by:

- Setup & Infrastructure
- Frontend Components
- State Management
- Integration & Testing
- Documentation & Polish

Each task will reference this quickstart guide for technical details.

## Reference Documentation

- **Feature Spec**: `spec.md` (user stories, requirements, success criteria)
- **Implementation Plan**: `plan.md` (constitution check, phase breakdown)
- **Research Findings**: `research.md` (architectural decisions)
- **Data Model**: `data-model.md` (Redux state, entity schema, validation)
- **Component Contracts**: `contracts/` (TypeScript interfaces, test scenarios)
- **Codebase Instructions**: `../../.github/copilot-instructions.md` (project conventions)
- **Accessibility Guidelines**: `../../.github/instructions/accessibility.instructions.md` (WCAG compliance)
- **React Guidelines**: `../../.github/instructions/reactjs.instructions.md` (React best practices)
- **TypeScript Guidelines**: `../../.github/instructions/typescript.instructions.md` (TypeScript conventions)

---

**Ready to implement?** Start with Phase 1 (Rename and Refactor Form Component) and follow the TDD workflow. Write tests first, then implement minimum code to pass. Happy coding! 🚀
