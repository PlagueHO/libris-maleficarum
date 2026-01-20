/**
 * CulturalRegionProperties Component
 *
 * Custom properties form for CulturalRegion entity type.
 * Fields: Languages (TagInput), Religions (TagInput), CulturalTraits
 *
 * @module components/MainPanel/customProperties/CulturalRegionProperties
 */

import { useState, useEffect } from 'react';
import { Textarea } from '@/components/ui/textarea';
import { TagInput } from '@/components/shared/TagInput';

export interface CulturalRegionPropertiesData {
  Languages?: string[];
  Religions?: string[];
  CulturalTraits?: string;
}

export interface CulturalRegionPropertiesProps {
  /** Current property values */
  value: CulturalRegionPropertiesData;

  /** Called when properties change */
  onChange: (properties: CulturalRegionPropertiesData) => void;

  /** Whether the form is disabled */
  disabled?: boolean;

  /** Whether the properties are in read-only mode */
  readOnly?: boolean;
}

/**
 * CulturalRegionProperties component for Languages, Religions, CulturalTraits
 */
export function CulturalRegionProperties({
  value,
  onChange,
  disabled = false,
  readOnly = false,
}: CulturalRegionPropertiesProps) {
  const [languages, setLanguages] = useState<string[]>(value.Languages || []);
  const [religions, setReligions] = useState<string[]>(value.Religions || []);
  const [culturalTraits, setCulturalTraits] = useState(value.CulturalTraits || '');

  // Sync local state with parent value prop
  /* eslint-disable react-hooks/set-state-in-effect */
  useEffect(() => {
    setLanguages(value.Languages || []);
    setReligions(value.Religions || []);
    setCulturalTraits(value.CulturalTraits || '');
  }, [value]);
  /* eslint-enable react-hooks/set-state-in-effect */

  const handleLanguagesChange = (newLanguages: string[]) => {
    setLanguages(newLanguages);
    onChange({ ...value, Languages: newLanguages.length > 0 ? newLanguages : undefined });
  };

  const handleReligionsChange = (newReligions: string[]) => {
    setReligions(newReligions);
    onChange({ ...value, Religions: newReligions.length > 0 ? newReligions : undefined });
  };

  const handleCulturalTraitsChange = (newTraits: string) => {
    setCulturalTraits(newTraits);
    onChange({ ...value, CulturalTraits: newTraits || undefined });
  };

  if (readOnly) {
    return (
      <div className="space-y-4">
        <h3 className="text-lg font-semibold border-b pb-2">
          Cultural Properties
        </h3>

        {value.Languages && value.Languages.length > 0 && (
          <div>
            <div className="text-sm font-medium text-muted-foreground mb-1">
              Languages
            </div>
            <div className="flex flex-wrap gap-2">
              {value.Languages.map((language) => (
                <span
                  key={language}
                  className="inline-flex items-center px-2.5 py-0.5 rounded-md text-xs font-medium bg-secondary text-secondary-foreground"
                >
                  {language}
                </span>
              ))}
            </div>
          </div>
        )}

        {value.Religions && value.Religions.length > 0 && (
          <div>
            <div className="text-sm font-medium text-muted-foreground mb-1">
              Religions
            </div>
            <div className="flex flex-wrap gap-2">
              {value.Religions.map((religion) => (
                <span
                  key={religion}
                  className="inline-flex items-center px-2.5 py-0.5 rounded-md text-xs font-medium bg-secondary text-secondary-foreground"
                >
                  {religion}
                </span>
              ))}
            </div>
          </div>
        )}

        {value.CulturalTraits && (
          <div>
            <div className="text-sm font-medium text-muted-foreground mb-1">
              Cultural Traits
            </div>
            <div className="text-sm whitespace-pre-wrap">{value.CulturalTraits}</div>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h3 className="text-lg font-semibold border-b pb-2">
        Cultural Properties
      </h3>

      <div>
        <TagInput
          label="Languages"
          value={languages}
          onChange={handleLanguagesChange}
          placeholder="Add a language..."
          description="Spoken or written languages in this cultural region"
          disabled={disabled}
          maxLength={50}
        />
      </div>

      <div>
        <TagInput
          label="Religions"
          value={religions}
          onChange={handleReligionsChange}
          placeholder="Add a religion..."
          description="Religious beliefs and practices in this region"
          disabled={disabled}
          maxLength={50}
        />
      </div>

      <div>
        <label htmlFor="culturalTraits" className="block text-sm font-medium mb-3">
          Cultural Traits
        </label>
        <Textarea
          id="culturalTraits"
          value={culturalTraits}
          onChange={(e) => handleCulturalTraitsChange(e.target.value)}
          placeholder="Describe unique cultural characteristics, customs, traditions, arts..."
          disabled={disabled}
          maxLength={500}
          className="min-h-32"
        />
        <div className="text-xs text-muted-foreground mt-2">
          {culturalTraits.length}/500 characters
        </div>
      </div>
    </div>
  );
}
