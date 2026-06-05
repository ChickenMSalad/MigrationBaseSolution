# P9D SQL Operational Store Cloud Validation Inventory

GeneratedUtc: 2026-05-25T01:57:42.3579514+00:00

This inventory verifies the repository-side cloud SQL operational store validation surfaces before Azure SQL smoke execution.

## docs\p9\P9D-Sql-Operational-Store-Cloud-Validation.md

Present.
- Contains: ConnectionStrings:MigrationOperationalStore
- Contains: MIGRATION_ConnectionStrings__MigrationOperationalStore
- Contains: RunId is uniqueidentifier / Guid
- Contains: WorkItemId is bigint / long

## scripts\sql\P9D-InspectOperationalStore.sql

Present.
- Contains: sys.tables
- Contains: sys.columns
- Contains: sys.foreign_keys
- Contains: sys.indexes
- Contains: ApproximateRows

## config\templates\p9d-sql-operational-store-cloud-settings.template.json

Present.
- Contains: MigrationOperationalStore
- Contains: OpenTelemetry
- Contains: SqlOperationalWorker
- Contains: SqlOperationalQueueExecutor

## src\Hosts\Migration.Hosts.SqlOperationalWorker\Program.cs

Present.
- Contains: AddOperationalOpenTelemetry
- Contains: AddEnvironmentVariables(prefix: "MIGRATION_")

## Recommended next checks

- Run scripts/sql/P9D-InspectOperationalStore.sql against the target operational database.
- Confirm WorkItemId columns are bigint / long and RunId remains uniqueidentifier / Guid.
- Confirm production cloud settings do not require a RunId override.
