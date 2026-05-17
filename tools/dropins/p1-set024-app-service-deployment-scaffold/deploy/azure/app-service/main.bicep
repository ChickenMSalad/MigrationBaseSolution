@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Deployment environment name such as dev, test, or prod.')
param environmentName string = 'dev'

@description('Base prefix used for resource names.')
param namePrefix string = 'migration'

@description('App Service plan SKU.')
param appServiceSku string = 'B1'

@description('Admin API app name. Must be globally unique.')
param adminApiAppName string = '${namePrefix}-${environmentName}-admin-api'

@description('Storage account name. Must be globally unique and lowercase alphanumeric.')
param storageAccountName string = '${namePrefix}${environmentName}sa'

@description('Artifact container name.')
param artifactContainerName string = '${namePrefix}-artifacts-${environmentName}'

@description('Control-plane container name.')
param controlPlaneContainerName string = '${namePrefix}-control-plane-${environmentName}'

@description('Queue name for migration runs.')
param runQueueName string = '${namePrefix}-runs-${environmentName}'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${namePrefix}-${environmentName}-plan'
  location: location
  sku: {
    name: appServiceSku
  }
  kind: 'app'
  properties: {
    reserved: false
  }
}

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
  }
}

resource blobService 'Microsoft.Storage/storageAccounts/blobServices@2023-05-01' = {
  parent: storage
  name: 'default'
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

resource queueService 'Microsoft.Storage/storageAccounts/queueServices@2023-05-01' = {
  parent: storage
  name: 'default'
}

resource runQueue 'Microsoft.Storage/storageAccounts/queueServices/queues@2023-05-01' = {
  parent: queueService
  name: runQueueName
}

resource adminApi 'Microsoft.Web/sites@2023-12-01' = {
  name: adminApiAppName
  location: location
  kind: 'app'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      alwaysOn: true
      healthCheckPath: '/health/ready'
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName
        }
        {
          name: 'Cloud__DeploymentProfile'
          value: environmentName
        }
        {
          name: 'Cloud__HostKind'
          value: 'azureAppService'
        }
        {
          name: 'Cloud__Region'
          value: location
        }
        {
          name: 'Cloud__Sku'
          value: appServiceSku
        }
        {
          name: 'Cloud__CredentialMode'
          value: 'managedIdentity'
        }
        {
          name: 'Cloud__ArtifactMode'
          value: 'azureBlob'
        }
        {
          name: 'Cloud__ArtifactContainerName'
          value: artifactContainerName
        }
        {
          name: 'Cloud__ArtifactStorageAccountName'
          value: storage.name
        }
        {
          name: 'Cloud__QueueStorageAccountName'
          value: storage.name
        }
        {
          name: 'ControlPlane__StorageRoot'
          value: 'az://${controlPlaneContainerName}'
        }
        {
          name: 'MigrationRunQueue__Provider'
          value: 'AzureQueue'
        }
        {
          name: 'MigrationRunQueue__QueueName'
          value: runQueue.name
        }
        {
          name: 'MigrationRunQueue__StorageAccountName'
          value: storage.name
        }
      ]
    }
  }
}

output adminApiUrl string = 'https://${adminApi.properties.defaultHostName}'
output adminApiPrincipalId string = adminApi.identity.principalId
output storageAccountName string = storage.name
output artifactContainer string = artifactContainer.name
output controlPlaneContainer string = controlPlaneContainer.name
output runQueue string = runQueue.name
