import { useContext } from 'react';
import {
  optimisticDeleteContext,
  type OptimisticDeleteContextValue,
} from './optimisticDeleteContext';

export function useOptimisticDelete(): OptimisticDeleteContextValue {
  const context = useContext(optimisticDeleteContext);

  if (!context) {
    throw new Error('useOptimisticDelete must be used within OptimisticDeleteProvider');
  }

  return context;
}
