import { AlertCircle, AlertTriangle, Info } from 'lucide-react';
import { cn } from '@/lib/utils';

export type MainPanelTransientAlertVariant = 'error' | 'warning' | 'info';

export interface MainPanelTransientAlertProps {
  title: string;
  message: string;
  variant?: MainPanelTransientAlertVariant;
  className?: string;
}

const ALERT_STYLES: Record<MainPanelTransientAlertVariant, { container: string; icon: string; Icon: typeof AlertCircle }> = {
  error: {
    container: 'border-destructive/40 bg-destructive/10 text-foreground',
    icon: 'text-destructive',
    Icon: AlertCircle,
  },
  warning: {
    container: 'border-amber-500/40 bg-amber-500/10 text-foreground',
    icon: 'text-amber-700 dark:text-amber-300',
    Icon: AlertTriangle,
  },
  info: {
    container: 'border-blue-500/40 bg-blue-500/10 text-foreground',
    icon: 'text-blue-700 dark:text-blue-300',
    Icon: Info,
  },
};

/**
 * Floating alert header for transient issues in the main panel.
 * Keep this component generic so it can be reused by create/edit/read flows.
 */
export function MainPanelTransientAlert({
  title,
  message,
  variant = 'error',
  className,
}: MainPanelTransientAlertProps) {
  const { container, icon, Icon } = ALERT_STYLES[variant];

  return (
    <div className={cn('sticky top-0 z-30 mb-6', className)}>
      <div
        role="alert"
        aria-live="assertive"
        className={cn(
          'rounded-lg border p-4 shadow-lg backdrop-blur-sm bg-background/95',
          container
        )}
      >
        <div className="flex items-start gap-3">
          <Icon className={cn('h-5 w-5 shrink-0 mt-0.5', icon)} aria-hidden="true" />
          <div className="space-y-1">
            <h2 className="text-sm font-semibold">{title}</h2>
            <p className="text-sm text-muted-foreground">{message}</p>
          </div>
        </div>
      </div>
    </div>
  );
}
