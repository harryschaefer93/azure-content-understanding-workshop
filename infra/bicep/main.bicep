// ====================================================================
// Azure AI Content Understanding — Infrastructure Deployment
// ====================================================================
//
// Deploys Azure AI Services account(s) with GPT and embedding model
// deployments for use with Content Understanding.
//
// Deployment modes:
//   Single-region (default):  One account hosts both CU and models.
//   Cross-region (optional):  Separate Models account in a second region
//                             with managed-identity RBAC.
//
// To enable cross-region, set modelsLocation to a region different
// from the primary location.
//
// ====================================================================

targetScope = 'resourceGroup'

// ====================================================================
// Parameters
// ====================================================================

@description('Resource naming prefix. Used to generate account names: {prefix}-cu and {prefix}-models.')
@minLength(2)
@maxLength(50)
param prefix string

@description('Primary Azure region for the Content Understanding account. Must support Azure AI Content Understanding.')
param location string = resourceGroup().location

@description('Optional: Azure region for model deployments. When set to a different region than location, enables cross-region mode with a separate Models account and managed-identity RBAC. Leave empty for single-region deployment.')
param modelsLocation string = ''

@description('SKU for AI Services accounts.')
param sku string = 'S0'

@description('Capacity (1K TPM units) for gpt-4.1 GlobalStandard deployment.')
param gpt41Capacity int = 100

@description('Capacity (1K TPM units) for gpt-4.1-mini GlobalStandard deployment.')
param gpt41MiniCapacity int = 100

@description('Capacity (1K TPM units) for gpt-4.1-mini Standard (region-guaranteed) deployment.')
param gpt41MiniStandardCapacity int = 100

@description('Capacity (1K TPM units) for gpt-4o Standard (region-guaranteed) deployment.')
param gpt4oStandardCapacity int = 100

@description('Capacity (1K TPM units) for text-embedding-ada-002 deployment.')
param embeddingAdaCapacity int = 50

@description('Capacity (1K TPM units) for text-embedding-3-large deployment.')
param embedding3LargeCapacity int = 50

@description('Capacity (1K TPM units) for text-embedding-3-small deployment.')
param embedding3SmallCapacity int = 50

@description('Tags to apply to all resources.')
param tags object = {}

// ====================================================================
// Variables
// ====================================================================

var crossRegion = !empty(modelsLocation) && modelsLocation != location
var cuAccountName = '${prefix}-cu'
var modelsAccountName = crossRegion ? '${prefix}-models' : cuAccountName
var effectiveModelsLocation = crossRegion ? modelsLocation : location

// ====================================================================
// Content Understanding AI Services Account (always created)
// ====================================================================

module cuAccount 'modules/ai-services.bicep' = {
  name: 'deploy-cu-account'
  params: {
    name: cuAccountName
    location: location
    sku: sku
    tags: tags
  }
}

// ====================================================================
// Models AI Services Account (cross-region only)
// ====================================================================

module modelsAccount 'modules/ai-services.bicep' = if (crossRegion) {
  name: 'deploy-models-account'
  params: {
    name: modelsAccountName
    location: effectiveModelsLocation
    sku: sku
    tags: tags
  }
}

// ====================================================================
// Model Deployments
// Deployed on the Models account (cross-region) or CU account (single-region).
// ====================================================================

module deployments 'modules/model-deployments.bicep' = {
  name: 'deploy-models'
  params: {
    accountName: crossRegion ? modelsAccount!.outputs.accountName : cuAccount.outputs.accountName
    gpt41Capacity: gpt41Capacity
    gpt41MiniCapacity: gpt41MiniCapacity
    gpt41MiniStandardCapacity: gpt41MiniStandardCapacity
    gpt4oStandardCapacity: gpt4oStandardCapacity
    embeddingAdaCapacity: embeddingAdaCapacity
    embedding3LargeCapacity: embedding3LargeCapacity
    embedding3SmallCapacity: embedding3SmallCapacity
  }
}

// ====================================================================
// RBAC: CU → Models (cross-region only)
// Grants the CU managed identity "Cognitive Services User" on the
// Models account for Entra ID cross-resource authentication.
// ====================================================================

module rbac 'modules/rbac.bicep' = if (crossRegion) {
  name: 'deploy-rbac'
  params: {
    principalId: cuAccount.outputs.principalId
    modelsAccountId: modelsAccount!.outputs.id
  }
}

// ====================================================================
// Outputs
// ====================================================================

@description('CU endpoint URL — use in notebooks, test harness, and curl commands.')
output cuEndpoint string = cuAccount.outputs.endpoint

@description('Resource ID of the Content Understanding account.')
output cuResourceId string = cuAccount.outputs.id

@description('Models endpoint — same as CU endpoint in single-region mode; separate in cross-region mode.')
output modelsEndpoint string = crossRegion ? modelsAccount!.outputs.endpoint : cuAccount.outputs.endpoint

@description('Resource ID of the account hosting model deployments.')
output modelsResourceId string = crossRegion ? modelsAccount!.outputs.id : cuAccount.outputs.id

@description('Connection name for defaults-body.json (account name without hyphens). For cross-region, this is the Models account name; for single-region, this is the CU account name.')
output modelsConnectionName string = replace(modelsAccountName, '-', '')
