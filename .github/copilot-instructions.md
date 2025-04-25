# Project coding standards

## TypeScript Guidelines
- Use TypeScript for all new code
- Follow functional programming principles where possible
- Use interfaces for data structures and type definitions
- Prefer immutable data (const, readonly)
- Use optional chaining (?.) and nullish coalescing (??) operators

## React Guidelines
- Use functional components with hooks
- Follow the React hooks rules (no conditional hooks)
- Use React.FC type for components with children
- Keep components small and focused
- Use CSS modules for component styling

## Naming Conventions
- Use PascalCase for component names, interfaces, and type aliases
- Use camelCase for variables, functions, and methods
- Prefix private class members with underscore (_)
- Use ALL_CAPS for constants

## Error Handling
- Use try/catch blocks for async operations
- Implement proper error boundaries in React components
- Always log errors with contextual information

## C# .NET Guidelines
- Use .NET 8 and Aspire.NET for backend development
- Follow Clean/Hexagonal Architecture principles
- Implement Repository Pattern for data access
- Optionally use Mediator Pattern for decoupling components
- Use Entity Framework Core with Cosmos DB provider
- Configure entities explicitly in DbContext (`OnModelCreating`)
- Use composite or synthetic partition keys (`UserId` + `CampaignId`)
- Prefer asynchronous methods (`async/await`) for I/O-bound operations
- Use dependency injection for service registration and resolution
- Follow PascalCase naming for classes, methods, properties, and namespaces
- Use camelCase naming for local variables and method parameters
- Prefix private fields with underscore (_)
- Use ALL_CAPS for constants
- Implement structured logging with contextual information
- Use try/catch blocks for handling exceptions and log errors appropriately
