# P9F Cloud Worker Deployment Readiness

P9F prepares the three operational worker roles for cloud deployment without changing runtime source code.

## Worker roles

Deploy these roles as separate cloud processes/containers unless you intentionally choose a combined host later:

1. SQL Operational Worker
   - Project: `src/Hosts/Migration.Hosts.SqlOperationalWorker`
   - Purpose: SQL-backed runnable-run discovery and local SQL queue execution.

2. Service Bus Dispatcher
   - Project: `src/Workers/Migration.Workers.ServiceBusDispatcher`
   - Purpose: reads SQL-dispatchable work items and sends Service Bus messages.

3. Service Bus Executor
   - Project: `src/Workers/Migration.Workers.ServiceBusExecutor`
   - Purpose: receives Service Bus messages and completes/fails SQL operational work items.

## Deployment order

1. Apply/validate the SQL operational store schema.
2. Create/validate Service Bus namespace and queue.
3. Configure shared cloud settings.
4. Deploy SQL Operational Worker with worker execution disabled if you only want readiness first.
5. Deploy Service Bus Dispatcher disabled, then enable after queue validation.
6. Deploy Service Bus Executor disabled, then enable after dispatcher validation.
7. Enable one role at a time and inspect logs/traces.

## Required settings

Use the existing `MIGRATION_` environment variable convention. Do not introduce new setting names unless the repo already supports them.

Minimum shared settings:

- `MIGRATION_ConnectionStrings__MigrationOperationalStore`
- `MIGRATION_OpenTelemetry__EnableTracing`
- `MIGRATION_OpenTelemetry__EnableAzureMonitorExporter`
- `MIGRATION_OpenTelemetry__AzureMonitorConnectionString`
- `MIGRATION_OpenTelemetry__TraceSamplingRatio`

SQL Operational Worker settings:

- `MIGRATION_SqlOperationalWorker__Enabled`
- `MIGRATION_SqlOperationalWorker__PollingIntervalSeconds`
- `MIGRATION_SqlOperationalWorker__BatchSize`
- `MIGRATION_SqlOperationalWorker__LeaseSeconds`
- `MIGRATION_SqlOperationalWorker__PartitionKey`
- `MIGRATION_SqlOperationalWorker__CompleteNoOpWorkItems`

Service Bus Dispatcher settings:

- Use the setting names already present in the dispatcher options class and appsettings templates.
- Required concepts: queue name, Service Bus connection/namespace, worker id, enabled flag, batch size/polling interval.

Service Bus Executor settings:

- Use the setting names already present in the executor options class and appsettings templates.
- Required concepts: queue name, Service Bus connection/namespace, worker id, retry delay, enabled flag.

## Production RunId policy

Do not configure a production RunId override. Cloud workers should discover runnable runs from SQL operational state.

## Proof checks

Before enabling all workers simultaneously, verify:

- SQL worker starts and readiness passes.
- Dispatcher starts and can resolve SQL + Service Bus settings.
- Executor starts and can resolve SQL + Service Bus settings.
- Azure Monitor receives traces for `Migration.Operational.Execution`.
- Service Bus queue has expected active/dead-letter counts.
- SQL operational tables show expected work item status transitions.

## Success criteria

P9F is complete when the repository contains a clear deployment checklist and generated inventory proving the runtime worker projects have:

- OpenTelemetry registration.
- Environment variable configuration support where applicable.
- Hosted service registration.
- Deployment settings templates.
- No required production RunId override.
