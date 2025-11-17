import { useCopilotReadable, useCopilotAction } from '@copilotkit/react-core';
import { useSelector } from 'react-redux';
import type { RootState } from '../store/store';

/**
 * Custom hook that exposes world-building context to CopilotKit agents.
 * This demonstrates how to share Redux state with the AI agent using AG-UI protocol.
 * 
 * This is a proof-of-concept that will be enhanced once the Microsoft Agent Framework
 * backend and WorldEntity hierarchy are implemented.
 */
export function useCopilotWorldContext() {
  // Get side panel state from Redux
  const isSidePanelExpanded = useSelector((state: RootState) => state.sidePanel.isExpanded);

  // Make current UI state readable by the agent
  useCopilotReadable({
    description: 'Current UI state including side panel visibility',
    value: {
      sidePanelOpen: isSidePanelExpanded,
      currentView: 'world-builder',
      activeFeatures: ['world-management', 'character-creation', 'campaign-planning'],
    },
  });

  // Example: Expose a placeholder world entity structure
  // This will be replaced with actual Cosmos DB entities once backend is implemented
  useCopilotReadable({
    description: 'Current world entity hierarchy (placeholder data)',
    value: {
      worlds: [
        {
          id: 'demo-world-001',
          name: 'Demo World',
          entityType: 'World',
          description: 'A demonstration world for proof-of-concept',
          children: [
            {
              id: 'demo-continent-001',
              name: 'Eldoria',
              entityType: 'Continent',
              description: 'The primary continent',
            },
          ],
        },
      ],
      note: 'This is placeholder data. Real WorldEntity hierarchy will come from Azure Cosmos DB.',
    },
  });

  // Define a frontend action that the agent can call
  // This demonstrates Human-in-the-Loop pattern
  useCopilotAction({
    name: 'createWorldEntity',
    description: 'Create a new world entity (world, continent, character, etc.). This is a demonstration action.',
    parameters: [
      {
        name: 'entityType',
        type: 'string',
        description: 'Type of entity to create (World, Continent, Country, Character, etc.)',
        required: true,
      },
      {
        name: 'name',
        type: 'string',
        description: 'Name of the entity',
        required: true,
      },
      {
        name: 'description',
        type: 'string',
        description: 'Description of the entity',
        required: false,
      },
    ],
    handler: async ({ entityType, name, description }) => {
      // This is a mock implementation
      // Real implementation will use Microsoft Agent Framework to persist to Cosmos DB
      console.log('Agent requested to create entity:', { entityType, name, description });
      
      return {
        success: true,
        message: `Mock: Created ${entityType} named "${name}"`,
        note: 'This is a demonstration. Real implementation will use AG-UI protocol to communicate with Microsoft Agent Framework backend.',
      };
    },
  });

  return {
    contextReady: true,
  };
}
