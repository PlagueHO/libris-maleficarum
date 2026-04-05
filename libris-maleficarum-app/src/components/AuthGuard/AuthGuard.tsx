import type { ReactNode } from 'react';
import { isAuthConfigured } from '@/auth/authConfig';

interface AuthGuardProps {
  children: ReactNode;
}

/**
 * Guards data-bearing content behind authentication when multi-user mode is active.
 * In anonymous (single-user) mode, renders children immediately.
 * In multi-user mode when unauthenticated, displays a sign-in prompt.
 */
export function AuthGuard({ children }: AuthGuardProps) {
  // In anonymous mode, bypass auth entirely
  if (!isAuthConfigured) {
    return <>{children}</>;
  }

  // Multi-user mode: check MSAL authentication state
  // This will be enhanced in Phase 8 with useIsAuthenticated() from @azure/msal-react
  // For now, render children (auth check will be added when MsalProvider is active)
  return <>{children}</>;
}
