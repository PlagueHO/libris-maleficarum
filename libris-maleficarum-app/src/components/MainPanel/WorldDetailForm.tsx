/**
 * WorldDetailForm Component
 *
 * Main panel form for creating and editing worlds.
 * This is a main panel form (not a modal) that works alongside ChatWindow.
 * Supports both create and edit modes with proper validation.
 *
 * @module components/MainPanel/WorldDetailForm
 */

import { useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useCreateWorldMutation, useUpdateWorldMutation } from '@/services/worldApi';
import { closeWorldForm, setUnsavedChanges, selectHasUnsavedChanges } from '@/store/worldSidebarSlice';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { FormActions } from '@/components/ui/form-actions';
import { FormLayout } from '@/components/ui/form-layout';
import type { World } from '@/services/types/world.types';

export interface WorldDetailFormProps {
  /** Form mode: 'create' for new world, 'edit' for existing */
  mode: 'create' | 'edit';

  /** World data (required in edit mode) */
  world?: World;

  /** Optional callback when form is successfully submitted */
  onSuccess?: () => void;
}

/**
 * World detail form component for creating/editing worlds in main panel
 *
 * @param props - Component props
 * @returns Form UI
 */
export function WorldDetailForm({ mode, world, onSuccess }: WorldDetailFormProps) {
  const dispatch = useDispatch();
  const hasUnsavedChanges = useSelector(selectHasUnsavedChanges);

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [errors, setErrors] = useState<{ name?: string }>({});
  const [originalName, setOriginalName] = useState('');
  const [originalDescription, setOriginalDescription] = useState('');

  const [createWorld, { isLoading: isCreating }] = useCreateWorldMutation();
  const [updateWorld, { isLoading: isUpdating }] = useUpdateWorldMutation();

  const isLoading = isCreating || isUpdating;

  /* eslint-disable react-hooks/set-state-in-effect */
  // Handle form initialization and mode switching
  // We need to reset form state when mode/world props change
  useEffect(() => {
    if (mode === 'edit' && world) {
      setName(world.name);
      setDescription(world.description || '');
      setOriginalName(world.name);
      setOriginalDescription(world.description || '');
    } else if (mode === 'create') {
      setName('');
      setDescription('');
      setOriginalName('');
      setOriginalDescription('');
    }
    // Clear validation errors when mode or world changes
    setErrors({});
    // Reset unsaved changes flag
    dispatch(setUnsavedChanges(false));
  }, [mode, world, dispatch]);
  /* eslint-enable react-hooks/set-state-in-effect */

  // Track unsaved changes
  useEffect(() => {
    const hasChanges = name !== originalName || description !== originalDescription;
    if (hasChanges !== hasUnsavedChanges) {
      dispatch(setUnsavedChanges(hasChanges));
    }
  }, [name, description, originalName, originalDescription, hasUnsavedChanges, dispatch]);

  // Warn user about unsaved changes when leaving
  useEffect(() => {
    const handleBeforeUnload = (e: BeforeUnloadEvent) => {
      if (hasUnsavedChanges) {
        e.preventDefault();
        e.returnValue = '';
        return '';
      }
    };

    window.addEventListener('beforeunload', handleBeforeUnload);
    return () => window.removeEventListener('beforeunload', handleBeforeUnload);
  }, [hasUnsavedChanges]);

  // Auto-focus name input when component mounts
  useEffect(() => {
    const timer = setTimeout(() => {
      const nameInput = document.getElementById('world-name-input');
      nameInput?.focus();
    }, 100);
    return () => clearTimeout(timer);
  }, []);

  const validate = (): boolean => {
    const newErrors: { name?: string } = {};

    if (!name.trim()) {
      newErrors.name = 'World name is required';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validate()) {
      return;
    }

    try {
      if (mode === 'create') {
        await createWorld({
          name: name.trim(),
          description: description.trim(),
        }).unwrap();

        // Set the newly created world as selected
        setName('');
        setDescription('');
        dispatch(closeWorldForm());

        if (onSuccess) {
          onSuccess();
        }
      } else if (mode === 'edit' && world) {
        await updateWorld({
          id: world.id,
          data: {
            name: name.trim(),
            description: description.trim(),
          },
        }).unwrap();

        dispatch(closeWorldForm());

        if (onSuccess) {
          onSuccess();
        }
      }
    } catch (error) {
      // Error handling is done by RTK Query
      console.error('Failed to save world:', error);
    }
  };

  const handleCancel = () => {
    setName(mode === 'edit' && world ? world.name : '');
    setDescription(mode === 'edit' && world ? world.description || '' : '');
    setErrors({});
    dispatch(closeWorldForm());
  };

  return (
    <FormLayout onBack={handleCancel}>
      <div className="mb-8">
        <h1 className="text-3xl font-bold mb-2">
          {mode === 'create' ? 'Create World' : 'Edit World'}
        </h1>
        <p className="text-sm text-muted-foreground mb-4">
          {mode === 'create'
            ? 'Create a new world for your campaign'
            : 'Update your world details'}
        </p>
      </div>

      <form onSubmit={handleSubmit} className="space-y-6">
        <div>
          <label htmlFor="world-name-input" className="block text-sm font-medium mb-3">
            World Name <span className="text-destructive">*</span>
          </label>
          <Input
            id="world-name-input"
            type="text"
            value={name}
            onChange={(e) => {
              const newValue = e.target.value;
              if (newValue.length <= 100) {
                setName(newValue);
              }
            }}
            placeholder="Enter world name"
            maxLength={100}
            disabled={isLoading}
            aria-invalid={!!errors.name}
            aria-describedby={errors.name ? 'world-name-error' : undefined}
          />
          {errors.name && (
            <p id="world-name-error" className="text-xs text-destructive block mt-1">
              {errors.name}
            </p>
          )}
          <p className="text-xs text-muted-foreground mt-2">
            {name.length}/100 characters
          </p>
        </div>

        <div>
          <label htmlFor="world-description-input" className="block text-sm font-medium mb-3">
            Description
          </label>
          <Textarea
            id="world-description-input"
            value={description}
            onChange={(e) => {
              const newValue = e.target.value;
              if (newValue.length <= 500) {
                setDescription(newValue);
              }
            }}
            placeholder="Enter world description (optional)"
            maxLength={500}
            disabled={isLoading}
            className="min-h-32"
          />
          <p className="text-xs text-muted-foreground mt-2">
            {description.length}/500 characters
          </p>
        </div>

        <FormActions
          submitLabel={mode === 'create' ? 'Create World' : 'Save World'}
          cancelLabel="Cancel"
          isLoading={isLoading}
          onCancel={handleCancel}
        />
      </form>
    </FormLayout>
  );
}
