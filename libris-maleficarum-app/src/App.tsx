import React, { useState } from 'react';
import { Provider } from 'react-redux';
// import { CopilotKit } from '@copilotkit/react-core';
import { store } from './store/store';
import TopToolbar from './components/TopToolbar/TopToolbar';
import SidePanel from './components/SidePanel/SidePanel';
import MainPanel from './components/MainPanel/MainPanel';
import ChatWindow from './components/ChatWindow/ChatWindow';
import styles from './App.module.css';
import { Switch } from '@fluentui/react-components';
// import { useCopilotWorldContext } from './hooks/useCopilotWorldContext';

/**
 * AppContent component provides CopilotKit context to the application.
 * Separated to allow hooks to access Redux state after Provider mount.
 */
const AppContent: React.FC = () => {
  // temporary theme toggle (state-only) per migration plan
  const [darkMode, setDarkMode] = useState(false);
  
  // Expose world-building context to CopilotKit agents
  // TODO: Re-enable when backend is ready
  // useCopilotWorldContext();

  return (
    <div className={styles.appRoot} role="application" aria-label="Libris Maleficarum App">
      <TopToolbar />
      <div className={styles.themeToggle}>
        <Switch
          checked={darkMode}
          onChange={(_ev: unknown, data: { checked?: boolean }) => setDarkMode(!!data?.checked)}
          label={darkMode ? 'Dark mode' : 'Light mode'}
        />
      </div>
      <div className={styles.layoutContainer}>
        <SidePanel />
        <MainPanel />
        <ChatWindow />
      </div>
    </div>
  );
};

const App: React.FC = () => {
  return (
    <Provider store={store}>
      {/* TODO: Re-enable CopilotKit when Microsoft Agent Framework backend is ready */}
      {/* <CopilotKit runtimeUrl="/api/copilotkit" agent="world-builder"> */}
      <AppContent />
      {/* </CopilotKit> */}
    </Provider>
  );
};

export default App;
