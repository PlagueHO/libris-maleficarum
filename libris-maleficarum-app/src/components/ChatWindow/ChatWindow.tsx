import React, { useState } from 'react';
import styles from './ChatWindow.module.css';

const ChatWindow: React.FC = () => {
  const [visible, setVisible] = useState<boolean>(false);

  const handleToggle = () => setVisible((prev) => !prev);

  return (
    <aside
      className={`${styles.chat} ${visible ? styles.visible : styles.hidden}`}
      aria-label="Chat Window"
      aria-hidden={!visible}
      tabIndex={visible ? 0 : -1}
    >
      <button
        className={styles.toggleButton}
        onClick={handleToggle}
        aria-label={visible ? 'Hide Chat Window' : 'Show Chat Window'}
      >
        {visible ? '⮞' : '⮜'}
      </button>
      {visible && (
        <div className={styles.chatContent} role="region" aria-label="AI Assistant Chat">
          {/* TODO: Implement chat/AI assistant UI */}
          <div>Chat / AI Assistant</div>
        </div>
      )}
    </aside>
  );
};

export default ChatWindow;
