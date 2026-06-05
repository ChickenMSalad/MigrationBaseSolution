# P9H First Cloud Smoke Execution

Purpose: prove the deployed cloud runtime can execute a small operational run end-to-end without changing runtime code.

## Scope

This set validates the first cloud proof-of-life execution path:

1. Azure SQL operational store is reachable.
2. SQL operational schema is present.
3. Worker deployment settings are configured without a production RunId override.
4. Service Bus dispatcher and executor settings are present.
5. OpenTelemetry/Azure Monitor settings are present.
6. A small run can be observed through SQL status and Azure Monitor traces.

## Required runtime roles

- Admin API / control plane
- SQL Operational Worker
- Service Bus Dispatcher
- Service Bus Executor

## Required settings

Use existing configuration names only. Do not invent new setting names.

Required baseline settings:

- ConnectionStrings:MigrationOperationalStore
- SqlOperationalWorker:Enabled
- SqlOperationalWorker:BatchSize
- SqlOperationalWorker:LeaseSeconds
- ServiceBusDispatcher:Enabled
- ServiceBusExecutor:Enabled
- OpenTelemetry:EnableTracing
- OpenTelemetry:EnableAzureMonitorExporter
- OpenTelemetry:AzureMonitorConnectionString or APPLICATIONINSIGHTS_CONNECTION_STRING

## Production RunId rule

Do not configure a production RunId override.

The worker should discover runnable SQL operational runs from the operational store. A RunId override is only acceptable for local diagnostics.

## Smoke execution order

1. Confirm Azure SQL schema and row counts with scripts/sql/P9H-InspectCloudSmokeState.sql.
2. Deploy roles with worker execution disabled if supported by the host settings.
3. Verify /health/live and /health/ready for the Admin API.
4. Enable SQL Operational Worker.
5. Enable Service Bus Dispatcher.
6. Enable Service Bus Executor.
7. Start a tiny operational run using the existing run creation/enqueue path.
8. Watch SQL operational run/work item state.
9. Verify Azure Monitor traces for ActivitySource Migration.Operational.Execution.
10. Confirm no dead-letter messages were produced unless the test intentionally forced one.

## Expected Activity names

- SqlQueueWorkItemExecution
- ServiceBusDispatch
- ServiceBusWorkItemExecution

## Success criteria

P9H is successful when:

- The runtime starts without configuration failures.
- SQL operational store connectivity works from deployed roles.
- Dispatcher can send a work item to Service Bus.
- Executor can receive and execute the work item.
- The work item reaches Completed or an expected retry/failure state.
- Azure Monitor receives traces from Migration.Operational.Execution.
- RunId remains Guid/uniqueidentifier.
- WorkItemId remains long/bigint.
- No production RunId override is required.
