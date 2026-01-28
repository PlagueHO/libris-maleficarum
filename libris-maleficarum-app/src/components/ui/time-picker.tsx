/**
 * TimePicker Component
 *
 * Shadcn/UI-styled time picker with hour and minute selection.
 * Provides an accessible time selection experience.
 *
 * @module components/ui/time-picker
 */

import * as React from 'react';
import { Clock } from 'lucide-react';

import { cn } from '@/lib/utils';
import { Input } from '@/components/ui/input';

/**
 * Props for the TimePicker component
 */
export interface TimePickerProps {
  /**
   * Currently selected time as "HH:mm" string (24-hour format)
   */
  value?: string;

  /**
   * Callback fired when time selection changes
   * @param time - Selected time as "HH:mm" string or undefined if cleared
   */
  onChange?: (time: string | undefined) => void;

  /**
   * Placeholder text when no time is selected
   * @default "Pick a time"
   */
  placeholder?: string;

  /**
   * Whether the picker is disabled
   * @default false
   */
  disabled?: boolean;

  /**
   * Custom className for the container
   */
  className?: string;

  /**
   * Accessible label for the time picker
   */
  'aria-label'?: string;

  /**
   * ID for the time picker input (for label association)
   */
  id?: string;
}

/**
 * TimePicker - Time input with hour:minute selection
 *
 * Uses native time input with enhanced styling for consistency.
 * Stores and returns time in "HH:mm" 24-hour format.
 *
 * @example
 * ```tsx
 * const [time, setTime] = React.useState<string | undefined>();
 *
 * <TimePicker
 *   value={time}
 *   onChange={setTime}
 *   placeholder="Select meeting time"
 * />
 * ```
 */
export function TimePicker({
  value,
  onChange,
  placeholder = 'Pick a time',
  disabled = false,
  className,
  'aria-label': ariaLabel,
  id,
}: TimePickerProps) {
  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newValue = e.target.value;
    onChange?.(newValue || undefined);
  };

  return (
    <div className={cn('relative', className)}>
      <Clock
        className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none"
        aria-hidden="true"
      />
      <Input
        id={id}
        type="time"
        value={value || ''}
        onChange={handleChange}
        disabled={disabled}
        aria-label={ariaLabel ?? placeholder}
        className="pl-10"
        placeholder={placeholder}
      />
    </div>
  );
}
