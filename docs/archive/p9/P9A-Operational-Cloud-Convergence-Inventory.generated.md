# P9A Operational Cloud Convergence Inventory

GeneratedUtc: 2026-05-25T01:31:22.0997971+00:00

This inventory verifies that P8 runtime execution, telemetry, cloud host, and proof-of-life surfaces are ready for P9 operational convergence.

## src\Core\Migration.Application\Operational\Telemetry\OperationalOpenTelemetryServiceCollectionExtensions.cs

Present.
- Contains: `AddOperationalOpenTelemetry`
- Contains: `AddOpenTelemetry`
- Contains: `AddSource`
- Contains: `AddAzureMonitorTraceExporter`

## src\Core\Migration.Application\Operational\Telemetry\OperationalExecutionActivitySources.cs

Present.
- Contains: `Migration.Operational.Execution`
- Contains: `SqlQueueWorkItemExecution`
- Contains: `ServiceBusDispatch`
- Contains: `ServiceBusWorkItemExecution`

## src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs

Present.
- Contains: `AddOperationalOpenTelemetry`
- Contains: `AddEnvironmentVariables(prefix: "MIGRATION_")`

## src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs

Present.
- Contains: `AddOperationalOpenTelemetry`

## src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs

Present.
- Contains: `AddOperationalOpenTelemetry`

## src\Workers\Migration.Workers.QueueExecutor\Services\SqlOperationalWorkItemWorker.cs

Present.
- Contains: `StartSqlQueueWorkItemExecution`
- Contains: `SetExecutionDuration`
- Contains: `SetExecutionResult`

## src\Workers\Migration.Workers.ServiceBusDispatcher\Dispatching\SqlWorkItemDispatcher.cs

Present.
- Contains: `StartServiceBusDispatch`
- Contains: `SetExecutionDuration`
- Contains: `SetExecutionResult`

## src\Workers\Migration.Workers.ServiceBusExecutor\Runtime\SqlServiceBusExecutorWorker.cs

Present.
- Contains: `StartServiceBusWorkItemExecution`
- Contains: `SetExecutionDuration`
- Contains: `SetExecutionResult`

## docs\p9\P9A-Operational-Cloud-Convergence-Proof.md

Present.
- Contains: `Proof order`
- Contains: `Success criteria`

## config\templates\p9a-operational-cloud-proof-settings.template.json

Present.
- Contains: `EnableTracing`
- Contains: `EnableAzureMonitorExporter`
- Contains: `TraceSamplingRatio`
