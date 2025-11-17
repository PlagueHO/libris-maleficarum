import { CopilotChat } from '@copilotkit/react-ui';
import '@copilotkit/react-ui/styles.css';
import styles from './WorldBuilderChat.module.css';

/**
 * WorldBuilderChat component provides an AI-powered chat interface
 * for world-building and campaign management using CopilotKit.
 * 
 * This is a proof-of-concept component that will connect to the
 * Microsoft Agent Framework backend via AG-UI protocol once implemented.
 */
export function WorldBuilderChat() {
  return (
    <div className={styles.chatContainer}>
      <CopilotChat
        labels={{
          title: 'World Builder Assistant',
          initial: 'Welcome to Libris Maleficarum! I can help you build worlds, create characters, and manage your campaigns. What would you like to create today?',
        }}
        className={styles.chat}
      />
    </div>
  );
}
