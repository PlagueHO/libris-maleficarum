import React from 'react';
import { FluentProvider, webLightTheme, webDarkTheme } from '@fluentui/react-components';

export type ThemeMode = 'light' | 'dark';

interface ThemeProviderProps {
  mode?: ThemeMode;
  children: React.ReactNode;
}

// Central theme wrapper using FluentProvider and web themes
export const ThemeProvider: React.FC<ThemeProviderProps> = ({ mode = 'light', children }) => {
  const theme = mode === 'dark' ? webDarkTheme : webLightTheme;
  // full-height root to allow layouts to stretch as before
  return <FluentProvider theme={theme} style={{ height: '100%' }}>{children}</FluentProvider>;
};

export default ThemeProvider;
