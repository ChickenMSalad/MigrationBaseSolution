# P9I Azure Monitor Trace Inspection Inventory

GeneratedUtc: 2026-05-25T02:26:00.4260670+00:00

This inventory verifies Azure Monitor trace inspection readiness for first cloud smoke execution.

## docs\p9\P9I-Azure-Monitor-Trace-Inspection.md

Present.
- Contains: Proof order
- Contains: Migration.Operational.Execution
- Contains: Success criteria

## config\templates\p9i-azure-monitor-trace-inspection-settings.template.json

Present.
- Contains: EnableTracing
- Contains: EnableAzureMonitorExporter
- Contains: AzureMonitorConnectionString

## scripts\kql\P9I-OperationalTraceInspection.kql

Present.
- Contains: SqlQueueWorkItemExecution
- Contains: ServiceBusDispatch
- Contains: ServiceBusWorkItemExecution
- Contains: migration.run.id
- Contains: migration.work_item.id

## src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivitySources.cs

Present.
- Contains: Migration.Operational.Execution
- Contains: SqlQueueWorkItemExecution
- Contains: ServiceBusDispatch
- Contains: ServiceBusWorkItemExecution

## Step reminder

Run scripts/kql/P9I-OperationalTraceInspection.kql manually in the Azure Monitor / Application Insights query window after a cloud smoke execution has run.
