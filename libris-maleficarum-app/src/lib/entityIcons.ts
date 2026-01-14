/**
 * Entity type to Lucide icon mapping
 * 
 * Maps WorldEntityType to corresponding Lucide React icons for visual hierarchy.
 * Used by EntityTreeNode to display entity type icons in the sidebar.
 * 
 * @module entityIcons
 */

import {
  Globe,
  Map,
  MapPin,
  Building,
  Home,
  User,
  Users,
  Calendar,
  Package,
  Scroll,
  type LucideIcon,
} from 'lucide-react';

/**
 * Entity type enumeration (must match WorldEntityType from types)
 */
export type EntityType =
  | 'Continent'
  | 'Country'
  | 'Region'
  | 'City'
  | 'Location'
  | 'Character'
  | 'Organization'
  | 'Event'
  | 'Item'
  | 'Campaign';

/**
 * Mapping from entity type to Lucide icon component
 */
export const entityIconMap: Record<EntityType, LucideIcon> = {
  Continent: Globe,        // Geographic: Globe for continents
  Country: Map,            // Geographic: Map for countries
  Region: MapPin,          // Geographic: MapPin for regions
  City: Building,          // Geographic: Building for cities
  Location: Home,          // Geographic: Home for specific locations
  Character: User,         // Narrative: User for characters
  Organization: Users,     // Narrative: Users (group) for organizations
  Event: Calendar,         // Narrative: Calendar for events
  Item: Package,           // Narrative: Package for items
  Campaign: Scroll,        // Campaign: Scroll for campaign
};

/**
 * Get icon component for entity type
 * 
 * @param entityType - The entity type
 * @returns Lucide icon component
 * @throws Error if entity type is not recognized
 */
export function getEntityIcon(entityType: EntityType): LucideIcon {
  const icon = entityIconMap[entityType];
  
  if (!icon) {
    console.warn(`No icon mapped for entity type "${entityType}", using default`);
    return Package; // Default fallback icon
  }

  return icon;
}

/**
 * Get all available entity types
 * 
 * @returns Array of entity types
 */
export function getAllEntityTypes(): EntityType[] {
  return Object.keys(entityIconMap) as EntityType[];
}
