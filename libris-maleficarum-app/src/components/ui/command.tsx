import * as React from 'react';
import { Command as CommandPrimitive } from 'cmdk';

import { cn } from '@/lib/utils';

function Command({ className, ...props }: React.ComponentPropsWithoutRef<typeof CommandPrimitive>) {
  return (
    <CommandPrimitive
      className={cn('flex h-full w-full flex-col overflow-hidden rounded-md bg-popover text-popover-foreground', className)}
      {...props}
    />
  );
}

function CommandInput({ className, ...props }: React.ComponentPropsWithoutRef<typeof CommandPrimitive.Input>) {
  return (
    <CommandPrimitive.Input
      className={cn(
        'h-10 w-full border-b border-input bg-transparent px-3 py-2 text-sm outline-none placeholder:text-muted-foreground disabled:cursor-not-allowed disabled:opacity-50',
        className,
      )}
      {...props}
    />
  );
}

function CommandList({ className, ...props }: React.ComponentPropsWithoutRef<typeof CommandPrimitive.List>) {
  return <CommandPrimitive.List className={cn('max-h-75 overflow-y-auto overflow-x-hidden', className)} {...props} />;
}

function CommandEmpty({ className, ...props }: React.ComponentPropsWithoutRef<typeof CommandPrimitive.Empty>) {
  return <CommandPrimitive.Empty className={cn('px-3 py-4 text-sm text-muted-foreground', className)} {...props} />;
}

function CommandGroup({ className, ...props }: React.ComponentPropsWithoutRef<typeof CommandPrimitive.Group>) {
  return <CommandPrimitive.Group className={cn('overflow-hidden p-1 text-foreground', className)} {...props} />;
}

function CommandItem({ className, ...props }: React.ComponentPropsWithoutRef<typeof CommandPrimitive.Item>) {
  return (
    <CommandPrimitive.Item
      className={cn(
        'relative flex cursor-default select-none items-center rounded-sm px-2 py-1.5 text-sm outline-none aria-selected:bg-accent aria-selected:text-accent-foreground data-[disabled=true]:pointer-events-none data-[disabled=true]:opacity-50',
        className,
      )}
      {...props}
    />
  );
}

function CommandSeparator({ className, ...props }: React.ComponentPropsWithoutRef<typeof CommandPrimitive.Separator>) {
  return <CommandPrimitive.Separator className={cn('-mx-1 my-1 h-px bg-border', className)} {...props} />;
}

export { Command, CommandInput, CommandList, CommandEmpty, CommandGroup, CommandItem, CommandSeparator };
