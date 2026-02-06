import { useEffect, useState, useCallback } from 'react';
import { TopToolbar } from './components/TopToolbar/TopToolbar'
import { WorldSidebar } from './components/WorldSidebar/WorldSidebar'
import { MainPanel } from './components/MainPanel/MainPanel'
import { ChatPanel } from './components/ChatPanel/ChatPanel'
import { DeleteConfirmationModal } from './components/MainPanel/DeleteConfirmationModal'
import { useGetDeleteOperationsQuery } from './services/asyncOperationsApi'
import { useAppDispatch, useAppSelector } from './store/store'
import { setPollingEnabled, performCleanup } from './store/notificationsSlice'
import { selectSelectedWorldId } from './store/worldSidebarSlice'
import { WorldProvider } from './contexts'
import { OptimisticDeleteProvider } from './components/WorldSidebar/OptimisticDeleteContext';
import { selectHasPendingOperations } from './store/notificationSelectors';
import { Toaster } from './components/ui/sonner';
import { logger } from './lib/logger';

function App() {
  const dispatch = useAppDispatch();
  const selectedWorldId = useAppSelector(selectSelectedWorldId);
  
  // Track optimistically deleted entity IDs
  const [optimisticallyDeletedIds, setOptimisticallyDeletedIds] = useState<Set<string>>(new Set());
  
  // Track when we need to poll after a delete (for smart polling)
  const [shouldPollAfterDelete, setShouldPollAfterDelete] = useState(false);

  /**
   * Optimistically remove entity from UI before backend delete completes
   * Called by DeleteConfirmationModal when user confirms delete
   */
  const handleOptimisticDelete = useCallback((entityId: string, childIds?: string[]) => {
    setOptimisticallyDeletedIds(prev => {
      const next = new Set(prev);
      next.add(entityId);
      
      // Add child entity IDs for cascading deletes
      if (childIds) {
        childIds.forEach(id => next.add(id));
      }
      
      return next;
    });
    
    // Enable polling after delete for 30 seconds
    setShouldPollAfterDelete(true);
  }, []);
  
  /**
   * Rollback optimistic delete (restore entity to UI)
   * Called when backend delete fails
   */
  const handleRollbackDelete = useCallback((entityId: string, childIds?: string[]) => {
    setOptimisticallyDeletedIds(prev => {
      const next = new Set(prev);
      next.delete(entityId);
      
      // Remove child entity IDs
      if (childIds) {
        childIds.forEach(id => next.delete(id));
      }
      
      return next;
    });
  }, []);
  
  // Enable polling when app mounts
  useEffect(() => {
    dispatch(setPollingEnabled(true));
    
    return () => {
      dispatch(setPollingEnabled(false));
    };
  }, [dispatch]);
  
  // T033: 24-hour cleanup interval for old notifications
  useEffect(() => {
    const CLEANUP_INTERVAL = 60 * 60 * 1000; // 1 hour in milliseconds
    const MAX_AGE = 24 * 60 * 60 * 1000; // 24 hours in milliseconds
    
    // Run cleanup immediately on mount
    const cutoffTimestamp = Date.now() - MAX_AGE;
    dispatch(performCleanup(cutoffTimestamp));
    
    // Set up interval to run cleanup every hour
    const interval = setInterval(() => {
      const cutoff = Date.now() - MAX_AGE;
      dispatch(performCleanup(cutoff));
    }, CLEANUP_INTERVAL);
    
    return () => clearInterval(interval);
  }, [dispatch]);
  
  // Poll for async operations
  // Smart polling: Only poll when there are active operations or we recently initiated a delete
  // This reduces unnecessary API calls when there's nothing to track
  const hasPendingOperations = useAppSelector(selectHasPendingOperations);
  const shouldPoll = hasPendingOperations || shouldPollAfterDelete;
  
  // Clear shouldPollAfterDelete flag after 30 seconds to stop polling
  useEffect(() => {
    if (!shouldPollAfterDelete) return;
    
    const timeout = setTimeout(() => {
      setShouldPollAfterDelete(false);
    }, 30000);
    
    return () => clearTimeout(timeout);
  }, [shouldPollAfterDelete]);
  
  // Log when polling starts/stops (debug visibility)
  useEffect(() => {
    if (shouldPoll && selectedWorldId) {
      logger.debug('STATE', 'Delete operations polling started', {
        hasPendingOperations,
        recentDelete: shouldPollAfterDelete,
      });
    } else if (selectedWorldId) {
      logger.debug('STATE', 'Delete operations polling stopped - no active operations');
    }
  }, [shouldPoll, selectedWorldId, hasPendingOperations, shouldPollAfterDelete]);
  
  useGetDeleteOperationsQuery(
    { 
      worldId: selectedWorldId!,
      // Don't filter by status - fetch all operations including completed
      // The notification panel handles filtering/display logic
    },
    {
      // Always perform the initial fetch when a world is selected,
      // even if we are not currently polling for changes.
      skip: !selectedWorldId,
      pollingInterval: shouldPoll ? 3000 : 0, // Poll every 3 seconds when enabled; no polling otherwise
      skipPollingIfUnfocused: true, // Pause polling when browser tab is inactive
    }
  );
  
  return (
    <OptimisticDeleteProvider value={{ onOptimisticDelete: handleOptimisticDelete, onRollbackDelete: handleRollbackDelete }}>
      <Toaster position="bottom-right" />
      <div className="h-screen flex flex-col bg-background text-foreground">
        <TopToolbar />
        {/* WorldProvider wraps entire app tree to prevent unmount/remount on world selection changes */}
        <WorldProvider initialWorldId={selectedWorldId || ''} initialWorldName="">
          <div className="flex-1 flex overflow-hidden">
            <WorldSidebar optimisticallyDeletedIds={optimisticallyDeletedIds} />
            <MainPanel />
            <ChatPanel />
          </div>
          <DeleteConfirmationModal />
        </WorldProvider>
      </div>
    </OptimisticDeleteProvider>
  )
}

export default App
