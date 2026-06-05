# P9J - Azure Resource Provisioning Plan

This set moves from readiness into concrete Azure deployment preparation.

## Goal

Define the Azure resources required before deploying the runtime hosts.

## Required Azure resources

1. Resource group
2. Azure SQL Server
3. Azure SQL Database for `MigrationOperationalStore`
4. Azure Service Bus namespace
5. Service Bus queue for operational work items
6. Azure Monitor / Application Insights resource
7. Container Apps environment or App Service plan
8. Runtime apps/containers for:
   - SQL Operational Worker
   - Service Bus Dispatcher
   - Service Bus Executor
   - Admin API control plane
9. Key Vault, if secrets are not stored directly in app settings
10. Managed identities, if using identity-based access later

## Deployment posture

Provision first. Deploy disabled second. Enable last.

## Do not configure

Do not configure a production RunId override.

The runtime should discover runnable migration runs from SQL.

## Required app settings

Use the `MIGRATION_` environment variable convention.

Examples:

```text
MIGRATION_ConnectionStrings__MigrationOperationalStore
MIGRATION_OpenTelemetry__EnableTracing
MIGRATION_OpenTelemetry__EnableAzureMonitorExporter
MIGRATION_OpenTelemetry__AzureMonitorConnectionString
MIGRATION_SqlOperationalWorker__Enabled
MIGRATION_ServiceBusDispatcher__Enabled
MIGRATION_ServiceBusExecutor__Enabled
```

## Proof order

1. Provision Azure resources.
2. Apply SQL schema to Azure SQL.
3. Run SQL inspection scripts against Azure SQL.
4. Create Service Bus queue.
5. Deploy Admin API disabled/passive if applicable.
6. Deploy workers disabled.
7. Verify app settings.
8. Enable SQL Operational Worker.
9. Enable Service Bus Dispatcher.
10. Enable Service Bus Executor.
11. Run tiny smoke manifest.
12. Verify SQL state, Service Bus state, and Azure Monitor traces.

## Success criteria

- Azure SQL is reachable.
- Service Bus queue exists.
- App settings are present and use `MIGRATION_` names.
- Azure Monitor connection string is configured if exporting traces.
- No production RunId override exists.
- Runtime apps can be deployed disabled before processing starts.
