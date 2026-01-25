/**
 * EntityDetailReadOnlyView Component Contract
 *
 * Read-only display of a world entity with Edit button.
 * Extracted from MainPanel.tsx inline JSX for reusability and testability.
 *
 * Displays: entity name, type badge, tags, description, custom properties (if present).
 * Action: Edit button (top-right) triggers edit mode.
 *
 * @module components/MainPanel/EntityDetailReadOnlyView
 */

import type { WorldEntity } from '@/services/types/worldEntity.types';

/**
 * EntityDetailReadOnlyView component properties
 */
export interface EntityDetailReadOnlyViewProps {
  /** Entity to display in read-only mode */
  entity: WorldEntity;

  /** Callback invoked when Edit button is clicked */
  onEditClick: () => void;

  /** Disable Edit button (optional, defaults to false) */
  disableEdit?: boolean;
}

/**
 * Component structure and layout
 */
export interface EntityDetailReadOnlyViewStructure {
  /** Wrapper: Card component (from Shadcn/ui) */
  wrapper: {
    component: 'Card';
    className: 'max-w-4xl mx-auto';
  };

  /** Header section */
  header: {
    component: 'CardHeader';
    layout: 'flex items-start justify-between';

    /** Left side: Entity name + badges */
    left: {
      name: {
        component: 'CardTitle';
        className: 'text-3xl font-bold';
        role: 'heading';
        ariaLevel: 1;
      };
      badges: {
        component: 'Badge';
        variants: {
          entityType: 'outline';
          tags: 'secondary';
        };
      };
    };

    /** Right side: Edit button */
    right: {
      editButton: {
        component: 'Button';
        variant: 'outline';
        size: 'default';
        icon: 'Edit' /* from lucide-react */;
        label: 'Edit';
        position: 'top-right corner';
      };
    };
  };

  /** Content section */
  content: {
    component: 'CardContent';
    description: {
      displayed: 'if entity.description exists';
      className: 'prose dark:prose-invert max-w-none whitespace-pre-wrap';
      fallback: 'text-muted-foreground italic' /* "No description available." */;
    };
    customProperties: {
      displayed: 'if entity.properties exists';
      format: 'JSON.parse(entity.properties)';
      layout: 'key-value pairs or structured display';
    };
  };
}

/**
 * Styling and visual design
 */
export interface EntityDetailReadOnlyViewStyling {
  /** TailwindCSS classes */
  container: 'max-w-4xl mx-auto space-y-6';
  card: 'Card from Shadcn/ui';
  headerLayout: 'flex items-start justify-between';
  entityName: 'text-3xl font-bold';
  badgeGroup: 'flex items-center gap-2';
  editButton: 'Button variant=outline size=default';
  description: 'prose dark:prose-invert max-w-none whitespace-pre-wrap';
  emptyDescription: 'text-muted-foreground italic';

  /** Responsive behavior */
  breakpoints: {
    mobile: 'Stack edit button below name on small screens';
    desktop: 'Edit button aligned to top-right';
  };
}

/**
 * Edit button behavior
 */
export interface EntityDetailReadOnlyViewEditButton {
  /** Button properties */
  label: 'Edit';
  icon: 'Edit' /* Lucide React icon */;
  variant: 'outline';
  size: 'default';

  /** Position */
  position: 'top-right corner of CardHeader';
  alignment: 'self-start';

  /** Click handler */
  onClick: () => void; // Calls onEditClick prop

  /** Disabled state */
  disabled: boolean | undefined; // From disableEdit prop

  /** Accessibility */
  ariaLabel: `Edit ${entity.name}`;
  tabIndex: 0; // Keyboard focusable
  onKeyDown: '(e: KeyboardEvent) => Enter/Space triggers click';
}

/**
 * Custom properties display (extensible)
 */
export interface EntityDetailReadOnlyViewCustomProperties {
  /** When to display */
  condition: 'entity.properties !== null && entity.properties !== ""';

  /** Parsing */
  parse: 'JSON.parse(entity.properties)';

  /** Rendering strategies */
  strategies: {
    /** Simple key-value list */
    keyValueList: {
      layout: '<dl> with <dt> and <dd>';
      styling: 'grid grid-cols-2 gap-2';
    };

    /** Grouped by category */
    categorized: {
      layout: 'Sections with headings';
      example: 'Geographic Region: climate, terrain, population';
    };

    /** Future: Custom renderers per entity type */
    typeSpecific: {
      GeographicRegion: 'GeoPropertiesDisplay component';
      PoliticalRegion: 'PoliPropertiesDisplay component';
    };
  };

  /** Fallback */
  fallback: 'Show raw JSON in <pre> tag if display strategy fails';
}

/**
 * Accessibility requirements (WCAG 2.2 Level AA)
 */
export interface EntityDetailReadOnlyViewAccessibility {
  /** Semantic structure */
  headingHierarchy: 'Entity name is h1 (aria-level=1)';
  landmarkRoles: 'Implicitly article/section via Card';

  /** Edit button */
  editButton: {
    accessibleLabel: `Edit ${entity.name}` /* Dynamic based on entity */;
    keyboardAccessible: 'tabindex=0, Enter/Space activation';
    focusVisible: 'Focus ring via focus-visible pseudo-class';
    minTouchTarget: '44x44px minimum';
  };

  /** Content readability */
  textContrast: '4.5:1 minimum for body text';
  linkContrast: '4.5:1 minimum (if links in description)';
  badgeContrast: '3:1 minimum for UI components';

  /** Screen reader experience */
  entityType: 'Announced as "Entity type: {type}" via Badge';
  tags: 'Announced as list of tags';
  description: 'Preserved whitespace for readability';
}

/**
 * Integration points
 */
export interface EntityDetailReadOnlyViewIntegration {
  /** Parent component */
  parent: 'MainPanel.tsx';

  /** Trigger condition */
  displayWhen: 'mainPanelMode === "viewing_entity" && selectedEntityId !== null';

  /** Data source */
  entityData: 'useGetWorldEntityByIdQuery({ worldId, entityId })';

  /** Edit action handler */
  onEditClick: 'dispatch(openEntityFormEdit(entity.id))';

  /** Redux actions triggered */
  actions: {
    edit: 'openEntityFormEdit(entityId)' /* Transitions to editing_entity mode */;
  };
}

/**
 * Test scenarios (for EntityDetailReadOnlyView.test.tsx)
 */
export interface EntityDetailReadOnlyViewTestScenarios {
  /** Rendering tests */
  rendering: {
    'Renders entity name as h1': boolean;
    'Displays entity type badge': boolean;
    'Displays all tags as badges': boolean;
    'Renders description with preserved whitespace': boolean;
    'Shows "No description" message when description is null': boolean;
    'Displays Edit button in top-right corner': boolean;
    'Renders custom properties if present': boolean;
  };

  /** Interaction tests */
  interaction: {
    'Calls onEditClick when Edit button clicked': boolean;
    'Calls onEditClick on Enter key when button focused': boolean;
    'Calls onEditClick on Space key when button focused': boolean;
    'Disables Edit button when disableEdit=true': boolean;
  };

  /** Accessibility tests */
  accessibility: {
    'Has no accessibility violations (jest-axe)': boolean;
    'Entity name has heading role with level 1': boolean;
    'Edit button has accessible label': boolean;
    'Edit button is keyboard-focusable': boolean;
    'Edit button meets minimum touch target size (44x44px)': boolean;
    'Text content meets contrast ratio 4.5:1': boolean;
    'Badges meet contrast ratio 3:1': boolean;
  };

  /** Edge cases */
  edgeCases: {
    'Handles entity with no tags': boolean;
    'Handles entity with null description': boolean;
    'Handles entity with empty string description': boolean;
    'Handles entity with very long name (>100 chars)': boolean;
    'Handles entity with malformed custom properties JSON': boolean;
  };
}

/**
 * Usage example
 */
export const EntityDetailReadOnlyViewUsageExample = `
import { EntityDetailReadOnlyView } from '@/components/MainPanel/EntityDetailReadOnlyView';
import { useDispatch } from 'react-redux';
import { openEntityFormEdit } from '@/store/worldSidebarSlice';

export function MainPanel() {
  const dispatch = useDispatch();
  const entity = /* ... fetch entity data ... */;

  const handleEditClick = () => {
    dispatch(openEntityFormEdit(entity.id));
  };

  return (
    <main className="flex-1 p-6 overflow-auto">
      <EntityDetailReadOnlyView
        entity={entity}
        onEditClick={handleEditClick}
      />
    </main>
  );
}
`;
