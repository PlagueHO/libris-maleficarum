import { ThemeToggle } from '@/components/shared/ThemeToggle';

/**
 * Application settings page.
 * Provides user-configurable preferences such as theme switching.
 */
export function SettingsPage() {
  return (
    <main className="mx-auto max-w-2xl p-6">
      <h1 className="text-2xl font-semibold mb-6">Settings</h1>

      <section aria-labelledby="appearance-heading" className="space-y-4">
        <h2 id="appearance-heading" className="text-lg font-medium">
          Appearance
        </h2>

        <div className="flex items-center justify-between rounded-lg border p-4">
          <div>
            <p className="font-medium">Theme</p>
            <p className="text-sm text-muted-foreground">
              Toggle between light and dark mode
            </p>
          </div>
          <ThemeToggle />
        </div>
      </section>
    </main>
  );
}
