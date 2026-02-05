import React, { useState, useEffect, useRef } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '../../store/store';
import { closeEntityForm, setUnsavedChanges, expandNode, setSelectedEntity } from '../../store/worldSidebarSlice';
import { logger } from '@/lib/logger';
import {
  useCreateWorldEntityMutation,
  useUpdateWorldEntityMutation,
  useGetWorldEntityByIdQuery,
} from '../../services/worldEntityApi';
import {
  WorldEntityType,
  ENTITY_SCHEMA_VERSIONS,
} from '../../services/types/worldEntity.types';
import { Input } from '../ui/input';
import { Textarea } from '../ui/textarea';
import { EntityTypeSelector } from '../shared/EntityTypeSelector';
import { FormActions } from '../ui/form-actions';
import { FormLayout } from '../ui/form-layout';
import { UnsavedChangesDialog } from '../shared/UnsavedChangesDialog';
import { Loader2 } from 'lucide-react';
import { validateWorldEntityForm, clearFieldError } from '../../services/validators/worldEntityValidator';
import { DynamicPropertiesForm } from './DynamicPropertiesForm';

/**
 * EntityDetailForm Component
 *
 * Unified form for creating and editing WorldEntity items.
 * Rendered in MainPanel when mainPanelMode is 'creating_entity' or 'editing_entity'.
 *
 * Features:
 * - Create mode: Shows parent context and suggests relevant entity types
 * - Edit mode: Pre-populates form fields with existing entity data
 * - Unsaved changes tracking with beforeunload warning
 * - Validation with inline error messages
 *
 * @component
 */
export function EntityDetailForm() {
  // All hooks must be called unconditionally at the top level
  const dispatch = useDispatch();
  const {
    editingEntityId,
    newEntityParentId,
    selectedWorldId,
    hasUnsavedChanges,
  } = useSelector((state: RootState) => state.worldSidebar);

  // Form State
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [entityType, setEntityType] = useState<WorldEntityType | ''>('');
  const [customProperties, setCustomProperties] = useState<Record<string, unknown> | null>(null);
  const [errors, setErrors] = useState<{ name?: string; type?: string; description?: string }>({});
  const [showUnsavedChangesDialog, setShowUnsavedChangesDialog] = useState(false);

  const isEditing = !!editingEntityId;

  // Data Fetching for Edit Mode
  const { data: existingEntity, isLoading: isLoadingEntity } =
    useGetWorldEntityByIdQuery(
      { worldId: selectedWorldId!, entityId: editingEntityId! },
      { skip: !isEditing || !selectedWorldId }
    );

  // Data Fetching for Create Mode (Parent Entity Context)
  const { data: parentEntity, isLoading: isLoadingParent } =
    useGetWorldEntityByIdQuery(
      { worldId: selectedWorldId!, entityId: newEntityParentId! },
      { skip: isEditing || !selectedWorldId || !newEntityParentId }
    );

  // Mutations
  const [createEntity, { isLoading: isCreating }] =
    useCreateWorldEntityMutation();
  const [updateEntity, { isLoading: isUpdating }] =
    useUpdateWorldEntityMutation();

  const isSubmitting = isCreating || isUpdating;

  // Sync form state when entity data loads or form mode changes
  useEffect(() => {
    if (isEditing && existingEntity) {
      // Populate on edit mode
      setName(existingEntity.name);
      setDescription(existingEntity.description || '');
      setEntityType(existingEntity.entityType);
      
      // Deserialize Properties field for Regional entity types
      if (existingEntity.properties) {
        try {
          const parsed = typeof existingEntity.properties === 'string' 
            ? JSON.parse(existingEntity.properties) 
            : existingEntity.properties;
          setCustomProperties(parsed);
        } catch (error) {
          console.error('Failed to parse entity properties:', error);
          setCustomProperties(null);
        }
      } else {
        setCustomProperties(null);
      }
    } else if (!isEditing) {
      // Reset for create mode
      setName('');
      setDescription('');
      setEntityType('');
      setCustomProperties(null);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [editingEntityId, existingEntity?.id]);

  // Clear custom properties when entity type changes to a non-property type
  useEffect(() => {
    if (!entityType) return;
    
    const propertyTypes: WorldEntityType[] = [
      WorldEntityType.GeographicRegion,
      WorldEntityType.PoliticalRegion,
      WorldEntityType.CulturalRegion,
      WorldEntityType.MilitaryRegion,
    ];
    
    if (!propertyTypes.includes(entityType as WorldEntityType)) {
      setCustomProperties(null);
    }
  }, [entityType]);

  // Track unsaved changes
  const hasChangesPrevRef = useRef(false);
  
  useEffect(() => {
    let hasChanges = false;
    
    if (isEditing && existingEntity) {
      // Compare with original values
      const originalName = existingEntity.name || '';
      const originalDescription = existingEntity.description || '';
      const originalType = existingEntity.entityType || '';
      
      hasChanges = name !== originalName || 
                   description !== originalDescription || 
                   entityType !== originalType;
    } else {
      // Check if potentially dirty (for Create mode)
      hasChanges = name.trim() !== '' || description.trim() !== '' || entityType !== '';
    }

    // Only dispatch if value actually changed
    if (hasChanges !== hasChangesPrevRef.current) {
      dispatch(setUnsavedChanges(hasChanges));
      hasChangesPrevRef.current = hasChanges;
    }
  }, [name, description, entityType, dispatch, isEditing, existingEntity]);

  // Cleanup: reset unsaved changes when component unmounts
  useEffect(() => {
    return () => {
      dispatch(setUnsavedChanges(false));
    };
  }, [dispatch]);

  // beforeunload handler for unsaved changes
  useEffect(() => {
    if (!hasUnsavedChanges) return;

    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      e.preventDefault();
      e.returnValue = '';
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, [hasUnsavedChanges]);

  // Skip rendering if no world selected (after all hooks)
  if (!selectedWorldId) return null;

  const validate = () => {
    const result = validateWorldEntityForm(
      {
        name,
        description,
        entityType,
        customProperties: customProperties as Record<string, unknown> | null,
      },
      isEditing
    );
    setErrors(result.errors);
    return result.isValid;
  };

  const handleClose = () => {
    if (hasUnsavedChanges) {
      setShowUnsavedChangesDialog(true);
    } else {
      dispatch(closeEntityForm());
    }
  };

  const handleDialogSave = async () => {
    // Trigger form submission logic
    if (!validate() || !selectedWorldId) {
      setShowUnsavedChangesDialog(false);
      return;
    }

    try {
      const typedEntityType = entityType as WorldEntityType;
      const hasProperties = customProperties && Object.keys(customProperties).length > 0;
      const properties = hasProperties ? JSON.stringify(customProperties) : undefined;

      if (isEditing && editingEntityId) {
        await updateEntity({
          worldId: selectedWorldId,
          entityId: editingEntityId,
          data: {
            name,
            description,
            properties,
            schemaVersion: ENTITY_SCHEMA_VERSIONS[typedEntityType],
          },
          currentEntityType: existingEntity?.entityType || typedEntityType,
        }).unwrap();
      } else if (newEntityParentId) {
        await createEntity({
          worldId: selectedWorldId,
          data: {
            name,
            description,
            entityType: typedEntityType,
            parentId: newEntityParentId,
            properties,
            schemaVersion: ENTITY_SCHEMA_VERSIONS[typedEntityType],
          },
        }).unwrap();
        dispatch(expandNode(newEntityParentId));
      }

      dispatch(setUnsavedChanges(false));
      setShowUnsavedChangesDialog(false);
      dispatch(closeEntityForm());
    } catch (error) {
      // Error handled by parent component (toast notification)
      setShowUnsavedChangesDialog(false);
      throw error;
    }
  };

  const handleDialogDiscard = () => {
    dispatch(setUnsavedChanges(false));
    setShowUnsavedChangesDialog(false);
    dispatch(closeEntityForm());
  };

  const handleDialogCancel = () => {
    setShowUnsavedChangesDialog(false);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate() || !selectedWorldId) return;

    logger.userAction(isEditing ? 'Update entity' : 'Create entity', {
      entityType,
      hasCustomProperties: !!customProperties && Object.keys(customProperties).length > 0,
    });

    try {
      // Type assertion done once for reuse
      const typedEntityType = entityType as WorldEntityType;
      
      // Only include properties if there's actual data (not empty object or null)
      const hasProperties = customProperties && Object.keys(customProperties).length > 0;
      const properties = hasProperties ? JSON.stringify(customProperties) : undefined;

      if (isEditing && editingEntityId) {
        await updateEntity({
          worldId: selectedWorldId,
          entityId: editingEntityId,
          data: {
            name,
            description,
            entityType: typedEntityType,
            tags: existingEntity?.tags || [],
            properties,
            schemaVersion: ENTITY_SCHEMA_VERSIONS[typedEntityType],
          },
          currentEntityType: existingEntity?.entityType || typedEntityType,
        }).unwrap();
        
        // T029: After successful edit save, return to read-only view
        dispatch(setUnsavedChanges(false));
        dispatch(closeEntityForm());
        dispatch(setSelectedEntity(editingEntityId));
      } else {
        await createEntity({
          worldId: selectedWorldId,
          data: {
            parentId: newEntityParentId,
            name,
            description,
            entityType: typedEntityType,
            tags: [],
            properties,
            schemaVersion: ENTITY_SCHEMA_VERSIONS[typedEntityType],
          },
        }).unwrap();
        
        // Auto-expand parent node to show newly created entity
        if (newEntityParentId) {
          dispatch(expandNode(newEntityParentId));
        }
        
        dispatch(setUnsavedChanges(false));
        dispatch(closeEntityForm());
      }
    } catch (error) {
      logger.error('UI', 'Failed to save entity', { error });
      
      // Log validation errors if present
      if (error && typeof error === 'object' && 'data' in error) {
        const apiError = error as { status?: number; data?: { errors?: Record<string, string[]> } };
        if (apiError.data?.errors) {
          logger.error('API', 'Validation errors from backend', apiError.data.errors);
        }
      }
    }
  }

  const isLoading = (isLoadingEntity && isEditing) || (isLoadingParent && !isEditing && newEntityParentId);

  return (
    <FormLayout onBack={handleClose}>
      <div className="mb-8">
        <h1 className="text-3xl font-bold mb-2">
          {isEditing ? 'Edit Entity' : 'Create Entity'}
        </h1>
        <p className="text-sm text-muted-foreground mb-4">
          {isEditing
            ? 'Update entity details'
            : parentEntity
              ? `Add a new entity under "${parentEntity.name}"`
              : 'Create a new root-level entity'}
        </p>
      </div>

      {isLoading ? (
        <div className="flex justify-center items-center h-96">
          <div className="flex flex-col items-center gap-2 text-muted-foreground">
            <Loader2 className="h-8 w-8 animate-spin" />
            <p>Loading...</p>
          </div>
        </div>
      ) : (
        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label htmlFor="name" className="block text-sm font-medium mb-3">
              Name <span className="text-destructive">*</span>
            </label>
            <Input
              id="name"
              value={name}
              onChange={(e) => {
                setName(e.target.value);
                if (errors.name) {
                  setErrors((prev) => clearFieldError(prev, 'name'));
                }
              }}
              placeholder="Entity name"
              aria-invalid={!!errors.name}
              aria-describedby={errors.name ? 'name-error' : undefined}
              disabled={isSubmitting}
              maxLength={100}
            />
            {errors.name && (
              <span id="name-error" className="text-xs text-destructive block mt-1">
                {errors.name}
              </span>
            )}
          </div>

          <div>
            <label htmlFor="type" className="block text-sm font-medium mb-3">
              Type <span className="text-destructive">*</span>
            </label>
            <EntityTypeSelector
              value={entityType}
              onValueChange={(val) => {
                setEntityType(val);
                if (errors.type) {
                  setErrors((prev) => clearFieldError(prev, 'type'));
                }
              }}
              parentType={parentEntity?.entityType || null}
              allowAllTypes={false}
              disabled={isEditing || isSubmitting}
              placeholder="Select entity type"
              aria-label="Entity type"
              aria-invalid={!!errors.type}
              aria-describedby={errors.type ? 'type-error' : undefined}
            />
            {errors.type && (
              <span id="type-error" className="text-xs text-destructive block mt-1">
                {errors.type}
              </span>
            )}
          </div>

          <div>
            <label htmlFor="description" className="block text-sm font-medium mb-3">
              Description
            </label>
            <Textarea
              id="description"
              value={description}
              onChange={(e) => {
                setDescription(e.target.value);
                if (errors.description) {
                  setErrors((prev) => clearFieldError(prev, 'description'));
                }
              }}
              placeholder="Brief description..."
              aria-invalid={!!errors.description}
              aria-describedby={errors.description ? 'description-error' : 'description-hint'}
              disabled={isSubmitting}
              maxLength={500}
              className="min-h-32"
            />
            {errors.description && (
              <span id="description-error" className="text-xs text-destructive block mt-1">
                {errors.description}
              </span>
            )}
            <div id="description-hint" className="text-xs text-muted-foreground mt-2">
              {description.length}/500 characters
            </div>
          </div>

          {entityType && (
            <DynamicPropertiesForm
              entityType={entityType as WorldEntityType}
              value={customProperties}
              onChange={setCustomProperties}
              disabled={isSubmitting}
            />
          )}

          <FormActions
            submitLabel={isEditing ? 'Save Changes' : 'Create'}
            cancelLabel="Cancel"
            isLoading={isSubmitting}
            isSubmitDisabled={Object.keys(errors).length > 0}
            onCancel={handleClose}
          />
        </form>
        )}

      <UnsavedChangesDialog
        open={showUnsavedChangesDialog}
        onSave={handleDialogSave}
        onDiscard={handleDialogDiscard}
        onCancel={handleDialogCancel}
      />
    </FormLayout>
  );
}
