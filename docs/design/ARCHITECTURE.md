# Architecture

This document describes the Azure resources deployed by [infra/main.bicep](../infra/main.bicep), the Azure Verified Modules (AVM) used, and the network topology.

The Libris Maleficarum solution uses a **network-isolated architecture** where all Azure services are deployed with private endpoints and network security groups to ensure secure communication within a Virtual Network.

## Network Architecture

The solution uses a hub-and-spoke network architecture with the following subnets:

| Subnet | Address Space | Purpose | Resources |
|--------|---------------|---------|-----------|
| frontend | 10.0.1.0/24 | Frontend applications and static web apps | Static Web Apps, Container Apps Environment, Application Gateways |
| backend | 10.0.2.0/24 | Backend services and databases | Storage Accounts, Cosmos DB, Private Endpoints |
| gateway | 10.0.3.0/24 | Application gateways and load balancers | Application Gateway, Load Balancers |
| shared | 10.0.4.0/24 | Shared services and AI services | Key Vault, AI Search, AI Services, Private Endpoints |
| AzureBastionSubnet | 10.0.255.0/27 | Azure Bastion (optional) | Azure Bastion Host |

### Network Security

Each subnet is protected by dedicated Network Security Groups (NSGs) with specific security rules:

- **Frontend NSG**:
  - Allows HTTP (port 80) and HTTPS (port 443) traffic from the internet
  - Allows Container Apps management traffic (port 5671) from AzureContainerApps service tag
  - Allows health check traffic (port 9000) from Azure Load Balancer
- **Backend NSG**:
  - Allows VNet-to-VNet traffic for internal communication
  - Denies all traffic from the internet (priority 4000)
- **Gateway NSG**:
  - Allows Gateway Manager traffic (ports 65200-65535) for application gateway management
  - Allows HTTPS traffic (port 443) from the internet
- **Shared NSG**:
  - Allows VNet-to-VNet traffic for internal service communication
  - Denies all traffic from the internet (priority 4000)

### Private Endpoints and DNS

All Azure services are deployed with Private Endpoints for secure communication:

- **Key Vault**: Private endpoint in shared subnet
- **Storage Account**: Private endpoint in backend subnet  
- **Cosmos DB**: Private endpoint in backend subnet
- **AI Search**: Private endpoint in shared subnet
- **AI Services**: Private endpoint in shared subnet

Private DNS Zones are automatically linked to the Virtual Network for proper DNS resolution.

## Core Resources

The following resources are always deployed as part of the Libris Maleficarum architecture:

### Infrastructure and Networking

| Resource | Purpose | AVM Reference |
|----------|---------|---------------|
| Resource Group | Container for all resources | [avm/res/resources/resource-group](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/resources/resource-group) |
| Virtual Network | Network isolation with 10.0.0.0/16 address space | [avm/res/network/virtual-network](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/network/virtual-network) |
| Network Security Groups | Security policies for each subnet | [avm/res/network/network-security-group](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/network/network-security-group) |
| Private DNS Zones | DNS resolution for private endpoints | [avm/res/network/private-dns-zone](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/network/private-dns-zone) |

### Monitoring and Observability

| Resource | Purpose | AVM Reference |
|----------|---------|---------------|
| Log Analytics Workspace | Centralized logging and monitoring | [avm/res/operational-insights/workspace](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/operational-insights/workspace) |
| Application Insights | Application performance monitoring | [avm/res/insights/component](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/insights/component) |

### Storage and Data

| Resource | Purpose | AVM Reference |
|----------|---------|---------------|
| Storage Account | Blob storage with private endpoint | [avm/res/storage/storage-account](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/storage/storage-account) |
| Cosmos DB | NoSQL database with private endpoint | [avm/res/document-db/database-account](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/document-db/database-account) |
| Key Vault | Secure secret management with private endpoint | [avm/res/key-vault/vault](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/key-vault/vault) |

### AI and Cognitive Services

| Resource | Purpose | AVM Reference |
|----------|---------|---------------|
| Azure AI Services | Multi-service AI capabilities with private endpoint | [avm/res/cognitive-services/account](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/cognitive-services/account) |
| Azure AI Search | Search and indexing with private endpoint | [avm/res/search/search-service](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/search/search-service) |

### Application Hosting

| Resource | Purpose | AVM Reference |
|----------|---------|---------------|
| Container Apps Environment | Managed environment for containerized applications | [avm/res/app/managed-environment](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/app/managed-environment) |
| Static Web App | Frontend hosting for web applications | [avm/res/web/static-site](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/web/static-site) |

## Optional Resources

| Resource | When Deployed | Purpose | AVM Reference |
|----------|---------------|---------|---------------|
| Azure Bastion | `createBastionHost=true` | Secure remote access to VMs | [avm/res/network/bastion-host](https://github.com/Azure/bicep-registry-modules/tree/main/avm/res/network/bastion-host) |

## Deployment Architecture

The Libris Maleficarum solution follows a zero-trust network architecture with the following characteristics:

### Security Features

1. **Network Isolation** – All Azure PaaS services use private endpoints and disable public access
1. **Centralized Logging** – Diagnostic settings forward metrics/logs to Log Analytics
1. **Tagging** – Every resource inherits the `azd-env-name` tag for traceability
1. **Azure Verified Modules** – All resources are deployed using [Azure Verified Modules (AVM)](https://aka.ms/avm)
1. **Zero Trust** – Architecture follows Microsoft's Zero Trust security model and Secure Future Initiative

### Configuration Parameters

The solution supports the following configuration parameters:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `environmentName` | string | *required* | Name used to generate unique resource names |
| `location` | string | *required* | Azure region for all resources |
| `resourceGroupName` | string | `''` | Optional custom resource group name |
| `createBastionHost` | bool | `false` | Whether to deploy Azure Bastion for secure access |

```bash
azd env set AZURE_AI_FOUNDRY_HUB_DEPLOY true
azd env set AZURE_AI_FOUNDRY_HUB_PROJECT_DEPLOY true
azd env set AZURE_NETWORK_ISOLATION true
```

**Result**: AI Services + Hub + Hub projects, private endpoints, full ML capabilities.

### Hybrid Configuration

```bash
azd env set AZURE_AI_FOUNDRY_HUB_DEPLOY true
azd env set AZURE_AI_FOUNDRY_HUB_PROJECT_DEPLOY false
azd env set AZURE_AI_FOUNDRY_PROJECT_DEPLOY true
azd env set AZURE_NETWORK_ISOLATION true
```

**Result**: AI Services + Hub + AI Services projects, private endpoints.

## Security & Best Practices

1. **Managed Identities** – API key authentication can be disabled. All resources use managed identities to authenticate to other Azure services.
1. **Centralized Logging** – Diagnostic settings forward metrics/logs to Log Analytics.
1. **Tagging** – Every resource inherits the `azd-env-name` tag for traceability.
1. **Azure Verified Modules** – All resources are deployed using [Azure Verified Modules (AVM)](https://aka.ms/avm).
1. **Network Isolation** – When enabled, all PaaS services use private endpoints and disable public access.
1. **Zero Trust** – Network isolation deployment follows Microsoft's Zero Trust security model and Secure Future Initiative.
1. **Flexible Architecture** – Choose between simple AI Services-only deployment or full Hub capabilities based on requirements.

## Deployment Outputs

The Bicep template provides the following outputs that can be used by applications or other infrastructure components:

| Output | Description | Use Case |
|--------|-------------|----------|
| `AZURE_LOCATION` | Azure region where resources are deployed | Application configuration |
| `AZURE_RESOURCE_GROUP` | Name of the resource group | Resource management |
| `AZURE_TENANT_ID` | Azure AD tenant ID | Authentication configuration |
| `STATIC_WEB_APP_URI` | URI of the deployed static web app | Frontend access |
| `CONTAINER_APPS_ENVIRONMENT_ID` | Resource ID of the Container Apps Environment | Container app deployments |
| `CONTAINER_APPS_ENVIRONMENT_NAME` | Name of the Container Apps Environment | Application configuration |
| `CONTAINER_APPS_ENVIRONMENT_DEFAULT_DOMAIN` | Default domain for container apps | DNS configuration |

## Private DNS Zones

The following private DNS zones are automatically created and linked to the Virtual Network:

| Service | Private DNS Zone | Purpose |
|---------|------------------|---------|
| Key Vault | `privatelink.vaultcore.azure.net` | Key Vault private endpoint resolution |
| Storage Account | `privatelink.blob.core.windows.net` | Blob storage private endpoint resolution |
| Cosmos DB | `privatelink.documents.azure.com` | Cosmos DB private endpoint resolution |
| AI Search | `privatelink.search.windows.net` | AI Search private endpoint resolution |
| AI Services | `privatelink.cognitiveservices.azure.com` | AI Services private endpoint resolution |
