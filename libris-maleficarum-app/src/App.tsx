import React, { useState } from 'react';
import { Provider } from 'react-redux';
import { store } from './store/store';
import TopToolbar from './components/TopToolbar/TopToolbar';
import SidePanel from './components/SidePanel/SidePanel';
import MainPanel from './components/MainPanel/MainPanel';
import ChatWindow from './components/ChatWindow/ChatWindow';
import styles from './App.module.css';
import { Switch } from '@fluentui/react-components';

const App: React.FC = () => {
  // temporary theme toggle (state-only) per migration plan
  const [darkMode, setDarkMode] = useState(false);

  return (
    <Provider store={store}>
      <div className={styles.appRoot} role="application" aria-label="Libris Maleficarum App">
        <TopToolbar />
        <div style={{ padding: '8px' }}>
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
    </Provider>
  );
};

export default App;
