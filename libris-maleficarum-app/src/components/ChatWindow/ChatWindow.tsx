import React, { useState } from 'react';
import {
  Button,
  makeStyles,
  shorthands,
  tokens,
} from '@fluentui/react-components';
import { ChevronLeft24Regular, ChevronRight24Regular } from '@fluentui/react-icons';
import { WorldBuilderChat } from '../WorldBuilderChat/WorldBuilderChat';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.borderLeft('1px', 'solid', tokens.colorNeutralStroke1),
    width: '400px',
    height: '100%',
  },
  hidden: {
    display: 'none',
  },
  toggleRail: {
    width: '48px',
    display: 'flex',
    alignItems: 'center',
    justifyContent: 'center',
    ...shorthands.borderLeft('1px', 'solid', tokens.colorNeutralStroke1),
  },
  chatContainer: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    position: 'relative',
  },
  toggleButton: {
    position: 'absolute',
    top: '8px',
    right: '8px',
    zIndex: 1000,
  },
});

/**
 * ChatWindow component provides a collapsible chat interface
 * powered by CopilotKit and AG-UI protocol.
 * 
 * This is a proof-of-concept that demonstrates:
 * - CopilotKit integration with Fluent UI v9
 * - AG-UI protocol readiness (via mock endpoint)
 * - Shared state between Redux and agents
 * - Frontend actions callable by agents
 * 
 * Once Microsoft Agent Framework backend is implemented,
 * this will connect to /api/copilotkit endpoint.
 */
const ChatWindow: React.FC = () => {
  const styles = useStyles();
  const [visible, setVisible] = useState<boolean>(false);

  const handleToggle = () => setVisible((prev) => !prev);

  return (
    <aside aria-label="Chat Window" aria-hidden={visible ? 'false' : 'true'} tabIndex={visible ? 0 : -1}>
      {/* Collapsed rail */}
      {!visible && (
        <div className={styles.toggleRail} role="complementary" aria-label="Collapsed Chat Rail">
          <Button
            aria-label="Show Chat Window"
            appearance="subtle"
            icon={<ChevronLeft24Regular />}
            onClick={handleToggle}
          />
        </div>
      )}

      {/* Expanded chat with CopilotKit */}
      {visible && (
        <div className={styles.root} role="region" aria-label="World Builder AI Assistant">
          <div className={styles.chatContainer}>
            <Button
              aria-label="Hide Chat Window"
              appearance="subtle"
              icon={<ChevronRight24Regular />}
              onClick={handleToggle}
              className={styles.toggleButton}
            />
            <WorldBuilderChat />
          </div>
        </div>
      )}
    </aside>
  );
};

export default ChatWindow;
