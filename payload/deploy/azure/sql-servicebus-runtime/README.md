# P4.8 SQL + Service Bus Runtime Azure Scaffold

This deployment scaffold provisions the Azure runtime resources required by the P4 SQL-first execution path:

| Area | Resource |
|---|---|
| Durable store | Azure SQL Server + `migration-operational` database |
| Execution messaging | Azure Service Bus namespace + `migration-work-items` queue |
| File/artifact storage | Azure Storage Account |
| Secret management | Azure Key Vault |
| Runtime configuration | Azure App Configuration |
| Observability | Log Analytics + Application Insights |
| Compute environment | Container Apps managed environment |

This set intentionally does **not** deploy application containers yet. It prepares the shared cloud runtime substrate for the Admin API, dispatcher, and executor workers.

## Preview

```powershell
./deploy/azure/sql-servicebus-runtime/deploy-sql-servicebus-runtime.ps1 `
  -ResourceGroupName rg-mbs-dev `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix mbs `
  -SqlAdministratorLogin sqladminuser `
  -SqlAdministratorPassword (Read-Host -AsSecureString "SQL admin password") `
  -WhatIf
```

## Deploy

```powershell
./deploy/azure/sql-servicebus-runtime/deploy-sql-servicebus-runtime.ps1 `
  -ResourceGroupName rg-mbs-dev `
  -Location eastus `
  -EnvironmentName dev `
  -NamePrefix mbs `
  -SqlAdministratorLogin sqladminuser `
  -SqlAdministratorPassword (Read-Host -AsSecureString "SQL admin password")
```

## Post-deployment wiring

Use the deployment outputs to configure:

- `ConnectionStrings:MigrationOperationalStore`
- `ServiceBus:ConnectionString` or managed identity settings
- `ServiceBus:WorkItemQueueName`
- Application Insights connection string
- Key Vault URI
- App Configuration endpoint
