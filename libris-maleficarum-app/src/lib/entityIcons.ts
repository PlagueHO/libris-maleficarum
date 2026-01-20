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
  Folder,
  CalendarDays,
  BookOpen,
  BookMarked,
  Bug,
  Box,
  Compass,
  Mountain,
  Shield,
  HelpCircle,
  type LucideIcon,
} from 'lucide-react';

/**
 * Entity type enumeration (must match WorldEntityType from types)
 */
export type EntityType =
  // Geographic types
  | 'Continent'
  | 'Country'
  | 'Region'
  | 'City'
  | 'Building'
  | 'Room'
  | 'Location'
  // Character & faction types
  | 'Character'
  | 'Faction'
  // Event & quest types
  | 'Event'
  | 'Quest'
  // Item types
  | 'Item'
  // Campaign types
  | 'Campaign'
  | 'Session'
  // Container types
  | 'Locations'
  | 'People'
  | 'Events'
  | 'History'
  | 'Lore'
  | 'Bestiary'
  | 'Items'
  | 'Adventures'
  | 'Geographies'
  // Regional types with custom properties
  | 'GeographicRegion'
  | 'PoliticalRegion'
  | 'CulturalRegion'
  | 'MilitaryRegion'
  // Other
  | 'Other';

/**
 * Mapping from entity type to Lucide icon component
 */
export const entityIconMap: Record<EntityType, LucideIcon> = {
  // Geographic types
  Continent: Globe,
  Country: Map,
  Region: MapPin,
  City: Building,
  Building: Building,
  Room: Home,
  Location: MapPin,
  
  // Character & faction types
  Character: User,
  Faction: Users,
  
  // Event & quest types
  Event: Calendar,
  Quest: Scroll,
  
  // Item types
  Item: Package,
  
  // Campaign types
  Campaign: Scroll,
  Session: Calendar,
  
  // Container types - organizational folders
  Locations: Folder,
  People: Users,
  Events: CalendarDays,
  History: BookOpen,
  Lore: BookMarked,
  Bestiary: Bug,
  Items: Box,
  Adventures: Compass,
  Geographies: Mountain,
  
  // Regional types with custom properties
  GeographicRegion: Globe,
  PoliticalRegion: Shield,
  CulturalRegion: Users,
  MilitaryRegion: Shield,
  
  // Other
  Other: HelpCircle,
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
