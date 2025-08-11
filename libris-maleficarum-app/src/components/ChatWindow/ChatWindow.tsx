import React, { useCallback, useMemo, useState } from 'react';
import {
  Button,
  Card,
  CardHeader,
  Divider,
  Spinner,
  Text,
  Textarea,
  makeStyles,
  shorthands,
  tokens,
} from '@fluentui/react-components';
import { ChevronLeft24Regular, ChevronRight24Regular, Send24Regular } from '@fluentui/react-icons';
import { Chat, ChatMyMessage } from '@fluentui-contrib/react-chat';

type ChatMessage = {
  id: string;
  text: string;
};

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    ...shorthands.borderLeft('1px', 'solid', tokens.colorNeutralStroke1),
    width: '300px',
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
  card: {
    display: 'flex',
    flexDirection: 'column',
    height: '100%',
    ...shorthands.padding('8px'),
  },
  list: {
    display: 'flex',
    flexDirection: 'column',
    gap: '8px',
    overflowY: 'auto',
    flexGrow: 1,
    ...shorthands.padding('8px'),
  },
  inputRow: {
    display: 'flex',
    alignItems: 'flex-end',
    gap: '8px',
    ...shorthands.padding('8px'),
  },
  textarea: {
    flexGrow: 1,
  },
});

const ChatWindow: React.FC = () => {
  const styles = useStyles();
  const [visible, setVisible] = useState<boolean>(false);
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [input, setInput] = useState('');
  const [sending, setSending] = useState(false);

  const canSend = useMemo(() => input.trim().length > 0 && !sending, [input, sending]);

  const handleToggle = () => setVisible((prev) => !prev);

  const sendMessage = useCallback(async () => {
    if (!canSend) return;
    setSending(true);
    // Simulate async send
    const text = input.trim();
    setInput('');
    setMessages((prev) => [...prev, { id: String(Date.now()), text }]);
    // Simulated latency
    await new Promise((r) => setTimeout(r, 200));
    setSending(false);
  }, [canSend, input]);

  const onKeyDown: React.KeyboardEventHandler<HTMLTextAreaElement> = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      void sendMessage();
    }
  };

  return (
    <aside aria-label="Chat Window" aria-hidden={!visible} tabIndex={visible ? 0 : -1}>
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

      {/* Expanded chat */}
      {visible && (
        <div className={styles.root} role="region" aria-label="AI Assistant Chat">
          <Card className={styles.card}>
            <CardHeader header={<Text weight="semibold">AI Assistant</Text>} />
            <Divider />

            <div className={styles.list} aria-live="polite">
              {messages.length === 0 ? (
                <Text size={300} aria-label="Empty Chat">No messages yet</Text>
              ) : (
                <Chat>
                  {messages.map((m) => (
                    <ChatMyMessage key={m.id}>{m.text}</ChatMyMessage>
                  ))}
                </Chat>
              )}
              {sending && (
                <div>
                  <Spinner label="Sending" />
                </div>
              )}
            </div>

            <div className={styles.inputRow}>
              <Textarea
                className={styles.textarea}
                resize="vertical"
                aria-label="Message input"
                placeholder="Type a message"
                value={input}
                onChange={(e) => setInput((e.target as HTMLTextAreaElement).value)}
                onKeyDown={onKeyDown}
              />
              <Button
                aria-label="Send message"
                appearance="primary"
                icon={<Send24Regular />}
                onClick={sendMessage}
                disabled={!canSend}
              />
              <Button
                aria-label="Hide Chat Window"
                appearance="subtle"
                icon={<ChevronRight24Regular />}
                onClick={handleToggle}
              />
            </div>
          </Card>
        </div>
      )}
    </aside>
  );
};

export default ChatWindow;
