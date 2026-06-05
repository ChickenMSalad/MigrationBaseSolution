# P9E Service Bus Topology Validation Inventory

GeneratedUtc: 2026-05-25T02:01:47.4355755+00:00

This inventory verifies repository-side Service Bus topology validation surfaces before cloud queue proof execution.

## docs\p9\P9E-Service-Bus-Topology-Validation.md

Present.
- Contains: Service Bus dispatcher
- Contains: Service Bus executor
- Contains: ServiceBusDispatch
- Contains: ServiceBusWorkItemExecution
- Contains: Do not invent a new setting name

## config\templates\p9e-service-bus-topology-settings.template.json

Present.
- Contains: MigrationOperationalStore
- Contains: ServiceBusDispatcher
- Contains: ServiceBusExecutor
- Contains: OpenTelemetry
- Contains: AzureMonitorConnectionString

## src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs

Present.
- Contains: AddOperationalOpenTelemetry
- Contains: AddHostedService

## src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs

Present.
- Contains: AddOperationalOpenTelemetry
- Contains: AddHostedService
