import { ThemeToggle } from '@/components/shared/ThemeToggle';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from '@/components/ui/sheet';

interface SettingsPanelProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

/**
 * Application settings panel.
 * Slides in from the right as an overlay panel.
 * Provides user-configurable preferences such as theme switching.
 */
export function SettingsPanel({ open, onOpenChange }: SettingsPanelProps) {
  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetContent side="right" aria-label="Settings panel">
        <SheetHeader>
          <SheetTitle>Settings</SheetTitle>
          <SheetDescription>
            Configure your application preferences
          </SheetDescription>
        </SheetHeader>

        <div className="flex-1 overflow-y-auto px-4 pb-4">
          <section aria-labelledby="appearance-heading" className="space-y-4">
            <h3 id="appearance-heading" className="text-sm font-medium">
              Appearance
            </h3>

            <div className="flex items-center justify-between rounded-lg border p-4">
              <div>
                <p className="text-sm font-medium">Theme</p>
                <p className="text-xs text-muted-foreground">
                  Toggle between light and dark mode
                </p>
              </div>
              <ThemeToggle />
            </div>
          </section>
        </div>
      </SheetContent>
    </Sheet>
  );
}
