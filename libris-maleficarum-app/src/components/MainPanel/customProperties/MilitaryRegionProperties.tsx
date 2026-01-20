/**
 * MilitaryRegionProperties Component
 *
 * Custom properties form for MilitaryRegion entity type.
 * Fields: CommandStructure, StrategicImportance, MilitaryAssets (TagInput)
 *
 * @module components/MainPanel/customProperties/MilitaryRegionProperties
 */

import { useState, useEffect } from 'react';
import { Textarea } from '@/components/ui/textarea';
import { TagInput } from '@/components/shared/TagInput';

export interface MilitaryRegionPropertiesData {
  CommandStructure?: string;
  StrategicImportance?: string;
  MilitaryAssets?: string[];
}

export interface MilitaryRegionPropertiesProps {
  /** Current property values */
  value: MilitaryRegionPropertiesData;

  /** Called when properties change */
  onChange: (properties: MilitaryRegionPropertiesData) => void;

  /** Whether the form is disabled */
  disabled?: boolean;

  /** Whether the properties are in read-only mode */
  readOnly?: boolean;
}

/**
 * MilitaryRegionProperties component for CommandStructure, StrategicImportance, MilitaryAssets
 */
export function MilitaryRegionProperties({
  value,
  onChange,
  disabled = false,
  readOnly = false,
}: MilitaryRegionPropertiesProps) {
  const [commandStructure, setCommandStructure] = useState(value.CommandStructure || '');
  const [strategicImportance, setStrategicImportance] = useState(value.StrategicImportance || '');
  const [militaryAssets, setMilitaryAssets] = useState<string[]>(value.MilitaryAssets || []);

  // Sync local state with parent value prop
  /* eslint-disable react-hooks/set-state-in-effect */
  useEffect(() => {
    setCommandStructure(value.CommandStructure || '');
    setStrategicImportance(value.StrategicImportance || '');
    setMilitaryAssets(value.MilitaryAssets || []);
  }, [value]);
  /* eslint-enable react-hooks/set-state-in-effect */

  const handleCommandStructureChange = (newStructure: string) => {
    setCommandStructure(newStructure);
    onChange({ ...value, CommandStructure: newStructure || undefined });
  };

  const handleStrategicImportanceChange = (newImportance: string) => {
    setStrategicImportance(newImportance);
    onChange({ ...value, StrategicImportance: newImportance || undefined });
  };

  const handleMilitaryAssetsChange = (newAssets: string[]) => {
    setMilitaryAssets(newAssets);
    onChange({ ...value, MilitaryAssets: newAssets.length > 0 ? newAssets : undefined });
  };

  if (readOnly) {
    return (
      <div className="space-y-4">
        <h3 className="text-lg font-semibold border-b pb-2">
          Military Properties
        </h3>

        {value.CommandStructure && (
          <div>
            <div className="text-sm font-medium text-muted-foreground mb-1">
              Command Structure
            </div>
            <div className="text-sm whitespace-pre-wrap">{value.CommandStructure}</div>
          </div>
        )}

        {value.StrategicImportance && (
          <div>
            <div className="text-sm font-medium text-muted-foreground mb-1">
              Strategic Importance
            </div>
            <div className="text-sm whitespace-pre-wrap">{value.StrategicImportance}</div>
          </div>
        )}

        {value.MilitaryAssets && value.MilitaryAssets.length > 0 && (
          <div>
            <div className="text-sm font-medium text-muted-foreground mb-1">
              Military Assets
            </div>
            <div className="flex flex-wrap gap-2">
              {value.MilitaryAssets.map((asset) => (
                <span
                  key={asset}
                  className="inline-flex items-center px-2.5 py-0.5 rounded-md text-xs font-medium bg-secondary text-secondary-foreground"
                >
                  {asset}
                </span>
              ))}
            </div>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h3 className="text-lg font-semibold border-b pb-2">
        Military Properties
      </h3>

      <div>
        <label htmlFor="commandStructure" className="block text-sm font-medium mb-3">
          Command Structure
        </label>
        <Textarea
          id="commandStructure"
          value={commandStructure}
          onChange={(e) => handleCommandStructureChange(e.target.value)}
          placeholder="Describe the military hierarchy and leadership (e.g., General → Colonel → Captain)..."
          disabled={disabled}
          maxLength={300}
          className="min-h-24"
        />
        <div className="text-xs text-muted-foreground mt-2">
          {commandStructure.length}/300 characters
        </div>
      </div>

      <div>
        <label htmlFor="strategicImportance" className="block text-sm font-medium mb-3">
          Strategic Importance
        </label>
        <Textarea
          id="strategicImportance"
          value={strategicImportance}
          onChange={(e) => handleStrategicImportanceChange(e.target.value)}
          placeholder="Explain why this region is strategically important (e.g., border defense, resource control)..."
          disabled={disabled}
          maxLength={300}
          className="min-h-24"
        />
        <div className="text-xs text-muted-foreground mt-2">
          {strategicImportance.length}/300 characters
        </div>
      </div>

      <div>
        <TagInput
          label="Military Assets"
          value={militaryAssets}
          onChange={handleMilitaryAssetsChange}
          placeholder="Add a military asset..."
          description="Fortifications, bases, units, equipment, or other military resources"
          disabled={disabled}
          maxLength={50}
        />
      </div>
    </div>
  );
}
