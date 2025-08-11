import React from 'react';
import {
  Toolbar,
  ToolbarButton,
  ToolbarDivider,
  Menu,
  MenuTrigger,
  MenuPopover,
  MenuList,
  MenuItem,
  Text,
} from '@fluentui/react-components';
import { Search24Regular, Alert24Regular, Person24Regular } from '@fluentui/react-icons';

const TopToolbar: React.FC = () => (
  <header role="banner" aria-label="Top Toolbar" style={{ borderBottom: '1px solid var(--colorNeutralStroke1)', padding: '0 8px' }}>
    <Toolbar aria-label="Global Navigation" style={{ justifyContent: 'space-between', width: '100%' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: 12 }}>
        {/* TODO: Add fantasy-themed logo/icon */}
        <Text size={600} weight="semibold">Libris Maleficarum</Text>
      </div>

      <div style={{ display: 'flex', alignItems: 'center', gap: 4 }}>
        <ToolbarButton aria-label="Search" icon={<Search24Regular />} />
        <ToolbarButton aria-label="Notifications" icon={<Alert24Regular />} />
        <ToolbarDivider />
        <Menu>
          <MenuTrigger disableButtonEnhancement>
            <ToolbarButton aria-label="Account" icon={<Person24Regular />} />
          </MenuTrigger>
          <MenuPopover>
            <MenuList>
              <MenuItem>Profile</MenuItem>
              <MenuItem>Settings</MenuItem>
              <MenuItem>Sign out</MenuItem>
            </MenuList>
          </MenuPopover>
        </Menu>
      </div>
    </Toolbar>
  </header>
);

export default TopToolbar;
