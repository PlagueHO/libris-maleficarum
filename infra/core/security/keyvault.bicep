metadata description = 'Creates an Azure Key Vault.'

@description('Name of the Azure Key Vault.')
param name string

@description('Location where the Azure Key Vault should be deployed.')
param location string = resourceGroup().location

@description('Tags to apply to the Azure Key Vault.')
param tags object = {}

@description('Allow the key vault to be used during resource creation (e.g., VM disk encryption).')
param enabledForDeployment bool = false

@description('Allow the key vault to be used for template deployment.')
param enabledForTemplateDeployment bool = false

@description('Allow Azure Disk Encryption to retrieve secrets from the vault.')
param enabledForDiskEncryption bool = false

@description('Enable or disable public network access to the Azure Key Vault.')
@allowed([
  'Enabled'
  'Disabled'
])
param publicNetworkAccess string = 'Enabled'

@description('The ID of the virtual network to which the Key Vault will be linked.')
param vnetId string = ''

@description('Soft delete retention period in days for the Azure Key Vault.')
param softDeleteRetentionInDays int = 90

@description('Enable or disable purge protection for the Azure Key Vault.')
param enablePurgeProtection bool = true

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enabledForDeployment: enabledForDeployment
    enabledForTemplateDeployment: enabledForTemplateDeployment
    enabledForDiskEncryption: enabledForDiskEncryption
    publicNetworkAccess: publicNetworkAccess
    enablePurgeProtection: enablePurgeProtection
    enableSoftDelete: true
    softDeleteRetentionInDays: softDeleteRetentionInDays
    enableRbacAuthorization: true
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Allow'
      ipRules: []
      virtualNetworkRules: [
        {
          id: vnetId
          ignoreMissingVnetServiceEndpoint: false
        }
      ]
    }
  }
}

output endpoint string = keyVault.properties.vaultUri
output id string = keyVault.id
output name string = keyVault.name
