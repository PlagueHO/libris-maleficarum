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

/**
 * Props for the TagInput component
 *
 * @public
 */
export interface TagInputProps {
  /**
   * Field label displayed above the input
   * @required
   * @example "Languages", "Member States", "Tags"
   */
  label: string;

  /**
   * Controlled array of tag values currently displayed
   * @required
   * @example ["English", "Spanish", "French"]
   */
  value: string[];

  /**
   * Callback fired when the tag array changes (add or remove)
   * @required
   * @param tags - Updated array of tag values
   * @example (tags) => setMyTags(tags)
   */
  onChange: (tags: string[]) => void;

  /**
   * Placeholder text shown in the input field when empty
   * @optional
   * @default "Type and press Enter"
   */
  placeholder?: string;

  /**
   * Whether the input is disabled and non-interactive
   * @optional
   * @default false
   */
  disabled?: boolean;

  /**
   * Whether the field is required (displays asterisk)
   * @optional
   * @default false
   */
  required?: boolean;

  /**
   * External validation error message to display
   * Overrides internal validation errors
   * @optional
   */
  error?: string;

  /**
   * Maximum character length per individual tag
   * @optional
   * @default 50
   */
  maxLength?: number;

  /**
   * Help text displayed below the label
   * @optional
   * @example "Enter languages spoken in this region"
   */
  description?: string;

  /**
   * Additional CSS class names to apply to the root element
   * @optional
   */
  className?: string;
}

/**
 * TagInput - Reusable component for entering lists of text values as chips/badges
 *
 * Features:
 * - **Add tags**: Type text and press Enter to add
 * - **Remove tags**: Click X button or use dismiss interaction
 * - **Validation**: Prevents duplicates and enforces max length
 * - **Accessibility**: Full keyboard support and ARIA labels
 *
 * @example
 * ```tsx
 * const [languages, setLanguages] = useState<string[]>([]);
 *
 * <TagInput
 *   label="Languages"
 *   value={languages}
 *   onChange={setLanguages}
 *   placeholder="Add a language..."
 *   description="Spoken languages in this region"
 *   maxLength={30}
 * />
 * ```
 *
 * @param props - Component props (see TagInputProps)
 * @returns Accessible tag input component
 *
 * @public
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
  const [highlightedTag, setHighlightedTag] = React.useState<string | null>(null);

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
        // T017: Visual feedback for duplicate tag (500ms highlight)
        setHighlightedTag(trimmed);
        setTimeout(() => setHighlightedTag(null), 500);
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
              className={cn(
                'gap-1 pr-1.5 transition-all',
                highlightedTag === tag && 'ring-2 ring-primary animate-pulse'
              )}
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
