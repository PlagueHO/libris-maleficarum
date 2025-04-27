metadata description = 'Creates an Azure Cosmos DB account.'

@description('Name of the Azure Cosmos DB account.')
param name string

@description('Location where the Azure Cosmos DB account should be deployed.')
param location string = resourceGroup().location

@description('Tags to apply to the Azure Cosmos DB account.')
param tags object = {}

@description('The key name to use for the Cosmos DB connection string in Key Vault.')
param connectionStringKey string = 'AZURE-COSMOS-CONNECTION-STRING'

@description('Name of the Azure Key Vault to store the Cosmos DB connection string.')
param keyVaultName string

@description('The API kind for the Cosmos DB account.')
@allowed(['GlobalDocumentDB', 'MongoDB', 'Parse'])
param kind string

@description('Flag indicating whether to create a private endpoint for the Cosmos DB account.')
param enablePrivateEndpoint bool = false

@description('The name of the virtual network where the private endpoint will be created.')
param privateEndpointVnetName string = ''

@description('The name of the subnet where the private endpoint will be created.')
param privateEndpointSubnetName string = ''

@description('The name of the private endpoint resource.')
param privateEndpointName string = '${name}-pe'

resource cosmos 'Microsoft.DocumentDB/databaseAccounts@2023-11-15' = {
  name: name
  kind: kind
  location: location
  tags: tags
  properties: {
    consistencyPolicy: { defaultConsistencyLevel: 'Session' }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    databaseAccountOfferType: 'Standard'
    enableAutomaticFailover: false
    enableMultipleWriteLocations: false
    apiProperties: (kind == 'MongoDB') ? { serverVersion: '4.2' } : {}
    capabilities: [{ name: 'EnableServerless' }]
    minimalTlsVersion: 'Tls12'
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' existing = {
  name: keyVaultName
}

resource cosmosConnectionString 'Microsoft.KeyVault/vaults/secrets@2022-07-01' = {
  parent: keyVault
  name: connectionStringKey
  properties: {
    value: cosmos.listConnectionStrings().connectionStrings[0].connectionString
  }
}

// Enable Cosmos DB private endpoint if specified
module cosmosPrivateEndpoint 'cosmos-account-private-endpoint.bicep' = if (enablePrivateEndpoint) {
  name: privateEndpointName
  scope: resourceGroup()
  params: {
    virtualNetworkName: privateEndpointVnetName
    subnetName: privateEndpointSubnetName
    cosmosAccountPrivateEndpointName: privateEndpointName
    cosmosAccountId: cosmos.id
    location: location
    tags: tags
  }
}

@description('The key name used for the Cosmos DB connection string in Key Vault.')
output connectionStringKey string = connectionStringKey

@description('The endpoint URI of the Cosmos DB account.')
output endpoint string = cosmos.properties.documentEndpoint

@description('The resource ID of the Cosmos DB account.')
output id string = cosmos.id

@description('The name of the Cosmos DB account.')
output name string = cosmos.name

@description('The resource ID of the Cosmos DB private endpoint, if enabled.')
output privateEndpointId string = enablePrivateEndpoint
  ? cosmosPrivateEndpoint.outputs.cosmosAccountPrivateEndpointId
  : ''

@description('The resource ID of the Cosmos DB private DNS zone group, if enabled.')
output privateDnsZoneGroupId string = enablePrivateEndpoint
  ? cosmosPrivateEndpoint.outputs.cosmosAccountPrivateDnsZoneGroupId
  : ''

@description('The resource ID of the Cosmos DB private DNS zone virtual network link, if enabled.')
output privateDnsZoneVirtualNetworkLinkId string = enablePrivateEndpoint
  ? cosmosPrivateEndpoint.outputs.cosmosAccountPrivateDnsZoneVirtualNetworkLinkId
  : ''

@description('The resource ID of the Cosmos DB private DNS zone, if enabled.')
output privateDnsZoneId string = enablePrivateEndpoint
  ? cosmosPrivateEndpoint.outputs.cosmosAccountPrivateDnsZoneId
  : ''

@description('The name of the Cosmos DB private DNS zone, if enabled.')
output privateDnsZoneName string = enablePrivateEndpoint
  ? cosmosPrivateEndpoint.outputs.cosmosAccountPrivateDnsZoneName
  : ''

@description('The resource ID of the Cosmos DB private DNS zone, if enabled.')
output privateDnsZoneResourceId string = enablePrivateEndpoint
  ? cosmosPrivateEndpoint.outputs.cosmosAccountPrivateDnsZoneResourceId
  : ''

@description('The name of the Cosmos DB private DNS zone virtual network link, if enabled.')
output privateDnsZoneVirtualNetworkLinkName string = enablePrivateEndpoint
  ? cosmosPrivateEndpoint.outputs.cosmosAccountPrivateDnsZoneVirtualNetworkLinkName
  : ''
