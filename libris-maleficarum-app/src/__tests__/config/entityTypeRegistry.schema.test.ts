/// <reference types="node" />

import { readFileSync } from 'fs';
import { resolve } from 'path';
import Ajv2020 from 'ajv/dist/2020.js';
import { describe, expect, it } from 'vitest';

describe('Entity Type Registry JSON Schema', () => {
  it('validates registries/entity-types.json against entity-type-registry.schema.json', () => {
    const schemaPath = resolve(
      process.cwd(),
      '..',
      'schemas',
      'registries',
      'entity-type-registry.schema.json',
    );
    const dataPath = resolve(
      process.cwd(),
      '..',
      'registries',
      'entity-types.json',
    );

    const schema = JSON.parse(readFileSync(schemaPath, 'utf8'));
    const registryData = JSON.parse(readFileSync(dataPath, 'utf8'));

    const ajv = new Ajv2020({ allErrors: true, strict: true });
    const validate = ajv.compile(schema);
    const isValid = validate(registryData);

    if (!isValid) {
      const detail = (validate.errors ?? [])
        .map((error) => `${error.instancePath || '/'} ${error.message ?? ''}`.trim())
        .join('\n');
      throw new Error(`Registry JSON failed schema validation:\n${detail}`);
    }

    expect(isValid).toBe(true);
  });
});
