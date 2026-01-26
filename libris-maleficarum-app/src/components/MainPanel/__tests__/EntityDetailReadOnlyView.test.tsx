/**
 * Unit tests for EntityDetailReadOnlyView component
 *
 * Tests read-only display of WorldEntity with DynamicPropertiesView integration
 *
 * @module __tests__/EntityDetailReadOnlyView.test
 */

import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { axe, toHaveNoViolations } from 'jest-axe';
import { EntityDetailReadOnlyView } from '../EntityDetailReadOnlyView';
import { WorldEntityType } from '@/services/types/worldEntity.types';
import type { WorldEntity } from '@/services/types/worldEntity.types';

expect.extend(toHaveNoViolations);

// Helper to create base entity with all required properties
const createEntity = (overrides: Partial<WorldEntity>): WorldEntity => ({
  id: 'entity-1',
  worldId: 'world-1',
  parentId: null,
  name: 'Test Entity',
  entityType: WorldEntityType.Continent,
  description: 'Test description',
  tags: [],
  properties: undefined,
  path: ['/Test Entity'],
  depth: 0,
  hasChildren: false,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
  schemaVersion: 1,
  ownerId: 'user-1',
  isDeleted: false,
  ...overrides,
});

describe('T037: EntityDetailReadOnlyView - Basic Rendering', () => {
  it('should render entity name, type, and Edit button', () => {
    const entity = createEntity({ name: 'Test Continent', tags: ['primary'] });
    const onEditClick = vi.fn();

    render(<EntityDetailReadOnlyView entity={entity} onEditClick={onEditClick} />);

    expect(screen.getByRole('heading', { level: 1, name: 'Test Continent' })).toBeInTheDocument();
    expect(screen.getByText('Continent')).toBeInTheDocument();
    expect(screen.getByText('primary')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /edit test continent/i })).toBeEnabled();
  });

  it('should call onEditClick when Edit button is clicked', async () => {
    const user = userEvent.setup();
    const onEditClick = vi.fn();
    const entity = createEntity({});

    render(<EntityDetailReadOnlyView entity={entity} onEditClick={onEditClick} />);

    await user.click(screen.getByRole('button', { name: /edit/i }));
    expect(onEditClick).toHaveBeenCalledOnce();
  });

  it('should show "No description available" when description is empty', () => {
    const entity = createEntity({ description: '' });
    const onEditClick = vi.fn();

    render(<EntityDetailReadOnlyView entity={entity} onEditClick={onEditClick} />);

    expect(screen.getByText('No description available.')).toBeInTheDocument();
  });
});

describe('T037: EntityDetailReadOnlyView - Custom Properties with DynamicPropertiesView', () => {
  it('should render GeographicRegion properties with numeric formatting', () => {
    const entity = createEntity({
      entityType: WorldEntityType.GeographicRegion,
      name: 'Tropical Rainforest',
      properties: JSON.stringify({
        Climate: 'Tropical monsoon',
        Terrain: 'Dense rainforest',
        Population: 500000,
        Area: 2500.75,
      }),
    });

    render(<EntityDetailReadOnlyView entity={entity} onEditClick={vi.fn()} />);

    expect(screen.getByRole('heading', { level: 2, name: /geographic region properties/i })).toBeInTheDocument();
    expect(screen.getByText('Climate')).toBeInTheDocument();
    expect(screen.getByText('Tropical monsoon')).toBeInTheDocument();
    // T033: Numeric formatting
    expect(screen.getByText('500,000')).toBeInTheDocument();
    expect(screen.getByText('2,500.75')).toBeInTheDocument();
  });

  it('should render PoliticalRegion with tagArray as badges', () => {
    const entity = createEntity({
      entityType: WorldEntityType.PoliticalRegion,
      name: 'Federation',
      properties: JSON.stringify({
        GovernmentType: 'Federal republic',
        MemberStates: ['State A', 'State B', 'State C'],
        EstablishedDate: '1776-07-04',
      }),
    });

    render(<EntityDetailReadOnlyView entity={entity} onEditClick={vi.fn()} />);

    // T034: TagArray as badges
    expect(screen.getByText('State A')).toBeInTheDocument();
    expect(screen.getByText('State B')).toBeInTheDocument();
    expect(screen.getByText('State C')).toBeInTheDocument();
  });

  it('should render CulturalRegion with multiple tagArrays', () => {
    const entity = createEntity({
      entityType: WorldEntityType.CulturalRegion,
      name: 'Cultural Heartland',
      properties: JSON.stringify({
        Languages: ['English', 'Spanish'],
        Religions: ['Christianity', 'Islam'],
        CulturalTraits: 'Diverse traditions',
      }),
    });

    render(<EntityDetailReadOnlyView entity={entity} onEditClick={vi.fn()} />);

    expect(screen.getByText('Languages')).toBeInTheDocument();
    expect(screen.getByText('English')).toBeInTheDocument();
    expect(screen.getByText('Spanish')).toBeInTheDocument();
    expect(screen.getByText('Religions')).toBeInTheDocument();
    expect(screen.getByText('Christianity')).toBeInTheDocument();
  });

  it('should not render properties section when entity has no properties', () => {
    const entity = createEntity({
      entityType: WorldEntityType.Character,
      name: 'Gandalf',
      properties: undefined,
    });

    render(<EntityDetailReadOnlyView entity={entity} onEditClick={vi.fn()} />);

    expect(screen.queryByRole('heading', { level: 2, name: /properties/i })).toBeNull();
  });

  it('should handle malformed JSON gracefully', () => {
    const entity = createEntity({
      entityType: WorldEntityType.GeographicRegion,
      properties: '{ invalid }',
    });

    render(<EntityDetailReadOnlyView entity={entity} onEditClick={vi.fn()} />);

    expect(screen.queryByRole('heading', { level: 2, name: /properties/i })).toBeNull();
  });
});

describe('EntityDetailReadOnlyView - Accessibility', () => {
  it('should have no accessibility violations', async () => {
    const entity = createEntity({
      entityType: WorldEntityType.GeographicRegion,
      properties: JSON.stringify({
        Climate: 'Temperate',
        Population: 1000000,
      }),
    });

    const { container } = render(<EntityDetailReadOnlyView entity={entity} onEditClick={vi.fn()} />);

    const results = await axe(container);
    expect(results).toHaveNoViolations();
  });

  it('should have proper heading hierarchy', () => {
    const entity = createEntity({
      entityType: WorldEntityType.GeographicRegion,
      name: 'Test Region',
      properties: JSON.stringify({ Climate: 'Temperate' }),
    });

    render(<EntityDetailReadOnlyView entity={entity} onEditClick={vi.fn()} />);

    expect(screen.getByRole('heading', { level: 1, name: 'Test Region' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 2, name: 'Description' })).toBeInTheDocument();
    expect(screen.getByRole('heading', { level: 2, name: /geographic region properties/i })).toBeInTheDocument();
  });
});
