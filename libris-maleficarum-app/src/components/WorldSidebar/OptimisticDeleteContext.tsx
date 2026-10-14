import type { ReactNode } from 'react';
import { optimisticDeleteContext } from './optimisticDeleteContext';
import type { OptimisticDeleteContextValue } from './optimisticDeleteContext';

interface OptimisticDeleteProviderProps {
  children: ReactNode;
  value: OptimisticDeleteContextValue;
}

export function OptimisticDeleteProvider({
  children,
  value,
}: OptimisticDeleteProviderProps) {
  return (
    <optimisticDeleteContext.Provider value={value}>
      {children}
    </optimisticDeleteContext.Provider>
  );
}
