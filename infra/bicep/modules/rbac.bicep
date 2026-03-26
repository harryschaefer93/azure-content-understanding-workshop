// Module: Cross-resource RBAC role assignment
// Grants the CU account's managed identity "Cognitive Services User" on the Models account
// for Entra ID cross-resource authentication.

@description('Principal ID of the CU account managed identity.')
param principalId string

@description('Resource ID of the Models account (scope for the role assignment).')
param modelsAccountId string

// Well-known role definition ID for "Cognitive Services User"
var cognitiveServicesUserRoleId = 'a97b65f3-24c7-4388-baec-2e87135dc908'

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(modelsAccountId, principalId, cognitiveServicesUserRoleId)
  scope: modelsAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', cognitiveServicesUserRoleId)
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

resource modelsAccount 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
  name: last(split(modelsAccountId, '/'))
}
