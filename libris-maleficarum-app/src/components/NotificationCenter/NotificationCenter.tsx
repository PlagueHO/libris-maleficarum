/**
 * NotificationCenter Component
 * 
 * Drawer panel that displays all async operations with real-time status updates.
 * Opens from right side, overlaying the chat panel area.
 * 
 * Features:
 * - List of all active and recent async operations
 * - Mark all as read action
 * - Clear all completed action
 * - Empty state message
 * - Scrollable list
 * - Keyboard accessible (ESC to close)
 * - ARIA live region for status announcements
 * 
 * @module NotificationCenter/NotificationCenter
 */

import { useEffect, useState } from 'react';
import { Drawer, DrawerContent, DrawerHeader, DrawerTitle, DrawerDescription } from '@/components/ui/drawer';
import { Button } from '@/components/ui/button';
import { ScrollArea } from '@/components/ui/scroll-area';
import { useAppSelector, useAppDispatch } from '@/store/store';
import { selectVisibleOperations } from '@/store/notificationSelectors';
import { clearAllCompleted, markAsRead, selectNotificationMetadata } from '@/store/notificationsSlice';
import { NotificationItem } from './NotificationItem';

export interface NotificationCenterProps {
  /**
   * Whether the notification center is open
   */
  open: boolean;

  /**
   * Callback when open state changes (close via ESC or click outside)
   */
  onOpenChange: (open: boolean) => void;
}

/**
 * NotificationCenter component
 * 
 * @example
 * ```tsx
 * const [open, setOpen] = useState(false);
 * <NotificationCenter open={open} onOpenChange={setOpen} />
 * ```
 */
export function NotificationCenter({ open, onOpenChange }: NotificationCenterProps) {
  const operations = useAppSelector(selectVisibleOperations);
  const metadata = useAppSelector(selectNotificationMetadata);
  const dispatch = useAppDispatch();
  const [announceMessage, setAnnounceMessage] = useState<string>('');
  
  // Count completed operations for "Clear Completed" button
  const completedCount = operations.filter(op => op.status === 'completed').length;
  
  // Check if all visible operations are already read
  const allRead = operations.length === 0 || operations.every(op => metadata[op.id]?.read);
  
  // Announce status changes for screen readers
  useEffect(() => {
    if (!open || operations.length === 0) return;
    
    // Find most recent status change (completed or failed operations)
    const recentlyCompleted = operations.find(
      op => (op.status === 'completed' || op.status === 'failed') && !metadata[op.id]?.read
    );
    
    if (recentlyCompleted) {
      const message = recentlyCompleted.status === 'completed'
        ? `Operation completed: ${recentlyCompleted.targetEntityName}`
        : `Operation failed: ${recentlyCompleted.targetEntityName}`;
      
      setAnnounceMessage(message);
      
      // Clear announcement after screen reader has time to announce
      setTimeout(() => setAnnounceMessage(''), 3000);
    }
  }, [operations, metadata, open]);
  
  // Mark all visible operations as read
  const handleMarkAllRead = () => {
    operations.forEach(op => {
      dispatch(markAsRead(op.id));
    });
  };
  
  // Clear all completed operations
  const handleClearCompleted = () => {
    const completedIds = operations
      .filter(op => op.status === 'completed')
      .map(op => op.id);
    
    dispatch(clearAllCompleted(completedIds));
  };
  
  return (
    <Drawer open={open} onOpenChange={onOpenChange} direction="right">
      <DrawerContent className="h-screen w-full sm:w-96 fixed top-0 right-0 left-auto rounded-none transition-transform duration-300 ease-in-out data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:slide-out-to-right data-[state=open]:slide-in-from-right">
        {/* ARIA live region for status announcements */}
        <div
          role="status"
          aria-live="polite"
          aria-atomic="true"
          className="sr-only"
        >
          {announceMessage}
        </div>
        
        <DrawerHeader className="border-b">
          <DrawerTitle>Notifications</DrawerTitle>
          <DrawerDescription>
            Track your async operations
          </DrawerDescription>
        </DrawerHeader>
        
        {/* Action Buttons */}
        <div className="flex gap-2 px-4 py-3 border-b">
          <Button
            variant="outline"
            size="sm"
            onClick={handleMarkAllRead}
            disabled={allRead}
          >
            Mark All Read
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={handleClearCompleted}
            disabled={completedCount === 0}
          >
            Clear Completed ({completedCount})
          </Button>
        </div>
        
        {/* Operations List */}
        <ScrollArea className="flex-1 px-4">
          {operations.length === 0 ? (
            <div className="flex items-center justify-center h-32">
              <p className="text-center text-muted-foreground py-8">
                No notifications
              </p>
            </div>
          ) : (
            <div className="space-y-2 py-4">
              {operations.map(operation => (
                <NotificationItem
                  key={operation.id}
                  operation={operation}
                />
              ))}
            </div>
          )}
        </ScrollArea>
      </DrawerContent>
    </Drawer>
  );
}
