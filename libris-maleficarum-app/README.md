# Libris Maleficarum - Frontend Application

React + TypeScript application for TTRPG campaign management with AI-enhanced worldbuilding tools.

## Features

### Core Functionality
- **World Management**: Create and manage fantasy worlds with hierarchical entity structures
- **Entity Types**: Support for Worlds, Continents, Countries, Regions, Cities, Characters, Locations, and more
- **Schema-Driven Properties**: Dynamic property forms based on entity type schemas
- **Async Operations**: Non-blocking entity operations with real-time notification center

### Async Entity Operations (Feature 012)

Long-running operations (such as deleting entities with many children) run asynchronously without blocking the UI.

**Notification Center**:
- Bell icon (top-right) shows unread operation count
- Click to view all active and recent async operations
- Real-time updates via polling (3-second interval)
- Operations show status: pending, in-progress, completed, failed, cancelled
- Progress indicators for in-progress operations: "X% complete • N/Total items"

**Key Features**:
- **Optimistic Updates**: Deleted entities immediately disappear from the UI while backend processes asynchronously
- **Retry Failed Operations**: Click retry button on failed operations
- **Cancel In-Progress**: Cancel pending or in-progress operations  
- **Session-Only Persistence**: Notifications persist during browser session only (24-hour automatic cleanup)
- **Accessibility**: WCAG 2.2 Level AA compliant with ARIA live regions and keyboard navigation

## Tech Stack

This template provides a minimal setup to get React working in Vite with HMR and some ESLint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Babel](https://babeljs.io/) (or [oxc](https://oxc.rs) when used in [rolldown-vite](https://vite.dev/guide/rolldown)) for Fast Refresh
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/) for Fast Refresh

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the ESLint configuration

If you are developing a production application, we recommend updating the configuration to enable type-aware lint rules:

```js
export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...

      // Remove tseslint.configs.recommended and replace with this
      tseslint.configs.recommendedTypeChecked,
      // Alternatively, use this for stricter rules
      tseslint.configs.strictTypeChecked,
      // Optionally, add this for stylistic rules
      tseslint.configs.stylisticTypeChecked,

      // Other configs...
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```

You can also install [eslint-plugin-react-x](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-x) and [eslint-plugin-react-dom](https://github.com/Rel1cx/eslint-react/tree/main/packages/plugins/eslint-plugin-react-dom) for React-specific lint rules:

```js
// eslint.config.js
import reactX from 'eslint-plugin-react-x'
import reactDom from 'eslint-plugin-react-dom'

export default defineConfig([
  globalIgnores(['dist']),
  {
    files: ['**/*.{ts,tsx}'],
    extends: [
      // Other configs...
      // Enable lint rules for React
      reactX.configs['recommended-typescript'],
      // Enable lint rules for React DOM
      reactDom.configs.recommended,
    ],
    languageOptions: {
      parserOptions: {
        project: ['./tsconfig.node.json', './tsconfig.app.json'],
        tsconfigRootDir: import.meta.dirname,
      },
      // other options...
    },
  },
])
```
