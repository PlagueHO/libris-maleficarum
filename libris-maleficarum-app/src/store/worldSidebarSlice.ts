/**
 * World Sidebar Redux Slice
 *
 * Manages UI state for the World Sidebar Navigation feature:
 * - Selected world ID
 * - Selected entity ID
 * - Expanded tree nodes
 * - Modal/form visibility states
 *
 * @module store/worldSidebarSlice
 */

import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type { RootState } from './store';

/**
 * Main panel display modes
 */
export type MainPanelMode = 'empty' | 'viewing_entity' | 'editing_world' | 'creating_world';

/**
 * World Sidebar state shape
 */
export interface WorldSidebarState {
  /** Currently selected world ID (null if no world selected) */
  selectedWorldId: string | null;

  /** Currently selected entity ID (null if no entity selected) */
  selectedEntityId: string | null;

  /** Array of expanded entity IDs for tree navigation */
  expandedNodeIds: string[];

  /** Main panel display mode */
  mainPanelMode: MainPanelMode;

  /** World form modal visibility */
  isWorldFormOpen: boolean;

  /** World ID being edited (null for create mode, non-null for edit mode) */
  editingWorldId: string | null;

  /** Entity form modal visibility */
  isEntityFormOpen: boolean;

  /** Entity ID being edited (null for create mode, non-null for edit mode) */
  editingEntityId: string | null;

  /** Parent entity ID for new entity creation (null for root entities) */
  newEntityParentId: string | null;
}

/**
 * Initial state for World Sidebar
 */
const initialState: WorldSidebarState = {
  selectedWorldId: null,
  selectedEntityId: null,
  expandedNodeIds: [],
  mainPanelMode: 'empty',
  isWorldFormOpen: false,
  editingWorldId: null,
  isEntityFormOpen: false,
  editingEntityId: null,
  newEntityParentId: null,
};

/**
 * World Sidebar slice
 */
export const worldSidebarSlice = createSlice({
  name: 'worldSidebar',
  initialState,
  reducers: {
    /**
     * Set the currently selected world
     *
     * @param state - Current state
     * @param action - Payload with world ID (null to clear selection)
     */
    setSelectedWorld: (state, action: PayloadAction<string | null>) => {
      console.log('REDUCER: setSelectedWorld', action.payload);
      state.selectedWorldId = action.payload;
      // Clear entity selection when world changes
      state.selectedEntityId = null;
      // Clear expanded nodes when world changes (new world, new hierarchy)
      state.expandedNodeIds = [];
    },

    /**
     * Set the currently selected entity
     *
     * @param state - Current state
     * @param action - Payload with entity ID (null to clear selection)
     */
    setSelectedEntity: (state, action: PayloadAction<string | null>) => {
      state.selectedEntityId = action.payload;
      if (action.payload) {
        state.mainPanelMode = 'viewing_entity';
      } else {
        state.mainPanelMode = 'empty';
      }
    },

    /**
     * Toggle a tree node's expanded state
     *
     * @param state - Current state
     * @param action - Payload with entity ID to toggle
     */
    toggleNodeExpanded: (state, action: PayloadAction<string>) => {
      const nodeId = action.payload;
      const index = state.expandedNodeIds.indexOf(nodeId);

      if (index > -1) {
        state.expandedNodeIds.splice(index, 1);
      } else {
        state.expandedNodeIds.push(nodeId);
      }
    },

    /**
     * Set multiple nodes as expanded
     *
     * @param state - Current state
     * @param action - Payload with array of entity IDs to expand
     */
    setExpandedNodes: (state, action: PayloadAction<string[]>) => {
      state.expandedNodeIds = action.payload;
    },

    /**
     * Expand a single node (without toggling)
     *
     * @param state - Current state
     * @param action - Payload with entity ID to expand
     */
    expandNode: (state, action: PayloadAction<string>) => {
      if (!state.expandedNodeIds.includes(action.payload)) {
        state.expandedNodeIds.push(action.payload);
      }
    },

    /**
     * Collapse a single node (without toggling)
     *
     * @param state - Current state
     * @param action - Payload with entity ID to collapse
     */
    collapseNode: (state, action: PayloadAction<string>) => {
      const index = state.expandedNodeIds.indexOf(action.payload);
      if (index > -1) {
        state.expandedNodeIds.splice(index, 1);
      }
    },

    /**
     * Collapse all nodes (reset expanded state)
     *
     * @param state - Current state
     */
    collapseAllNodes: (state) => {
      state.expandedNodeIds = [];
    },

    /**
     * Open the world form in main panel in create mode
     *
     * @param state - Current state
     */
    openWorldFormCreate: (state) => {
      state.mainPanelMode = 'creating_world';
      state.editingWorldId = null;
    },

    /**
     * Open the world form modal in edit mode
     *
     * @param state - Current state
     * @param action - Payload with world ID to edit
     */
    openWorldFormEdit: (state, action: PayloadAction<string>) => {
      state.isWorldFormOpen = true;
      state.editingWorldId = action.payload;
      state.mainPanelMode = 'editing_world';
    },

    /**
     * Close the world form
     *
     * @param state - Current state
     */
    closeWorldForm: (state) => {
      state.editingWorldId = null;
      state.mainPanelMode = 'empty';
    },

    /**
     * Open the entity form modal in create mode
     *
     * @param state - Current state
     * @param action - Payload with parent entity ID (null for root entities)
     */
    openEntityFormCreate: (
      state,
      action: PayloadAction<string | null>,
    ) => {
      state.isEntityFormOpen = true;
      state.editingEntityId = null;
      state.newEntityParentId = action.payload;
    },

    /**
     * Open the entity form modal in edit mode
     *
     * @param state - Current state
     * @param action - Payload with entity ID to edit
     */
    openEntityFormEdit: (state, action: PayloadAction<string>) => {
      state.isEntityFormOpen = true;
      state.editingEntityId = action.payload;
      state.newEntityParentId = null;
    },

    /**
     * Close the entity form modal
     *
     * @param state - Current state
     */
    closeEntityForm: (state) => {
      state.isEntityFormOpen = false;
      state.editingEntityId = null;
      state.newEntityParentId = null;
    },
  },
});

/**
 * Action creators exported for use in components
 */
export const {
  setSelectedWorld,
  setSelectedEntity,
  toggleNodeExpanded,
  setExpandedNodes,
  expandNode,
  collapseNode,
  collapseAllNodes,
  openWorldFormCreate,
  openWorldFormEdit,
  closeWorldForm,
  openEntityFormCreate,
  openEntityFormEdit,
  closeEntityForm,
} = worldSidebarSlice.actions;

/**
 * Selectors for accessing World Sidebar state
 *
 * Usage in components:
 * const selectedWorldId = useSelector(selectSelectedWorldId);
 */
export const selectSelectedWorldId = (state: RootState): string | null =>
  state.worldSidebar.selectedWorldId;

export const selectSelectedEntityId = (state: RootState): string | null =>
  state.worldSidebar.selectedEntityId;

export const selectMainPanelMode = (state: RootState): MainPanelMode =>
  state.worldSidebar.mainPanelMode;

export const selectExpandedNodeIds = (state: RootState): string[] =>
  state.worldSidebar.expandedNodeIds;

export const selectIsNodeExpanded = (nodeId: string) => (state: RootState): boolean =>
  state.worldSidebar.expandedNodeIds.includes(nodeId);

export const selectIsWorldFormOpen = (state: RootState): boolean =>
  state.worldSidebar.isWorldFormOpen;

export const selectEditingWorldId = (state: RootState): string | null =>
  state.worldSidebar.editingWorldId;

export const selectIsEntityFormOpen = (state: RootState): boolean =>
  state.worldSidebar.isEntityFormOpen;

export const selectEditingEntityId = (state: RootState): string | null =>
  state.worldSidebar.editingEntityId;

export const selectNewEntityParentId = (state: RootState): string | null =>
  state.worldSidebar.newEntityParentId;

/**
 * Reducer export for store configuration
 */
export default worldSidebarSlice.reducer;
