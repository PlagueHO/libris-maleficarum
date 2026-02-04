import { useEffect } from 'react';
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

function App() {
  const dispatch = useAppDispatch();
  const selectedWorldId = useAppSelector(selectSelectedWorldId);
  
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
  
  // Poll for async operations (active operations only)
  // This provides real-time status updates in the notification center
  // Only poll when a world is selected
  useGetDeleteOperationsQuery(
    { 
      worldId: selectedWorldId!,
      status: ['pending', 'in_progress']
    },
    {
      skip: !selectedWorldId, // Skip query when no world selected
      pollingInterval: 3000, // Poll every 3 seconds
      skipPollingIfUnfocused: true, // Pause polling when browser tab is inactive
    }
  );
  
  return (
    <div className="h-screen flex flex-col bg-background text-foreground">
      <TopToolbar />
      {/* Wrap world-scoped content with WorldProvider when a world is selected */}
      {selectedWorldId ? (
        <WorldProvider initialWorldId={selectedWorldId} initialWorldName="">
          <div className="flex-1 flex overflow-hidden">
            <WorldSidebar />
            <MainPanel />
            <ChatPanel />
          </div>
          <DeleteConfirmationModal />
        </WorldProvider>
      ) : (
        <>
          <div className="flex-1 flex overflow-hidden">
            <WorldSidebar />
            <MainPanel />
            <ChatPanel />
          </div>
          <DeleteConfirmationModal />
        </>
      )}
    </div>
  )
}

export default App
