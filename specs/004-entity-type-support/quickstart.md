# Quickstart: Container Entity Type Support

**Feature**: Container Entity Type Support  
**Date**: 2026-01-20  
**Audience**: Developers implementing this feature

## Overview

This guide walks you through implementing Container and Regional Entity Type support in the Libris Maleficarum frontend. You'll add 14 new entity types, build a reusable TagInput component, and extend EntityDetailForm to support custom properties.

**Time Estimate**: 8-12 hours for full implementation + tests

---

## Prerequisites

- Node.js 20+ and pnpm installed
- VS Code with recommended extensions
- Feature branch `004-entity-type-support` checked out
- Familiarity with React 19, TypeScript, Fluent UI v9, Vitest

---

## Step 1: Extend WorldEntityType Enum (30 min)

### 1.1 Update worldEntity.types.ts

**File**: `libris-maleficarum-app/src/services/types/worldEntity.types.ts`

Add 14 new entity types to the `WorldEntityType` const object:

```typescript
export const WorldEntityType = {
  // Existing types (keep all)
  Continent: 'Continent',
  // ... existing types ...
  
  // NEW: Container types
  Locations: 'Locations',
  People: 'People',
  Events: 'Events',
  History: 'History',
  Lore: 'Lore',
  Bestiary: 'Bestiary',
  Items: 'Items',
  Adventures: 'Adventures',
  Geographies: 'Geographies',
  
  // NEW: Regional types with custom properties
  GeographicRegion: 'GeographicRegion',
  PoliticalRegion: 'PoliticalRegion',
  CulturalRegion: 'CulturalRegion',
  MilitaryRegion: 'MilitaryRegion',
} as const;
```

**Note**: `Other` already exists; reuse for Container category.

### 1.2 Update ENTITY_TYPE_META

Add metadata for new types:

```typescript
export const ENTITY_TYPE_META: Record<WorldEntityType, {
  label: string;
  description: string;
  category: 'Geography' | 'Characters & Factions' | 'Events & Quests' | 'Items' | 'Campaigns' | 'Containers' | 'Other';
  icon: string;  // Add icon property
}> = {
  // Existing entries...
  
  // NEW: Container types
  [WorldEntityType.Locations]: {
    label: 'Locations',
    description: 'Container for geographic and spatial entities',
    category: 'Containers',
    icon: 'FolderRegular',
  },
  // ... add remaining 8 Container types ...
  
  // NEW: Regional types
  [WorldEntityType.GeographicRegion]: {
    label: 'Geographic Region',
    description: 'Natural geographic area with climate and terrain properties',
    category: 'Geography',
    icon: 'GlobeRegular',
  },
  // ... add remaining 3 regional types ...
};
```

**Reference**: See `specs/004-entity-type-support/data-model.md` for full metadata.

### 1.3 Update ENTITY_TYPE_SUGGESTIONS

Add suggestion mappings for Container and Regional types:

```typescript
export const ENTITY_TYPE_SUGGESTIONS: Record<WorldEntityType, WorldEntityType[]> = {
  // Existing entries...
  
  // NEW: Container suggestions
  [WorldEntityType.Locations]: [
    WorldEntityType.Continent,
    WorldEntityType.GeographicRegion,
    WorldEntityType.PoliticalRegion,
    WorldEntityType.Country,
    // ... etc
  ],
  // ... add remaining 8 Container types ...
  
  // NEW: Regional type suggestions
  [WorldEntityType.GeographicRegion]: [
    WorldEntityType.Country,
    WorldEntityType.Province,
    WorldEntityType.Region,
    WorldEntityType.GeographicRegion,  // Can nest
  ],
  // ... add remaining 3 regional types ...
  
  // UPDATE: Existing types to recommend regional types
  [WorldEntityType.Continent]: [
    WorldEntityType.GeographicRegion,  // NEW
    WorldEntityType.PoliticalRegion,   // NEW
    WorldEntityType.Country,
    // ... existing entries
  ],
};
```

**Reference**: See `specs/004-entity-type-support/data-model.md` for complete mappings.

### 1.4 Update getEntityTypeSuggestions

Modify root-level suggestions to include Container types:

```typescript
export function getEntityTypeSuggestions(
  parentType: WorldEntityType | null,
): WorldEntityType[] {
  if (!parentType) {
    // Root level suggestions - add Container types
    return [
      WorldEntityType.Locations,
      WorldEntityType.People,
      WorldEntityType.Events,
      WorldEntityType.Adventures,
      WorldEntityType.Lore,
      WorldEntityType.Continent,
      WorldEntityType.Campaign,
    ];
  }

  return ENTITY_TYPE_SUGGESTIONS[parentType] ?? [];
}
```

### 1.5 Write Tests

**File**: `libris-maleficarum-app/src/services/types/worldEntity.types.test.ts`

```typescript
import { describe, it, expect } from 'vitest';
import { WorldEntityType, ENTITY_TYPE_META, getEntityTypeSuggestions } from './worldEntity.types';

describe('WorldEntityType enum', () => {
  it('includes all Container types', () => {
    expect(WorldEntityType.Locations).toBe('Locations');
    expect(WorldEntityType.People).toBe('People');
    // ... test all 9 container types
  });

  it('includes all Regional types', () => {
    expect(WorldEntityType.GeographicRegion).toBe('GeographicRegion');
    // ... test all 4 regional types
  });
});

describe('ENTITY_TYPE_META', () => {
  it('has metadata for all Container types', () => {
    expect(ENTITY_TYPE_META[WorldEntityType.Locations].category).toBe('Containers');
    expect(ENTITY_TYPE_META[WorldEntityType.Locations].icon).toBe('FolderRegular');
  });

  it('has metadata for all Regional types', () => {
    expect(ENTITY_TYPE_META[WorldEntityType.GeographicRegion].category).toBe('Geography');
    expect(ENTITY_TYPE_META[WorldEntityType.GeographicRegion].icon).toBe('GlobeRegular');
  });
});

describe('getEntityTypeSuggestions', () => {
  it('suggests Container types for root level', () => {
    const suggestions = getEntityTypeSuggestions(null);
    expect(suggestions).toContain(WorldEntityType.Locations);
    expect(suggestions).toContain(WorldEntityType.People);
  });

  it('suggests regional types for Continent parent', () => {
    const suggestions = getEntityTypeSuggestions(WorldEntityType.Continent);
    expect(suggestions).toContain(WorldEntityType.GeographicRegion);
    expect(suggestions).toContain(WorldEntityType.PoliticalRegion);
  });
});
```

**Run tests**: `pnpm test worldEntity.types.test.ts`

---

## Step 2: Create TagInput Component (2-3 hours)

### 2.1 Create Component Structure

**Files to create**:

- `libris-maleficarum-app/src/components/shared/TagInput/TagInput.tsx`
- `libris-maleficarum-app/src/components/shared/TagInput/TagInput.module.css`
- `libris-maleficarum-app/src/components/shared/TagInput/TagInput.test.tsx`
- `libris-maleficarum-app/src/components/shared/TagInput/index.ts` (barrel export)

### 2.2 Implement TagInput Component

**File**: `TagInput.tsx`

```typescript
import React, { useState, KeyboardEvent } from 'react';
import { Field, Input, Tag, TagGroup } from '@fluentui/react-components';
import { Dismiss24Regular } from '@fluentui/react-icons';
import styles from './TagInput.module.css';

export interface TagInputProps {
  label: string;
  value: string[];  // Array of tags
  onChange: (tags: string[]) => void;
  placeholder?: string;
  disabled?: boolean;
  required?: boolean;
  validationMessage?: string;
  maxLength?: number;  // Max length per tag
}

export function TagInput({
  label,
  value,
  onChange,
  placeholder = 'Type and press Enter',
  disabled = false,
  required = false,
  validationMessage,
  maxLength = 50,
}: TagInputProps) {
  const [inputValue, setInputValue] = useState('');

  const handleKeyDown = (e: KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter' && inputValue.trim()) {
      e.preventDefault();
      const trimmed = inputValue.trim();
      if (trimmed.length > maxLength) {
        // Could add validation message here
        return;
      }
      if (!value.includes(trimmed)) {
        onChange([...value, trimmed]);
      }
      setInputValue('');
    }
  };

  const handleRemoveTag = (tagToRemove: string) => {
    onChange(value.filter(tag => tag !== tagToRemove));
  };

  return (
    <Field
      label={label}
      required={required}
      validationMessage={validationMessage}
      className={styles.field}
    >
      <div className={styles.container}>
        {value.length > 0 && (
          <TagGroup className={styles.tagGroup} role="list">
            {value.map((tag) => (
              <Tag
                key={tag}
                dismissible
                dismissIcon={<Dismiss24Regular />}
                onDismiss={() => handleRemoveTag(tag)}
                role="listitem"
                aria-label={`Tag: ${tag}`}
              >
                {tag}
              </Tag>
            ))}
          </TagGroup>
        )}
        <Input
          type="text"
          value={inputValue}
          onChange={(e) => setInputValue(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={placeholder}
          disabled={disabled}
          className={styles.input}
          aria-label={`${label} input`}
        />
      </div>
    </Field>
  );
}
```

### 2.3 Add Styles

**File**: `TagInput.module.css`

```css
.field {
  margin-bottom: var(--spacingVerticalM);
}

.container {
  display: flex;
  flex-direction: column;
  gap: var(--spacingVerticalS);
}

.tagGroup {
  display: flex;
  flex-wrap: wrap;
  gap: var(--spacingHorizontalXS);
}

.input {
  width: 100%;
}
```

### 2.4 Write Tests

**File**: `TagInput.test.tsx`

```typescript
import React from 'react';
import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe } from 'jest-axe';
import { TagInput } from './TagInput';

describe('TagInput', () => {
  it('renders with label and input', () => {
    render(<TagInput label="Languages" value={[]} onChange={vi.fn()} />);
    expect(screen.getByLabelText('Languages input')).toBeInTheDocument();
  });

  it('adds tag on Enter key', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<TagInput label="Languages" value={[]} onChange={onChange} />);
    
    const input = screen.getByLabelText('Languages input');
    await user.type(input, 'English{Enter}');
    
    expect(onChange).toHaveBeenCalledWith(['English']);
  });

  it('displays existing tags', () => {
    render(<TagInput label="Languages" value={['English', 'French']} onChange={vi.fn()} />);
    expect(screen.getByText('English')).toBeInTheDocument();
    expect(screen.getByText('French')).toBeInTheDocument();
  });

  it('removes tag on dismiss', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<TagInput label="Languages" value={['English', 'French']} onChange={onChange} />);
    
    const dismissButtons = screen.getAllByRole('button', { name: /dismiss/i });
    await user.click(dismissButtons[0]);
    
    expect(onChange).toHaveBeenCalledWith(['French']);
  });

  it('prevents duplicate tags', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<TagInput label="Languages" value={['English']} onChange={onChange} />);
    
    const input = screen.getByLabelText('Languages input');
    await user.type(input, 'English{Enter}');
    
    expect(onChange).not.toHaveBeenCalled();
  });

  it('has no accessibility violations', async () => {
    const { container } = render(<TagInput label="Languages" value={['English']} onChange={vi.fn()} />);
    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });
});
```

**Run tests**: `pnpm test TagInput.test.tsx`

---

## Step 3: Create Numeric Validation Utility (1 hour)

### 3.1 Create Validator

**File**: `libris-maleficarum-app/src/lib/validators/numericValidation.ts`

```typescript
/**
 * Numeric validation utilities for entity custom properties
 */

const MAX_SAFE = Number.MAX_SAFE_INTEGER;

export interface NumericValidationResult {
  valid: boolean;
  error?: string;
  value?: number;
}

/**
 * Parse numeric input with optional comma separators
 * @param input - User input (e.g., "1,000,000" or "1000000")
 * @returns Parsed number or null if invalid
 */
export function parseNumericInput(input: string): number | null {
  if (!input || input.trim() === '') return null;
  
  // Remove commas and trim
  const cleaned = input.replace(/,/g, '').trim();
  
  // Check if it's a valid number
  if (!/^-?\d+(\.\d+)?$/.test(cleaned)) {
    return null;
  }
  
  const num = parseFloat(cleaned);
  return isNaN(num) ? null : num;
}

/**
 * Format number with thousand separators
 * @param value - Numeric value
 * @param decimals - Decimal places (default: 0 for integers)
 * @returns Formatted string (e.g., "1,000,000")
 */
export function formatNumericDisplay(value: number, decimals: number = 0): string {
  return new Intl.NumberFormat('en-US', {
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals,
  }).format(value);
}

/**
 * Validate integer field (Population)
 */
export function validateInteger(input: string): NumericValidationResult {
  const value = parseNumericInput(input);
  
  if (value === null) {
    return { valid: false, error: 'Must be a valid number' };
  }
  
  if (!Number.isInteger(value)) {
    return { valid: false, error: 'Must be a whole number' };
  }
  
  if (value < 0) {
    return { valid: false, error: 'Must be non-negative' };
  }
  
  if (value > MAX_SAFE) {
    return { valid: false, error: `Must be less than ${formatNumericDisplay(MAX_SAFE)}` };
  }
  
  return { valid: true, value };
}

/**
 * Validate decimal field (Area)
 */
export function validateDecimal(input: string): NumericValidationResult {
  const value = parseNumericInput(input);
  
  if (value === null) {
    return { valid: false, error: 'Must be a valid number' };
  }
  
  if (value < 0) {
    return { valid: false, error: 'Must be non-negative' };
  }
  
  if (value > MAX_SAFE) {
    return { valid: false, error: `Must be less than ${formatNumericDisplay(MAX_SAFE)}` };
  }
  
  return { valid: true, value };
}
```

### 3.2 Write Tests

**File**: `libris-maleficarum-app/src/lib/validators/numericValidation.test.ts`

```typescript
import { describe, it, expect } from 'vitest';
import { parseNumericInput, formatNumericDisplay, validateInteger, validateDecimal } from './numericValidation';

describe('parseNumericInput', () => {
  it('parses number with commas', () => {
    expect(parseNumericInput('1,000,000')).toBe(1000000);
  });

  it('parses plain number', () => {
    expect(parseNumericInput('1000000')).toBe(1000000);
  });

  it('parses decimal', () => {
    expect(parseNumericInput('1234.56')).toBe(1234.56);
  });

  it('returns null for invalid input', () => {
    expect(parseNumericInput('abc')).toBeNull();
    expect(parseNumericInput('')).toBeNull();
  });
});

describe('formatNumericDisplay', () => {
  it('formats integer with thousand separators', () => {
    expect(formatNumericDisplay(1000000)).toBe('1,000,000');
  });

  it('formats decimal with specified precision', () => {
    expect(formatNumericDisplay(1234.5678, 2)).toBe('1,234.57');
  });
});

describe('validateInteger', () => {
  it('validates positive integer', () => {
    const result = validateInteger('1000');
    expect(result.valid).toBe(true);
    expect(result.value).toBe(1000);
  });

  it('rejects negative number', () => {
    const result = validateInteger('-100');
    expect(result.valid).toBe(false);
    expect(result.error).toContain('non-negative');
  });

  it('rejects decimal', () => {
    const result = validateInteger('100.5');
    expect(result.valid).toBe(false);
    expect(result.error).toContain('whole number');
  });

  it('rejects number > MAX_SAFE_INTEGER', () => {
    const result = validateInteger('9007199254740992');
    expect(result.valid).toBe(false);
  });
});

describe('validateDecimal', () => {
  it('validates positive decimal', () => {
    const result = validateDecimal('1234.56');
    expect(result.valid).toBe(true);
    expect(result.value).toBe(1234.56);
  });

  it('validates integer as decimal', () => {
    const result = validateDecimal('1000');
    expect(result.valid).toBe(true);
  });

  it('rejects negative number', () => {
    const result = validateDecimal('-100.5');
    expect(result.valid).toBe(false);
  });
});
```

**Run tests**: `pnpm test numericValidation.test.ts`

---

## Step 4: Extend EntityDetailForm (3-4 hours)

### 4.1 Create Custom Property Components

Create helper components for each regional type's properties:

**File**: `libris-maleficarum-app/src/components/MainPanel/EntityDetailForm/GeographicRegionProperties.tsx`

```typescript
import React from 'react';
import { Input, Combobox, Option } from '@fluentui/react-components';
import { Field } from '@fluentui/react-components';
import { validateInteger, validateDecimal, formatNumericDisplay } from '../../../lib/validators/numericValidation';
import styles from './EntityDetailForm.module.css';

export interface GeographicRegionPropertiesProps {
  properties: {
    Climate?: string;
    Terrain?: string;
    Population?: string;  // Stored as string for input handling
    Area?: string;
  };
  onChange: (properties: Record<string, unknown>) => void;
  disabled?: boolean;
}

const CLIMATE_OPTIONS = ['Tropical', 'Arid', 'Temperate', 'Continental', 'Polar'];
const TERRAIN_OPTIONS = ['Mountains', 'Plains', 'Forest', 'Desert', 'Mixed'];

export function GeographicRegionProperties({ properties, onChange, disabled }: GeographicRegionPropertiesProps) {
  const [populationError, setPopulationError] = React.useState<string>();
  const [areaError, setAreaError] = React.useState<string>();

  const handlePopulationChange = (value: string) => {
    onChange({ ...properties, Population: value });
    
    if (value) {
      const result = validateInteger(value);
      setPopulationError(result.valid ? undefined : result.error);
    } else {
      setPopulationError(undefined);
    }
  };

  const handleAreaChange = (value: string) => {
    onChange({ ...properties, Area: value });
    
    if (value) {
      const result = validateDecimal(value);
      setAreaError(result.valid ? undefined : result.error);
    } else {
      setAreaError(undefined);
    }
  };

  return (
    <div className={styles.customProperties}>
      <Field label="Climate">
        <Combobox
          value={properties.Climate || ''}
          onOptionSelect={(_, data) => onChange({ ...properties, Climate: data.optionValue })}
          disabled={disabled}
          placeholder="Select climate type"
        >
          {CLIMATE_OPTIONS.map((option) => (
            <Option key={option} value={option}>{option}</Option>
          ))}
        </Combobox>
      </Field>

      <Field label="Terrain">
        <Combobox
          value={properties.Terrain || ''}
          onOptionSelect={(_, data) => onChange({ ...properties, Terrain: data.optionValue })}
          disabled={disabled}
          placeholder="Select terrain type"
        >
          {TERRAIN_OPTIONS.map((option) => (
            <Option key={option} value={option}>{option}</Option>
          ))}
        </Combobox>
      </Field>

      <Field label="Population (optional)" validationMessage={populationError}>
        <Input
          type="text"
          inputMode="numeric"
          value={properties.Population || ''}
          onChange={(e) => handlePopulationChange(e.target.value)}
          disabled={disabled}
          placeholder="e.g., 195,000,000"
        />
      </Field>

      <Field label="Area (km², optional)" validationMessage={areaError}>
        <Input
          type="text"
          inputMode="numeric"
          value={properties.Area || ''}
          onChange={(e) => handleAreaChange(e.target.value)}
          disabled={disabled}
          placeholder="e.g., 1,234,567.89"
        />
      </Field>
    </div>
  );
}
```

**Repeat** for PoliticalRegionProperties, CulturalRegionProperties, MilitaryRegionProperties.

### 4.2 Update EntityDetailForm

**File**: `libris-maleficarum-app/src/components/MainPanel/EntityDetailForm.tsx`

Add import:

```typescript
import { GeographicRegionProperties } from './EntityDetailForm/GeographicRegionProperties';
// ... import other property components
```

Add state for custom properties:

```typescript
const [customProperties, setCustomProperties] = useState<Record<string, unknown>>({});
```

Add conditional rendering function:

```typescript
function renderCustomProperties() {
  if (!entityType) return null;

  switch (entityType) {
    case WorldEntityType.GeographicRegion:
      return (
        <GeographicRegionProperties
          properties={customProperties}
          onChange={setCustomProperties}
          disabled={isSubmitting}
        />
      );
    case WorldEntityType.PoliticalRegion:
      // ... similar
    case WorldEntityType.CulturalRegion:
      // ... similar
    case WorldEntityType.MilitaryRegion:
      // ... similar
    default:
      return null;
  }
}
```

Update JSX to include custom properties:

```tsx
{/* Existing fields: name, description, entity type */}

{/* NEW: Custom properties section */}
{renderCustomProperties()}

{/* Existing: form actions */}
```

Update submit handler to include Properties:

```typescript
const handleSubmit = async (e: React.FormEvent) => {
  e.preventDefault();
  
  // ... validation ...
  
  const entityData = {
    parentId: isEditing ? existingEntity.parentId : newEntityParentId,
    entityType: entityType as WorldEntityType,
    name,
    description,
    tags: [],  // Existing
    Properties: Object.keys(customProperties).length > 0 ? customProperties : undefined,
  };
  
  // ... submit ...
};
```

### 4.3 Update Tests

**File**: `EntityDetailForm.test.tsx`

```typescript
it('renders custom property fields for GeographicRegion', () => {
  // Setup state with GeographicRegion selected
  render(<EntityDetailForm />);
  
  // Select GeographicRegion type
  // ... trigger type selection ...
  
  expect(screen.getByLabelText('Climate')).toBeInTheDocument();
  expect(screen.getByLabelText('Terrain')).toBeInTheDocument();
  expect(screen.getByLabelText(/Population/)).toBeInTheDocument();
  expect(screen.getByLabelText(/Area/)).toBeInTheDocument();
});

it('submits entity with custom properties', async () => {
  // ... test submission with Properties field ...
});
```

---

## Step 5: Update WorldSidebar Icons (1 hour)

### 5.1 Import Icons

**File**: `libris-maleficarum-app/src/components/SidePanel/WorldSidebar.tsx`

```typescript
import {
  FolderRegular,
  PeopleRegular,
  CalendarRegular,
  BookRegular,
  PawRegular,
  BoxRegular,
  CompassRegular,
  MapRegular,
  GlobeRegular,
  ShieldRegular,
} from '@fluentui/react-icons';
```

### 5.2 Create Icon Mapping Function

```typescript
function getEntityIcon(entityType: WorldEntityType): JSX.Element {
  const iconName = ENTITY_TYPE_META[entityType]?.icon;
  
  switch (iconName) {
    case 'FolderRegular': return <FolderRegular />;
    case 'PeopleRegular': return <PeopleRegular />;
    case 'CalendarRegular': return <CalendarRegular />;
    case 'BookRegular': return <BookRegular />;
    case 'PawRegular': return <PawRegular />;
    case 'BoxRegular': return <BoxRegular />;
    case 'CompassRegular': return <CompassRegular />;
    case 'MapRegular': return <MapRegular />;
    case 'GlobeRegular': return <GlobeRegular />;
    case 'ShieldRegular': return <ShieldRegular />;
    default: return <FolderRegular />;  // Fallback
  }
}
```

### 5.3 Use Icon in Rendering

```tsx
{entities.map((entity) => (
  <div key={entity.id} className={styles.entityRow}>
    {getEntityIcon(entity.entityType)}
    <span>{entity.name}</span>
  </div>
))}
```

### 5.4 Add Accessibility

Ensure icons have `aria-hidden="true"` since they're decorative (text label provides context):

```tsx
<span aria-hidden="true">{getEntityIcon(entity.entityType)}</span>
```

---

## Step 6: Run Full Test Suite

### 6.1 Unit Tests

```bash
pnpm test
```

Expected: All tests pass with >80% coverage.

### 6.2 Accessibility Tests

Verify jest-axe tests pass for all new/modified components:

```bash
pnpm test --grep "accessibility"
```

### 6.3 Manual Testing Checklist

- [ ] Create a Container entity (Locations) at root level
- [ ] Verify Container icon displays correctly
- [ ] Create a GeographicRegion under a Continent
- [ ] Fill in Climate, Terrain, Population, Area
- [ ] Submit and verify Properties are saved
- [ ] Refresh page and verify custom properties display in read-only mode
- [ ] Test TagInput with PoliticalRegion (Member States)
- [ ] Add multiple tags, remove tags, verify keyboard navigation
- [ ] Test numeric validation (enter negative number, exceed max, enter decimals)
- [ ] Verify error messages display correctly

---

## Step 7: Update Agent Context

Run the agent context update script to add new technologies:

```bash
.\.specify\scripts\powershell\update-agent-context.ps1 -AgentType copilot
```

This updates `.github/copilot-instructions.md` with references to new components.

---

## Next Steps

After completing this quickstart:

1. Run `/speckit.tasks` to generate task breakdown
1. Create PR with feature branch
1. Request code review
1. Address feedback
1. Merge to main

---

## Troubleshooting

**Issue**: Type errors in worldEntity.types.ts  
**Solution**: Ensure all new types are added to the const object AND to ENTITY_TYPE_META and ENTITY_TYPE_SUGGESTIONS.

**Issue**: TagInput tests failing  
**Solution**: Ensure `@testing-library/user-event` is installed: `pnpm add -D @testing-library/user-event`

**Issue**: Fluent icons not found  
**Solution**: Ensure `@fluentui/react-icons` is installed: `pnpm add @fluentui/react-icons`

**Issue**: Numeric validation not working  
**Solution**: Check that `parseNumericInput` is stripping commas correctly. Test with console.log.

---

## Resources

- [Fluent UI v9 Documentation](https://react.fluentui.dev/)
- [Vitest Documentation](https://vitest.dev/)
- [jest-axe Documentation](https://github.com/nickcolley/jest-axe)
- [Feature Specification](./spec.md)
- [Data Model Design](./data-model.md)
- [API Contracts](./contracts/API.md)

---

**Estimated Total Time**: 8-12 hours (including tests and documentation)

**Complexity**: Medium (requires TypeScript enums, React components, validation logic, accessibility)

**Risk Areas**:

- Numeric validation edge cases
- Icon import paths
- TagInput keyboard interactions
- Custom property serialization
