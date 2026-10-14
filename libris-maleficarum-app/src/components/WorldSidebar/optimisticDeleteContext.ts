import { createContext } from 'react';

export interface OptimisticDeleteContextValue {
  onOptimisticDelete: (entityId: string, childIds?: string[]) => void;
  onRollbackDelete: (entityId: string, childIds?: string[]) => void;
}

export const optimisticDeleteContext =
  createContext<OptimisticDeleteContextValue | null>(null);
