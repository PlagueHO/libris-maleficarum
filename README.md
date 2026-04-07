# Libris Maleficarum

[![CI][ci-shield]][ci-url]
[![CD][cd-shield]][cd-url]
[![License][license-shield]][license-url]
[![Azure][azure-shield]][azure-url]
[![IaC][iac-shield]][iac-url]

> **Early Development** — This project is under active development. Features, APIs, and infrastructure are subject to change. Contributions and feedback are welcome.

## What is Libris Maleficarum?

**Libris Maleficarum** is an AI-enhanced campaign management and world-building platform for tabletop role-playing games (TTRPGs). It enables game masters and players to create, organize, and manage rich, interconnected campaign worlds with AI-powered assistance. I created this project to explore the intersection of AI, software architecture, and game design, while building a useful tool for helping me plan and run my own D&D campaigns.

### Target Users

- **Game Masters** — Create and manage campaign worlds, locations, characters, and lore with hierarchical organization and AI suggestions.
- **Content Creators** — Build reusable world templates and assets.
- **Players** — Access shared campaign content collaboratively (Future feature).

### Key Features

- **Hierarchical World Organization** — Unlimited nesting of world entities (World > Continent > Region > Country > City > Character, and more).
- **29 Entity Types** — Locations, people, events, factions, items, and custom types with schema-driven properties.
- **AI-Powered Assistance** — Conversational agentic interface for world-building powered by Microsoft Agent Framework and CopilotKit.
- **Full-text and Semantic Search** — Find entities by name or by concept using Azure AI Search.
- **Rich Asset Management** — Attach maps, portraits, session recordings, and documents to any entity.
- **Fantasy D&D Theme** — Royal blue and gold palette with dark/light mode, designed for the tabletop RPG experience.
- **Multi-System Support** — D&D 5e, Pathfinder, and custom systems via flexible entity schemas.

## Technology Stack

| Layer | Technologies |
|---|---|
| **Frontend** | React 19, TypeScript, Vite, Redux Toolkit, TailwindCSS v4, Shadcn/UI, CopilotKit |
| **Backend** | .NET 10, ASP.NET Core, EF Core (Cosmos DB), Microsoft Aspire Clients, Microsoft Agent Framework |
| **Infrastructure** | Azure Bicep (Azure Verified Modules), Cosmos DB, AI Search, Key Vault, Container Apps, Static Web App |
| **CI/CD** | GitHub Actions, Azure Developer CLI (`azd`) |
| **Testing** | Vitest, Testing Library, jest-axe, Playwright (frontend) · MSTest, FluentAssertions (backend) |

## Getting Started

### Prerequisites

- [Node.js 20+](https://nodejs.org/) with [pnpm](https://pnpm.io/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for Aspire orchestration and Cosmos DB Emulator)
- [Azure Developer CLI (`azd`)](https://aka.ms/install-azd) (for Azure deployment)
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) (optional, for manual Azure operations)
- [Aspire CLI](https://aka.ms/aspire) (for local orchestration)

### Run Locally with Aspire

The fastest way to run the full stack locally is with Microsoft Aspire, which orchestrates the frontend and backend services, Cosmos DB Emulator, and service discovery automatically.

1. **Clone the repository**

   ```powershell
   git clone https://github.com/PlagueHO/libris-maleficarum.git
   cd libris-maleficarum
   ```

1. **Start the entire application stack with Aspire**

   ```powershell
   aspire run
   ```

   This starts all services and starts the Aspire Dashboard for observability (logs, traces, metrics).

   The app is served at `https://127.0.0.1:4000` (the first run generates a self-signed SSL certificate — your browser will warn).

### Frontend-Only Development

If you only need the frontend (with mocked APIs via MSW):

```powershell
cd libris-maleficarum-app
pnpm install
pnpm dev
```

### Useful Commands

#### Frontend (`libris-maleficarum-app/`)

| Command | Description |
|---|---|
| `pnpm dev` | Start Vite dev server |
| `pnpm build` | TypeScript check + production build |
| `pnpm test` | Run tests (headless) |
| `pnpm test:ui` | Run tests with Vitest browser UI |
| `pnpm lint` | Lint with ESLint |
| `pnpm format` | Format with Prettier |

#### Backend (`libris-maleficarum-service/`)

| Command | Description |
|---|---|
| `dotnet run --project src/Orchestration/AppHost` | Start Aspire orchestration |
| `dotnet build LibrisMaleficarum.slnx` | Build all projects |
| `dotnet test LibrisMaleficarum.slnx` | Run all tests |
| `dotnet test --filter TestCategory=Unit` | Run unit tests only |
| `dotnet test --filter TestCategory=Integration` | Run integration tests only |

## Authentication and Access Control

The application supports two authentication modes and an optional access code gate. These can be used independently or together.

### Single-user mode (default)

When no Entra ID configuration is provided, the API runs in **single-user anonymous mode**. All requests are treated as a single anonymous user. No sign-in or bearer token is required. This is the default for local development.

### Multi-user mode (Entra ID)

When `AzureAd:ClientId` is configured, the API enables **Microsoft Entra ID** JWT bearer authentication. Users must sign in via MSAL and provide a valid bearer token with each request.

Configure the backend in `appsettings.json`:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "<your-client-id>",
    "Audience": "<your-audience>",
    "Scopes": "access_as_user"
  }
}
```

The frontend reads `ENTRA_CLIENT_ID` and `ENTRA_TENANT_ID` environment variables (injected by Aspire or set manually) to configure MSAL for interactive sign-in.

### Access code gate (optional)

The access code is an optional shared secret that protects all API endpoints. It works in both single-user and multi-user modes. When enabled, every request must include an `X-Access-Code` header. The frontend shows a dialog prompting for the code before allowing access.

Set the access code via environment variable or configuration:

```powershell
# Environment variable (recommended)
$env:ACCESS_CODE = "your-secret-code"
```

```json
// appsettings.Development.json (local dev only — do not commit secrets)
{
  "AccessControl": {
    "AccessCode": "your-secret-code"
  }
}
```

To disable, leave `AccessCode` empty or omit `ACCESS_CODE`. When no access code is configured, all requests pass through without restriction.

For Azure deployments, set the `ACCESS_CODE` secret in your GitHub repository settings. The CI/CD workflows pass it through to the infrastructure automatically.

## Deploy to Azure

This project uses the [Azure Developer CLI](https://learn.microsoft.com/azure/developer/azure-developer-cli/overview) to provision infrastructure and deploy the application.

1. **Authenticate**

   ```powershell
   azd auth login
   ```

1. **Initialize** (if you haven't cloned the repo)

   ```powershell
   azd init -t PlagueHO/libris-maleficarum
   ```

1. **Preview infrastructure changes**

   ```powershell
   azd provision --preview
   ```

1. **Provision and deploy**

   ```powershell
   azd up
   ```

1. **Deploy app only** (after initial provisioning)

   ```powershell
   azd deploy
   ```

### Delete the Deployment

```powershell
azd down --force --purge
```

> [!WARNING]
> This deletes all resources created during deployment, including any data. Back up important data before running this command.

## Architecture

The solution follows a Clean/Hexagonal architecture for the backend and a feature-based component structure for the frontend.

Infrastructure is deployed with Azure Bicep using [Azure Verified Modules](https://aka.ms/avm) and follows a network-isolated, zero-trust approach with private endpoints for all PaaS services.

For detailed design documentation, see the [docs/design/](docs/design/) folder:

- [Architecture Overview](docs/design/OVERVIEW.md)
- [Data Model](docs/design/DATA_MODEL.md)
- [Technology Choices](docs/design/TECHNOLOGY.md)
- [Frontend Design](docs/design/FRONTEND.md)
- [Backend Design](docs/design/BACKEND.md)
- [Infrastructure](docs/design/INFRASTRUCTURE.md)
- [CI/CD](docs/design/CI_CD.md)
- [Testing Strategy](docs/design/TESTING.md)

## Contributing

Contributions are welcome. Please open an issue to discuss proposed changes before submitting a pull request.

## License

This project is licensed under the [MIT License](LICENSE).

<!-- Badge reference links -->
[ci-shield]: https://img.shields.io/github/actions/workflow/status/PlagueHO/libris-maleficarum/continuous-integration.yml?branch=main&label=CI
[ci-url]: https://github.com/PlagueHO/libris-maleficarum/actions/workflows/continuous-integration.yml
[cd-shield]: https://img.shields.io/github/actions/workflow/status/PlagueHO/libris-maleficarum/continuous-delivery.yml?branch=main&label=CD
[cd-url]: https://github.com/PlagueHO/libris-maleficarum/actions/workflows/continuous-delivery.yml
[license-shield]: https://img.shields.io/github/license/PlagueHO/libris-maleficarum
[license-url]: https://github.com/PlagueHO/libris-maleficarum/blob/main/LICENSE
[azure-shield]: https://img.shields.io/badge/Azure-Solution%20Accelerator-0078D4?logo=microsoftazure&logoColor=white
[azure-url]: https://azure.microsoft.com/
[iac-shield]: https://img.shields.io/badge/Infrastructure%20as%20Code-Bicep-5C2D91?logo=azurepipelines&logoColor=white
[iac-url]: https://learn.microsoft.com/azure/azure-resource-manager/bicep/overview
