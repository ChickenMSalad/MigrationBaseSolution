# P9K Azure CLI Resource Creation Runbook

This runbook is a command template. Review values before running anything in Azure.

## Required variables

Set these in your local shell before running Azure CLI commands.

```powershell
$subscriptionId = "<subscription-id>"
$resourceGroup = "rg-migration-runtime-dev"
$location = "eastus"
$environmentName = "dev"
$sqlServerName = "sql-migration-runtime-dev"
$sqlDatabaseName = "MigrationOperationalStore"
$serviceBusNamespace = "sb-migration-runtime-dev"
$serviceBusQueue = "migration-operational-work-items"
$appInsightsName = "appi-migration-runtime-dev"
$logAnalyticsWorkspace = "law-migration-runtime-dev"
```

## Azure CLI commands

```powershell
az account set --subscription $subscriptionId
az group create --name $resourceGroup --location $location

az monitor log-analytics workspace create `
  --resource-group $resourceGroup `
  --workspace-name $logAnalyticsWorkspace `
  --location $location

az monitor app-insights component create `
  --app $appInsightsName `
  --location $location `
  --resource-group $resourceGroup `
  --workspace $logAnalyticsWorkspace

az servicebus namespace create `
  --resource-group $resourceGroup `
  --name $serviceBusNamespace `
  --location $location `
  --sku Standard

az servicebus queue create `
  --resource-group $resourceGroup `
  --namespace-name $serviceBusNamespace `
  --name $serviceBusQueue `
  --max-delivery-count 10
```

## SQL creation note

Azure SQL server creation requires an admin login/password or identity-based workflow. Do not commit credentials. Create SQL using your chosen secure workflow, then apply the operational schema and run the P9D/P9H inspection scripts.

## After provisioning

1. Capture connection strings securely.
2. Populate app settings or Key Vault references.
3. Keep all workers disabled.
4. Run SQL inspection scripts against Azure SQL.
5. Validate Service Bus queue existence.
6. Deploy disabled apps/workers.
