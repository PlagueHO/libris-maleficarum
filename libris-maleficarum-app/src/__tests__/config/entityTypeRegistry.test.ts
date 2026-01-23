/**
 * @file Entity Type Registry Helper Function Tests
 * @description Tests for helper functions in entityTypeRegistry.ts
 * Tests for User Story 5 - Helper Functions (P2)
 */

import { describe, it, expect } from 'vitest';
import {
  getEntityTypeConfig,
  getRootEntityTypes,
  getAllEntityTypes,
  ENTITY_TYPE_REGISTRY,
  type EntityTypeConfig,
} from '../../services/config/entityTypeRegistry';
import { WorldEntityType } from '../../services/types/worldEntity.types';

describe('Entity Type Registry - Helper Functions', () => {
  /**
   * T030: Test for getEntityTypeConfig() with valid type
   * Ensures function returns correct configuration for valid entity types
   */
  describe('T030: getEntityTypeConfig() - Valid Type', () => {
    it('should return configuration for valid entity type', () => {
      const config = getEntityTypeConfig(WorldEntityType.Continent);

      expect(config).toBeDefined();
      expect(config?.type).toBe('Continent');
      expect(config?.label).toBe('Continent');
      expect(config?.category).toBe('Geography');
      expect(config?.schemaVersion).toBeGreaterThanOrEqual(1);
    });

    it('should return correct configuration for all entity types', () => {
      const allTypes = getAllEntityTypes();

      allTypes.forEach((expectedConfig) => {
        const config = getEntityTypeConfig(
          expectedConfig.type as WorldEntityType
        );

        expect(config).toBeDefined();
        expect(config).toEqual(expectedConfig);
      });
    });

    it('should have correct TypeScript return type', () => {
      const config = getEntityTypeConfig(WorldEntityType.Campaign);

      // TypeScript should infer EntityTypeConfig | undefined
      if (config) {
        expect(config.type).toBe('Campaign');
        expect(config.schemaVersion).toBeTypeOf('number');
        expect(config.suggestedChildren).toBeInstanceOf(Array);
      }
    });

    it('should return configuration with all required properties', () => {
      const config = getEntityTypeConfig(WorldEntityType.Character);

      expect(config).toBeDefined();
      if (config) {
        expect(config).toHaveProperty('type');
        expect(config).toHaveProperty('label');
        expect(config).toHaveProperty('description');
        expect(config).toHaveProperty('category');
        expect(config).toHaveProperty('icon');
        expect(config).toHaveProperty('schemaVersion');
        expect(config).toHaveProperty('suggestedChildren');
      }
    });
  });

  /**
   * T031: Test for getEntityTypeConfig() with invalid type
   * Ensures function returns undefined for invalid entity types
   */
  describe('T031: getEntityTypeConfig() - Invalid Type', () => {
    it('should return undefined for invalid entity type', () => {
      const config = getEntityTypeConfig('InvalidType' as WorldEntityType);

      expect(config).toBeUndefined();
    });

    it('should return undefined for empty string', () => {
      const config = getEntityTypeConfig('' as WorldEntityType);

      expect(config).toBeUndefined();
    });

    it('should return undefined for null/undefined', () => {
      const configNull = getEntityTypeConfig(null as unknown as WorldEntityType);
      const configUndefined = getEntityTypeConfig(
        undefined as unknown as WorldEntityType
      );

      expect(configNull).toBeUndefined();
      expect(configUndefined).toBeUndefined();
    });

    it('should handle case-sensitive type names', () => {
      // Entity types are case-sensitive
      const config = getEntityTypeConfig('continent' as WorldEntityType);

      expect(config).toBeUndefined();
    });
  });

  /**
   * T032: Test for getRootEntityTypes()
   * Ensures function returns only types with canBeRoot: true
   */
  describe('T032: getRootEntityTypes() - Returns Only Root Types', () => {
    it('should return only entity types with canBeRoot: true', () => {
      const rootTypes = getRootEntityTypes();

      rootTypes.forEach((typeName) => {
        const config = getEntityTypeConfig(typeName);

        expect(config).toBeDefined();
        expect(config?.canBeRoot).toBe(true);
      });
    });

    it('should not return entity types without canBeRoot or canBeRoot: false', () => {
      const rootTypes = new Set(getRootEntityTypes());
      const nonRootConfigs = (ENTITY_TYPE_REGISTRY as readonly EntityTypeConfig[]).filter(
        (config) => config.canBeRoot !== true
      );

      nonRootConfigs.forEach((config) => {
        expect(rootTypes.has(config.type as WorldEntityType)).toBe(false);
      });
    });

    it('should return at least one root type', () => {
      const rootTypes = getRootEntityTypes();

      expect(rootTypes.length).toBeGreaterThan(0);
    });

    it('should return expected root types', () => {
      const rootTypes = getRootEntityTypes();
      const expectedRootTypes = [
        'Continent',
        'Campaign',
        'Folder',
        'Locations',
        'People',
        'Events',
        'History',
        'Lore',
        'Bestiary',
        'Items',
        'Adventures',
        'Geographies',
        'Other',
      ];

      expectedRootTypes.forEach((typeName) => {
        expect(rootTypes).toContain(typeName as WorldEntityType);
      });

      // Verify count matches
      expect(rootTypes.length).toBe(expectedRootTypes.length);
    });

    it('should have correct TypeScript return type', () => {
      const rootTypes = getRootEntityTypes();

      // Should be WorldEntityType[]
      expect(rootTypes).toBeInstanceOf(Array);
      rootTypes.forEach((typeName) => {
        expect(typeof typeName).toBe('string');
      });
    });
  });

  /**
   * T033: Test for getAllEntityTypes()
   * Ensures function returns complete registry
   */
  describe('T033: getAllEntityTypes() - Returns Complete Registry', () => {
    it('should return the complete entity type registry', () => {
      const allTypes = getAllEntityTypes();

      expect(allTypes).toBe(ENTITY_TYPE_REGISTRY);
    });

    it('should return 29 entity type configurations', () => {
      const allTypes = getAllEntityTypes();

      expect(allTypes).toHaveLength(29);
    });

    it('should return readonly array', () => {
      const allTypes = getAllEntityTypes();

      // TypeScript enforces readonly via type system
      expect(allTypes).toBeInstanceOf(Array);
    });

    it('should have correct TypeScript return type', () => {
      const allTypes = getAllEntityTypes();

      // Should be readonly EntityTypeConfig[]
      expect(allTypes).toBeInstanceOf(Array);
      allTypes.forEach((config) => {
        expect(config).toHaveProperty('type');
        expect(config).toHaveProperty('label');
        expect(config).toHaveProperty('description');
        expect(config).toHaveProperty('category');
        expect(config).toHaveProperty('icon');
        expect(config).toHaveProperty('schemaVersion');
        expect(config).toHaveProperty('suggestedChildren');
      });
    });

    it('should return same reference on multiple calls', () => {
      const allTypes1 = getAllEntityTypes();
      const allTypes2 = getAllEntityTypes();

      expect(allTypes1).toBe(allTypes2);
    });

    it('should include all entity types found via getEntityTypeConfig', () => {
      const allTypes = getAllEntityTypes();
      const allTypeNames = allTypes.map((c) => c.type);

      allTypeNames.forEach((typeName) => {
        const config = getEntityTypeConfig(typeName as WorldEntityType);
        expect(config).toBeDefined();
      });
    });
  });

  /**
   * Integration tests for helper functions
   */
  describe('Helper Function Integration', () => {
    it('should have consistent data between all helper functions', () => {
      const allTypes = getAllEntityTypes();
      const rootTypes = getRootEntityTypes();

      // All root types should be found in complete registry
      rootTypes.forEach((typeName) => {
        const config = allTypes.find((c) => c.type === typeName);
        expect(config).toBeDefined();
        expect(config?.canBeRoot).toBe(true);
      });
    });

    it('should work together for common use cases', () => {
      // Use case: Get configuration for all root types
      const rootTypes = getRootEntityTypes();
      const rootConfigs = rootTypes
        .map((typeName) => getEntityTypeConfig(typeName))
        .filter((config): config is EntityTypeConfig => config !== undefined);

      expect(rootConfigs.length).toBe(rootTypes.length);
      rootConfigs.forEach((config) => {
        expect(config.canBeRoot).toBe(true);
      });
    });

    it('should support filtering entity types by category', () => {
      const allTypes = getAllEntityTypes();
      const geographyTypes = allTypes.filter(
        (config) => config.category === 'Geography'
      );

      expect(geographyTypes.length).toBeGreaterThan(0);
      geographyTypes.forEach((config) => {
        expect(config.category).toBe('Geography');
      });
    });

    it('should support finding suggested children configurations', () => {
      const continentConfig = getEntityTypeConfig(WorldEntityType.Continent);
      expect(continentConfig).toBeDefined();

      if (continentConfig) {
        const suggestedConfigs = continentConfig.suggestedChildren
          .map((typeName) =>
            getEntityTypeConfig(typeName as WorldEntityType)
          )
          .filter(
            (config): config is EntityTypeConfig => config !== undefined
          );

        expect(suggestedConfigs.length).toBe(
          continentConfig.suggestedChildren.length
        );
      }
    });
  });
});
