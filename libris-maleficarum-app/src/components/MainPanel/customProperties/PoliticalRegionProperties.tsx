/**
 * PoliticalRegionProperties Component
 *
 * Custom properties form for PoliticalRegion entity type.
 * Fields: GovernmentType, MemberStates (TagInput), EstablishedDate
 *
 * @module components/MainPanel/customProperties/PoliticalRegionProperties
 */

import { useState, useEffect } from 'react';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { TagInput } from '@/components/shared/TagInput';

export interface PoliticalRegionPropertiesData {
  GovernmentType?: string;
  MemberStates?: string[];
  EstablishedDate?: string;
}

export interface PoliticalRegionPropertiesProps {
  /** Current property values */
  value: PoliticalRegionPropertiesData;

  /** Called when properties change */
  onChange: (properties: PoliticalRegionPropertiesData) => void;

  /** Whether the form is disabled */
  disabled?: boolean;

  /** Whether the properties are in read-only mode */
  readOnly?: boolean;
}

/**
 * PoliticalRegionProperties component for GovernmentType, MemberStates, EstablishedDate
 */
export function PoliticalRegionProperties({
  value,
  onChange,
  disabled = false,
  readOnly = false,
}: PoliticalRegionPropertiesProps) {
  const [governmentType, setGovernmentType] = useState(value.GovernmentType || '');
  const [memberStates, setMemberStates] = useState<string[]>(value.MemberStates || []);
  const [establishedDate, setEstablishedDate] = useState(value.EstablishedDate || '');

  // Sync local state with parent value prop
  /* eslint-disable react-hooks/set-state-in-effect */
  useEffect(() => {
    setGovernmentType(value.GovernmentType || '');
    setMemberStates(value.MemberStates || []);
    setEstablishedDate(value.EstablishedDate || '');
  }, [value]);
  /* eslint-enable react-hooks/set-state-in-effect */

  const handleGovernmentTypeChange = (newType: string) => {
    setGovernmentType(newType);
    onChange({ ...value, GovernmentType: newType || undefined });
  };

  const handleMemberStatesChange = (newStates: string[]) => {
    setMemberStates(newStates);
    onChange({ ...value, MemberStates: newStates.length > 0 ? newStates : undefined });
  };

  const handleEstablishedDateChange = (newDate: string) => {
    setEstablishedDate(newDate);
    onChange({ ...value, EstablishedDate: newDate || undefined });
  };

  if (readOnly) {
    return (
      <div className="space-y-4">
        <h3 className="text-lg font-semibold border-b pb-2">
          Political Properties
        </h3>

        {value.GovernmentType && (
          <div>
            <div className="text-sm font-medium text-muted-foreground mb-1">
              Government Type
            </div>
            <div className="text-sm">{value.GovernmentType}</div>
          </div>
        )}

        {value.MemberStates && value.MemberStates.length > 0 && (
          <div>
            <div className="text-sm font-medium text-muted-foreground mb-1">
              Member States
            </div>
            <div className="flex flex-wrap gap-2">
              {value.MemberStates.map((state) => (
                <span
                  key={state}
                  className="inline-flex items-center px-2.5 py-0.5 rounded-md text-xs font-medium bg-secondary text-secondary-foreground"
                >
                  {state}
                </span>
              ))}
            </div>
          </div>
        )}

        {value.EstablishedDate && (
          <div>
            <div className="text-sm font-medium text-muted-foreground mb-1">
              Established Date
            </div>
            <div className="text-sm">{value.EstablishedDate}</div>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h3 className="text-lg font-semibold border-b pb-2">
        Political Properties
      </h3>

      <div>
        <label htmlFor="governmentType" className="block text-sm font-medium mb-3">
          Government Type
        </label>
        <Textarea
          id="governmentType"
          value={governmentType}
          onChange={(e) => handleGovernmentTypeChange(e.target.value)}
          placeholder="e.g., Democracy, Monarchy, Federation, Empire..."
          disabled={disabled}
          maxLength={200}
          className="min-h-24"
        />
        <div className="text-xs text-muted-foreground mt-2">
          {governmentType.length}/200 characters
        </div>
      </div>

      <div>
        <TagInput
          label="Member States"
          value={memberStates}
          onChange={handleMemberStatesChange}
          placeholder="Add a member state..."
          description="States, provinces, or territories within this political region"
          disabled={disabled}
          maxLength={50}
        />
      </div>

      <div>
        <label htmlFor="establishedDate" className="block text-sm font-medium mb-3">
          Established Date
        </label>
        <Input
          id="establishedDate"
          type="text"
          value={establishedDate}
          onChange={(e) => handleEstablishedDateChange(e.target.value)}
          placeholder="e.g., Year 1456, 3rd Age, Spring 2024..."
          disabled={disabled}
          maxLength={100}
        />
        <div className="text-xs text-muted-foreground mt-2">
          Free-form date (supports fantasy calendars)
        </div>
      </div>
    </div>
  );
}
