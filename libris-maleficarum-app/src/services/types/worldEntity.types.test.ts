/**
 * Tests for WorldEntity types, metadata, and suggestions
 * 
 * @module services/types/worldEntity.types.test
 */

import { describe, it, expect } from 'vitest';
import {
  WorldEntityType,
  ENTITY_TYPE_META,
  ENTITY_TYPE_SUGGESTIONS,
  getEntityTypeSuggestions,
  getEntityTypeMeta,
} from './worldEntity.types';

describe('WorldEntityType enum', () => {
  describe('existing types', () => {
    it('includes all original geographic types', () => {
      expect(WorldEntityType.Continent).toBe('Continent');
      expect(WorldEntityType.Country).toBe('Country');
      expect(WorldEntityType.Region).toBe('Region');
      expect(WorldEntityType.City).toBe('City');
      expect(WorldEntityType.Building).toBe('Building');
      expect(WorldEntityType.Room).toBe('Room');
      expect(WorldEntityType.Location).toBe('Location');
    });

    it('includes all original narrative types', () => {
      expect(WorldEntityType.Character).toBe('Character');
      expect(WorldEntityType.Faction).toBe('Faction');
      expect(WorldEntityType.Event).toBe('Event');
      expect(WorldEntityType.Quest).toBe('Quest');
      expect(WorldEntityType.Item).toBe('Item');
    });

    it('includes all original campaign types', () => {
      expect(WorldEntityType.Campaign).toBe('Campaign');
      expect(WorldEntityType.Session).toBe('Session');
    });

    it('includes Other type', () => {
      expect(WorldEntityType.Other).toBe('Other');
    });
  });

  describe('new Container types', () => {
    it('includes Locations container', () => {
      expect(WorldEntityType.Locations).toBe('Locations');
    });

    it('includes People container', () => {
      expect(WorldEntityType.People).toBe('People');
    });

    it('includes Events container', () => {
      expect(WorldEntityType.Events).toBe('Events');
    });

    it('includes History container', () => {
      expect(WorldEntityType.History).toBe('History');
    });

    it('includes Lore container', () => {
      expect(WorldEntityType.Lore).toBe('Lore');
    });

    it('includes Bestiary container', () => {
      expect(WorldEntityType.Bestiary).toBe('Bestiary');
    });

    it('includes Items container', () => {
      expect(WorldEntityType.Items).toBe('Items');
    });

    it('includes Adventures container', () => {
      expect(WorldEntityType.Adventures).toBe('Adventures');
    });

    it('includes Geographies container', () => {
      expect(WorldEntityType.Geographies).toBe('Geographies');
    });
  });

  describe('new Regional types', () => {
    it('includes GeographicRegion', () => {
      expect(WorldEntityType.GeographicRegion).toBe('GeographicRegion');
    });

    it('includes PoliticalRegion', () => {
      expect(WorldEntityType.PoliticalRegion).toBe('PoliticalRegion');
    });

    it('includes CulturalRegion', () => {
      expect(WorldEntityType.CulturalRegion).toBe('CulturalRegion');
    });

    it('includes MilitaryRegion', () => {
      expect(WorldEntityType.MilitaryRegion).toBe('MilitaryRegion');
    });
  });
});

describe('ENTITY_TYPE_META', () => {
  describe('Container type metadata', () => {
    it('has metadata for Locations container', () => {
      const meta = ENTITY_TYPE_META[WorldEntityType.Locations];
      expect(meta.label).toBe('Locations');
      expect(meta.category).toBe('Containers');
      expect(meta.icon).toBe('Folder');
      expect(meta.description).toContain('geographic');
    });

    it('has metadata for People container', () => {
      const meta = ENTITY_TYPE_META[WorldEntityType.People];
      expect(meta.label).toBe('People');
      expect(meta.category).toBe('Containers');
      expect(meta.icon).toBe('Users');
      expect(meta.description).toContain('characters');
    });

    it('has metadata for Events container', () => {
      const meta = ENTITY_TYPE_META[WorldEntityType.Events];
      expect(meta.label).toBe('Events');
      expect(meta.category).toBe('Containers');
      expect(meta.icon).toBe('CalendarDays');
    });

    it('has metadata for History container', () => {
      const meta = ENTITY_TYPE_META[WorldEntityType.History];
      expect(meta.label).toBe('History');
      expect(meta.category).toBe('Containers');
      expect(meta.icon).toBe('BookOpen');
    });

    it('has metadata for Lore container', () => {
      const meta = ENTITY_TYPE_META[WorldEntityType.Lore];
      expect(meta.label).toBe('Lore');
      expect(meta.category).toBe('Containers');
      expect(meta.icon).toBe('BookMarked');
    });

    it('has metadata for Bestiary container', () => {
      const meta = ENTITY_TYPE_META[WorldEntityType.Bestiary];
      expect(meta.label).toBe('Bestiary');
      expect(meta.category).toBe('Containers');
      expect(meta.icon).toBe('Bug');
    });

    it('has metadata for Items container', () => {
      const meta = ENTITY_TYPE_META[WorldEntityType.Items];
      expect(meta.label).toBe('Items');
      expect(meta.category).toBe('Containers');
      expect(meta.icon).toBe('Box');
    });

    it('has metadata for Adventures container', () => {
      const meta = ENTITY_TYPE_META[WorldEntityType.Adventures];
      expect(meta.label).toBe('Adventures');
      expect(meta.category).toBe('Containers');
      expect(meta.icon).toBe('Compass');
    });

    it('has metadata for Geographies container', () => {
      const meta = ENTITY_TYPE_META[WorldEntityType.Geographies];
      expect(meta.label).toBe('Geographies');
      expect(meta.category).toBe('Containers');
      expect(meta.icon).toBe('Mountain');
    });

    it('all Container types have Containers category', () => {
      const containerTypes = [
        WorldEntityType.Locations,
        WorldEntityType.People,
        WorldEntityType.Events,
        WorldEntityType.History,
        WorldEntityType.Lore,
        WorldEntityType.Bestiary,
        WorldEntityType.Items,
        WorldEntityType.Adventures,
        WorldEntityType.Geographies,
      ];

      containerTypes.forEach(type => {
        expect(ENTITY_TYPE_META[type].category).toBe('Containers');
      });
    });

    it('all Container types have icon defined', () => {
      const containerTypes = [
        WorldEntityType.Locations,
        WorldEntityType.People,
        WorldEntityType.Events,
        WorldEntityType.History,
        WorldEntityType.Lore,
        WorldEntityType.Bestiary,
        WorldEntityType.Items,
        WorldEntityType.Adventures,
        WorldEntityType.Geographies,
      ];

      containerTypes.forEach(type => {
        expect(ENTITY_TYPE_META[type].icon).toBeDefined();
        expect(ENTITY_TYPE_META[type].icon).not.toBe('');
      });
    });
  });

  describe('Regional type metadata', () => {
    it('has metadata for GeographicRegion', () => {
      const meta = ENTITY_TYPE_META[WorldEntityType.GeographicRegion];
      expect(meta.label).toBe('Geographic Region');
      expect(meta.category).toBe('Geography');
      expect(meta.icon).toBe('Globe');
      expect(meta.description).toContain('climate');
    });

    it('has metadata for PoliticalRegion', () => {
      const meta = ENTITY_TYPE_META[WorldEntityType.PoliticalRegion];
      expect(meta.label).toBe('Political Region');
      expect(meta.category).toBe('Geography');
      expect(meta.icon).toBe('Shield');
      expect(meta.description).toContain('Political');
    });

    it('has metadata for CulturalRegion', () => {
      const meta = ENTITY_TYPE_META[WorldEntityType.CulturalRegion];
      expect(meta.label).toBe('Cultural Region');
      expect(meta.category).toBe('Geography');
      expect(meta.icon).toBe('Users');
      expect(meta.description).toContain('Cultural');
    });

    it('has metadata for MilitaryRegion', () => {
      const meta = ENTITY_TYPE_META[WorldEntityType.MilitaryRegion];
      expect(meta.label).toBe('Military Region');
      expect(meta.category).toBe('Geography');
      expect(meta.icon).toBe('Shield');
      expect(meta.description).toContain('Military');
    });

    it('all Regional types have Geography category', () => {
      const regionalTypes = [
        WorldEntityType.GeographicRegion,
        WorldEntityType.PoliticalRegion,
        WorldEntityType.CulturalRegion,
        WorldEntityType.MilitaryRegion,
      ];

      regionalTypes.forEach(type => {
        expect(ENTITY_TYPE_META[type].category).toBe('Geography');
      });
    });

    it('all Regional types have icon defined', () => {
      const regionalTypes = [
        WorldEntityType.GeographicRegion,
        WorldEntityType.PoliticalRegion,
        WorldEntityType.CulturalRegion,
        WorldEntityType.MilitaryRegion,
      ];

      regionalTypes.forEach(type => {
        expect(ENTITY_TYPE_META[type].icon).toBeDefined();
        expect(ENTITY_TYPE_META[type].icon).not.toBe('');
      });
    });
  });

  describe('getEntityTypeMeta function', () => {
    it('returns metadata for Container type', () => {
      const meta = getEntityTypeMeta(WorldEntityType.Locations);
      expect(meta).toBeDefined();
      expect(meta.label).toBe('Locations');
      expect(meta.category).toBe('Containers');
    });

    it('returns metadata for Regional type', () => {
      const meta = getEntityTypeMeta(WorldEntityType.GeographicRegion);
      expect(meta).toBeDefined();
      expect(meta.label).toBe('Geographic Region');
      expect(meta.category).toBe('Geography');
    });

    it('returns metadata for existing type', () => {
      const meta = getEntityTypeMeta(WorldEntityType.Continent);
      expect(meta).toBeDefined();
      expect(meta.label).toBe('Continent');
    });
  });
});

describe('ENTITY_TYPE_SUGGESTIONS', () => {
  describe('Container type suggestions', () => {
    it('Locations container suggests geographic types', () => {
      const suggestions = ENTITY_TYPE_SUGGESTIONS[WorldEntityType.Locations];
      expect(suggestions).toContain(WorldEntityType.Continent);
      expect(suggestions).toContain(WorldEntityType.GeographicRegion);
      expect(suggestions).toContain(WorldEntityType.PoliticalRegion);
      expect(suggestions).toContain(WorldEntityType.Country);
    });

    it('People container suggests character types', () => {
      const suggestions = ENTITY_TYPE_SUGGESTIONS[WorldEntityType.People];
      expect(suggestions).toContain(WorldEntityType.Character);
      expect(suggestions).toContain(WorldEntityType.Faction);
    });

    it('Events container suggests event types', () => {
      const suggestions = ENTITY_TYPE_SUGGESTIONS[WorldEntityType.Events];
      expect(suggestions).toContain(WorldEntityType.Event);
      expect(suggestions).toContain(WorldEntityType.Quest);
    });

    it('Adventures container suggests campaign types', () => {
      const suggestions = ENTITY_TYPE_SUGGESTIONS[WorldEntityType.Adventures];
      expect(suggestions).toContain(WorldEntityType.Campaign);
      expect(suggestions).toContain(WorldEntityType.Session);
      expect(suggestions).toContain(WorldEntityType.Quest);
    });
  });

  describe('Regional type suggestions', () => {
    it('GeographicRegion suggests nesting capability', () => {
      const suggestions = ENTITY_TYPE_SUGGESTIONS[WorldEntityType.GeographicRegion];
      expect(suggestions).toContain(WorldEntityType.GeographicRegion); // Can nest
      expect(suggestions).toContain(WorldEntityType.Country);
      expect(suggestions).toContain(WorldEntityType.Region);
    });

    it('PoliticalRegion suggests nesting capability', () => {
      const suggestions = ENTITY_TYPE_SUGGESTIONS[WorldEntityType.PoliticalRegion];
      expect(suggestions).toContain(WorldEntityType.PoliticalRegion); // Can nest
      expect(suggestions).toContain(WorldEntityType.Country);
    });

    it('CulturalRegion suggests nesting capability', () => {
      const suggestions = ENTITY_TYPE_SUGGESTIONS[WorldEntityType.CulturalRegion];
      expect(suggestions).toContain(WorldEntityType.CulturalRegion); // Can nest
    });

    it('MilitaryRegion suggests nesting capability', () => {
      const suggestions = ENTITY_TYPE_SUGGESTIONS[WorldEntityType.MilitaryRegion];
      expect(suggestions).toContain(WorldEntityType.MilitaryRegion); // Can nest
    });
  });

  describe('updated existing type suggestions', () => {
    it('Continent now suggests Regional types first', () => {
      const suggestions = ENTITY_TYPE_SUGGESTIONS[WorldEntityType.Continent];
      // GeographicRegion and PoliticalRegion should be in top positions
      expect(suggestions[0]).toBe(WorldEntityType.GeographicRegion);
      expect(suggestions[1]).toBe(WorldEntityType.PoliticalRegion);
    });
  });
});

describe('getEntityTypeSuggestions function', () => {
  describe('root level suggestions (no parent)', () => {
    it('returns Container types first when parent is null', () => {
      const suggestions = getEntityTypeSuggestions(null);
      expect(suggestions).toContain(WorldEntityType.Locations);
      expect(suggestions).toContain(WorldEntityType.People);
      expect(suggestions).toContain(WorldEntityType.Events);
      expect(suggestions).toContain(WorldEntityType.Adventures);
      expect(suggestions).toContain(WorldEntityType.Lore);
    });

    it('still includes Continent and Campaign for root', () => {
      const suggestions = getEntityTypeSuggestions(null);
      expect(suggestions).toContain(WorldEntityType.Continent);
      expect(suggestions).toContain(WorldEntityType.Campaign);
    });

    it('Container types appear before standard types', () => {
      const suggestions = getEntityTypeSuggestions(null);
      const locationsIndex = suggestions.indexOf(WorldEntityType.Locations);
      const continentIndex = suggestions.indexOf(WorldEntityType.Continent);
      expect(locationsIndex).toBeLessThan(continentIndex);
    });
  });

  describe('parent-based suggestions', () => {
    it('returns suggestions for Continent parent (including Regional types)', () => {
      const suggestions = getEntityTypeSuggestions(WorldEntityType.Continent);
      expect(suggestions).toContain(WorldEntityType.GeographicRegion);
      expect(suggestions).toContain(WorldEntityType.PoliticalRegion);
      expect(suggestions).toContain(WorldEntityType.Country);
    });

    it('returns suggestions for Container parent', () => {
      const suggestions = getEntityTypeSuggestions(WorldEntityType.Locations);
      expect(suggestions).toContain(WorldEntityType.Continent);
      expect(suggestions).toContain(WorldEntityType.GeographicRegion);
    });

    it('returns suggestions for Regional parent', () => {
      const suggestions = getEntityTypeSuggestions(WorldEntityType.GeographicRegion);
      expect(suggestions).toContain(WorldEntityType.GeographicRegion); // Nesting
      expect(suggestions).toContain(WorldEntityType.Country);
    });

    it('returns empty array for types with no suggestions', () => {
      const suggestions = getEntityTypeSuggestions(WorldEntityType.Item);
      expect(suggestions).toEqual([]);
    });

    it('returns empty array for unknown parent type', () => {
      const suggestions = getEntityTypeSuggestions('UnknownType' as WorldEntityType);
      expect(suggestions).toEqual([]);
    });
  });
});
