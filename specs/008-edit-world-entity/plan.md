# Implementation Plan: World Entity Editing

**Branch**: `008-edit-world-entity` | **Date**: 2026-01-25 | **Spec**: [spec.md](./spec.md)

## Summary

Enable users to edit existing world entities through two primary pathways:
1. **Quick edit from hierarchy** (P1): Click edit icon next to entity in WorldSidebar hierarchy → edit form displays in MainPanel
2. **Edit from detail view** (P2): Click Edit button when viewing entity → form fields become editable in MainPanel

**Key Behaviors**:
- Edit form displays in MainPanel, replacing existing content
- Unsaved changes trigger Yes/No/Cancel dialog before navigation
- Entity type is read-only during edit (future enhancement planned)
- Successful save returns to read-only detail view
- Schema-based validation using entity type registry
- Edit button positioned in top-right corner of detail view

**Technical Approach**: Leverage existing `EntityDetailForm` component (rename to `WorldEntityForm`), add edit mode toggle to entity detail view, implement unsaved changes dialog, add edit icon to `EntityTreeNode`.

## Technical Context

**Language/Version**: TypeScript 5.x + React 19  
**Primary Dependencies**: Redux Toolkit, React Router, Shadcn/ui + Radix UI, TailwindCSS v4, Vitest + Testing Library, RTK Query  
**Storage**: Azure Cosmos DB (via REST API), client-side Redux state  
**Testing**: Vitest + Testing Library + jest-axe (frontend accessibility testing)  
**Target Platform**: Modern web browsers (Chrome/Edge/Firefox/Safari latest 2 versions)  
**Project Type**: Web application (React SPA frontend only - this feature is frontend-only)  
**Performance Goals**: 
  - Edit initiation < 2 seconds from hierarchy, < 1 second from detail view
  - Validation feedback < 500ms
  - Hierarchy update < 1 second after save
**Constraints**: 
  - WCAG 2.2 Level AA compliance required
  - Zero data loss on cancel/error
  - Single-user application (no concurrent edit handling)
**Scale/Scope**: ~8 components (new/modified), ~12 test files, 1 UI dialog, 3 Redux action creators (potentially)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I: Cloud-Native Architecture
✅ **PASS** - Frontend-only feature, no infrastructure changes. Existing Azure Cosmos DB API used.

### Principle II: Clean Architecture & Separation of Concerns
✅ **PASS** - Maintains clear separation:
  - UI Components (WorldEntityForm, EntityDetailReadOnlyView, UnsavedChangesDialog)
  - State Management (worldSidebarSlice actions/selectors)
  - Business Logic (validation via entity type registry)
  - Data Access (existing RTK Query hooks: useUpdateWorldEntityMutation)

### Principle III: Test-Driven Development (NON-NEGOTIABLE)
✅ **PASS** - Plan includes TDD workflow:
  - Write tests BEFORE implementation for each component
  - jest-axe accessibility tests for all interactive elements (Edit button, dialog, icons)
  - AAA pattern (Arrange-Act-Assert)
  - Test coverage monitored in CI

### Principle IV: Framework & Technology Standards
✅ **PASS** - Uses mandated stack:
  - React 19 with TypeScript ✓
  - Shadcn/ui (Dialog, Button components) ✓
  - Redux Toolkit for state ✓
  - Vitest for testing ✓

### Principle V: Developer Experience & Inner Loop
✅ **PASS** - Frontend development workflow:
  - Hot reload via Vite (existing)
  - Component dev in isolation via Vitest UI
  - Single command: `pnpm dev`

### Principle VI: Security & Privacy by Default
✅ **PASS** - No new security concerns:
  - Uses existing authenticated API calls
  - No new secrets or credentials
  - Inherits existing auth model (Azure Entra ID)

### Principle VII: Semantic Versioning & Breaking Changes
✅ **PASS** - Non-breaking feature addition:
  - MINOR version bump (new feature, backward compatible)
  - Existing create/view flows unchanged
  - Component rename (EntityDetailForm → WorldEntityForm) is internal refactor

**GATE RESULT**: ✅ **ALL CHECKS PASSED** - Proceed to Phase 0

## Project Structure

### Documentation (this feature)

```
specs/008-edit-world-entity/
├── spec.md                # Feature specification (completed)
├── plan.md                # This file  
├── research.md            # Phase 0: Research findings (to be generated)
├── data-model.md          # Phase 1: Data model (minimal - Redux state shape)
├── quickstart.md          # Phase 1: Developer quickstart
├── contracts/             # Phase 1: Component contracts (TypeScript interfaces)
│   ├── WorldEntityForm.contract.ts
│   ├── EntityDetailReadOnlyView.contract.ts
│   └── UnsavedChangesDialog.contract.ts
└── checklists/
    └── requirements.md    # Specification quality checklist (completed)
```

### Source Code (repository root)

**Frontend (React SPA)**:
```
libris-maleficarum-app/src/
├── components/
│   ├── MainPanel/
│   │   ├── MainPanel.tsx                          # MODIFY: Add Edit button in detail view, handle read-only mode
│   │   ├── WorldEntityForm.tsx                    # RENAME FROM: EntityDetailForm.tsx
│   │   ├── WorldEntityForm.test.tsx               # RENAME FROM: EntityDetailForm.test.tsx
│   │   ├── EntityDetailReadOnlyView.tsx           # NEW: Read-only entity view with Edit button
│   │   ├── EntityDetailReadOnlyView.test.tsx      # NEW: Tests for read-only view
│   │   ├── UnsavedChangesDialog.tsx               # NEW: Yes/No/Cancel dialog
│   │   ├── UnsavedChangesDialog.test.tsx          # NEW: Dialog accessibility tests
│   │   ├── DeleteConfirmationModal.tsx            # EXISTING: No changes
│   │   └── WorldDetailForm.tsx                    # EXISTING: No changes
│   │
│   ├── WorldSidebar/
│   │   ├── EntityTreeNode.tsx                     # MODIFY: Add edit icon (pen), handle click
│   │   ├── EntityTreeNode.test.tsx                # MODIFY: Add tests for edit icon interaction
│   │   ├── EntityContextMenu.tsx                  # EXISTING: Already has "Edit Entity" menu item
│   │   └── EntityContextMenu.test.tsx             # EXISTING: Tests already cover edit action
│   │
│   └── ui/
│       ├── dialog.tsx                             # EXISTING: Shadcn/ui Dialog (reuse for UnsavedChangesDialog)
│       └── button.tsx                             # EXISTING: Shadcn/ui Button (reuse for Edit button)
│
├── store/
│   └── worldSidebarSlice.ts                       # MODIFY (MINOR): May need to add navigation confirmation state
│
└── services/
    ├── worldEntityApi.ts                          # EXISTING: useUpdateWorldEntityMutation already exists
    └── config/
        └── entityTypeRegistry.ts                  # EXISTING: Validation schema source
```

**Tests**:
```
libris-maleficarum-app/src/
├── __tests__/
│   └── integration/
│       ├── entityEdit.test.tsx                    # NEW: End-to-end edit workflow tests
│       └── unsavedChangesFlow.test.tsx            # NEW: Integration test for unsaved changes dialog
```

**Structure Decision**: 
This is a **web application (Option 2 - frontend only)** feature. All changes are in the `libris-maleficarum-app/` React SPA. No backend changes required—existing Cosmos DB API (`useUpdateWorldEntityMutation`) handles persistence. Component co-location pattern: each component has corresponding `.test.tsx` file in the same directory.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**NO VIOLATIONS** - All constitution principles satisfied. No complexity justifications needed.

---

## Phase 0: Research & Unknowns Resolution

*Status: To be completed by `/speckit.plan` command*

### Research Topics

1. **Component Reusability Analysis**
   - **Question**: Can `EntityDetailForm` (to be renamed `WorldEntityForm`) cleanly support both create and edit modes with a single implementation?
   - **Current State**: `EntityDetailForm` already distinguishes create/edit via `editingEntityId` prop and loads existing data in edit mode
   - **Decision Needed**: Confirm no significant refactor needed; just add read-only mode for entity type field

2. **Unsaved Changes Detection Pattern**
   - **Question**: What's the best practice for detecting form changes in React + Redux Toolkit context?
   - **Current State**: `hasUnsavedChanges` flag already exists in `worldSidebarSlice`
   - **Decision Needed**: Research deep-equal comparison vs dirty field tracking for form state

3. **Edit Icon UX Pattern**
   - **Question**: Should edit icon be always visible, on hover only, or on selection?
   - **Current Patterns**: EntityTreeNode already has hover-triggered "Plus" icon for quick create
   - **Decision Needed**: Align with existing pattern (hover-triggered) for consistency

4. **Dialog Confirmation Best Practices**
   - **Question**: How to handle dialog confirmation with async save operations?
   - **Standards**: Shadcn/ui Dialog with controlled open state + async handlers
   - **Decision Needed**: Define loading states and error handling in dialog

5. **Schema Validation Strategy**
   - **Question**: How does entity type registry provide validation rules?
   - **Current State**: Entity type registry exists (feature 007), schema versions tracked
   - **Decision Needed**: Clarify validation API surface (e.g., `getValidationSchema(entityType)`)

### Output: `research.md`
Document decisions for each topic with:
- **Decision**: What was chosen
- **Rationale**: Why it was chosen
- **Alternatives Considered**: What else was evaluated
- **References**: Links to existing code patterns, Shadcn/ui docs, React best practices

---

## Phase 1: Design & Contracts

*Status: To be completed by `/speckit.plan` command*

### Data Model (`data-model.md`)

**Redux State Shape** (modifications to `worldSidebarSlice`):

```typescript
export interface WorldSidebarState {
  // ... existing fields ...
  
  // NEW (if needed): Dialog state for unsaved changes confirmation
  showUnsavedChangesDialog?: boolean;
  pendingNavigation?: {
    action: 'edit_entity' | 'navigate_away' | 'close_form';
    entityId?: string; // For 'edit_entity' action
  };
}
```

**Entity Edit Session** (component-local state, not Redux):
```typescript
interface EditSessionState {
  originalValues: Partial<WorldEntity>;  // For change detection
  currentValues: Partial<WorldEntity>;   // Form state
  validationErrors: Record<string, string>; // Field-level errors
  isDirty: boolean;                      // Has any field changed?
}
```

### API Contracts (`contracts/`)

#### `WorldEntityForm.contract.ts`
```typescript
/**
 * WorldEntityForm Component Contract
 * 
 * Unified form for creating and editing WorldEntity items.
 * Supports both create mode (new entity) and edit mode (existing entity).
 */

export interface WorldEntityFormProps {
  /** Operating mode: 'create' for new entities, 'edit' for existing */
  mode: 'create' | 'edit';
  
  /** Entity ID to edit (required when mode='edit', null when mode='create') */
  entityId?: string | null;
  
  /** Parent entity ID for new entity creation (optional, null for root entities) */
  parentId?: string | null;
  
  /** Callback invoked after successful save */
  onSaveSuccess?: (savedEntity: WorldEntity) => void;
  
  /** Callback invoked when form is cancelled */
  onCancel?: () => void;
}

export interface WorldEntityFormState {
  name: string;
  description: string;
  entityType: WorldEntityType | '';
  customProperties: Record<string, unknown> | null;
  errors: { name?: string; type?: string };
  isDirty: boolean;
}
```

#### `EntityDetailReadOnlyView.contract.ts`
```typescript
/**
 * EntityDetailReadOnlyView Component Contract
 * 
 * Read-only display of a world entity with Edit button.
 * Displays entity name, type, tags, description, and custom properties.
 */

export interface EntityDetailReadOnlyViewProps {
  /** Entity to display */
  entity: WorldEntity;
  
  /** Callback invoked when Edit button is clicked */
  onEditClick: () => void;
  
  /** Whether the edit action is disabled (optional) */
  disableEdit?: boolean;
}
```

#### `UnsavedChangesDialog.contract.ts`
```typescript
/**
 * UnsavedChangesDialog Component Contract
 * 
 * Confirmation dialog shown when user attempts navigation with unsaved changes.
 * Offers Yes (save and proceed), No (discard and proceed), Cancel (stay) options.
 */

export interface UnsavedChangesDialogProps {
  /** Whether dialog is open */
  open: boolean;
  
  /** Callback when Yes (save) is clicked - should return Promise for async save */
  onSave: () => Promise<void>;
  
  /** Callback when No (discard) is clicked */
  onDiscard: () => void;
  
  /** Callback when Cancel is clicked or dialog is dismissed */
  onCancel: () => void;
  
  /** Loading state during save operation */
  isSaving?: boolean;
}
```

### Developer Quickstart (`quickstart.md`)

```markdown
# World Entity Editing - Developer Quickstart

## Prerequisites
- Node.js 20.x installed
- pnpm package manager
- Repository cloned and dependencies installed (`pnpm install`)

## Running Development Server
```bash
cd libris-maleficarum-app
pnpm dev
```
Access at https://127.0.0.1:4000

## Running Tests
```bash
# All tests
pnpm test

# Single component
pnpm test src/components/MainPanel/WorldEntityForm.test.tsx

# Watch mode
pnpm test -- --watch

# Accessibility tests only
pnpm test -- --grep "accessibility"
```

## Component Overview

### WorldEntityForm
Location: `src/components/MainPanel/WorldEntityForm.tsx`
Purpose: Unified form for create/edit modes
Mode Toggle: Via `editingEntityId` in Redux state
Entry Points: 
- Create: `openEntityFormCreate(parentId)` action
- Edit: `openEntityFormEdit(entityId)` action

### EntityDetailReadOnlyView
Location: `src/components/MainPanel/EntityDetailReadOnlyView.tsx`
Purpose: Read-only entity display with Edit button (top-right)
Trigger: MainPanel when `mainPanelMode === 'viewing_entity'`

### UnsavedChangesDialog
Location: `src/components/MainPanel/UnsavedChangesDialog.tsx`
Purpose: Confirm navigation away from unsaved changes
Trigger: User clicks edit icon/button while MainPanel has `hasUnsavedChanges: true`

## Key Redux Actions
- `openEntityFormEdit(entityId)` - Open entity in edit mode
- `setUnsavedChanges(boolean)` - Track form dirty state
- `closeEntityForm()` - Exit edit mode

## Testing Checklist
- [ ] Edit icon appears on hover in hierarchy
- [ ] Edit button appears in top-right of read-only detail view
- [ ] Clicking edit icon/button displays form in MainPanel
- [ ] Unsaved changes dialog appears when appropriate
- [ ] Entity type field is read-only in edit mode
- [ ] Save returns to read-only detail view
- [ ] Hierarchy updates after save
- [ ] All interactive elements keyboard-accessible
- [ ] jest-axe passes for all components
```

---

## Phase 2: Task Planning

*This phase is handled by `/speckit.tasks` command - NOT part of `/speckit.plan` output.*

After this plan is reviewed and research.md/data-model.md/quickstart.md/contracts/ are generated, run:
```
/speckit.tasks
```

This will break down the implementation into granular, testable tasks organized by:
- Setup & Infrastructure
- Frontend Components
- State Management
- Integration & Testing
- Documentation & Polish

---

## Notes for Implementation

### Code Reusability Priorities

1. **Rename `EntityDetailForm` → `WorldEntityForm`**
   - Update imports across codebase
   - Update test file names
   - Rationale: Better semantic clarity (distinguishes from potential other entity forms)

2. **Extract Read-Only View**
   - Current `MainPanel.tsx` has inline entity display JSX
   - Extract to `EntityDetailReadOnlyView.tsx` component
   - Rationale: Separation of concerns (read-only vs edit), reusability, testability

3. **Reuse Existing Patterns**
   - `hasUnsavedChanges` tracking (already in slice)
   - Hover-triggered action icons (like Plus icon in EntityTreeNode)
   - Shadcn/ui Dialog component (consistent with DeleteConfirmationModal)
   - Validation error display (follow WorldEntityForm pattern)

4. **Avoid Duplication**
   - Entity type selector component (reuse existing `EntityTypeSelector`)
   - Form layout component (reuse existing `FormLayout`)
   - Form action buttons (reuse existing `FormActions`)
   - Input/Textarea/Badge components (existing Shadcn/ui)

### Accessibility Requirements (WCAG 2.2 Level AA)

- Edit icon: `aria-label="Edit {entityName}"`, `role="button"`, `tabindex="0"`, keyboard activation (Enter/Space)
- Edit button: Shadcn/ui Button with clear label, keyboard focus visible
- Dialog: Focus trap, Escape to cancel, focus return to trigger element
- Form validation: `aria-invalid`, `aria-describedby` for error messages
- All interactive elements: Minimum 44×44px touch target
- Color contrast: 4.5:1 for text, 3:1 for UI components
- Screen reader announcements: Use `aria-live` for save success/error feedback

### Performance Considerations

- Use React.memo() for EntityTreeNode to prevent unnecessary re-renders
- Debounce validation (e.g., 300ms) to avoid excessive API calls
- Optimistic UI updates: Update hierarchy immediately after save, revalidate in background
- Form change detection: Shallow comparison of field values vs deep equality

### Error Handling

- Network errors during save: Show toast notification, preserve form state
- Validation errors: Inline per-field messages, prevent save button activation
- Entity not found (404): Redirect to empty MainPanel, show error toast
- Concurrent modification (if detected): Show conflict dialog (future enhancement)
