import React, { useState, useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import type { RootState } from '../../store/store';
import { closeEntityForm } from '../../store/worldSidebarSlice';
import {
  useCreateWorldEntityMutation,
  useUpdateWorldEntityMutation,
  useGetWorldEntityByIdQuery,
} from '../../services/worldEntityApi';
import {
  WorldEntityType,
  getEntityTypeSuggestions,
} from '../../services/types/worldEntity.types';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from '../ui/dialog';
import { Input } from '../ui/input';
import { Textarea } from '../ui/textarea';
import { Button } from '../ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '../ui/select';
import { Loader2 } from 'lucide-react';

export function EntityFormModal() {
  const dispatch = useDispatch();
  const {
    isEntityFormOpen,
    editingEntityId,
    newEntityParentId,
    selectedWorldId,
  } = useSelector((state: RootState) => state.worldSidebar);

  // Form State
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [entityType, setEntityType] = useState<WorldEntityType | ''>('');
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

  // Determine available entity types based on context
  const availableTypes = useMemo(() => {
    if (isEditing) {
      // Allow all types when editing (or could be restricted)
      return Object.values(WorldEntityType);
    }

    if (newEntityParentId) {
      // Child entity: depends on parent type
      if (!parentEntity) return []; // Wait for parent to load
      return getEntityTypeSuggestions(parentEntity.entityType);
    }

    // Root entity
    return getEntityTypeSuggestions(null);
  }, [isEditing, newEntityParentId, parentEntity]);

  // Mutations
  const [createEntity, { isLoading: isCreating }] =
    useCreateWorldEntityMutation();
  const [updateEntity, { isLoading: isUpdating }] =
    useUpdateWorldEntityMutation();

  const isSubmitting = isCreating || isUpdating;

  // Sync form state when entity data loads or modal state changes
  // Note: This pattern is acceptable for form synchronization with external data
  useEffect(() => {
    if (!isEntityFormOpen) {
      // Reset on close
      setName('');
      setDescription('');
      setEntityType('');
      setErrors({});
    } else if (isEditing && existingEntity) {
      // Populate on edit mode
      setName(existingEntity.name);
      setDescription(existingEntity.description || '');
      setEntityType(existingEntity.entityType);
    } else if (!isEditing) {
      // Reset for create mode
      setName('');
      setDescription('');
      setEntityType('');
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isEntityFormOpen, editingEntityId, existingEntity?.id]);

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
      if (isEditing && editingEntityId) {
        await updateEntity({
          worldId: selectedWorldId,
          entityId: editingEntityId,
          data: {
            name,
            description,
            // entityType is usually immutable after creation, but if backend allows:
            // entityType: entityType as WorldEntityType,
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
          },
        }).unwrap();
      }
      handleClose();
    } catch (error) {
      console.error('Failed to save entity', error);
      // Could set generic error state here
    }
  };

  // Skip rendering if no world selected (shouldn't happen if triggered correctly)
  if (!selectedWorldId) return null;

  return (
    <Dialog open={isEntityFormOpen} onOpenChange={(open) => !open && handleClose()}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? 'Edit Entity' : 'Create Entity'}
          </DialogTitle>
          <DialogDescription className="sr-only">
            {isEditing ? 'Form to edit an existing entity' : 'Form to create a new entity'}
          </DialogDescription>
        </DialogHeader>

        {(isLoadingEntity && isEditing) || (isLoadingParent && !isEditing && newEntityParentId) ? (
          <div className="flex justify-center p-4">
            <Loader2 className="h-6 w-6 animate-spin" />
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="grid gap-4 py-4">
            <div className="grid gap-2">
              <label htmlFor="name" className="text-sm font-medium">
                Name
              </label>
              <Input
                id="name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Entity name"
                aria-invalid={!!errors.name}
              />
              {errors.name && (
                <span className="text-xs text-red-500">{errors.name}</span>
              )}
            </div>

            <div className="grid gap-2">
              <label htmlFor="type" className="text-sm font-medium">
                Type
              </label>
              <Select
                value={entityType}
                onValueChange={(val) => setEntityType(val as WorldEntityType)}
                disabled={isLoadingParent} // Disable while loading parent context
              >
                <SelectTrigger id="type" aria-label="Type">
                  <SelectValue placeholder="Select type" />
                </SelectTrigger>
                <SelectContent>
                  {availableTypes.map((type) => (
                    <SelectItem key={type} value={type}>
                      {type}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {availableTypes.length === 0 && !isLoadingParent && !isEditing && (
                <span className="text-xs text-amber-500">
                  No valid child types available for this parent.
                </span>
              )}
              {errors.type && (
                <span className="text-xs text-red-500">{errors.type}</span>
              )}
            </div>

            <div className="grid gap-2">
              <label htmlFor="description" className="text-sm font-medium">
                Description
              </label>
              <Textarea
                id="description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                placeholder="Brief description..."
              />
            </div>

            <DialogFooter>
              <Button type="button" variant="outline" onClick={handleClose}>
                Cancel
              </Button>
              <Button type="submit" disabled={isSubmitting}>
                {isSubmitting && (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                )}
                {isEditing ? 'Save Changes' : 'Create'}
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
}

