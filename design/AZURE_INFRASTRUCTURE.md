# Azure Infrastructure

Libris Maleficarum leverages Azure cloud services for scalability, security, and extensibility.

- **Frontend Hosting:** Azure Static Web Apps, optionally Azure Front Door.
- **Backend APIs:** Azure Container Apps (ACA) hosting .NET 8 APIs (Aspire.NET).
- **Data Storage:** Azure Cosmos DB (multiple containers), Azure Storage (Blob Storage).
- **AI & Search Services:** Azure AI Search, Azure AI Services.
- **Messaging & Eventing:** Azure Service Bus.
- **Security & Secrets Management:** Azure Key Vault.
- **Monitoring & Observability:** Azure Application Insights, Log Analytics.

All backend communications from the frontend and external services are routed exclusively through the API layer hosted in Azure Container Apps.

## Azure Architecture Diagram

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

## Infrastructure as Code (IaC)

- **Bicep Templates Location:** `infra/`
- **Deployment:** Automated via GitHub Actions workflows stored in the `.github` folder.
- **Secrets Management:** Secrets and sensitive configuration values securely managed via **Azure Key Vault**.
- **Monitoring & Observability:** Integrated with **Azure Application Insights** and **Log Analytics** for comprehensive monitoring and diagnostics.
