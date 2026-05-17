# Frontend Design

The Libris Maleficarum frontend is a modern, accessible, and visually engaging Single Page Application (SPA) built with React and TypeScript. The frontend code resides in the `libris-maleficarum-app/` folder.

## Application Structure

- **Single Page Application:**  
  The frontend is implemented as a SPA, providing fast navigation, seamless user experience, and dynamic content updates without full page reloads.

- **Layout:**  
  - **Top Toolbar:**  
    A persistent toolbar at the top provides access to global navigation, search, notifications, user profile, and quick actions. This is always visible and anchors the application.
  - **Side Panel (Left, Expandable):**  
    A collapsible sidebar on the left provides access to navigation, world selection (drop down), campaign structure and entities, entity management, and quick actions. This panel can be expanded or collapsed as needed. The panel will contain a settings button to open a settings modal for user preferences and application settings at the bottom.
  - **Main Panel (Center):**  
    The central area is the primary workspace for world-building, campaign management, editing entities, and viewing content. It adapts contextually to the user's current task.
  - **Chat Window (Right, Collapsible ):**  
    A chat/AI assistant panel on the right can be shown or hidden. This enables real-time collaboration, AI-driven suggestions, and contextual chat with the system or other users.

- **UI/UX Principles:**  
  - Modern, accessible, and responsive design.
  - Fantasy-themed styling, with rich colors, custom fonts, and immersive visual elements.
  - Keyboard navigation and ARIA support for accessibility.
  - Smooth transitions, animations, and feedback for user actions.

## Technology & Patterns

- **React 19 + TypeScript:**  
  All components are implemented as functional components using hooks, following best practices for maintainability and performance.

- **Redux Toolkit:**  
  Application state is managed using Redux Toolkit, enabling predictable state management, undo/redo, and time-travel debugging.

- **CopilotKit Integration:**  
  - **Agentic UI Components:** `<CopilotChat />`, `<CopilotSidebar />`, `<CopilotTextarea />` for AI-powered interactions.
  - **Shared State:** Bidirectional synchronization between frontend state and backend agents via AG-UI protocol.
  - **Frontend Actions:** User-triggered AI operations that execute in the frontend with agent coordination.
  - **Generative UI:** Dynamic component rendering based on agent responses and tool outputs.
  - **Human-in-the-Loop:** User approval workflows for critical agent operations.
  - **AG-UI Client:** Native protocol support for agent-user interaction (SSE/WebSocket).

- **TailwindCSS:**  
  Utility-first CSS framework for rapid, maintainable styling with custom dark fantasy theme.

- **Shadcn/ui + Radix UI:**  
  - Copy-paste component library built on Radix UI primitives (headless, accessible)
  - Fully customizable components for Card, Button, Dialog, etc.
  - Accessible by default (ARIA, keyboard navigation, screen reader support)
  - Perfect for creating immersive fantasy-themed interfaces
  - Owned by the project (components live in `src/components/ui/`)

- **Accessibility:**  
  - All interactive elements are accessible via keyboard.
  - ARIA roles and labels are used throughout.
  - High-contrast and screen reader support.
  - Radix UI primitives ensure WCAG AA compliance by default.
  - Testable with jest-axe.

## Visual Theme

- **Dark Fantasy Theme:**  
  - Custom TailwindCSS theme with HSL color variables for easy customization
  - Dark slate backgrounds (220° hue, 18% saturation, 10-12% lightness)
  - Gold/amber accents for interactive elements (42° hue, 88% saturation)
  - Purple secondary colors for mystical elements (280° hue, 40% saturation)
  - Crimson accent colors for important actions (355° hue, 65% saturation)
  - Optional light "parchment" theme for daytime use
  - Subtle shadows and borders to evoke a magical, immersive feel.
  - Typography: Inter font family for clean readability with fantasy aesthetic

## Summary

The frontend delivers a rich, modern, and accessible user experience, optimized for TTRPG world-building and campaign management. It leverages the latest React ecosystem tools and UI techniques, while maintaining a strong fantasy aesthetic and usability focus.
