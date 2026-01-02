import React from 'react';
import { Card, CardHeader, Divider, makeStyles, shorthands } from '@fluentui/react-components';

const useStyles = makeStyles({
  root: {
    display: 'flex',
    flexDirection: 'column',
    flexGrow: 1,
    ...shorthands.padding('24px'),
    overflowY: 'auto',
    backgroundColor: 'var(--colorNeutralBackground1)',
  },
  contentSection: {
    display: 'flex',
    flexDirection: 'column',
    width: '100%',
    maxWidth: '1200px',
    marginLeft: 'auto',
    marginRight: 'auto',
  },
  card: {
    width: '100%',
    marginBottom: '16px',
  },
  heading: {
    margin: 0,
    fontSize: '32px',
    fontWeight: 600,
  },
});

const MainPanel: React.FC = () => {
  const styles = useStyles();

  return (
    <main className={styles.root} role="main" aria-label="Main Work Panel" tabIndex={0}>
      <section className={styles.contentSection}>
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
