# P9H First Cloud Smoke Execution Inventory

GeneratedUtc: 2026-05-25T02:18:52.9738012+00:00

This inventory verifies repository-side first cloud smoke execution readiness before running deployed workers against Azure SQL, Service Bus, and Azure Monitor.

## docs\p9\P9H-First-Cloud-Smoke-Execution.md

Present.
- Missing: Proof order
- Contains: Success criteria
- Contains: Do not configure a production RunId override

## config\templates\p9h-first-cloud-smoke-execution-settings.template.json

Present.
- Contains: MigrationOperationalStore
- Contains: ServiceBusDispatcher
- Contains: ServiceBusExecutor
- Contains: OpenTelemetry

## scripts\sql\P9H-InspectCloudSmokeState.sql

Present.
- Contains: sys.tables
- Contains: sys.columns
- Contains: RunId
- Contains: WorkItemId

## src\Core\Migration.Application\Operational\Telemetry\OperationalOpenTelemetryServiceCollectionExtensions.cs

Present.
- Contains: AddOperationalOpenTelemetry
- Contains: AddAzureMonitorTraceExporter

## src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivitySources.cs

Present.
- Contains: Migration.Operational.Execution
- Contains: SqlQueueWorkItemExecution
- Contains: ServiceBusDispatch
- Contains: ServiceBusWorkItemExecution

## src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs

Present.
- Contains: StartServiceBusDispatch
- Contains: SetExecutionDuration
- Contains: SetExecutionResult

## src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs

Present.
- Contains: StartServiceBusWorkItemExecution
- Contains: SetExecutionDuration
- Contains: SetExecutionResult

## src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs

Present.
- Contains: StartSqlQueueWorkItemExecution
- Contains: SetExecutionDuration
- Contains: SetExecutionResult

## Recommended next checks

- Run scripts/sql/P9H-InspectCloudSmokeState.sql against the target Azure SQL operational database.
- Deploy worker roles disabled first if enabled flags are available.
- Enable SQL worker, dispatcher, and executor in that order.
- Run a tiny operational manifest smoke test.
- Verify Azure Monitor traces for Migration.Operational.Execution.
