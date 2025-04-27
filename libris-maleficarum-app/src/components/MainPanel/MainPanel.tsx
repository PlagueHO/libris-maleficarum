import React from 'react';
import styles from './MainPanel.module.css';

const MainPanel: React.FC = () => (
  <main className={styles.main} role="main" aria-label="Main Work Panel" tabIndex={0}>
    {/* TODO: Contextual workspace for world-building, campaign management, etc. */}
    <section className={styles.contentSection}>
      <h1 className={styles.heading}>Welcome to Libris Maleficarum</h1>
      {/* TODO: Render dynamic content based on user task/context */}
    </section>
  </main>
);

export default MainPanel;
