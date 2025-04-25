import React from 'react';
import styles from './TopToolbar.module.css';

const TopToolbar: React.FC = () => (
  <header className={styles.toolbar} role="banner" aria-label="Top Toolbar">
    <nav className={styles.nav} aria-label="Global Navigation">
      {/* TODO: Add fantasy-themed logo/icon */}
      <span className={styles.logo}>Libris Maleficarum</span>
      {/* TODO: Add navigation links, search, notifications, user profile */}
      <ul className={styles.navList}>
        <li><button className={styles.navButton} aria-label="Search">🔍</button></li>
        <li><button className={styles.navButton} aria-label="Notifications">🔔</button></li>
        <li><button className={styles.navButton} aria-label="Profile">👤</button></li>
      </ul>
    </nav>
  </header>
);

export default TopToolbar;
