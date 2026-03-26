// Reusable module: Azure AI Services account
// Used for both the Content Understanding account and the optional cross-region Models account.

@description('Globally unique name for the AI Services account (2-64 chars).')
@minLength(2)
@maxLength(64)
param name string

@description('Azure region for the account.')
param location string

@description('SKU name.')
param sku string = 'S0'

@description('Tags to apply.')
param tags object = {}

resource account 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: name
  location: location
  kind: 'AIServices'
  sku: {
    name: sku
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    customSubDomainName: name
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
  }
  tags: tags
}

@description('Endpoint URL of the AI Services account.')
output endpoint string = account.properties.endpoint

@description('Resource ID of the AI Services account.')
output id string = account.id

@description('Principal ID of the system-assigned managed identity.')
output principalId string = account.identity.principalId

@description('Account name (for use in deployment references).')
output accountName string = account.name
