using './main.bicep'

// Environment Configuration
param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'azdtemp')
param location = readEnvironmentVariable('AZURE_LOCATION', 'EastUS2')

// Resource Group Configuration
param resourceGroupName = readEnvironmentVariable('AZURE_RESOURCE_GROUP', '')

// Optional Components
param createBastionHost = bool(readEnvironmentVariable('AZURE_CREATE_BASTION_HOST', 'false'))
