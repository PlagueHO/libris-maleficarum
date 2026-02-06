/**
 * NotificationBell Component
 * 
 * Bell icon button with unread badge for opening the notification center.
 * Displays count of unread async operations.
 * 
 * Features:
 * - Bell icon (Lucide Bell)
 * - Unread badge (shows count, max 99+)
 * - Keyboard accessible
 * - ARIA labels for screen readers
 * 
 * @module NotificationCenter/NotificationBell
 */

import { Bell } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { useAppSelector } from '@/store/store';
import { selectUnreadCount } from '@/store/notificationSelectors';

export interface NotificationBellProps {
  /**
   * Callback when bell is clicked
   */
  onClick: () => void;
}

/**
 * NotificationBell component
 * 
 * @example
 * ```tsx
 * <NotificationBell onClick={() => setNotificationCenterOpen(true)} />
 * ```
 */
export function NotificationBell({ onClick }: NotificationBellProps) {
  const unreadCount = useAppSelector(selectUnreadCount);
  
  // Format count for display (max 99+)
  const displayCount = unreadCount > 99 ? '99+' : unreadCount.toString();
  
  // Build aria-label with unread count
  const ariaLabel = unreadCount > 0
    ? `Tome dispatches (${unreadCount} unread)`
    : 'Tome dispatches';
  
  return (
    <Button
      variant="ghost"
      size="icon"
      onClick={onClick}
      aria-label={ariaLabel}
      className="relative"
      data-testid="notification-bell"
    >
      <Bell className="h-5 w-5" aria-hidden="true" />
      
      {unreadCount > 0 && (
        <Badge
          variant="destructive"
          className="absolute -top-1 -right-1 h-5 min-w-5 px-1 flex items-center justify-center text-[10px] font-bold"
          aria-hidden="true"
          data-testid="notification-badge"
        >
          {displayCount}
        </Badge>
      )}
    </Button>
  );
}
