/**
 * TagInput Component
 *
 * Reusable tag/chip input component for text list properties.
 * Users can type text and press Enter to add tags, with dismiss buttons to remove.
 *
 * Features:
 * - Keyboard handling (Enter to add, X button to remove)
 * - Duplicate prevention
 * - Max length validation
 * - Accessible with ARIA labels
 *
 * @module components/shared/TagInput
 */

import * as React from 'react';
import { X } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';

export interface TagInputProps {
  /** Field label */
  label: string;

  /** Array of tag values */
  value: string[];

  /** Callback when tags change */
  onChange: (tags: string[]) => void;

  /** Placeholder text for input */
  placeholder?: string;

  /** Disabled state */
  disabled?: boolean;

  /** Required field */
  required?: boolean;

  /** Validation error message */
  error?: string;

  /** Maximum length per tag */
  maxLength?: number;

  /** Optional description/hint text */
  description?: string;

  /** Additional className */
  className?: string;
}

/**
 * TagInput component for entering lists of text values as chips
 */
export function TagInput({
  label,
  value,
  onChange,
  placeholder = 'Type and press Enter',
  disabled = false,
  required = false,
  error,
  maxLength = 50,
  description,
  className,
}: TagInputProps) {
  const [inputValue, setInputValue] = React.useState('');
  const [localError, setLocalError] = React.useState<string>();

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && inputValue.trim()) {
      e.preventDefault();
      const trimmed = inputValue.trim();

      // Validation
      if (trimmed.length > maxLength) {
        setLocalError(`Tag must be ${maxLength} characters or less`);
        return;
      }

      if (value.includes(trimmed)) {
        setLocalError('Tag already exists');
        return;
      }

      // Add tag
      onChange([...value, trimmed]);
      setInputValue('');
      setLocalError(undefined);
    }
  };

  const handleRemoveTag = (tagToRemove: string) => {
    onChange(value.filter((tag) => tag !== tagToRemove));
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setInputValue(e.target.value);
    setLocalError(undefined); // Clear error on input change
  };

  const displayError = error || localError;

  return (
    <div className={cn('flex flex-col gap-2', className)}>
      {/* Label */}
      <label
        htmlFor={`tag-input-${label}`}
        className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
      >
        {label}
        {required && <span className="text-destructive ml-1">*</span>}
      </label>

      {/* Description */}
      {description && (
        <p className="text-sm text-muted-foreground">{description}</p>
      )}

      {/* Tags display */}
      {value.length > 0 && (
        <div
          className="flex flex-wrap gap-2"
          role="list"
          aria-label={`${label} tags`}
        >
          {value.map((tag) => (
            <Badge
              key={tag}
              variant="secondary"
              className="gap-1 pr-1.5"
              role="listitem"
            >
              <span>{tag}</span>
              <button
                type="button"
                onClick={() => handleRemoveTag(tag)}
                disabled={disabled}
                className="rounded-full hover:bg-secondary-foreground/10 p-0.5 transition-colors disabled:pointer-events-none disabled:opacity-50"
                aria-label={`Remove ${tag}`}
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          ))}
        </div>
      )}

      {/* Input field */}
      <Input
        id={`tag-input-${label}`}
        type="text"
        value={inputValue}
        onChange={handleInputChange}
        onKeyDown={handleKeyDown}
        placeholder={placeholder}
        disabled={disabled}
        aria-invalid={!!displayError}
        aria-describedby={
          displayError ? `tag-input-error-${label}` : undefined
        }
        aria-label={`${label} input`}
      />

      {/* Error message */}
      {displayError && (
        <p
          id={`tag-input-error-${label}`}
          className="text-sm text-destructive"
          role="alert"
        >
          {displayError}
        </p>
      )}
    </div>
  );
}
