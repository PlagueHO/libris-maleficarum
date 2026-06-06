targetScope = 'resourceGroup'

@sys.description('Required. Display name for the workbook.')
param displayName string

@sys.description('Required. Azure region for the workbook resource.')
param location string

@sys.description('Required. Source resource ID for workbook queries (for example, an Application Insights component ID).')
param sourceId string

@sys.description('Optional. Description shown in the workbook metadata.')
param description string = 'Operational dashboard for SearchIndexWorker metrics.'

@sys.description('Optional. Tags to apply to the workbook resource.')
param tags object = {}

var workbookSchemaVersion = 'Notebook/1.0'
var workbookJson = string(loadJsonContent('search-index-worker.workbook.json'))

resource workbook 'Microsoft.Insights/workbooks@2023-06-01' = {
  name: guid(resourceGroup().id, 'Microsoft.Insights/workbooks', displayName)
  location: location
  kind: 'shared'
  tags: tags
  properties: {
    category: 'workbook'
    description: description
    displayName: displayName
    serializedData: workbookJson
    sourceId: sourceId
    version: workbookSchemaVersion
  }
}

@sys.description('The resource ID of the workbook.')
output resourceId string = workbook.id
