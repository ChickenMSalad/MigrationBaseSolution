# P9B Azure Environment / Resource Inventory

Purpose: establish the Azure resource baseline required before cloud execution proof-of-life.

This set does not mutate runtime code, SQL, packages, or project files.

## Required Azure resources

Minimum cloud proof-of-life requires:

- Azure SQL / SQL Server database for the operational store.
- Azure Service Bus namespace and work-item queue.
- Azure Monitor / Application Insights resource.
- Container/App Service hosting targets for:
  - `Migration.Hosts.SqlOperationalWorker`
  - `Migration.Workers.ServiceBusDispatcher`
  - `Migration.Workers.ServiceBusExecutor`
- Key Vault or app settings source for runtime secrets.

## Required runtime settings

All cloud-hosted runtime settings should use the repo-standard `MIGRATION_` environment variable prefix where possible.

Minimum settings:

```text
MIGRATION_ConnectionStrings__MigrationOperationalStore
MIGRATION_OpenTelemetry__EnableTracing
MIGRATION_OpenTelemetry__EnableAzureMonitorExporter
MIGRATION_OpenTelemetry__AzureMonitorConnectionString
MIGRATION_OpenTelemetry__TraceSamplingRatio
```

Service Bus dispatcher/executor settings depend on the existing worker option names already present in the repo. Use this set's inventory script to capture the exact option surfaces before finalizing cloud app settings.

## P9B success criteria

- Resource inventory doc exists.
- Cloud proof settings template exists.
- OTEL runtime registration is present.
- Runtime Activity usage is present.
- Worker host registration is present.
- Service Bus dispatcher/executor projects are present.
- SQL operational worker host is present.

## Next step

P9C should finalize concrete cloud app settings and deployment configuration from this inventory.
