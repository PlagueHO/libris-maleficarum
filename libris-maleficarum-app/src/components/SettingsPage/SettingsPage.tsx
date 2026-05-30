import { useCallback, useEffect, useState } from 'react';
import { CheckCircle2, Loader2, Monitor, Moon, RefreshCw, Sun, XCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { getAccessStatus } from '@/services/configService';
import { useTheme, type ThemePreference } from '@/hooks/useTheme';

interface SettingsPageProps {
  onClose: () => void;
}

interface BackendStatus {
  state: 'loading' | 'connected' | 'disconnected';
  message?: string;
}

function isThemePreference(value: string): value is ThemePreference {
  return value === 'light' || value === 'dark' || value === 'system';
}

/**
 * Settings page rendered in the main content region.
 * Mirrors the settings layout from Prompt Babbler while using Libris styling conventions.
 */
export function SettingsPage({ onClose }: SettingsPageProps) {
  const { theme, setTheme } = useTheme();
  const [backendStatus, setBackendStatus] = useState<BackendStatus>({ state: 'loading' });

  const refreshBackendStatus = useCallback(async () => {
    setBackendStatus({ state: 'loading' });

    try {
      await getAccessStatus();
      setBackendStatus({ state: 'connected' });
    } catch (error) {
      const message = error instanceof Error ? error.message : 'Unable to reach backend service.';
      setBackendStatus({ state: 'disconnected', message });
    }
  }, []);

  useEffect(() => {
    void refreshBackendStatus();
  }, [refreshBackendStatus]);

  const handleThemeChange = (value: string) => {
    if (isThemePreference(value)) {
      setTheme(value);
    }
  };

  return (
    <main className="flex-1 overflow-auto p-6" id="settings-main-content">
      <div className="mx-auto max-w-4xl space-y-6">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h1 className="text-2xl font-bold">Settings</h1>
            <p className="text-sm text-muted-foreground">
              Appearance preferences and backend status.
            </p>
          </div>
          <Button type="button" variant="outline" size="sm" onClick={onClose}>
            Return to World
          </Button>
        </div>

        <Separator />

        <section aria-labelledby="appearance-heading" className="space-y-4">
          <h2 id="appearance-heading" className="text-lg font-semibold">
            Appearance
          </h2>
          <div className="max-w-sm space-y-2">
            <label htmlFor="theme-select" className="text-sm font-medium">
              Theme
            </label>
            <Select value={theme} onValueChange={handleThemeChange}>
              <SelectTrigger id="theme-select" aria-label="Theme">
                <SelectValue placeholder="Choose a theme" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="light">
                  <Sun className="size-4" aria-hidden="true" />
                  <span>Light</span>
                </SelectItem>
                <SelectItem value="dark">
                  <Moon className="size-4" aria-hidden="true" />
                  <span>Dark</span>
                </SelectItem>
                <SelectItem value="system">
                  <Monitor className="size-4" aria-hidden="true" />
                  <span>System</span>
                </SelectItem>
              </SelectContent>
            </Select>
            <p className="text-sm text-muted-foreground">
              Choose the color mode for the application.
            </p>
          </div>
        </section>

        <Card>
          <CardHeader>
            <CardTitle>Backend Status</CardTitle>
            <CardDescription>
              Monitor your connection to backend services.
            </CardDescription>
          </CardHeader>
          <CardContent>
            <div className="flex items-start justify-between gap-4">
              <div className="flex-1 space-y-2" aria-live="polite">
                {backendStatus.state === 'loading' ? (
                  <p className="flex items-center gap-2 text-sm text-muted-foreground" role="status">
                    <Loader2 className="size-4 animate-spin" aria-hidden="true" />
                    <span>Checking status...</span>
                  </p>
                ) : null}

                {backendStatus.state === 'connected' ? (
                  <p className="flex items-center gap-2 text-sm text-status-success" role="status">
                    <CheckCircle2 className="size-4" aria-hidden="true" />
                    <span>Connected</span>
                  </p>
                ) : null}

                {backendStatus.state === 'disconnected' ? (
                  <>
                    <p className="flex items-center gap-2 text-sm text-destructive" role="status">
                      <XCircle className="size-4" aria-hidden="true" />
                      <span>Disconnected</span>
                    </p>
                    {backendStatus.message ? (
                      <p className="ml-6 text-xs text-muted-foreground">{backendStatus.message}</p>
                    ) : null}
                  </>
                ) : null}
              </div>

              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => void refreshBackendStatus()}
                disabled={backendStatus.state === 'loading'}
                aria-label="Refresh backend status"
              >
                <RefreshCw className="size-4" aria-hidden="true" />
                <span>Refresh</span>
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    </main>
  );
}
