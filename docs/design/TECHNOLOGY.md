# Technology Overview

## Azure Services Overview

### Core Azure Services Deployed

- **Azure AI Foundry Service (Formerly known as Azure AI Services)**
  Provides access to Azure AI Services and Foundry based projects.

- **Azure AI Search** *(Optional, but recommended)*
  Enterprise-grade search service for AI-powered indexing and retrieval. Can be excluded from deployment if not required.


- **Azure Storage Account**
  Secure storage for application user data and assets.

- **Azure Key Vault**
  Securely manages secrets, keys, and certificates.

- **Azure Virtual Network (VNet) & Subnets**
  Provides network isolation and secure communication between resources.

- **Private Endpoints & Private DNS Zones**
  Ensures all services communicate privately within the VNet, supporting zero-trust architecture.

- **Azure Bastion (Optional)** *(Optional - when Network Isolation is enabled)*
  Secure RDP/SSH access to resources without exposing public IPs.

- **Azure Log Analytics Workspace**
  Centralized logging and monitoring for all deployed resources.

- **Azure Application Insights**
  Application performance monitoring and diagnostics.

### Infrastructure as Code

- **Bicep with Azure Verified Modules (AVM)**
  All resources are provisioned using Bicep and AVM for modular, secure, and repeatable deployments.

### Security & Identity

- **Managed Identities**
  Used for secure, passwordless authentication between Azure resources.

- **Role-Based Access Control (RBAC)**
  Fine-grained access management for users and service principals.

## Application Technology Stack

- **Backend:**
  - **.NET 10** & **Aspire.NET**
  - **Entity Framework Core** (Cosmos DB provider)
  - **IUserContextService** (stubbed; replace with Entra ID CIAM integration later)
  - **Repository layer** abstracting data operations.
  - **Stubbed identity** (via `IUserContextService`) to handle multi-user scenarios.
  - **Microsoft Agent Framework** for AI agent orchestration, tools, and capabilities.
  - **AG-UI Protocol** for agent-to-user interaction (native support in Microsoft Agent Framework).

- **Frontend:**
  - **React 19** with **TypeScript** (Vite)
  - **Redux Toolkit** for state management
  - **TailwindCSS** for utility-first styling [https://tailwindcss.com/](https://tailwindcss.com/)
  - **Shadcn/ui** component library built on Radix UI primitives [https://ui.shadcn.com/](https://ui.shadcn.com/). Documentation at [https://ui.shadcn.com/docs](https://ui.shadcn.com/docs)
  - **Radix UI** for accessible headless components [https://www.radix-ui.com/](https://www.radix-ui.com/)
  - **CopilotKit** for agentic user experiences and AG-UI client [https://docs.copilotkit.ai/](https://docs.copilotkit.ai/)
  - **AG-UI Protocol** for standardized agent-user communication [https://docs.ag-ui.com/](https://docs.ag-ui.com/)

- **Data Storage:**
  - **Azure Cosmos DB** using multiple containers for hierarchical, flexible entity storage and a dedicated container for document hierarchy.

- **Deployment & DevOps:**
  - Local development using the Cosmos DB Emulator
  - Dockerization for cloud deployment (Azure App Service or AKS)
  - CI/CD via GitHub Actions
