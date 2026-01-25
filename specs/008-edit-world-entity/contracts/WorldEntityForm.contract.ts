/**
 * WorldEntityForm Component Contract
 *
 * Unified form for creating and editing WorldEntity items.
 * Renamed from EntityDetailForm for semantic clarity.
 *
 * @module components/MainPanel/WorldEntityForm
 */

import type { WorldEntity, WorldEntityType } from '@/services/types/worldEntity.types';

/**
 * WorldEntityForm component properties
 *
 * The form operates in two modes:
 * - Create mode: When editingEntityId is null new entity creation)
 * - Edit mode: When editingEntityId is set (editing existing entity)
 *
 * Mode is determined by Redux state (worldSidebarSlice.editingEntityId)
 */
export interface WorldEntityFormProps {
  /**
   * No props needed - component reads state from Redux
   * - editingEntityId: null (create) vs string (edit)
   * - newEntityParentId: Parent ID for new entities
   * - selectedWorldId: Current world context
   */
}

/**
 * Internal form state shape (component-local, not Redux)
 */
export interface WorldEntityFormState {
  /** Entity name (1-100 characters, required) */
  name: string;

  /** Entity description (0-500 characters, optional) */
  description: string;

  /** Entity type - EDITABLE in create mode, READ-ONLY in edit mode */
  entityType: WorldEntityType | '';

  /** Custom properties (JSON-serializable object, entity-type specific) */
  customProperties: Record<string, unknown> | null;

  /** Validation errors by field name */
  errors: {
    name?: string;
    type?: string;
    [fieldName: string]: string | undefined;
  };
}

/**
 * Form behaviors and constraints
 */
export interface WorldEntityFormBehavior {
  /** CREATE MODE (editingEntityId === null) */
  create: {
    /** Load parent entity context if newEntityParentId is set */
    loadParentContext: boolean;

    /** Entity type selector is enabled */
    entityTypeEditable: true;

    /** Submit button label */
    submitLabel: 'Create';

    /** On successful save: Close form, expand parent node in hierarchy */
    onSuccess: () => void;
  };

  /** EDIT MODE (editingEntityId !== null) */
  edit: {
    /** Load existing entity data */
    loadEntityData: boolean;

    /** Entity type selector is DISABLED (read-only) */
    entityTypeEditable: false;

    /** Submit button label */
    submitLabel: 'Save Changes';

    /** On successful save: Transition to read-only detail view */
    onSuccess: () => void;
  };

  /** SHARED BEHAVIORS */
  shared: {
    /** Track unsaved changes via setUnsavedChanges(boolean) Redux action */
    trackChanges: boolean;

    /** Show beforeunload warning if hasUnsavedChanges === true */
    preventNavigationWithoutWarning: boolean;

    /** Validate on submit (client-side schema-based rules) */
    validateBeforeSave: boolean;

    /** Show inline field-level error messages */
    displayValidationErrors: boolean;

    /** On cancel: Dispatch closeEntityForm(), discard changes */
    onCancel: () => void;
  };
}

/**
 * Validation requirements
 */
export interface WorldEntityFormValidation {
  /** Name field constraints */
  name: {
    required: true;
    minLength: 1; // After trim
    maxLength: 100;
    errorMessages: {
      required: 'Name is required';
      tooLong: 'Name must be 100 characters or less';
    };
  };

  /** Entity type constraints */
  entityType: {
    required: true;
    readOnlyInEditMode: true;
    errorMessages: {
      required: 'Type is required';
    };
  };

  /** Description field constraints */
  description: {
    required: false;
    maxLength: 500;
    errorMessages: {
      tooLong: 'Description must be 500 characters or less';
    };
  };

  /** Custom properties validation (type-specific, extensible) */
  customProperties: {
    mustBeSerializable: true; // JSON.stringify() must succeed
    validateByEntityType: boolean; // Schema-based validation
  };
}

/**
 * Accessibility requirements (WCAG 2.2 Level AA)
 */
export interface WorldEntityFormAccessibility {
  /** Form element has accessible label */
  formRole: 'form';
  formAriaLabel: 'Entity creation form' | 'Entity editing form';

  /** Required fields marked with aria-required */
  requiredFieldsMarked: boolean;

  /** Invalid fields marked with aria-invalid */
  invalidFieldsMarked: boolean;

  /** Error messages associated via aria-describedby */
  errorMessagesLinked: boolean;

  /** Submit/Cancel buttons keyboard-accessible */
  buttonsKeyboardAccessible: boolean;

  /** Focus management: First field receives focus on mount */
  focusFirstFieldOnMount: boolean;

  /** Focus returns to trigger element on close */
  focusReturnOnClose: boolean;
}

/**
 * Integration points
 */
export interface WorldEntityFormIntegration {
  /** Redux state selectors */
  selectors: {
    selectedWorldId: 'selectSelectedWorldId';
    editingEntityId: 'selectEditingEntityId';
    newEntityParentId: 'selectNewEntityParentId';
    hasUnsavedChanges: 'selectHasUnsavedChanges';
  };

  /** Redux actions */
  actions: {
    closeForm: 'closeEntityForm()';
    setUnsavedChanges: 'setUnsavedChanges(boolean)';
    expandNode: 'expandNode(entityId)'; // After create
  };

  /** RTK Query hooks */
  queries: {
    getEntity: 'useGetWorldEntityByIdQuery({ worldId, entityId })';
    getParent: 'useGetWorldEntityByIdQuery({ worldId, entityId: parentId })';
  };

  /** RTK Query mutations */
  mutations: {
    create: 'useCreateWorldEntityMutation()';
    update: 'useUpdateWorldEntityMutation()';
  };
}

/**
 * Test scenarios (for WorldEntityForm.test.tsx)
 */
export interface WorldEntityFormTestScenarios {
  /** Rendering tests */
  rendering: {
    'Renders form in create mode': boolean;
    'Renders form in edit mode with pre-populated data': boolean;
    'Displays loading state while entity loads': boolean;
    'Shows error state if entity fails to load': boolean;
  };

  /** Interaction tests */
  interaction: {
    'Allows typing in name field': boolean;
    'Allows typing in description field': boolean;
    'Allows selecting entity type in create mode': boolean;
    'Disables entity type selector in edit mode': boolean;
    'Tracks unsaved changes on field modification': boolean;
    'Submits form on Save button click': boolean;
    'Cancels form on Cancel button click': boolean;
  };

  /** Validation tests */
  validation: {
    'Shows error when name is empty': boolean;
    'Shows error when entity type is not selected': boolean;
    'Shows error when description exceeds 500 chars': boolean;
    'Clears errors after fixing invalid fields': boolean;
    'Prevents submission when validation fails': boolean;
  };

  /** Accessibility tests */
  accessibility: {
    'Has no accessibility violations (jest-axe)': boolean;
    'Form has accessible label': boolean;
    'Required fields marked with aria-required': boolean;
    'Invalid fields marked with aria-invalid': boolean;
    'Error messages linked via aria-describedby': boolean;
    'Submit/Cancel buttons keyboard-accessible': boolean;
  };

  /** Integration tests */
  integration: {
    'Calls createEntity mutation in create mode': boolean;
    'Calls updateEntity mutation in edit mode': boolean;
    'Closes form on successful save': boolean;
    'Expands parent node after entity creation': boolean;
    'Transitions to read-only view after edit save': boolean;
    'Shows error toast on save failure': boolean;
  };
}
