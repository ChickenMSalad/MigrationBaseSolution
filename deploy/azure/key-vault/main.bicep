@description('Azure region for Key Vault.')
param location string = resourceGroup().location

@description('Deployment environment name such as dev, test, or prod.')
param environmentName string = 'dev'

@description('Base prefix used for resource names.')
param namePrefix string = 'migration'

@description('Key Vault name. Must be globally unique.')
param keyVaultName string = '${namePrefix}-${environmentName}-kv'

@description('Whether soft delete is enabled.')
param enableSoftDelete bool = true

@description('Soft delete retention in days.')
@minValue(7)
@maxValue(90)
param softDeleteRetentionInDays int = 30

@description('Whether purge protection is enabled. Recommended for production.')
param enablePurgeProtection bool = false

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: keyVaultName
  location: location
  properties: {
    tenantId: tenant().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    enableRbacAuthorization: true
    enabledForTemplateDeployment: false
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enableSoftDelete: enableSoftDelete
    softDeleteRetentionInDays: softDeleteRetentionInDays
    enablePurgeProtection: enablePurgeProtection
    publicNetworkAccess: 'Enabled'
  }
}

output keyVaultName string = keyVault.name
output keyVaultUri string = keyVault.properties.vaultUri
output keyVaultResourceId string = keyVault.id
