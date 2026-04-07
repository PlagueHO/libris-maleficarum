using './main.bicep'

// Environment Configuration
param environmentName = readEnvironmentVariable('AZURE_ENV_NAME', 'azdtemp')
param location = readEnvironmentVariable('AZURE_LOCATION', 'EastUS2')

// Resource Group Configuration
param resourceGroupName = readEnvironmentVariable('AZURE_RESOURCE_GROUP', '')

// Optional Components
param createBastionHost = bool(readEnvironmentVariable('AZURE_CREATE_BASTION_HOST', 'false'))

// Static Web App location override (must be one of: centralus, eastasia, eastus2, westeurope, westus2)
// Leave empty to use the primary location.
param staticWebAppLocation = toLower(readEnvironmentVariable('AZURE_STATIC_WEB_APP_LOCATION', ''))

// Optional access code for API protection in single-user mode
param accessCode = readEnvironmentVariable('ACCESS_CODE', '')
