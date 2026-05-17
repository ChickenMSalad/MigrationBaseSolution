@description('Azure region for storage resources.')
param location string = resourceGroup().location

@description('Deployment environment name such as dev, test, or prod.')
param environmentName string = 'dev'

@description('Base prefix used for resource names.')
param namePrefix string = 'migration'

@description('Storage account name. Must be globally unique and lowercase alphanumeric.')
param storageAccountName string = '${namePrefix}${environmentName}sa'

@description('Artifact container name.')
param artifactContainerName string = '${namePrefix}-artifacts-${environmentName}'

@description('Control-plane container name.')
param controlPlaneContainerName string = '${namePrefix}-control-plane-${environmentName}'

@description('Audit container name.')
param auditContainerName string = '${namePrefix}-audit-${environmentName}'

@description('Queue name for migration runs.')
param runQueueName string = '${namePrefix}-runs-${environmentName}'

resource storage 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
  properties: {
    deleteRetentionPolicy: {
      enabled: true
      days: 7
    }
    containerDeleteRetentionPolicy: {
      enabled: true
      days: 7
    }
  }
}

resource artifactContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: artifactContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource controlPlaneContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: controlPlaneContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource auditContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-05-01' = {
  parent: blobService
  name: auditContainerName
  properties: {
    publicAccess: 'None'
  }
}

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource runQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueService
  name: runQueueName
}

output storageAccountName string = storage.name
output storageAccountResourceId string = storage.id
output artifactContainerName string = artifactContainer.name
output controlPlaneContainerName string = controlPlaneContainer.name
output auditContainerName string = auditContainer.name
output runQueueName string = runQueue.name
