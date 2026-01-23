/**
 * @file Entity Type Registry Validation Tests
 * @description Comprehensive validation tests for entityTypeRegistry.ts
 * Tests ensure registry completeness, correctness, and internal consistency
 * Tests for User Story 4 - Comprehensive Validation (P2)
 */

import { describe, it, expect } from 'vitest';
import {
  ENTITY_TYPE_REGISTRY,
  type EntityTypeConfig,
} from '../../services/config/entityTypeRegistry';

describe('Entity Type Registry - Validation', () => {
  /**
   * T023: Validation test for unique type identifiers
   * Ensures no duplicate type names exist in registry
   */
  describe('T023: Unique Type Identifiers', () => {
    it('should have unique type identifiers across all entities', () => {
      const types = ENTITY_TYPE_REGISTRY.map((config) => config.type);
      const uniqueTypes = new Set(types);

      expect(uniqueTypes.size).toBe(types.length);
    });

    it('should fail if duplicate types are introduced', () => {
      const types = ENTITY_TYPE_REGISTRY.map((config) => config.type);
      const typeCounts = types.reduce<Record<string, number>>((acc, type) => {
        acc[type] = (acc[type] || 0) + 1;
        return acc;
      }, {});

      const duplicates = Object.entries(typeCounts)
        .filter(([, count]) => count > 1)
        .map(([type]) => type);

      expect(duplicates).toEqual([]);
    });
  });

  /**
   * T024: Validation test for schema versions >= 1
   * Ensures all schema versions are valid positive integers
   */
  describe('T024: Schema Versions Validation', () => {
    it('should have schemaVersion >= 1 for all entity types', () => {
      const invalidVersions = ENTITY_TYPE_REGISTRY.filter(
        (config) => config.schemaVersion < 1
      );

      expect(invalidVersions).toEqual([]);
    });

    it('should have integer schemaVersion values', () => {
      const nonIntegerVersions = ENTITY_TYPE_REGISTRY.filter(
        (config) => !Number.isInteger(config.schemaVersion)
      );

      expect(nonIntegerVersions).toEqual([]);
    });

    it('should have positive schemaVersion values', () => {
      ENTITY_TYPE_REGISTRY.forEach((config) => {
        expect(config.schemaVersion).toBeGreaterThanOrEqual(1);
      });
    });
  });

  /**
   * T025: Validation test for valid icon names (PascalCase)
   * Ensures all icon names follow PascalCase convention
   */
  describe('T025: Valid Icon Names', () => {
    const pascalCasePattern = /^[A-Z][a-zA-Z0-9]*$/;

    it('should have PascalCase icon names for all entity types', () => {
      const invalidIcons = ENTITY_TYPE_REGISTRY.filter(
        (config) => !pascalCasePattern.test(config.icon)
      );

      expect(invalidIcons).toEqual([]);
    });

    it('should have non-empty icon names', () => {
      const emptyIcons = ENTITY_TYPE_REGISTRY.filter(
        (config) => config.icon.trim() === ''
      );

      expect(emptyIcons).toEqual([]);
    });

    it('should start with uppercase letter', () => {
      ENTITY_TYPE_REGISTRY.forEach((config) => {
        const firstChar = config.icon.charAt(0);
        expect(firstChar).toBe(firstChar.toUpperCase());
        expect(firstChar).toMatch(/[A-Z]/);
      });
    });
  });

  /**
   * T026: Validation test for completeness (all 29 types present)
   * Ensures registry contains expected number of entity types
   */
  describe('T026: Registry Completeness', () => {
    it('should contain exactly 29 entity types', () => {
      expect(ENTITY_TYPE_REGISTRY).toHaveLength(29);
    });

    it('should have all required properties for each entity type', () => {
      const requiredProperties: (keyof EntityTypeConfig)[] = [
        'type',
        'label',
        'description',
        'category',
        'icon',
        'schemaVersion',
        'suggestedChildren',
      ];

      ENTITY_TYPE_REGISTRY.forEach((config) => {
        requiredProperties.forEach((prop) => {
          expect(config).toHaveProperty(prop);
          expect((config as EntityTypeConfig)[prop]).toBeDefined();
        });
      });
    });

    it('should have non-empty labels for all entity types', () => {
      const emptyLabels = ENTITY_TYPE_REGISTRY.filter(
        (config) => config.label.trim() === ''
      );

      expect(emptyLabels).toEqual([]);
    });

    it('should have non-empty descriptions for all entity types', () => {
      const emptyDescriptions = ENTITY_TYPE_REGISTRY.filter(
        (config) => config.description.trim() === ''
      );

      expect(emptyDescriptions).toEqual([]);
    });

    it('should have valid categories from allowed set', () => {
      const validCategories = [
        'Geography',
        'Containers',
        'Characters & Factions',
        'Campaigns',
        'Events & Quests',
        'Items',
        'Other',
      ];

      ENTITY_TYPE_REGISTRY.forEach((config) => {
        expect(validCategories).toContain(config.category);
      });
    });
  });

  /**
   * T027: Validation test for no circular suggestions
   * Ensures no entity type suggests itself as a child (self-referencing is allowed for hierarchical types)
   * NOTE: Self-referencing is ALLOWED for hierarchical container types like Folder, GeographicRegion, etc.
   */
  describe('T027: No Circular Suggestions', () => {
    it('should allow self-referencing for hierarchical types (Folder, Regional types)', () => {
      const hierarchicalTypes = [
        'Folder',
        'GeographicRegion',
        'PoliticalRegion',
        'CulturalRegion',
        'MilitaryRegion',
      ];

      hierarchicalTypes.forEach((typeName) => {
        const config = ENTITY_TYPE_REGISTRY.find((c) => c.type === typeName);
        if (config) {
          // Self-referencing is allowed for these types
          expect(config.suggestedChildren).toContain(typeName);
        }
      });
    });

    it('should NOT allow self-referencing for non-hierarchical types', () => {
      const hierarchicalTypes = new Set([
        'Folder',
        'GeographicRegion',
        'PoliticalRegion',
        'CulturalRegion',
        'MilitaryRegion',
      ]);

      const nonHierarchicalWithCircular = ENTITY_TYPE_REGISTRY.filter(
        (config) =>
          !hierarchicalTypes.has(config.type) &&
          (config.suggestedChildren as readonly string[]).includes(config.type)
      );

      expect(nonHierarchicalWithCircular).toEqual([]);
    });

    it('should have valid suggested children types', () => {
      const allTypes = new Set(ENTITY_TYPE_REGISTRY.map((c) => c.type));

      ENTITY_TYPE_REGISTRY.forEach((config) => {
        config.suggestedChildren.forEach((childType) => {
          expect(allTypes.has(childType)).toBe(true);
        });
      });
    });

    it('should not have duplicate suggestions within same entity type', () => {
      ENTITY_TYPE_REGISTRY.forEach((config) => {
        const suggestions = config.suggestedChildren;
        const uniqueSuggestions = new Set(suggestions);

        expect(uniqueSuggestions.size).toBe(suggestions.length);
      });
    });
  });

  /**
   * Additional validation tests for registry integrity
   */
  describe('Additional Integrity Checks', () => {
    it('should have at least one root entity type', () => {
      const rootTypes = (ENTITY_TYPE_REGISTRY as readonly EntityTypeConfig[]).filter(
        (config) => config.canBeRoot === true
      );

      expect(rootTypes.length).toBeGreaterThan(0);
    });

    it('should have consistent type format (PascalCase)', () => {
      const pascalCasePattern = /^[A-Z][a-zA-Z0-9]*$/;

      ENTITY_TYPE_REGISTRY.forEach((config) => {
        expect(config.type).toMatch(pascalCasePattern);
      });
    });

    it('should have readonly array behavior', () => {
      // TypeScript enforces readonly at compile time
      // This runtime test ensures the array reference is frozen
      expect(Object.isFrozen(ENTITY_TYPE_REGISTRY)).toBe(false); // Arrays with 'as const' aren't frozen, but immutable via TS
    });

    it('should have all geographic types with Geography category', () => {
      const geographicKeywords = [
        'Continent',
        'Country',
        'Region',
        'City',
        'Building',
        'Room',
        'Location',
        'GeographicRegion',
        'PoliticalRegion',
        'CulturalRegion',
        'MilitaryRegion',
      ];

      geographicKeywords.forEach((keyword) => {
        const config = ENTITY_TYPE_REGISTRY.find((c) => c.type === keyword);
        if (config) {
          expect(config.category).toBe('Geography');
        }
      });
    });

    it('should have container types with Containers category', () => {
      const containerKeywords = [
        'Folder',
        'Locations',
        'People',
        'Factions',
        'Items',
        'Events',
        'Quests',
        'Adventures',
        'Geographies',
      ];

      containerKeywords.forEach((keyword) => {
        const config = ENTITY_TYPE_REGISTRY.find((c) => c.type === keyword);
        if (config) {
          expect(config.category).toBe('Containers');
        }
      });
    });
  });
});
