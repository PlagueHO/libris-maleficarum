import { CircleUser, Settings, LogOut, LogIn } from 'lucide-react';
import { useMsal, useIsAuthenticated } from '@azure/msal-react';
import { Button } from '@/components/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { isAuthConfigured, loginRequest } from '@/auth/authConfig';

/** Extracts up to two initials from a display name. */
function getInitials(name: string | undefined): string {
  if (!name) return '?';
  const parts = name.trim().split(/\s+/);
  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }
  return parts[0][0]?.toUpperCase() ?? '?';
}

/**
 * User menu for multi-user mode with MSAL authentication.
 * Uses MSAL hooks (requires MsalProvider ancestor).
 */
function AuthenticatedUserMenu({ onOpenSettings }: { onOpenSettings: () => void }) {
  const { instance, accounts } = useMsal();
  const isAuthenticated = useIsAuthenticated();

  const account = accounts[0];
  const displayName = account?.name ?? account?.username ?? 'User';
  const email = account?.username ?? '';
  const initials = getInitials(displayName);

  const handleSignIn = async () => {
    try {
      await instance.loginPopup(loginRequest);
    } catch (error) {
      console.error('Sign-in failed:', error);
    }
  };

  const handleSignOut = async () => {
    try {
      await instance.logoutPopup();
    } catch (error) {
      console.error('Sign-out failed:', error);
    }
  };

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        {isAuthenticated ? (
          <Button variant="ghost" size="sm" className="gap-2" title={displayName}>
            <span className="flex size-6 items-center justify-center rounded-full bg-primary text-[10px] font-semibold text-primary-foreground" aria-hidden="true">
              {initials}
            </span>
            <span className="max-w-30 truncate text-sm">{displayName}</span>
          </Button>
        ) : (
          <Button variant="ghost" size="sm">
            <LogIn className="size-4" aria-hidden="true" />
            Sign in
          </Button>
        )}
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        {isAuthenticated ? (
          <>
            <DropdownMenuLabel className="font-normal">
              <div className="flex flex-col space-y-1">
                <p className="text-sm font-medium leading-none">{displayName}</p>
                {email && (
                  <p className="text-xs leading-none text-muted-foreground">{email}</p>
                )}
              </div>
            </DropdownMenuLabel>
            <DropdownMenuSeparator />
            <DropdownMenuGroup>
              <DropdownMenuItem onSelect={onOpenSettings}>
                <Settings className="size-4" aria-hidden="true" />
                Settings
              </DropdownMenuItem>
            </DropdownMenuGroup>
            <DropdownMenuSeparator />
            <DropdownMenuItem onSelect={() => void handleSignOut()}>
              <LogOut className="size-4" aria-hidden="true" />
              Sign out
            </DropdownMenuItem>
          </>
        ) : (
          <>
            <DropdownMenuItem onSelect={() => void handleSignIn()}>
              <LogIn className="size-4" aria-hidden="true" />
              Sign in with Entra ID
            </DropdownMenuItem>
            <DropdownMenuSeparator />
            <DropdownMenuGroup>
              <DropdownMenuItem onSelect={onOpenSettings}>
                <Settings className="size-4" aria-hidden="true" />
                Settings
              </DropdownMenuItem>
            </DropdownMenuGroup>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/**
 * User menu for anonymous single-user mode (no MSAL dependency).
 */
function AnonymousUserMenu({ onOpenSettings }: { onOpenSettings: () => void }) {
  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className="gap-2 opacity-60"
          title="Entra ID SSO is not enabled. Running in anonymous single-user mode."
        >
          <CircleUser className="size-5" aria-hidden="true" />
          <span className="text-sm">Anonymous</span>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuLabel className="font-normal">
          <div className="flex flex-col space-y-1">
            <p className="text-sm font-medium leading-none">Anonymous Mode</p>
            <p className="text-xs leading-none text-muted-foreground">
              Entra ID SSO is not configured.
            </p>
          </div>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuGroup>
          <DropdownMenuItem onSelect={onOpenSettings}>
            <Settings className="size-4" aria-hidden="true" />
            Settings
          </DropdownMenuItem>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuItem disabled>
          <LogIn className="size-4" aria-hidden="true" />
          Sign in
          <span className="ml-auto text-xs text-muted-foreground">Disabled</span>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}

/**
 * User menu dropdown component.
 * Renders the appropriate variant based on auth configuration.
 */
interface UserMenuProps {
  onOpenSettings: () => void;
}

export function UserMenu({ onOpenSettings }: UserMenuProps) {
  if (isAuthConfigured) {
    return <AuthenticatedUserMenu onOpenSettings={onOpenSettings} />;
  }
  return <AnonymousUserMenu onOpenSettings={onOpenSettings} />;
}
