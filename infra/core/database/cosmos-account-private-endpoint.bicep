metadata description = 'Creates a Private Endpoint for a Cosmos DB Account.'

@description('Specifies the name of the virtual network.')
param virtualNetworkName string

@description('Specifies the name of the subnet which contains the private endpoint.')
param subnetName string

@description('Specifies the name of the private endpoint to create.')
param cosmosAccountPrivateEndpointName string

@description('Specifies the resource id of the Azure Cosmos DB Account.')
param cosmosAccountId string

@description('Specifies the location.')
param location string = resourceGroup().location

@description('Specifies the resource tags.')
param tags object

// Virtual Network
resource vnet 'Microsoft.Network/virtualNetworks@2021-08-01' existing = {
  name: virtualNetworkName
}

// Private DNS Zones
resource cosmosAccountPrivateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' existing = {
  name: 'privatelink.documents.azure.com'
}

// Virtual Network Links
resource cosmosAccountPrivateDnsZoneVirtualNetworkLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2020-06-01' = {
  parent: cosmosAccountPrivateDnsZone
  name: 'link_to_${toLower(virtualNetworkName)}'
  location: 'global'
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: vnet.id
    }
  }
}

// Private Endpoints
resource cosmosAccountPrivateEndpoint 'Microsoft.Network/privateEndpoints@2021-08-01' = {
  name: cosmosAccountPrivateEndpointName
  location: location
  tags: tags
  properties: {
    subnet: {
      id: '${vnet.id}/subnets/${subnetName}'
    }
    privateLinkServiceConnections: [
      {
        name: cosmosAccountPrivateEndpointName
        properties: {
          privateLinkServiceId: cosmosAccountId
          groupIds: [
            'Sql'
          ]
        }
      }
    ]
  }
}

resource cosmosAccountPrivateDnsZoneGroupName 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2021-08-01' = {
  parent: cosmosAccountPrivateEndpoint
  name: 'PrivateDnsZoneGroupName'
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'dnsConfig'
        properties: {
          privateDnsZoneId: cosmosAccountPrivateDnsZone.id
        }
      }
    ]
  }
}

@description('The resource ID of the Cosmos DB private endpoint.')
output cosmosAccountPrivateEndpointId string = cosmosAccountPrivateEndpoint.id

@description('The resource ID of the Cosmos DB private DNS zone group.')
output cosmosAccountPrivateDnsZoneGroupId string = cosmosAccountPrivateDnsZoneGroupName.id

@description('The resource ID of the Cosmos DB private DNS zone virtual network link.')
output cosmosAccountPrivateDnsZoneVirtualNetworkLinkId string = cosmosAccountPrivateDnsZoneVirtualNetworkLink.id

@description('The resource ID of the Cosmos DB private DNS zone.')
output cosmosAccountPrivateDnsZoneId string = cosmosAccountPrivateDnsZone.id

@description('The name of the Cosmos DB private DNS zone.')
output cosmosAccountPrivateDnsZoneName string = cosmosAccountPrivateDnsZone.name

@description('The resource ID of the Cosmos DB private DNS zone.')
output cosmosAccountPrivateDnsZoneResourceId string = cosmosAccountPrivateDnsZone.id

@description('The name of the Cosmos DB private DNS zone virtual network link.')
output cosmosAccountPrivateDnsZoneVirtualNetworkLinkName string = cosmosAccountPrivateDnsZoneVirtualNetworkLink.name
