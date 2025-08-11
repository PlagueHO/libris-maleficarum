import React from 'react';
import { Card, CardHeader, Divider, makeStyles, shorthands } from '@fluentui/react-components';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    flexGrow: 1,
    ...shorthands.padding('16px'),
    overflowY: 'auto',
  },
  centerSection: {
    display: 'flex',
    flexDirection: 'column',
    alignItems: 'center',
    justifyContent: 'center',
    flexGrow: 1,
  },
  card: {
    width: '100%',
    maxWidth: '960px',
  },
  heading: {
    textAlign: 'center',
    margin: 0,
  },
});

const MainPanel: React.FC = () => {
  const styles = useStyles();

  return (
    <main className={styles.root} role="main" aria-label="Main Work Panel" tabIndex={0}>
      {/* TODO: Contextual workspace for world-building, campaign management, etc. */}
      <section className={styles.centerSection}>
        <Card className={styles.card}>
          <CardHeader header={<h1 className={styles.heading}>Welcome to Libris Maleficarum</h1>} />
          <Divider />
          {/* TODO: Render dynamic content based on user task/context */}
        </Card>
      </section>
    </main>
  );
};

export default MainPanel;
