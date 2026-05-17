@description('Principal id for the Admin API managed identity.')
param adminApiPrincipalId string

@description('Principal id for the Queue Executor managed identity.')
param queueExecutorPrincipalId string

@description('Storage account resource id.')
param storageAccountResourceId string

@description('Key Vault resource id. Leave empty to skip Key Vault role assignments.')
param keyVaultResourceId string = ''

// Built-in role definition ids.
var storageBlobDataContributorRoleId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
var storageQueueDataContributorRoleId = '974c5e8b-45b9-4653-ba55-5f855dd0fb88'
var keyVaultSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'

resource adminBlobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountResourceId, adminApiPrincipalId, storageBlobDataContributorRoleId)
  scope: resourceId(storageAccountResourceId)
  properties: {
    principalId: adminApiPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalType: 'ServicePrincipal'
  }
}

resource adminQueueRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountResourceId, adminApiPrincipalId, storageQueueDataContributorRoleId)
  scope: resourceId(storageAccountResourceId)
  properties: {
    principalId: adminApiPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageQueueDataContributorRoleId)
    principalType: 'ServicePrincipal'
  }
}

resource workerBlobRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountResourceId, queueExecutorPrincipalId, storageBlobDataContributorRoleId)
  scope: resourceId(storageAccountResourceId)
  properties: {
    principalId: queueExecutorPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleId)
    principalType: 'ServicePrincipal'
  }
}

resource workerQueueRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(storageAccountResourceId, queueExecutorPrincipalId, storageQueueDataContributorRoleId)
  scope: resourceId(storageAccountResourceId)
  properties: {
    principalId: queueExecutorPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', storageQueueDataContributorRoleId)
    principalType: 'ServicePrincipal'
  }
}

resource adminKeyVaultRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(keyVaultResourceId)) {
  name: guid(keyVaultResourceId, adminApiPrincipalId, keyVaultSecretsUserRoleId)
  scope: resourceId(keyVaultResourceId)
  properties: {
    principalId: adminApiPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalType: 'ServicePrincipal'
  }
}

resource workerKeyVaultRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (!empty(keyVaultResourceId)) {
  name: guid(keyVaultResourceId, queueExecutorPrincipalId, keyVaultSecretsUserRoleId)
  scope: resourceId(keyVaultResourceId)
  properties: {
    principalId: queueExecutorPrincipalId
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleId)
    principalType: 'ServicePrincipal'
  }
}
