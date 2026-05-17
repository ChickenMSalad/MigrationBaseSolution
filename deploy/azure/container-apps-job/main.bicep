@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Deployment environment name such as dev, test, or prod.')
param environmentName string = 'dev'

@description('Base prefix used for resource names.')
param namePrefix string = 'migration'

@description('Azure Container Apps environment name.')
param containerAppsEnvironmentName string = '${namePrefix}-${environmentName}-cae'

@description('Queue executor job name.')
param queueExecutorJobName string = '${namePrefix}-${environmentName}-queue-executor'

@description('Container image for the queue executor.')
param queueExecutorImage string = 'migration-queue-executor:local'

@description('Storage account name used by queue/control-plane/artifact settings.')
param storageAccountName string = '${namePrefix}${environmentName}sa'

@description('Queue name for migration runs.')
param runQueueName string = '${namePrefix}-runs-${environmentName}'

@description('Control-plane container name.')
param controlPlaneContainerName string = '${namePrefix}-control-plane-${environmentName}'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${namePrefix}-${environmentName}-logs'
  location: location
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: containerAppsEnvironmentName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

resource queueExecutorJob 'Microsoft.App/jobs@2024-03-01' = {
  name: queueExecutorJobName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    environmentId: containerAppsEnvironment.id
    configuration: {
      triggerType: 'Schedule'
      replicaTimeout: 1800
      replicaRetryLimit: 1
      scheduleTriggerConfig: {
        cronExpression: '*/5 * * * *'
        parallelism: 1
        replicaCompletionCount: 1
      }
    }
    template: {
      containers: [
        {
          name: 'queue-executor'
          image: queueExecutorImage
          env: [
            {
              name: 'DOTNET_ENVIRONMENT'
              value: environmentName
            }
            {
              name: 'Cloud__DeploymentProfile'
              value: environmentName
            }
            {
              name: 'Cloud__HostKind'
              value: 'azureContainerApps'
            }
            {
              name: 'Cloud__Region'
              value: location
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
              name: 'ControlPlane__StorageRoot'
              value: 'az://${controlPlaneContainerName}'
            }
            {
              name: 'MigrationRunQueue__Provider'
              value: 'AzureQueue'
            }
            {
              name: 'MigrationRunQueue__QueueName'
              value: runQueueName
            }
            {
              name: 'MigrationRunQueue__StorageAccountName'
              value: storageAccountName
            }
          ]
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
        }
      ]
    }
  }
}

output containerAppsEnvironmentName string = containerAppsEnvironment.name
output queueExecutorJobName string = queueExecutorJob.name
output queueExecutorPrincipalId string = queueExecutorJob.identity.principalId
