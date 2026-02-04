/**
 * Optimistic Delete Context
 * 
 * Provides callback for optimistic UI updates during async delete operations.
 * Used to instantly remove entities from the UI while backend deletion proceeds.
 * 
 * @module WorldSidebar/OptimisticDeleteContext
 */

import { createContext, useContext } from 'react';

export interface OptimisticDeleteContextValue {
  /**
   * Optimistically remove an entity from the UI
   * 
   * @param entityId - ID of entity being deleted
   * @param childIds - Optional array of child entity IDs (for cascading deletes)
   */
  onOptimisticDelete: (entityId: string, childIds?: string[]) => void;
}

const OptimisticDeleteContext = createContext<OptimisticDeleteContextValue | null>(null);

export const OptimisticDeleteProvider = OptimisticDeleteContext.Provider;

/**
 * Hook to access optimistic delete callback
 * 
 * @returns Optimistic delete context value
 * @throws Error if used outside OptimisticDeleteProvider
 */
export function useOptimisticDelete(): OptimisticDeleteContextValue {
  const context = useContext(OptimisticDeleteContext);
  
  if (!context) {
    throw new Error('useOptimisticDelete must be used within OptimisticDeleteProvider');
  }
  
  return context;
}
