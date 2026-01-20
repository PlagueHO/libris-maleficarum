/**
 * GeographicRegionProperties Component
 *
 * Custom properties form for GeographicRegion entity type.
 * Fields: Climate, Terrain, Population (integer), Area (decimal)
 *
 * @module components/MainPanel/customProperties/GeographicRegionProperties
 */

import { useState, useEffect } from 'react';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import {
  validateInteger,
  validateDecimal,
  formatNumericDisplay,
} from '@/lib/validators/numericValidation';

export interface GeographicRegionPropertiesData {
  Climate?: string;
  Terrain?: string;
  Population?: number;
  Area?: number;
}

export interface GeographicRegionPropertiesProps {
  /** Current property values */
  value: GeographicRegionPropertiesData;

  /** Called when properties change */
  onChange: (properties: GeographicRegionPropertiesData) => void;

  /** Whether the form is disabled */
  disabled?: boolean;

  /** Whether the properties are in read-only mode */
  readOnly?: boolean;
}

/**
 * GeographicRegionProperties component for Climate, Terrain, Population, Area
 */
export function GeographicRegionProperties({
  value,
  onChange,
  disabled = false,
  readOnly = false,
}: GeographicRegionPropertiesProps) {
  const [climate, setClimate] = useState(value.Climate || '');
  const [terrain, setTerrain] = useState(value.Terrain || '');
  const [population, setPopulation] = useState(
    value.Population ? formatNumericDisplay(value.Population) : ''
  );
  const [area, setArea] = useState(
    value.Area ? formatNumericDisplay(value.Area, 2) : ''
  );

  const [errors, setErrors] = useState<{
    Population?: string;
    Area?: string;
  }>({});

  // Sync local state with parent value prop
  /* eslint-disable react-hooks/set-state-in-effect */
  useEffect(() => {
    setClimate(value.Climate || '');
    setTerrain(value.Terrain || '');
    setPopulation(value.Population ? formatNumericDisplay(value.Population) : '');
    setArea(value.Area ? formatNumericDisplay(value.Area, 2) : '');
  }, [value]);
  /* eslint-enable react-hooks/set-state-in-effect */

  const handleClimateChange = (newClimate: string) => {
    setClimate(newClimate);
    onChange({ ...value, Climate: newClimate || undefined });
  };

  const handleTerrainChange = (newTerrain: string) => {
    setTerrain(newTerrain);
    onChange({ ...value, Terrain: newTerrain || undefined });
  };

  const handlePopulationChange = (input: string) => {
    setPopulation(input);

    // Validate integer
    const validation = validateInteger(input);
    if (!validation.valid) {
      setErrors({ ...errors, Population: validation.error });
      return;
    }

    // Clear error and update parent
    setErrors({ ...errors, Population: undefined });
    onChange({ ...value, Population: validation.value });
  };

  const handleAreaChange = (input: string) => {
    setArea(input);

    // Validate decimal
    const validation = validateDecimal(input);
    if (!validation.valid) {
      setErrors({ ...errors, Area: validation.error });
      return;
    }

    // Clear error and update parent
    setErrors({ ...errors, Area: undefined });
    onChange({ ...value, Area: validation.value });
  };

  const handlePopulationBlur = () => {
    // Format on blur if valid number
    if (value.Population !== undefined) {
      setPopulation(formatNumericDisplay(value.Population));
    }
  };

  const handleAreaBlur = () => {
    // Format on blur if valid number
    if (value.Area !== undefined) {
      setArea(formatNumericDisplay(value.Area, 2));
    }
  };

  if (readOnly) {
    return (
      <div className="space-y-4">
        <h3 className="text-lg font-semibold border-b pb-2">
          Geographic Properties
        </h3>

        {value.Climate && (
          <div>
            <div className="text-sm font-medium text-muted-foreground mb-1">
              Climate
            </div>
            <div className="text-sm">{value.Climate}</div>
          </div>
        )}

        {value.Terrain && (
          <div>
            <div className="text-sm font-medium text-muted-foreground mb-1">
              Terrain
            </div>
            <div className="text-sm">{value.Terrain}</div>
          </div>
        )}

        {value.Population !== undefined && (
          <div>
            <div className="text-sm font-medium text-muted-foreground mb-1">
              Population
            </div>
            <div className="text-sm">{formatNumericDisplay(value.Population)}</div>
          </div>
        )}

        {value.Area !== undefined && (
          <div>
            <div className="text-sm font-medium text-muted-foreground mb-1">
              Area (sq km)
            </div>
            <div className="text-sm">{formatNumericDisplay(value.Area, 2)}</div>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <h3 className="text-lg font-semibold border-b pb-2">
        Geographic Properties
      </h3>

      <div>
        <label htmlFor="climate" className="block text-sm font-medium mb-3">
          Climate
        </label>
        <Textarea
          id="climate"
          value={climate}
          onChange={(e) => handleClimateChange(e.target.value)}
          placeholder="Describe the climate (e.g., temperate, tropical, arid)..."
          disabled={disabled}
          maxLength={200}
          className="min-h-24"
        />
        <div className="text-xs text-muted-foreground mt-2">
          {climate.length}/200 characters
        </div>
      </div>

      <div>
        <label htmlFor="terrain" className="block text-sm font-medium mb-3">
          Terrain
        </label>
        <Textarea
          id="terrain"
          value={terrain}
          onChange={(e) => handleTerrainChange(e.target.value)}
          placeholder="Describe the terrain (e.g., mountainous, coastal, plains)..."
          disabled={disabled}
          maxLength={200}
          className="min-h-24"
        />
        <div className="text-xs text-muted-foreground mt-2">
          {terrain.length}/200 characters
        </div>
      </div>

      <div>
        <label htmlFor="population" className="block text-sm font-medium mb-3">
          Population
        </label>
        <Input
          id="population"
          type="text"
          inputMode="numeric"
          value={population}
          onChange={(e) => handlePopulationChange(e.target.value)}
          onBlur={handlePopulationBlur}
          placeholder="e.g., 1,000,000"
          disabled={disabled}
          aria-invalid={!!errors.Population}
          aria-describedby={errors.Population ? 'population-error' : undefined}
        />
        {errors.Population && (
          <span
            id="population-error"
            className="text-xs text-destructive block mt-1"
            role="alert"
          >
            {errors.Population}
          </span>
        )}
        <div className="text-xs text-muted-foreground mt-2">
          Whole number only
        </div>
      </div>

      <div>
        <label htmlFor="area" className="block text-sm font-medium mb-3">
          Area (sq km)
        </label>
        <Input
          id="area"
          type="text"
          inputMode="decimal"
          value={area}
          onChange={(e) => handleAreaChange(e.target.value)}
          onBlur={handleAreaBlur}
          placeholder="e.g., 150,000.50"
          disabled={disabled}
          aria-invalid={!!errors.Area}
          aria-describedby={errors.Area ? 'area-error' : undefined}
        />
        {errors.Area && (
          <span
            id="area-error"
            className="text-xs text-destructive block mt-1"
            role="alert"
          >
            {errors.Area}
          </span>
        )}
        <div className="text-xs text-muted-foreground mt-2">
          Decimal values allowed
        </div>
      </div>
    </div>
  );
}
