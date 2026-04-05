import { useState } from 'react';
import { Menu, Sparkles } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { useDispatch } from 'react-redux';
import { resetToHome } from '@/store/worldSidebarSlice';
import { NotificationBell, NotificationCenter } from '@/components/NotificationCenter';
import { ThemeToggle } from '@/components/shared/ThemeToggle';
import { UserMenu } from '@/components/UserMenu';

interface TopToolbarProps {
  onOpenSettings: () => void;
}

export function TopToolbar({ onOpenSettings }: TopToolbarProps) {
  const dispatch = useDispatch();
  const [notificationCenterOpen, setNotificationCenterOpen] = useState(false);

  return (
    <>
      <header data-testid="top-toolbar" className="border-b border-border bg-card">
        <div className="flex h-14 items-center px-4 gap-2">
          <Button
            variant="ghost"
            size="icon"
            onClick={() => dispatch(resetToHome())}
            aria-label="Go to home"
          >
            <Menu className="h-5 w-5" />
          </Button>

          <Separator orientation="vertical" className="h-6" />

          <button
            type="button"
            className="flex items-center gap-2 rounded-md px-2 py-1 hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none transition-colors"
            onClick={() => dispatch(resetToHome())}
          >
            <Sparkles className="h-5 w-5 text-primary" aria-hidden="true" />
            <h1 className="text-lg font-semibold">Libris Maleficarum</h1>
          </button>

          <div className="ml-auto flex items-center gap-2">
            <ThemeToggle />
            <NotificationBell onClick={() => setNotificationCenterOpen(true)} />
            <UserMenu onOpenSettings={onOpenSettings} />
          </div>
        </div>
      </header>
      
      <NotificationCenter
        open={notificationCenterOpen}
        onOpenChange={setNotificationCenterOpen}
      />
    </>
  );
}
