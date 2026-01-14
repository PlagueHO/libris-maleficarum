/**
 * WorldFormModal Component
 *
 * Modal form for creating and editing worlds.
 * Supports both create and edit modes with proper validation.
 *
 * @module components/WorldSidebar/WorldFormModal
 */

import { useEffect, useState } from 'react';
import { useCreateWorldMutation, useUpdateWorldMutation } from '@/services/worldApi';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import type { World } from '@/services/types/world.types';
import styles from './WorldFormModal.module.css';

export interface WorldFormModalProps {
  /** Whether the modal is open */
  isOpen: boolean;

  /** Modal mode: 'create' for new world, 'edit' for existing */
  mode: 'create' | 'edit';

  /** World data (required in edit mode) */
  world?: World;

  /** Callback when modal should close */
  onClose: () => void;
}

/**
 * World form modal component for creating/editing worlds
 *
 * @param props - Component props
 * @returns Modal UI
 */
export function WorldFormModal({
  isOpen,
  mode,
  world,
  onClose,
}: WorldFormModalProps) {
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [errors, setErrors] = useState<{ name?: string }>({});

  const [createWorld, { isLoading: isCreating }] = useCreateWorldMutation();
  const [updateWorld, { isLoading: isUpdating }] = useUpdateWorldMutation();

  const isLoading = isCreating || isUpdating;

  // Pre-populate form in edit mode
  // Synchronize form state with modal props on mode change
  useEffect(() => {
    if (mode === 'edit' && world) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setName(world.name);
      setDescription(world.description || '');
    } else {
      setName('');
      setDescription('');
    }
    setErrors({});
  }, [mode, world, isOpen]);

  // Auto-focus name input when modal opens
  useEffect(() => {
    if (isOpen) {
      const timer = setTimeout(() => {
        const nameInput = document.getElementById('world-name-input');
        nameInput?.focus();
      }, 100);
      return () => clearTimeout(timer);
    }
  }, [isOpen]);

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
          description: description.trim() || undefined,
        }).unwrap();
      } else if (world) {
        await updateWorld({
          id: world.id,
          data: {
            name: name.trim(),
            description: description.trim() || undefined,
          },
        }).unwrap();
      }

      // Reset form and close modal
      setName('');
      setDescription('');
      setErrors({});
      onClose();
    } catch (error) {
      console.error('Failed to save world:', error);
    }
  };

  const handleCancel = () => {
    // Discard changes and close
    setName('');
    setDescription('');
    setErrors({});
    onClose();
  };

  const handleNameChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value.slice(0, 100); // Max 100 characters
    setName(value);
    if (errors.name && value.trim()) {
      setErrors((prev) => ({ ...prev, name: undefined }));
    }
  };

  const handleDescriptionChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    const value = e.target.value.slice(0, 500); // Max 500 characters
    setDescription(value);
  };

  const isSubmitDisabled = !name.trim() || isLoading;
  const submitButtonText = isLoading
    ? mode === 'create'
      ? 'Creating...'
      : 'Saving...'
    : mode === 'create'
      ? 'Create'
      : 'Save';

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && handleCancel()}>
      <DialogContent
        className={styles.dialogContent}
        aria-labelledby="world-form-title"
        aria-describedby="world-form-description"
      >
        <DialogHeader>
          <DialogTitle id="world-form-title">
            {mode === 'create' ? 'Create World' : 'Edit World'}
          </DialogTitle>
          <DialogDescription id="world-form-description">
            {mode === 'create'
              ? 'Create a new world for your campaign.'
              : 'Update world details.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className={styles.form}>
          <div className={styles.field}>
            <label htmlFor="world-name-input" className={styles.label}>
              World Name
              <span className={styles.required} aria-label="required">
                *
              </span>
            </label>
            <input
              id="world-name-input"
              type="text"
              value={name}
              onChange={handleNameChange}
              className={styles.input}
              placeholder="e.g., Forgotten Realms"
              maxLength={100}
              required
              aria-required="true"
              aria-invalid={!!errors.name}
              aria-describedby={errors.name ? 'name-error' : undefined}
            />
            {errors.name && (
              <p id="name-error" className={styles.error} role="alert">
                {errors.name}
              </p>
            )}
          </div>

          <div className={styles.field}>
            <label htmlFor="world-description-input" className={styles.label}>
              Description (optional)
            </label>
            <textarea
              id="world-description-input"
              value={description}
              onChange={handleDescriptionChange}
              className={styles.textarea}
              placeholder="A brief description of your world..."
              maxLength={500}
              rows={4}
              aria-label="Description"
            />
            <p className={styles.hint}>
              {description.length}/500 characters
            </p>
          </div>

          <DialogFooter className={styles.footer}>
            <button
              type="button"
              onClick={handleCancel}
              className={styles.cancelButton}
              disabled={isLoading}
            >
              Cancel
            </button>
            <button
              type="submit"
              className={styles.submitButton}
              disabled={isSubmitDisabled}
              aria-busy={isLoading}
            >
              {submitButtonText}
            </button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
