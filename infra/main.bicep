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
// Add the following to main.bicepparam to provide values:
// param resourceGroupName = readEnvironmentVariable('AZURE_RESOURCE_GROUP', 'myGroupName')
//
@description('Name of the resource group to create.')
param resourceGroupName string = ''

// Should an Azure Bastion be created?
@description('Should an Azure Bastion be created?')
param createBastionHost bool = false

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
var sendTologAnalyticsCustomSettingName = 'send-to-${logAnalyticsName}'
var applicationInsightsName = '${abbrs.insightsComponents}${environmentName}'
var virtualNetworkName = '${abbrs.networkVirtualNetworks}${environmentName}'
var storageAccounName = toLower(replace('${abbrs.storageStorageAccounts}${environmentName}', '-', ''))
var keyVaultName = toLower(replace('${abbrs.keyVaultVaults}${environmentName}', '-', ''))
var cosmosDbAccountName = toLower(replace('${abbrs.cosmosDBAccounts}${environmentName}', '-', ''))
var aiSearchName = '${abbrs.aiSearchSearchServices}${environmentName}'
var aiServicesName = '${abbrs.aiServicesAccounts}${environmentName}'
var aiServicesCustomSubDomainName = toLower(replace(environmentName, '-', ''))
var staticSiteName = toLower(replace('${abbrs.webStaticSites}${environmentName}', '-', ''))
var bastionHostName = '${abbrs.networkBastionHosts}${environmentName}'

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
    // Shared Services Subnet (key vaults, etc.)
    name: 'SharedServices'
    addressPrefix: '10.0.4.0/24'
  }
  {
    // Bastion Gateway Subnet
    name: 'AzureBastionSubnet'
    addressPrefix: '10.0.255.0/27'
  }
]

// Organize resources in a resource group using Azure Verified Module (AVM)
module rg 'br/public:avm/res/resources/resource-group:0.4.1' = {
  name: 'resource-group-deployment'
  scope: subscription()
  params: {
    name: !empty(resourceGroupName) ? resourceGroupName : '${abbrs.resourcesResourceGroups}${environmentName}'
    location: location
    tags: tags
  }
}

// Create the Log Analytics workspace using Azure Verified Module (AVM)
module logAnalyticsWorkspace 'br/public:avm/res/operational-insights/workspace:0.11.2' = {
  name: 'logAnalytics-workspace-deployment'
  scope: resourceGroup(rg.name)
  params: {
    name: logAnalyticsName
    location: location
    tags: tags
  }
}

// Create the Application Insights resource using Azure Verified Module (AVM)
module applicationInsights 'br/public:avm/res/insights/component:0.6.0' = {
  name: 'application-insights-deployment'
  scope: resourceGroup(rg.name)
  params: {
    name: applicationInsightsName
    location: location
    tags: tags
    workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
  }
}

// Create the Virtual Network and subnets using Azure Verified Modules (AVM)
module virtualNetwork 'br/public:avm/res/network/virtual-network:0.7.0' = {
  name: 'virtual-network-deployment'
  scope: resourceGroup(rg.name)
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

// Create the Private DNS Zone for the Key Vault to be used by Private Link using Azure Verified Module (AVM)
module keyVaultPrivateDnsZone 'br/public:avm/res/network/private-dns-zone:0.7.1' = {
  name: 'keyvault-private-dns-zone-deployment'
  scope: resourceGroup(rg.name)
  params: {
    name: 'privatelink.vaultcore.azure.net'
    location: 'global'
    tags: tags
  }
}

// Create a Key Vault with private endpoint in the Shared Services subnet using Azure Verified Module (AVM)
module keyVault 'br/public:avm/res/key-vault/vault:0.13.0' = {
  name: 'keyvault-deployment'
  scope: resourceGroup(rg.name)
  params: {
    name: keyVaultName
    location: location
    tags: tags
    diagnosticSettings: [
      {
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
      }
    ]
    enablePurgeProtection: false
    enableRbacAuthorization: true
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Deny'
    }
    privateEndpoints: [
      {
        privateDnsZoneGroup: {
          privateDnsZoneGroupConfigs: [
            {
              privateDnsZoneResourceId: keyVaultPrivateDnsZone.outputs.resourceId
            }
          ]
        }
        service: 'vault'
        subnetResourceId: virtualNetwork.outputs.subnetResourceIds[4]
      }
    ]
  }
}

// Create Private DNS Zone for the Storage Account blob service to be used by Private Link using Azure Verified Module (AVM)
module storageBlobPrivateDnsZone 'br/public:avm/res/network/private-dns-zone:0.7.1' = {
  name: 'storage-blobservice-private-dns-zone-deployment'
  scope: resourceGroup(rg.name)
  params: {
    name: 'privatelink.blob.${environment().suffixes.storage}'
    location: 'global'
    tags: tags
  }
}

// Create a Storage Account with private endpoint in the AppStorage subnet using Azure Verified Module (AVM)
module storageAccount 'br/public:avm/res/storage/storage-account:0.20.0' = {
  name: 'storage-account-deployment'
  scope: resourceGroup(rg.name)
  params: {
    name: storageAccounName
    allowBlobPublicAccess: false
    blobServices: {
      automaticSnapshotPolicyEnabled: false
      containerDeleteRetentionPolicyEnabled: false
      deleteRetentionPolicyEnabled: false
      lastAccessTimeTrackingPolicyEnabled: true
    }
    diagnosticSettings: [
      {
        metricCategories: [
          {
            category: 'AllMetrics'
          }
        ]
        name: sendTologAnalyticsCustomSettingName
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
      }
    ]
    enableHierarchicalNamespace: false
    enableNfsV3: false
    enableSftp: false
    largeFileSharesState: 'Enabled'
    location: location
    managedIdentities: {
      systemAssigned: true
    }
    privateEndpoints: [
      {
        privateDnsZoneGroup: {
          privateDnsZoneGroupConfigs: [
            {
              privateDnsZoneResourceId: storageBlobPrivateDnsZone.outputs.resourceId
            }
          ]
        }
        service: 'blob'
        subnetResourceId: virtualNetwork.outputs.subnetResourceIds[2]
        tags: tags
      }
    ]
    sasExpirationPeriod: '180.00:00:00'
    skuName: 'Standard_LRS'
    tags: tags
  }
}

// Create Private DNS Zone for the Cosmos DB account to be used by Private Link using Azure Verified Module (AVM)
module cosmosDbPrivateDnsZone 'br/public:avm/res/network/private-dns-zone:0.7.1' = {
  name: 'cosmosdb-private-dns-zone-deployment'
  scope: resourceGroup(rg.name)
  params: {
    name: 'privatelink.documents.azure.com'
    location: 'global'
    tags: tags
  }
}

// Create a Cosmos DB account with private endpoint in the AppStorage subnet using Azure Verified Module (AVM)

module cosmosDbAccount 'br/public:avm/res/document-db/database-account:0.15.0' = {
  name: 'cosmos-db-account-deployment'
  scope: resourceGroup(rg.name)
  params: {
    name: cosmosDbAccountName
    location: location
    tags: tags
    failoverLocations: [
      {
        failoverPriority: 0
        isZoneRedundant: false
        locationName: location
      }
    ]
    automaticFailover: false
    capabilitiesToAdd: [
      'EnableServerless'
    ]
    databaseAccountOfferType: 'Standard'
    diagnosticSettings: [
      {
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
      }
    ]
    disableKeyBasedMetadataWriteAccess: true
    disableLocalAuthentication: true
    minimumTlsVersion: 'Tls12'
    networkRestrictions: {
      networkAclBypass: 'None'
      publicNetworkAccess: 'Disabled'
    }
    privateEndpoints: [
      {
        privateDnsZoneGroup: {
          privateDnsZoneGroupConfigs: [
            {
              privateDnsZoneResourceId: cosmosDbPrivateDnsZone.outputs.resourceId
            }
          ]
        }
        service: 'Sql'
        subnetResourceId: virtualNetwork.outputs.subnetResourceIds[2]
      }
    ]
    backupStorageRedundancy: 'Local'
    sqlDatabases: [
      {
        name: 'no-containers-specified'
      }
    ]
  }
}

// Create Private DNS Zone for Azure AI Search to be used by Private Link using Azure Verified Module (AVM)
module aiSearchPrivateDnsZone 'br/public:avm/res/network/private-dns-zone:0.7.1' = {
  name: 'ai-search-private-dns-zone'
  scope: resourceGroup(rg.name)
  params: {
    name: 'privatelink.search.windows.net'
    location: 'global'
    tags: tags
  }
}

// Create Azure AI Search service with private endpoint in the AiServices subnet using Azure Verified Module (AVM)
module aiSearchService 'br/public:avm/res/search/search-service:0.10.0' = {
  name: 'ai-search-service-deployment'
  scope: resourceGroup(rg.name)
  params: {
    name: aiSearchName
    location: location
    sku: 'standard'
    semanticSearch: 'standard'
    diagnosticSettings: [
      {
        metricCategories: [
          {
            category: 'AllMetrics'
          }
        ]
        name: sendTologAnalyticsCustomSettingName
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
      }
    ]
    privateEndpoints: [
      {
        privateDnsZoneGroup: {
          privateDnsZoneGroupConfigs: [
            {
              privateDnsZoneResourceId: aiSearchPrivateDnsZone.outputs.resourceId
            }
          ]
        }
        subnetResourceId: virtualNetwork.outputs.subnetResourceIds[3]
        tags: tags
      }
    ]
    publicNetworkAccess: 'Disabled'
    tags: tags
  }
}

// Create Private DNS Zone for Azure AI Services to be used by Private Link using Azure Verified Module (AVM)
module aiServicesPrivateDnsZone 'br/public:avm/res/network/private-dns-zone:0.7.1' = {
  name: 'ai-services-private-dns-zone'
  scope: resourceGroup(rg.name)
  params: {
    name: 'privatelink.cognitiveservices.azure.com'
    location: 'global'
    tags: tags
  }
}

// Create Azure AI Services instance with private endpoint in the AiServices subnet using Azure Verified Module (AVM)
module aiServicesAccount 'br/public:avm/res/cognitive-services/account:0.11.0' = {
  name: 'ai-services-account-deployment'
  scope: resourceGroup(rg.name)
  params: {
    kind: 'AIServices'
    name: aiServicesName
    location: location
    customSubDomainName: aiServicesCustomSubDomainName
    sku: 'S0'
    diagnosticSettings: [
      {
        workspaceResourceId: logAnalyticsWorkspace.outputs.resourceId
      }
    ]
    privateEndpoints: [
      {
        privateDnsZoneGroup: {
          privateDnsZoneGroupConfigs: [
            {
              privateDnsZoneResourceId: aiServicesPrivateDnsZone.outputs.resourceId
            }
          ]
        }
        subnetResourceId: virtualNetwork.outputs.subnetResourceIds[3]
        tags: tags
      }
    ]
    publicNetworkAccess: 'Disabled'
  }
}

// Create a Static Web App for the application using Azure Verified Module (AVM)
module staticSite 'br/public:avm/res/web/static-site:0.9.0' = {
  name: 'static-site-deployment'
  scope: resourceGroup(rg.name)
  params: {
    name: staticSiteName
    location: location
    allowConfigFileUpdates: true
    enterpriseGradeCdnStatus: 'Disabled'
    sku: 'Standard'
    stagingEnvironmentPolicy: 'Enabled'
    tags: tags
  }
}

// Optional: Create an Azure Bastion host in the virtual network using Azure Verified Module (AVM)
module bastionHost 'br/public:avm/res/network/bastion-host:0.6.1' = if (createBastionHost) {
  name: 'bastion-host-deployment'
  scope: resourceGroup(rg.name)
  params: {
    name: bastionHostName
    location: location
    virtualNetworkResourceId: virtualNetwork.outputs.resourceId
    skuName: 'Developer'
    tags: tags
  }
}

@description('The Azure region where resources are deployed.')
output AZURE_LOCATION string = location

@description('The name of the resource group.')
output AZURE_RESOURCE_GROUP string = rg.outputs.name

@description('The Azure Active Directory tenant ID.')
output AZURE_TENANT_ID string = tenant().tenantId

@description('The URI of the deployed static web app.')
output STATIC_WEB_APP_URI string = staticSite.outputs.defaultHostname
