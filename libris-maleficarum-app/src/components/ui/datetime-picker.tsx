/**
 * DateTimePicker Component
 *
 * Shadcn/UI DateTimePicker that combines date and time selection.
 * Provides date picker popover plus time input for complete datetime selection.
 *
 * @module components/ui/datetime-picker
 */

import * as React from 'react';
import { format, parse, setHours, setMinutes } from 'date-fns';
import { CalendarIcon } from 'lucide-react';

import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Calendar } from '@/components/ui/calendar';
import { Input } from '@/components/ui/input';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';

/**
 * Props for the DateTimePicker component
 */
export interface DateTimePickerProps {
  /**
   * Currently selected datetime
   */
  value?: Date;

  /**
   * Callback fired when datetime selection changes
   * @param datetime - Selected datetime or undefined if cleared
   */
  onChange?: (datetime: Date | undefined) => void;

  /**
   * Placeholder text when no datetime is selected
   * @default "Pick date and time"
   */
  placeholder?: string;

  /**
   * Whether the picker is disabled
   * @default false
   */
  disabled?: boolean;

  /**
   * Custom className for the trigger button
   */
  className?: string;

  /**
   * Accessible label for the datetime picker
   */
  'aria-label'?: string;

  /**
   * ID for the datetime picker trigger (for label association)
   */
  id?: string;
}

/**
 * DateTimePicker - Combined date and time selection
 *
 * Uses a calendar popover for date selection and a time input for time.
 * Returns a complete Date object with both date and time components.
 *
 * @example
 * ```tsx
 * const [datetime, setDatetime] = React.useState<Date | undefined>();
 *
 * <DateTimePicker
 *   value={datetime}
 *   onChange={setDatetime}
 *   placeholder="Select session start"
 * />
 * ```
 */
export function DateTimePicker({
  value,
  onChange,
  placeholder = 'Pick date and time',
  disabled = false,
  className,
  'aria-label': ariaLabel,
  id,
}: DateTimePickerProps) {
  const [open, setOpen] = React.useState(false);

  // Extract time string from Date
  const timeValue = value ? format(value, 'HH:mm') : '';

  const handleDateSelect = (date: Date | undefined) => {
    if (!date) {
      onChange?.(undefined);
      return;
    }

    // Preserve existing time if set, otherwise use current date's time or 00:00
    if (value) {
      const hours = value.getHours();
      const minutes = value.getMinutes();
      const newDateTime = setMinutes(setHours(date, hours), minutes);
      onChange?.(newDateTime);
    } else {
      onChange?.(date);
    }
  };

  const handleTimeChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const timeString = e.target.value;
    if (!timeString) {
      // Keep date, clear time (set to 00:00)
      if (value) {
        const newDateTime = setMinutes(setHours(value, 0), 0);
        onChange?.(newDateTime);
      }
      return;
    }

    // Parse time and apply to current date
    const parsed = parse(timeString, 'HH:mm', new Date());
    const hours = parsed.getHours();
    const minutes = parsed.getMinutes();

    if (value) {
      const newDateTime = setMinutes(setHours(value, hours), minutes);
      onChange?.(newDateTime);
    } else {
      // No date selected yet, use today with the selected time
      const today = new Date();
      const newDateTime = setMinutes(setHours(today, hours), minutes);
      onChange?.(newDateTime);
    }
  };

  return (
    <div className={cn('flex gap-2', className)}>
      <Popover open={open} onOpenChange={setOpen}>
        <PopoverTrigger asChild>
          <Button
            id={id}
            variant="outline"
            disabled={disabled}
            aria-label={ariaLabel ?? placeholder}
            aria-expanded={open}
            className={cn(
              'flex-1 justify-start text-left font-normal',
              !value && 'text-muted-foreground',
            )}
          >
            <CalendarIcon className="mr-2 size-4" aria-hidden="true" />
            {value ? format(value, 'PPP') : <span>{placeholder}</span>}
          </Button>
        </PopoverTrigger>
        <PopoverContent className="w-auto p-0" align="start">
          <Calendar
            mode="single"
            selected={value}
            onSelect={handleDateSelect}
            initialFocus
          />
        </PopoverContent>
      </Popover>
      <Input
        type="time"
        value={timeValue}
        onChange={handleTimeChange}
        disabled={disabled}
        className="w-28"
        aria-label="Time"
      />
    </div>
  );
}
