# Libris Maleficarum

**Libris Maleficarum** (Latin for "Book of Witchcraft") is an AI-enhanced campaign management and narrative tool designed for tabletop RPGs. While it's currently aimed at providing campaign, session, and character management, its flexible design and architecture will allow it to evolve into a general TTRPG assistant platform.

---

## Overview

Libris Maleficarum is built with modern, scalable technologies:

- A **.NET 8** backend using **Aspire** for cloud-native application composition, orchestration, and developer productivity.
- **Entity Framework Core** (Cosmos DB provider) alongside the **Repository Pattern** for data access.
- Azure Cosmos DB using multiple containers to support flexible, hierarchical, and extensible data structures.
- A **React** + **TypeScript** frontend that communicates with the backend via RESTful APIs.
- A stubbed identity service for future multi-user support and eventual integration with Entra ID CIAM.

---

## Architecture Overview

### High-Level Components

- **Backend (.NET 8 with Aspire.NET):**
  - API endpoints and business logic.
  - Entity Framework Core for Cosmos DB access.
  - Repository layer abstracting data operations.
  - Stubbed identity (via `IUserContextService`) to handle multi-user scenarios.

- **Frontend (React + TypeScript):**
  - Modern, responsive UI.
  - Communication with the backend using Axios.
  - Future integration with MSAL.js for authentication.

- **Data Storage (Azure Cosmos DB):**
  - Multiple containers to support hierarchical and flexible document structures.
  - Core entity is "World" at the root. Each "World" can contain nested documents (e.g., "Continent", "Country", etc.).
  - A dedicated container maintains the hierarchical structure of documents within each world.
  - Documents are indexed by Azure AI Search for advanced querying and semantic search.

- **Future Enhancements:**
  - Integration with Entra ID CIAM for secure, multi-user authentication.
  - Expansion of AI features to support dynamic narrative generation and analytics.

### High-Level Architecture Diagram

```mermaid
graph TD
    Frontend["Frontend (React + TypeScript)"]
    API["API Gateway (.NET 8 APIs with Aspire.NET)"]
    DomainServices["Domain & Application Services (Repositories)"]
    Identity["Identity & Authentication (Stubbed, future Entra ID CIAM)"]
    CosmosDB["Azure Cosmos DB (Multiple Containers)"]
    HierarchyContainer["Hierarchy Container (World/Hierarchy Structure)"]
    AIIndex["Azure AI Search (Indexing Documents)"]

    Frontend -->|Axios HTTP| API
    API --> DomainServices
    API --> Identity
    DomainServices --> CosmosDB
    CosmosDB --> HierarchyContainer
    CosmosDB --> AIIndex
    Identity --> CosmosDB
```

---

## Technology Stack

- **Backend:**
  - **.NET 8** & **Aspire.NET**
  - **Entity Framework Core** (Cosmos DB provider)
  - **Repository Pattern**
  - **IUserContextService** (stubbed; replace with Entra ID CIAM integration later)

- **Frontend:**
  - **React** with **TypeScript** (Create React App)
  - **Redux** for state management
  - **Axios** for HTTP communication
  - Optionally **React Router** for multi-page interface

- **Data Storage:**
  - **Azure Cosmos DB** using multiple containers for hierarchical, flexible entity storage and a dedicated container for document hierarchy.

- **Deployment & DevOps:**
  - Local development using the Cosmos DB Emulator
  - Dockerization for cloud deployment (Azure App Service or AKS)
  - CI/CD via GitHub Actions or Azure DevOps

---

## Data Model & Cosmos DB Design

All TTRPG-related entities are stored in Azure Cosmos DB using a flexible, hierarchical model.

### World Entity Container

The World Entity container is the core of the data model, designed to support complex world-building and campaign management. The following key features are included:

- **WorldEntity**: The core document type for all campaign/world data. Each "WorldEntity" can represent a "World" (root), or any nested entity such as "Continent", "Country", "Region", "City", "Character", etc. Entities can be arbitrarily nested to support complex world-building.
- **Ownership**: Every WorldEntity includes an `OwnerId` property (e.g., a user ID) to indicate the owner, though this may not be used yet.
- **Document IDs**: All WorldEntities use a GUID for the `id` property to ensure uniqueness.
- **Partition Key**: The partition key for the WorldEntity container is `/WorldId`. This ensures efficient lookups and RU/s usage, as most queries will be scoped to a single world.
- **Indexing**: All WorldEntities are indexed by Azure AI Search for advanced semantic and full-text search.

#### Example: WorldEntity Documents

```json
// WorldEntity document (World)
{
  "id": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "OwnerId": "user-abc",
  "Name": "Eldoria",
  "EntityType": "World",
  "CreatedDate": "2024-06-01T10:00:00Z",
  "ModifiedDate": "2024-06-01T10:00:00Z"
  // ... properties for WorldEntity (e.g., description, lore, etc.)
}

// WorldEntity document (Continent, child of World)
{
  "id": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "OwnerId": "user-abc",
  "Name": "Arcanis",
  "EntityType": "Continent",
  "CreatedDate": "2024-06-01T10:00:00Z",
  "ModifiedDate": "2024-06-01T10:00:00Z"
  // ... properties for WorldEntity (e.g., description, lore, etc.)

}

// WorldEntity document (Country, child of Continent)
{
  "id": "c9d8e7f6-5a4b-3c2d-1e0f-9a8b7c6d5e4f",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "OwnerId": "user-abc",
  "Name": "Valoria",
  "EntityType": "Country",
  "CreatedDate": "2024-06-01T10:00:00Z",
  "ModifiedDate": "2024-06-01T10:00:00Z"
  // ... properties for WorldEntity (e.g., description, lore, etc.)
}
```

- Each WorldEntity includes a reference to its parent (`ParentId`), the root world (`WorldId`), and an `OwnerId`.
- The `id` for every WorldEntity is a GUID.
- The partition key is `/WorldId` for efficient partitioning and query performance.
- The hierarchy container enables efficient traversal and management of nested WorldEntities.

### World Entity Hierarchy Container

A dedicated **World Entity Hierarchy container** is used to efficiently store and manage the relationships between WorldEntities (e.g., parent-child relationships). This container enables:

- Fast traversal of the world structure (e.g., finding all children of a given entity, or the full path to a root).
- Efficient queries for hierarchical operations (e.g., moving, copying, or deleting subtrees).
- **Partition Key**: The partition key for the hierarchy container is `/WorldId-ParentId` (a synthetic/composite key combining `WorldId` and `ParentId`). This ensures efficient queries for all children of a given parent within a world, which is the most common access pattern.

#### Example: WorldEntityHierarchy Document

```json
{
  "id": "f1e2d3c4-b5a6-7890-1234-56789abcdef0",
  "WorldId": "b8e8e7e2-1c2d-4c3a-9e7b-2a1b2c3d4e5f",
  "ParentId": "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
  "ChildIds": [],  // Added to represent child entities
  "Name": "Valoria",
  "EntityType": "Country"
}
```

- Each document in the hierarchy container represents a relationship (e.g., parent-child) between two WorldEntities.
- The `PartitionKey` is a hierarchical of `WorldId` and `ParentId` for efficient access to all children of a parent within a world.
- This structure supports advanced queries and operations on the world graph.

#### WorldEntity.EntityType and WorldEntityHierarchy.EntityType Properties

The `EntityType` property defines the type of each WorldEntity. The following is a suggested hierarchy for TTRPG world/campaign/setting/adventure/scenario management:

- **World** (root)
  - **Locations**
    - Continent
    - Country
    - Region
    - City
    - Settlement
    - Landmark
    - Dungeon
    - Building
    - Room
    - Map
  - **Geographies**
    - Mountain
    - River
    - Lake
    - Forest
    - Desert
    - Ocean
    - Island
    - ClimateZone
  - **People**
    - Character
      - PlayerCharacter
      - NonPlayerCharacter
    - Organization
    - Faction
    - Family
    - Race
    - Culture
  - **Events**
    - HistoricalEvent
    - CurrentEvent
    - Quest
    - Encounter
    - Battle
    - Festival
    - Disaster
  - **History**
    - Timeline
    - Era
    - Chronicle
    - Legend
    - Myth
  - **Lore**
    - Religion
    - Deity
    - MagicSystem
    - Artifact
    - Technology
    - Language
    - Law
    - Custom
    - Story
  - **Bestiary**
    - Creature
    - Monster
    - Animal
  - **Items**
    - Equipment
    - Weapon
    - Armor
    - Treasure
    - Consumable
  - **Adventures**
    - Campaign
    - Scenario
    - Session
    - Scene
    - PlotHook
    - Secret
  - **Other**
    - Note
    - Image
    - Audio
    - Document
    - Homebrew

This hierarchy is extensible and can be expanded as needed for different TTRPG systems and campaign needs.

There will need to be some form of rules engine or validation to ensure that WorldEntity heirarchy is valid, e.g. a "Continent" cannot be a child of a "Monster", but a "Monster" can be a child of a "Continent". We could use AI validation for this, or a simple rules engine.

---

## Azure Architecture

Libris Maleficarum leverages Azure cloud services for scalability, security, and extensibility:

- **Frontend Hosting:**
  - **Azure Static Web Apps** for hosting the React + TypeScript frontend.
  - Optionally, **Azure Front Door** for global CDN, SSL termination, and enhanced security.

- **Backend APIs:**
  - **Azure Container Apps (ACA)** hosting .NET 8 APIs (Aspire.NET).
  - All backend communications routed through ACA-hosted APIs.

- **Data Storage:**
  - **Azure Cosmos DB** (multiple containers for hierarchical world/entity storage and hierarchy management).
  - **Azure Storage (Blob Storage)** for static assets (images, documents, etc.).

- **AI & Search Services:**
  - **Azure AI Search** for specialized indexing (e.g., embeddings, semantic search).
  - **Azure AI Services (multi-service account)** for AI-driven features (e.g., narrative generation, analytics).

- **Messaging & Eventing:**
  - **Azure Service Bus** for asynchronous messaging and event-driven communication between backend components.

- **Security & Secrets Management:**
  - **Azure Key Vault** for secure storage and management of secrets and configuration.

- **Monitoring & Observability:**
  - **Azure Application Insights** and **Log Analytics** for monitoring, logging, and diagnostics.

- **Networking & Security (Optional):**
  - **Azure Front Door** as a global entry point providing CDN, SSL termination, and enhanced security.

All backend communications from the frontend and external services are routed exclusively through the API layer hosted in Azure Container Apps, ensuring a secure and consistent interface.

### Azure Architecture Diagram

```mermaid
graph TD
    AzureFrontDoor["Azure Front Door (Optional)"]
    StaticWebApps["Azure Static Web Apps<br/>(React + TypeScript)"]
    ContainerApps["Azure Container Apps<br/>(.NET 8 APIs with Aspire.NET)"]
    CosmosDB["Azure Cosmos DB<br/>(Multiple Containers)"]
    HierarchyContainer["Hierarchy Container<br/>(World/Hierarchy Structure)"]
    AzureStorage["Azure Storage<br/>(Static Assets)"]
    AISearch["Azure AI Search<br/>(Specialized Indexing)"]
    ServiceBus["Azure Service Bus<br/>(Async Messaging)"]
    AIServices["Azure AI Services<br/>(Multi-service AI account)"]
    KeyVault["Azure Key Vault<br/>(Secrets Management)"]
    AppInsights["Azure Application Insights<br/>(Monitoring)"]
    LogAnalytics["Azure Log Analytics<br/>(Diagnostics)"]

    AzureFrontDoor --> StaticWebApps
    StaticWebApps --> ContainerApps
    ContainerApps --> CosmosDB
    CosmosDB --> HierarchyContainer
    CosmosDB --> AISearch
    ContainerApps --> AzureStorage
    ContainerApps --> ServiceBus
    ContainerApps --> AIServices

    %% Standalone monitoring and secrets boxes
    KeyVault
    AppInsights
    LogAnalytics
```

### Infrastructure as Code (IaC)

Infrastructure resources will be provisioned and managed using **Bicep** templates stored in the `infra` folder of the repository. These templates define all Azure resources, ensuring consistent, repeatable, and version-controlled deployments.

- **Bicep Templates Location:** `infra/`
- **Deployment:** Automated via GitHub Actions workflows stored in the `.github` folder.
- **Secrets Management:** Secrets and sensitive configuration values securely managed via **Azure Key Vault**.
- **Monitoring & Observability:** Integrated with **Azure Application Insights** and **Log Analytics** for comprehensive monitoring and diagnostics.

## CI/CD Strategy

Continuous Integration and Continuous Deployment (CI/CD) will be implemented using **GitHub Actions**, stored in the `.github` folder:

- **Build & Test:** Automated builds, unit tests, linting, and security scans.
- **Deployment:** Automated deployments to Azure Static Web Apps (frontend) and Azure Container Apps (backend APIs).
- **Infrastructure Deployment:** Automated deployment of Azure resources using Bicep templates.
- **Secrets Management:** Securely managed via Azure Key Vault integration.
- **Monitoring & Logging:** Integrated with Azure Application Insights and Log Analytics for operational visibility.

## Folder Structure

The repository is organized for clarity, scalability, and best practices for both .NET 8 backend and React/TypeScript frontend development.

```text
/
├── .devcontainer/           # GitHub Codespaces/dev container config
├── .github/                 # GitHub Actions workflows and community files
├── infra/                   # Infrastructure as Code (Bicep templates, deployment scripts)
├── backend/                 # .NET 8 backend solution
│   ├── src/                         # All backend projects and solution
│   │   ├── LibrisMaleficarum.sln            # Solution file
│   │   ├── Api/                            # ASP.NET Core minimal API project
│   │   │   └── LibrisMaleficarum.Api.csproj
│   │   ├── Application/                    # Application layer (CQRS, services, DTOs)
│   │   │   └── LibrisMaleficarum.Application.csproj
│   │   ├── Domain/                         # Domain entities, value objects, interfaces
│   │   │   └── LibrisMaleficarum.Domain.csproj
│   │   ├── Infrastructure/                 # EF Core, Cosmos DB, repository implementations
│   │   │   └── LibrisMaleficarum.Infrastructure.csproj
│   │   └── tests/                          # Backend unit/integration tests
│   │       ├── LibrisMaleficarum.Api.Tests/
│   │       ├── LibrisMaleficarum.Application.Tests/
│   │       ├── LibrisMaleficarum.Domain.Tests/
│   │       └── LibrisMaleficarum.Infrastructure.Tests/
├── frontend/                # React + TypeScript frontend app
│   ├── src/                 # Source code (components, hooks, services, types, etc.)
│   │   ├── components/
│   │   ├── hooks/
│   │   ├── services/
│   │   ├── types/
│   │   ├── App.tsx
│   │   └── index.tsx
│   ├── public/              # Static assets (index.html, images, etc.)
│   ├── tests/               # Frontend unit/integration tests
│   ├── package.json
│   ├── tsconfig.json
│   └── ...                  # Other config files (vite.config.ts, .env, etc.)
├── README.md                # Project overview and getting started
├── DESIGN.md                # Architecture and design documentation
└── ...                      # Solution-level files, .editorconfig, etc.
```

### Key Points

- **Backend** follows Clean/Hexagonal Architecture, with all projects and the solution file under `backend/src/`.
- **Backend tests** are in `backend/src/tests/` and mirror the main project structure.
- **Frontend** is in `frontend/` and follows React/TypeScript best practices: `src/components`, `src/hooks`, `src/services`, `src/types`, etc.
- **Frontend tests** are in `frontend/tests/`.
- **Infrastructure as Code** is in `infra/`.
- **Codespaces/devcontainer config** is in `.devcontainer/`.
- **CI/CD and GitHub workflows** are in `.github/`.

This structure supports modularity, testability, and maintainability for both backend and frontend codebases.

## Automated Test Approaches

A comprehensive automated testing strategy is essential for ensuring code quality, maintainability, and reliability across both frontend and backend.

### Frontend (React + TypeScript)

- **Unit Testing:**  
  Use [Jest](https://jestjs.io/) as the primary test runner and assertion library for unit tests.  
  [React Testing Library](https://testing-library.com/docs/react-testing-library/intro/) is used for testing React components in a way that simulates user interaction and focuses on behavior over implementation details.

- **Component Testing:**  
  Write focused tests for each React component, using mocks and stubs for dependencies.  
  Use [React Testing Library](https://testing-library.com/docs/react-testing-library/intro/) to test component rendering, props, events, and state changes.

- **Integration Testing:**  
  Test the integration of multiple components and services, including API calls (mocked using [msw](https://mswjs.io/)).

- **End-to-End (E2E) Testing:**  
  Optionally, use [Cypress](https://www.cypress.io/) for E2E tests to simulate real user workflows in the browser.

### Backend (.NET 8, Aspire.NET, EF Core)

- **Unit Testing:**  
  Use [xUnit](https://xunit.net/) for unit tests of application and domain logic.  
  Mock dependencies using [Moq](https://github.com/moq/moq4).  
  Use [FluentAssertions](https://fluentassertions.com/) for expressive assertions.

- **Component/Integration Testing:**  
  Write integration tests for repository implementations, service classes, and API endpoints.  
  Use [TestServer](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.testhost.testserver) from ASP.NET Core for in-memory API/component testing.  
  Use [Respawn](https://github.com/jbogard/Respawn) or similar tools to reset database state between tests.

- **API Testing:**  
  Test API endpoints using [xUnit](https://xunit.net/) and [FluentAssertions](https://fluentassertions.com/) for expressive assertions.  
  Use [WebApplicationFactory](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.testing.webapplicationfactory-1) for end-to-end API tests.

- **Test Data:**  
  Use builders or test data factories to create domain objects for tests, ensuring clarity and maintainability.

### General Strategies

- **Arrange-Act-Assert (AAA) Pattern:**  
  Structure all tests using the AAA pattern for clarity.

- **Continuous Integration:**  
  All tests are run automatically in CI pipelines (GitHub Actions) on pull requests and merges.

- **Code Coverage:**  
  Monitor code coverage for both frontend and backend, aiming for high coverage on critical logic.

- **Test Organization:**  
  - Frontend tests are located in `frontend/tests/` or colocated with components as `*.test.tsx`.
  - Backend tests are in `backend/src/tests/`, mirroring the main project structure.

### Summary of Testing Frameworks and Libraries

| Layer     | Frameworks/Libraries                | Test Types                |
|-----------|-------------------------------------|---------------------------|
| Frontend  | Jest, React Testing Library, Cypress| Unit, Component, E2E      |
| Backend   | xUnit, Moq, FluentAssertions, TestServer | Unit, Integration, API   |
