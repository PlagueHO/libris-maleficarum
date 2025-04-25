import React, { useState } from 'react';
import styles from './SidePanel.module.css';

const SidePanel: React.FC = () => {
  const [isExpanded, setIsExpanded] = useState<boolean>(true);

  const handleToggle = () => setIsExpanded((prev) => !prev);

  return (
    <aside
      className={`${styles.panel} ${isExpanded ? styles.expanded : styles.shrunk}`}
      aria-label="Side Panel"
      aria-expanded={isExpanded}
    >
      <button
        className={styles.toggleButton}
        onClick={handleToggle}
        aria-label={isExpanded ? 'Shrink Side Panel' : 'Expand Side Panel'}
      >
        {isExpanded ? '⮜' : '⮞'}
      </button>
      <nav className={styles.toolsNav} aria-label="Side Navigation">
        <button className={styles.toolButton}>
          <span className={styles.icon}>🌍</span>
          {isExpanded && <span className={styles.label}>Worlds</span>}
        </button>
        <button className={styles.toolButton}>
          <span className={styles.icon}>📜</span>
          {isExpanded && <span className={styles.label}>Campaigns</span>}
        </button>
        <button className={styles.toolButton}>
          <span className={styles.icon}>🏰</span>
          {isExpanded && <span className={styles.label}>Entities</span>}
        </button>
      </nav>
    </aside>
  );
};

export default SidePanel;
