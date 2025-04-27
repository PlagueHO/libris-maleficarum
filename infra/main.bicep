targetScope = 'subscription'

// The main bicep module to provision Azure resources.
// For a more complete walkthrough to understand how this file works with azd,
// see https://learn.microsoft.com/en-us/azure/developer/azure-developer-cli/make-azd-compatible?pivots=azd-create

@minLength(1)
@maxLength(64)
@description('Name of the the environment which is used to generate a short unique hash used in all resources.')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

// Optional parameters to override the default azd resource naming conventions.
// Add the following to main.parameters.json to provide values:
// "resourceGroupName": {
//      "value": "myGroupName"
// }
@description('Name of the resource group to create.')
param resourceGroupName string = ''

// Should an Azure Bastion be created?
@description('Should an Azure Bastion be created?')
param createBastionHost bool = false

@description('SKU for the Static Web App.')
param staticWebAppSku string = 'Standard'

var abbrs = loadJsonContent('./abbreviations.json')

// tags that should be applied to all resources.
var tags = {
  // Tag all resources with the environment name.
  'azd-env-name': environmentName
}

// Generate a unique token to be used in naming resources.
// Remove linter suppression after using.
#disable-next-line no-unused-vars
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))

var logAnalyticsName = '${abbrs.operationalInsightsWorkspaces}${environmentName}'
var applicationInsightsName = '${abbrs.insightsComponents}${environmentName}'
var virtualNetworkName = '${abbrs.networkVirtualNetworks}${environmentName}'
var storageAccounName = toLower(replace('${abbrs.storageStorageAccounts}${environmentName}', '-', ''))
var keyVaultName = toLower(replace('${abbrs.keyVaultVaults}${environmentName}', '-', ''))
var cosmosDbAccountName = toLower(replace('${abbrs.cosmosDBAccounts}${environmentName}', '-', ''))

var subnets = [
  {
    // Default subnet (generally not used)
    name: 'Default'
    addressPrefix: '10.0.0.0/24'
  }
  {
    // Azure Container App Services Subnet
    name: 'AppServices'
    addressPrefix: '10.0.1.0/24'
  }
  {
    // App Storage Subnet (storage acconts, databases etc.)
    name: 'AppStorage'
    addressPrefix: '10.0.2.0/24'
  }
  {
    // Azure AI Services Subnet (AI Search, AI Services, etc.)
    name: 'AiServices'
    addressPrefix: '10.0.3.0/24'
  }
  {
    // Shared Services Subnet (key vaults, monitoring, etc.)
    name: 'SharedServices'
    addressPrefix: '10.0.4.0/24'
  }
  {
    // Bastion Gateway Subnet
    name: 'AzureBastionSubnet'
    addressPrefix: '10.0.255.0/27'
  }
]

// Organize resources in a resource group
resource rg 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

// Create the Log Analytics workspace using Azure Verified Module (AVM)
module logAnalyticsWorkspace 'br/public:avm/res/operational-insights/workspace:0.11.1' = {
  name: 'logAnalyticsWorkspace'
  scope: rg
  params: {
    name: logAnalyticsName
    location: location
    tags: tags
  }
}

// Create the Application Insights resource using Azure Verified Module (AVM)
module applicationInsights 'br/public:avm/res/insights/component:0.6.0' = {
  name: 'applicationInsights'
  scope: rg
  params: {
    name: applicationInsightsName
    location: location
    tags: tags
    workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
  }
}

// Virtual Network to host all AI services and supporting resources
module virtualNetwork 'core/networking/virtual-network.bicep' = {
  name: 'virtual-network'
  scope: rg
  params: {
    name: virtualNetworkName
    location: location
    tags: tags
    addressPrefixes: [
      '10.0.0.0/16'
    ]
    subnets: subnets
  }
}

// Private DNS Zone for the Key Vault to be used by Private Link
module keyVaultPrivateDnsZone 'core/networking/private-dns-zone.bicep' = {
  name: 'keyvault-private-dns-zone'
  scope: rg
  params: {
    privateDnsZoneName: 'privatelink.vaultcore.azure.net'
    location: 'global'
    tags: tags
  }
}

// Create a Key Vault with private endpoint in the Shared Services subnet
module keyVault 'core/security/key-vault.bicep' = {
  name: 'key-vault'
  scope: rg
  params: {
    name: keyVaultName
    location: location
    tags: tags
    publicNetworkAccess: 'Disabled'
    enablePrivateEndpoint: true
    privateEndpointVnetName: virtualNetworkName
    privateEndpointSubnetName: 'SharedServices'
    privateEndpointName: '${keyVaultName}-pe'
  }
}

// Private DNS Zone for the storage accounts to be used by Private Link
module storagePrivateDnsZone 'core/networking/private-dns-zone.bicep' = {
  name: 'storage-blobservice-private-dns-zone'
  scope: rg
  params: {
    privateDnsZoneName: 'privatelink.blob.${environment().suffixes.storage}'
    location: 'global'
    tags: tags
  }
}

// Create a Storage Account with private endpoint in the SharedServices subnet
module storageAccount 'core/storage/storage-account.bicep' = {
  name: 'storage-account'
  scope: rg
  params: {
    name: storageAccounName
    location: location
    tags: tags
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    allowCrossTenantReplication: false
    allowSharedKeyAccess: true
    defaultToOAuthAuthentication: true
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    dnsEndpointType: 'Standard'
    isHnsEnabled: false
    kind: 'StorageV2'
    minimumTlsVersion: 'TLS1_2'
    publicNetworkAccess: 'Disabled'
    enablePrivateEndpoint: true
    privateEndpointVnetName: virtualNetworkName
    privateEndpointSubnetName: 'SharedServices'
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.outputs.resourceId
  }
}

// Create Private DNS Zone for the Cosmos DB account to be used by Private Link
module cosmosDbPrivateDnsZone 'core/networking/private-dns-zone.bicep' = {
  name: 'cosmosdb-private-dns-zone'
  scope: rg
  params: {
    privateDnsZoneName: 'privatelink.documents.azure.com'
    location: 'global'
    tags: tags
  }
}

// Create a Cosmos DB account with private endpoint in the AppStorage subnet
module cosmosDbAccount 'core/database/cosmos-account.bicep' = {
  name: 'cosmos-db-account'
  scope: rg
  params: {
    name: cosmosDbAccountName
    location: location
    tags: tags
    keyVaultName: keyVaultName
    kind: 'GlobalDocumentDB'
    enablePrivateEndpoint: true
    privateEndpointVnetName: virtualNetworkName
    privateEndpointSubnetName: 'AppStorage'
  }
}

// Create Private DNS Zone for Azure AI Search to be used by Private Link
module searchPrivateDnsZone 'core/networking/private-dns-zone.bicep' = {
  name: 'search-private-dns-zone'
  scope: rg
  params: {
    privateDnsZoneName: 'privatelink.search.windows.net'
    tags: tags
  }
}

// Create Azure AI Search service with private endpoint in the AiServices subnet
module aiSearchService 'core/search/ai-search-service.bicep' = {
  name: 'ai-search-service'
  scope: rg
  params: {
    name: '${abbrs.aiSearchSearchServices}${environmentName}'
    location: location
    tags: tags
    sku: {
      name: 'basic'
    }
    enablePrivateEndpoint: true
    privateEndpointVnetName: virtualNetworkName
    privateEndpointSubnetName: 'AiServices'
  }
}

// Create Private DNS Zone for Azure AI Services to be used by Private Link
module aiServicesPrivateDnsZone 'core/networking/private-dns-zone.bicep' = {
  name: 'aiservices-private-dns-zone'
  scope: rg
  params: {
    privateDnsZoneName: 'privatelink.cognitiveservices.azure.com'
    tags: tags
  }
}

// Create Azure AI Services instance with private endpoint in the AiServices subnet
module aiServices 'core/ai/ai-services.bicep' = {
  name: 'ai-services'
  scope: rg
  params: {
    name: '${abbrs.aiServicesAccounts}${environmentName}'
    location: location
    tags: tags
    enablePrivateEndpoint: true
    privateEndpointVnetName: virtualNetworkName
    privateEndpointSubnetName: 'AiServices'
  }
}

// Create a Static Web App for the application
module staticWebApp 'core/host/staticwebapp.bicep' = {
  name: 'static-web-app'
  scope: rg
  params: {
    name: '${abbrs.webStaticSites}${environmentName}'
    location: location
    tags: tags
    sku: {
      name: staticWebAppSku
    }
  }
}

// Optional: Create an Azure Bastion host in the virtual network.
module bastion 'core/networking/bastion-host.bicep' = if (createBastionHost) {
  name: 'bastion-host'
  scope: rg
  params: {
    name: '${abbrs.networkBastionHosts}${environmentName}'
    location: location
    tags: tags
    virtualNetworkId: virtualNetwork.outputs.virtualNetworkId
    publicIpName: '${abbrs.networkPublicIPAddresses}${abbrs.networkBastionHosts}${environmentName}'
    publicIpSku: 'Standard'
  }
}

@description('The Azure region where resources are deployed.')
output AZURE_LOCATION string = location

@description('The Azure Active Directory tenant ID.')
output AZURE_TENANT_ID string = tenant().tenantId

@description('The URI of the deployed static web app.')
output STATIC_WEB_APP_URI string = staticWebApp.outputs.uri
