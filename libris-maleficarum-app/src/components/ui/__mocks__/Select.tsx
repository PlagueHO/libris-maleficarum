/**
 * Mock Select Component for Testing
 *
 * Simplified mock of Radix UI Select that renders inline (no portals)
 * to work around JSDOM limitations in unit tests.
 *
 * Real component uses React portals which don't render in JSDOM test environment.
 * Integration tests still use the real component.
 */

import * as React from 'react';
import { cn } from '@/lib/utils';

interface SelectContextValue {
  value?: string;
  onValueChange?: (value: string) => void;
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

const SelectContext = React.createContext<SelectContextValue>({
  open: false,
  onOpenChange: () => {},
});

function Select({
  value,
  onValueChange,
  children,
  ...props
}: {
  value?: string;
  onValueChange?: (value: string) => void;
  children: React.ReactNode;
  [key: string]: unknown;
}) {
  const [open, setOpen] = React.useState(false);
  const [internalValue, setInternalValue] = React.useState(value);

  React.useEffect(() => {
    setInternalValue(value);
  }, [value]);

  const handleValueChange = (newValue: string) => {
    setInternalValue(newValue);
    onValueChange?.(newValue);
    setOpen(false);
  };

  return (
    <SelectContext.Provider
      value={{
        value: internalValue,
        onValueChange: handleValueChange,
        open,
        onOpenChange: setOpen,
      }}
    >
      <div data-slot="select" {...props}>
        {children}
      </div>
    </SelectContext.Provider>
  );
}

function SelectGroup({ children, ...props }: { children: React.ReactNode; [key: string]: unknown }) {
  return (
    <div data-slot="select-group" role="group" {...props}>
      {children}
    </div>
  );
}

function SelectValue({
  placeholder,
  ...props
}: {
  placeholder?: string;
  [key: string]: unknown;
}) {
  const { value } = React.useContext(SelectContext);
  return (
    <span data-slot="select-value" {...props}>
      {value || placeholder}
    </span>
  );
}

function SelectTrigger({
  className,
  children,
  ...props
}: {
  className?: string;
  children: React.ReactNode;
  [key: string]: unknown;
}) {
  const { open, onOpenChange } = React.useContext(SelectContext);

  return (
    <button
      type="button"
      role="combobox"
      aria-expanded={open}
      aria-haspopup="listbox"
      className={cn(
        'flex w-full items-center justify-between gap-2 rounded-md border border-input bg-transparent px-3 py-2 text-sm shadow-xs',
        className
      )}
      onClick={() => onOpenChange(!open)}
      data-slot="select-trigger"
      {...props}
    >
      {children}
    </button>
  );
}

function SelectContent({
  className,
  children,
  ...props
}: {
  className?: string;
  children: React.ReactNode;
  [key: string]: unknown;
}) {
  const { open } = React.useContext(SelectContext);

  if (!open) return null;

  return (
    <div
      role="listbox"
      data-slot="select-content"
      className={cn('mt-1 rounded-md border border-input bg-background p-1 shadow-md', className)}
      {...props}
    >
      {children}
    </div>
  );
}

function SelectItem({
  value,
  className,
  children,
  ...props
}: {
  value: string;
  className?: string;
  children: React.ReactNode;
  [key: string]: unknown;
}) {
  const { value: selectedValue, onValueChange } = React.useContext(SelectContext);
  const isSelected = selectedValue === value;

  return (
    <div
      role="option"
      aria-selected={isSelected}
      data-slot="select-item"
      className={cn(
        'cursor-pointer rounded px-2 py-1.5 text-sm hover:bg-accent',
        isSelected && 'bg-accent',
        className
      )}
      onClick={() => onValueChange?.(value)}
      {...props}
    >
      {children}
    </div>
  );
}

function SelectLabel({
  className,
  children,
  ...props
}: {
  className?: string;
  children: React.ReactNode;
  [key: string]: unknown;
}) {
  return (
    <div
      data-slot="select-label"
      className={cn('px-2 py-1.5 text-sm font-semibold', className)}
      {...props}
    >
      {children}
    </div>
  );
}

function SelectSeparator({
  className,
  ...props
}: {
  className?: string;
  [key: string]: unknown;
}) {
  return (
    <div
      data-slot="select-separator"
      className={cn('-mx-1 my-1 h-px bg-border', className)}
      {...props}
    />
  );
}

export {
  Select,
  SelectGroup,
  SelectValue,
  SelectTrigger,
  SelectContent,
  SelectItem,
  SelectLabel,
  SelectSeparator,
};
