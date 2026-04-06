import type { ReactNode } from 'react';
import { useIsAuthenticated, useMsal } from '@azure/msal-react';
import { isAuthConfigured, loginRequest } from '@/auth/authConfig';
import { LogIn } from 'lucide-react';
import { Button } from '@/components/ui/button';

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

  // Multi-user mode: gate behind MSAL authentication
  return <AuthenticatedGuard>{children}</AuthenticatedGuard>;
}

/**
 * Inner component that uses MSAL hooks (requires MsalProvider ancestor).
 */
function AuthenticatedGuard({ children }: { children: ReactNode }) {
  const isAuthenticated = useIsAuthenticated();
  const { instance } = useMsal();

  if (isAuthenticated) {
    return <>{children}</>;
  }

  const handleSignIn = async () => {
    try {
      await instance.loginPopup(loginRequest);
    } catch (error) {
      console.error('Sign-in failed:', error);
    }
  };

  return (
    <div
      role="alert"
      className="flex flex-col items-center justify-center gap-4 p-8 text-center"
    >
      <LogIn className="h-10 w-10 text-muted-foreground" aria-hidden="true" />
      <h2 className="text-lg font-semibold">Sign in to continue</h2>
      <p className="text-sm text-muted-foreground">
        You need to sign in with your Entra ID account to access this content.
      </p>
      <Button onClick={() => void handleSignIn()}>
        <LogIn className="mr-2 h-4 w-4" aria-hidden="true" />
        Sign in
      </Button>
    </div>
  );
}
