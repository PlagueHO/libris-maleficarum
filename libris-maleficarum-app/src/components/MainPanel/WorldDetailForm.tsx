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
import { useDispatch } from 'react-redux';
import { useCreateWorldMutation, useUpdateWorldMutation } from '@/services/worldApi';
import { closeWorldForm } from '@/store/worldSidebarSlice';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import type { World } from '@/services/types/world.types';
import styles from './WorldDetailForm.module.css';

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

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [errors, setErrors] = useState<{ name?: string }>({});

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
    } else if (mode === 'create') {
      setName('');
      setDescription('');
    }
    // Clear validation errors when mode or world changes
    setErrors({});
  }, [mode, world]);
  /* eslint-enable react-hooks/set-state-in-effect */

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

  const isSubmitDisabled = isLoading;

  return (
    <main className={styles.container}>
      <div className={styles.formWrapper}>
        <div className={styles.header}>
          <h1 className={styles.title}>
            {mode === 'create' ? 'Create World' : 'Edit World'}
          </h1>
          <p className={styles.subtitle}>
            {mode === 'create'
              ? 'Create a new world for your campaign'
              : 'Update your world details'}
          </p>
        </div>

        <form onSubmit={handleSubmit} className={styles.form}>
          <div className={styles.formGroup}>
            <label htmlFor="world-name-input" className={styles.label}>
              World Name <span className={styles.required}>*</span>
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
              className={errors.name ? styles.inputError : ''}
              aria-invalid={!!errors.name}
              aria-describedby={errors.name ? 'world-name-error' : undefined}
            />
            {errors.name && (
              <p id="world-name-error" className={styles.errorMessage}>
                {errors.name}
              </p>
            )}
            <p className={styles.charCount}>
              {name.length}/100 characters
            </p>
          </div>

          <div className={styles.formGroup}>
            <label htmlFor="world-description-input" className={styles.label}>
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
              className={styles.textarea}
            />
            <p className={styles.charCount}>
              {description.length}/500 characters
            </p>
          </div>

          <div className={styles.actions}>
            <Button
              type="submit"
              disabled={isSubmitDisabled}
              className={styles.submitButton}
            >
              {isLoading && <span className={styles.spinner} aria-hidden="true">⋯</span>}
              {mode === 'create' ? 'Create' : 'Save'} World
            </Button>
            <Button
              type="button"
              variant="outline"
              onClick={handleCancel}
              disabled={isLoading}
              className={styles.cancelButton}
            >
              Cancel
            </Button>
          </div>
        </form>
      </div>
    </main>
  );
}
