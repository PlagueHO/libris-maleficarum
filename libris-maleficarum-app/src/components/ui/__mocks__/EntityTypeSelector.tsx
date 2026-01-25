/**
 * Mock EntityTypeSelector for testing
 *
 * This mock renders entity type options inline (not in a portal)
 * to make them accessible in JSDOM test environment.
 */
import { useState } from 'react';
import {
  WorldEntityType,
  getEntityTypeSuggestions,
  getEntityTypeMeta,
} from '@/services/types/worldEntity.types';

export interface EntityTypeSelectorProps {
  value: WorldEntityType | '';
  onValueChange: (type: WorldEntityType) => void;
  parentType?: WorldEntityType | null;
  allowAllTypes?: boolean;
  disabled?: boolean;
  placeholder?: string;
  'aria-label'?: string;
  'aria-invalid'?: boolean;
}

export function EntityTypeSelector({
  value,
  onValueChange,
  parentType,
  allowAllTypes = false,
  disabled = false,
  placeholder = 'Select entity type',
  'aria-label': ariaLabel,
  'aria-invalid': ariaInvalid,
}: EntityTypeSelectorProps) {
  const [open, setOpen] = useState(false);

  // Get recommended types (or all types if allowAllTypes is true)
  const recommendedTypes = allowAllTypes
    ? Object.values(WorldEntityType)
    : getEntityTypeSuggestions(parentType ?? null);

  // Get all types for categorization
  const allTypes = Object.values(WorldEntityType);
  const otherTypes = allTypes.filter((type) => !recommendedTypes.includes(type));

  const selectedMeta = value ? getEntityTypeMeta(value as WorldEntityType) : null;

  const handleSelect = (type: WorldEntityType) => {
    onValueChange(type);
    setOpen(false);
  };

  return (
    <div data-testid="entity-type-selector-mock">
      {/* Trigger Button */}
      <button
        type="button"
        disabled={disabled}
        onClick={() => setOpen(!open)}
        role="combobox"
        aria-expanded={open}
        aria-label={ariaLabel || placeholder}
        aria-invalid={ariaInvalid}
        data-slot="trigger"
      >
        {selectedMeta ? selectedMeta.label : placeholder}
      </button>

      {/* Options List (inline rendering - no portal) */}
      {open && !disabled && (
        <div data-slot="content" role="listbox">
          {/* Recommended Types */}
          {recommendedTypes.length > 0 && (
            <div data-slot="recommended-section">
              <div data-slot="section-label">Recommended</div>
              {recommendedTypes.map((type) => {
                const meta = getEntityTypeMeta(type);
                const isSelected = value === type;
                return (
                  <button
                    key={type}
                    onClick={() => handleSelect(type)}
                    role="option"
                    aria-selected={isSelected}
                    data-type={type}
                    type="button"
                  >
                    {meta.label}
                  </button>
                );
              })}
            </div>
          )}

          {/* Other Types */}
          {otherTypes.length > 0 && (
            <div data-slot="other-section">
              {otherTypes.map((type) => {
                const meta = getEntityTypeMeta(type);
                const isSelected = value === type;
                return (
                  <button
                    key={type}
                    onClick={() => handleSelect(type)}
                    role="option"
                    aria-selected={isSelected}
                    data-type={type}
                    type="button"
                  >
                    {meta.label}
                  </button>
                );
              })}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
