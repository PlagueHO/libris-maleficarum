import React from 'react';
import { Provider } from 'react-redux';
import { store } from './store/store';
import TopToolbar from './components/TopToolbar/TopToolbar';
import SidePanel from './components/SidePanel/SidePanel';
import MainPanel from './components/MainPanel/MainPanel';
import ChatWindow from './components/ChatWindow/ChatWindow';
import styles from './App.module.css';

const App: React.FC = () => (
  <Provider store={store}>
    <div className={styles.appRoot} role="application" aria-label="Libris Maleficarum App">
      <TopToolbar />
      <div className={styles.layoutContainer}>
        <SidePanel />
        <MainPanel />
        <ChatWindow />
      </div>
    </div>
  </Provider>
);

export default App;
