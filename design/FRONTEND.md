# Frontend Design

The Libris Maleficarum frontend is a modern, accessible, and visually engaging Single Page Application (SPA) built with React and TypeScript.

## Application Structure

- **Single Page Application:**  
  The frontend is implemented as a SPA, providing fast navigation, seamless user experience, and dynamic content updates without full page reloads.

- **Layout:**  
  - **Top Toolbar:**  
    A persistent toolbar at the top provides access to global navigation, search, notifications, user profile, and quick actions. This is always visible and anchors the application.
  - **Expandable Tools Panel (Left):**  
    A collapsible sidebar on the left provides access to navigation, world/campaign tools, entity management, and quick actions. This panel can be expanded or collapsed as needed.
  - **Main Work Panel (Center):**  
    The central area is the primary workspace for world-building, campaign management, editing entities, and viewing content. It adapts contextually to the user's current task.
  - **Collapsible Chat Window (Right):**  
    A chat/AI assistant panel on the right can be shown or hidden. This enables real-time collaboration, AI-driven suggestions, and contextual chat with the system or other users.

- **UI/UX Principles:**  
  - Modern, accessible, and responsive design.
  - Fantasy-themed styling, with rich colors, custom fonts, and immersive visual elements.
  - Keyboard navigation and ARIA support for accessibility.
  - Smooth transitions, animations, and feedback for user actions.

## Technology & Patterns

- **React + TypeScript:**  
  All components are implemented as functional components using hooks, following best practices for maintainability and performance.

- **Redux:**  
  Application state is managed using Redux, enabling predictable state management, undo/redo, and time-travel debugging.

- **CSS Modules:**  
  Component-level styling is achieved using CSS modules for encapsulation and maintainability.

- **Component Library & UI Techniques:**  
  - Utilizes the latest React component patterns (e.g., compound components, context, custom hooks).
  - Leverages modern UI libraries (e.g., Radix UI, Headless UI, or custom fantasy-themed components).
  - Responsive design for desktop and tablet use, with mobile support planned.
  - Theming support for light/dark/fantasy modes.

- **Accessibility:**  
  - All interactive elements are accessible via keyboard.
  - ARIA roles and labels are used throughout.
  - High-contrast and screen reader support.

## Visual Theme

- **Fantasy Theme:**  
  - Custom color palette inspired by fantasy worlds (deep purples, golds, parchment, etc.).
  - Themed icons, backgrounds, and UI elements.
  - Subtle textures and decorative borders to evoke a magical, immersive feel.

## Summary

The frontend delivers a rich, modern, and accessible user experience, optimized for TTRPG world-building and campaign management. It leverages the latest React ecosystem tools and UI techniques, while maintaining a strong fantasy aesthetic and usability focus.
