import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { EntityContextMenu } from './EntityContextMenu';
import { renderWithProviders } from '@/__tests__/utils/testUtils';
import {
  openEntityFormCreate,
  openEntityFormEdit,
} from '@/store/worldSidebarSlice';
import type { WorldEntity } from '@/services/types/worldEntity.types';
import { WorldEntityType } from '@/services/types/worldEntity.types';

// Mock the WorldSidebar actions
vi.mock('@/store/worldSidebarSlice', async () => {
  const actual = await vi.importActual('@/store/worldSidebarSlice');
  return {
    ...actual,
    openEntityFormCreate: vi.fn().mockReturnValue({ type: 'mock/create' }),
    openEntityFormEdit: vi.fn().mockReturnValue({ type: 'mock/edit' }),
  };
});

describe('EntityContextMenu', () => {
  const mockEntity: WorldEntity = {
    id: 'entity-1',
    name: 'Test Entity',
    description: 'Description',
    entityType: WorldEntityType.Region,
    worldId: 'world-1',
    parentId: 'parent-1',
    hasChildren: false,
    tags: [],
    path: ['root', 'entity-1'],
    depth: 1,
    ownerId: 'u1',
    createdAt: '2023-01-01',
    updatedAt: '2023-01-01',
    isDeleted: false,
    schemaVersion: 1,
  };

  const TestWrapper = () => (
    <EntityContextMenu entity={mockEntity}>
      <div data-testid="trigger">Right Click Me</div>
    </EntityContextMenu>
  );

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('renders children correctly', () => {
    renderWithProviders(<TestWrapper />);
    expect(screen.getByTestId('trigger')).toBeInTheDocument();
  });

  it('opens context menu on right click', async () => {
    renderWithProviders(<TestWrapper />);
    const trigger = screen.getByTestId('trigger');

    await userEvent.pointer({ keys: '[MouseRight]', target: trigger });

    // Check for menu items
    await waitFor(() => {
      expect(screen.getByText('New Codex Entry')).toBeInTheDocument();
      expect(screen.getByText('Edit Codex Entry')).toBeInTheDocument();
      expect(screen.getByText('Delete Codex Entry')).toBeInTheDocument();
    });
  });

  it('dispatches openEntityFormCreate when "Add Child Entity" is clicked', async () => {
    renderWithProviders(<TestWrapper />);
    const trigger = screen.getByTestId('trigger');

    await userEvent.pointer({ keys: '[MouseRight]', target: trigger });

    await waitFor(() => {
      screen.getByText('New Codex Entry').click();
    });

    expect(openEntityFormCreate).toHaveBeenCalledWith(mockEntity.id);
  });

  it('dispatches openEntityFormEdit when "Edit Entity" is clicked', async () => {
    renderWithProviders(<TestWrapper />);
    const trigger = screen.getByTestId('trigger');

    await userEvent.pointer({ keys: '[MouseRight]', target: trigger });

    await waitFor(() => {
      screen.getByText('Edit Codex Entry').click();
    });

    expect(openEntityFormEdit).toHaveBeenCalledWith(mockEntity.id);
  });
});
