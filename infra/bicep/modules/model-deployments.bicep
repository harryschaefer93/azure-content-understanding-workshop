// Module: Model deployments on an AI Services account
// Deploys GPT and embedding models in a serial chain (ARM concurrency limitation).

@description('Name of the AI Services account to deploy models on.')
param accountName string

@description('Capacity (1K TPM units) for gpt-4.1 GlobalStandard deployment.')
param gpt41Capacity int = 100

@description('Capacity (1K TPM units) for gpt-4.1-mini GlobalStandard deployment.')
param gpt41MiniCapacity int = 100

@description('Capacity (1K TPM units) for gpt-4.1-mini Standard deployment.')
param gpt41MiniStandardCapacity int = 100

@description('Capacity (1K TPM units) for gpt-4o Standard deployment.')
param gpt4oStandardCapacity int = 100

@description('Capacity (1K TPM units) for text-embedding-ada-002 Standard deployment.')
param embeddingAdaCapacity int = 50

@description('Capacity (1K TPM units) for text-embedding-3-large Standard deployment.')
param embedding3LargeCapacity int = 50

@description('Capacity (1K TPM units) for text-embedding-3-small Standard deployment.')
param embedding3SmallCapacity int = 50

resource account 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
  name: accountName
}

// Serial deployment chain to avoid ARM concurrency issues

resource gpt41 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: account
  name: 'gpt-41'
  sku: {
    name: 'GlobalStandard'
    capacity: gpt41Capacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4.1'
    }
  }
}

resource gpt41Mini 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: account
  name: 'gpt-41-mini'
  dependsOn: [gpt41]
  sku: {
    name: 'GlobalStandard'
    capacity: gpt41MiniCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4.1-mini'
    }
  }
}

resource gpt41MiniStd 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: account
  name: 'gpt-41-mini-std'
  dependsOn: [gpt41Mini]
  sku: {
    name: 'Standard'
    capacity: gpt41MiniStandardCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4.1-mini'
    }
  }
}

resource gpt4oStd 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: account
  name: 'gpt-4o-std'
  dependsOn: [gpt41MiniStd]
  sku: {
    name: 'Standard'
    capacity: gpt4oStandardCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-11-20'
    }
  }
}

resource embeddingAda 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: account
  name: 'text-embedding-ada-002'
  dependsOn: [gpt4oStd]
  sku: {
    name: 'Standard'
    capacity: embeddingAdaCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-ada-002'
    }
  }
}

resource embedding3Large 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: account
  name: 'text-embedding-3-large'
  dependsOn: [embeddingAda]
  sku: {
    name: 'Standard'
    capacity: embedding3LargeCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-3-large'
    }
  }
}

resource embedding3Small 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: account
  name: 'text-embedding-3-small'
  dependsOn: [embedding3Large]
  sku: {
    name: 'Standard'
    capacity: embedding3SmallCapacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-3-small'
    }
  }
}
