@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('Short environment name, for example dev, test, prod.')
param environmentName string = 'dev'

@description('Resource name prefix. Keep short enough for globally unique resources.')
param namePrefix string = 'mbs'

@description('SQL administrator login. Prefer Microsoft Entra/managed identity for production hardening after scaffold deployment.')
param sqlAdministratorLogin string

@secure()
@description('SQL administrator password for scaffold deployment only.')
param sqlAdministratorPassword string

@description('SQL database SKU.')
param sqlDatabaseSkuName string = 'S0'

@description('Service Bus SKU.')
@allowed([
  'Basic'
  'Standard'
  'Premium'
])
param serviceBusSkuName string = 'Standard'

@description('Container Apps environment workload profile name is intentionally omitted for consumption profile simplicity.')
param tags object = {
  application: 'MigrationBaseSolution'
  environment: environmentName
  phase: 'P4.8'
}

var normalizedPrefix = toLower(replace(namePrefix, '-', ''))
var uniqueSuffix = uniqueString(resourceGroup().id, namePrefix, environmentName)
var sqlServerName = '${normalizedPrefix}-${environmentName}-sql-${uniqueSuffix}'
var sqlDatabaseName = 'migration-operational'
var serviceBusNamespaceName = '${normalizedPrefix}-${environmentName}-sb-${uniqueSuffix}'
var workItemQueueName = 'migration-work-items'
var storageName = take('${normalizedPrefix}${environmentName}st${uniqueSuffix}', 24)
var logAnalyticsName = '${namePrefix}-${environmentName}-log'
var appInsightsName = '${namePrefix}-${environmentName}-appi'
var keyVaultName = take('${normalizedPrefix}-${environmentName}-kv-${uniqueSuffix}', 24)
var appConfigName = '${namePrefix}-${environmentName}-appcfg-${uniqueSuffix}'
var containerAppsEnvironmentName = '${namePrefix}-${environmentName}-cae'

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  tags: tags
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

resource sqlServer 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: sqlServerName
  location: location
  tags: tags
  properties: {
    administratorLogin: sqlAdministratorLogin
    administratorLoginPassword: sqlAdministratorPassword
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
  }
}

resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  tags: tags
  sku: {
    name: sqlDatabaseSkuName
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 268435456000
  }
}

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: serviceBusNamespaceName
  location: location
  tags: tags
  sku: {
    name: serviceBusSkuName
    tier: serviceBusSkuName
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: false
  }
}

resource workItemQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: workItemQueueName
  properties: {
    lockDuration: 'PT5M'
    maxDeliveryCount: 10
    requiresDuplicateDetection: true
    duplicateDetectionHistoryTimeWindow: 'PT10M'
    deadLetteringOnMessageExpiration: true
    enablePartitioning: serviceBusSkuName == 'Premium' ? false : true
  }
}

resource storage 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    supportsHttpsTrafficOnly: true
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  tags: tags
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enabledForDeployment: true
    enabledForTemplateDeployment: true
    publicNetworkAccess: 'Enabled'
  }
}

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2023-03-01' = {
  name: appConfigName
  location: location
  tags: tags
  sku: {
    name: 'standard'
  }
  properties: {
    disableLocalAuth: false
    publicNetworkAccess: 'Enabled'
  }
}

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: containerAppsEnvironmentName
  location: location
  tags: tags
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

output sqlServerFullyQualifiedDomainName string = sqlServer.properties.fullyQualifiedDomainName
output sqlDatabaseName string = sqlDatabase.name
output serviceBusNamespaceName string = serviceBusNamespace.name
output serviceBusWorkItemQueueName string = workItemQueue.name
output storageAccountName string = storage.name
output keyVaultName string = keyVault.name
output appConfigurationName string = appConfig.name
output applicationInsightsConnectionString string = appInsights.properties.ConnectionString
output containerAppsEnvironmentName string = containerAppsEnvironment.name
