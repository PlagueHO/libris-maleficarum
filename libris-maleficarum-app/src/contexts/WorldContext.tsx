/**
 * WorldContext provides the currently selected world to all components
 * within the world-scoped part of the application.
 *
 * This is application scope/routing context, NOT server state.
 * Server state (world data) lives in RTK Query cache.
 *
 * @example
 * ```tsx
 * function MyComponent() {
 *   const { worldId, worldName } = useWorld();
 *   // Use worldId in API calls
 * }
 * ```
 */

import { createContext, useContext, useState, useCallback, useMemo, type ReactNode } from 'react';

export interface WorldContextValue {
  /**
   * Currently selected world ID
   */
  worldId: string;

  /**
   * Currently selected world name (for display purposes)
   */
  worldName: string;

  /**
   * Update the current world
   */
  setWorld: (worldId: string, worldName: string) => void;
}

const WorldContext = createContext<WorldContextValue | null>(null);

export interface WorldProviderProps {
  children: ReactNode;
  /**
   * Initial world ID (optional - can set later via setWorld)
   */
  initialWorldId?: string;
  /**
   * Initial world name (optional)
   */
  initialWorldName?: string;
}

/**
 * WorldProvider wraps the world-scoped part of the application.
 * Typically wraps everything after world selection.
 */
export function WorldProvider({
  children,
  initialWorldId = '',
  initialWorldName = ''
}: WorldProviderProps) {
  const [worldId, setWorldId] = useState<string>(initialWorldId);
  const [worldName, setWorldName] = useState<string>(initialWorldName);

  const setWorld = useCallback((id: string, name: string) => {
    setWorldId(id);
    setWorldName(name);
  }, []);

  const value = useMemo(() => ({
    worldId,
    worldName,
    setWorld
  }), [worldId, worldName, setWorld]);

  return (
    <WorldContext.Provider value={value}>
      {children}
    </WorldContext.Provider>
  );
}

/**
 * Hook to access current world context.
 *
 * @throws {Error} If used outside of WorldProvider
 * @throws {Error} If worldId is empty (no world selected)
 *
 * @example
 * ```tsx
 * function DeleteButton({ entityId }: Props) {
 *   const { worldId } = useWorld();
 *   const [deleteEntity] = useInitiateEntityDeleteMutation();
 *
 *   const handleDelete = () => {
 *     deleteEntity({ worldId, entityId });
 *   };
 * }
 * ```
 */
// eslint-disable-next-line react-refresh/only-export-components
export function useWorld(): WorldContextValue {
  const context = useContext(WorldContext);

  if (!context) {
    throw new Error(
      'useWorld must be used within a WorldProvider. ' +
      'Wrap your component tree with <WorldProvider>.'
    );
  }

  if (!context.worldId) {
    throw new Error(
      'No world selected. Ensure a world is selected before accessing this component. ' +
      'Call setWorld() from WorldProvider to set the current world.'
    );
  }

  return context;
}

/**
 * Hook to access world context without throwing if worldId is empty.
 * Useful for components that work in both "no world selected" and "world selected" states.
 *
 * @example
 * ```tsx
 * function WorldSelector() {
 *   const context = useWorldOptional();
 *   return <div>Current: {context?.worldName || 'None'}</div>;
 * }
 * ```
 */
// eslint-disable-next-line react-refresh/only-export-components
export function useWorldOptional(): WorldContextValue | null {
  return useContext(WorldContext);
}
