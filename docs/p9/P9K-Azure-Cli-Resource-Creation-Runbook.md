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



$db = "MigrationOperationalStore"
$sqlAdmin = "migrationadmin"

$sqlPassword = Read-Host "Enter SQL admin password" -AsSecureString
$sqlPasswordPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sqlPassword)
)


$location = "westus3"
$sqlServer = "mbsdeanruntime$(Get-Random -Minimum 10000 -Maximum 99999)"  

az sql server create `
  --name $sqlServer `
  --resource-group $resourceGroup `
  --location $location `
  --admin-user $sqlAdmin `
  --admin-password $sqlPasswordPlain

az sql db create `
  --resource-group $resourceGroup `
  --server $sqlServer `
  --name $db `
  --service-objective Basic

az sql server firewall-rule create `
  --resource-group $resourceGroup `
  --server $sqlServer `
  --name AllowAzureServices `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0

$myIp = (Invoke-RestMethod https://api.ipify.org)

az sql server firewall-rule create `
  --resource-group $resourceGroup `
  --server $sqlServer `
  --name AllowMyCurrentIp `
  --start-ip-address $myIp `
  --end-ip-address $myIp

$connectionString = "Server=tcp:$sqlServer.database.windows.net,1433;Initial Catalog=$db;Persist Security Info=False;User ID=$sqlAdmin;Password=$sqlPasswordPlain;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

sqlcmd -S "$sqlServer.database.windows.net" -d $db -U $sqlAdmin -P $sqlPasswordPlain -i .\scripts\sql\P9D-InspectOperationalStore.sql
sqlcmd -S "$sqlServer.database.windows.net" -d $db -U $sqlAdmin -P $sqlPasswordPlain -i .\scripts\sql\P9H-InspectCloudSmokeState.sql

## After provisioning

1. Capture connection strings securely.
2. Populate app settings or Key Vault references.
3. Keep all workers disabled.
4. Run SQL inspection scripts against Azure SQL.
5. Validate Service Bus queue existence.
6. Deploy disabled apps/workers.


az servicebus queue show `
  --resource-group $resourceGroup `
  --namespace-name $serviceBusNamespace `
  --name $serviceBusQueue `
  -o table

MIGRATION_ConnectionStrings__MigrationOperationalStore
MIGRATION_OpenTelemetry__EnableTracing
MIGRATION_OpenTelemetry__EnableAzureMonitorExporter
MIGRATION_OpenTelemetry__AzureMonitorConnectionString

$commonSettings = @(
  "MIGRATION_ConnectionStrings__MigrationOperationalStore=$sqlConnectionString",
  "MIGRATION_OpenTelemetry__EnableTracing=true",
  "MIGRATION_OpenTelemetry__EnableAzureMonitorExporter=false",
  "MIGRATION_ServiceBusDispatcher__Enabled=false",
  "MIGRATION_ServiceBusExecutor__Enabled=false",
  "MIGRATION_SqlOperationalWorker__Enabled=false"
)

1. app service plan

$appServicePlan = "asp-migration-runtime-dev"

az appservice plan create `
  --name $appServicePlan `
  --resource-group $resourceGroup `
  --location $location `
  --is-linux `
  --sku B1

az appservice plan show `
  --name $appServicePlan `
  --resource-group $resourceGroup `
  -o table

$sqlWorkerApp = "app-migration-sql-worker-dev"
$dispatcherApp = "app-migration-sb-dispatcher-dev"
$executorApp = "app-migration-sb-executor-dev"
$adminApiApp = "app-migration-admin-api-dev"

az webapp create `
  --resource-group $resourceGroup `
  --plan $appServicePlan `
  --name $sqlWorkerApp `
  --runtime "DOTNETCORE:8.0"

az webapp create `
  --resource-group $resourceGroup `
  --plan $appServicePlan `
  --name $dispatcherApp `
  --runtime "DOTNETCORE:8.0"

az webapp create `
  --resource-group $resourceGroup `
  --plan $appServicePlan `
  --name $executorApp `
  --runtime "DOTNETCORE:8.0"

az webapp create `
  --resource-group $resourceGroup `
  --plan $appServicePlan `
  --name $adminApiApp `
  --runtime "DOTNETCORE:8.0"

az webapp list `
  --resource-group $resourceGroup `
  --query "[].{Name:name, State:state, Host:defaultHostName}" `
  -o table

2. For each deployed app/worker, apply settings before starting

Example App Service:

az webapp config appsettings set `
  --resource-group $resourceGroup `
  --name $sqlWorkerApp `
  --settings $commonSettings

az webapp config appsettings set `
  --resource-group $resourceGroup `
  --name $dispatcherApp `
  --settings $commonSettings

az webapp config appsettings set `
  --resource-group $resourceGroup `
  --name $executorApp `
  --settings $commonSettings

az webapp config appsettings set `
  --resource-group $resourceGroup `
  --name $adminApiApp `
  --settings $commonSettings


3. publish


dotnet publish .\src\Hosts\Migration.Hosts.SqlOperationalWorker\Migration.Hosts.SqlOperationalWorker.csproj -c Release -o .\artifacts\publish\sql-worker
dotnet publish .\src\Workers\Migration.Workers.ServiceBusDispatcher\Migration.Workers.ServiceBusDispatcher.csproj -c Release -o .\artifacts\publish\sb-dispatcher
dotnet publish .\src\Workers\Migration.Workers.ServiceBusExecutor\Migration.Workers.ServiceBusExecutor.csproj -c Release -o .\artifacts\publish\sb-executor
dotnet publish .\src\Core\Migration.Admin.Api\Migration.Admin.Api.csproj -c Release -o .\artifacts\publish\admin-api

*************no**************

Compress-Archive -Path .\artifacts\publish\sql-worker\* -DestinationPath .\artifacts\publish\sql-worker.zip -Force
Compress-Archive -Path .\artifacts\publish\sb-dispatcher\* -DestinationPath .\artifacts\publish\sb-dispatcher.zip -Force
Compress-Archive -Path .\artifacts\publish\sb-executor\* -DestinationPath .\artifacts\publish\sb-executor.zip -Force
Compress-Archive -Path .\artifacts\publish\admin-api\* -DestinationPath .\artifacts\publish\admin-api.zip -Force

az webapp deploy --resource-group $resourceGroup --name $sqlWorkerApp --src-path .\artifacts\publish\sql-worker.zip --type zip
az webapp deploy --resource-group $resourceGroup --name $dispatcherApp --src-path .\artifacts\publish\sb-dispatcher.zip --type zip
az webapp deploy --resource-group $resourceGroup --name $executorApp --src-path .\artifacts\publish\sb-executor.zip --type zip
az webapp deploy --resource-group $resourceGroup --name $adminApiApp --src-path .\artifacts\publish\admin-api.zip --type zip

*****************************

az webapp deployment source config-zip `
  --resource-group $resourceGroup `
  --name $adminApiApp `
  --src .\artifacts\publish\admin-api.zip

cd .\artifacts\publish\sb-dispatcher
tar -a -c -f ..\sb-dispatcher.zip *
cd ..\..\..

cd .\artifacts\publish\sb-executor
tar -a -c -f ..\sb-executor.zip *
cd ..\..\..

cd .\artifacts\publish\sql-worker
tar -a -c -f ..\sql-worker.zip *
cd ..\..\..

az webapp deployment source config-zip `
  --resource-group $resourceGroup `
  --name $dispatcherApp `
  --src .\artifacts\publish\sb-dispatcher.zip

az webapp deployment source config-zip `
  --resource-group $resourceGroup `
  --name $executorApp `
  --src .\artifacts\publish\sb-executor.zip

az webapp deployment source config-zip `
  --resource-group $resourceGroup `
  --name $sqlWorkerApp `
  --src .\artifacts\publish\sql-worker.zip


az webapp config set `
  --resource-group $resourceGroup `
  --name $dispatcherApp `
  --startup-file "dotnet Migration.Workers.ServiceBusDispatcher.dll"

az webapp config set `
  --resource-group $resourceGroup `
  --name $executorApp `
  --startup-file "dotnet Migration.Workers.ServiceBusExecutor.dll"

az webapp config set `
  --resource-group $resourceGroup `
  --name $sqlWorkerApp `
  --startup-file "dotnet Migration.Hosts.SqlOperationalWorker.dll"

az webapp restart `
  --resource-group $resourceGroup `
  --name $dispatcherApp

az webapp restart `
  --resource-group $resourceGroup `
  --name $executorApp

az webapp restart `
  --resource-group $resourceGroup `
  --name $sqlWorkerApp


az webapp list `
  --resource-group $resourceGroup `
  --query "[].{Name:name, State:state, Host:defaultHostName}" `
  -o table