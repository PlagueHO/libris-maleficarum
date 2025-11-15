<!--
SYNC IMPACT REPORT
==================
Version Change: TEMPLATE → 1.0.0
Date: 2025-11-16

Modified Principles:
- NEW: I. Cloud-Native Architecture (defines Azure-first deployment strategy with network isolation)
- NEW: II. Clean Architecture & Separation of Concerns (establishes layered architecture principles)
- NEW: III. Test-Driven Development (NON-NEGOTIABLE) (mandates TDD with accessibility testing)
- NEW: IV. Framework & Technology Standards (specifies React 19+, .NET 9+, Aspire.NET, Microsoft Agent Framework)
- NEW: V. Developer Experience & Inner Loop (establishes Aspire.NET for local orchestration)
- NEW: VI. Security & Privacy by Default (enforces Zero Trust security model)
- NEW: VII. Semantic Versioning & Breaking Changes (defines versioning strategy with GitVersion)

Added Sections:
- Architecture Constraints (Data Model, Frontend Standards, Backend Standards, Infrastructure Standards)
- Development Workflow (Continuous Integration, Continuous Deployment, Review Requirements, Azure Federation Authentication)
- Governance (amendment process, enforcement, guidance reference)

Removed Sections:
- All template placeholders

Templates Requiring Updates:
✅ .specify/templates/plan-template.md - Updated (Constitution Check section aligns with new principles)
✅ .specify/templates/spec-template.md - Updated (User scenarios align with TDD principle)
✅ .specify/templates/tasks-template.md - No changes needed (task categorization compatible)
✅ .github/copilot-instructions.md - Updated (references constitution for governance)

Follow-up TODOs:
- None (all placeholders have been filled with concrete values)

Key Changes:
- Established Azure cloud-native architecture as primary deployment target
- Mandated Aspire.NET for developer inner loop orchestration
- Required Microsoft Agent Framework (NOT Semantic Kernel) for AI interactions
- Enforced network isolation with private endpoints and NSGs
- Specified exact technology versions (React 19+, .NET 9+, Fluent UI v9)
- Defined semantic versioning strategy with GitVersion automation
- Established Zero Trust security model with Azure Entra ID + RBAC
-->

# Libris Maleficarum Constitution

## Core Principles

### I. Cloud-Native Architecture

All components MUST be designed for cloud deployment with Azure as the primary target. Services MUST use network isolation with private endpoints where supported. Infrastructure MUST be defined as code using Azure Bicep with Azure Verified Modules (AVM). All resources MUST be tagged with `azd-env-name` for traceability.

**Rationale**: Ensures secure, scalable, traceable deployments aligned with Zero Trust security model and Microsoft Secure Future Initiative.

### II. Clean Architecture & Separation of Concerns

Backend MUST follow Clean/Hexagonal Architecture principles. Frontend MUST maintain clear separation between UI components, state management, and business logic. Each layer MUST have well-defined interfaces and dependencies flow inward toward domain entities.

**Rationale**: Enables independent testing, maintainability, and flexibility to swap implementations without affecting other layers.

### III. Test-Driven Development (NON-NEGOTIABLE)

Tests MUST be written before implementation. Frontend MUST include accessibility testing with jest-axe for all components. Backend MUST include unit, integration, and API tests. All tests MUST follow Arrange-Act-Assert (AAA) pattern. Test coverage MUST be monitored in CI pipeline.

**Rationale**: Ensures code quality, prevents regressions, guarantees accessibility compliance, and validates requirements before implementation.

### IV. Framework & Technology Standards

Frontend MUST use React 19+ with TypeScript, Fluent UI v9, Vitest, and Redux Toolkit. Backend MUST use .NET 9+ with Aspire.NET for orchestration and EF Core with Cosmos DB provider. AI interactions MUST use Microsoft Agent Framework (NOT Semantic Kernel). All infrastructure MUST use Azure Verified Modules (AVM) for Bicep.

**Rationale**: Standardizes on modern, well-supported frameworks that align with Microsoft ecosystem and enable rapid development with strong typing.

### V. Developer Experience & Inner Loop

Backend local development MUST use Aspire.NET AppHost for service orchestration. Frontend MUST provide hot reload via Vite. All services MUST be runnable with a single command. Connection strings and service discovery MUST be automated. Aspire Dashboard MUST be available for observability during development.

**Rationale**: Minimizes setup time, ensures consistent development environment, provides immediate feedback, and simplifies debugging distributed applications.

### VI. Security & Privacy by Default

Secrets MUST NEVER be hardcoded—use Azure Key Vault and managed identities. Public network access MUST be disabled where supported. All PaaS services MUST use private endpoints. Authentication MUST use Azure Entra ID. Role-Based Access Control (RBAC) MUST be enabled for all resources.

**Rationale**: Implements defense-in-depth security strategy, prevents credential leaks, reduces attack surface, and ensures compliance with security best practices.

### VII. Semantic Versioning & Breaking Changes

Version format MUST be MAJOR.MINOR.PATCH following Semantic Versioning 2.0.0. MAJOR bump for breaking changes, MINOR for backward-compatible features, PATCH for bug fixes. GitVersion MUST be used for automated versioning. Commit messages MUST use `+semver:` tags to trigger appropriate version bumps.

**Rationale**: Provides clear communication of change impact, enables automated versioning, and helps consumers understand upgrade implications.

## Architecture Constraints

### Data Model

- WorldEntity hierarchy MUST use partition key `/WorldId` for main container
- WorldEntityHierarchy MUST use composite partition key `/WorldId-ParentId` for efficient parent-child queries
- All entity IDs MUST be GUIDs
- Azure AI Search MUST be used for semantic and full-text search across entities
- Cosmos DB items MUST NOT exceed 2MB limit

### Frontend Standards

- Components MUST be functional with hooks (no class components)
- ARIA roles and labels MUST be present on all interactive elements
- CSS Modules MUST be co-located with components
- Each component MUST have corresponding `.test.tsx` file
- State management MUST use Redux Toolkit with typed `RootState` and `AppDispatch`

### Backend Standards

- Code MUST be organized in `src/` with projects: Api, Domain, Infrastructure, Orchestration
- Repository pattern MUST abstract data operations
- Aspire.NET AppHost MUST define service dependencies and configurations
- Microsoft Agent Framework MUST be used for all AI agent interactions
- NOT for production—Aspire is development-time only; use Azure Container Apps/Kubernetes for production

### Infrastructure Standards

- All Bicep files MUST pass `bicep lint` with zero errors/warnings
- AVM module versions MUST be kept current
- Network Security Groups (NSGs) MUST be defined for each subnet
- Subnet allocation: Frontend (10.0.1.0/24), Backend (10.0.2.0/24), Gateway (10.0.3.0/24), Shared (10.0.4.0/24)
- Deployment validation MUST use `azd provision --preview` before applying changes

## Development Workflow

### Continuous Integration

- Pull requests MUST trigger linting and Bicep validation
- All tests MUST pass before merge
- Code coverage reports MUST be generated
- Breaking changes MUST be explicitly justified and documented

### Continuous Deployment

- Pushes to `main` MUST trigger deployment to Test environment
- GitVersion MUST determine build version automatically
- Infrastructure validation MUST precede deployment ("what-if" check)
- Secrets MUST be stored in GitHub environment secrets (not repository secrets)

### Review Requirements

- All PRs MUST verify constitutional compliance
- Accessibility violations MUST block PR merge
- Security issues MUST be addressed before merge
- Complexity additions MUST be justified with rationale

### Azure Federation Authentication

- GitHub Actions MUST authenticate via Azure AD Federated Credentials
- Credentials MUST use format: `repo:<org>/<repo>:environment:<env>`
- Contributor role MUST be assigned to service principal at subscription level
- Secrets required: `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_CLIENT_ID`

## Governance

This constitution supersedes all other development practices and guidelines. All code contributions, architectural decisions, and infrastructure changes MUST comply with these principles.

**Amendments**: Changes to this constitution require:

1. Documented rationale for the amendment
1. Impact assessment on existing code/infrastructure
1. Migration plan if breaking changes are introduced
1. Approval from project maintainers

**Enforcement**: All pull requests and code reviews MUST verify compliance with constitutional principles. Deviations MUST be explicitly justified and documented.

**Guidance**: For runtime development guidance and best practices, refer to `.github/copilot-instructions.md`.

**Version**: 1.0.0 | **Ratified**: 2025-11-16 | **Last Amended**: 2025-11-16
