import * as React from 'react';
import { ThemeProvider } from '@mui/material';
import customTheme from './theme';

type AppProviderProps = {
  children: React.ReactNode;
};

export const AppProvider = ({ children }: AppProviderProps) => {
  const theme = customTheme()

  return (
    <ThemeProvider theme={theme}>
      {children}
    </ThemeProvider>
  );
};