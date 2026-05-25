# P9F Cloud Worker Deployment Inventory

GeneratedUtc: 2026-05-25T02:11:03.7427069+00:00

This inventory verifies repository-side worker deployment readiness before deploying cloud worker roles.

## docs\p9\P9F-Cloud-Worker-Deployment-Readiness.md

Present.
- Contains: SQL Operational Worker
- Contains: Service Bus Dispatcher
- Contains: Service Bus Executor
- Contains: Do not configure a production RunId override

## config\templates\p9f-cloud-worker-deployment-settings.template.json

Present.
- Contains: MigrationOperationalStore
- Contains: OpenTelemetry
- Contains: SqlOperationalWorker
- Contains: ServiceBusDispatcher
- Contains: ServiceBusExecutor

## src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs

Present.
- Contains: AddOperationalOpenTelemetry
- Contains: AddEnvironmentVariables(prefix: "MIGRATION_")

## src\Workers\Migration.Workers.ServiceBusDispatcher\Program.cs

Present.
- Contains: AddOperationalOpenTelemetry
- Contains: AddHostedService

## src\Workers\Migration.Workers.ServiceBusExecutor\Program.cs

Present.
- Contains: AddOperationalOpenTelemetry
- Contains: AddHostedService

## Recommended next checks

- Build all worker projects.
- Deploy workers disabled first if the host settings support enabled flags.
- Enable dispatcher and executor only after SQL and Service Bus validation are complete.
- Verify Azure Monitor traces for Migration.Operational.Execution after the first smoke run.
