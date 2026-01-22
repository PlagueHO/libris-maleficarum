import React, { useState, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '../../store/store';
import { closeEntityForm, setUnsavedChanges, expandNode } from '../../store/worldSidebarSlice';
import {
  useCreateWorldEntityMutation,
  useUpdateWorldEntityMutation,
  useGetWorldEntityByIdQuery,
} from '../../services/worldEntityApi';
import { WorldEntityType } from '../../services/types/worldEntity.types';
import { getSchemaVersion } from '../../services/constants/entitySchemaVersions';
import { Input } from '../ui/input';
import { Textarea } from '../ui/textarea';
import { EntityTypeSelector } from '../ui/entity-type-selector';
import { FormActions } from '../ui/form-actions';
import { FormLayout } from '../ui/form-layout';
import { Loader2 } from 'lucide-react';
import {
  GeographicRegionProperties,
  type GeographicRegionPropertiesData,
  PoliticalRegionProperties,
  type PoliticalRegionPropertiesData,
  CulturalRegionProperties,
  type CulturalRegionPropertiesData,
  MilitaryRegionProperties,
  type MilitaryRegionPropertiesData,
} from './customProperties';

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
  const [customProperties, setCustomProperties] = useState<
    | GeographicRegionPropertiesData
    | PoliticalRegionPropertiesData
    | CulturalRegionPropertiesData
    | MilitaryRegionPropertiesData
    | Record<string, unknown>
    | null
  >(null);
  const [errors, setErrors] = useState<{ name?: string; type?: string }>({});

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
  useEffect(() => {
    const hasChanges = name.trim() !== '' || description.trim() !== '' || entityType !== '';
    dispatch(setUnsavedChanges(hasChanges));
    return () => {
      dispatch(setUnsavedChanges(false));
    };
  }, [name, description, entityType, dispatch]);

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
    const newErrors: { name?: string; type?: string } = {};
    if (!name.trim()) newErrors.name = 'Name is required';
    if (!entityType) newErrors.type = 'Type is required';
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleClose = () => {
    dispatch(closeEntityForm());
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validate() || !selectedWorldId) return;

    try {
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
            properties,
            schemaVersion: getSchemaVersion(entityType as WorldEntityType),
          },
        }).unwrap();
      } else {
        await createEntity({
          worldId: selectedWorldId,
          data: {
            parentId: newEntityParentId,
            name,
            description,
            entityType: entityType as WorldEntityType,
            tags: [],
            properties,
            schemaVersion: getSchemaVersion(entityType as WorldEntityType),
          },
        }).unwrap();
        
        // Auto-expand parent node to show newly created entity
        if (newEntityParentId) {
          dispatch(expandNode(newEntityParentId));
        }
      }
      handleClose();
    } catch (error) {
      console.error('Failed to save entity', error);
    }
  }

  const isLoading = (isLoadingEntity && isEditing) || (isLoadingParent && !isEditing && newEntityParentId);

  /**
   * Renders custom property fields based on the selected entity type
   */
  const renderCustomProperties = () => {
    if (!entityType) return null;

    switch (entityType) {
      case WorldEntityType.GeographicRegion:
        return (
          <div className="border-t pt-6 mt-6">
            <GeographicRegionProperties
              value={(customProperties as GeographicRegionPropertiesData) || {}}
              onChange={(props) => setCustomProperties(props)}
              disabled={isSubmitting}
            />
          </div>
        );

      case WorldEntityType.PoliticalRegion:
        return (
          <div className="border-t pt-6 mt-6">
            <PoliticalRegionProperties
              value={(customProperties as PoliticalRegionPropertiesData) || {}}
              onChange={(props) => setCustomProperties(props)}
              disabled={isSubmitting}
            />
          </div>
        );

      case WorldEntityType.CulturalRegion:
        return (
          <div className="border-t pt-6 mt-6">
            <CulturalRegionProperties
              value={(customProperties as CulturalRegionPropertiesData) || {}}
              onChange={(props) => setCustomProperties(props)}
              disabled={isSubmitting}
            />
          </div>
        );

      case WorldEntityType.MilitaryRegion:
        return (
          <div className="border-t pt-6 mt-6">
            <MilitaryRegionProperties
              value={(customProperties as MilitaryRegionPropertiesData) || {}}
              onChange={(props) => setCustomProperties(props)}
              disabled={isSubmitting}
            />
          </div>
        );

      default:
        return null;
    }
  };

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
              onChange={(e) => setName(e.target.value)}
              placeholder="Entity name"
              aria-invalid={!!errors.name}
              disabled={isSubmitting}
              maxLength={100}
            />
            {errors.name && (
              <span className="text-xs text-destructive block mt-1">{errors.name}</span>
            )}
          </div>

          <div>
            <label htmlFor="type" className="block text-sm font-medium mb-3">
              Type <span className="text-destructive">*</span>
            </label>
            <EntityTypeSelector
              value={entityType}
              onValueChange={(val) => setEntityType(val)}
              parentType={parentEntity?.entityType || null}
              allowAllTypes={false}
              disabled={isSubmitting}
              placeholder="Select entity type"
              aria-label="Entity type"
              aria-invalid={!!errors.type}
            />
            {errors.type && (
              <span className="text-xs text-destructive block mt-1">{errors.type}</span>
            )}
          </div>

          <div>
            <label htmlFor="description" className="block text-sm font-medium mb-3">
              Description
            </label>
            <Textarea
              id="description"
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              placeholder="Brief description..."
              disabled={isSubmitting}
              maxLength={500}
              className="min-h-32"
            />
            <div className="text-xs text-muted-foreground mt-2">
              {description.length}/500 characters
            </div>
          </div>

          {renderCustomProperties()}

          <FormActions
            submitLabel={isEditing ? 'Save Changes' : 'Create'}
            cancelLabel="Cancel"
            isLoading={isSubmitting}
            onCancel={handleClose}
          />
        </form>
        )}
    </FormLayout>
  );
}
