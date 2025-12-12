# Skipped Tests

These tests are temporarily skipped due to a known Vitest issue with CSS imports from dependencies.

## Issue

When running tests that import components using `@copilotkit/react-ui`, Vitest encounters an error:
```
TypeError: Unknown file extension ".css" for node_modules/.pnpm/katex@0.16.25/node_modules/katex/dist/katex.min.css
```

This occurs because:
1. `@copilotkit/react-ui` imports `@copilotkit/react-ui/styles.css`
2. That CSS file transitively imports `katex/dist/katex.min.css`
3. Vitest's ESM loader cannot handle CSS imports from dependencies

## Affected Tests

- `ChatWindow.test.tsx` - Tests for the ChatWindow component
- `WorldBuilderChat.test.tsx` - Tests for the WorldBuilderChat component

## Workarounds Attempted

- ✗ Mocking CSS imports in `vitest.setup.ts`
- ✗ Using Vite resolve aliases
- ✗ Creating custom Vite plugin to stub CSS
- ✗ Configuring `server.deps.inline`
- ✗ Using different pool options

## References

- [Vitest Issue #2834](https://github.com/vitest-dev/vitest/issues/2834)
- [Vite CSS handling in SSR](https://vitejs.dev/guide/ssr.html#ssr-specific-plugin-logic)

## Resolution

These tests will be re-enabled once:
1. Vitest adds better CSS handling for dependencies, or
2. CopilotKit provides a CSS-less import path, or
3. We migrate to a different test framework that handles this better

## Running These Tests

To run these tests manually (they will fail):
```bash
pnpm vitest run src/__tests__/skipped
```
