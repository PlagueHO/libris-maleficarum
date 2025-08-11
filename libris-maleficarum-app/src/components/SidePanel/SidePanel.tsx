import React from 'react';
import { useSelector, useDispatch } from 'react-redux';
import { RootState, toggle } from '../../store/store';
import {
  Drawer,
  DrawerHeader,
  DrawerHeaderTitle,
  DrawerBody,
  Button,
  Tooltip,
  TabList,
  Tab,
  tokens,
} from '@fluentui/react-components';
import {
  PanelLeftExpand24Regular,
  PanelLeftContract24Regular,
  Globe24Regular,
  BookContacts24Regular,
  ShapeIntersect24Regular,
} from '@fluentui/react-icons';

const SidePanel: React.FC = () => {
  const isExpanded = useSelector((state: RootState) => state.sidePanel.isExpanded);
  const dispatch = useDispatch();

  const handleToggle = () => dispatch(toggle());

  // Inline placement Drawer behaves like a collapsible left rail
  return (
    <div aria-label="Side Panel" style={{ display: 'flex' }}>
      <Drawer
        type="inline"
        open={isExpanded}
        position="start"
        style={{ borderRight: `1px solid ${tokens.colorNeutralStroke1}` }}
      >
        <DrawerHeader>
          <DrawerHeaderTitle
            action={
              <Tooltip content="Collapse" relationship="label">
                <Button
                  aria-label="Collapse side panel"
                  appearance="subtle"
                  icon={<PanelLeftContract24Regular />}
                  onClick={handleToggle}
                />
              </Tooltip>
            }
          >
            Navigation
          </DrawerHeaderTitle>
        </DrawerHeader>
        <DrawerBody>
          <nav aria-label="Side Navigation">
            <TabList vertical defaultSelectedValue="worlds" appearance="subtle">
              <Tab id="tab-worlds" icon={<Globe24Regular />} value="worlds">Worlds</Tab>
              <Tab id="tab-campaigns" icon={<BookContacts24Regular />} value="campaigns">Campaigns</Tab>
              <Tab id="tab-entities" icon={<ShapeIntersect24Regular />} value="entities">Entities</Tab>
            </TabList>
          </nav>
        </DrawerBody>
      </Drawer>

      {/* Collapsed rail with expand button when not expanded */}
      {!isExpanded && (
        <div
          role="complementary"
          aria-label="Collapsed Side Panel"
          style={{
            width: 48,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            borderRight: `1px solid ${tokens.colorNeutralStroke1}`,
          }}
        >
          <Tooltip content="Expand" relationship="label">
            <Button
              aria-label="Expand side panel"
              appearance="subtle"
              icon={<PanelLeftExpand24Regular />}
              onClick={handleToggle}
            />
          </Tooltip>
        </div>
      )}
    </div>
  );
};

export default SidePanel;
