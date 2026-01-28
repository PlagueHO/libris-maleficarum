/**
 * DatePicker Component
 *
 * Shadcn/UI DatePicker that combines a button trigger with a calendar popover.
 * Provides an accessible date selection experience.
 *
 * @module components/ui/date-picker
 */

import * as React from 'react';
import { format } from 'date-fns';
import { CalendarIcon } from 'lucide-react';

import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/button';
import { Calendar } from '@/components/ui/calendar';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';

/**
 * Props for the DatePicker component
 */
export interface DatePickerProps {
  /**
   * Currently selected date
   */
  value?: Date;

  /**
   * Callback fired when date selection changes
   * @param date - Selected date or undefined if cleared
   */
  onChange?: (date: Date | undefined) => void;

  /**
   * Placeholder text when no date is selected
   * @default "Pick a date"
   */
  placeholder?: string;

  /**
   * Date format string (date-fns format)
   * @default "PPP" (e.g., "January 28, 2026")
   */
  dateFormat?: string;

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
   * Accessible label for the date picker
   */
  'aria-label'?: string;

  /**
   * ID for the date picker trigger (for label association)
   */
  id?: string;
}

/**
 * DatePicker - Button-triggered calendar popover for date selection
 *
 * Combines a trigger button with a calendar popover for an accessible
 * date picking experience. Supports keyboard navigation and screen readers.
 *
 * @example
 * ```tsx
 * const [date, setDate] = React.useState<Date | undefined>();
 *
 * <DatePicker
 *   value={date}
 *   onChange={setDate}
 *   placeholder="Select session date"
 * />
 * ```
 */
export function DatePicker({
  value,
  onChange,
  placeholder = 'Pick a date',
  dateFormat = 'PPP',
  disabled = false,
  className,
  'aria-label': ariaLabel,
  id,
}: DatePickerProps) {
  const [open, setOpen] = React.useState(false);

  const handleSelect = (date: Date | undefined) => {
    onChange?.(date);
    setOpen(false);
  };

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          id={id}
          variant="outline"
          disabled={disabled}
          aria-label={ariaLabel ?? placeholder}
          aria-expanded={open}
          className={cn(
            'w-full justify-start text-left font-normal',
            !value && 'text-muted-foreground',
            className,
          )}
        >
          <CalendarIcon className="mr-2 size-4" aria-hidden="true" />
          {value ? format(value, dateFormat) : <span>{placeholder}</span>}
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-auto p-0" align="start">
        <Calendar
          mode="single"
          selected={value}
          onSelect={handleSelect}
          initialFocus
        />
      </PopoverContent>
    </Popover>
  );
}
